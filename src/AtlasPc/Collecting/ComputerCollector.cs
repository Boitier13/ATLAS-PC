using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using AtlasPc.Models;
using Microsoft.Win32;

namespace AtlasPc.Collecting
{
    /// <summary>Résultat d'une collecte matérielle : ce qui a été lu et ce qui a échoué.</summary>
    public sealed class CollectResult
    {
        public ComputerInfo Computer { get; set; }
        public List<VolumeInfo> Volumes { get; set; }
        public List<string> Issues { get; set; }
        public int QueriesRun { get; set; }
        public int QueriesTotal { get; set; }
    }

    /// <summary>
    /// Lecture du matériel et de Windows, en lecture seule, sans droit administrateur.
    ///
    /// Neuf appels WMI au total — sept classes plus deux associations :
    /// <list type="number">
    ///   <item>Win32_ComputerSystem (fabricant, modèle, mémoire installée)</item>
    ///   <item>Win32_Processor (nom, cœurs, threads)</item>
    ///   <item>Win32_VideoController (cartes graphiques)</item>
    ///   <item>Win32_BIOS (éditeur, version)</item>
    ///   <item>Win32_OperatingSystem (nom, version, build, architecture, mémoire utilisable, date)</item>
    ///   <item>Win32_LogicalDisk, filtré DriveType = 3 (volumes fixes)</item>
    ///   <item>Win32_DiskDrive (disques physiques)</item>
    ///   <item>Win32_DiskDriveToDiskPartition (disque → partition)</item>
    ///   <item>Win32_LogicalDiskToPartition (partition → volume)</item>
    /// </list>
    ///
    /// Chaque appel est isolé : son échec devient une ligne de <c>Issues</c>, jamais une
    /// exception. Les huitième et neuvième appels servent uniquement au rattachement
    /// volume → disque physique ; s'ils échouent, les colonnes <c>disk_*</c> restent vides.
    /// </summary>
    public static class ComputerCollector
    {
        private const string WindowsKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        /// <summary>Délai laissé à chaque appel WMI (appliqué par EnumerationOptions).</summary>
        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);

        /// <summary>Budget des appels WMI : au-delà, les requêtes restantes ne sont pas lancées.</summary>
        private static readonly TimeSpan QueryBudget = TimeSpan.FromSeconds(30);

        private const int QueryCount = 9;

        /// <summary>
        /// Espace de noms WMI local. Passé sous forme de chaîne parce que
        /// <see cref="ManagementObjectSearcher"/> n'offre pas de constructeur
        /// <c>(ObjectQuery, EnumerationOptions)</c> : la surcharge réellement disponible
        /// est <c>(string scope, string queryString, EnumerationOptions options)</c>.
        /// </summary>
        private const string ScopePath = @"\\.\root\cimv2";

        public static CollectResult Collect()
        {
            var watch = Stopwatch.StartNew();
            var issues = new List<string>();
            var computer = new ComputerInfo();
            var volumes = new List<VolumeInfo>();

            var result = new CollectResult
            {
                Computer = computer,
                Volumes = volumes,
                Issues = issues,
                QueriesRun = 0,
                QueriesTotal = QueryCount
            };

            var gpuNames = new List<string>();
            var disks = new Dictionary<string, DiskRow>(StringComparer.OrdinalIgnoreCase);
            var partitionToDisk = new Dictionary<string, DiskRow>(StringComparer.OrdinalIgnoreCase);
            var volumesByDevice = new Dictionary<string, VolumeInfo>(StringComparer.OrdinalIgnoreCase);

            // 1. Ordinateur ------------------------------------------------------
            Run("Win32_ComputerSystem",
                "SELECT Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem",
                item =>
                {
                    if (computer.Manufacturer == null) computer.Manufacturer = Text(item, "Manufacturer");
                    if (computer.Model == null) computer.Model = Text(item, "Model");

                    var ram = Number(item, "TotalPhysicalMemory");
                    if (ram.HasValue && ram.Value > 0) computer.RamBytes = ram;
                },
                result, watch);

            // 2. Processeur ------------------------------------------------------
            Run("Win32_Processor",
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor",
                item =>
                {
                    if (computer.CpuName == null) computer.CpuName = Text(item, "Name");
                    if (!computer.CpuCores.HasValue) computer.CpuCores = Integer(item, "NumberOfCores");
                    if (!computer.CpuThreads.HasValue) computer.CpuThreads = Integer(item, "NumberOfLogicalProcessors");
                },
                result, watch);

            // 3. Cartes graphiques -----------------------------------------------
            Run("Win32_VideoController",
                "SELECT Name FROM Win32_VideoController",
                item =>
                {
                    var name = Text(item, "Name");
                    if (name != null && !gpuNames.Contains(name)) gpuNames.Add(name);
                },
                result, watch);

            // 4. BIOS ------------------------------------------------------------
            Run("Win32_BIOS",
                "SELECT Manufacturer, SMBIOSBIOSVersion FROM Win32_BIOS",
                item =>
                {
                    if (computer.BiosVendor == null) computer.BiosVendor = Text(item, "Manufacturer");
                    if (computer.BiosVersion == null) computer.BiosVersion = Text(item, "SMBIOSBIOSVersion");
                },
                result, watch);

            // 5. Windows ---------------------------------------------------------
            DateTime? wmiInstallDate = null;
            Run("Win32_OperatingSystem",
                "SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate, TotalVisibleMemorySize " +
                "FROM Win32_OperatingSystem",
                item =>
                {
                    if (computer.OsName == null) computer.OsName = Text(item, "Caption");
                    if (computer.OsVersion == null) computer.OsVersion = Text(item, "Version");
                    if (computer.OsBuild == null) computer.OsBuild = Text(item, "BuildNumber");
                    if (computer.OsArch == null) computer.OsArch = Text(item, "OSArchitecture");

                    if (!computer.RamUsableBytes.HasValue)
                    {
                        // TotalVisibleMemorySize est exprimé en kilo-octets.
                        var visible = Number(item, "TotalVisibleMemorySize");
                        if (visible.HasValue && visible.Value > 0) computer.RamUsableBytes = visible.Value * 1024L;
                    }

                    if (!wmiInstallDate.HasValue) wmiInstallDate = ParseCimDate(Text(item, "InstallDate"));
                },
                result, watch);

            // 6. Volumes fixes ---------------------------------------------------
            Run("Win32_LogicalDisk",
                "SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace FROM Win32_LogicalDisk " +
                "WHERE DriveType = 3",
                item =>
                {
                    var drive = Text(item, "DeviceID");
                    if (drive == null || volumesByDevice.ContainsKey(drive)) return;

                    var volume = new VolumeInfo
                    {
                        Drive = drive,
                        Label = Text(item, "VolumeName"),
                        FileSystem = Text(item, "FileSystem"),
                        TotalBytes = Positive(Number(item, "Size")),
                        FreeBytes = Number(item, "FreeSpace")
                    };

                    volumesByDevice.Add(drive, volume);
                    volumes.Add(volume);
                },
                result, watch);

            // 7. Disques physiques -----------------------------------------------
            Run("Win32_DiskDrive",
                "SELECT DeviceID, Index, Model, Size FROM Win32_DiskDrive",
                item =>
                {
                    var deviceId = Text(item, "DeviceID");
                    if (deviceId == null || disks.ContainsKey(deviceId)) return;

                    var row = new DiskRow
                    {
                        DeviceId = deviceId,
                        Index = Integer(item, "Index"),
                        Model = Text(item, "Model"),
                        SizeBytes = Positive(Number(item, "Size"))
                    };

                    disks.Add(deviceId, row);
                },
                result, watch);

            // 8. Disque → partition ----------------------------------------------
            Run("Win32_DiskDriveToDiskPartition",
                "SELECT Antecedent, Dependent FROM Win32_DiskDriveToDiskPartition",
                item =>
                {
                    var diskDevice = DeviceId(Text(item, "Antecedent"));
                    var partitionDevice = DeviceId(Text(item, "Dependent"));
                    if (diskDevice == null || partitionDevice == null) return;

                    DiskRow disk;
                    if (disks.TryGetValue(diskDevice, out disk) && !partitionToDisk.ContainsKey(partitionDevice))
                        partitionToDisk.Add(partitionDevice, disk);
                },
                result, watch);

            // 9. Partition → volume ----------------------------------------------
            Run("Win32_LogicalDiskToPartition",
                "SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition",
                item =>
                {
                    var partitionDevice = DeviceId(Text(item, "Antecedent"));
                    var driveDevice = DeviceId(Text(item, "Dependent"));
                    if (partitionDevice == null || driveDevice == null) return;

                    DiskRow disk;
                    VolumeInfo volume;
                    if (partitionToDisk.TryGetValue(partitionDevice, out disk) &&
                        volumesByDevice.TryGetValue(driveDevice, out volume))
                    {
                        volume.DiskIndex = disk.Index;
                        volume.DiskModel = disk.Model;
                        volume.DiskSizeBytes = disk.SizeBytes;
                    }
                },
                result, watch);

            // Cartes graphiques : noms concaténés, dans l'ordre renvoyé par WMI.
            if (gpuNames.Count > 0) computer.GpuName = string.Join(" · ", gpuNames.ToArray());

            // Windows : le registre corrige le nom, la version d'affichage et le build.
            ApplyRegistryFacts(computer, result);

            // Date d'installation : le registre donne l'installation d'origine ;
            // WMI donne celle de l'image courante (dernière installation ou mise à niveau).
            var registryDate = ReadRegistryInstallDate(issues);
            if (registryDate.HasValue)
            {
                computer.OsInstallDateUtc = registryDate;
            }
            else if (wmiInstallDate.HasValue)
            {
                computer.OsInstallDateUtc = wmiInstallDate;
                issues.Add("Date d'installation : fournie par WMI (dernière installation ou mise à niveau), " +
                           "le registre ne contenait pas de valeur exploitable");
            }

            volumes.Sort(delegate(VolumeInfo a, VolumeInfo b)
            {
                return string.Compare(a.Drive, b.Drive, StringComparison.OrdinalIgnoreCase);
            });

            return result;
        }

        // --- Exécution des requêtes ---------------------------------------------

        private static void Run(string label, string wql, Action<ManagementBaseObject> read,
                                CollectResult result, Stopwatch watch)
        {
            if (watch.Elapsed > QueryBudget)
            {
                result.Issues.Add(label + " : non exécutée, budget de 30 s atteint");
                return;
            }

            try
            {
                var options = new EnumerationOptions
                {
                    Timeout = QueryTimeout,
                    ReturnImmediately = true
                };

                // Surcharge (scope, requête, options) : la seule des trois qui existe.
                using (var searcher = new ManagementObjectSearcher(ScopePath, wql, options))
                using (var items = searcher.Get())
                {
                    foreach (ManagementBaseObject item in items)
                    {
                        try
                        {
                            read(item);
                        }
                        catch (Exception)
                        {
                            // Une propriété illisible ne doit pas faire perdre l'objet entier.
                        }
                    }
                }

                result.QueriesRun++;
            }
            catch (ManagementException ex)
            {
                result.Issues.Add(label + " : " + Clean(ex.Message));
            }
            catch (COMException ex)
            {
                result.Issues.Add(label + " : " + Clean(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                result.Issues.Add(label + " : accès refusé par WMI");
            }
            catch (Exception ex)
            {
                result.Issues.Add(label + " : " + Clean(ex.Message));
            }
        }

        // --- Registre ------------------------------------------------------------

        private static void ApplyRegistryFacts(ComputerInfo computer, CollectResult result)
        {
            string productName = null;
            string displayVersion = null;
            string currentBuild = null;
            var ubr = 0;

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(WindowsKeyPath, false))
                {
                    if (key == null)
                    {
                        result.Issues.Add("Registre : clé CurrentVersion introuvable, nom de Windows non corrigé");
                        return;
                    }

                    productName = AsText(key.GetValue("ProductName"));
                    displayVersion = AsText(key.GetValue("DisplayVersion"));
                    currentBuild = AsText(key.GetValue("CurrentBuild"));
                    ubr = ReadUbr(key.GetValue("UBR"));
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add("Registre : " + Clean(ex.Message));
                return;
            }

            if (productName != null)
            {
                // Depuis Windows 11, ProductName peut encore annoncer « Windows 10 » :
                // c'est le numéro de build qui tranche (22000 et au-delà).
                var build = ParseBuild(currentBuild) ?? ParseBuild(computer.OsBuild);
                if (build.HasValue && build.Value >= 22000)
                {
                    if (productName.IndexOf("Windows 10", StringComparison.OrdinalIgnoreCase) >= 0)
                        productName = Replace(productName, "Windows 10", "Windows 11");
                    else if (productName.IndexOf("Windows 11", StringComparison.OrdinalIgnoreCase) < 0)
                        productName = productName + " (Windows 11)";
                }

                computer.OsName = productName;
            }

            if (displayVersion != null) computer.OsVersion = displayVersion;

            if (currentBuild != null)
            {
                computer.OsBuild = ubr > 0
                    ? currentBuild + "." + ubr.ToString(CultureInfo.InvariantCulture)
                    : currentBuild;
            }
        }

        private static DateTime? ReadRegistryInstallDate(List<string> issues)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(WindowsKeyPath, false))
                {
                    if (key == null) return null;

                    var raw = key.GetValue("InstallDate");
                    if (raw == null) return null;

                    var seconds = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
                    if (seconds <= 0) return null;

                    // La valeur est un DWORD : au-delà de 2038 elle se relit négative.
                    var unsigned = unchecked((uint)seconds);
                    return Sanitize(DateTimeOffset.FromUnixTimeSeconds((long)unsigned).UtcDateTime);
                }
            }
            catch (Exception ex)
            {
                issues.Add("Registre : date d'installation illisible (" + Clean(ex.Message) + ")");
                return null;
            }
        }

        private static int ReadUbr(object value)
        {
            if (value == null) return 0;

            try
            {
                var number = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                if (number <= 0 || number > int.MaxValue) return 0;
                return (int)number;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static string AsText(object value)
        {
            if (value == null) return null;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string Replace(string text, string oldValue, string newValue)
        {
            var index = text.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return text;
            return text.Substring(0, index) + newValue + text.Substring(index + oldValue.Length);
        }

        private static int? ParseBuild(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var text = value.Trim();
            var dot = text.IndexOf('.');
            if (dot >= 0) text = text.Substring(0, dot);

            int build;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out build)) return build;
            return null;
        }

        // --- Conversions ---------------------------------------------------------

        private static string Text(ManagementBaseObject item, string property)
        {
            try
            {
                return AsText(item[property]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static long? Number(ManagementBaseObject item, string property)
        {
            try
            {
                var value = item[property];
                if (value == null) return null;

                var number = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return number;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static long? Positive(long? value)
        {
            return value.HasValue && value.Value > 0 ? value : null;
        }

        private static int? Integer(ManagementBaseObject item, string property)
        {
            var value = Number(item, property);
            if (!value.HasValue || value.Value <= 0 || value.Value > int.MaxValue) return null;
            return (int)value.Value;
        }

        /// <summary>
        /// Extrait <c>DeviceID</c> d'un chemin d'objet WMI, par exemple
        /// <c>Win32_DiskDrive.DeviceID="\\\\.\\PHYSICALDRIVE0"</c>. Sert de clé de
        /// rapprochement entre les deux associations.
        /// </summary>
        private static string DeviceId(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            const string marker = "DeviceID=\"";
            var start = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;

            start += marker.Length;
            var end = path.IndexOf('"', start);
            if (end <= start) return null;

            return path.Substring(start, end - start);
        }

        /// <summary>
        /// Convertit une date CIM (<c>20260115093000.000000+060</c>) en UTC.
        /// <c>ManagementDateTimeConverter</c> renvoie une heure locale.
        /// </summary>
        private static DateTime? ParseCimDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            try
            {
                var local = ManagementDateTimeConverter.ToDateTime(value);
                return Sanitize(DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DateTime? Sanitize(DateTime utc)
        {
            if (utc == DateTime.MinValue) return null;
            if (utc.Year < 2000) return null;
            if (utc > DateTime.UtcNow.AddDays(1)) return null;
            return utc;
        }

        private static string Clean(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "échec sans message";

            var builder = new StringBuilder(message.Length);
            foreach (var c in message.Trim())
            {
                builder.Append(char.IsControl(c) ? ' ' : c);
            }

            var text = builder.ToString();
            return text.Length > 140 ? text.Substring(0, 140) + "…" : text;
        }

        private sealed class DiskRow
        {
            public string DeviceId { get; set; }
            public int? Index { get; set; }
            public string Model { get; set; }
            public long? SizeBytes { get; set; }
        }
    }
}
