using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace GitHub.UI.Login
{
    public partial class LoginCredentialsView : UserControl
    {
        private readonly TextBox _loginBox;
        private readonly TextBox _passwordBox;
        private readonly Button _loginLink;

        public LoginCredentialsView()
        {
            InitializeComponent();

            _loginBox = this.FindControl<TextBox>("loginBox");
            _passwordBox = this.FindControl<TextBox>("passwordBox");
            _loginLink = this.FindControl<Button>("loginLink");

            PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(IsVisible) && IsVisible)
                {
                    Dispatcher.UIThread.Post(SetFocus, DispatcherPriority.ApplicationIdle);
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// The DataContext of this view as a LoginCredentialsViewModel.
        /// </summary>
        public LoginCredentialsViewModel ViewModel => DataContext as LoginCredentialsViewModel;

        void SetFocus()
        {
            if (ViewModel is null)
            {
                return;
            }

            if (ViewModel.IsLoginUsingUsernameAndPasswordVisible)
            {
                if (string.IsNullOrWhiteSpace(ViewModel.UsernameOrEmail))
                {
                    _loginBox.Focus();
                }
                else
                {
                    _passwordBox.Focus();
                }
            }
            else if (ViewModel.IsLoginUsingBrowserVisible)
            {
                _loginLink.Focus();
            }
        }
    }
}
