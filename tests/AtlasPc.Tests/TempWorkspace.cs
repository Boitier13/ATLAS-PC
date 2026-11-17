using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using AtlasPc.Data;

namespace AtlasPc.Tests
{
    /// <summary>
    /// Une arborescence réelle dans le dossier temporaire de l'utilisateur. Aucune
    /// doublure : le scanner lit vraiment le disque.
    /// </summary>
    internal sealed class TempWorkspace : IDisposable
    {
        private readonly List<string> _denied = new List<string>();

        public TempWorkspace(string name)
        {
            Root = Path.Combine(Path.GetTempPath(),
                "atlas-" + name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; private set; }

        /// <summary>La lettre du disque réel qui porte le dossier temporaire (« C: »).</summary>
        public string Drive
        {
            get
            {
                var root = Path.GetPathRoot(Root);
                return string.IsNullOrEmpty(root) ? null : root.Substring(0, 2);
            }
        }

        public string Full(string relative)
        {
            return Path.Combine(Root, relative);
        }

        public string Dir(string relative)
        {
            var path = Full(relative);
            Directory.CreateDirectory(path);
            return path;
        }

        public string File(string relative, int sizeBytes)
        {
            var path = Full(relative);
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.Write(new byte[sizeBytes], 0, sizeBytes);
            }

            return path;
        }

        /// <summary>
        /// Refuse la lecture d'un dossier pour l'utilisateur courant : c'est le seul moyen
        /// fidèle de provoquer un vrai refus d'accès sans être administrateur.
        /// </summary>
        public void DenyListing(string relative)
        {
            var path = Full(relative);
            if (!NativeSecurity.TryDenyListing(path))
                throw new SkipTestException("impossible de refuser la lecture de « " + relative + " » (icacls) sur cette machine");

            _denied.Add(path);
        }

        public void Dispose()
        {
            foreach (var path in _denied) NativeSecurity.AllowListing(path);

            try
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
            }
            catch (Exception)
            {
                // Un dossier temporaire resté en place ne doit pas faire échouer un test.
            }
        }
    }

    /// <summary>Une base SQLite temporaire, créée par le vrai <see cref="Database"/>.</summary>
    internal sealed class TestDatabase : IDisposable
    {
        private readonly string _path;

        private TestDatabase(Database database, string path)
        {
            Database = database;
            _path = path;
        }

        public Database Database { get; private set; }

        public static TestDatabase Create()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "atlas-db-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".db");

            var database = new Database(path);
            database.Open();
            return new TestDatabase(database, path);
        }

        public long NewInventory(InventoryStore store, int roots)
        {
            return store.Create(roots, "test");
        }

        /// <summary>Lecture directe sur la connexion de l'interface : c'est la preuve cherchée.</summary>
        public long Scalar(SQLiteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        public string Text(SQLiteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Toutes les valeurs d'une colonne, pour les contrôles d'intégrité du test.</summary>
        public List<string> Texts(SQLiteConnection connection, string sql)
        {
            var values = new List<string>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
                    }
                }
            }

            return values;
        }

        public void Dispose()
        {
            Database.Dispose();

            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                try
                {
                    if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
                }
                catch (Exception)
                {
                    // Un fichier temporaire resté en place ne doit pas faire échouer un test.
                }
            }
        }
    }

    /// <summary>
    /// Appels système que les tests ne peuvent pas simuler : refus d'accès réel, jonction
    /// réelle. Tous échouent proprement, et le disent.
    /// </summary>
    internal static class NativeSecurity
    {
        public static bool TryDenyListing(string path)
        {
            return Run("icacls.exe", "\"" + path + "\" /deny \"" + User + ":(RD)\"") == 0;
        }

        public static bool AllowListing(string path)
        {
            return Run("icacls.exe", "\"" + path + "\" /remove:d \"" + User + "\"") == 0;
        }

        /// <summary>Une jonction de dossier se crée sans droits d'administrateur, un lien symbolique non.</summary>
        public static bool TryCreateJunction(string link, string target)
        {
            try
            {
                if (Directory.Exists(link)) return true;
                if (Run("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"") != 0) return false;
                return Directory.Exists(link);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string User
        {
            get { return Environment.UserDomainName + "\\" + Environment.UserName; }
        }

        private static int Run(string program, string arguments)
        {
            try
            {
                var start = new ProcessStartInfo(program, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(start))
                {
                    if (process == null) return -1;

                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
