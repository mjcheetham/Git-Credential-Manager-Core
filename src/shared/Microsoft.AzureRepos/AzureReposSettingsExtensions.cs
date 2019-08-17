using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    public static class AzureReposSettingsExtensions
    {
        public static bool GetIsAccessTokenModeEnabled(this ISettings settings)
        {
            if (settings.TryGetSetting(
                AzureDevOpsConstants.EnvironmentVariables.AzureReposAccessTokenMode,
                Constants.GitConfiguration.Credential.SectionName,
                AzureDevOpsConstants.GitConfiguration.Credential.AzureReposAccessTokenMode,
                out string value))
            {
                return value.ToBooleanyOrDefault(false);
            }

            return false;
        }
    }
}
