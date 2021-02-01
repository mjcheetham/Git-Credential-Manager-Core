using System.CommandLine;

namespace Microsoft.Git.CredentialManager.Commands
{
    public interface ICommandProvider
    {
        /// <summary>
        /// Configure a custom provider command.
        /// </summary>
        /// <param name="rootCommand">Root provider command.</param>
        void ConfigureCommand(Command rootCommand);
    }

    internal class ProviderCommand : Command
    {
        public ProviderCommand(ICommandContext context, IHostProvider provider)
            : base(provider.Id, $"Commands for interacting with the {provider.Name} host provider")
        {
            Context = context;
        }

        protected ICommandContext Context { get; }
    }
}
