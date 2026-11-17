using System;
using System.Globalization;

namespace AtlasPc.Models
{
    /// <summary>
    /// Un volume fixe et le disque physique qui le porte : une ligne de la table
    /// <c>volume</c>. Les trois colonnes <c>disk_*</c> restent <c>null</c> quand le
    /// rattachement n'a pas pu être lu (Storage Spaces, disque virtuel, association
    /// indisponible) : la vue affiche alors « — ».
    /// </summary>
    public sealed class VolumeInfo
    {
        private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        public long Id { get; set; }
        public long InventoryId { get; set; }

        public string Drive { get; set; }
        public string Label { get; set; }
        public string FileSystem { get; set; }
        public long? TotalBytes { get; set; }
        public long? FreeBytes { get; set; }

        public int? DiskIndex { get; set; }
        public string DiskModel { get; set; }
        public long? DiskSizeBytes { get; set; }

        // --- Affichage --------------------------------------------------------

        public string DriveText
        {
            get
            {
                var drive = string.IsNullOrWhiteSpace(Drive) ? "—" : Drive.Trim();
                var label = string.IsNullOrWhiteSpace(Label) ? null : Label.Trim();
                return label == null ? drive : drive + "  " + label;
            }
        }

        public string FileSystemText
        {
            get { return string.IsNullOrWhiteSpace(FileSystem) ? "—" : FileSystem.Trim(); }
        }

        public string CapacityText
        {
            get { return TotalBytes.HasValue && TotalBytes.Value > 0 ? Inventory.FormatBytes(TotalBytes.Value) : "—"; }
        }

        public string FreeText
        {
            get { return FreeBytes.HasValue ? Inventory.FormatBytes(FreeBytes.Value) : "—"; }
        }

        public string DiskText
        {
            get { return string.IsNullOrWhiteSpace(DiskModel) ? "—" : DiskModel.Trim(); }
        }

        public string DiskHint
        {
            get
            {
                if (!DiskSizeBytes.HasValue) return "disque physique non identifié";
                var index = DiskIndex.HasValue ? "Disque " + (DiskIndex.Value + 1) + " · " : string.Empty;
                return index + Inventory.FormatBytes(DiskSizeBytes.Value);
            }
        }

        /// <summary>Taux occupé. <c>null</c> si la capacité n'a pas été lue : on n'invente rien.</summary>
        public double? PercentUsed
        {
            get
            {
                if (!TotalBytes.HasValue || TotalBytes.Value <= 0 || !FreeBytes.HasValue) return null;
                if (FreeBytes.Value < 0) return null;

                var used = TotalBytes.Value - FreeBytes.Value;
                if (used < 0) used = 0;
                if (used > TotalBytes.Value) used = TotalBytes.Value;

                return used * 100.0 / TotalBytes.Value;
            }
        }

        public double PercentValue
        {
            get { return PercentUsed.HasValue ? PercentUsed.Value : 0.0; }
        }

        public bool HasPercent
        {
            get { return PercentUsed.HasValue; }
        }

        public string PercentText
        {
            get { return PercentUsed.HasValue ? PercentUsed.Value.ToString("0", French) + " %" : "—"; }
        }

        /// <summary>
        /// Niveau d'occupation pour la couleur de la barre :
        /// <c>low</c> (&lt; 70 %), <c>medium</c> (70–85 %), <c>high</c> (&gt; 85 %).
        /// </summary>
        public string PercentLevel
        {
            get
            {
                if (!PercentUsed.HasValue) return "unknown";
                if (PercentUsed.Value < 70.0) return "low";
                if (PercentUsed.Value <= 85.0) return "medium";
                return "high";
            }
        }
    }
}
