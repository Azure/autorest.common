﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using AutoRest.Core.Utilities;
using AutoRest.Core.Utilities.Collections;
using Newtonsoft.Json;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Core.Model
{
    public enum MethodFlavor
    {
        // default/regular (Implementation == null && ForwardTo == null && Url != null)
        RestCall,
        // forward to other method (Implementation == null && ForwardTo != null), parameters are significant (match by wirename - since displayname can be overridden)
        ForwardTo,
        // just paste the implementation (Implementation != null)
        Implementation,
    }

    /// <summary>
    /// Defines a method for the client model.
    /// </summary>
    public partial class Method : IChild
    {
        private string _description;
        private string _summary;
        private readonly Fixable<string> _name = new Fixable<string>();
        private readonly Fixable<string> _group = new Fixable<string>();
        private MethodGroup _parent;

        [JsonIgnore]
        public string Qualifier => "Method";

        [JsonIgnore]
        public MethodGroup MethodGroup
        {
            get { return _parent; }
            set
            {
                // when the reference to the parent is set
                // we should disambiguate the name 
                // it is imporant that this reference gets set before 
                // the item is actually added to the containing collection.

                if (!ReferenceEquals(_parent, value))
                {
                    _parent = value;
                    // only perform disambiguation if this item is not already 
                    // referencing the parent 

                    // (which implies that it's in the collection, but I can't prove that.)
                    Disambiguate();

                    // and if we're adding ourselves to a new parent, better make sure 
                    // our children are disambiguated too.
                    Children.Disambiguate();
                }
            }
        }

        [JsonIgnore]
        public IParent Parent => MethodGroup;

        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        //[Rule(typeof(IsIdentifier))]
        public Fixable<string> Name { get { return _name; } set { _name.CopyFrom(value); } }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public Fixable<string> Group { get { return _group; } set { _group.CopyFrom(value); } }

        partial void InitializeCollections();
        /// <summary>
        /// Initializes a new instance of the Method class.
        /// </summary>
        protected Method()
        {
            InitializeCollections();
            Name.OnGet += n => CodeNamer.Instance.GetMethodName(n.Else("unnamed_method"));
            Group.OnGet += groupName => CodeNamer.Instance.GetMethodGroupName(groupName);
        }

        public virtual void Disambiguate()
        {
            // basic behavior : get a unique name for this method.
            var originalName = Name;
            var name = CodeNamer.Instance.GetUnique(originalName, this, Parent.IdentifiersInScope, Parent.Children.Except(this.SingleItemAsEnumerable()));
            if (name != originalName)
            {
                Name = name;
            }
        }

        /// <Summary>
        /// The name on the wire for the method (the OperationId in the spec) .
        /// </Summary>
        public string SerializedName { get; set; }

        /// <summary>
        /// Gets or sets the HTTP url.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings",
            Justification = "Url might be used as a template, thus making it invalid url in certain scenarios.")]
        public virtual string Url { get; set; }

        /// <summary>
        /// Indicates whether the HTTP url is absolute.
        /// </summary>
        public bool IsAbsoluteUrl { get; set; }

        /// <summary>
        /// Gets or sets the HTTPMethod.
        /// </summary>
        public HttpMethod HttpMethod { get; set; }

        [JsonIgnore]
        public CodeModel CodeModel => Parent?.CodeModel;

        /// <summary>
        /// Gets or sets the logical parameter.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Parameter> LogicalParameters
        {
            get
            {
                 return Parameters.Where(gp => gp.Location != ParameterLocation.None)
                    .Union(InputParameterTransformation.Select(m => m.OutputParameter));
            }
        }
        
        /// <summary>
        /// Gets or sets the body parameter.
        /// </summary>
        [JsonIgnore]
        public Parameter Body => LogicalParameters.FirstOrDefault(p => p.Location == ParameterLocation.Body);

        /// <summary>
        /// Gets the list of input Parameter transformations
        /// </summary>
        public List<ParameterTransformation> InputParameterTransformation { get; private set; } = new List<ParameterTransformation>();

        /// <summary>
        /// Gets or sets response bodies by HttpStatusCode.
        /// and headers.
        /// </summary>
        public Dictionary<HttpStatusCode, Response> Responses { get; private set; } = new Dictionary<HttpStatusCode, Response>();

        /// <summary>
        /// Gets or sets the default response.
        /// </summary>
        public Response DefaultResponse { get; set; } = New<Response>();

        /// <summary>
        /// Gets or sets the method return type. The tuple contains a body
        /// and headers.
        /// </summary>
        public Response ReturnType { get; set; } = New<Response>();

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value.StripControlCharacters(); }
        }

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        public string Summary
        {
            get { return _summary; }
            set { _summary = value.StripControlCharacters(); }
        }

        /// <summary>
        /// Gets or sets a URL pointing to related external documentation.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings",
            Justification = "May not parse as a valid URI.")]
        public string ExternalDocsUrl { get; set; }

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        public string RequestContentType { get; set; }

        /// <summary>
        ///  The potential response content types.
        /// </summary>
        public string[] ResponseContentTypes { get; set;}

        /// <summary>
        /// Gets vendor extensions dictionary.
        /// </summary>
        public Dictionary<string, object> Extensions { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Indicates if the method is deprecated.
        /// </summary>
        public bool Deprecated { get; set; }
        
        /// <summary>
        /// Indicates if the method is supposed to be hidden from end-users.
        /// This is useful if a convenience layer is built around the method,
        /// rendering the method itself required but confusing or useless to end-users
        /// </summary>
        public bool Hidden { get; set; }

        [JsonIgnore]
        public IEnumerable<string> MyReservedNames => Name.Value.SingleItemAsEnumerable();

        [JsonIgnore]
        public virtual IEnumerable<IIdentifier> IdentifiersInScope => this.SingleItemConcat(Parent?.IdentifiersInScope);

        [JsonIgnore]
        public IEnumerable<IChild> Children => Parameters;


        [JsonIgnore]
        public virtual HashSet<string> LocallyUsedNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        public virtual bool IsXNullableReturnType => Responses.Keys.Any(key => Responses[key].IsNullable);


        [JsonProperty("implementation")]
        private Dictionary<string, string> Implementation { get; set; }

        public string GetImplementation(string language) =>
            Implementation == null ? null :
            Implementation.ContainsKey(language) ? Implementation[language] :
            Implementation.ContainsKey("") ? Implementation[""] : null;

        public Method ForwardTo { get; set; }

        public MethodFlavor Flavor =>
            this.Implementation != null ? MethodFlavor.Implementation :
            this.ForwardTo != null ? MethodFlavor.ForwardTo :
            MethodFlavor.RestCall;
    }
}
