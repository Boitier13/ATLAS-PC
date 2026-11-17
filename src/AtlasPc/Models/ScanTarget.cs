using System;

namespace AtlasPc.Models
{
    /// <summary>Un disque à parcourir : ce que l'utilisateur a coché.</summary>
    public sealed class ScanTarget
    {
        /// <summary>Lettre du disque, « C: » — la clé de <c>scan_root</c>.</summary>
        public string Drive { get; set; }

        /// <summary>Chemin racine, « C:\ ».</summary>
        public string Path { get; set; }

        public string Label { get; set; }

        public string DisplayText
        {
            get { return string.IsNullOrEmpty(Label) ? Drive : Drive + " — " + Label; }
        }
    }

    /// <summary>Résultat du parcours d'une racine, statut compris.</summary>
    public sealed class RootScanResult
    {
        public string Drive { get; set; }
        public string Path { get; set; }
        public string Status { get; set; }
        public long Files { get; set; }
        public long Folders { get; set; }
        public long Bytes { get; set; }
        public long Issues { get; set; }
        public long IgnoredLinks { get; set; }
        public string StatusText
        {
            get { return InventoryStatus.Label(Status); }
        }
    }

    /// <summary>Résultat complet d'un parcours : une entrée par racine demandée.</summary>
    public sealed class ScanResult
    {
        public System.Collections.Generic.List<RootScanResult> Roots { get; set; }
        public long Files { get; set; }
        public long Folders { get; set; }
        public long Bytes { get; set; }
        public long Issues { get; set; }
        public long IgnoredLinks { get; set; }

        /// <summary>Vrai si l'utilisateur a interrompu le parcours.</summary>
        public bool WasCancelled { get; set; }

        public ScanResult()
        {
            Roots = new System.Collections.Generic.List<RootScanResult>();
        }
    }

    /// <summary>
    /// Photo des compteurs à un instant donné, publiée pendant le parcours.
    /// Tous les chiffres sont réels : aucun pourcentage n'est inventé.
    /// </summary>
    public sealed class ScanSnapshot
    {
        public string Drive { get; set; }
        public string CurrentPath { get; set; }
        public int RootNumber { get; set; }
        public int RootCount { get; set; }
        public long Files { get; set; }
        public long Folders { get; set; }
        public long Bytes { get; set; }
        public long Issues { get; set; }
        public long IgnoredLinks { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
