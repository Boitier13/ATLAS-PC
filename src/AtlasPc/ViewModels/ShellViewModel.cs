using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AtlasPc.Collecting;
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
        private readonly ComputerViewModel _computer;

        private ComputerInfo _latestComputer;
        private List<VolumeInfo> _latestVolumes;

        private NavigationItem _selectedSection;
        private Inventory _latestInventory;
        private bool _isScanning;
        private bool _isProgressIndeterminate;
        private double _progressPercent;
        private string _progressText;

        public ShellViewModel(InventoryStore store, Database database)
        {
            if (store == null) throw new ArgumentNullException("store");
            if (database == null) throw new ArgumentNullException("database");
            _store = store;
            _database = database;

            Inventories = new ObservableCollection<Inventory>();
            _computer = new ComputerViewModel();
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

        /// <summary>
        /// Vrai pendant la collecte matérielle : la durée d'une requête WMI n'est pas
        /// connue à l'avance, la barre ne peut donc pas afficher un pourcentage honnête.
        /// </summary>
        public bool IsProgressIndeterminate
        {
            get { return _isProgressIndeterminate; }
            private set
            {
                if (Set(ref _isProgressIndeterminate, value))
                    OnPropertyChanged("IsProgressDeterminate");
            }
        }

        public bool IsProgressDeterminate
        {
            get { return !_isProgressIndeterminate; }
        }

        // --- Résumé matériel (tableau de bord) --------------------------------

        public string CpuText
        {
            get { return _latestComputer == null ? "—" : _latestComputer.CpuValue; }
        }

        public string CpuHint
        {
            get { return _latestComputer == null ? "aucun inventaire" : _latestComputer.CpuHint; }
        }

        public string RamText
        {
            get { return _latestComputer == null ? "—" : _latestComputer.RamValue; }
        }

        public string RamHint
        {
            get { return _latestComputer == null ? "aucun inventaire" : _latestComputer.RamHint; }
        }

        public string StorageText
        {
            get
            {
                if (_latestVolumes == null || _latestVolumes.Count == 0) return "—";

                long total = 0;
                foreach (var volume in _latestVolumes)
                {
                    if (volume.TotalBytes.HasValue && volume.TotalBytes.Value > 0) total += volume.TotalBytes.Value;
                }

                return total > 0 ? Inventory.FormatBytes(total) : "—";
            }
        }

        public string StorageHint
        {
            get
            {
                if (_latestVolumes == null || _latestVolumes.Count == 0) return "aucun volume lu";

                long free = 0;
                foreach (var volume in _latestVolumes)
                {
                    if (volume.FreeBytes.HasValue && volume.FreeBytes.Value > 0) free += volume.FreeBytes.Value;
                }

                var count = _latestVolumes.Count == 1 ? "1 volume" : _latestVolumes.Count + " volumes";
                return free > 0 ? count + " · " + Inventory.FormatBytes(free) + " libres" : count;
            }
        }

        // --- Commandes --------------------------------------------------------

        public ICommand StartInventoryCommand { get; private set; }
        public ICommand CancelScanCommand { get; private set; }
        public ICommand DeleteInventoryCommand { get; private set; }

        private CancellationTokenSource _cancellation;

        /// <summary>
        /// Étape 2 : collecte réellement le matériel et Windows, puis clôt l'inventaire en
        /// <c>partial</c> — les fichiers ne sont pas inventoriés (étape 3A). Aucun disque
        /// n'est parcouru ici, et la note de l'inventaire le dit.
        ///
        /// La collecte WMI tourne hors du thread d'interface ; garde-fou global de 35 s,
        /// appels eux-mêmes bornés à 10 s et budget interne de 30 s.
        /// </summary>
        private async void StartInventory()
        {
            if (IsScanning) return;

            _cancellation = new CancellationTokenSource();
            IsScanning = true;
            IsProgressIndeterminate = true;
            ProgressPercent = 0;
            ProgressText = "Lecture du matériel et de Windows…";

            long inventoryId = 0;
            try
            {
                inventoryId = _store.Create(0, AppVersion);

                var collectTask = Task.Run(() => ComputerCollector.Collect());
                var guard = Task.Delay(TimeSpan.FromSeconds(35));
                var finished = await Task.WhenAny(collectTask, guard);

                if (_cancellation.IsCancellationRequested)
                {
                    Forget(collectTask);
                    _store.Close(inventoryId, InventoryStatus.Cancelled, 0, 0, 0, 0,
                        "Collecte annulée par l'utilisateur. Aucun fichier n'a été inventorié (étape 3A).");
                    return;
                }

                if (finished != collectTask)
                {
                    // Le service WMI ne répond pas : on cesse d'attendre et on le dit.
                    Forget(collectTask);
                    _store.Close(inventoryId, InventoryStatus.Partial, 0, 0, 0, 1,
                        "Collecte abandonnée après 35 s : WMI n'a pas répondu. " +
                        "Aucun fichier n'a été inventorié (étape 3A).");
                    return;
                }

                var collected = await collectTask;

                ProgressText = "Enregistrement du matériel…";
                _store.SaveHardware(inventoryId, collected.Computer, collected.Volumes);
                _store.Close(inventoryId, InventoryStatus.Partial, 0, 0, 0,
                    collected.Issues.Count, BuildNote(collected));
            }
            catch (Exception ex)
            {
                if (inventoryId != 0)
                {
                    try
                    {
                        _store.Close(inventoryId, InventoryStatus.Partial, 0, 0, 0, 1,
                            "Collecte interrompue par une erreur : " + ex.Message);
                    }
                    catch (Exception)
                    {
                        // La clôture en échec ne doit pas masquer l'erreur d'origine.
                    }
                }

                Dialogs.Error("Inventaire", "La collecte du matériel n'a pas pu se terminer.\n\n" + ex.Message);
            }
            finally
            {
                if (_cancellation != null)
                {
                    _cancellation.Dispose();
                    _cancellation = null;
                }

                IsProgressIndeterminate = false;
                ProgressPercent = 0;
                ProgressText = null;
                IsScanning = false;
                LoadInventories();
            }
        }

        /// <summary>Observe une tâche abandonnée pour que son exception ne remonte pas.</summary>
        private static void Forget(Task task)
        {
            task.ContinueWith(t => GC.KeepAlive(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Note enregistrée dans l'inventaire : ce qui a été lu, ce qui a échoué, et ce qui
        /// n'a pas été fait. Elle reste visible après redémarrage.
        /// </summary>
        private static string BuildNote(CollectResult result)
        {
            var text = "Matériel et Windows : " + result.QueriesRun + "/" + result.QueriesTotal + " appels WMI";

            if (result.Issues.Count > 0)
            {
                text += ". Incidents : " + string.Join(" ; ", result.Issues.ToArray());
            }
            else
            {
                text += ", sans incident";
            }

            text += ". Fichiers non inventoriés : le parcours des disques arrive à l'étape 3A.";

            return text.Length > 900 ? text.Substring(0, 900) + "…" : text;
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

                _latestComputer = null;
                _latestVolumes = null;

                if (LatestInventory != null)
                {
                    _latestComputer = _store.GetComputer(LatestInventory.Id);
                    _latestVolumes = _store.GetVolumes(LatestInventory.Id);
                }

                _computer.Refresh(LatestInventory, _latestComputer, _latestVolumes);

                OnPropertyChanged(string.Empty);
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

        private ObservableCollection<NavigationItem> BuildSections()
        {
            return new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Tableau de bord", "IconDashboard", this),
                new NavigationItem("Ordinateur", "IconComputer", _computer),
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
