// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace GitHub
{
    public interface IGitHubAuthentication : IDisposable
    {
        Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms);

        Task<OAuth2TokenResult> GetOAuthTokenAsync(Uri targetUri, IEnumerable<string> scopes, bool useDeviceCode);
    }

    public class AuthenticationPromptResult
    {
        public AuthenticationPromptResult(AuthenticationModes mode)
        {
            AuthenticationMode = mode;
        }

        public AuthenticationPromptResult(AuthenticationModes mode, ICredential credential)
            : this(mode)
        {
            Credential = credential;
        }

        public AuthenticationModes AuthenticationMode { get; }

        public ICredential Credential { get; set; }
    }

    [Flags]
    public enum AuthenticationModes
    {
        None       = 0,
        Basic      = 1,
        OAuth      = 1 << 1,
        Pat        = 1 << 2,
        DeviceCode = 1 << 3,

        All        = Basic | OAuth | Pat | DeviceCode
    }

    public class GitHubAuthentication : AuthenticationBase, IGitHubAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "github",
        };

        public GitHubAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

            if (modes == AuthenticationModes.None)
            {
                throw new ArgumentException(@$"Must specify at least one {nameof(AuthenticationModes)}", nameof(modes));
            }

            if (modes == AuthenticationModes.OAuth)
            {
                throw new ArgumentException(@$"Authentication mode '{AuthenticationModes.OAuth}' requires an interactive/UI session.", nameof(modes));
            }

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                var promptArgs = new StringBuilder("prompt");
                if (modes == AuthenticationModes.All)
                {
                    promptArgs.Append(" --all");
                }
                else
                {
                    if ((modes & AuthenticationModes.Basic)      != 0) promptArgs.Append(" --basic");
                    if ((modes & AuthenticationModes.DeviceCode) != 0) promptArgs.Append(" --devcode");
                    if ((modes & AuthenticationModes.OAuth)      != 0) promptArgs.Append(" --oauth");
                    if ((modes & AuthenticationModes.Pat)        != 0) promptArgs.Append(" --pat");
                }
                if (!GitHubHostProvider.IsGitHubDotCom(targetUri)) promptArgs.AppendFormat(" --enterprise-url {0}", QuoteCmdArg(targetUri.ToString()));
                if (!string.IsNullOrWhiteSpace(userName)) promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));

                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, promptArgs.ToString(), null);

                if (!resultDict.TryGetValue("mode", out string responseMode))
                {
                    throw new Exception("Missing 'mode' in response");
                }

                switch (responseMode.ToLowerInvariant())
                {
                    case "pat":
                        if (!resultDict.TryGetValue("pat", out string pat))
                        {
                            throw new Exception("Missing 'pat' in response");
                        }

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Pat, new GitCredential(userName, pat));

                    case "oauth":
                        return new AuthenticationPromptResult(AuthenticationModes.OAuth);

                    case "devcode":
                        return new AuthenticationPromptResult(AuthenticationModes.DeviceCode);

                    case "basic":
                        if (!resultDict.TryGetValue("username", out userName))
                        {
                            throw new Exception("Missing 'username' in response");
                        }

                        if (!resultDict.TryGetValue("password", out string password))
                        {
                            throw new Exception("Missing 'password' in response");
                        }

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Basic, new GitCredential(userName, password));

                    default:
                        throw new Exception($"Unknown mode value in response '{responseMode}'");
                }
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                switch (modes)
                {
                    case AuthenticationModes.Basic:
                        Context.Terminal.WriteLine("Enter GitHub credentials for '{0}'...", targetUri);

                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            userName = Context.Terminal.Prompt("Username");
                        }
                        else
                        {
                            Context.Terminal.WriteLine("Username: {0}", userName);
                        }

                        string password = Context.Terminal.PromptSecret("Password");

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Basic, new GitCredential(userName, password));

                    case AuthenticationModes.OAuth:
                        return new AuthenticationPromptResult(AuthenticationModes.OAuth);

                    case AuthenticationModes.DeviceCode:
                        return new AuthenticationPromptResult(AuthenticationModes.DeviceCode);

                    case AuthenticationModes.Pat:
                        Context.Terminal.WriteLine("Enter GitHub personal access token for '{0}'...", targetUri);
                        string pat = Context.Terminal.PromptSecret("Token");
                        return new AuthenticationPromptResult(
                            AuthenticationModes.Pat, new GitCredential(userName, pat));

                    case AuthenticationModes.None:
                        throw new ArgumentOutOfRangeException(nameof(modes), @$"At least one {nameof(AuthenticationModes)} must be supplied");

                    default:
                        var menuTitle = $"Select an authentication method for '{targetUri}'";
                        var menu = new TerminalMenu(Context.Terminal, menuTitle);

                        TerminalMenuItem oauthItem = null;
                        TerminalMenuItem devCodeItem = null;
                        TerminalMenuItem patItem = null;
                        TerminalMenuItem basicItem = null;

                        if (Context.SessionManager.IsDesktopSession && // OAuth browser flow requires a UI
                            (modes & AuthenticationModes.OAuth)      != 0) oauthItem   = menu.Add("Web browser");
                        if ((modes & AuthenticationModes.DeviceCode) != 0) devCodeItem = menu.Add("Device code");
                        if ((modes & AuthenticationModes.Pat)        != 0) patItem     = menu.Add("Personal access token");
                        if ((modes & AuthenticationModes.Basic)      != 0) basicItem   = menu.Add("Username/password");

                        // Default to the 'first' choice in the menu
                        TerminalMenuItem choice = menu.Show(0);

                        if (choice == oauthItem)   goto case AuthenticationModes.OAuth;
                        if (choice == devCodeItem) goto case AuthenticationModes.DeviceCode;
                        if (choice == basicItem)   goto case AuthenticationModes.Basic;
                        if (choice == patItem)     goto case AuthenticationModes.Pat;

                        throw new Exception();
                }
            }
        }

        public async Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms)
        {
            ThrowIfUserInteractionDisabled();

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "2fa", null);

                if (!resultDict.TryGetValue("code", out string authCode))
                {
                    throw new Exception("Missing 'code' in response");
                }

                return authCode;
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                Context.Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

                if (isSms)
                {
                    Context.Terminal.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
                }
                else
                {
                    Context.Terminal.WriteLine("Use your registered authentication app to generate an authentication code.");
                }

                return Context.Terminal.Prompt("Authentication code");
            }
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenAsync(Uri targetUri, IEnumerable<string> scopes, bool useDeviceCode)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri);

            // If we're not using the device code flow we require an interactive/UI session
            if (!Context.SessionManager.IsDesktopSession && !useDeviceCode)
            {
                throw new InvalidOperationException("Browser-based OAuth flows require an interactive/UI session.");
            }

            if (!useDeviceCode)
            {
                var browserOptions = new OAuth2WebBrowserOptions
                {
                    SuccessResponseHtml = GitHubResources.AuthenticationResponseSuccessHtml,
                    FailureResponseHtmlFormat = GitHubResources.AuthenticationResponseFailureHtmlFormat
                };
                var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

                // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
                Context.Terminal.WriteLine("Please complete authentication in your browser... press CTRL+C to cancel.");

                OAuth2AuthorizationCodeResult authCodeResult = await oauthClient.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);

                return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
            }

            OAuth2DeviceCodeResult deviceCodeResult = await oauthClient.GetDeviceCodeAsync(scopes, CancellationToken.None);

            if (Context.SessionManager.IsDesktopSession && TryFindHelperExecutablePath(out string helperPath))
            {
                var promptArgs = new StringBuilder("devcode");
                promptArgs.AppendFormat(" --code {0}", QuoteCmdArg(deviceCodeResult.UserCode));
                promptArgs.AppendFormat(" --url {0}", QuoteCmdArg(deviceCodeResult.VerificationUri.ToString()));

                // Show the code but don't wait for the dialog to be closed.
                // If the dialog is closed then cancel the GetToken operation.
                var cts = new CancellationTokenSource();
                var _ = InvokeHelperAsync(helperPath, promptArgs.ToString()).ContinueWith(_ => cts.Cancel());

                try
                {
                    return await oauthClient.GetTokenByDeviceCodeAsync(deviceCodeResult, cts.Token);
                }
                catch (TaskCanceledException e)
                {
                    throw new Exception("Authentication was cancelled");
                }
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                Context.Terminal.WriteLine(
                    "To complete authentication please visit {0} and enter the following code: {1}",
                    deviceCodeResult.VerificationUri, deviceCodeResult.UserCode);

                return await oauthClient.GetTokenByDeviceCodeAsync(deviceCodeResult, CancellationToken.None);
            }
        }

        private bool TryFindHelperExecutablePath(out string path)
        {
            return TryFindHelperExecutablePath(
                GitHubConstants.EnvironmentVariables.AuthenticationHelper,
                GitHubConstants.GitConfiguration.Credential.AuthenticationHelper,
                GitHubConstants.DefaultAuthenticationHelper,
                out path);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
