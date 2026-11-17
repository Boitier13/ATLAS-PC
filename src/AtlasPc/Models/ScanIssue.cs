namespace AtlasPc.Models
{
    /// <summary>
    /// Natures d'incident d'un parcours. Les valeurs sont celles du CHECK de
    /// <c>scan_issue.kind</c> : en ajouter une obligerait à modifier le schéma.
    /// </summary>
    public static class ScanIssueKind
    {
        public const string AccessDenied = "access_denied";
        public const string IoError = "io_error";
        public const string PathTooLong = "path_too_long";
        public const string CancelledBefore = "cancelled_before";
        public const string Unexpected = "unexpected";

        public static string Label(string kind)
        {
            switch (kind)
            {
                case AccessDenied: return "Accès refusé";
                case IoError: return "Erreur de lecture";
                case PathTooLong: return "Chemin trop long";
                case CancelledBefore: return "Non analysé (annulation)";
                case Unexpected: return "Erreur inattendue";
                default: return "Incident";
            }
        }
    }

    /// <summary>
    /// Une zone non analysée : un dossier qui a refusé la lecture, ou la zone où le
    /// parcours s'est arrêté. Une ligne par dossier, jamais par fichier.
    /// </summary>
    public sealed class ScanIssue
    {
        public long Id { get; set; }
        public long InventoryId { get; set; }

        /// <summary>Lettre du disque (« C: »), pour retrouver la racine concernée.</summary>
        public string RootDrive { get; set; }

        /// <summary>Dossier concerné. C'est lui, et tout son sous-arbre, qui sont protégés.</summary>
        public string Path { get; set; }

        public string Kind { get; set; }
        public int? Code { get; set; }
        public string Message { get; set; }

        public string KindText
        {
            get { return ScanIssueKind.Label(Kind); }
        }
    }
}
