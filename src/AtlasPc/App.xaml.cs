using System;
using System.Windows;
using System.Windows.Threading;
using AtlasPc.Data;
using AtlasPc.Shell;
using AtlasPc.ViewModels;
using AtlasPc.Views;

namespace AtlasPc
{
    public partial class App : Application
    {
        private Database _database;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;

            if (!OpenDatabase())
            {
                Shutdown(1);
                return;
            }

            var store = new InventoryStore(_database.Connection);

            // Un inventaire laissé en "running" vient d'une session interrompue :
            // il ne contient rien d'exploitable et ne doit pas apparaître dans l'historique.
            try
            {
                store.DeleteAbandoned();
            }
            catch (Exception)
            {
                // Le nettoyage est un confort : son échec ne doit pas empêcher le démarrage.
            }

            var viewModel = new ShellViewModel(store, _database);

            var window = new ShellWindow { DataContext = viewModel };
            MainWindow = window;
            window.Show();
        }

        /// <summary>
        /// Ouvre la base ; en cas d'échec, propose de renommer le fichier et d'en recréer
        /// une vide (l'ancien fichier n'est jamais supprimé).
        /// </summary>
        private bool OpenDatabase()
        {
            _database = new Database(Database.DefaultPath);

            try
            {
                _database.Open();
                return true;
            }
            catch (Exception ex)
            {
                if (!Dialogs.OfferRecreate(Database.DefaultPath, ex)) return false;

                try
                {
                    _database.RenameToBackup();
                    _database = new Database(Database.DefaultPath);
                    _database.Open();
                    return true;
                }
                catch (Exception again)
                {
                    Dialogs.Error("Base de données",
                        "La base n'a pas pu être recréée.\n\n" + again.Message);
                    return false;
                }
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Une exception non prévue ne doit pas fermer l'application sans explication.
            e.Handled = true;
            Dialogs.Error("Erreur inattendue",
                "ATLAS PC a rencontré une erreur inattendue.\n\n" + e.Exception.Message);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_database != null)
            {
                _database.Dispose();
                _database = null;
            }

            base.OnExit(e);
        }
    }
}
