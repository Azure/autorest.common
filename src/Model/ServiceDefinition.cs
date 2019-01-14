// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoRest.Modeler.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoRest.Modeler.Model
{

    public class Deserializer<T> : SwaggerJsonConverter
    {
        private readonly Func<JsonReader, object, JsonSerializerSettings, T> _deserializer;
        public Deserializer(string json, Func<JsonReader, object, JsonSerializerSettings, T> deserializer)
        {
            Document = JObject.Parse(json);
            _deserializer = deserializer;
        }

        public override bool CanConvert(System.Type objectType) => objectType == typeof(T);

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue,
            JsonSerializer serializer) =>_deserializer(reader, existingValue, GetSettings(serializer));
        
    }

    


    public class Paths : Dictionary<string, Path>
    {
        public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
        public AutoRest.Core.Model.XmsMetadata XMsMetadata { get; set; }

        public static Paths Deserialize(JsonReader reader, object existingValue, JsonSerializerSettings serializerSettings)
        {
            var result = existingValue as Paths ?? new Paths();
            
            var item = JObject.Load(reader);
            if (null != item)
            {
                foreach (var path in item.Properties())
                {
                    if (path.Name.ToLowerInvariant() == "x-ms-metadata")
                    {
                        result.XMsMetadata = JsonConvert.DeserializeObject<AutoRest.Core.Model.XmsMetadata>(path.Value.ToString(), serializerSettings);
                        continue;
                    }

                    if (path.Name.StartsWith("x-"))
                    {
                        result.Extensions.Add(path.Name, path.Value);
                        continue;
                    }
                  
                    result.Add(path.Name, JsonConvert.DeserializeObject<Path>(path.Value.ToString(), serializerSettings));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Class that represents Swagger 2.0 schema
    /// http://json.schemastore.org/swagger-2.0
    /// Swagger Object - https://github.com/wordnik/swagger-spec/blob/master/versions/2.0.md#swagger-object- 
    /// </summary>
    public class ServiceDefinition : SwaggerBase
    {
        public ServiceDefinition()
        {
            Components = new Components();
            Paths = new Paths();
            CustomPaths = new Paths();
            Tags = new List<Tag>();
        }

        /// <summary>
        /// Specifies the OpenApi Specification version being used. 
        /// </summary>
        public string OpenApi { get; set; }

        /// <summary>
        /// Provides metadata about the API. The metadata can be used by the clients if needed.
        /// </summary>
        public Info Info { get; set; }

        public IList<Server> Servers { get; set; }

        /// <summary>
        /// Key is actual path and the value is serializationProperty of http operations and operation objects.
        /// </summary>
        public Paths Paths { get; set; }

        /// <summary>
        /// Key is actual path and the value is serializationProperty of http operations and operation objects.
        /// </summary>
        [JsonProperty("x-ms-paths")]
        public Paths CustomPaths { get; set; }

        public Components Components { get; set; }

        /// <summary>
        /// A list of tags used by the specification with additional metadata. The order 
        /// of the tags can be used to reflect on their order by the parsing tools. Not all 
        /// tags that are used by the Operation Object must be declared. The tags that are 
        /// not declared may be organized randomly or based on the tools' logic. Each 
        /// tag name in the list MUST be unique.
        /// </summary>
        public IList<Tag> Tags { get; set; }

        /// <summary>
        /// Additional external documentation
        /// </summary>
        public ExternalDoc ExternalDocs { get; set; }
    }
}