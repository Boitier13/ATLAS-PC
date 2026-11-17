using System;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace AtlasPc.Data
{
    /// <summary>
    /// Ouvre le fichier SQLite unique de l'application et crée le schéma.
    /// Aucune migration : le schéma est créé en une fois, de façon idempotente.
    /// Seule exception : deux colonnes ajoutées en v2, posées par
    /// <see cref="EnsureColumn"/> sur une base créée avant elles.
    /// </summary>
    public sealed class Database : IDisposable
    {
        /// <summary>Numéro de schéma attendu par cette version du logiciel.</summary>
        public const int SchemaVersion = 2;

        private const string SchemaResourceName = "AtlasPc.Data.Schema.sql";
        private const string FolderName = "ATLAS PC";
        private const string FileName = "atlas.db";

        private SQLiteConnection _connection;

        public Database(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException("path");
            FilePath = path;
        }

        /// <summary>%LOCALAPPDATA%\ATLAS PC</summary>
        public static string DefaultFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    FolderName);
            }
        }

        /// <summary>%LOCALAPPDATA%\ATLAS PC\atlas.db</summary>
        public static string DefaultPath
        {
            get { return Path.Combine(DefaultFolder, FileName); }
        }

        public string FilePath { get; private set; }

        public SQLiteConnection Connection
        {
            get
            {
                if (_connection == null) throw new InvalidOperationException("La base n'est pas ouverte.");
                return _connection;
            }
        }

        public long SizeBytes
        {
            get
            {
                try
                {
                    var info = new FileInfo(FilePath);
                    return info.Exists ? info.Length : 0L;
                }
                catch (IOException)
                {
                    return 0L;
                }
            }
        }

        /// <summary>
        /// Ouvre la base, applique les PRAGMA, vérifie son intégrité et crée le schéma.
        /// Lève <see cref="InvalidDataException"/> si le fichier est inutilisable.
        /// </summary>
        public void Open()
        {
            var folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            _connection = OpenConnection(16000, true);
            CheckIntegrity();
            EnsureSchema();
        }

        /// <summary>
        /// Ouvre une <b>seconde</b> connexion sur le même fichier, pour le thread de scan.
        /// <see cref="SQLiteConnection"/> n'est pas faite pour être partagée entre deux
        /// threads : l'interface garde la sienne pour lire, le scan écrit avec la sienne.
        /// Le mode WAL autorise un écrivain et des lecteurs simultanés.
        /// </summary>
        /// <param name="cacheKb">Taille du cache de cette connexion, en Ko.</param>
        public SQLiteConnection CreateConnection(int cacheKb)
        {
            if (!File.Exists(FilePath))
                throw new InvalidOperationException("La base n'est pas encore ouverte : impossible d'en ouvrir une seconde connexion.");

            // Le mode journal est inscrit dans le fichier : la seconde connexion n'a pas
            // à le reposer, ce qui évite une tentative de verrou inutile à l'ouverture.
            return OpenConnection(cacheKb <= 0 ? 8000 : cacheKb, false);
        }

        private SQLiteConnection OpenConnection(int cacheKb, bool setJournalMode)
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = FilePath,
                Version = 3,
                ForeignKeys = true,
                JournalMode = setJournalMode ? SQLiteJournalModeEnum.Wal : SQLiteJournalModeEnum.Default,
                SyncMode = SynchronizationModes.Normal,
                FailIfMissing = false,
                Pooling = false
            };

            var connection = new SQLiteConnection(builder.ToString());
            connection.Open();

            ExecutePragma(connection, "PRAGMA cache_size = -" +
                cacheKb.ToString(CultureInfo.InvariantCulture) + ";");
            ExecutePragma(connection, "PRAGMA temp_store = MEMORY;");

            // Deux connexions, un seul écrivain à la fois : plutôt que d'échouer sur
            // « database is locked », on attend jusqu'à 5 s.
            ExecutePragma(connection, "PRAGMA busy_timeout = 5000;");

            return connection;
        }

        private static void ExecutePragma(SQLiteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        private void CheckIntegrity()
        {
            string result;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "PRAGMA quick_check;";
                result = Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Le fichier de base de données semble endommagé (contrôle d'intégrité : " +
                    (result ?? "sans réponse") + ").");
            }
        }

        private void EnsureSchema()
        {
            var script = ReadSchemaScript();
            if (script == null)
                throw new InvalidOperationException("Le script de schéma embarqué est introuvable.");

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = script;
                command.ExecuteNonQuery();
            }

            EnsureColumn("computer", "cpu_threads", "INTEGER");
            EnsureColumn("computer", "ram_usable_bytes", "INTEGER");

            var version = ReadUserVersion();
            if (version > SchemaVersion)
            {
                throw new InvalidDataException(
                    "La base a été créée par une version plus récente d'ATLAS PC (schéma " +
                    version.ToString(CultureInfo.InvariantCulture) + ", cette version lit le schéma " +
                    SchemaVersion.ToString(CultureInfo.InvariantCulture) + ").");
            }

            if (version != SchemaVersion)
            {
                Execute("PRAGMA user_version = " + SchemaVersion.ToString(CultureInfo.InvariantCulture) + ";");
            }
        }

        /// <summary>
        /// Ajoute une colonne si elle manque. Une base créée en v1 possède déjà la table
        /// <c>computer</c> : <c>CREATE TABLE IF NOT EXISTS</c> n'y touche plus, d'où cette
        /// vérification. Noms de table et de colonne fixés dans le code : aucune injection.
        /// </summary>
        private void EnsureColumn(string table, string column, string type)
        {
            var found = false;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(" + table + ");";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                        if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                Execute("ALTER TABLE " + table + " ADD COLUMN " + column + " " + type + ";");
            }
        }

        private static string ReadSchemaScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(SchemaResourceName))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private int ReadUserVersion()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version;";
                var value = command.ExecuteScalar();
                return value == null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private void Execute(string sql)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Renomme le fichier en <c>atlas.db.ko-AAAAMMJJ-HHmmss</c> et renvoie le nouveau chemin.
        /// Utilisé uniquement après accord explicite de l'utilisateur.
        /// </summary>
        public string RenameToBackup()
        {
            Close();

            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var backup = FilePath + ".ko-" + stamp;
            if (File.Exists(FilePath)) File.Move(FilePath, backup);

            // Les fichiers annexes de SQLite (WAL) sont déplacés avec la base.
            MoveIfExists(FilePath + "-wal", backup + "-wal");
            MoveIfExists(FilePath + "-shm", backup + "-shm");

            return backup;
        }

        private static void MoveIfExists(string from, string to)
        {
            try
            {
                if (File.Exists(from)) File.Move(from, to);
            }
            catch (IOException)
            {
                // Un fichier annexe impossible à déplacer ne doit pas bloquer la recréation.
            }
        }

        public void Close()
        {
            if (_connection == null) return;

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
                _connection = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
