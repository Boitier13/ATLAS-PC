using System;
using System.Globalization;

namespace AtlasPc.Models
{
    /// <summary>
    /// Matériel et Windows d'un inventaire : une ligne de la table <c>computer</c>.
    /// Une valeur absente reste <c>null</c> et s'affiche « — » : jamais 0, jamais une
    /// valeur déduite d'une autre mesure.
    /// </summary>
    public sealed class ComputerInfo
    {
        private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        public long InventoryId { get; set; }

        public string Manufacturer { get; set; }
        public string Model { get; set; }

        public string CpuName { get; set; }
        public int? CpuCores { get; set; }
        public int? CpuThreads { get; set; }

        /// <summary>
        /// Mémoire physique installée : somme des capacités des modules
        /// (<c>Win32_PhysicalMemory.Capacity</c>). C'est la valeur que Windows appelle
        /// « mémoire physique installée » — 6,00 Go sur une machine de 6 Go.
        /// </summary>
        public long? RamBytes { get; set; }

        /// <summary>
        /// Mémoire utilisable par Windows (<c>Win32_ComputerSystem.TotalPhysicalMemory</c>).
        /// Notion distincte de <see cref="RamBytes"/> : la réserve matérielle et le circuit
        /// graphique intégré ne sont pas comptés dedans — 5,84 Go sur une machine de 6 Go.
        /// </summary>
        public long? RamUsableBytes { get; set; }

        public string GpuName { get; set; }

        public string BiosVendor { get; set; }
        public string BiosVersion { get; set; }

        public string OsName { get; set; }
        public string OsVersion { get; set; }
        public string OsBuild { get; set; }
        public string OsArch { get; set; }

        /// <summary>Date d'installation de Windows, en UTC. Voir la note de la vue.</summary>
        public DateTime? OsInstallDateUtc { get; set; }

        // --- Affichage --------------------------------------------------------

        public string IdentityValue
        {
            get
            {
                var manufacturer = Clean(Manufacturer);
                var model = Clean(Model);
                if (manufacturer != null && model != null)
                {
                    if (model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase)) return model;
                    return manufacturer + " " + model;
                }

                return manufacturer ?? model ?? "—";
            }
        }

        public string ModelValue
        {
            get { return Clean(Model) ?? "—"; }
        }

        public string ManufacturerValue
        {
            get { return Clean(Manufacturer) ?? "—"; }
        }

        public string CpuValue
        {
            get { return Clean(CpuName) ?? "—"; }
        }

        public string CpuHint
        {
            get
            {
                string cores = null;
                if (CpuCores.HasValue) cores = CpuCores.Value + (CpuCores.Value > 1 ? " cœurs" : " cœur");

                string threads = null;
                if (CpuThreads.HasValue) threads = CpuThreads.Value + " threads";

                if (cores != null && threads != null) return cores + " · " + threads;
                if (cores != null) return cores;
                if (threads != null) return threads;
                return "cœurs non lus";
            }
        }

        public string GpuValue
        {
            get { return Clean(GpuName) ?? "—"; }
        }

        public string GpuHint
        {
            get
            {
                var gpu = Clean(GpuName);
                if (gpu == null) return "carte graphique non lue";

                var count = 1;
                for (var i = 0; i < gpu.Length - 2; i++)
                {
                    if (gpu[i] == ' ' && gpu[i + 1] == '·' && gpu[i + 2] == ' ') count++;
                }

                return count > 1 ? count + " cartes graphiques" : "1 carte graphique";
            }
        }

        public string WindowsValue
        {
            get { return Clean(OsName) ?? "—"; }
        }

        public string WindowsHint
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>(3);

                var version = Clean(OsVersion);
                if (version != null) parts.Add(version);

                var build = Clean(OsBuild);
                if (build != null) parts.Add("build " + build);

                var arch = Clean(OsArch);
                if (arch != null) parts.Add(arch);

                return parts.Count == 0 ? "version non lue" : string.Join(" · ", parts);
            }
        }

        /// <summary>Mémoire installée, seule valeur affichée comme « la mémoire ».</summary>
        public string RamValue
        {
            get { return FormatMemory(RamBytes); }
        }

        /// <summary>
        /// Mémoire utilisable, affichée à part. Une valeur absente est écrite comme telle :
        /// jamais de substitution d'une notion par l'autre.
        /// </summary>
        public string RamHint
        {
            get
            {
                var usable = RamUsableBytes.HasValue && RamUsableBytes.Value > 0
                    ? FormatMemory(RamUsableBytes)
                    : null;

                if (!RamBytes.HasValue && usable == null) return "modules mémoire non lus";
                if (!RamBytes.HasValue) return usable + " utilisables · modules mémoire non lus";
                if (usable == null) return "mémoire utilisable non lue";
                return "dont " + usable + " utilisables";
            }
        }

        public string BiosValue
        {
            get { return Clean(BiosVersion) ?? "—"; }
        }

        public string BiosHint
        {
            get
            {
                var vendor = Clean(BiosVendor);
                return vendor == null ? "éditeur non lu" : vendor;
            }
        }

        public string InstallText
        {
            get
            {
                if (!OsInstallDateUtc.HasValue || OsInstallDateUtc.Value == DateTime.MinValue) return "—";
                return OsInstallDateUtc.Value.ToLocalTime().ToString("dd/MM/yyyy", French);
            }
        }

        public static string FormatMemory(long? bytes)
        {
            if (!bytes.HasValue || bytes.Value <= 0) return "—";
            return Inventory.FormatBytes(bytes.Value);
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
