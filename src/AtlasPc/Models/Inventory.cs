using System;
using System.Globalization;

namespace AtlasPc.Models
{
    /// <summary>Statuts d'un inventaire et d'un disque analysé.</summary>
    public static class InventoryStatus
    {
        public const string Running = "running";
        public const string Complete = "complete";
        public const string Partial = "partial";
        public const string Cancelled = "cancelled";
        public const string Inaccessible = "inaccessible";

        public static string Label(string status)
        {
            switch (status)
            {
                case Complete: return "Complet";
                case Partial: return "Partiel";
                case Cancelled: return "Annulé";
                case Inaccessible: return "Inaccessible";
                case Running: return "En cours";
                default: return "—";
            }
        }
    }

    /// <summary>Un inventaire daté, tel que la base le stocke.</summary>
    public sealed class Inventory
    {
        private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        public long Id { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? FinishedAtUtc { get; set; }
        public string Status { get; set; }
        public string AppVersion { get; set; }
        public int RootsCount { get; set; }
        public long FilesCount { get; set; }
        public long FoldersCount { get; set; }
        public long TotalBytes { get; set; }
        public long IssuesCount { get; set; }
        public string Note { get; set; }

        public string StartedText
        {
            get
            {
                return StartedAtUtc == DateTime.MinValue
                    ? "—"
                    : StartedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", French);
            }
        }

        public string StatusText
        {
            get { return InventoryStatus.Label(Status); }
        }

        public string RootsText
        {
            get
            {
                if (RootsCount <= 0) return "aucun";
                return RootsCount == 1 ? "1 disque" : RootsCount.ToString(French) + " disques";
            }
        }

        public string FilesText
        {
            get { return FilesCount.ToString("N0", French); }
        }

        public string SizeText
        {
            get { return FormatBytes(TotalBytes); }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "—";
            if (bytes < 1024) return bytes.ToString(French) + " o";
            if (bytes < 1024L * 1024L) return (bytes / 1024.0).ToString("0.#", French) + " Ko";
            if (bytes < 1024L * 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.#", French) + " Mo";
            if (bytes < 1024L * 1024L * 1024L * 1024L) return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.#", French) + " Go";
            return (bytes / (1024.0 * 1024.0 * 1024.0 * 1024.0)).ToString("0.#", French) + " To";
        }
    }
}
