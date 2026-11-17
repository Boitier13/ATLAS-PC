using System;
using System.Windows;
using AtlasPc.Models;

namespace AtlasPc.Shell
{
    /// <summary>
    /// Boîtes de dialogue de l'application. Regroupées ici pour que les ViewModels
    /// n'aient jamais à référencer WPF.
    /// </summary>
    internal static class Dialogs
    {
        private static Window Owner
        {
            get
            {
                var app = Application.Current;
                if (app == null) return null;
                var window = app.MainWindow;
                return window != null && window.IsVisible ? window : null;
            }
        }

        public static bool ConfirmDeleteInventory(Inventory inventory)
        {
            if (inventory == null) return false;

            var message =
                "Supprimer l'inventaire du " + inventory.StartedText + " ?\n\n" +
                inventory.RootsText + " · " + inventory.FilesText + " fichiers · " +
                inventory.SizeText + "\n\n" +
                "Les données de cet inventaire seront définitivement perdues. " +
                "Les fichiers de vos disques ne sont pas concernés.";

            return Show(message, "Supprimer un inventaire",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
        }

        /// <summary>Propose de renommer une base illisible et d'en recréer une vide.</summary>
        public static bool OfferRecreate(string databasePath, Exception error)
        {
            var message =
                "ATLAS PC ne peut pas ouvrir sa base de données.\n\n" +
                databasePath + "\n\n" +
                error.Message + "\n\n" +
                "La base peut être renommée (elle ne sera pas supprimée) et une base vide " +
                "recréée. Les inventaires qu'elle contenait ne seront plus visibles tant que " +
                "le fichier renommé n'aura pas été remis en place.";

            return Show(message, "Base de données illisible",
                MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK;
        }

        public static void Error(string title, string message)
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Affiche une boîte de dialogue avec la fenêtre principale comme propriétaire
        /// lorsqu'elle existe ; sans propriétaire sinon. Le défaut proposé est « Annuler ».
        /// </summary>
        private static MessageBoxResult Show(string message, string title,
                                             MessageBoxButton buttons, MessageBoxImage image)
        {
            var owner = Owner;
            if (owner != null)
            {
                return MessageBox.Show(owner, message, title, buttons, image, MessageBoxResult.Cancel);
            }

            return MessageBox.Show(message, title, buttons, image, MessageBoxResult.Cancel);
        }
    }
}
