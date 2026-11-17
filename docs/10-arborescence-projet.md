# Livrable 7 — Arborescence proposée du futur code source

> Ce document décrit une **organisation**, pas du code. Aucun fichier source n'est
> créé à ce stade ; les noms de fichiers ci-dessous sont des emplacements prévus.

---

## 1. Vue d'ensemble

```
ATLAS-PC/
├─ ATLAS-PC.sln
├─ Directory.Build.props              propriétés communes (LangVersion, nullable, versions)
├─ .editorconfig                      style, encodage UTF-8, fins de ligne
├─ .gitignore
├─ README.md                          présentation, capture, démarrage rapide
├─ LICENSE                            MIT (ou Apache-2.0) — à figer avant le premier commit
├─ THIRD-PARTY-NOTICES.md             licences des dépendances livrées
├─ CHANGELOG.md
│
├─ docs/                              ce dossier technique (déjà présent)
│  ├─ 00-lisez-moi.md … 12-risques-et-limites.md
│  └─ 13-registre-inspiration.md       à créer au démarrage du développement
│
├─ assets/
│  ├─ maquette-atlas-pc.png
│  ├─ app.ico                         icône multi-résolutions
│  └─ fonts/                          (vide : Segoe UI est fournie par Windows)
│
├─ src/
│  ├─ Atlas.Core/                     domaine et contrats — aucune dépendance
│  ├─ Atlas.Data/                     SQLite : schéma, repositories, comparaison
│  ├─ Atlas.Scan/                     moteur de parcours de fichiers
│  ├─ Atlas.Collectors/               matériel, OS, logiciels, outils, projets
│  ├─ Atlas.Reports/                  HTML, CSV, JSON
│  └─ Atlas.App/                      application WPF (point d'entrée)
│
├─ tests/
│  ├─ Atlas.Core.Tests/
│  ├─ Atlas.Data.Tests/
│  ├─ Atlas.Scan.Tests/
│  ├─ Atlas.Collectors.Tests/
│  └─ Atlas.Reports.Tests/
│
├─ tools/
│  ├─ Atlas.DevHarness/               console de développement (pas livrée)
│  └─ build/                          scripts de publication
│
└─ .github/workflows/
   ├─ build.yml
   └─ test.yml
```

---

## 2. Les projets

| Projet | Type | Cible | Références | Rôle |
|---|---|---|---|---|
| `Atlas.Core` | bibliothèque | `net48` (+ `net10.0-windows` en second) | **aucune** | modèles, contrats, règles de portée et de comparaison, énumérations de statut |
| `Atlas.Data` | bibliothèque | `net48` | Core, `Microsoft.Data.Sqlite` | schéma, migrations, écriture par lots, requêtes de comparaison |
| `Atlas.Scan` | bibliothèque | `net48` | Core | énumération Win32, pipeline, progression, annulation |
| `Atlas.Collectors` | bibliothèque | `net48` | Core, `System.Management` | matériel (WMI), registre, outils, projets |
| `Atlas.Reports` | bibliothèque | `net48` | Core | génération HTML/CSV/JSON |
| `Atlas.App` | application WPF | `net48` | **tous** | vues, view-models, services de présentation |
| `Atlas.*.Tests` | test | `net48` | projet testé | tests unitaires (NUnit ou xUnit) |

**Pourquoi `net48` et pas `netstandard2.0` pour les bibliothèques** : `Atlas.Scan`
et `Atlas.Collectors` utilisent des API Windows (P/Invoke, `System.Management`,
registre) qui n'existent pas en `netstandard2.0`. `Atlas.Core` et `Atlas.Reports`
pourraient être `netstandard2.0` ; les aligner sur `net48` simplifie et évite les
mauvaises surprises de résolution d'assemblies. La multi-cible se limite à
`net48;net10.0-windows` pour `Core`, `Data`, `Reports` — les deux projets Windows
restant spécifiques.

---

## 3. Détail des dossiers

### 3.1 `src/Atlas.Core/`

```
Atlas.Core/
├─ Inventories/
│  ├─ Inventory.cs                 racine : id, dates, statut, compteurs
│  ├─ InventoryStatus.cs           enum Complete | Partial | Cancelled | Failed
│  ├─ InventoryScope.cs            volume ou dossier analysé
│  └─ InventorySummary.cs          agrégats de synthèse (pour le tableau de bord)
├─ Hardware/
│  ├─ ComputerInfo.cs  CpuInfo.cs  MemoryModuleInfo.cs
│  ├─ GpuInfo.cs       DiskInfo.cs PartitionInfo.cs VolumeInfo.cs
│  ├─ BiosInfo.cs      OsInfo.cs
│  └─ FieldState.cs                Available | Unavailable | Approx | Inferred | Redacted
├─ Software/
│  ├─ SoftwareItem.cs              nom, version, éditeur, source, confiance
│  └─ SoftwareSource.cs            enum RegistryMsi | RegistryExe | Store | Portable | Shortcut
├─ Tools/
│  ├─ DevTool.cs                   kind, chemin, version, méthode, statut
│  └─ DevToolKind.cs               enum Python | Pip | Node | Npm | Git | Tesseract
├─ Files/
│  ├─ FolderRecord.cs  FileRecord.cs
│  ├─ FileAttributesFlags.cs       miroir lisible des attributs Win32 utiles
│  └─ ScanError.cs                 chemin, code, message, occurrences
├─ Projects/
│  ├─ ProjectRecord.cs  ProjectKind.cs
│  └─ ProjectMarker.cs             table des marqueurs et types associés
├─ Comparison/
│  ├─ InventoryComparison.cs       résultat complet
│  ├─ FileChange.cs                Ajouté | Disparu | Modifié
│  ├─ SoftwareChange.cs  ToolChange.cs
│  └─ StorageDelta.cs
├─ Contracts/
│  ├─ ICollector.cs                CollectAsync(contexte, jeton) → résultat + statut
│  ├─ IScanEngine.cs               ScanAsync(portée, progression, jeton) → résumé
│  ├─ IInventoryRepository.cs      création, écriture, lecture, comparaison
│  ├─ IReportWriter.cs             HTML | CSV | JSON
│  └─ IProgressReporter.cs         abstraction de la progression (testable)
└─ Ports/
   └─ IFileSystem.cs, IRegistryReader.cs, IWmiQuery.cs, IProcessRunner.cs
```

Les **ports** (`IFileSystem`, `IRegistryReader`, `IWmiQuery`, `IProcessRunner`) sont le
point clé de la testabilité : ils permettent de tester la logique de collecte et de
scan sans toucher au disque, au registre ni à WMI. Les implémentations réelles vivent
dans `Atlas.Scan` et `Atlas.Collectors`.

### 3.2 `src/Atlas.Scan/`

```
Atlas.Scan/
├─ Native/
│  ├─ Kernel32.cs                  FindFirstFileExW, GetDiskFreeSpace, GetCompressedFileSize
│  ├─ SafeFindHandle.cs            SafeHandleZeroOrMinusOneIsInvalid
│  ├─ Win32FindData.cs             structures avec CharSet Unicode
│  └─ Win32Error.cs                codes + traduction en messages
├─ PathHandling/
│  ├─ LongPath.cs                  préfixes \\?\ et \\?\UNC\, concaténation sûre
│  └─ PathKey.cs                   normalisation + FNV-1a 64 bits (path_hash)
├─ Engine/
│  ├─ DirectoryScanEngine.cs       orchestration : pile, lots, progression, annulation
│  ├─ DirectoryEnumeration.cs      un dossier → entrées (testable via IFileSystem)
│  ├─ ScanOptions.cs               travailleurs (1-2), mode discret, exclusions
│  ├─ ScanProgress.cs              compteurs + chemin courant
│  └─ BatchWriter.cs               accumulation et envoi des lots à IInventoryRepository
└─ Projects/
   └─ ProjectDetector.cs           détection des marqueurs pendant le parcours
```

### 3.3 `src/Atlas.Collectors/`

```
Atlas.Collectors/
├─ Wmi/
│  ├─ WmiReader.cs                 implémentation de IWmiQuery avec délai par requête
│  └─ WmiResults.cs                objets uniformes (statut, message, durée)
├─ Hardware/
│  ├─ ComputerCollector.cs  CpuCollector.cs  MemoryCollector.cs
│  ├─ GpuCollector.cs       StorageCollector.cs  BiosCollector.cs
│  └─ OsCollector.cs
├─ Software/
│  ├─ RegistrySoftwareCollector.cs   3 ruches, filtrage documenté
│  └─ PortableSoftwareCollector.cs   heuristique, confiance faible
├─ Tools/
│  ├─ ToolLocator.cs                 PATH + dossiers connus
│  └─ ToolVersionReader.cs           FileVersionInfo → fichier → commande (3 s)
├─ Registry/
│  └─ RegistryReader.cs              implémentation de IRegistryReader (lecture seule)
└─ CollectorsPipeline.cs             enchaînement ordonné + arrêt propre sur échec
```

### 3.4 `src/Atlas.Data/`

```
Atlas.Data/
├─ Sqlite/
│  ├─ SqliteConnectionFactory.cs    ouvertures, PRAGMA, chemin %LOCALAPPDATA%
│  ├─ SqliteInventoryRepository.cs  implémentation de IInventoryRepository
│  └─ SchemaMigrator.cs             version → application des scripts
├─ Scripts/
│  ├─ 001-initial.sql
│  ├─ 002-indexes.sql
│  └─ ...                           scripts embarqués en ressources
├─ Writers/
│  ├─ FolderBatchWriter.cs          insertion par lots, calcul des totaux
│  ├─ FileBatchWriter.cs
│  └─ StatsWriter.cs                agrégats précalculés
├─ Queries/
│  ├─ ComparisonQueries.cs          les requêtes du livrable 5
│  ├─ DashboardQueries.cs
│  └─ RetentionService.cs           allègement et purge
└─ Maintenance/
   └─ DatabaseHealth.cs             intégrité, taille, VACUUM/incremental
```

### 3.5 `src/Atlas.Reports/`

```
Atlas.Reports/
├─ Html/
│  ├─ HtmlReportWriter.cs
│  ├─ Templates/report.html         gabarit unique, jetons remplacés
│  └─ Assets/report.css             CSS embarqué (aucune ressource externe)
├─ Csv/
│  └─ CsvReportWriter.cs            un fichier par section + un pour les fichiers
├─ Json/
│  └─ JsonReportWriter.cs           writer maison, schéma versionné
└─ Masking/
   └─ PathMasker.cs                 C:\Users\<nom> → C:\Users\%USER%
```

### 3.6 `src/Atlas.App/`

```
Atlas.App/
├─ App.xaml / App.xaml.cs           démarrage, gestion globale des erreurs
├─ app.manifest                     asInvoker, PerMonitorV2, longPathAware
├─ Shell/
│  ├─ MainWindow.xaml               sidebar + barre supérieure + zone de contenu
│  ├─ NavigationService.cs
│  └─ ShellViewModel.cs
├─ Views/
│  ├─ DashboardView.xaml            Tableau de bord
│  ├─ ComputerView.xaml             Ordinateur
│  ├─ FilesView.xaml                Fichiers
│  ├─ SoftwareView.xaml             Logiciels
│  ├─ ToolsView.xaml                Outils
│  ├─ DevelopmentView.xaml          Développement
│  ├─ EvolutionView.xaml            Évolution
│  └─ ReportsView.xaml              Rapports
├─ ViewModels/
│  ├─ DashboardViewModel.cs … ReportsViewModel.cs
│  └─ Rows/                         lignes de tableau (structs en lecture seule)
├─ Controls/
│  ├─ BarChart.cs                   dessin natif, aucune dépendance
│  ├─ DonutChart.cs
│  ├─ StatCard.xaml                 carte de synthèse réutilisable
│  ├─ StatusBadge.xaml              Complet / Partiel / Annulé / Échec
│  ├─ ProgressBanner.xaml
│  └─ EmptyState.xaml
├─ Services/
│  ├─ InventoryRunner.cs            enchaîne collecte → scan → clôture
│  ├─ ProgressBridge.cs             IProgress → Dispatcher
│  └─ DialogService.cs
├─ Themes/
│  ├─ Colors.xaml  Typography.xaml  Controls.xaml
│  └─ (aucune animation en V1)
└─ Resources/app.ico
```

---

## 4. Règles de dépendance (vérifiables en revue)

```
Atlas.App ──► Atlas.Core ◄── Atlas.Scan
    │             ▲      ◄── Atlas.Collectors
    ├─────────────┤      ◄── Atlas.Data
    └─────────────┘      ◄── Atlas.Reports
```

| Règle | Vérification |
|---|---|
| `Atlas.Core` ne référence aucun autre projet ni package | inspection du `.csproj` |
| `Atlas.Core` ne contient aucun `using System.Windows.*` | recherche textuelle en CI |
| `Atlas.Data` ne contient aucun `using System.Windows.*` | idem |
| `Atlas.Scan` et `Atlas.Collectors` ne contiennent aucune API d'écriture (`File.Write*`, `File.Delete`, `File.Move`, `Registry.SetValue`, `Directory.CreateDirectory` hors dossier applicatif) | test « garde-fou » (livrable 4, § 10) |
| `Atlas.App` ne contient aucun SQL | recherche de `SELECT`/`INSERT`/`CREATE TABLE` dans `Atlas.App` |
| Aucun projet ne référence `Atlas.App` | inspection |

Ces règles sont peu coûteuses à écrire et évitent la dérive classique vers un
« projet unique fourre-tout ».

---

## 5. Emplacements sur la machine cible

| Élément | Emplacement | Remarque |
|---|---|---|
| Exécutable | `%LOCALAPPDATA%\Programs\ATLAS PC\` (ou dossier choisi, portable) | pas d'installeur en V1 |
| Base | `%LOCALAPPDATA%\ATLAS PC\atlas.db` | jamais dans le dossier de l'exe (droits, roaming) |
| Journaux | `%LOCALAPPDATA%\ATLAS PC\logs\` | rotation à 5 fichiers × 1 Mo |
| Exports | `%USERPROFILE%\Documents\ATLAS PC\Rapports\` (modifiable) | choisi par l'utilisateur |
| Configuration | `%LOCALAPPDATA%\ATLAS PC\settings.json` | petite, lisible, sans secret |

Tout ce qu'ATLAS PC écrit est **confiné** à ces quatre emplacements. C'est la
traduction opérationnelle de la règle de lecture seule.

---

## 6. Conventions

- **Langue du code** : anglais pour les identifiants, français pour les libellés
  d'interface et la documentation. Une seule règle, appliquée partout.
- **Nommage** : `PascalCase` pour les types et membres publics, `_camelCase` pour les
  champs privés, `I` pour les interfaces, `Async` pour les méthodes asynchrones.
- **Fichiers** : un type public par fichier, nom du fichier = nom du type.
- **Nullable** : activé dans `Directory.Build.props` pour les projets qui le
  supportent ; sinon, annotations explicites.
- **Analyseurs** : `Microsoft.CodeAnalysis.NetAnalyzers` + `.editorconfig` ; les
  avertissements sont traités en erreurs en Release.
- **Tests** : nommage `Method_Scenario_ExpectedResult`.
- **Commits** : conventionnels (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).

---

## 7. Intégration continue

| Workflow | Déclencheur | Étapes |
|---|---|---|
| `build.yml` | push, PR | restore, build Release `/warnaserror`, test « garde-fou » des API d'écriture |
| `test.yml` | push, PR | tests unitaires, publication du rapport de couverture |
| `release.yml` | tag `v*` | build, `zip` portable, notes de version, somme SHA-256 |

Pas de publication automatique sur un magasin : la V1 est un dossier compressé et un
exécutable, conformément à « fonctionne localement, gratuitement, sans abonnement ».
