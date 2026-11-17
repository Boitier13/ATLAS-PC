using System.Windows.Media;

namespace AtlasPc.ViewModels
{
    /// <summary>Une entrée de la barre latérale : titre, icône, vue associée.</summary>
    public sealed class NavigationItem
    {
        public NavigationItem(string title, string iconKey, object viewModel)
        {
            Title = title;
            IconKey = iconKey;
            ViewModel = viewModel;
        }

        public string Title { get; private set; }

        /// <summary>Clé de la géométrie d'icône, résolue dans le thème.</summary>
        public string IconKey { get; private set; }

        public object ViewModel { get; private set; }

        public Geometry Icon
        {
            get
            {
                var app = System.Windows.Application.Current;
                if (app == null) return Geometry.Empty;

                var geometry = app.TryFindResource(IconKey) as Geometry;
                return geometry ?? Geometry.Empty;
            }
        }
    }
}
