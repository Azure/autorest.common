﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoRest.Core.Utilities;
using Newtonsoft.Json;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Core.Model
{
    /// <summary>
    /// Defines a model type for enumerations.
    /// </summary>
    public class EnumType : ModelType
    {
        [JsonIgnore]
        protected virtual string ModelAsStringType => New<PrimaryType>(KnownPrimaryType.String).Name;

        [JsonIgnore]
        public override string Qualifier => "Enum";
        [JsonIgnore]
        public override IEnumerable<IChild> Children => Values;

        /// <summary>
        /// Creates a new instance of EnumType object.
        /// </summary>
        protected EnumType()
        {
            Values = new List<EnumValue>();
            Name.OnGet += s => string.IsNullOrEmpty(s) ? "enum" : CodeNamer.Instance.GetTypeName(s);
        }

        /// <summary>
        /// Gets or sets the enum values. 
        /// </summary>
        public List<EnumValue> Values { get; private set; }

        /// <summary>
        /// Whether the enum should be modeled as legacy modelAsString implementation
        /// </summary>
        public bool OldModelAsString;

        public void SetName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Indicates whether the set of enum values will be generated as string constants.
        /// </summary>
        public bool ModelAsString { get; set; }

        [JsonIgnore]
        public override string DeclarationName => ModelAsString ? ModelAsStringType : base.DeclarationName;

        /// <summary>
        /// Determines whether the specified model type is structurally equal to this object.
        /// </summary>
        /// <param name="other">The object to compare with this object.</param>
        /// <returns>true if the specified object is functionally equal to this object; otherwise, false.</returns>
        public override bool StructurallyEquals(IModelType other)
        {
            if (ReferenceEquals(other as EnumType, null))
            {
                return false;
            }

            return Name == other.Name &&
                Values.OrderBy(t => t).SequenceEqual((other as EnumType).Values.OrderBy(t => t), new Utilities.EqualityComparer<EnumValue>((a, b) => a.Name == b.Name)) &&
                ModelAsString == (other as EnumType).ModelAsString;
        }

        [JsonIgnore]
        public override string ExtendedDocumentation
            => $"Possible values include: {string.Join(", ", Values.Select(v => $"'{v.Name}'"))}";
        
        /// <summary>
        /// The underlying type of a property or a parameter on which the
        /// enum constraint or x-ms-enum extension is applied. This enables support
        /// for enums of other primary types like Integer, Decimal, Boolean apart 
        //  from string.
        /// </summary>
        public PrimaryType UnderlyingType { get; set; }
    }
}