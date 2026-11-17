# Livrable 7 — Arborescence du futur projet

> **Révision du 17 novembre 2026 (2ᵉ).** La rubrique Fichiers devenant la fonction
> centrale, l'arborescence gagne un dossier `Shell/` (ouvrir, copier), trois classes de
> données et une séparation nette entre **navigation** et **recherche**. Toujours deux
> projets, aucune couche, aucune interface.

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
│  ├─ maquette-fichiers.png
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
│  ├─ Inventory.cs             id, dates, statut, disques, compteurs
│  ├─ ScanRootInfo.cs          disque, libellé, statut, compteurs, erreur
│  ├─ ComputerInfo.cs          matériel + Windows (une classe, des propriétés)
│  ├─ VolumeInfo.cs            volume + disque physique (fusion de l'ancienne DiskInfo)
│  ├─ SoftwareItem.cs  ToolInfo.cs  ProjectInfo.cs
│  ├─ FolderRow.cs             chemin, parent, taille, fichiers, dossiers
│  ├─ FileRow.cs               nom, dossier, extension, taille, date
│  ├─ SearchQuery.cs           texte, filtres, tri, page
│  ├─ FileCategory.cs          dictionnaire statique famille → extensions (+ couleur)
│  ├─ ScanIssue.cs             dossier, code, message
│  └─ ComparisonResult.cs      ajoutés, supprimés, non vérifiables, modifiés, delta
│
├─ Data/
│  ├─ Schema.sql               ressource embarquée, les 10 tables + index
│  ├─ Database.cs              ouverture, PRAGMA, création/migration du schéma
│  ├─ InventoryStore.cs        créer, lister, supprimer, clôturer un inventaire
│  ├─ ScanStore.cs             écriture par lots (racines, dossiers, fichiers, incidents)
│  ├─ BrowseQueries.cs         disques, enfants d'un dossier, contenu, fil d'Ariane
│  ├─ SearchQueries.cs         recherche préfixe/sous-chaîne, filtres, comptages
│  ├─ DashboardQueries.cs      les trois requêtes de synthèse
│  └─ ComparisonQueries.cs     les quatre requêtes de comparaison
│
├─ Scanning/
│  ├─ FileScanner.cs           parcours d'une racine, totaux, progression, annulation
│  ├─ ScanProgress.cs          compteurs + chemin courant + racine courante
│  └─ BatchWriter.cs           accumulation et envoi des lots
│
├─ Collecting/
│  ├─ ComputerCollector.cs     WMI + registre
│  ├─ SoftwareCollector.cs     trois clés Uninstall
│  ├─ ToolCollector.cs         six outils
│  └─ ProjectDetector.cs       marqueurs package.json / requirements.txt / pyproject.toml
│
├─ Shell/
│  ├─ ItemOpener.cs            vérifie l'existence, ouvre dossier ou fichier
│  └─ ClipboardHelper.cs       copie d'un chemin, tolérant à l'absence de presse-papiers
│
├─ ViewModels/
│  ├─ ObservableObject.cs      ~20 lignes : INotifyPropertyChanged
│  ├─ RelayCommand.cs          ~25 lignes
│  ├─ ShellViewModel.cs        navigation, inventaire en cours, état global
│  ├─ DashboardViewModel.cs    ComputerViewModel.cs
│  ├─ FilesViewModel.cs        disques, arbre, sélection, fil d'Ariane, commandes
│  ├─ SearchViewModel.cs       texte, filtres, résultats paginés, annulation
│  ├─ SoftwareViewModel.cs     ToolsViewModel.cs   DevelopmentViewModel.cs
│  └─ EvolutionViewModel.cs
│
├─ Views/
│  ├─ ShellWindow.xaml         barre latérale + barre supérieure + zone de contenu
│  ├─ DashboardView.xaml       OrdinateurView.xaml
│  ├─ FilesView.xaml           arbre + filtres + tableau + ligne d'état
│  ├─ SoftwareView.xaml        ToolsView.xaml   DevelopmentView.xaml
│  └─ EvolutionView.xaml
│
├─ Controls/
│  ├─ StatCard.xaml            carte de synthèse (icône teintée)
│  ├─ BarRow.xaml              libellé + barre + valeur (le seul graphique)
│  ├─ TypeDot.xaml             pastille de famille de fichier
│  ├─ FilterChip.xaml          filtre actif (teinte + croix)
│  ├─ DiskChip.xaml            pastille de disque avec état
│  └─ StatusBadge.xaml         Complet / Partiel / Inaccessible / Annulé
│
├─ Themes/
│  └─ Theme.xaml               couleurs, rampes, typographie, styles
│
└─ Resources/
   └─ app.ico
```

### Nombre de fichiers par dossier

| Dossier | Fichiers | Remarque |
|---|---|---|
| `Models` | 12 | classes de données, sans logique |
| `Data` | 8 | tout le SQL est ici |
| `Scanning` | 3 | aucune dépendance à WPF |
| `Collecting` | 4 | une classe par domaine |
| `Shell` | 2 | 5 commandes, 60 lignes à elles deux |
| `ViewModels` | 11 | dont deux pour la rubrique Fichiers |
| `Views` | 8 | XAML |
| `Controls` | 6 | |
| `Themes` | 1 | |

Total ≈ 55 fichiers, dont 8 XAML et un fichier SQL. C'est l'ordre de grandeur d'un
petit logiciel de bureau, pas d'une plateforme.

**Le prix de la fonction centrale** : environ 12 fichiers de plus que la version
précédente, tous concentrés sur la rubrique Fichiers. C'est un choix : la recherche et la
navigation sont ce qui doit être le plus soigné, et donc aussi le plus découpé.

---

## 3. Ce qu'on ne trouve **pas** dans l'arborescence

| Absent | Pourquoi |
|---|---|
| `Atlas.Core`, `Atlas.Data`, `Atlas.Scan`… | un seul projet : les dossiers suffisent |
| `Contracts/`, `Ports/`, `Abstractions/` | aucune interface pour un seul implémenteur |
| `Services/` | les ViewModels appellent `Data` directement |
| `Reports/` | fonction supprimée |
| `Search/` séparé de `Data/` | la recherche est du SQL : elle reste dans `Data` |
| `Indexing/`, moteur plein texte | hors V1 ; seulement si la mesure l'exige, dans `Data` |
| `Migrations/` (dossier de scripts) | le schéma est créé en une fois dans `Database.cs` |
| `Scripts/`, `tools/`, `build/` | publication = un `dotnet publish` documenté dans le README |
| `Interop/` | aucune signature P/Invoke en V1 |
| `Resources/fonts`, `Resources/icons` | Segoe UI et les icônes système suffisent |

---

## 4. Le projet de tests `tests/AtlasPc.Tests/`

Il est **conservé** parce qu'il teste des choses qui ne se vérifient pas à l'œil. Les
tests de la rubrique Fichiers sont les plus nombreux : c'est la fonction centrale.

| Test | Ce qu'il prouve |
|---|---|
| `Scanner_CalculeLesTotauxDeDossier` | les tailles remontées sont exactes |
| `Scanner_SauteLesPointsDeReparse` | aucune boucle infinie |
| `Scanner_EnregistreLesDossiersIllisibles` | un refus d'accès ne casse pas le scan |
| `Scanner_AnnuleSansPerdreLeLotEnCours` | l'annulation laisse un inventaire cohérent |
| `Scanner_ClotParRacine` | deux disques = deux statuts, jamais un statut global trompeur |
| `Scanner_AnnuleEtEcritLeLotEnCours` | à l'annulation, aucun fichier déjà vu n'est perdu |
| `Scanner_MarqueLesDossiersNonVisites` | après annulation, les dossiers restants sont en `cancelled_before` |
| `Search_TrouveParNomExactParPrefixeEtParSousChaine` | les trois cas renvoient l'ensemble attendu |
| `Search_NeLitPasLeDisque` | dossier analysé **supprimé après le scan** : la recherche renvoie toujours les mêmes résultats (preuve que seule la base est lue) |
| `Search_IgnoreLaCasseEtLesAccentsExacts` | comportement de `NOCASE` documenté et testé |
| `Search_PagineSansDoublonNiTrou` | `ORDER BY` stable avec `LIMIT/OFFSET` |
| `Search_FiltreParExtensionFamilleTailleDate` | chaque filtre et leur combinaison |
| `Search_RestreintAuDossierCourant` | le filtre d'emplacement n'inclut pas les frères |
| `Search_RespecteLaCouverture` | un disque `inaccessible` est signalé dans le résultat |
| `Browse_DonneLesEnfantsDUnDossier` | arbre correct, racines comprises |
| `Browse_FilDArianeRemonteALaRacine` | chemin reconstruit sans erreur |
| `Opener_RefuseUnCheminDisparu` | aucune tentative d'ouverture d'un fichier absent |
| `Opener_NeModifieRien` | garde-fou : aucune API d'écriture dans `Shell` |
| `Comparison_DistingueSuppressionEtZoneNonAnalysee` | **la règle la plus importante** |
| `Comparison_NApprouveAucuneSuppressionSurUnScanAnnule` | un disque `cancelled` ne produit que des disparitions non vérifiables |
| `Comparison_TraiteUnRenommageCommeRetraitPlusAjout` | même taille, même date, nom différent → 1 supprimé + 1 ajouté |
| `Comparison_DetecteAjoutSuppressionModification` | jeu de référence créé, modifié, supprimé |
| `Comparison_RefuseDesDisquesDifferents` | comparaison par disque, jamais silencieuse |
| `Software_FiltreSystemComponentEtParentKeyName` | filtrage du registre correct |
| `ProjectDetector_IgnoreNodeModules` | pas de faux projets |

Vingt-trois tests, dont dix-sept sur la rubrique Fichiers et la comparaison. Les tests utilisent des **dossiers
temporaires réels** et une base SQLite temporaire : aucune doublure, aucun faux système
de fichiers.

---

## 5. Détail des fichiers clés

| Fichier | Rôle | Taille attendue |
|---|---|---|
| `Data/Schema.sql` | les 10 tables et les index | ~130 lignes |
| `Data/Database.cs` | ouverture, `PRAGMA`, schéma, version | ~120 lignes |
| `Data/ScanStore.cs` | insertion par lots, transactions, clôture des racines, incidents d'annulation | ~200 lignes |
| `Data/BrowseQueries.cs` | disques, enfants, contenu, fil d'Ariane | ~150 lignes |
| `Data/SearchQueries.cs` | recherche, filtres, comptage, tri | ~220 lignes |
| `Data/ComparisonQueries.cs` | 4 requêtes + lecture | ~200 lignes |
| `Scanning/FileScanner.cs` | parcours d'une racine, totaux, progression, annulation | ~220 lignes |
| `Shell/ItemOpener.cs` | existence, ouverture, messages | ~80 lignes |
| `ViewModels/FilesViewModel.cs` | arbre, sélection, fil d'Ariane, disques | ~230 lignes |
| `ViewModels/SearchViewModel.cs` | saisie, filtres, pages, annulation | ~250 lignes |
| `Views/FilesView.xaml` | la vue la plus riche | ~260 lignes de XAML |

Aucun fichier ne devrait dépasser ~260 lignes. Au-delà, c'est le signe qu'une
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

**Distribution** : un dossier compressé contenant l'exécutable et la DLL SQLite. Aucun
installeur, aucun désinstalleur.

---

## 7. Conventions

- Identifiants en anglais, libellés d'interface et documentation en français.
- `PascalCase` pour les types et membres publics, `_camelCase` pour les champs privés.
- Un type public par fichier, nom du fichier = nom du type.
- `LangVersion` par défaut de `net48` (C# 7.3). Pas de polyfill, pas de `record`, pas
  de `init`.
- `.editorconfig` pour l'encodage et les fins de ligne seulement.
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
