﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoRest.Core.Utilities;
using AutoRest.Core.Utilities.Collections;
using Newtonsoft.Json;

namespace AutoRest.Core.Model
{
    public interface ILiteralType
    {
    }

    /// <summary>
    /// Defines model data type.
    /// </summary>
    [JsonObject(IsReference = true)]
    public partial class CompositeType : ModelType, ILiteralType
    {
        private string _summary;
        private string _documentation;
        private CompositeType _baseModelType;

        partial void InitializeCollections();
        /// <summary>
        /// Initializes a new instance of CompositeType class.
        /// </summary>
        protected CompositeType()
        {
            InitializeCollections();
            Name.OnGet += value => CodeNamer.Instance.GetTypeName(value);
        }

        protected CompositeType(string name) : this()
        {
            Name = name;
        }

        /// <Summary>
        /// The name on the wire for the ModelType.
        /// </Summary>
        public string SerializedName { get; set; }

        /// <summary>
        /// Gets the union of Parent and current type properties
        /// </summary>
        [JsonIgnore]
        public virtual IEnumerable<Property> ComposedProperties
        {
            get
            {
                if (BaseModelType != null)
                {
                    return BaseModelType.ComposedProperties.Union(Properties);
                }

                return this.Properties;
            }
        }

        /// <summary>
        /// Gets or sets the base model type.
        /// </summary>
        public virtual CompositeType BaseModelType
        {
            get { return _baseModelType; }
            set { _baseModelType = value; }
        }

        /// <summary>
        /// Gets or sets the discriminator property for polymorphic types.
        /// </summary>
        public virtual string PolymorphicDiscriminator { get; set; }

        /// <summary>
        /// Returns the name of the polymorphic discriminator if this or its parent is polymorphic
        /// </summary>
        [JsonIgnore]
        public virtual string BasePolymorphicDiscriminator
            => PolymorphicDiscriminator.Else(BaseModelType?.BasePolymorphicDiscriminator ?? null);

        /// <summary>
        /// Returns true if this type or its parent is polymorphic.
        /// </summary>
        [JsonIgnore]
        public bool BaseIsPolymorphic => IsPolymorphic || true == BaseModelType?.BaseIsPolymorphic;

        /// <summary>
        /// Returns true if this CompositeType has a polymorphic discrimiator set.
        /// note: this does not check any base types to see if they are set.
        /// </summary>
        [JsonIgnore]
        public bool IsPolymorphic => !string.IsNullOrEmpty(PolymorphicDiscriminator);

        /// <summary>
        /// Returns a property that is used for polymorphic discriminator. 
        /// (Not created for every language)
        /// </summary>
        [JsonIgnore]
        public Property PolymorphicDiscriminatorProperty
            => ComposedProperties.FirstOrDefault(each => each.IsPolymorphicDiscriminator);

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        public virtual string Summary
        {
            get { return _summary; }
            set { _summary = value.StripControlCharacters(); }
        }

        /// <summary>
        /// Gets or sets the CompositeType documentation.
        /// </summary>
        public string Documentation
        {
            get { return _documentation; }
            set { _documentation = value.StripControlCharacters(); }
        }

        /// <summary>
        /// Gets or sets a URL pointing to related external documentation.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings",
            Justification = "May not parse as a valid URI.")]
        public virtual string ExternalDocsUrl { get; set; }

        /// <summary>
        /// Returns true if any of the properties is a Constant or is 
        /// a CompositeType which ContainsConstantProperties set to true.
        /// </summary>
        public virtual bool ContainsConstantProperties { get; set; }

        /// <summary>
        /// Gets the union of Parent and current type properties
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> ComposedExtensions
        {
            get
            {
                if (BaseModelType != null)
                {
                    return Extensions.Concat(BaseModelType.ComposedExtensions
                        .Where(pair => !Extensions.Keys.Contains(pair.Key)))
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                }

                return this.Extensions;
            }
        }

        [JsonIgnore]
        public override string DefaultValue => IsConstant ? "{}" : null;


        /// <summary>
        /// Determines if the CompositeType is Constant (ie, the value is known at compile time)
        /// 
        /// Note: Added a check to ensure that it's not polymorphic. 
        /// If it's polymorphic, it can't possibly be known at compile time.
        /// </summary>
        [JsonIgnore]
        public override bool IsConstant => !BaseIsPolymorphic && ComposedProperties.Any() && ComposedProperties.All(p => p.IsConstant);
        
        [JsonIgnore]
        public override IEnumerable<IChild> Children => Properties;

        [JsonIgnore]
        public override string Qualifier => "Model";

        public class CompositeTypeComparer : IComparer<CompositeType>
        {
            /// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
            /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.
            /// Value Less than zero<paramref name="x" /> is less than <paramref name="y" />.
            /// Value Zero<paramref name="x" /> equals <paramref name="y" />.
            /// Value Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.</returns>
            /// <param name="x">The first CompositeType to compare.</param>
            /// <param name="y">The second CompositeType to compare.</param>
            public int Compare(CompositeType x, CompositeType y)
            {
                return !ReferenceEquals(x, y) ?
                    ReferenceEquals(x.BaseModelType, null) || ReferenceEquals(y.BaseModelType, x) ? -1 :
                        ReferenceEquals(y.BaseModelType, null) || ReferenceEquals(x.BaseModelType, y) ? 1 : 0 :
                           0;
            }
        }

        public static CompositeTypeComparer Comparer => new CompositeTypeComparer();

        [JsonProperty(PropertyName = "x-ms-metadata")]
        public XmsMetadata XMsMetadata { get; set; }
    }
}