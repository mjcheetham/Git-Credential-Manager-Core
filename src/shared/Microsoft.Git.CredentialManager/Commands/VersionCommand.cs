// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Print version information for Git Credential Manager.
    /// </summary>
    public class VersionCommand : CommandBase
    {
        public override bool CanExecute(string[] args)
        {
            return args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "--version"))
                || args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "version"));
        }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            IPlatformInformation pi = context.PlatformInformation;

            var sb = new StringBuilder();
            sb.AppendLine($"Git Credential Manager version {pi.ApplicationVersion}");
            sb.AppendLine();
            sb.AppendLine($"Version          : {pi.ApplicationVersion}");
            sb.AppendLine($"Commit ID        : {pi.ApplicationCommit}");
            sb.AppendLine($"CPU Architecture : {pi.CpuArchitecture}");
            sb.AppendLine($"Operating System : {pi.OperatingSystemName} {pi.OperatingSystemVersion}");
            sb.AppendLine($"Runtime          : {pi.RuntimeName} {pi.RuntimeVersion}");

            context.Streams.Out.WriteLine(sb.ToString());

            return Task.CompletedTask;
        }
    }
}
