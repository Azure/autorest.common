﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace AutoRest.Core.Logging
{
    /// <summary>
    /// Represents a log entry in tracing output.
    /// </summary>
    public class LogMessage
    {
        public LogMessage(Category severity, string message, FileObjectPath path = null)
        {
            Severity = severity;
            Message = message;
            Path = path;

            if (true == Settings.Instance?.Verbose)
            {
                var stackTrace = Environment.StackTrace;

                // cut away logging part
                var lastMention = stackTrace.LastIndexOf(typeof(LogMessage).Namespace);
                stackTrace = stackTrace.Substring(lastMention);
                // skip to next stack frame
                stackTrace = stackTrace.Substring(stackTrace.IndexOf('\n') + 1);

                VerboseData = stackTrace;
            }
        }

        public Category Severity { get; }

        public string Message { get; }

        /// <summary>
        /// The JSON document path to the element being validated.
        /// </summary>
        public FileObjectPath Path { get; }

        /// <summary>
        /// Additional data, set only if `Settings.Instance.Verbose` is set.
        /// </summary>
        public string VerboseData { get; } = null;
    }
}