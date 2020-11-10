using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GitHub.UI.Controls;

namespace GitHub.UI.Login
{
    public partial class Login2FaView : UserControl
    {
        private TwoFactorInput _authenticationCode;

        public Login2FaView()
        {
            InitializeComponent();

            _authenticationCode = this.FindControl<TwoFactorInput>("authenticationCode");

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
        /// The DataContext of this view as a Login2FaView.
        /// </summary>
        public Login2FaViewModel ViewModel => DataContext as Login2FaViewModel;

        void SetFocus()
        {
            _authenticationCode.SetFocus();
        }
    }
}
