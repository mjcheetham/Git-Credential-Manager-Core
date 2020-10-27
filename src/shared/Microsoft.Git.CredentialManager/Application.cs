// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Interop;

namespace Microsoft.Git.CredentialManager
{
    public class Application : ApplicationBase, IConfigurableComponent
    {
        private readonly string _appPath;
        private readonly IHostProviderRegistry _providerRegistry;
        private readonly IConfigurationService _configurationService;

        public Application(ICommandContext context, string appPath)
            : this(context, new HostProviderRegistry(context), new ConfigurationService(context), appPath)
        {
        }

        internal Application(ICommandContext context,
                             IHostProviderRegistry providerRegistry,
                             IConfigurationService configurationService,
                             string appPath)
            : base(context)
        {
            EnsureArgument.NotNull(providerRegistry, nameof(providerRegistry));
            EnsureArgument.NotNull(configurationService, nameof(configurationService));
            EnsureArgument.NotNullOrWhiteSpace(appPath, nameof(appPath));

            _appPath = appPath;
            _providerRegistry = providerRegistry;
            _configurationService = configurationService;

            _configurationService.AddComponent(this);
        }

        public void RegisterProviders(params IHostProvider[] providers)
        {
            _providerRegistry.Register(providers);

            // Add any providers that are also configurable components to the configuration service
            foreach (IConfigurableComponent configurableProvider in providers.OfType<IConfigurableComponent>())
            {
                _configurationService.AddComponent(configurableProvider);
            }
        }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            string appName = Path.GetFileNameWithoutExtension(_appPath);

            // Construct all supported commands
            var commands = new CommandBase[]
            {
                new GetCommand(_providerRegistry),
                new StoreCommand(_providerRegistry),
                new EraseCommand(_providerRegistry),
                new ConfigureCommand(_configurationService),
                new UnconfigureCommand(_configurationService),
                new VersionCommand(),
                new HelpCommand(appName),
            };

            // Trace the current version and program arguments
            Context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            if (args.Length == 0)
            {
                Context.Streams.Error.WriteLine("Missing command.");
                HelpCommand.PrintUsage(Context.Streams.Error, appName);
                return -1;
            }

            foreach (var cmd in commands)
            {
                if (cmd.CanExecute(args))
                {
                    try
                    {
                        await cmd.ExecuteAsync(Context, args);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException ae)
                        {
                            ae.Handle(WriteException);
                        }
                        else
                        {
                            WriteException(e);
                        }

                        return -1;
                    }
                }
            }

            Context.Streams.Error.WriteLine("Unrecognized command '{0}'.", args[0]);
            HelpCommand.PrintUsage(Context.Streams.Error, appName);
            return -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _providerRegistry?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected bool WriteException(Exception ex)
        {
            // Try and use a nicer format for some well-known exception types
            switch (ex)
            {
                case InteropException interopEx:
                    Context.Streams.Error.WriteLine("fatal: {0} [0x{1:x}]", interopEx.Message, interopEx.ErrorCode);
                    break;
                default:
                    Context.Streams.Error.WriteLine("fatal: {0}", ex.Message);
                    break;
            }

            // Recurse to print all inner exceptions
            if (!(ex.InnerException is null))
            {
                WriteException(ex.InnerException);
            }

            return true;
        }

        #region IConfigurableComponent

        string IConfigurableComponent.Name => "Git Credential Manager";

        Task IConfigurableComponent.ConfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IGitConfiguration config;
            switch (target)
            {
                case ConfigurationTarget.User:
                    // For per-user configuration, we are looking for the following to be set in the global config:
                    //
                    // [credential]
                    //     ...                 # any number of helper entries (possibly none)
                    //     helper =            # an empty value to reset/clear any previous entries (if applicable)
                    //     helper = {_appPath} # the expected executable value & directly following the empty value
                    //     ...                 # any number of helper entries (possibly none)
                    //
                    config = Context.Git.GetConfiguration(GitConfigurationLevel.Global);
                    string[] currentValues = config.GetRegex(helperKey, Constants.RegexPatterns.Any).ToArray();

                    // Try to locate an existing app entry with a blank reset/clear entry immediately preceding
                    int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, _appPath));
                    if (appIndex > 0 && string.IsNullOrWhiteSpace(currentValues[appIndex - 1]))
                    {
                        Context.Trace.WriteLine("Credential helper user configuration is already set correctly.");
                    }
                    else
                    {
                        Context.Trace.WriteLine("Updating Git credential helper user configuration...");

                        // Clear any existing app entries in the configuration
                        config.UnsetAll(helperKey, Regex.Escape(_appPath));

                        // Add an empty value for `credential.helper`, which has the effect of clearing any helper value
                        // from any lower-level Git configuration, then add a second value which is the actual executable path.
                        config.ReplaceAll(helperKey, Constants.RegexPatterns.None, string.Empty);
                        config.ReplaceAll(helperKey, Constants.RegexPatterns.None, _appPath);
                    }
                    break;

                case ConfigurationTarget.System:
                    // For machine-wide configuration, we are looking for the following to be set in the system config:
                    //
                    // [credential]
                    //     helper = {_appPath}
                    //
                    config = Context.Git.GetConfiguration(GitConfigurationLevel.System);
                    string currentValue = config.GetValue(helperKey);
                    if (Context.FileSystem.IsSamePath(currentValue, _appPath))
                    {
                        Context.Trace.WriteLine("Credential helper system configuration is already set correctly.");
                    }
                    else
                    {
                        Context.Trace.WriteLine("Updating Git credential helper system configuration...");
                        config.SetValue(helperKey, _appPath);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown configuration target.");
            }

            return Task.CompletedTask;
        }

        Task IConfigurableComponent.UnconfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IGitConfiguration config;
            switch (target)
            {
                case ConfigurationTarget.User:
                    // For per-user configuration, we are looking for the following to be set in the global config:
                    //
                    // [credential]
                    //     ...                 # any number of helper entries (possibly none)
                    //     helper =            # an empty value to reset/clear any previous entries (if applicable)
                    //     helper = {_appPath} # the expected executable value & directly following the empty value
                    //     ...                 # any number of helper entries (possibly none)
                    //
                    // We should remove the {_appPath} entry, and any blank entries immediately preceding IFF there are no more entries following.
                    //
                    Context.Trace.WriteLine("Removing Git credential helper user configuration...");

                    config = Context.Git.GetConfiguration(GitConfigurationLevel.Global);
                    string[] currentValues = config.GetRegex(helperKey, Constants.RegexPatterns.Any).ToArray();

                    int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, _appPath));
                    if (appIndex > -1)
                    {
                        // Check for the presence of a blank entry immediately preceding an app entry in the last position
                        if (appIndex > 0 && appIndex == currentValues.Length - 1 &&
                            string.IsNullOrWhiteSpace(currentValues[appIndex - 1]))
                        {
                            // Clear the blank entry
                            config.UnsetAll(helperKey, Constants.RegexPatterns.Empty);
                        }

                        // Clear app entry
                        config.UnsetAll(helperKey, Regex.Escape(_appPath));
                    }
                    break;

                case ConfigurationTarget.System:
                    // For machine-wide configuration, we are looking for the following to be set in the system config:
                    //
                    // [credential]
                    //     helper = {_appPath}
                    //
                    // We should remove the {_appPath} entry if it exists.
                    //
                    Context.Trace.WriteLine("Removing Git credential helper system configuration...");
                    config = Context.Git.GetConfiguration(GitConfigurationLevel.System);
                    config.UnsetAll(helperKey, Regex.Escape(_appPath));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown configuration target.");
            }

            return Task.CompletedTask;
        }

        private string GetGitConfigAppName()
        {
            const string gitCredentialPrefix = "git-credential-";

            string appName = Path.GetFileNameWithoutExtension(_appPath);
            if (appName != null && appName.StartsWith(gitCredentialPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return appName.Substring(gitCredentialPrefix.Length);
            }

            return _appPath;
        }

        #endregion
    }
}
