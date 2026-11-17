# Livrable 7 — Arborescence du futur projet

> **Révision du 17 novembre 2026.** Un seul projet applicatif au lieu de six, aucune
> couche d'abstraction, un projet de tests unique conservé parce qu'il apporte un
> avantage réel.

---

## 1. Vue d'ensemble

```
ATLAS-PC/
├─ ATLAS-PC.sln
├─ .editorconfig
├─ .gitignore
├─ README.md
├─ LICENSE                     MIT
├─ CHANGELOG.md
├─ docs/                       ce dossier
├─ assets/
│  ├─ maquette-atlas-pc.png
│  └─ app.ico
├─ src/
│  └─ AtlasPc/
└─ tests/
   └─ AtlasPc.Tests/
```

Deux projets, pas plus. Pas de `Directory.Build.props` : deux fichiers `.csproj`
suffisent.

---

## 2. Le projet applicatif `src/AtlasPc/`

```
AtlasPc/
├─ AtlasPc.csproj              net48, WinExe, UseWPF
├─ app.config                  redirection de binding SQLite + chemins longs
├─ app.manifest                asInvoker, PerMonitorV2, longPathAware
├─ App.xaml / App.xaml.cs      démarrage, gestion globale des exceptions
│
├─ Models/
│  ├─ Inventory.cs             id, dates, statut, racine, compteurs
│  ├─ ComputerInfo.cs          matériel + Windows (une classe, des propriétés)
│  ├─ DiskInfo.cs  VolumeInfo.cs
│  ├─ SoftwareItem.cs  ToolInfo.cs  ProjectInfo.cs
│  ├─ FileEntry.cs             struct : dossier, nom, extension, taille, date
│  ├─ ScanIssue.cs             dossier, code, message
│  └─ ComparisonResult.cs      ajoutés, supprimés, non vérifiables, modifiés, delta
│
├─ Data/
│  ├─ Schema.sql               ressource embarquée, les 10 tables + index
│  ├─ Database.cs              ouverture, PRAGMA, création/migration du schéma
│  ├─ InventoryStore.cs        créer, lister, supprimer, clôturer un inventaire
│  ├─ ScanStore.cs             écriture par lots (dossiers, fichiers, incidents)
│  ├─ ReadQueries.cs           lectures d'affichage (dossiers, fichiers, top, types)
│  └─ ComparisonQueries.cs     les quatre requêtes du livrable 5
│
├─ Scanning/
│  ├─ FileScanner.cs           parcours DFS, totaux, progression, annulation
│  ├─ ScanProgress.cs          compteurs + chemin courant
│  └─ BatchWriter.cs           accumulation et envoi des lots
│
├─ Collecting/
│  ├─ ComputerCollector.cs     WMI + registre
│  ├─ SoftwareCollector.cs     trois clés Uninstall
│  ├─ ToolCollector.cs         six outils
│  └─ ProjectDetector.cs       marqueurs package.json / requirements.txt / pyproject.toml
│
├─ ViewModels/
│  ├─ ObservableObject.cs      ~20 lignes : INotifyPropertyChanged + OnPropertyChanged
│  ├─ RelayCommand.cs          ~25 lignes
│  ├─ ShellViewModel.cs        navigation, inventaire en cours, état global
│  ├─ DashboardViewModel.cs  ComputerViewModel.cs  FilesViewModel.cs
│  └─ SoftwareViewModel.cs   ToolsViewModel.cs     DevelopmentViewModel.cs
│     EvolutionViewModel.cs
│
├─ Views/
│  ├─ ShellWindow.xaml         barre latérale + barre supérieure + zone de contenu
│  ├─ DashboardView.xaml       OrdinateurView.xaml    FilesView.xaml
│  ├─ SoftwareView.xaml        ToolsView.xaml         DevelopmentView.xaml
│  └─ EvolutionView.xaml
│
├─ Controls/
│  ├─ StatCard.xaml            carte de synthèse
│  ├─ BarRow.xaml              libellé + barre + valeur (le seul graphique)
│  └─ StatusBadge.xaml         Complet / Partiel / Annulé
│
├─ Themes/
│  └─ Theme.xaml               couleurs, typographie, styles de tableau et de bouton
│
└─ Resources/
   └─ app.ico
```

### Nombre de fichiers par dossier

| Dossier | Fichiers | Remarque |
|---|---|---|
| `Models` | 10 | classes de données, sans logique |
| `Data` | 6 | tout le SQL est ici |
| `Scanning` | 3 | aucune dépendance à WPF |
| `Collecting` | 4 | une classe par domaine |
| `ViewModels` | 9 | un par vue + 2 utilitaires |
| `Views` | 8 | XAML |
| `Controls` | 3 | |
| `Themes` | 1 | |

Total ≈ 44 fichiers, dont 8 fichiers XAML et un fichier SQL. C'est l'ordre de grandeur
d'un petit logiciel de bureau, pas d'une plateforme.

---

## 3. Ce qu'on ne trouve **pas** dans l'arborescence

| Absent | Pourquoi |
|---|---|
| `Atlas.Core`, `Atlas.Data`, `Atlas.Scan`… | un seul projet : les dossiers suffisent |
| `Contracts/`, `Ports/`, `Abstractions/` | aucune interface pour un seul implémenteur |
| `Services/` | aurait servi d'interface entre vues et données : les ViewModels appelent `Data` directement |
| `Reports/` | fonction supprimée |
| `Migrations/` (dossier de scripts) | une seule table de version dans `Database.cs`, le schéma est créé en une fois |
| `Scripts/`, `tools/`, `build/` | publication = un `dotnet publish` documenté dans le README |
| `Interop/` | aucune signature P/Invoke en V1 |
| `Resources/fonts`, `Resources/icons` | Segoe UI et les icônes système suffisent |

---

## 4. Le projet de tests `tests/AtlasPc.Tests/`

Il est **conservé** parce qu'il teste des choses qui ne se vérifient pas à l'œil :

| Test | Ce qu'il prouve |
|---|---|
| `Scanner_CalculeLesTotauxDeDossier` | les tailles remontées sont exactes |
| `Scanner_SauteLesPointsDeReparse` | aucune boucle infinie |
| `Scanner_EnregistreLesDossiersIllisibles` | un refus d'accès ne casse pas le scan |
| `Scanner_AnnuleSansPerdreLeLotEnCours` | l'annulation laisse un inventaire cohérent |
| `Comparison_DistingueSuppressionEtZoneNonAnalysee` | **la règle la plus importante du cahier des charges** |
| `Comparison_DetecteAjoutSuppressionModification` | sur un jeu de fichiers créé puis modifié |
| `Software_FiltreSystemComponentEtParentKeyName` | le filtrage du registre est correct |
| `ProjectDetector_IgnoreNodeModules` | pas de faux projets |
| `Scanning_NeContientAucuneApiEcriture` | garde-fou de lecture seule (analyse par réflexion) |

Le projet de tests utilise des **dossiers temporaires réels** pour le scan et une base
SQLite temporaire pour la comparaison. Aucune interface n'est nécessaire : on teste le
code réel, pas des doublures. C'est plus fiable et plus court.

---

## 5. Détail des fichiers clés

| Fichier | Rôle | Taille attendue |
|---|---|---|
| `Data/Schema.sql` | les 10 tables et les index, en un seul script | ~120 lignes |
| `Data/Database.cs` | ouverture, `PRAGMA`, exécution du schéma, version | ~120 lignes |
| `Data/ScanStore.cs` | insertion par lots, transactions | ~150 lignes |
| `Data/ComparisonQueries.cs` | 4 requêtes + lecture | ~200 lignes |
| `Scanning/FileScanner.cs` | parcours, totaux, progression, annulation | ~200 lignes |
| `Collecting/ComputerCollector.cs` | 7 requêtes WMI + registre | ~180 lignes |
| `Collecting/ToolCollector.cs` | résolution + versions | ~180 lignes |
| `ViewModels/FilesViewModel.cs` | arbre, tableau, recherche | ~200 lignes |
| `Views/*.xaml` | 7 vues | 80-150 lignes chacune |

Aucun fichier ne devrait dépasser 300 lignes. Au-delà, c'est le signe qu'une
responsabilité a été mal placée.

---

## 6. Emplacements sur la machine

| Élément | Emplacement |
|---|---|
| Exécutable et DLL | dossier choisi par l'utilisateur (copie de dossier, pas d'installeur) |
| Base | `%LOCALAPPDATA%\ATLAS PC\atlas.db` |
| Journal d'erreurs | `%LOCALAPPDATA%\ATLAS PC\erreurs.log` (rotation à 1 Mo × 3) |

Rien d'autre n'est écrit. Aucune clé de registre, aucun fichier dans `Program Files`,
aucun service.

**Distribution** : un dossier compressé contenant l'exécutable et la DLL SQLite.
Aucun installeur, aucun désinstalleur : supprimer le dossier suffit, la base restant
dans `%LOCALAPPDATA%` si l'utilisateur veut la conserver.

---

## 7. Conventions

- Identifiants en anglais, libellés d'interface et documentation en français.
- `PascalCase` pour les types et membres publics, `_camelCase` pour les champs privés.
- Un type public par fichier, nom du fichier = nom du type.
- `LangVersion` par défaut de `net48` (C# 7.3). Pas de polyfill, pas de `record`, pas
  de `init` : la syntaxe disponible suffit largement.
- Pas d'analyseurs supplémentaires, pas de règles de style exotiques : `.editorconfig`
  pour l'encodage et les fins de ligne seulement.
- Commits : `feat:`, `fix:`, `docs:`, `test:`, `chore:`.

---

## 8. Intégration continue

Un seul workflow, trois commandes, exécuté sur `windows-latest` :

```yaml
- dotnet build ATLAS-PC.sln -c Release /warnaserror
- dotnet test  tests/AtlasPc.Tests -c Release
- dotnet publish src/AtlasPc -c Release -o artifacts
```

Rien d'autre. Pas de publication automatique, pas de signature, pas de paquet.
