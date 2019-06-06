// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, IEnumerable<string> scopes);
    }

    public class MicrosoftAuthentication : AuthenticationBase, IMicrosoftAuthentication
    {
        public MicrosoftAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, IEnumerable<string> scopes)
        {
            IPublicClientApplication app = CreateApp(authority, clientId, redirectUri);

            AcquireTokenInteractiveParameterBuilder request = app.AcquireTokenInteractive(scopes)
                                                                 .WithPrompt(Prompt.SelectAccount);

            // On macOS we provide our own custom web UI (which we proxy to native helper applications)
            if (PlatformUtils.IsMacOS())
            {
                ICustomWebUi nativeWebUi = new MsalNativeCustomWebUiAdaptor(Context.NativeUi);
                request.WithCustomWebUi(nativeWebUi);
            }

            AuthenticationResult result = await request.ExecuteAsync();

            return result.AccessToken;
        }

        private IPublicClientApplication CreateApp(string authority, string clientId, Uri redirectUri)
        {
            var builder = PublicClientApplicationBuilder.Create(clientId)
                                                        .WithAuthority(authority)
                                                        .WithRedirectUri(redirectUri.ToString());

            // Listen to MSAL logs if GCM_TRACE_MSAUTH is set
            if (Context.IsEnvironmentVariableTruthy(Constants.EnvironmentVariables.GcmTraceMsAuth, false))
            {
                // If GCM secret tracing is enabled also enable "PII" logging in MSAL
                bool enablePiiLogging = Context.Trace.IsSecretTracingEnabled;

                void OnMessage(LogLevel level, string message, bool containsPii)
                {
                    Context.Trace.WriteLine($"[{level.ToString()}] {message}", memberName: "MSAL");
                }

                builder.WithLogging(OnMessage, null, enablePiiLogging, false);
            }

            IPublicClientApplication app = builder.Build();

            // Register the VS token cache (if available)
            if (PlatformUtils.IsWindows())
            {
                var tokenCache = new VisualStudioTokenCache(Context);
                tokenCache.Register(app);
            }

            return app;
        }
    }

    public class MsalNativeCustomWebUiAdaptor : ICustomWebUi
    {
        private readonly INativeUi _nativeUi;

        public MsalNativeCustomWebUiAdaptor(INativeUi nativeUi)
        {
            EnsureArgument.NotNull(nativeUi, nameof(nativeUi));

            _nativeUi = nativeUi;
        }

        public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            var options = new WebViewOptions
            {
                WindowTitle   = "Git Credential Manager",
                StartLocation = authorizationUri.ToString(),
                EndLocation   = redirectUri.ToString(),
            };

            WebViewResult result = await _nativeUi.ShowWebViewAsync(options, cancellationToken);

            if (result.UserDismissedDialog)
            {
                throw new Exception("User dismissed the authentication web view");
            }

            if (Uri.TryCreate(result.FinalLocation, UriKind.RelativeOrAbsolute, out Uri finalUri))
            {
                return finalUri;
            }

            throw new Exception($"Unknown final URL '{result.FinalLocation ?? "(null)"}'");
        }
    }
}
