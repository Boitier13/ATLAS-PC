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
