// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public abstract class AuthenticationBase
    {
        protected readonly ICommandContext Context;

        protected AuthenticationBase(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
        }

        protected void EnsureTerminalPromptsEnabled()
        {
            if (Context.TryGetEnvironmentVariable(Constants.EnvironmentVariables.GitTerminalPrompts, out string envarPrompts)
                && envarPrompts == "0")
            {
                Context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot show credential prompt because terminal prompts have been disabled.");
            }
        }
    }
}
