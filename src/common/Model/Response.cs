﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using AutoRest.Core.Utilities;

namespace AutoRest.Core.Model
{
    /// <summary>
    /// Defines a structure for operation response.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Initializes a new instance of Response.
        /// </summary>
        /// <param name="body">Body type.</param>
        /// <param name="headers">Headers type.</param>
        public Response(IModelType body, IModelType headers) 
        {
            Body = body;
            Headers = headers;
        }

        public Response()
        {
            
        }
        /// <summary>
        /// Gets or sets the body type.
        /// </summary>
        public IModelType Body{ get; set; }

        /// <summary>
        /// Gets or sets the headers type.
        /// </summary>
        public IModelType Headers { get; set; }

        public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();

        public bool IsNullable => Extensions?.Get<bool>("x-nullable") ?? true;
    }
}
