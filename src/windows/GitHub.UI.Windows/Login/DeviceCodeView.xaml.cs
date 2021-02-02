using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitHub.UI.Login
{
    public partial class DeviceCodeView : UserControl
    {
        public DeviceCodeView()
        {
            InitializeComponent();
        }

        public DeviceCodeViewModel ViewModel => DataContext as DeviceCodeViewModel;
    }
}
