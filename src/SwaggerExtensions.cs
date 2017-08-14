﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using AutoRest.Extensions.Properties;
using AutoRest.Swagger;
using AutoRest.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParameterLocation = AutoRest.Core.Model.ParameterLocation;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Extensions
{
    /// <summary>
    /// Base code generator for Azure.
    /// Normalizes the ServiceClient according to Azure conventions and Swagger extensions.
    /// </summary>
    public abstract class SwaggerExtensions
    {
        public const string SkipUrlEncodingExtension = "x-ms-skip-url-encoding";
        public const string NameOverrideExtension = "x-ms-client-name";
        public const string FlattenExtension = "x-ms-client-flatten";
        public const string FlattenOriginalTypeName = "x-ms-client-flatten-original-type-name";
        public const string ParameterGroupExtension = "x-ms-parameter-grouping";
        public const string ParameterizedHostExtension = "x-ms-parameterized-host";
        public const string UseSchemePrefix = "useSchemePrefix";
        public const string PositionInOperation = "positionInOperation";
        public const string ParameterLocationExtension = "x-ms-parameter-location";
        public const string ExternalExtension = "x-ms-external";
        public const string HeaderCollectionPrefix = "x-ms-header-collection-prefix";

        /// <summary>
        /// Normalizes client model using generic extensions.
        /// </summary>
        /// <param name="codeModelient">Service client</param>
        /// <param name="settings">AutoRest settings</param>
        /// <returns></returns>
        public static void NormalizeClientModel(CodeModel codeModel)
        {
            ProcessGlobalParameters(codeModel);
            FlattenModels(codeModel);
            FlattenMethodParameters(codeModel);
            ParameterGroupExtensionHelper.AddParameterGroups(codeModel);
            ProcessParameterizedHost(codeModel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "We are normalizing a URI, which is lowercase by convention")]
        public static void ProcessParameterizedHost(CodeModel codeModel)
        {
            using (NewContext)
            {
                if (codeModel == null)
                {
                    throw new ArgumentNullException("codeModel");
                }

                if (codeModel.Extensions.TryGetValue(ParameterizedHostExtension, out var extensionObject) &&
                    !codeModel.Extensions.ContainsKey(ParameterizedHostExtension + "DONE"))
                {
                    var hostExtension = extensionObject as JObject;
                    codeModel.Extensions.Add(ParameterizedHostExtension + "DONE", true);

                    if (hostExtension != null)
                    {
                        var hostTemplate = (string) hostExtension["hostTemplate"];
                        var parametersJson = hostExtension["parameters"].ToString();
                        var useSchemePrefix = true;
                        if (hostExtension[UseSchemePrefix] != null)
                        {
                            useSchemePrefix = bool.Parse(hostExtension[UseSchemePrefix].ToString());
                        }

                        var position = "first";

                        if (hostExtension[PositionInOperation] != null)
                        {
                            var pat = "^(fir|la)st$";
                            Regex r = new Regex(pat, RegexOptions.IgnoreCase);
                            var text = hostExtension[PositionInOperation].ToString();
                            Match m = r.Match(text);
                            if (!m.Success)
                            {
                                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                    Resources.InvalidExtensionProperty, text, PositionInOperation,
                                    ParameterizedHostExtension, "first, last"));
                            }
                            position = text;
                        }

                        if (!string.IsNullOrEmpty(parametersJson))
                        {
                            var serviceDefinition = Singleton<ServiceDefinition>.Instance;
                            var jsonSettings = new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.None,
                                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                            };

                            var swaggerParams = JsonConvert.DeserializeObject<List<SwaggerParameter>>(parametersJson,
                                jsonSettings);
                            List<Parameter> hostParamList = new List<Parameter>();
                            foreach (var swaggerParameter in swaggerParams)
                            {
                                // Build parameter
                                var modeler = new SwaggerModeler(Settings.Instance);
                                modeler.ServiceDefinition = serviceDefinition;
                                modeler.CodeModel = codeModel;
                                var parameterBuilder = new ParameterBuilder(swaggerParameter, modeler);
                                var parameter = parameterBuilder.Build();

                                // check to see if the parameter exists in properties, and needs to have its name normalized
                                if (codeModel.Properties.Any(p => p.SerializedName.EqualsIgnoreCase(parameter.SerializedName)))
                                {
                                    parameter.ClientProperty =
                                        codeModel.Properties.Single(
                                            p => p.SerializedName.Equals(parameter.SerializedName));
                                }
                                parameter.Extensions["hostParameter"] = true;
                                hostParamList.Add(parameter);
                            }

                            foreach (var method in codeModel.Methods)
                            {
                                if (position.EqualsIgnoreCase("first"))
                                {
                                    method.InsertRange(((IEnumerable<Parameter>)hostParamList).Reverse());
                                }
                                else
                                {
                                    method.AddRange(hostParamList);
                                }

                            }
                            if (useSchemePrefix)
                            {
                                codeModel.BaseUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}",
                                    serviceDefinition.Schemes[0].ToString().ToLowerInvariant(),
                                    hostTemplate, serviceDefinition.BasePath);
                            }
                            else
                            {
                                codeModel.BaseUrl = string.Format(CultureInfo.InvariantCulture, "{0}{1}",
                                    hostTemplate, serviceDefinition.BasePath);
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Flattens the Resource Properties.
        /// </summary>
        /// <param name="codeModelient"></param>
        public static void FlattenModels(CodeModel codeModel)
        {
            if (codeModel == null)
            {
                throw new ArgumentNullException("codeModel");
            }

            // About "flattenDepth": flattening was not really deterministic and depended on order of specification
            // Sorting by the following method enforces the right behavior
            Func<IModelType, int, int> flattenDepth = null;
            flattenDepth = (type, depth) =>
            {
                var ct = type as CompositeType;
                if (ReferenceEquals(ct, null) || !ct.Properties.Any(p => p.ShouldBeFlattened()) || depth > 16)
                    return 0;
                return 1 + ct.Properties.Max(prop => flattenDepth(prop.ModelType, depth + 1));
            };

            HashSet<string> typesToDelete = new HashSet<string>();
            foreach (var compositeType in codeModel.ModelTypes.OrderByDescending(type => flattenDepth(type, 0)))
            {
                if (compositeType.Properties.Any(p => p.ShouldBeFlattened())
                    && !typesToDelete.Contains(compositeType.Name))
                {
                    List<Property> oldProperties = compositeType.Properties.ToList();
                    compositeType.ClearProperties();

                    foreach (Property innerProperty in oldProperties)
                    {
                        if (innerProperty.ShouldBeFlattened() && compositeType != innerProperty.ModelType)
                        {
                            FlattenProperty(innerProperty, typesToDelete)
                                .ForEach(p => compositeType.Add(p));
                        }
                        else
                        {
                            compositeType.Add(innerProperty);
                        }
                    }

                    RemoveFlatteningConflicts(compositeType);
                }
            }

            RemoveUnreferencedTypes(codeModel, typesToDelete);
        }

        /// <summary>
        /// Ensures that global parameters that are tagged with x-ms-paramater-location: "method" are not client properties
        /// </summary>
        /// <param name="codeModelient"></param>
        public static void ProcessGlobalParameters(CodeModel codeModel)
        {
            if (codeModel == null)
            {
                throw new ArgumentNullException("codeModel");
            }

            List<Property> propertiesToProcess = new List<Property>();
            foreach(var property in codeModel.Properties)
            {
                if (property.Extensions.ContainsKey(ParameterLocationExtension) && property.Extensions[ParameterLocationExtension].ToString().EqualsIgnoreCase("method"))
                {
                    propertiesToProcess.Add(property);
                }
            }
            //set the clientProperty to null for such parameters in the method.
            foreach(var prop in propertiesToProcess)
            {
                codeModel.Remove(prop);
                foreach(var method in codeModel.Operations.SelectMany(each => each.Methods))
                {
                    foreach(var parameter in method.Parameters)
                    {
                        if (parameter.Name.FixedValue == prop.Name.FixedValue && parameter.IsClientProperty)
                        {
                            parameter.ClientProperty = null;
                        } 
                    }
                }
            }
        }

        private static void RemoveFlatteningConflicts(CompositeType compositeType)
        {
            if (compositeType == null)
            {
                throw new ArgumentNullException("compositeType");
            }

            foreach (Property innerProperty in compositeType.Properties)
            {
                // Check conflict among peers

                var conflictingPeers = compositeType.Properties
                    .Where(p => p.Name == innerProperty.Name && p.SerializedName != innerProperty.SerializedName);

                if (conflictingPeers.Any())
                {
                    foreach (var cp in conflictingPeers.Concat(new[] { innerProperty }))
                    {
                        if (cp.Extensions.ContainsKey(FlattenOriginalTypeName))
                        {
                            cp.Name = cp.Extensions[FlattenOriginalTypeName].ToString() + "_" + cp.Name;
                        }
                    }
                }

                if (compositeType.BaseModelType != null)
                {
                    var conflictingParentProperties = compositeType.BaseModelType.ComposedProperties
                        .Where(p => p.Name == innerProperty.Name && p.SerializedName != innerProperty.SerializedName);

                    if (conflictingParentProperties.Any())
                    {
                        innerProperty.Name = compositeType.Name + "_" + innerProperty.Name;
                    }
                }
            }
        }

        private static IEnumerable<Property> FlattenProperty(Property propertyToFlatten, HashSet<string> typesToDelete)
        {
            if (propertyToFlatten == null)
            {
                throw new ArgumentNullException("propertyToFlatten");
            }
            if (typesToDelete == null)
            {
                throw new ArgumentNullException("typesToDelete");
            }

            CompositeType typeToFlatten = propertyToFlatten.ModelType as CompositeType;
            if (typeToFlatten == null)
            {
                return new[] { propertyToFlatten };
            }

            List<Property> extractedProperties = new List<Property>();
            foreach (Property innerProperty in typeToFlatten.ComposedProperties)
            {
                Debug.Assert(typeToFlatten.SerializedName != null);
                Debug.Assert(innerProperty.SerializedName != null);

                if (innerProperty.ShouldBeFlattened() && typeToFlatten != innerProperty.ModelType)
                {
                    extractedProperties.AddRange(FlattenProperty(innerProperty, typesToDelete)
                        .Select(fp => UpdateSerializedNameWithPathHierarchy(fp, propertyToFlatten.SerializedName, false)));
                }
                else
                {
                    Property clonedProperty = Duplicate(innerProperty);
                    if (!clonedProperty.Extensions.ContainsKey(FlattenOriginalTypeName))
                    {
                        clonedProperty.Extensions[FlattenOriginalTypeName] = typeToFlatten.Name;
                        UpdateSerializedNameWithPathHierarchy(clonedProperty, propertyToFlatten.SerializedName, true);
                    }
                    extractedProperties.Add(clonedProperty);
                }
            }

            typesToDelete.Add(typeToFlatten.Name);

            return extractedProperties;
        }

        private static Property UpdateSerializedNameWithPathHierarchy(Property property, string basePath, bool escapePropertyName)
        {
            if (property == null)
            {
                throw new ArgumentNullException("property");
            }
            if (basePath == null)
            {
                basePath = "";
            }
       
            string propertyName = property.SerializedName;
            if (escapePropertyName)
            {
                propertyName = propertyName.Replace(".", "\\\\.")?.Replace("\\\\\\\\", "\\\\");
            }
            property.SerializedName = basePath + "." + propertyName;
            return property;
        }

        /// <summary>
        /// Cleans all model types that are not used
        /// </summary>
        /// <param name="codeModelient"></param>
        /// <param name="typeNames"></param>
        public static void RemoveUnreferencedTypes(CodeModel codeModel, HashSet<string> typeNames)
        {
            if (codeModel == null)
            {
                throw new ArgumentNullException("codeModel");
            }

            if (typeNames == null)
            {
                throw new ArgumentNullException("typeNames");
            }

            while (typeNames.Count > 0)
            {
                string typeName = typeNames.First();
                typeNames.Remove(typeName);

                var typeToDelete = codeModel.ModelTypes.First(t => t.Name == typeName);

                var isUsedInErrorTypes = codeModel.ErrorTypes.Any(e => e.Name == typeName);
                var isUsedInResponses = codeModel.Methods.Any(m => m.Responses.Any(r => typeReferenced(typeToDelete, r.Value.Body)));
                var isUsedInParameters = codeModel.Methods.Any(m => m.Parameters.Any(p => typeReferenced(typeToDelete, p.ModelType)));
                var isBaseType = codeModel.ModelTypes.Any(t => t.BaseModelType == typeToDelete);
                var isUsedInProperties = codeModel.ModelTypes.Where(t => !typeNames.Contains(t.Name))
                                                                 .Any(t => t.Properties.Any(p => typeReferenced(typeToDelete, p.ModelType)));
                if (!isUsedInErrorTypes &&
                    !isUsedInResponses &&
                    !isUsedInParameters &&
                    !isBaseType &&
                    !isUsedInProperties)
                {
                    codeModel.Remove(typeToDelete);
                }
            }
        }

        private static bool typeReferenced(CompositeType candidate, IModelType tester)
        {
            if (candidate == tester)
            {
                return true;
            }

            SequenceType sequenceTester = tester as SequenceType;
            if (sequenceTester != null)
            {
                return typeReferenced(candidate, sequenceTester.ElementType);
            }
            DictionaryType dictionaryTester = tester as DictionaryType;
            if (dictionaryTester != null)
            {
                return typeReferenced(candidate, dictionaryTester.ValueType);
            }

            return false;
        }

        /// <summary>
        /// Flattens the request payload if the number of properties of the 
        /// payload is less than or equal to the PayloadFlatteningThreshold.
        /// </summary>
        /// <param name="codeModelient">Service client</param>                            
        /// <param name="settings">AutoRest settings</param>                            
        public static void FlattenMethodParameters(CodeModel codeModel)
        {
            if (codeModel == null)
            {
                throw new ArgumentNullException("codeModel");    
            }
          

            foreach (var method in codeModel.Methods)
            {
                var bodyParameter = method.Parameters.FirstOrDefault(
                    p => p.Location == ParameterLocation.Body);

                if (bodyParameter != null)
                {
                    var bodyParameterType = bodyParameter.ModelType as CompositeType;
                    if (bodyParameterType != null && 
                        !bodyParameterType.BaseIsPolymorphic &&
                        (bodyParameterType.ComposedProperties.Count(p => !p.IsConstant && !p.IsReadOnly) <= Settings.Instance.PayloadFlatteningThreshold ||
                         bodyParameter.ShouldBeFlattened()))
                    {
                        var parameterTransformation = new ParameterTransformation
                        {
                            OutputParameter = bodyParameter
                        };
                        method.InputParameterTransformation.Add(parameterTransformation);
                        method.Remove(bodyParameter);

                        foreach (var property in bodyParameterType.ComposedProperties.Where(p => !p.IsConstant && p.Name != null && !p.IsReadOnly))
                        {
                            var newMethodParameter = New<Parameter>();
                            newMethodParameter.LoadFrom(property);

                            var documentationString = !string.IsNullOrEmpty(property.Summary) ? property.Summary + " " : string.Empty;
                            documentationString += property.Documentation;
                            newMethodParameter.Documentation = documentationString;

                            bodyParameter.Extensions.ForEach(kv => { newMethodParameter.Extensions[kv.Key] = kv.Value; });
                            method.Add(newMethodParameter);

                            parameterTransformation.ParameterMappings.Add(new ParameterMapping
                            {
                                InputParameter = newMethodParameter,
                                OutputParameterProperty = property.GetClientName()
                            });
                        }
                    }
                }
            }
        }
    }
}