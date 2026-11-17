using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AtlasPc.Data;
using AtlasPc.Models;
using AtlasPc.Scanning;

namespace AtlasPc.Tests
{
    /// <summary>
    /// Le moteur, sur de vraies arborescences : totaux, reparse, refus d'accès,
    /// annulation, statuts par racine, protection du sous-arbre, lecture seule.
    /// </summary>
    internal static class ScannerTests
    {
        public static void Register()
        {
            TestRunner.Add("Scanner_CalculeLesTotauxDeDossier", Totals);
            TestRunner.Add("Scanner_CompteLesEnfantsDirectsEtLaTailleRecursive", DirectCounts);
            TestRunner.Add("Scanner_SauteLesPointsDeReparse", ReparsePoints);
            TestRunner.Add("Scanner_EnregistreLesDossiersIllisibles", Unreadable);
            TestRunner.Add("Scanner_ClotParRacine", TwoRoots);
            TestRunner.Add("Scanner_AnnuleEtEcritLeLotEnCours", CancelKeepsBatch);
            TestRunner.Add("Scanner_MarqueLaZoneIncomplete", CancelMarksZone);
            TestRunner.Add("Issues_ProtegentLeSousArbreSeulement", SubtreeProtection);
            TestRunner.Add("Scanner_NeModifieAucunFichier", ReadOnly);
        }

        // --- Totaux et dénombrements -------------------------------------------

        private static void Totals()
        {
            using (var workspace = new TempWorkspace("totaux"))
            using (var db = TestDatabase.Create())
            {
                workspace.File("a.txt", 100);
                workspace.File("b.txt", 250);
                workspace.File(@"sub\c.txt", 400);
                workspace.File(@"sub\deep\d.txt", 50);

                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    var result = Scan(store, inventoryId, workspace);

                    Assert.Equal(4, result.Files, "quatre fichiers");
                    Assert.Equal(3, result.Folders, "trois dossiers : racine, sub, deep");
                    Assert.Equal(800, result.Bytes, "800 octets au total");
                    Assert.Equal(InventoryStatus.Complete, result.Roots[0].Status, "racine complète");

                    var folders = store.GetFolders(inventoryId);
                    var root = Find(folders, workspace.Root);
                    Assert.Equal(800, root.SizeBytes, "taille récursive de la racine");
                    Assert.Equal(2, root.FilesCount, "deux fichiers directs à la racine");
                    Assert.Equal(1, root.DirsCount, "un sous-dossier direct");

                    var sub = Find(folders, workspace.Full("sub"));
                    Assert.Equal(450, sub.SizeBytes, "taille récursive de sub");
                    Assert.Equal(1, sub.FilesCount, "un fichier direct dans sub");

                    Assert.True(root.ParentPath == null, "la racine n'a pas de parent");
                    Assert.Equal(workspace.Root, sub.ParentPath, "parent_path de sub");
                }
            }
        }

        private static void DirectCounts()
        {
            using (var workspace = new TempWorkspace("directs"))
            using (var db = TestDatabase.Create())
            {
                // 3 fichiers directs, 2 sous-dossiers, 6 fichiers plus bas.
                workspace.File("a.txt", 1);
                workspace.File("b.txt", 2);
                workspace.File("c.txt", 4);
                workspace.File(@"x\1.txt", 8);
                workspace.File(@"x\2.txt", 16);
                workspace.File(@"y\3.txt", 32);
                workspace.File(@"y\z\4.txt", 64);

                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    Scan(store, inventoryId, workspace);
                    var folders = store.GetFolders(inventoryId);

                    var root = Find(folders, workspace.Root);
                    Assert.Equal(3, root.FilesCount, "trois fichiers directs seulement");
                    Assert.Equal(2, root.DirsCount, "deux sous-dossiers directs seulement");
                    Assert.Equal(127, root.SizeBytes, "la taille, elle, est récursive (1+2+4+8+16+32+64)");

                    var y = Find(folders, workspace.Full("y"));
                    Assert.Equal(1, y.FilesCount, "un fichier direct dans y");
                    Assert.Equal(1, y.DirsCount, "un sous-dossier direct dans y");
                    Assert.Equal(96, y.SizeBytes, "taille récursive de y (32 + 64)");
                }
            }
        }

        // --- Points de reparse --------------------------------------------------

        private static void ReparsePoints()
        {
            using (var workspace = new TempWorkspace("reparse"))
            using (var db = TestDatabase.Create())
            {
                workspace.File("a.txt", 10);
                workspace.File(@"sub\c.txt", 400);

                // Jonction vers un dossier situé dans le périmètre : suivie, elle ferait
                // une boucle. C'est le test qui compte.
                if (!NativeSecurity.TryCreateJunction(workspace.Full("lien"), workspace.Full("sub")))
                    throw new SkipTestException("impossible de créer une jonction sur cette machine (mklink /J)");

                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    var result = Scan(store, inventoryId, workspace);

                    Assert.Equal(2, result.Files, "le fichier derrière la jonction n'est compté qu'une fois");
                    Assert.Equal(2, result.Folders, "la jonction ne produit aucune ligne de dossier");
                    Assert.Equal(1, result.IgnoredLinks, "un point de reparse ignoré, et compté");

                    foreach (var folder in store.GetFolders(inventoryId))
                        Assert.True(!folder.Path.EndsWith("lien", StringComparison.OrdinalIgnoreCase),
                            "aucune ligne pour la jonction : " + folder.Path);
                }
            }
        }

        // --- Refus d'accès ------------------------------------------------------

        private static void Unreadable()
        {
            using (var workspace = new TempWorkspace("refus"))
            using (var db = TestDatabase.Create())
            {
                workspace.File("a.txt", 10);
                workspace.Dir("prive");
                workspace.File(@"prive\secret.txt", 20);
                workspace.File(@"prive\autre.txt", 30);

                workspace.DenyListing("prive");

                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    var result = Scan(store, inventoryId, workspace);

                    Assert.Equal(1, result.Files, "seul le fichier lisible est inventorié");
                    Assert.Equal(InventoryStatus.Partial, result.Roots[0].Status,
                        "une racine avec une zone en erreur est partielle, jamais complète");

                    var issues = store.GetIssues(inventoryId);
                    Assert.Equal(1, issues.Count, "un incident, une seule ligne");
                    Assert.Equal(ScanIssueKind.AccessDenied, issues[0].Kind, "nature access_denied");
                    Assert.Equal(workspace.Full("prive"), issues[0].Path, "le dossier refusé");
                    Assert.True(!string.IsNullOrEmpty(issues[0].Message), "le message est conservé");

                    // Le dossier refusé reste connu : il existe, on sait qu'il existe.
                    Assert.Equal(2, store.CountFolders(inventoryId), "le dossier refusé garde sa ligne");
                }
            }
        }

        // --- Statuts par racine --------------------------------------------------

        private static void TwoRoots()
        {
            using (var workspace = new TempWorkspace("deux-racines"))
            using (var db = TestDatabase.Create())
            {
                workspace.File(@"A\a.txt", 10);
                workspace.File(@"B\b.txt", 20);
                workspace.Dir(@"B\prive");
                workspace.DenyListing(@"B\prive");

                // Deux racines d'un même inventaire : la clé de scan_root est (inventaire,
                // disque). Les lettres sont fictives ici, aucun parcours ne les utilise.
                var inventoryId = NewInventory(db, 2);

                using (var store = ScanStore.Open(db.Database))
                {
                    var scanner = new FileScanner(store);
                    var result = scanner.Scan(inventoryId,
                        new[]
                        {
                            new ScanTarget { Drive = "R1:", Path = workspace.Full("A") },
                            new ScanTarget { Drive = "R2:", Path = workspace.Full("B") }
                        },
                        CancellationToken.None, null);

                    Assert.Equal(2, result.Roots.Count, "deux racines, deux résultats");
                    Assert.Equal(InventoryStatus.Complete, result.Roots[0].Status, "première racine complète");
                    Assert.Equal(InventoryStatus.Partial, result.Roots[1].Status, "seconde racine partielle");
                    Assert.True(!result.WasCancelled, "aucune annulation");

                    var roots = store.GetRoots(inventoryId);
                    Assert.Equal(2, roots.Count, "deux lignes de racine");
                    Assert.Equal(InventoryStatus.Complete, roots[0].Status, "statut de la première racine, relu");
                    Assert.Equal(InventoryStatus.Partial, roots[1].Status, "statut de la seconde racine, relu");
                    Assert.True(!roots[0].BlocksComparisons, "une racine complète autorise les conclusions");
                    Assert.True(!roots[1].BlocksComparisons, "une racine partielle les autorise aussi, sous garde");
                }
            }
        }

        // --- Annulation ----------------------------------------------------------

        private static void CancelKeepsBatch()
        {
            using (var workspace = new TempWorkspace("annulation"))
            using (var db = TestDatabase.Create())
            {
                for (var directory = 0; directory < 20; directory++)
                {
                    for (var file = 0; file < 10; file++)
                    {
                        workspace.File(Path.Combine("d" + directory, "f" + file + ".txt"), 10);
                    }
                }

                // 200 fichiers : moins d'un lot de 500. Seul le vidage d'annulation peut
                // les écrire — c'est précisément ce que ce test vérifie.
                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    var source = new CancellationTokenSource();
                    var reports = 0;
                    ScanSnapshot atCancel = null;

                    var result = Scan(store, inventoryId, workspace, source.Token, snapshot =>
                    {
                        reports++;
                        if (reports == 3)
                        {
                            atCancel = snapshot;
                            source.Cancel();
                        }
                    });

                    Assert.True(result.WasCancelled, "le parcours est signalé annulé");
                    Assert.Equal(InventoryStatus.Cancelled, result.Roots[0].Status, "la racine est annulée");

                    Assert.NotNull(atCancel, "trois dossiers ont été terminés avant l'arrêt");

                    var written = store.CountFiles(inventoryId);
                    Assert.True(written > 0, "le lot en cours a été écrit, rien n'est perdu");
                    Assert.Equal(result.Files, written, "tous les fichiers vus sont en base");
                    Assert.True(written < 200, "le parcours ne s'était pas terminé");
                }
            }
        }

        private static void CancelMarksZone()
        {
            using (var workspace = new TempWorkspace("zone"))
            using (var db = TestDatabase.Create())
            {
                // root \ a \ b \ c0..c19 : l'annulation tombe alors que b est ouvert.
                workspace.File(@"a\b\c0\f.txt", 10);
                for (var index = 1; index < 20; index++)
                {
                    workspace.File(Path.Combine("a", "b", "c" + index, "f.txt"), 10);
                }

                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    var source = new CancellationTokenSource();
                    var reports = 0;

                    Scan(store, inventoryId, workspace, source.Token, snapshot =>
                    {
                        // Les c sont tous frères : au cinquième dossier terminé, la pile
                        // contient racine, a et b.
                        reports++;
                        if (reports == 5) source.Cancel();
                    });

                    var issues = store.GetIssues(inventoryId);

                    Assert.Equal(1, issues.Count, "une seule ligne d'incident, pas une par dossier restant");
                    Assert.Equal(ScanIssueKind.CancelledBefore, issues[0].Kind, "nature cancelled_before");
                    Assert.Equal(workspace.Full(Path.Combine("a", "b")), issues[0].Path,
                        "la zone enregistrée est le dossier où le parcours s'est arrêté");
                    Assert.True(!string.IsNullOrEmpty(issues[0].Message), "le message explique la limite");

                    // Ce qui a été vu est en base : aucune ligne de fichier orpheline.
                    var folders = store.GetFolders(inventoryId);
                    Assert.True(folders.Count >= 3, "les dossiers ouverts ont été écrits");
                    Assert.True(store.CountFiles(inventoryId) > 0, "les fichiers vus sont conservés");

                    foreach (var file in db.Texts(db.Database.Connection,
                        "SELECT DISTINCT folder_path FROM file WHERE inventory_id = " + inventoryId))
                    {
                        Assert.NotNull(Find(folders, file), "chaque dossier de fichier existe : " + file);
                    }

                    Assert.Equal(InventoryStatus.Cancelled, store.GetRoots(inventoryId)[0].Status,
                        "la racine porte le statut annulé");
                }
            }
        }

        // --- Protection du sous-arbre -------------------------------------------

        private static void SubtreeProtection()
        {
            using (var workspace = new TempWorkspace("protection"))
            using (var db = TestDatabase.Create())
            {
                workspace.File("public/a.txt", 10);
                workspace.Dir("privee");
                workspace.File(@"privee\dossier\x.txt", 10);

                workspace.DenyListing("privee");

                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    var result = Scan(store, inventoryId, workspace);
                    Assert.Equal(InventoryStatus.Partial, result.Roots[0].Status, "racine partielle");

                    var blocked = workspace.Full("privee");

                    Assert.True(store.IsProtected(inventoryId, blocked), "le dossier refusé est protégé");
                    Assert.True(store.IsProtected(inventoryId, Path.Combine(blocked, "dossier", "x.txt")),
                        "son sous-arbre est protégé");
                    Assert.True(store.IsProtected(inventoryId, Path.Combine(blocked, "profond", "encore", "plus", "f.txt")),
                        "tout descendant l'est, même non vu");
                    Assert.True(!store.IsProtected(inventoryId, workspace.Full("public")),
                        "un dossier voisin ne l'est pas");
                    Assert.True(!store.IsProtected(inventoryId, blocked + "bis"),
                        "un chemin qui commence pareil n'est pas un descendant");
                    Assert.True(!store.IsProtected(inventoryId, workspace.Root),
                        "le parent du dossier refusé n'est pas protégé");
                }
            }
        }

        // --- Lecture seule -------------------------------------------------------

        private static void ReadOnly()
        {
            using (var workspace = new TempWorkspace("lecture"))
            using (var db = TestDatabase.Create())
            {
                workspace.File("a.txt", 10);
                workspace.File(@"sub\b.txt", 20);

                var before = Snapshot(workspace.Root);
                var inventoryId = NewInventory(db);

                using (var store = ScanStore.Open(db.Database))
                {
                    Scan(store, inventoryId, workspace);
                }

                var after = Snapshot(workspace.Root);

                Assert.Equal(before.Count, after.Count, "même nombre d'entrées après le parcours");
                foreach (var entry in before)
                {
                    Assert.True(after.ContainsKey(entry.Key), "entrée toujours présente : " + entry.Key);
                    Assert.Equal(entry.Value, after[entry.Key], "contenu et date intacts : " + entry.Key);
                }
            }
        }

        // --- Outils --------------------------------------------------------------

        private static long NewInventory(TestDatabase db, int roots = 1)
        {
            return new InventoryStore(db.Database.Connection).Create(roots, "test");
        }

        private static ScanResult Scan(ScanStore store, long inventoryId, TempWorkspace workspace,
                                       CancellationToken token = default(CancellationToken),
                                       Action<ScanSnapshot> report = null)
        {
            var scanner = new FileScanner(store);
            var targets = new[]
            {
                new ScanTarget { Drive = workspace.Drive, Path = workspace.Root, Label = "test" }
            };

            return scanner.Scan(inventoryId, targets, token, report);
        }

        private static ScanFolderRow Find(List<ScanFolderRow> folders, string path)
        {
            foreach (var folder in folders)
            {
                if (string.Equals(folder.Path, path, StringComparison.OrdinalIgnoreCase)) return folder;
            }

            throw new Exception("dossier absent de la base : " + path);
        }

        /// <summary>Contenu et date de chaque entrée : la preuve qu'aucun fichier n'a bougé.</summary>
        private static SortedDictionary<string, string> Snapshot(string root)
        {
            var map = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                var info = new FileInfo(path);
                map[path] = info.Length + "|" + info.LastWriteTimeUtc.Ticks;
            }

            foreach (var path in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                map[path + "\\"] = "dossier";
            }

            return map;
        }
    }
}
