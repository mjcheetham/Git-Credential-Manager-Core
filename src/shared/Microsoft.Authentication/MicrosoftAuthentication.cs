// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace Microsoft.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<IMicrosoftAuthenticationResult> GetTokenAsync(string authority, string clientId, Uri redirectUri,
            string[] scopes, string userName);
    }

    public interface IMicrosoftAuthenticationResult
    {
        string AccessToken { get; }
        string AccountUpn { get; }
    }

    public enum MicrosoftAuthenticationFlowType
    {
        Auto = 0,
        EmbeddedWebView = 1,
        SystemWebView = 2,
        DeviceCode = 3,
        OperatingSystemBroker = 4,
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
            string authority, string clientId, Uri redirectUri, string[] scopes, string userName)
        {
            PublicClientApplicationBuilder builder = CreateAppBuilder(authority, clientId, redirectUri);

            // Check for a user flow preference
            MicrosoftAuthenticationFlowType flowType = GetFlowType();

            // Register to use the broker now if we are allowed to and are required to based on the selected flow
            if (CanUseOperatingSystemBroker() &&
                (flowType == MicrosoftAuthenticationFlowType.Auto ||
                 flowType == MicrosoftAuthenticationFlowType.OperatingSystemBroker))
            {
                builder.WithExperimentalFeatures().WithBroker();
            }

            IPublicClientApplication app = builder.Build();

            // Register the token cache
            await RegisterTokenCacheAsync(app);

            AuthenticationResult result = null;

            // Try silent authentication first if we know about an existing user
            if (!string.IsNullOrWhiteSpace(userName))
            {
                result = await GetAccessTokenSilentlyAsync(app, scopes, userName);
            }

            //
            // If we failed to acquire an AT silently (either because we don't have an existing user, or the user's RT
            // has expired) we need to prompt the user for credentials.
            //
            // If the user has expressed a preference in how the want to perform the interactive authentication flows
            // then we respect that.
            //
            // Otherwise, depending on the current platform and session type we try to show the most appropriate
            // authentication interface:
            //
            // If we can use the OS broker then delegate to that. This is the best and natural experience.
            //
            // On Windows we also support the WinForms & WebView2-based 'embedded' UI.
            //
            // On other platforms MSAL only supports the system webview flow (launch the user's browser) and the device
            // code flow:
            //
            //    - The system webview flow requires that the redirect URI is a loopback address, and that we are in an
            //      interactive session.
            //    - The device code flow has no limitations other than a way to communicate to the user the code
            //      required to authenticate.
            //
            if (result is null)
            {
                // If the user has disabled interaction all we can do is fail at this point
                ThrowIfUserInteractionDisabled();

                switch (flowType)
                {
                    case MicrosoftAuthenticationFlowType.Auto:
                        if (CanUseOperatingSystemBroker())
                            goto case MicrosoftAuthenticationFlowType.OperatingSystemBroker;

                        if (CanUseEmbeddedWebView(app))
                            goto case MicrosoftAuthenticationFlowType.EmbeddedWebView;

                        if (CanUseSystemWebView(app, redirectUri))
                            goto case MicrosoftAuthenticationFlowType.SystemWebView;

                        // Fall back to device code flow
                        goto case MicrosoftAuthenticationFlowType.DeviceCode;

                    case MicrosoftAuthenticationFlowType.OperatingSystemBroker:
                        Context.Trace.WriteLine("Performing interactive auth with broker...");
                        EnsureCanUseOperatingSystemBroker();
                        result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
                        break;

                    case MicrosoftAuthenticationFlowType.EmbeddedWebView:
                        Context.Trace.WriteLine("Performing interactive auth with embedded web view...");
                        EnsureCanUseEmbeddedWebView(app);
                        result = await app.AcquireTokenInteractive(scopes)
                            .WithPrompt(Prompt.SelectAccount)
                            .WithUseEmbeddedWebView(true)
                            .WithEmbeddedWebViewOptions(GetEmbeddedWebViewOptions(app))
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
                        return MicrosoftAuthenticationFlowType.OperatingSystemBroker;
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

        private PublicClientApplicationBuilder CreateAppBuilder(string authority, string clientId, Uri redirectUri)
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

            return appBuilder;
        }

        #endregion

        #region Helpers

        private async Task RegisterTokenCacheAsync(IPublicClientApplication app)
        {
            Context.Trace.WriteLine(
                "Configuring Microsoft Authentication token cache to instance shared with Microsoft developer tools...");

            if (!PlatformUtils.IsWindows() && !PlatformUtils.IsPosix())
            {
                string osType = PlatformUtils.GetPlatformInformation().OperatingSystemType;
                Context.Trace.WriteLine($"Token cache integration is not supported on {osType}.");
                return;
            }

            // We use the MSAL extension library to provide us consistent cache file access semantics (synchronisation, etc)
            // as other Microsoft developer tools such as the Azure PowerShell CLI.
            MsalCacheHelper helper = null;
            try
            {
                var storageProps = CreateTokenCacheProps(useLinuxFallback: false);
                helper = await MsalCacheHelper.CreateAsync(storageProps);

                // Test that cache access is working correctly
                helper.VerifyPersistence();
            }
            catch (MsalCachePersistenceException ex)
            {
                Context.Streams.Error.WriteLine("warning: cannot persist Microsoft authentication token cache securely!");
                Context.Trace.WriteLine("Cannot persist Microsoft Authentication data securely!");
                Context.Trace.WriteException(ex);

                if (PlatformUtils.IsMacOS())
                {
                    // On macOS sometimes the Keychain returns the "errSecAuthFailed" error - we don't know why
                    // but it appears to be something to do with not being able to access the keychain.
                    // Locking and unlocking (or restarting) often fixes this.
                    Context.Streams.Error.WriteLine(
                        "warning: there is a problem accessing the login Keychain - either manually lock and unlock the " +
                        "login Keychain, or restart the computer to remedy this");
                }
                else if (PlatformUtils.IsLinux())
                {
                    // On Linux the SecretService/keyring might not be available so we must fall-back to a plaintext file.
                    Context.Streams.Error.WriteLine("warning: using plain-text fallback token cache");
                    Context.Trace.WriteLine("Using fall-back plaintext token cache on Linux.");
                    var storageProps = CreateTokenCacheProps(useLinuxFallback: true);
                    helper = await MsalCacheHelper.CreateAsync(storageProps);
                }
            }

            if (helper is null)
            {
                Context.Streams.Error.WriteLine("error: failed to set up Microsoft Authentication token cache!");
                Context.Trace.WriteLine("Failed to integrate with shared token cache!");
            }
            else
            {
                helper.RegisterCache(app.UserTokenCache);
                Context.Trace.WriteLine("Microsoft developer tools token cache configured.");
            }
        }

        private StorageCreationProperties CreateTokenCacheProps(bool useLinuxFallback)
        {
            const string cacheFileName = "msal.cache";
            string cacheDirectory;
            if (PlatformUtils.IsWindows())
            {
                // The shared MSAL cache is located at "%LocalAppData%\.IdentityService\msal.cache" on Windows.
                cacheDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    ".IdentityService"
                );
            }
            else
            {
                // The shared MSAL cache metadata is located at "~/.local/.IdentityService/msal.cache" on UNIX.
                cacheDirectory = Path.Combine(Context.FileSystem.UserHomePath, ".local", ".IdentityService");
            }

            // The keychain is used on macOS with the following service & account names
            var builder = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory)
                .WithMacKeyChain("Microsoft.Developer.IdentityService", "MSALCache");

            if (useLinuxFallback)
            {
                builder.WithLinuxUnprotectedFile();
            }
            else
            {
                // The SecretService/keyring is used on Linux with the following collection name and attributes
                builder.WithLinuxKeyring(cacheFileName,
                    "default", "MSALCache",
                    new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                    new KeyValuePair<string, string>("Microsoft.Developer.IdentityService", "1.0.0.0"));
            }

            return builder.Build();
        }

        private EmbeddedWebViewOptions GetEmbeddedWebViewOptions(IPublicClientApplication app)
        {
            string runtimePath = null;
            
            // If MSAL can locate the WebView2 evergreen runtime then we don't have to use
            // our potentially outdated bundled instance.  
            if (!app.IsEmbeddedWebViewAvailable() && !TryGetWebView2Runtime(out runtimePath))
            {
                throw new InvalidOperationException("Unable to locate WebView2 runtime");
            }

            return new EmbeddedWebViewOptions
            {
                Title = "Git Credential Manager",
                WebView2BrowserExecutableFolder = runtimePath
            };
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

        private bool TryGetWebView2Runtime(out string path)
        {
            // TODO: resolve this
            // TODO: include the runtime
            path = @"C:\Users\mattche\webview2";
            return true;
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

        private bool CanUseEmbeddedWebView(IPublicClientApplication app)
        {
#if WINDOWS
            // If we're on Windows and in an interactive session then MSAL can show the WebView2-based embedded UI
            return PlatformUtils.IsWindows() && Context.SessionManager.IsDesktopSession && 
                   (app.IsEmbeddedWebViewAvailable() || TryGetWebView2Runtime(out _));
#else
            return false;
#endif
        }

        private void EnsureCanUseEmbeddedWebView(IPublicClientApplication app)
        {
#if WINDOWS
            if (!PlatformUtils.IsWindows())
            {
                throw new InvalidOperationException("Embedded web view is only available on Windows.");
            }

            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("Embedded web view is not available without a desktop session.");
            }

            if (!app.IsEmbeddedWebViewAvailable() && !TryGetWebView2Runtime(out _))
            {
                throw new InvalidOperationException("Embedded web view requires the WebView2 runtime which was not found.");
            }
#else
            throw new InvalidOperationException("Embedded web view is only available on when targeting at least net5.0-windows10.0.17763.0.");
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

        private static bool CanUseOperatingSystemBroker()
        {
            // We only support the broker on Windows 10
            return PlatformUtils.IsWindows10();
        }

        private static void EnsureCanUseOperatingSystemBroker()
        {
            if (!PlatformUtils.IsWindows10())
            {
                throw new InvalidOperationException("OS broker is only available in Windows 10.");
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
        }
    }
}
