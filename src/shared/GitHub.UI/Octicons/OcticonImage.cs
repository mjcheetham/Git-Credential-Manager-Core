using Avalonia;
using Avalonia.Controls.Primitives;

namespace GitHub.UI.Octicons
{
    public class OcticonImage : TemplatedControl
    {
        public Octicon Icon
        {
            get => GetValue(OcticonPath.IconProperty);
            set => SetValue(OcticonPath.IconProperty, value);
        }

        public static readonly AvaloniaProperty IconProperty = OcticonPath.IconProperty.AddOwner<OcticonImage>();
    }
}
