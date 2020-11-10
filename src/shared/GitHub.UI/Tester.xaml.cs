using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitHub.UI
{
    public class Tester : Window
    {
        public Tester()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
