using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using AtlasPc.Models;

namespace AtlasPc.Data
{
    /// <summary>Créer, lire, clôturer et supprimer les inventaires.</summary>
    public sealed class InventoryStore
    {
        private const string SelectColumns =
            "id, started_at, finished_at, status, app_version, roots_count, " +
            "files_count, folders_count, total_bytes, issues_count, note";

        private readonly SQLiteConnection _connection;

        public InventoryStore(SQLiteConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            _connection = connection;
        }

        /// <summary>
        /// Crée un inventaire au statut <c>running</c> et renvoie son identifiant.
        /// </summary>
        public long Create(int rootsCount, string appVersion)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "INSERT INTO inventory (started_at, status, app_version, roots_count) " +
                    "VALUES (@startedAt, @status, @appVersion, @rootsCount);";
                command.Parameters.AddWithValue("@startedAt", SqlDate.ToDb(DateTime.UtcNow));
                command.Parameters.AddWithValue("@status", InventoryStatus.Running);
                command.Parameters.AddWithValue("@appVersion", appVersion ?? "0.0.0");
                command.Parameters.AddWithValue("@rootsCount", rootsCount);
                command.ExecuteNonQuery();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT last_insert_rowid();";
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Clôt un inventaire : statut final, compteurs et note éventuelle.</summary>
        public void Close(long inventoryId, string status, long files, long folders,
                          long totalBytes, long issues, string note)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "UPDATE inventory SET finished_at = @finishedAt, status = @status, " +
                    "files_count = @files, folders_count = @folders, total_bytes = @bytes, " +
                    "issues_count = @issues, note = @note WHERE id = @id;";
                command.Parameters.AddWithValue("@finishedAt", SqlDate.ToDb(DateTime.UtcNow));
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@files", files);
                command.Parameters.AddWithValue("@folders", folders);
                command.Parameters.AddWithValue("@bytes", totalBytes);
                command.Parameters.AddWithValue("@issues", issues);
                command.Parameters.AddWithValue("@note", (object)note ?? DBNull.Value);
                command.Parameters.AddWithValue("@id", inventoryId);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>Les inventaires, du plus récent au plus ancien.</summary>
        public List<Inventory> List(int limit)
        {
            var result = new List<Inventory>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT " + SelectColumns + " FROM inventory " +
                    "ORDER BY started_at DESC, id DESC LIMIT @limit;";
                command.Parameters.AddWithValue("@limit", limit <= 0 ? 50 : limit);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) result.Add(Read(reader));
                }
            }

            return result;
        }

        public Inventory Get(long inventoryId)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT " + SelectColumns + " FROM inventory WHERE id = @id;";
                command.Parameters.AddWithValue("@id", inventoryId);

                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? Read(reader) : null;
                }
            }
        }

        /// <summary>Supprime un inventaire ; les tables enfants suivent en cascade.</summary>
        public void Delete(long inventoryId)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM inventory WHERE id = @id;";
                command.Parameters.AddWithValue("@id", inventoryId);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Supprime les lignes restées au statut <c>running</c> : elles proviennent d'une
        /// session interrompue brutalement et ne contiennent rien d'exploitable.
        /// </summary>
        public int DeleteAbandoned()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM inventory WHERE status = @status;";
                command.Parameters.AddWithValue("@status", InventoryStatus.Running);
                return command.ExecuteNonQuery();
            }
        }

        // --- Matériel et volumes (étape 2) -------------------------------------

        /// <summary>
        /// Remplace le matériel et les volumes d'un inventaire, dans une transaction.
        /// Relancer une collecte sur le même inventaire ne crée donc jamais de doublon.
        /// </summary>
        public void SaveHardware(long inventoryId, ComputerInfo computer, IList<VolumeInfo> volumes)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                Execute("DELETE FROM computer WHERE inventory_id = @id;", transaction, inventoryId);
                Execute("DELETE FROM volume WHERE inventory_id = @id;", transaction, inventoryId);

                if (computer != null) InsertComputer(inventoryId, computer, transaction);

                if (volumes != null)
                {
                    foreach (var volume in volumes)
                    {
                        InsertVolume(inventoryId, volume, transaction);
                    }
                }

                transaction.Commit();
            }
        }

        /// <summary>Matériel d'un inventaire, ou <c>null</c> s'il n'a pas été collecté.</summary>
        public ComputerInfo GetComputer(long inventoryId)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT inventory_id, manufacturer, model, cpu_name, cpu_cores, cpu_threads, " +
                    "ram_bytes, ram_usable_bytes, gpu_name, bios_vendor, bios_version, os_name, " +
                    "os_version, os_build, os_arch, os_install_date " +
                    "FROM computer WHERE inventory_id = @id;";
                command.Parameters.AddWithValue("@id", inventoryId);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new ComputerInfo
                    {
                        InventoryId = reader.GetInt64(0),
                        Manufacturer = Text(reader, 1),
                        Model = Text(reader, 2),
                        CpuName = Text(reader, 3),
                        CpuCores = Integer(reader, 4),
                        CpuThreads = Integer(reader, 5),
                        RamBytes = Number(reader, 6),
                        RamUsableBytes = Number(reader, 7),
                        GpuName = Text(reader, 8),
                        BiosVendor = Text(reader, 9),
                        BiosVersion = Text(reader, 10),
                        OsName = Text(reader, 11),
                        OsVersion = Text(reader, 12),
                        OsBuild = Text(reader, 13),
                        OsArch = Text(reader, 14),
                        OsInstallDateUtc = Date(reader, 15)
                    };
                }
            }
        }

        /// <summary>Volumes fixes d'un inventaire, dans l'ordre des lettres de lecteur.</summary>
        public List<VolumeInfo> GetVolumes(long inventoryId)
        {
            var result = new List<VolumeInfo>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT id, inventory_id, drive, label, file_system, total_bytes, free_bytes, " +
                    "disk_index, disk_model, disk_size_bytes " +
                    "FROM volume WHERE inventory_id = @id ORDER BY drive;";
                command.Parameters.AddWithValue("@id", inventoryId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new VolumeInfo
                        {
                            Id = reader.GetInt64(0),
                            InventoryId = reader.GetInt64(1),
                            Drive = Text(reader, 2),
                            Label = Text(reader, 3),
                            FileSystem = Text(reader, 4),
                            TotalBytes = Number(reader, 5),
                            FreeBytes = Number(reader, 6),
                            DiskIndex = Integer(reader, 7),
                            DiskModel = Text(reader, 8),
                            DiskSizeBytes = Number(reader, 9)
                        });
                    }
                }
            }

            return result;
        }

        private void InsertComputer(long inventoryId, ComputerInfo computer, SQLiteTransaction transaction)
        {
            using (var command = _connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO computer (inventory_id, manufacturer, model, cpu_name, cpu_cores, " +
                    "cpu_threads, ram_bytes, ram_usable_bytes, gpu_name, bios_vendor, bios_version, " +
                    "os_name, os_version, os_build, os_arch, os_install_date) " +
                    "VALUES (@inventoryId, @manufacturer, @model, @cpuName, @cpuCores, @cpuThreads, " +
                    "@ramBytes, @ramUsableBytes, @gpuName, @biosVendor, @biosVersion, @osName, " +
                    "@osVersion, @osBuild, @osArch, @osInstallDate);";

                command.Parameters.AddWithValue("@inventoryId", inventoryId);
                command.Parameters.AddWithValue("@manufacturer", Db(computer.Manufacturer));
                command.Parameters.AddWithValue("@model", Db(computer.Model));
                command.Parameters.AddWithValue("@cpuName", Db(computer.CpuName));
                command.Parameters.AddWithValue("@cpuCores", Db(computer.CpuCores));
                command.Parameters.AddWithValue("@cpuThreads", Db(computer.CpuThreads));
                command.Parameters.AddWithValue("@ramBytes", Db(computer.RamBytes));
                command.Parameters.AddWithValue("@ramUsableBytes", Db(computer.RamUsableBytes));
                command.Parameters.AddWithValue("@gpuName", Db(computer.GpuName));
                command.Parameters.AddWithValue("@biosVendor", Db(computer.BiosVendor));
                command.Parameters.AddWithValue("@biosVersion", Db(computer.BiosVersion));
                command.Parameters.AddWithValue("@osName", Db(computer.OsName));
                command.Parameters.AddWithValue("@osVersion", Db(computer.OsVersion));
                command.Parameters.AddWithValue("@osBuild", Db(computer.OsBuild));
                command.Parameters.AddWithValue("@osArch", Db(computer.OsArch));
                command.Parameters.AddWithValue("@osInstallDate", Db(SqlDate.ToDb(computer.OsInstallDateUtc)));

                command.ExecuteNonQuery();
            }
        }

        private void InsertVolume(long inventoryId, VolumeInfo volume, SQLiteTransaction transaction)
        {
            using (var command = _connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO volume (inventory_id, drive, label, file_system, total_bytes, " +
                    "free_bytes, disk_index, disk_model, disk_size_bytes) " +
                    "VALUES (@inventoryId, @drive, @label, @fileSystem, @totalBytes, @freeBytes, " +
                    "@diskIndex, @diskModel, @diskSizeBytes);";

                command.Parameters.AddWithValue("@inventoryId", inventoryId);
                command.Parameters.AddWithValue("@drive", Db(volume.Drive));
                command.Parameters.AddWithValue("@label", Db(volume.Label));
                command.Parameters.AddWithValue("@fileSystem", Db(volume.FileSystem));
                command.Parameters.AddWithValue("@totalBytes", Db(volume.TotalBytes));
                command.Parameters.AddWithValue("@freeBytes", Db(volume.FreeBytes));
                command.Parameters.AddWithValue("@diskIndex", Db(volume.DiskIndex));
                command.Parameters.AddWithValue("@diskModel", Db(volume.DiskModel));
                command.Parameters.AddWithValue("@diskSizeBytes", Db(volume.DiskSizeBytes));

                command.ExecuteNonQuery();
            }
        }

        private void Execute(string sql, SQLiteTransaction transaction, long inventoryId)
        {
            using (var command = _connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@id", inventoryId);
                command.ExecuteNonQuery();
            }
        }

        private static object Db(object value)
        {
            return value ?? DBNull.Value;
        }

        private static string Text(SQLiteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : reader.GetString(index);
        }

        private static long? Number(SQLiteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (long?)null : reader.GetInt64(index);
        }

        private static int? Integer(SQLiteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (int?)null : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static DateTime? Date(SQLiteDataReader reader, int index)
        {
            if (reader.IsDBNull(index)) return null;

            var value = SqlDate.FromDb(reader.GetString(index));
            return value == DateTime.MinValue ? (DateTime?)null : value;
        }

        private static Inventory Read(SQLiteDataReader reader)
        {
            return new Inventory
            {
                Id = reader.GetInt64(0),
                StartedAtUtc = SqlDate.FromDb(reader.IsDBNull(1) ? null : reader.GetString(1)),
                FinishedAtUtc = reader.IsDBNull(2) ? (DateTime?)null : SqlDate.FromDb(reader.GetString(2)),
                Status = reader.IsDBNull(3) ? InventoryStatus.Cancelled : reader.GetString(3),
                AppVersion = reader.IsDBNull(4) ? null : reader.GetString(4),
                RootsCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                FilesCount = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                FoldersCount = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
                TotalBytes = reader.IsDBNull(8) ? 0 : reader.GetInt64(8),
                IssuesCount = reader.IsDBNull(9) ? 0 : reader.GetInt64(9),
                Note = reader.IsDBNull(10) ? null : reader.GetString(10)
            };
        }
    }
}
