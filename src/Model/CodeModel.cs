﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using AutoRest.Core.Utilities;
using AutoRest.Core.Utilities.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Core.Model
{
    /// <summary>
    /// Defines the client model for every service.
    /// </summary>
    public partial class CodeModel : ICodeModel
    {
        private string _documentation;
        private string _namespace;
        private string _modelsName;
        private string _name;

        [JsonIgnore]
        public virtual bool IsAzure => false;

        partial void InitializeCollections();
        /// <summary>
        /// Creates a new instance of Client class.
        /// </summary>
        protected CodeModel()
        {
            InitializeCollections();
        }

        public IEnumerable<Parameter> HostParametersFront { get; set; }
        public IEnumerable<Parameter> HostParametersBack { get; set; }

        [JsonIgnore]
        public IEnumerable<CompositeType> AllModelTypes => ModelTypes.Union(HeaderTypes).Union(ErrorTypes);

        [JsonIgnore]
        public virtual IEnumerable<Method> Methods => Operations.SelectMany(group => group.Methods);

        public virtual Method Add(Method method)
        {
            if (method.Group.IsNullOrEmpty())
            {
                method.Group.Value = string.Empty;
            }
            // methods are delegated to the method group of their choice. 
            return GetOrAddMethodGroup(method.Group).Add(method);
        }
        public virtual void AddRange(IEnumerable<Method> methods)
        {
            foreach (var method in methods)
            {
                Add(method);
            }
        }

        public virtual MethodGroup GetOrAddMethodGroup(string groupName)
        {
            groupName = groupName ?? string.Empty;
            return Operations.FirstOrDefault(group => @group.Name.EqualsIgnoreCase(groupName)) ??
                   Add(New<MethodGroup>(groupName));
        }

        /// <summary>
        /// Gets or sets the non-canonical name of the client model.
        /// </summary>
        public string Name
        {
            get => (Settings.Instance?.ClientName).Else(CodeNamer.Instance.GetClientName(_name));
            set
            {
                _name = value;
                Children.Disambiguate();
            }
        }

        /// <summary>
        /// Gets or sets the base namespace of the client model if applicable.
        /// </summary>
        public virtual string Namespace
        {
            get => _namespace;
            set
            {
                if (string.IsNullOrWhiteSpace(_namespace))
                {
                    if (value != _namespace)
                    {
                        _namespace = CodeNamer.Instance.GetNamespaceName(value);
                        Children.Disambiguate();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the Models Name if applicable.
        /// </summary>
        public virtual string ModelsName
        {
            get => _modelsName;
            set => _modelsName = CodeNamer.Instance.GetNamespaceName(value);
        }

        /// <summary>
        /// Gets or sets the version of the API described by this service.
        /// </summary>
        public virtual string ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets the base url of the service.  This can be a templated url.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1056:UriPropertiesShouldNotBeStrings",
            Justification = "Url might be used as a template, thus making " +
            "it invalid url in certain scenarios.")]
        public virtual string BaseUrl { get; set; }

        /// <summary>
        /// Gets the method groups.
        /// </summary>
        [JsonIgnore]
        public virtual IEnumerable<string> MethodGroupNames => Operations.Where(group => !group.Name.IsNullOrEmpty()).Select(group => group.TypeName.Value);

        /// <summary>
        /// Gets or sets the documentation.
        /// </summary>
        public string Documentation
        {
            get => _documentation;
            set
            {
                if (string.IsNullOrWhiteSpace(_documentation))
                {
                    _documentation = value.StripControlCharacters();
                }
            }
        }

        /// <summary>
        /// Gets vendor extensions dictionary.
        /// </summary>
        public Dictionary<string, object> Extensions { get; private set; } = new Dictionary<string, object>();

        public Dictionary<string, object> CodeGenExtensions { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Reference to the container of this type.
        /// </summary>
        [JsonIgnore]
        CodeModel IParent.CodeModel => this;

        [JsonIgnore]
        public virtual IEnumerable<IIdentifier> IdentifiersInScope => 
            ((IEnumerable<IIdentifier>)Operations).Concat(ModelTypes).ConcatSingleItem(this);

        [JsonIgnore]
        public virtual IEnumerable<IChild> Children
            => ((IEnumerable<IChild>)ModelTypes).Concat(HeaderTypes).Concat(ErrorTypes).Concat(EnumTypes).Concat(Properties);

        [JsonIgnore]
        public string Qualifier => "Client";

        [JsonIgnore]
        public virtual IEnumerable<string> MyReservedNames
        {
            get
            {
                yield return Name;
                yield return Namespace;
                yield return ModelsName;
            }
        }

        [JsonIgnore]
        public virtual HashSet<string> LocallyUsedNames => null;
        
        [JsonIgnore]
        public bool ShouldGenerateXmlSerialization => Methods.Any(method => 
                method.RequestContentType == "application/xml" || 
                (method.ResponseContentTypes?.Any(rct => rct.StartsWith("application/xml")) ?? false)) && (Settings.Instance.Host?.GetValue<bool?>("enable-xml").Result == true);
    }
}