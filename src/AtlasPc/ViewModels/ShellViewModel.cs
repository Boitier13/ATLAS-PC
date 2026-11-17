using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AtlasPc.Data;
using AtlasPc.Models;
using AtlasPc.Shell;

namespace AtlasPc.ViewModels
{
    /// <summary>
    /// Fenêtre principale : navigation entre les sept rubriques et état de l'inventaire
    /// en cours. Le tableau de bord utilise ce même view-model.
    /// </summary>
    public sealed class ShellViewModel : ObservableObject
    {
        private const int InventoryListLimit = 50;

        private readonly InventoryStore _store;
        private readonly Database _database;

        private NavigationItem _selectedSection;
        private Inventory _latestInventory;
        private bool _isScanning;
        private double _progressPercent;
        private string _progressText;

        public ShellViewModel(InventoryStore store, Database database)
        {
            if (store == null) throw new ArgumentNullException("store");
            if (database == null) throw new ArgumentNullException("database");
            _store = store;
            _database = database;

            Inventories = new ObservableCollection<Inventory>();
            Sections = BuildSections();

            StartInventoryCommand = new RelayCommand(StartInventory, () => !IsScanning);
            CancelScanCommand = new RelayCommand(CancelScan, () => IsScanning);
            DeleteInventoryCommand = new RelayCommand(DeleteInventory);

            SelectedSection = Sections[0];
            LoadInventories();
        }

        // --- Navigation -------------------------------------------------------

        public ObservableCollection<NavigationItem> Sections { get; private set; }

        public NavigationItem SelectedSection
        {
            get { return _selectedSection; }
            set { Set(ref _selectedSection, value); OnPropertyChanged("CurrentTitle"); }
        }

        public string CurrentTitle
        {
            get { return _selectedSection == null ? "ATLAS PC" : _selectedSection.Title; }
        }

        public string VersionText
        {
            get { return "version " + AppVersion; }
        }

        // --- Inventaires ------------------------------------------------------

        public ObservableCollection<Inventory> Inventories { get; private set; }

        public Inventory LatestInventory
        {
            get { return _latestInventory; }
            private set
            {
                if (Set(ref _latestInventory, value))
                {
                    OnPropertyChanged("HasInventory");
                    OnPropertyChanged("DatabaseSizeText");
                }
            }
        }

        public bool HasInventory
        {
            get { return _latestInventory != null; }
        }

        public string DatabaseSizeText
        {
            get
            {
                var size = _database.SizeBytes;
                return size <= 0 ? "base vide" : "base : " + Inventory.FormatBytes(size);
            }
        }

        // --- Progression ------------------------------------------------------

        public bool IsScanning
        {
            get { return _isScanning; }
            private set
            {
                if (Set(ref _isScanning, value))
                {
                    OnPropertyChanged("IsNotScanning");
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsNotScanning
        {
            get { return !_isScanning; }
        }

        public double ProgressPercent
        {
            get { return _progressPercent; }
            private set { Set(ref _progressPercent, value); }
        }

        public string ProgressText
        {
            get { return _progressText; }
            private set { Set(ref _progressText, value); }
        }

        // --- Commandes --------------------------------------------------------

        public ICommand StartInventoryCommand { get; private set; }
        public ICommand CancelScanCommand { get; private set; }
        public ICommand DeleteInventoryCommand { get; private set; }

        private CancellationTokenSource _cancellation;

        /// <summary>
        /// Étape 1 : simulation. Aucun disque n'est lu ; la ligne est créée puis clôturée
        /// en <c>cancelled</c> avec une note explicite, pour ne jamais laisser croire à un
        /// inventaire réel. Remplacé à l'étape 3A par le vrai scan.
        /// </summary>
        private async void StartInventory()
        {
            if (IsScanning) return;

            _cancellation = new CancellationTokenSource();
            IsScanning = true;
            ProgressPercent = 0;
            ProgressText = "Préparation (simulation de l'étape 1)…";

            long inventoryId = 0;
            try
            {
                inventoryId = _store.Create(0, AppVersion);
                var steps = SimulationSteps();

                for (var i = 0; i < steps.Count; i++)
                {
                    await Task.Delay(220, _cancellation.Token);
                    ProgressPercent = (i + 1) * 100.0 / steps.Count;
                    ProgressText = steps[i];
                }

                _store.Close(inventoryId, InventoryStatus.Cancelled, 0, 0, 0, 0,
                    "Simulation de l'étape 1 : aucun disque n'a été parcouru.");
            }
            catch (OperationCanceledException)
            {
                if (inventoryId != 0)
                {
                    _store.Close(inventoryId, InventoryStatus.Cancelled, 0, 0, 0, 0,
                        "Annulé par l'utilisateur (simulation de l'étape 1).");
                }
            }
            catch (Exception ex)
            {
                if (inventoryId != 0)
                {
                    _store.Close(inventoryId, InventoryStatus.Cancelled, 0, 0, 0, 0,
                        "Échec de la simulation : " + ex.Message);
                }

                Dialogs.Error("Inventaire", "L'inventaire simulé n'a pas pu se terminer.\n\n" + ex.Message);
            }
            finally
            {
                if (_cancellation != null)
                {
                    _cancellation.Dispose();
                    _cancellation = null;
                }

                ProgressPercent = 0;
                ProgressText = null;
                IsScanning = false;
                LoadInventories();
            }
        }

        private void CancelScan()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                ProgressText = "Annulation…";
                _cancellation.Cancel();
            }
        }

        private void DeleteInventory(object parameter)
        {
            var inventory = parameter as Inventory;
            if (inventory == null) return;
            if (!Dialogs.ConfirmDeleteInventory(inventory)) return;

            try
            {
                _store.Delete(inventory.Id);
                LoadInventories();
            }
            catch (Exception ex)
            {
                Dialogs.Error("Suppression", "L'inventaire n'a pas pu être supprimé.\n\n" + ex.Message);
            }
        }

        private void LoadInventories()
        {
            try
            {
                var list = _store.List(InventoryListLimit);

                Inventories.Clear();
                foreach (var inventory in list) Inventories.Add(inventory);

                LatestInventory = Inventories.Count > 0 ? Inventories[0] : null;
                OnPropertyChanged("DatabaseSizeText");
            }
            catch (Exception ex)
            {
                Dialogs.Error("Base de données", "Les inventaires n'ont pas pu être lus.\n\n" + ex.Message);
            }
        }

        private static string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version == null ? "0.1.0" : version.ToString(3);
            }
        }

        private static List<string> SimulationSteps()
        {
            return new List<string>
            {
                "Simulation — lecture du matériel (aucun accès disque)",
                "Simulation — lecture du registre des logiciels",
                "Simulation — détection des outils de développement",
                "Simulation — parcours des fichiers",
                "Simulation — écriture des lots",
                "Simulation — clôture de l'inventaire",
                "Simulation — aucun disque n'a été parcouru"
            };
        }

        private ObservableCollection<NavigationItem> BuildSections()
        {
            return new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Tableau de bord", "IconDashboard", this),
                new NavigationItem("Ordinateur", "IconComputer",
                    new PlaceholderViewModel("Ordinateur", "Étape 2",
                        "Fabricant, modèle, processeur, mémoire, carte graphique, BIOS, Windows, " +
                        "disques et volumes seront collectés ici.")),
                new NavigationItem("Fichiers", "IconFolder",
                    new PlaceholderViewModel("Fichiers", "Étapes 3A et 3B",
                        "L'explorateur et la recherche par nom ouvriront ici : arbre des dossiers, " +
                        "filtres, chemin complet, ouverture d'un élément.")),
                new NavigationItem("Logiciels", "IconSoftware",
                    new PlaceholderViewModel("Logiciels", "Étape 4",
                        "Les logiciels déclarés au registre seront listés ici : nom, version, " +
                        "éditeur, emplacement, date d'installation.")),
                new NavigationItem("Outils", "IconTools",
                    new PlaceholderViewModel("Outils", "Étape 4",
                        "Python, pip, Node.js, npm, Git et Tesseract seront détectés ici, " +
                        "avec leur chemin, leur version et leur statut.")),
                new NavigationItem("Développement", "IconCode",
                    new PlaceholderViewModel("Développement", "Étape 4",
                        "Les projets Python et Node.js seront listés ici, avec l'indicateur de " +
                        "dépôt Git. Aucune analyse de dépendances.")),
                new NavigationItem("Évolution", "IconChart",
                    new PlaceholderViewModel("Évolution", "Étape 5",
                        "La comparaison de deux inventaires arrivera ici : ajoutés, supprimés, " +
                        "modifiés, espace occupé."))
            };
        }
    }
}
