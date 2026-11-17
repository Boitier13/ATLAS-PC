using System;
using System.Collections.Generic;
using AtlasPc.Data;
using AtlasPc.Models;

namespace AtlasPc.Tests
{
    /// <summary>
    /// Première vérification demandée : deux connexions SQLite simultanées sur le même
    /// fichier, l'une pour l'interface, l'autre pour le scan. Si elle échoue, tout le
    /// reste du moteur doit être revu — d'où son rang.
    /// </summary>
    internal static class StorageTests
    {
        public static void Register()
        {
            TestRunner.Add("Stock_DeuxConnexionsSimultanees", TwoConnections);
            TestRunner.Add("Stock_EcritureParLotsEtRelecture", Batches);
        }

        private static void TwoConnections()
        {
            using (var db = TestDatabase.Create())
            {
                var ui = new InventoryStore(db.Database.Connection);
                var inventoryId = ui.Create(1, "test");
                Assert.True(inventoryId > 0, "l'inventaire est créé");

                using (var scan = ScanStore.Open(db.Database))
                {
                    // Écriture par la connexion du scan, pendant que celle de l'interface
                    // reste ouverte. C'est exactement la situation d'un inventaire.
                    scan.OpenRoot(inventoryId, new ScanTarget { Drive = "Q:", Path = @"Q:\", Label = "test" });

                    scan.WriteBatch(inventoryId,
                        new[]
                        {
                            new PendingFile
                            {
                                FolderPath = @"Q:\dossier",
                                Name = "fichier.txt",
                                Ext = "txt",
                                SizeBytes = 42,
                                ModifiedUtc = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc)
                            }
                        },
                        new[]
                        {
                            new PendingFolder
                            {
                                ParentPath = @"Q:\",
                                Path = @"Q:\dossier",
                                SizeBytes = 42,
                                FilesCount = 1,
                                DirsCount = 0,
                                ModifiedUtc = null
                            }
                        });

                    // 1. la connexion de l'interface voit le lot, sans avoir été rouverte.
                    Assert.Equal(1, db.Scalar(db.Database.Connection,
                        "SELECT COUNT(*) FROM file WHERE inventory_id = " + inventoryId),
                        "la connexion de l'interface voit le lot écrit par celle du scan");

                    Assert.Equal(1, db.Scalar(db.Database.Connection,
                        "SELECT COUNT(*) FROM folder WHERE inventory_id = " + inventoryId),
                        "les dossiers sont visibles aussi");

                    // 2. l'inverse : écriture par la connexion de l'interface, lecture par le scan.
                    ui.Close(inventoryId, InventoryStatus.Complete, 1, 1, 42, 0, "note de test");
                    scan.CloseRoot(inventoryId, "Q:", InventoryStatus.Complete, 1, 1, 42, 0, null);

                    var roots = scan.GetRoots(inventoryId);
                    Assert.Equal(1, roots.Count, "une racine");
                    Assert.Equal(InventoryStatus.Complete, roots[0].Status, "la connexion du scan relit le statut");
                    Assert.Equal(1, roots[0].FilesCount, "le compteur de la racine est relu");

                    Assert.Equal(InventoryStatus.Complete, ui.Get(inventoryId).Status,
                        "la connexion de l'interface relit la clôture");
                }

                // 3. après fermeture de la connexion de scan, la base reste cohérente.
                Assert.Equal(1, db.Scalar(db.Database.Connection,
                    "SELECT COUNT(*) FROM file WHERE inventory_id = " + inventoryId),
                    "la base survit à la fermeture de la seconde connexion");
            }
        }

        private static void Batches()
        {
            using (var db = TestDatabase.Create())
            {
                var store = new InventoryStore(db.Database.Connection);
                var inventoryId = store.Create(1, "test");
                var modified = new DateTime(2023, 12, 31, 23, 59, 58, DateTimeKind.Utc);

                using (var scan = ScanStore.Open(db.Database))
                {
                    scan.OpenRoot(inventoryId, new ScanTarget { Drive = "Q:", Path = @"Q:\" });

                    // Trois lots : 500, 500, 200.
                    var total = 0;
                    for (var batch = 0; batch < 3; batch++)
                    {
                        var files = new List<PendingFile>();
                        var count = batch < 2 ? 500 : 200;

                        for (var index = 0; index < count; index++)
                        {
                            total++;
                            files.Add(new PendingFile
                            {
                                FolderPath = @"Q:\gros",
                                Name = "f" + total + ".bin",
                                Ext = index % 2 == 0 ? "bin" : (string)null,
                                SizeBytes = 1000 + index,
                                ModifiedUtc = modified
                            });
                        }

                        scan.WriteBatch(inventoryId, files, null);
                    }

                    Assert.Equal(1200, scan.CountFiles(inventoryId), "les trois lots sont écrits en entier");

                    // Une entrée sur deux n'a pas d'extension : 600 lignes sur 1200.
                    Assert.Equal(600, db.Scalar(db.Database.Connection,
                        "SELECT COUNT(*) FROM file WHERE inventory_id = " + inventoryId + " AND ext IS NULL"),
                        "une extension absente reste NULL, jamais une chaîne vide");

                    Assert.Equal("2023-12-31T23:59:58Z", db.Text(db.Database.Connection,
                        "SELECT modified_at FROM file WHERE name = 'f1.bin' AND inventory_id = " + inventoryId),
                        "la date est stockée en ISO UTC, comparable en texte");

                    Assert.Equal(1200, db.Scalar(db.Database.Connection,
                        "SELECT COUNT(DISTINCT name) FROM file WHERE inventory_id = " + inventoryId),
                        "aucun doublon");
                }
            }
        }
    }
}
