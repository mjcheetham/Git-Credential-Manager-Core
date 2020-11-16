﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Logging.Serilog;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI
{
    public static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't
        // initialized yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        private static void AppMain(Avalonia.Application app, string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => cts.Cancel();
            var gui = new AvaloniaGui(cts);

            try
            {
                // Show test UI when given no arguments
                if (args.Length == 0)
                {
                    gui.ShowWindow(() => new Tester());
                }
                else
                {
                    var prompts = new AuthenticationPrompts(gui);
                    var resultDict = new Dictionary<string, string>();

                    if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "prompt"))
                    {
                        string enterpriseUrl = CommandLineUtils.GetParameter(args, "--enterprise-url");
                        bool basic = CommandLineUtils.TryGetSwitch(args, "--basic");
                        bool oauth = CommandLineUtils.TryGetSwitch(args, "--oauth");
                        string username = CommandLineUtils.GetParameter(args, "--username");

                        if (!basic && !oauth)
                        {
                            throw new Exception("at least one authentication mode must be specified");
                        }

                        var result = prompts.ShowCredentialPrompt(
                            enterpriseUrl, basic, oauth,
                            ref username,
                            out string password);

                        switch (result)
                        {
                            case CredentialPromptResult.BasicAuthentication:
                                resultDict["mode"] = "basic";
                                resultDict["username"] = username;
                                resultDict["password"] = password;
                                break;

                            case CredentialPromptResult.OAuthAuthentication:
                                resultDict["mode"] = "oauth";
                                break;

                            case CredentialPromptResult.Cancel:
                                throw new OperationCanceledException("authentication prompt was canceled");

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "2fa"))
                    {
                        bool isSms = CommandLineUtils.TryGetSwitch(args, "--sms");

                        if (!prompts.ShowAuthenticationCodePrompt(isSms, out string authCode))
                        {
                            throw new Exception("failed to get authentication code");
                        }

                        resultDict["code"] = authCode;
                    }
                    else
                    {
                        throw new Exception($"unknown argument '{args[0]}'");
                    }

                    Console.Out.WriteDictionary(resultDict);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteDictionary(new Dictionary<string, string>
                {
                    ["error"] = e.Message
                });
                Environment.Exit(-1);
            }
        }
    }
}
