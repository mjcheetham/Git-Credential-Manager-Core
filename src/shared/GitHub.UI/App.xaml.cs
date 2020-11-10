using Avalonia;
using Avalonia.Markup.Xaml;

namespace GitHub.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}
