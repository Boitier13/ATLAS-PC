using System;

namespace AtlasPc.Models
{
    /// <summary>Une ligne de <c>scan_root</c> : l'état d'un disque dans un inventaire.</summary>
    public sealed class ScanRootInfo
    {
        public long Id { get; set; }
        public long InventoryId { get; set; }
        public string Path { get; set; }
        public string Drive { get; set; }
        public string Label { get; set; }
        public string Status { get; set; }
        public long FilesCount { get; set; }
        public long FoldersCount { get; set; }
        public long TotalBytes { get; set; }
        public long IssuesCount { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? FinishedAtUtc { get; set; }
        public string ErrorMessage { get; set; }

        public string StatusText
        {
            get { return InventoryStatus.Label(Status); }
        }

        /// <summary>
        /// Vrai quand le disque n'a pas été analysé jusqu'au bout : une comparaison ne
        /// peut alors en déduire aucune suppression.
        /// </summary>
        public bool BlocksComparisons
        {
            get
            {
                return Status == InventoryStatus.Cancelled ||
                       Status == InventoryStatus.Inaccessible ||
                       Status == InventoryStatus.Running;
            }
        }
    }

    /// <summary>Une ligne de <c>folder</c>, telle que la base la stocke.</summary>
    public sealed class ScanFolderRow
    {
        public string ParentPath { get; set; }
        public string Path { get; set; }

        /// <summary>Somme récursive des tailles des fichiers du sous-arbre.</summary>
        public long SizeBytes { get; set; }

        /// <summary>Nombre de fichiers <b>directs</b> (pas les descendants).</summary>
        public int FilesCount { get; set; }

        /// <summary>Nombre de sous-dossiers <b>directs</b> (pas les descendants).</summary>
        public int DirsCount { get; set; }

        public DateTime? ModifiedAtUtc { get; set; }
    }
}
