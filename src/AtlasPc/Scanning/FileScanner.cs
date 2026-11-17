using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using AtlasPc.Data;
using AtlasPc.Models;

namespace AtlasPc.Scanning
{
    /// <summary>
    /// Parcours d'un disque : profondeur d'abord, un seul thread, une racine après
    /// l'autre.
    ///
    /// <b>Pile explicite, pas de récursion.</b> La profondeur est une donnée du disque,
    /// pas une garantie : une arborescence pathologique ferait déborder la pile d'appels
    /// et tuerait le processus. La pile est ici une <see cref="List{T}"/> de contextes,
    /// avec un plafond de sécurité (<see cref="MaxDepth"/>) qui signale un chemin
    /// anormalement profond au lieu de l'analyser.
    ///
    /// <b>Ordre d'écriture.</b> Un dossier n'est écrit qu'après ses enfants, pour
    /// connaître sa taille récursive. Vérifié dans <c>Schema.sql</c> : <c>folder</c> ne
    /// référence que <c>inventory</c>, <c>file</c> n'a aucune clé étrangère vers
    /// <c>folder</c> (le lien se fait par le chemin), et <c>folder</c> ne porte ni
    /// <c>UNIQUE</c> ni <c>CHECK</c>. Cet ordre ne viole donc aucune contrainte.
    /// </summary>
    public sealed class FileScanner
    {
        /// <summary>Lignes accumulées avant écriture.</summary>
        public const int BatchSize = 500;

        /// <summary>Plafond de sécurité de la pile de parcours.</summary>
        public const int MaxDepth = 1024;

        private readonly ScanStore _store;

        public FileScanner(ScanStore store)
        {
            if (store == null) throw new ArgumentNullException("store");
            _store = store;
        }

        /// <summary>Âge maximal d'un lot avant écriture.</summary>
        public static TimeSpan BatchDelay
        {
            get { return TimeSpan.FromSeconds(2); }
        }

        /// <summary>
        /// Parcourt les racines demandées et clôt chacune avec son statut propre.
        /// <paramref name="report"/> reçoit une photo des compteurs à chaque dossier
        /// terminé ; c'est l'appelant qui décide à quel rythme l'afficher.
        /// </summary>
        public ScanResult Scan(long inventoryId, IList<ScanTarget> targets,
                               CancellationToken token, Action<ScanSnapshot> report)
        {
            if (targets == null) throw new ArgumentNullException("targets");

            var roots = new List<ScanTarget>(targets.Count);
            foreach (var target in targets)
            {
                if (target == null) continue;
                roots.Add(new ScanTarget
                {
                    Drive = target.Drive,
                    Path = NormalizeRoot(target.Path),
                    Label = target.Label
                });
            }

            var run = new Run
            {
                InventoryId = inventoryId,
                Watch = Stopwatch.StartNew(),
                Report = report,
                Result = new ScanResult(),
                Counters = new Tally(),
                RootCount = roots.Count,
                Writer = new BatchWriter(_store, inventoryId, BatchSize, BatchDelay),
                Stack = new List<Frame>()
            };

            for (var index = 0; index < roots.Count; index++)
            {
                var target = roots[index];
                run.RootNumber = index + 1;

                if (run.Cancelled || token.IsCancellationRequested)
                {
                    run.Cancelled = true;
                    run.Result.Roots.Add(SkipRoot(run, target));
                    continue;
                }

                run.Result.Roots.Add(ScanRoot(run, target, token));
            }

            run.Writer.Flush();

            run.Result.Files = run.Counters.Files;
            run.Result.Folders = run.Counters.Folders;
            run.Result.Bytes = run.Counters.Bytes;
            run.Result.Issues = run.Counters.Issues;
            run.Result.IgnoredLinks = run.Counters.Links;
            run.Result.WasCancelled = run.Cancelled;

            return run.Result;
        }

        // --- Une racine ---------------------------------------------------------

        private RootScanResult ScanRoot(Run run, ScanTarget target, CancellationToken token)
        {
            var start = run.Counters.Copy();
            var root = new RootScanResult
            {
                Drive = target.Drive,
                Path = target.Path,
                Status = InventoryStatus.Running
            };

            _store.OpenRoot(run.InventoryId, target);

            if (!Directory.Exists(target.Path))
            {
                _store.CloseRoot(run.InventoryId, target.Drive, InventoryStatus.Inaccessible, 0, 0, 0, 1,
                    "Disque absent ou inaccessible au moment du parcours.");
                run.Counters.Issues++;
                run.Writer.Flush();

                root.Status = InventoryStatus.Inaccessible;
                root.Issues = 1;
                return root;
            }

            var rootFrame = new Frame
            {
                Path = target.Path,
                ParentPath = null,
                ModifiedUtc = DirectoryTime(target.Path),
                Children = new List<Child>()
            };

            run.Stack.Clear();
            run.Stack.Add(rootFrame);
            Enumerate(run, target, rootFrame);

            var cancelledHere = false;

            while (run.Stack.Count > 0)
            {
                if (token.IsCancellationRequested)
                {
                    cancelledHere = true;
                    run.Cancelled = true;
                    Cancel(run, target);
                    break;
                }

                var top = run.Stack[run.Stack.Count - 1];

                if (top.Next < top.Children.Count)
                {
                    var child = top.Children[top.Next];
                    top.Next++;

                    var childPath = Path.Combine(top.Path, child.Name);

                    if (run.Stack.Count >= MaxDepth)
                    {
                        // Le dossier existe, on le note, on n'y descend pas, et on le dit :
                        // jamais un oubli silencieux.
                        _store.AddIssue(run.InventoryId, new ScanIssue
                        {
                            RootDrive = target.Drive,
                            Path = childPath,
                            Kind = ScanIssueKind.PathTooLong,
                            Message = "Profondeur maximale d'analyse atteinte : ce dossier et son contenu n'ont pas été parcourus."
                        });
                        run.Counters.Issues++;
                        top.DirsCount++;

                        run.Writer.AddFolder(new PendingFolder
                        {
                            ParentPath = top.Path,
                            Path = childPath,
                            SizeBytes = 0,
                            FilesCount = 0,
                            DirsCount = 0,
                            ModifiedUtc = child.ModifiedUtc
                        });
                        run.Counters.Folders++;
                        continue;
                    }

                    var childFrame = new Frame
                    {
                        Path = childPath,
                        ParentPath = top.Path,
                        ModifiedUtc = child.ModifiedUtc,
                        Children = new List<Child>()
                    };

                    Enumerate(run, target, childFrame);
                    run.Stack.Add(childFrame);
                    continue;
                }

                // Tous les enfants sont passés : le dossier peut être écrit, avec sa
                // taille récursive et ses dénombrements directs.
                run.Stack.RemoveAt(run.Stack.Count - 1);

                run.Writer.AddFolder(new PendingFolder
                {
                    ParentPath = top.ParentPath,
                    Path = top.Path,
                    SizeBytes = top.SizeBytes,
                    FilesCount = top.FilesCount,
                    DirsCount = top.DirsCount,
                    ModifiedUtc = top.ModifiedUtc
                });
                run.Counters.Folders++;

                if (run.Stack.Count > 0)
                {
                    var parent = run.Stack[run.Stack.Count - 1];
                    parent.SizeBytes += top.SizeBytes;
                    parent.DirsCount += 1;
                }

                Report(run, target, top.Path);
            }

            var delta = run.Counters.Minus(start);

            string status;
            if (cancelledHere) status = InventoryStatus.Cancelled;
            else if (delta.Issues > 0) status = InventoryStatus.Partial;
            else status = InventoryStatus.Complete;

            run.Writer.Flush();
            _store.CloseRoot(run.InventoryId, target.Drive, status,
                delta.Files, delta.Folders, delta.Bytes, delta.Issues, null);

            root.Status = status;
            root.Files = delta.Files;
            root.Folders = delta.Folders;
            root.Bytes = delta.Bytes;
            root.Issues = delta.Issues;
            root.IgnoredLinks = delta.Links;

            return root;
        }

        /// <summary>Une racine que l'annulation a empêchée de commencer.</summary>
        private RootScanResult SkipRoot(Run run, ScanTarget target)
        {
            _store.OpenRoot(run.InventoryId, target);
            _store.AddIssue(run.InventoryId, new ScanIssue
            {
                RootDrive = target.Drive,
                Path = target.Path,
                Kind = ScanIssueKind.CancelledBefore,
                Message = "Disque non parcouru : annulation demandée avant le début de son analyse."
            });
            _store.CloseRoot(run.InventoryId, target.Drive, InventoryStatus.Cancelled, 0, 0, 0, 1,
                "Non parcouru : annulation demandée avant le début de l'analyse.");

            run.Counters.Issues++;
            run.Writer.Flush();

            return new RootScanResult
            {
                Drive = target.Drive,
                Path = target.Path,
                Status = InventoryStatus.Cancelled,
                Issues = 1
            };
        }

        // --- Un dossier ---------------------------------------------------------

        /// <summary>
        /// Lit un dossier : écrit ses fichiers directs dans le lot, retient le nom de ses
        /// sous-dossiers, et enregistre un incident si la lecture échoue. Un refus
        /// d'accès n'interrompt jamais le parcours.
        /// </summary>
        private void Enumerate(Run run, ScanTarget target, Frame frame)
        {
            try
            {
                var directory = new DirectoryInfo(frame.Path);

                foreach (var info in directory.EnumerateFileSystemInfos())
                {
                    // Jamais suivi : jonctions, liens symboliques, OneDrive. Suivre un
                    // point de reparse, c'est risquer une boucle infinie et un double
                    // comptage. On le compte comme ignoré, et on le dit.
                    if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        run.Counters.Links++;
                        continue;
                    }

                    var sub = info as DirectoryInfo;
                    if (sub != null)
                    {
                        frame.Children.Add(new Child
                        {
                            Name = sub.Name,
                            ModifiedUtc = sub.LastWriteTimeUtc
                        });
                        continue;
                    }

                    var file = (FileInfo)info;

                    frame.FilesCount++;
                    frame.SizeBytes += file.Length;

                    run.Counters.Files++;
                    run.Counters.Bytes += file.Length;
                    run.Writer.AddFile(frame.Path, file.Name, Extension(file.Name),
                        file.Length, file.LastWriteTimeUtc);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Issue(run, target, frame.Path, ScanIssueKind.AccessDenied, ex);
            }
            catch (PathTooLongException ex)
            {
                Issue(run, target, frame.Path, ScanIssueKind.PathTooLong, ex);
            }
            catch (IOException ex)
            {
                Issue(run, target, frame.Path, ScanIssueKind.IoError, ex);
            }
            catch (SecurityException ex)
            {
                Issue(run, target, frame.Path, ScanIssueKind.AccessDenied, ex);
            }
            catch (Exception ex)
            {
                Issue(run, target, frame.Path, ScanIssueKind.Unexpected, ex);
            }
        }

        private void Issue(Run run, ScanTarget target, string path, string kind, Exception error)
        {
            _store.AddIssue(run.InventoryId, new ScanIssue
            {
                RootDrive = target.Drive,
                Path = path,
                Kind = kind,
                Code = error.HResult,
                Message = error.Message
            });

            run.Counters.Issues++;
        }

        // --- Annulation ---------------------------------------------------------

        /// <summary>
        /// Arrêt demandé. On écrit <b>une seule</b> ligne, exactement là où l'on se
        /// trouvait : on ne prétend pas connaître les dossiers qui restaient à visiter,
        /// puisqu'on ne les a pas lus. La protection vient de la racine, qui passe en
        /// <c>cancelled</c> — et de la règle du sous-arbre, qui couvre ce dossier.
        /// </summary>
        private void Cancel(Run run, ScanTarget target)
        {
            if (run.Stack.Count == 0)
            {
                run.Writer.Flush();
                return;
            }

            var zone = run.Stack[run.Stack.Count - 1];

            _store.AddIssue(run.InventoryId, new ScanIssue
            {
                RootDrive = target.Drive,
                Path = zone.Path,
                Kind = ScanIssueKind.CancelledBefore,
                Message = "Parcours interrompu dans ce dossier : son contenu n'a pas été analysé en entier, et le reste du disque non plus. La racine est marquée annulée."
            });
            run.Counters.Issues++;

            // Les dossiers encore ouverts sont écrits avec leurs totaux partiels : une
            // ligne de fichier ne doit jamais désigner un dossier absent de la table.
            for (var index = run.Stack.Count - 1; index >= 0; index--)
            {
                var frame = run.Stack[index];

                run.Writer.AddFolder(new PendingFolder
                {
                    ParentPath = frame.ParentPath,
                    Path = frame.Path,
                    SizeBytes = frame.SizeBytes,
                    FilesCount = frame.FilesCount,
                    DirsCount = frame.DirsCount,
                    ModifiedUtc = frame.ModifiedUtc
                });
                run.Counters.Folders++;
            }

            run.Stack.Clear();
            run.Writer.Flush();
        }

        // --- Outils -------------------------------------------------------------

        private void Report(Run run, ScanTarget target, string currentPath)
        {
            if (run.Report == null) return;

            run.Report(new ScanSnapshot
            {
                Drive = target.Drive,
                CurrentPath = currentPath,
                RootNumber = run.RootNumber,
                RootCount = run.RootCount,
                Files = run.Counters.Files,
                Folders = run.Counters.Folders,
                Bytes = run.Counters.Bytes,
                Issues = run.Counters.Issues,
                IgnoredLinks = run.Counters.Links,
                Elapsed = run.Watch.Elapsed
            });
        }

        /// <summary>Extension sans le point, en minuscules ; <c>null</c> s'il n'y en a pas.</summary>
        private static string Extension(string name)
        {
            var ext = Path.GetExtension(name);
            if (string.IsNullOrEmpty(ext) || ext.Length < 2) return null;
            return ext.Substring(1).ToLowerInvariant();
        }

        private static DateTime? DirectoryTime(string path)
        {
            try
            {
                var info = new DirectoryInfo(path);
                return info.Exists ? info.LastWriteTimeUtc : (DateTime?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>« C: » devient « C:\ » : sans séparateur, Path.Combine produirait « C:Users ».</summary>
        private static string NormalizeRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length == 2 && path[1] == ':') return path + Path.DirectorySeparatorChar;
            return path;
        }

        // --- Types internes -----------------------------------------------------

        private sealed class Run
        {
            public long InventoryId;
            public BatchWriter Writer;
            public Stopwatch Watch;
            public Action<ScanSnapshot> Report;
            public ScanResult Result;
            public Tally Counters;
            public List<Frame> Stack;
            public bool Cancelled;
            public int RootNumber;
            public int RootCount;
        }

        /// <summary>Compteurs cumulés ; les chiffres d'une racine sont obtenus par différence.</summary>
        private sealed class Tally
        {
            public long Files;
            public long Folders;
            public long Bytes;
            public long Issues;
            public long Links;

            public Tally Copy()
            {
                return new Tally { Files = Files, Folders = Folders, Bytes = Bytes, Issues = Issues, Links = Links };
            }

            public Tally Minus(Tally other)
            {
                return new Tally
                {
                    Files = Files - other.Files,
                    Folders = Folders - other.Folders,
                    Bytes = Bytes - other.Bytes,
                    Issues = Issues - other.Issues,
                    Links = Links - other.Links
                };
            }
        }

        /// <summary>Le contexte d'un dossier en cours : c'est une entrée de la pile, pas un appel.</summary>
        private sealed class Frame
        {
            public string Path;
            public string ParentPath;
            public DateTime? ModifiedUtc;
            public List<Child> Children;
            public int Next;
            public long SizeBytes;
            public int FilesCount;
            public int DirsCount;
        }

        private struct Child
        {
            public string Name;
            public DateTime ModifiedUtc;
        }
    }
}
