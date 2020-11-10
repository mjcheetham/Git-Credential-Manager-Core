using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace GitHub.UI.Octicons
{
    /// <summary>
    /// Represent a raw path with no transformation. Uses the coordinate system from the octicon/svg files which
    /// are all drawn on a 1024px high canvas with variable width. If you're just after the shape this control
    /// can be used with Stretch=Uniform. If you're looking for an accurately scaled octicon correctly position
    /// you'll have to explicitly set the height of the path to 1024 and wrap it in a viewbox to scale it down
    /// to the size you want.
    /// </summary>
    public class OcticonPath : Shape
    {
        private static readonly Lazy<Dictionary<Octicon, Lazy<Geometry>>> cache =
            new Lazy<Dictionary<Octicon, Lazy<Geometry>>>(PrepareCache);

        public static readonly StyledProperty<Octicon> IconProperty = AvaloniaProperty.Register<OcticonPath, Octicon>(
            "Icon", defaultValue: Octicon.mark_github, notifying: OnIconChanged);

        public Octicon Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return GetGeometryForIcon(this.Icon);
        }

        private static Geometry GetGeometryForIcon(Octicon icon)
        {
            Dictionary<Octicon, Lazy<Geometry>> c = cache.Value;

            if (c.TryGetValue(icon, out Lazy<Geometry> g))
            {
                return g.Value;
            }

            throw new ArgumentException($@"Unknown Octicon: {icon}", nameof(icon));
        }

        // Initializes the cache dictionary with lazy entries for all available octicons
        private static Dictionary<Octicon, Lazy<Geometry>> PrepareCache()
        {
            return Enum.GetValues(typeof(Octicon))
                .Cast<Octicon>()
                .ToDictionary(icon => icon, icon => new Lazy<Geometry>(() => LoadGeometry(icon), LazyThreadSafetyMode.None));
        }

        private static Geometry LoadGeometry(Octicon icon)
        {
            string name = Enum.GetName(typeof(Octicon), icon);

            if (name == "lock")
            {
                name = "_lock";
            }

            var pathData = OcticonPaths.ResourceManager.GetString(name, CultureInfo.InvariantCulture);

            if (pathData == null)
            {
                throw new ArgumentException($"Could not find octicon geometry for '{name}'");
            }

            return Geometry.Parse(pathData);
        }

        private static void OnIconChanged(IAvaloniaObject sender, bool isBefore)
        {
            // Update path property to new value
            if (!isBefore)
            {
                Octicon newValue = sender.GetValue(IconProperty);
                sender.SetValue(Path.DataProperty, GetGeometryForIcon(newValue));
            }
        }
    }
}
