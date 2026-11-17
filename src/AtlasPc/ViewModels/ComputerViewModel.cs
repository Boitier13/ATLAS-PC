using System.Collections.Generic;
using System.Collections.ObjectModel;
using AtlasPc.Models;

namespace AtlasPc.ViewModels
{
    /// <summary>
    /// Rubrique <b>Ordinateur</b> : le matériel et les volumes du dernier inventaire.
    /// Les données sont déjà en base — cette vue ne relit jamais le matériel.
    /// </summary>
    public sealed class ComputerViewModel : ObservableObject
    {
        private Inventory _inventory;
        private ComputerInfo _computer;
        private string _volumesCaption = "aucun volume lu";
        private string _headerText = "Aucun inventaire";

        public ComputerViewModel()
        {
            Volumes = new ObservableCollection<VolumeInfo>();
        }

        public ObservableCollection<VolumeInfo> Volumes { get; private set; }

        /// <summary>Vrai dès qu'une collecte matérielle existe pour l'inventaire affiché.</summary>
        public bool HasData
        {
            get { return _computer != null; }
        }

        public bool IsEmpty
        {
            get { return _computer == null; }
        }

        public bool HasVolumes
        {
            get { return Volumes.Count > 0; }
        }

        public string HeaderText
        {
            get { return _headerText; }
        }

        /// <summary>
        /// Titre de l'état vide : « aucun inventaire » n'est pas la même chose qu'un
        /// inventaire sans fiche matérielle (collecte annulée ou échouée).
        /// </summary>
        public string EmptyTitle
        {
            get { return _inventory == null ? "Aucun inventaire" : "Matériel non collecté"; }
        }

        public string EmptyMessage
        {
            get
            {
                if (_inventory == null)
                {
                    return "Le bouton « Lancer un inventaire » lit le matériel et les volumes de cet " +
                           "ordinateur. Les fichiers ne sont pas encore parcourus : cela arrive à l'étape 3A.";
                }

                return "Cet inventaire n'a pas de fiche matérielle : la collecte a été annulée, a échoué " +
                       "ou a dépassé son délai. La note ci-dessous en donne la raison ; relancez un " +
                       "inventaire pour la remplir.";
            }
        }

        public string VolumesCaption
        {
            get { return _volumesCaption; }
        }

        // --- Cartes -----------------------------------------------------------

        public string IdentityValue
        {
            get { return _computer == null ? "—" : _computer.ModelValue; }
        }

        public string CpuValue
        {
            get { return _computer == null ? "—" : _computer.CpuValue; }
        }

        public string CpuHint
        {
            get { return _computer == null ? "aucun inventaire" : _computer.CpuHint; }
        }

        public string GpuValue
        {
            get { return _computer == null ? "—" : _computer.GpuValue; }
        }

        public string GpuHint
        {
            get { return _computer == null ? "aucun inventaire" : _computer.GpuHint; }
        }

        public string WindowsValue
        {
            get { return _computer == null ? "—" : _computer.WindowsValue; }
        }

        public string WindowsHint
        {
            get { return _computer == null ? "aucun inventaire" : _computer.WindowsHint; }
        }

        public string RamValue
        {
            get { return _computer == null ? "—" : _computer.RamValue; }
        }

        public string RamHint
        {
            get { return _computer == null ? "aucun inventaire" : _computer.RamHint; }
        }

        public string BiosValue
        {
            get { return _computer == null ? "—" : _computer.BiosValue; }
        }

        public string BiosHint
        {
            get { return _computer == null ? "aucun inventaire" : _computer.BiosHint; }
        }

        public string IdentityHint
        {
            get { return _computer == null ? "aucun inventaire" : _computer.ManufacturerValue; }
        }

        /// <summary>Ligne de détail : date d'installation et origine de cette date.</summary>
        public string DetailText
        {
            get
            {
                if (_computer == null) return string.Empty;
                return "Windows installé le " + _computer.InstallText +
                       " · date d'origine lue au registre ; à défaut, date rapportée par WMI " +
                       "(dernière installation ou mise à niveau)";
            }
        }

        // --- Note de collecte --------------------------------------------------

        public bool HasNote
        {
            get { return _inventory != null && !string.IsNullOrWhiteSpace(_inventory.Note); }
        }

        public string NoteText
        {
            get { return _inventory == null ? null : _inventory.Note; }
        }

        public string ExclusionsText
        {
            get
            {
                return "Volumes fixes uniquement (DriveType 3) : les disques amovibles, les lecteurs réseau, " +
                       "les CD-ROM et les partitions sans lettre ne sont pas inventoriés.";
            }
        }

        // --- Chargement --------------------------------------------------------

        /// <summary>
        /// Affiche un inventaire et son matériel. Les données viennent de la base :
        /// aucun accès WMI n'est refait ici.
        /// </summary>
        public void Refresh(Inventory inventory, ComputerInfo computer, IList<VolumeInfo> volumes)
        {
            _inventory = inventory;
            _computer = computer;

            Volumes.Clear();
            if (volumes != null)
            {
                foreach (var volume in volumes) Volumes.Add(volume);
            }

            _headerText = inventory == null
                ? "Aucun inventaire"
                : "Inventaire du " + inventory.StartedText + " · état : " + inventory.StatusText;

            _volumesCaption = BuildVolumesCaption(volumes);

            OnPropertyChanged(string.Empty);
        }

        /// <summary>Vide la vue : aucun inventaire en base.</summary>
        public void Clear()
        {
            Refresh(null, null, null);
        }

        private static string BuildVolumesCaption(IList<VolumeInfo> volumes)
        {
            if (volumes == null || volumes.Count == 0) return "aucun volume fixe n'a été lu";

            long total = 0;
            long free = 0;
            var counted = 0;

            foreach (var volume in volumes)
            {
                if (volume.TotalBytes.HasValue && volume.TotalBytes.Value > 0)
                {
                    total += volume.TotalBytes.Value;
                    counted++;
                }

                if (volume.FreeBytes.HasValue && volume.FreeBytes.Value > 0) free += volume.FreeBytes.Value;
            }

            var count = volumes.Count == 1 ? "1 volume fixe" : volumes.Count + " volumes fixes";
            if (counted == 0) return count + " · capacité totale non lue";

            var text = count + " · " + Inventory.FormatBytes(total) + " au total";
            if (free > 0) text += " · " + Inventory.FormatBytes(free) + " libres";
            return text;
        }
    }
}
