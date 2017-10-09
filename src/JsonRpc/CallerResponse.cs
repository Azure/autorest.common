﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Perks.JsonRPC
{
    public interface ICallerResponse
    {
        bool SetCompleted(JToken result);
        bool SetException(JToken error);
        bool SetCancelled();
    }

    public class CallerResponse<T> : TaskCompletionSource<T>, ICallerResponse
    {
        public string Id { get; private set; }
        private Action<JObject> _setResult;

        public CallerResponse(string id, Action<JObject> setResult)
        {
            Id = id;
            _setResult = setResult;
        }
        public CallerResponse(string id)
        {
            Id = id;
        }

        public bool SetCompleted(JToken result)
        {
            T value;
            bool trueLikeValue = obj != null && !0.Equals(obj) && !false.Equals(obj) && !"".Equals(obj);
            if (typeof(T) == typeof(bool))
            {
                var obj = result.ToObject<object>();
                value = (T)(object)(trueLikeValue);
            }
            else if (typeof(T) == typeof(bool?))
            {
                var obj = result.ToObject<object>();
                value = obj == null ? (T)(object)(null) : (T)(object)(trueLikeValue);
            }
            else
            {
                value = result.ToObject<T>();
            }
            return TrySetResult(value);
        }

        public bool SetException(JToken error)
        {
            return TrySetException(error.ToObject<Exception>());
        }

        public bool SetCancelled()
        {
            return TrySetCanceled();
        }
    }
}