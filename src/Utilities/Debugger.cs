// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// 

using System;

namespace AutoRest.Core.Utilities
{
    public static class Debugger
    {
        public static void Await()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Console.Error.WriteLine($"Waiting for debugger to attach to process {System.Diagnostics.Process.GetCurrentProcess().Id}");
            }
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Threading.Thread.Sleep(100);
                Console.Error.Write(".");
            }
            System.Diagnostics.Debugger.Break();
        }
    }
}