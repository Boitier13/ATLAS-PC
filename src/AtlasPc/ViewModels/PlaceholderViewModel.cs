namespace AtlasPc.ViewModels
{
    /// <summary>
    /// Rubrique qui n'est pas encore développée : elle annonce ce qu'elle contiendra
    /// et à quelle étape elle arrive. Aucune donnée simulée n'est affichée.
    /// </summary>
    public sealed class PlaceholderViewModel
    {
        public PlaceholderViewModel(string title, string stage, string message)
        {
            Title = title;
            Stage = stage;
            Message = message;
        }

        public string Title { get; private set; }

        /// <summary>Étape du plan où la rubrique sera livrée.</summary>
        public string Stage { get; private set; }

        public string Message { get; private set; }
    }
}
