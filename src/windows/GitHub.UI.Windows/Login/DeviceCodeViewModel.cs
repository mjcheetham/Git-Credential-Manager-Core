using System.Windows.Input;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace GitHub.UI.Login
{
    public class DeviceCodeViewModel : WindowViewModel
    {
        private string userCode;
        private string verificationUrl;

        public DeviceCodeViewModel(string userCode, string verificationUrl)
        {
            UserCode = userCode;
            VerificationUrl = verificationUrl;

            CancelCommand = new RelayCommand(Cancel);
            NavigateLearnMoreCommand = new RelayCommand(NavigateLearnMore);
            NavigateVerificationCommand = new RelayCommand(NavigateVerification);
        }

        public override bool IsValid => true;

        public override string Title => GitHubResources.DeviceCodeTitle;

        public string Description => GitHubResources.DeviceCodeDescription;

        public string AutoCloseMessage => GitHubResources.DeviceCodeAutoCloseMessage;

        public string VerificationUrl
        {
            get => this.verificationUrl;
            private set => SetAndRaisePropertyChanged(ref this.verificationUrl, value);
        }

        public string UserCode
        {
            get => this.userCode;
            set => SetAndRaisePropertyChanged(ref this.userCode, value);
        }

        public RelayCommand CancelCommand { get; }

        public ICommand NavigateLearnMoreCommand { get; }

        public ICommand NavigateVerificationCommand { get; }

        private void NavigateLearnMore()
        {
            OpenDefaultBrowser(NavigateLearnMoreUrl);
        }

        private void NavigateVerification()
        {
            OpenDefaultBrowser(VerificationUrl);
        }

        public string NavigateLearnMoreUrl => "https://aka.ms/gcmcore-githubdevicecode";
    }
}
