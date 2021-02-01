// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

#if NETFRAMEWORK
using Microsoft.Identity.Client.Desktop;
#endif

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<IMicrosoftAuthenticationResult> GetTokenAsync(string authority, string clientId, Uri redirectUri, string resource,
            Uri remoteUri, string userName);
    }

    public interface IMicrosoftAuthenticationResult
    {
        string AccessToken { get; }
        string AccountUpn { get; }
        string TokenSource { get; }
    }

    public enum MicrosoftAuthenticationFlowType
    {
        Auto = 0,
        EmbeddedWebView = 1,
        SystemWebView = 2,
        DeviceCode = 3,
        Broker = 4,
    }

    public class MicrosoftAuthentication : AuthenticationBase, IMicrosoftAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "msa",  "microsoft",   "microsoftaccount",
            "aad",  "azure",       "azuredirectory",
            "live", "liveconnect", "liveid",
        };

        public MicrosoftAuthentication(ICommandContext context)
            : base(context) {}

        #region IMicrosoftAuthentication

        public async Task<IMicrosoftAuthenticationResult> GetTokenAsync(
            string authority, string clientId, Uri redirectUri, string resource, Uri remoteUri, string userName)
        {
            // Try to acquire an access token in the current process
            string[] scopes = { $"{resource}/.default" };
            return await GetTokenInProcAsync(authority, clientId, redirectUri, scopes, userName);
        }

        #endregion

        #region Authentication strategies

        /// <summary>
        /// Obtain an access token using MSAL running inside the current process.
        /// </summary>
        private async Task<IMicrosoftAuthenticationResult> GetTokenInProcAsync(string authority, string clientId, Uri redirectUri, string[] scopes, string userName)
        {
            // Check for a user flow preference and enable broker support if they haven't forced a non-broker flow
            MicrosoftAuthenticationFlowType flowType = GetFlowType();
            bool enableBroker = flowType == MicrosoftAuthenticationFlowType.Auto || flowType == MicrosoftAuthenticationFlowType.Broker;

            // Create the public client application for authentication
            IPublicClientApplication app = await CreatePublicClientApplicationAsync(authority, clientId, redirectUri, enableBroker);

            AuthenticationResult result = null;

            // Try silent authentication first if we know about an existing user
            if (!string.IsNullOrWhiteSpace(userName))
            {
                result = await GetAccessTokenSilentlyAsync(app, scopes, userName);
            }

            //
            // If we failed to acquire an AT silently (either because we don't have an existing user, or the user's RT has expired)
            // we need to prompt the user for credentials.
            //
            // If the user has expressed a preference in how the want to perform the interactive authentication flows then we respect that.
            // Otherwise, depending on the current platform and session type we try to show the most appropriate authentication interface:
            //
            // On Windows 10 & .NET Framework, MSAL supports the Web Account Manager (WAM) broker - we try to use that if possible
            // in the first instance.
            //
            // On .NET Framework MSAL supports the WinForms based 'embedded' webview UI. For Windows + .NET Framework this is the
            // best and natural experience.
            //
            // On other runtimes (e.g., .NET Core) MSAL only supports the system webview flow (launch the user's browser),
            // and the device-code flows.
            //
            //     Note: .NET Core 3 allows using WinForms when run on Windows but MSAL does not yet support this.
            //
            // The system webview flow requires that the redirect URI is a loopback address, and that we are in an interactive session.
            //
            // The device code flow has no limitations other than a way to communicate to the user the code required to authenticate.
            //
            if (result is null)
            {
                // If the user has disabled interaction all we can do is fail at this point
                ThrowIfUserInteractionDisabled();

                switch (flowType)
                {
                    case MicrosoftAuthenticationFlowType.Auto:
                        if (CanUseBroker(app, redirectUri))
                            goto case MicrosoftAuthenticationFlowType.Broker;

                        if (CanUseEmbeddedWebView())
                            goto case MicrosoftAuthenticationFlowType.EmbeddedWebView;

                        if (CanUseSystemWebView(app, redirectUri))
                            goto case MicrosoftAuthenticationFlowType.SystemWebView;

                        // Fall back to device code flow
                        goto case MicrosoftAuthenticationFlowType.DeviceCode;

                    case MicrosoftAuthenticationFlowType.Broker:
                        Context.Trace.WriteLine("Performing interactive auth with OS broker...");
                        EnsureCanUseBroker(app, redirectUri);
                        result = await app.AcquireTokenInteractive(scopes)
                            // We must configure the system webview as a fallback
                            .WithPrompt(Prompt.SelectAccount)
                            .WithSystemWebViewOptions(GetSystemWebViewOptions())
                            .ExecuteAsync();
                        break;

                    case MicrosoftAuthenticationFlowType.EmbeddedWebView:
                        Context.Trace.WriteLine("Performing interactive auth with embedded web view...");
                        EnsureCanUseEmbeddedWebView();
                        result = await app.AcquireTokenInteractive(scopes)
                            .WithPrompt(Prompt.SelectAccount)
                            .WithUseEmbeddedWebView(true)
                            .ExecuteAsync();
                        break;

                    case MicrosoftAuthenticationFlowType.SystemWebView:
                        Context.Trace.WriteLine("Performing interactive auth with system web view...");
                        EnsureCanUseSystemWebView(app, redirectUri);
                        result = await app.AcquireTokenInteractive(scopes)
                            .WithPrompt(Prompt.SelectAccount)
                            .WithSystemWebViewOptions(GetSystemWebViewOptions())
                            .ExecuteAsync();
                        break;

                    case MicrosoftAuthenticationFlowType.DeviceCode:
                        Context.Trace.WriteLine("Performing interactive auth with device code...");
                        // We don't have a way to display a device code without a terminal at the moment
                        // TODO: introduce a small GUI window to show a code if no TTY exists
                        ThrowIfTerminalPromptsDisabled();
                        result = await app.AcquireTokenWithDeviceCode(scopes, ShowDeviceCodeInTty).ExecuteAsync();
                        break;

                    default:
                        goto case MicrosoftAuthenticationFlowType.Auto;
                }
            }

            return new MsalResult(result);
        }

        private MicrosoftAuthenticationFlowType GetFlowType()
        {
            if (Context.Settings.TryGetSetting(
                Constants.EnvironmentVariables.MsAuthFlow,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.MsAuthFlow,
                out string valueStr))
            {
                Context.Trace.WriteLine($"Microsoft auth flow overriden to '{valueStr}'.");
                switch (valueStr.ToLowerInvariant())
                {
                    case "auto":
                        return MicrosoftAuthenticationFlowType.Auto;
                    case "broker":
                        return MicrosoftAuthenticationFlowType.Broker;
                    case "embedded":
                        return MicrosoftAuthenticationFlowType.EmbeddedWebView;
                    case "system":
                        return MicrosoftAuthenticationFlowType.SystemWebView;
                    default:
                        if (Enum.TryParse(valueStr, ignoreCase: true, out MicrosoftAuthenticationFlowType value))
                            return value;
                        break;
                }

                Context.Streams.Error.WriteLine($"warning: unknown Microsoft Authentication flow type '{valueStr}'; using 'auto'");
            }

            return MicrosoftAuthenticationFlowType.Auto;
        }

        /// <summary>
        /// Obtain an access token without showing UI or prompts.
        /// </summary>
        private async Task<AuthenticationResult> GetAccessTokenSilentlyAsync(IPublicClientApplication app, string[] scopes, string userName)
        {
            try
            {
                Context.Trace.WriteLine($"Attempting to acquire token silently for user '{userName}'...");

                // We can either call `app.GetAccountsAsync` and filter through the IAccount objects for the instance with the correct user name,
                // or we can just pass the user name string we have as the `loginHint` and let MSAL do exactly that for us instead!
                return await app.AcquireTokenSilent(scopes, loginHint: userName).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                Context.Trace.WriteLine("Failed to acquire token silently; user interaction is required.");
                return null;
            }
        }

        private async Task<IPublicClientApplication> CreatePublicClientApplicationAsync(
            string authority, string clientId, Uri redirectUri, bool enableBroker)
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor(Context.HttpClientFactory);

            var appBuilder = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .WithRedirectUri(redirectUri.ToString())
                .WithHttpClientFactory(httpFactoryAdaptor);

            // Listen to MSAL logs if GCM_TRACE_MSAUTH is set
            if (Context.Settings.IsMsalTracingEnabled)
            {
                // If GCM secret tracing is enabled also enable "PII" logging in MSAL
                bool enablePiiLogging = Context.Trace.IsSecretTracingEnabled;

                appBuilder.WithLogging(OnMsalLogMessage, LogLevel.Verbose, enablePiiLogging, false);
            }

#if NETFRAMEWORK
            // On Windows & .NET Framework try and use the WAM broker
            if (enableBroker && PlatformUtils.IsWindows())
            {
                appBuilder.WithExperimentalFeatures();
                appBuilder.WithWindowsBroker();
            }
#endif

            IPublicClientApplication app = appBuilder.Build();

            // Register the application token cache
            await RegisterTokenCacheAsync(app);

            return app;
        }

        #endregion

        #region Helpers

        private async Task RegisterTokenCacheAsync(IPublicClientApplication app)
        {
            Context.Trace.WriteLine("Configuring Microsoft Authentication token cache...");

            const string cacheFileName = "msal.cache";

            if (PlatformUtils.IsWindows())
            {
                Context.Trace.WriteLine("Using token cache shared with Microsoft developer tools on Windows...");

                // The shared MSAL cache is located at "%LocalAppData%\.IdentityService\msal.cache" on Windows.
                // We use the MSAL extension library to provide us consistent cache file access semantics (synchronisation, etc)
                // as Visual Studio itself follows, as well as other Microsoft developer tools such as the Azure PowerShell CLI.
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string cacheDirectory = Path.Combine(appData, ".IdentityService");

                var storageProps = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory, app.AppConfig.ClientId).Build();

                var helper = await MsalCacheHelper.CreateAsync(storageProps);
                helper.RegisterCache(app.UserTokenCache);

                Context.Trace.WriteLine("Microsoft developer tools token cache configured.");
            }
            else if (PlatformUtils.IsMacOS())
            {
                Context.Trace.WriteLine("Using token cache shared with Microsoft developer tools on macOS...");

                // The shared MSAL cache is located at "~/.local/.IdentityService/msal.cache" on UNIX.
                string cacheDirectory = Path.Combine(Context.FileSystem.UserHomePath, ".local");

                var storageProps = new StorageCreationPropertiesBuilder(
                        cacheFileName, cacheDirectory, app.AppConfig.ClientId)
                    .WithMacKeyChain("Microsoft.Developer.IdentityService", "MSALCache")
                    .Build();

                var helper = await MsalCacheHelper.CreateAsync(storageProps);
                helper.RegisterCache(app.UserTokenCache);

                Context.Trace.WriteLine("Microsoft developer tools token cache configured.");
            }
            else
            {
                string osType = PlatformUtils.GetPlatformInformation().OperatingSystemType;
                Context.Trace.WriteLine($"Token cache integration is not supported on {osType}.");
            }
        }

        private static SystemWebViewOptions GetSystemWebViewOptions()
        {
            // TODO: add nicer HTML success and error pages
            return new SystemWebViewOptions();
        }

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            Context.Terminal.WriteLine(dcr.Message);

            return Task.CompletedTask;
        }

        private void OnMsalLogMessage(LogLevel level, string message, bool containspii)
        {
            Context.Trace.WriteLine($"[{level.ToString()}] {message}", memberName: "MSAL");
        }

        private class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
        {
            private readonly IHttpClientFactory _factory;
            private HttpClient _instance;

            public MsalHttpClientFactoryAdaptor(IHttpClientFactory factory)
            {
                EnsureArgument.NotNull(factory, nameof(factory));

                _factory = factory;
            }

            public HttpClient GetHttpClient()
            {
                // MSAL calls this method each time it wants to use an HTTP client.
                // We ensure we only create a single instance to avoid socket exhaustion.
                return _instance ?? (_instance = _factory.CreateClient());
            }
        }

        #endregion

        #region Auth flow capability detection

        private bool CanUseBroker(IPublicClientApplication app, Uri redirectUri)
        {
            // We only support the broker on Windows 10 and .NET Framework
#if NETFRAMEWORK
            return Context.SessionManager.IsDesktopSession && PlatformUtils.IsWindows10() && CanUseSystemWebView(app, redirectUri);
#else
            return false;
#endif
        }

        private void EnsureCanUseBroker(IPublicClientApplication app, Uri redirectUri)
        {
            // We only support the broker on Windows 10 and .NET Framework
#if NETFRAMEWORK
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("Broker authentication is not available without a desktop session.");
            }

            if (!PlatformUtils.IsWindows10())
            {
                throw new InvalidOperationException("Broker authentication is only available on Windows 10.");
            }

            EnsureCanUseSystemWebView(app, redirectUri);
#else
            throw new InvalidOperationException("Broker authentication is not available on .NET Core.");
#endif
        }

        private bool CanUseEmbeddedWebView()
        {
            // If we're in an interactive session and on .NET Framework then MSAL can show the WinForms-based embedded UI
#if NETFRAMEWORK
            return Context.SessionManager.IsDesktopSession;
#else
            return false;
#endif
        }

        private void EnsureCanUseEmbeddedWebView()
        {
#if NETFRAMEWORK
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("Embedded web view is not available without a desktop session.");
            }
#else
            throw new InvalidOperationException("Embedded web view is not available on .NET Core.");
#endif
        }

        private bool CanUseSystemWebView(IPublicClientApplication app, Uri redirectUri)
        {
            // MSAL requires the application redirect URI is a loopback address to use the System WebView
            return Context.SessionManager.IsDesktopSession && app.IsSystemWebViewAvailable && redirectUri.IsLoopback;
        }

        private void EnsureCanUseSystemWebView(IPublicClientApplication app, Uri redirectUri)
        {
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("System web view is not available without a desktop session.");
            }

            if (!app.IsSystemWebViewAvailable)
            {
                throw new InvalidOperationException("System web view is not available on this platform.");
            }

            if (!redirectUri.IsLoopback)
            {
                throw new InvalidOperationException("System web view is not available for this service configuration.");
            }
        }

        #endregion

        private class MsalResult : IMicrosoftAuthenticationResult
        {
            private readonly AuthenticationResult _msalResult;

            public MsalResult(AuthenticationResult msalResult)
            {
                _msalResult = msalResult;
            }

            public string AccessToken => _msalResult.AccessToken;
            public string AccountUpn => _msalResult.Account.Username;
            public string TokenSource => _msalResult.AuthenticationResultMetadata.TokenSource.ToString();
        }
    }
}
