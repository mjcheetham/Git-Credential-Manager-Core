// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public static class ProcessHelper
    {
        public static bool TryFindHelperExecutable(ICommandContext context, string helperName, out string path)
        {
            if (PlatformUtils.IsWindows())
            {
                helperName += ".exe";
            }

            string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            context.Trace.WriteLine($"Attempting to locate helper '{helperName}' in '{executableDirectory}'");

            path = Path.Combine(executableDirectory, helperName);
            if (!context.FileSystem.FileExists(path))
            {
                context.Trace.WriteLine($"Did not find helper '{helperName}' in '{executableDirectory}'");
                return false;
            }

            context.Trace.WriteLine($"Found helper '{helperName}' at '{path}'");
            return true;
        }

        public static async Task<IDictionary<string, string>> InvokeHelperAsync(string path, string args, IDictionary<string, string> standardInput)
        {
            var procStartInfo = new ProcessStartInfo(path)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

            var process = Process.Start(procStartInfo);
            if (process is null)
            {
                throw new Exception($"Failed to start helper process '{path}'");
            }

            if (!(standardInput is null))
            {
                await process.StandardInput.WriteDictionaryAsync(standardInput);
            }

            IDictionary<string, string> resultDict = await process.StandardOutput.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            await Task.Run(() => process.WaitForExit());
            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                if (!resultDict.TryGetValue("error", out string errorMessage))
                {
                    errorMessage = "Unknown";
                }

                throw new Exception($"helper error ({exitCode}): {errorMessage}");
            }

            return resultDict;
        }
    }
}
