// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Identity.Client;

namespace Microsoft.Authentication.Helper
{
    public class Application : ApplicationBase
    {
        public Application(ICommandContext context)
            : base(context) { }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            try
            {
                IDictionary<string, string> inputDict = await Context.StdIn.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

                string authority   = GetArgument(inputDict, "authority");
                string clientId    = GetArgument(inputDict, "clientId");
                string redirectUri = GetArgument(inputDict, "redirectUri");
                string resource    = GetArgument(inputDict, "resource");

                string accessToken = await GetAccessTokenAsync(authority, clientId, new Uri(redirectUri), resource);

                var resultDict = new Dictionary<string, string> {["accessToken"] = accessToken};

                Context.StdOut.WriteDictionary(resultDict);

                return 0;
            }
            catch (Exception e)
            {
                var resultDict = new Dictionary<string, string> {["error"] = e.ToString()};

                Context.StdOut.WriteDictionary(resultDict);

                return -1;
            }
        }

        private void OnMsalLogMessage(LogLevel level, string message, bool containspii)
        {
            Context.Trace.WriteLine($"[{level.ToString()}] {message}", memberName: "MSAL");
        }

        private static string GetArgument(IDictionary<string, string> inputDict, string name)
        {
            if (!inputDict.TryGetValue(name, out string value))
            {
                throw new ArgumentException($"missing '{name}' input");
            }

            return value;
        }

        protected virtual async Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource)
        {
            string[] scopes = { $"{resource}/.default" };

            var appBuilder = PublicClientApplicationBuilder.Create(clientId)
                                                           .WithAuthority(authority);

            // Listen to MSAL logs if GCM_TRACE_MSAUTH is set
            if (Context.IsEnvironmentVariableTruthy(Constants.EnvironmentVariables.GcmTraceMsAuth, false))
            {
                // If GCM secret tracing is enabled also enable "PII" logging in MSAL
                bool enablePiiLogging = Context.Trace.IsSecretTracingEnabled;

                appBuilder.WithLogging(OnMsalLogMessage, null, enablePiiLogging, false);
            }

            IPublicClientApplication app = appBuilder.Build();

            // Register the VS token cache
            var cache = new VisualStudioTokenCache(Context);
            cache.Register(app);

            AuthenticationResult result = await app.AcquireTokenInteractive(scopes)
                                                   .WithPrompt(Prompt.SelectAccount)
                                                   .ExecuteAsync();

            return result.AccessToken;
        }
    }
}
