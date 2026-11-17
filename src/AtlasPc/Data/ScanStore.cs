using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using AtlasPc.Models;

namespace AtlasPc.Data
{
    /// <summary>
    /// Tout le SQL du parcours des fichiers : racines, dossiers, fichiers, incidents.
    /// Instances préparées une fois, réutilisées à chaque lot — c'est ce qui rend
    /// l'insertion de plusieurs centaines de milliers de lignes supportable.
    ///
    /// L'instance utilise de préférence <b>sa propre connexion</b> (voir
    /// <see cref="Open"/>), ouverte sur le même fichier que celle de l'interface :
    /// une connexion SQLite ne doit pas servir deux threads à la fois.
    /// </summary>
    public sealed class ScanStore : IDisposable
    {
        /// <summary>Cache de la connexion de scan : 8 Mo, la moitié de celle de l'interface.</summary>
        public const int ScanCacheKb = 8000;

        private const int MaxMessageLength = 300;

        private readonly SQLiteConnection _connection;
        private readonly bool _ownsConnection;

        private readonly SQLiteCommand _insertFile;
        private readonly SQLiteCommand _insertFolder;
        private readonly SQLiteCommand _insertIssue;
        private readonly SQLiteCommand _openRoot;
        private readonly SQLiteCommand _closeRoot;

        public ScanStore(SQLiteConnection connection)
            : this(connection, false)
        {
        }

        private ScanStore(SQLiteConnection connection, bool owns)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            _connection = connection;
            _ownsConnection = owns;

            _insertFile = connection.CreateCommand();
            _insertFile.CommandText =
                "INSERT INTO file (inventory_id, folder_path, name, ext, size_bytes, modified_at) " +
                "VALUES (@inventoryId, @folderPath, @name, @ext, @sizeBytes, @modifiedAt);";
            _insertFile.Parameters.Add(new SQLiteParameter("@inventoryId", DbType.Int64));
            _insertFile.Parameters.Add(new SQLiteParameter("@folderPath", DbType.String));
            _insertFile.Parameters.Add(new SQLiteParameter("@name", DbType.String));
            _insertFile.Parameters.Add(new SQLiteParameter("@ext", DbType.String));
            _insertFile.Parameters.Add(new SQLiteParameter("@sizeBytes", DbType.Int64));
            _insertFile.Parameters.Add(new SQLiteParameter("@modifiedAt", DbType.String));
            _insertFile.Prepare();

            _insertFolder = connection.CreateCommand();
            _insertFolder.CommandText =
                "INSERT INTO folder (inventory_id, parent_path, path, size_bytes, files_count, " +
                "dirs_count, modified_at) " +
                "VALUES (@inventoryId, @parentPath, @path, @sizeBytes, @filesCount, @dirsCount, @modifiedAt);";
            _insertFolder.Parameters.Add(new SQLiteParameter("@inventoryId", DbType.Int64));
            _insertFolder.Parameters.Add(new SQLiteParameter("@parentPath", DbType.String));
            _insertFolder.Parameters.Add(new SQLiteParameter("@path", DbType.String));
            _insertFolder.Parameters.Add(new SQLiteParameter("@sizeBytes", DbType.Int64));
            _insertFolder.Parameters.Add(new SQLiteParameter("@filesCount", DbType.Int32));
            _insertFolder.Parameters.Add(new SQLiteParameter("@dirsCount", DbType.Int32));
            _insertFolder.Parameters.Add(new SQLiteParameter("@modifiedAt", DbType.String));
            _insertFolder.Prepare();

            _insertIssue = connection.CreateCommand();
            _insertIssue.CommandText =
                "INSERT INTO scan_issue (inventory_id, root_drive, path, kind, code, message) " +
                "VALUES (@inventoryId, @rootDrive, @path, @kind, @code, @message);";
            _insertIssue.Parameters.Add(new SQLiteParameter("@inventoryId", DbType.Int64));
            _insertIssue.Parameters.Add(new SQLiteParameter("@rootDrive", DbType.String));
            _insertIssue.Parameters.Add(new SQLiteParameter("@path", DbType.String));
            _insertIssue.Parameters.Add(new SQLiteParameter("@kind", DbType.String));
            _insertIssue.Parameters.Add(new SQLiteParameter("@code", DbType.Int32));
            _insertIssue.Parameters.Add(new SQLiteParameter("@message", DbType.String));
            _insertIssue.Prepare();

            _openRoot = connection.CreateCommand();
            _openRoot.CommandText =
                "INSERT INTO scan_root (inventory_id, path, drive, label, status, started_at) " +
                "VALUES (@inventoryId, @path, @drive, @label, @status, @startedAt);";
            _openRoot.Parameters.Add(new SQLiteParameter("@inventoryId", DbType.Int64));
            _openRoot.Parameters.Add(new SQLiteParameter("@path", DbType.String));
            _openRoot.Parameters.Add(new SQLiteParameter("@drive", DbType.String));
            _openRoot.Parameters.Add(new SQLiteParameter("@label", DbType.String));
            _openRoot.Parameters.Add(new SQLiteParameter("@status", DbType.String));
            _openRoot.Parameters.Add(new SQLiteParameter("@startedAt", DbType.String));
            _openRoot.Prepare();

            _closeRoot = connection.CreateCommand();
            _closeRoot.CommandText =
                "UPDATE scan_root SET status = @status, files_count = @filesCount, " +
                "folders_count = @foldersCount, total_bytes = @totalBytes, issues_count = @issuesCount, " +
                "finished_at = @finishedAt, error_message = @error " +
                "WHERE inventory_id = @inventoryId AND drive = @drive;";
            _closeRoot.Parameters.Add(new SQLiteParameter("@status", DbType.String));
            _closeRoot.Parameters.Add(new SQLiteParameter("@filesCount", DbType.Int64));
            _closeRoot.Parameters.Add(new SQLiteParameter("@foldersCount", DbType.Int64));
            _closeRoot.Parameters.Add(new SQLiteParameter("@totalBytes", DbType.Int64));
            _closeRoot.Parameters.Add(new SQLiteParameter("@issuesCount", DbType.Int64));
            _closeRoot.Parameters.Add(new SQLiteParameter("@finishedAt", DbType.String));
            _closeRoot.Parameters.Add(new SQLiteParameter("@error", DbType.String));
            _closeRoot.Parameters.Add(new SQLiteParameter("@inventoryId", DbType.Int64));
            _closeRoot.Parameters.Add(new SQLiteParameter("@drive", DbType.String));
            _closeRoot.Prepare();
        }

        /// <summary>
        /// Ouvre la connexion du scan et renvoie le magasin. Sa connexion lui appartient :
        /// il la referme en fin de parcours.
        /// </summary>
        public static ScanStore Open(Database database)
        {
            if (database == null) throw new ArgumentNullException("database");
            return new ScanStore(database.CreateConnection(ScanCacheKb), true);
        }

        /// <summary>Déclare une racine, au statut <c>running</c>.</summary>
        public void OpenRoot(long inventoryId, ScanTarget target)
        {
            if (target == null) throw new ArgumentNullException("target");

            _openRoot.Parameters["@inventoryId"].Value = inventoryId;
            _openRoot.Parameters["@path"].Value = target.Path;
            _openRoot.Parameters["@drive"].Value = target.Drive;
            _openRoot.Parameters["@label"].Value = (object)target.Label ?? DBNull.Value;
            _openRoot.Parameters["@status"].Value = InventoryStatus.Running;
            _openRoot.Parameters["@startedAt"].Value = SqlDate.ToDb(DateTime.UtcNow);
            _openRoot.ExecuteNonQuery();
        }

        /// <summary>Fixe le statut final d'une racine et ses compteurs.</summary>
        public void CloseRoot(long inventoryId, string drive, string status, long files, long folders,
                              long bytes, long issues, string errorMessage)
        {
            _closeRoot.Parameters["@status"].Value = status;
            _closeRoot.Parameters["@filesCount"].Value = files;
            _closeRoot.Parameters["@foldersCount"].Value = folders;
            _closeRoot.Parameters["@totalBytes"].Value = bytes;
            _closeRoot.Parameters["@issuesCount"].Value = issues;
            _closeRoot.Parameters["@finishedAt"].Value = SqlDate.ToDb(DateTime.UtcNow);
            _closeRoot.Parameters["@error"].Value = Truncate(errorMessage);
            _closeRoot.Parameters["@inventoryId"].Value = inventoryId;
            _closeRoot.Parameters["@drive"].Value = drive;
            _closeRoot.ExecuteNonQuery();
        }

        /// <summary>
        /// Écrit un lot de fichiers et de dossiers dans une seule transaction.
        /// Un lot interrompu ne laisse donc jamais la base à moitié remplie.
        /// </summary>
        public void WriteBatch(long inventoryId, IList<PendingFile> files, IList<PendingFolder> folders)
        {
            var fileCount = files == null ? 0 : files.Count;
            var folderCount = folders == null ? 0 : folders.Count;
            if (fileCount == 0 && folderCount == 0) return;

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    _insertFile.Transaction = transaction;
                    _insertFolder.Transaction = transaction;

                    for (var index = 0; index < fileCount; index++)
                    {
                        var file = files[index];
                        _insertFile.Parameters["@inventoryId"].Value = inventoryId;
                        _insertFile.Parameters["@folderPath"].Value = file.FolderPath;
                        _insertFile.Parameters["@name"].Value = file.Name;
                        _insertFile.Parameters["@ext"].Value = (object)file.Ext ?? DBNull.Value;
                        _insertFile.Parameters["@sizeBytes"].Value = file.SizeBytes;
                        _insertFile.Parameters["@modifiedAt"].Value = SqlDate.ToDb(file.ModifiedUtc);
                        _insertFile.ExecuteNonQuery();
                    }

                    for (var index = 0; index < folderCount; index++)
                    {
                        var folder = folders[index];
                        _insertFolder.Parameters["@inventoryId"].Value = inventoryId;
                        _insertFolder.Parameters["@parentPath"].Value = (object)folder.ParentPath ?? DBNull.Value;
                        _insertFolder.Parameters["@path"].Value = folder.Path;
                        _insertFolder.Parameters["@sizeBytes"].Value = folder.SizeBytes;
                        _insertFolder.Parameters["@filesCount"].Value = folder.FilesCount;
                        _insertFolder.Parameters["@dirsCount"].Value = folder.DirsCount;
                        _insertFolder.Parameters["@modifiedAt"].Value =
                            (object)SqlDate.ToDb(folder.ModifiedUtc) ?? DBNull.Value;
                        _insertFolder.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                finally
                {
                    // Après un échec, les commandes ne doivent plus désigner une
                    // transaction refermée.
                    _insertFile.Transaction = null;
                    _insertFolder.Transaction = null;
                }
            }
        }

        /// <summary>Enregistre une zone non analysée. Rare : une ligne par dossier, jamais par fichier.</summary>
        public void AddIssue(long inventoryId, ScanIssue issue)
        {
            if (issue == null) throw new ArgumentNullException("issue");

            _insertIssue.Parameters["@inventoryId"].Value = inventoryId;
            _insertIssue.Parameters["@rootDrive"].Value = (object)issue.RootDrive ?? DBNull.Value;
            _insertIssue.Parameters["@path"].Value = issue.Path;
            _insertIssue.Parameters["@kind"].Value = issue.Kind;
            _insertIssue.Parameters["@code"].Value = (object)issue.Code ?? DBNull.Value;
            _insertIssue.Parameters["@message"].Value = Truncate(issue.Message);
            _insertIssue.ExecuteNonQuery();
        }

        // --- Lectures (tests, et étape 5) -------------------------------------

        public long CountFiles(long inventoryId)
        {
            return Scalar("SELECT COUNT(*) FROM file WHERE inventory_id = @id;", inventoryId);
        }

        public long CountFolders(long inventoryId)
        {
            return Scalar("SELECT COUNT(*) FROM folder WHERE inventory_id = @id;", inventoryId);
        }

        public List<ScanIssue> GetIssues(long inventoryId)
        {
            var result = new List<ScanIssue>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT id, inventory_id, root_drive, path, kind, code, message " +
                    "FROM scan_issue WHERE inventory_id = @id ORDER BY path, kind;";
                command.Parameters.AddWithValue("@id", inventoryId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ScanIssue
                        {
                            Id = reader.GetInt64(0),
                            InventoryId = reader.GetInt64(1),
                            RootDrive = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Path = reader.GetString(3),
                            Kind = reader.GetString(4),
                            Code = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            Message = reader.IsDBNull(6) ? null : reader.GetString(6)
                        });
                    }
                }
            }

            return result;
        }

        public List<ScanRootInfo> GetRoots(long inventoryId)
        {
            var result = new List<ScanRootInfo>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT id, inventory_id, path, drive, label, status, files_count, folders_count, " +
                    "total_bytes, issues_count, started_at, finished_at, error_message " +
                    "FROM scan_root WHERE inventory_id = @id ORDER BY drive;";
                command.Parameters.AddWithValue("@id", inventoryId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ScanRootInfo
                        {
                            Id = reader.GetInt64(0),
                            InventoryId = reader.GetInt64(1),
                            Path = reader.GetString(2),
                            Drive = reader.GetString(3),
                            Label = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Status = reader.GetString(5),
                            FilesCount = reader.GetInt64(6),
                            FoldersCount = reader.GetInt64(7),
                            TotalBytes = reader.GetInt64(8),
                            IssuesCount = reader.GetInt64(9),
                            StartedAtUtc = Date(reader, 10),
                            FinishedAtUtc = Date(reader, 11),
                            ErrorMessage = reader.IsDBNull(12) ? null : reader.GetString(12)
                        });
                    }
                }
            }

            return result;
        }

        public List<ScanFolderRow> GetFolders(long inventoryId)
        {
            var result = new List<ScanFolderRow>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT parent_path, path, size_bytes, files_count, dirs_count, modified_at " +
                    "FROM folder WHERE inventory_id = @id ORDER BY path;";
                command.Parameters.AddWithValue("@id", inventoryId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ScanFolderRow
                        {
                            ParentPath = reader.IsDBNull(0) ? null : reader.GetString(0),
                            Path = reader.GetString(1),
                            SizeBytes = reader.GetInt64(2),
                            FilesCount = reader.GetInt32(3),
                            DirsCount = reader.GetInt32(4),
                            ModifiedAtUtc = Date(reader, 5)
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Vrai si ce chemin ne peut servir à aucune conclusion : son disque n'a pas été
        /// analysé jusqu'au bout (<c>cancelled</c>, <c>inaccessible</c>) ou il tombe dans
        /// le sous-arbre d'un incident.
        ///
        /// Règle du sous-arbre : un incident sur <c>C:\a\b</c> protège <c>C:\a\b</c> et
        /// tout ce qui commence par <c>C:\a\b\</c> — jamais <c>C:\a\bc</c>, jamais
        /// <c>C:\a</c> (le parent, lui, est connu). C'est l'étape 5 qui s'en servira ;
        /// la règle est ici pour être testée dès maintenant.
        /// </summary>
        public bool IsProtected(long inventoryId, string path)
        {
            if (string.IsNullOrEmpty(path)) return true;

            var drive = path.Length >= 2 ? path.Substring(0, 2) : path;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM scan_root WHERE inventory_id = @inventoryId " +
                    "AND drive = @drive AND status IN ('cancelled', 'inaccessible', 'running');";
                command.Parameters.AddWithValue("@inventoryId", inventoryId);
                command.Parameters.AddWithValue("@drive", drive);
                if (Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0) return true;
            }

            using (var command = _connection.CreateCommand())
            {
                // substr plutôt que LIKE : un chemin contenant « % » ou « _ » ne doit pas
                // se transformer en motif.
                command.CommandText =
                    "SELECT COUNT(*) FROM scan_issue WHERE inventory_id = @inventoryId AND (" +
                    "  rtrim(path, '\\') = rtrim(@path, '\\') COLLATE NOCASE" +
                    "  OR (length(rtrim(@path, '\\')) > length(rtrim(path, '\\'))" +
                    "      AND substr(rtrim(@path, '\\'), 1, length(rtrim(path, '\\')) + 1)" +
                    "          = rtrim(path, '\\') || '\\' COLLATE NOCASE)" +
                    ");";
                command.Parameters.AddWithValue("@inventoryId", inventoryId);
                command.Parameters.AddWithValue("@path", path);
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        private long Scalar(string sql, long inventoryId)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@id", inventoryId);
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static DateTime? Date(SQLiteDataReader reader, int index)
        {
            if (reader.IsDBNull(index)) return null;

            var value = SqlDate.FromDb(reader.GetString(index));
            return value == DateTime.MinValue ? (DateTime?)null : value;
        }

        private static object Truncate(string message)
        {
            if (string.IsNullOrEmpty(message)) return DBNull.Value;
            return message.Length <= MaxMessageLength ? message : message.Substring(0, MaxMessageLength) + "…";
        }

        public void Dispose()
        {
            _insertFile.Dispose();
            _insertFolder.Dispose();
            _insertIssue.Dispose();
            _openRoot.Dispose();
            _closeRoot.Dispose();

            if (_ownsConnection)
            {
                try
                {
                    _connection.Close();
                }
                catch (SQLiteException)
                {
                    // Fermeture : une erreur ici n'a pas de conséquence utile.
                }
                finally
                {
                    _connection.Dispose();
                }
            }
        }
    }
}
