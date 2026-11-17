# Livrable 1.1 — Rapport d'analyse : NeoTreescan

> Dépôt : `https://github.com/neortal-technologies/NeoTreescan`
> Commit examiné : `40acd1d27b7f8f255ef23341856d66125073dc10` (2026-04-22)
> Version : 1.0.0 · Licence : **Apache-2.0** · Analyse **statique** (code non exécuté)

---

## 1. Identification

| Élément | Valeur vérifiée | Source |
|---|---|---|
| Nature | Analyseur d'espace disque Windows (arborescence + treemap + export Excel) | `README.md` |
| Langage / UI | C# · WPF · .NET 9 | `src/NeoTreescan.App/NeoTreescan.App.csproj` (`net9.0-windows`, `UseWPF`) |
| Projets dans la solution | **2** projets de code (`NeoTreescan.Core`, `NeoTreescan.App`) + 1 outil annexe | `NeoTreescan.sln` lignes 8 et 10 |
| Volume de code | ≈ 1 990 lignes `.cs` + 3 fichiers `.xaml` (27 `.cs`) | `wc -l` sur `src/` |
| Dépendances NuGet | `ClosedXML 0.105.0`, `CommunityToolkit.Mvvm 8.4.2` | les deux `.csproj` |
| Tests | **Aucun projet de test**, aucun fichier de test | absence vérifiée dans `.sln` et dans tout le dépôt |
| Publication | `.exe` unique auto-contenu `win-x64`, compressé (≈ 65 Mo annoncés) | `NeoTreescan.App.csproj` (`PublishSingleFile`, `SelfContained`, `EnableCompressionInSingleFile`) |
| CI | `windows-latest`, `setup-dotnet 9.0.x`, build `/warnaserror`, publish | `.github/workflows/build.yml` |

La solution est petite, lisible d'un bout à l'autre en une heure : c'est sa principale
qualité pour une étude. Il n'y a ni couche de persistance, ni base de données, ni
service, ni couche réseau.

---

## 2. Architecture réelle du code

```
src/NeoTreescan.Core/            (net9.0-windows, aucune dépendance UI)
  Models/       FolderNode, FileEntry, ScanResult
  Scanning/     IDirectoryScanner, Win32DirectoryScanner, MftScanner, ScannerSelector
  Analysis/     Aggregators, SizeFormat
  Export/       ExcelExporter
  Interop/      NativeMethods, LongPath, Privilege
src/NeoTreescan.App/             (WPF, WinExe)
  MainWindow.xaml, AboutWindow.xaml
  Controls/TreemapControl.cs     (rendu custom)
  ViewModels/                   MainViewModel, FolderNodeViewModel, Rows
  Converters/, Resources/, Branding.cs, WindowTheme.cs, app.manifest
```

Séparation Core/App **réellement respectée** : `Core` ne référence aucun assembly WPF
(aucun `using System.Windows` dans `src/NeoTreescan.Core/`, vérifié). C'est le point
d'architecture le plus réutilisable du dépôt.

---

## 3. Mécanisme de scan — ce qui est vérifié

### 3.1 API Win32 employée

`src/NeoTreescan.Core/Interop/NativeMethods.cs` :

- `FindFirstFileExW` / `FindNextFileW`, importés de `kernel32.dll` en Unicode avec
  `SetLastError = true`.
- Niveau `FindExInfoBasic` (pas de nom 8.3, donc moins d'I/O) et drapeau
  `FIND_FIRST_EX_LARGE_FETCH = 2` : c'est le mode recommandé par Microsoft pour les
  énumérations locales massives. Vérifié ligne `public const int FIND_FIRST_EX_LARGE_FETCH = 2;`.
- Handle encapsulé dans `SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid`, donc pas
  de fuite de handle en cas d'exception. Bon point à reprendre.

### 3.2 Boucle de scan

`src/NeoTreescan.Core/Scanning/Win32DirectoryScanner.cs` :

| Aspect | Implémentation constatée |
|---|---|
| Parcours | Récursif profondeur d'abord (`ScanRecursive`). |
| Progression | `System.Threading.Timer` à **200 ms** qui publie un `ScanProgress` (fichiers, dossiers, octets, chemin courant, durée). |
| Annulation | `state.Ct.ThrowIfCancellationRequested()` **en entrée de chaque dossier** ; l'exception est capturée dans `ScanAsync` et positionne `result.Cancelled = true`. Granularité = un dossier, donc annulation quasi immédiate. |
| Parallélisme | **Seulement aux deux premiers niveaux** : `if (subdirs.Count > 1 && node.Depth < 2) Parallel.ForEach(..., MaxDegreeOfParallelism = Environment.ProcessorCount)`. Au-delà, récursion séquentielle. |
| Points de reparse | Détectés (`FILE_ATTRIBUTE_REPARSE_POINT`), **ajoutés à l'arbre mais jamais parcourus** : pas de boucle possible. |
| Erreurs | `GetLastWin32Error()` capturé, nœud marqué `HadErrors`, message horodaté ajouté à `ScanResult.Errors` sous verrou. |
| Taille allouée | **Estimée** par `RoundUpToBlock(size, 4096)` : bloc de 4 Kio codé en dur. |

### 3.3 Trois défauts techniques mesurables (vérifiés dans le code)

1. **Taille allouée fausse hors 4 Kio de cluster.** `RoundUpToBlock(size, 4096)`
   suppose des clusters de 4 096 octets. Sur un volume formaté en 512 o, 8 Kio ou
   64 Kio, ou pour un fichier compressé/sparse, le chiffre est erroné. Sur l'OptiPlex
   cible (disque probablement formaté par Windows 10, donc 4 Kio) l'écart est
   probablement nul, mais ce n'est pas garanti. La bonne source est
   `GetDiskFreeSpace` (taille de cluster réelle) et `GetCompressedFileSize`
   (allocation réelle). **À ne pas reprendre tel quel.**
2. **`Parallel.ForEach` dimensionné sur `ProcessorCount`.** Sur un bi-cœur, cela crée
   deux travailleurs qui vont solliciter le même disque mécanique en parallèle :
   contention de tête de lecture, pas de gain. Le code protège au moins contre
   l'explosion de threads en limitant le parallélisme aux deux premiers niveaux.
3. **Mémoire proportionnelle au nombre de fichiers, avec chemin dupliqué.**
   Voir § 4.

### 3.4 Chemins longs et UNC

`src/NeoTreescan.Core/Interop/LongPath.cs` : `Prefix()` ajoute `\\?\` (ou `\\?\UNC\`)
et normalise les séparateurs, `CombineNative()` concatène sans passer par `Path.Combine`
(qui réintroduirait la normalisation). L'application déclare aussi
`longPathAware = true` dans `src/NeoTreescan.App/app.manifest`, et le manifeste
demande `asInvoker` (pas d'élévation automatique) et `PerMonitorV2`. Ces trois
éléments sont exacts et directement réutilisables **comme connaissance**, pas comme
code.

### 3.5 Élévation

`src/NeoTreescan.Core/Interop/Privilege.cs` : `TryEnableBackupPrivilege()` sort
immédiatement si le processus n'est pas administrateur, sinon active
`SeBackupPrivilege`, `SeRestorePrivilege`, `SeSecurityPrivilege` via
`AdjustTokenPrivileges`, et vérifie `ERROR_NOT_ALL_ASSIGNED` (1300) par
`Marshal.GetLastWin32Error() == 0`. C'est correct et c'est exactement le mécanisme
décrit dans le README. `MainViewModel.RestartAsAdmin()` relance le processus avec
`Verb = "runas"` — **sur action de l'utilisateur**, jamais automatiquement.
Conclusion pour ATLAS PC : l'absence d'admin est bien un choix viable ; les zones
protégées seront simplement signalées.

### 3.6 Le scanner MFT est un leurre assumé

`src/NeoTreescan.Core/Scanning/MftScanner.cs` : la classe **délègue entièrement** à
`Win32DirectoryScanner` puis renomme le résultat `"MFT(fallback)"`. Le commentaire
d'en-tête le dit explicitement (« Placeholder type… is not implemented here »). Les
constantes `FSCTL_ENUM_USN_DATA` et `FSCTL_QUERY_USN_JOURNAL` existent dans
`NativeMethods.cs` mais **ne sont utilisées nulle part**. `ScannerSelector.cs` ne
renvoie ce scanner que si : chemin non-UNC **et** administrateur **et** volume NTFS
**et** racine = volume entier. Autrement dit : dans la configuration d'ATLAS PC
(sans admin), ce chemin n'est jamais emprunté. À retenir comme **avertissement** :
le README peut laisser croire à un scan MFT performant ; le code ne le fournit pas.

---

## 4. Gestion mémoire — le point critique pour 6 Go

Modèle : **arbre d'objets complet en mémoire**, construit pendant le scan et conservé
après (`MainViewModel.LastResult`).

`src/NeoTreescan.Core/Models/FolderNode.cs` : chaque nœud porte `Name`, `FullPath`,
`Parent`, `List<FolderNode>`, `List<FileEntry>`, 8 compteurs `long`/`int`, date,
drapeaux.

`src/NeoTreescan.Core/Models/FileEntry.cs` :

```csharp
public readonly record struct FileEntry(
    string Name, string FullPath, long Size, long Allocated,
    DateTime LastWriteUtc, FileAttributes Attributes)
```

**Chaque fichier porte deux chaînes** : son nom *et* son chemin complet. Or le chemin
complet est redondant : il est déjà déterminé par la position dans l'arbre. Sur un
volume de 400 000 fichiers avec un chemin moyen de 90 caractères, le seul champ
`FullPath` coûte ≈ 400 000 × (22 + 2 × 90) ≈ **81 Mo** de chaînes, auxquelles
s'ajoutent les en-têtes d'objet, les `List<>` par dossier, les noms de fichiers et
les objets `FolderNode`. **Estimation** (non mesurée) : 250 à 400 Mo pour un disque
de 400 000 à 600 000 fichiers.

Aggravant : `src/NeoTreescan.Core/Analysis/Aggregators.cs` reparcourt **tout l'arbre
quatre fois** après le scan (`ByFileType`, `ByAge`, `BySize`, `TopFiles` appelés
séparément dans `MainViewModel.PopulateAnalysis`). Ce n'est pas un problème de
mémoire mais de latence perçue en fin de scan ; sur un CPU ancien, quatre parcours de
600 000 éléments se sentent.

Côté UI, bonne pratique à retenir : `src/NeoTreescan.App/ViewModels/FolderNodeViewModel.cs`
charge les enfants **paresseusement** (placeholder `null!` puis remplissage à la
première ouverture, tri décroissant par taille). Le `TreeView` ne matérialise donc
jamais tout l'arbre. C'est le bon réflexe pour ATLAS PC.

**Verdict mémoire : inadapté au PC cible, et inutile pour ATLAS PC**, qui doit de
toute façon écrire les fichiers en base au fil de l'eau.

---

## 5. Progression, annulation, reprise

- Progression : compteurs + chemin courant toutes les 200 ms. Pas d'ETA, pas de
  pourcentage (impossible sans connaître le total à l'avance).
- Annulation : propre, granulaire au dossier, résultat partiel conservé
  (`ScanResult.Cancelled = true`) et affiché « Scan cancelled. »
  (`MainViewModel.ScanAsync`).
- **Reprise après interruption : inexistante.** Aucune persistance : fermer
  l'application perd le scan. C'est précisément le manque que l'historique ATLAS PC
  doit combler.

---

## 6. Accès Windows, stockage et visualisations

| Sujet | Constat | Chemin |
|---|---|---|
| Matériel / OS | **Rien.** Aucune requête WMI, aucun accès registre | absence vérifiée |
| Logiciels installés | **Rien** | absence vérifiée |
| Outils de développement | **Rien** | absence vérifiée |
| Projets | **Rien** | absence vérifiée |
| Stockage des résultats | **Aucun** (pas de base, pas de fichier de session) | absence vérifiée |
| Exports | **Excel uniquement** (`.xlsx`, 6 feuilles) via ClosedXML | `src/NeoTreescan.Core/Export/ExcelExporter.cs` |
| Graphes | Treemap « squarified » maison, dessiné dans `OnRender` | `src/NeoTreescan.App/Controls/TreemapControl.cs` (314 lignes) |
| Limite du treemap | `maxDepth: 4` en dur ; la liste `_hits` est **reconstruite à chaque rendu** ; pas de cache de layout | même fichier |
| Barres de répartition | Barres proportionnelles en XAML dans les onglets | `MainWindow.xaml`, `Rows.cs` |
| Thème | Sombre + barre de titre sombre via DWM | `WindowTheme.cs` |

Le treemap est une belle démonstration, mais c'est du code d'affichage dense (layout +
hit-testing + libellés) : le reprendre « à l'identique » coûterait cher pour un
bénéfice incertain sur 1440 × 900 (voir `docs/09-interface.md` § 6).

---

## 7. Licence et implications

- `LICENSE` = **Apache License 2.0**, `NOTICE` présent et correctement renseigné
  (produit, société, composants tiers : ClosedXML MIT, CommunityToolkit.Mvvm MIT,
  .NET/WPF MIT).
- `CHANGELOG.md` indique un changement de licence **MIT → Apache-2.0** dans la section
  `[Unreleased]`. Conséquence pratique : les versions antérieures au changement
  restent sous MIT ; la version examinée est Apache-2.0. En cas de réutilisation, il
  faut citer la version de départ et conserver `LICENSE` + `NOTICE`.
- Apache-2.0 autorise la réutilisation dans un logiciel propriétaire ou sous autre
  licence libre, avec attribution, conservation des mentions, et signalement des
  modifications. Elle apporte en plus une concession de brevets explicite.
- **Ce qui n'est pas permis** : utiliser les marques « NeoTreescan » / « Neortal ».

Détail complet : `docs/06-licences-et-risques.md`.

---

## 8. Ce qui est réutilisable, ce qui ne l'est pas

### À retenir comme **inspiration** (idées, pas code)

1. La séparation logique/interface — ATLAS PC la garde, mais par **dossiers** dans un
   seul projet (`Scanning/` ignore WPF).
2. ~~`FindFirstFileExW` + `FIND_FIRST_EX_LARGE_FETCH`~~ — **écarté** en V1 : l'API
   standard .NET suffit, et cette interop reste le plan de repli si la mesure l'exige.
3. ~~Le préfixe `\\?\`~~ — écarté avec l'interop ; les chemins longs échouent
   désormais **explicitement** dans `scan_issue` plutôt que silencieusement.
4. `IProgress<ScanProgress>` + `CancellationToken` : le contrat d'un moteur annulable,
   conservé sous la forme d'un `Action<ScanProgress>` et d'un jeton.
5. Chargement paresseux des nœuds dans l'arbre.
6. Le manifeste : `asInvoker`, `PerMonitorV2`, `longPathAware`.

### À ne **pas** reprendre

| Élément | Pourquoi |
|---|---|
| Modèle `FolderNode` + `FileEntry` en mémoire | Mémoire non bornée, chemin dupliqué par fichier |
| `RoundUpToBlock(…, 4096)` | Faux hors cluster 4 Kio |
| `MaxDegreeOfParallelism = ProcessorCount` | Contre-productif sur disque mécanique bi-cœur |
| `MftScanner` | C'est un stub ; ne pas croire au MFT |
| Treemap maison | Coût de développement élevé pour un besoin non exigé |
| Le découpage en **projets** séparés (`Core`, `App`) | ATLAS PC reste sur un projet unique |
| Cible `net9.0-windows` | Runtime **hors support depuis le 10/11/2026** |
| Export Excel comme seul format | ATLAS PC ne comporte **aucun** export |

---

## 9. Éléments **non vérifiés**

- Comportement réel sur un OptiPlex 780 (aucune exécution).
- Exactitude du chiffre « 65 Mo » et du temps de démarrage à froid de l'EXE
  auto-contenu sur disque mécanique.
- Comportement du treemap au-delà de quelques dizaines de milliers d'éléments.
- Qualité de l'export Excel sur de très gros volumes (ClosedXML est gourmand en
  mémoire sur de grandes feuilles).
- Le contenu des captures d'écran `screenshots/*.png` n'a pas été analysé
  visuellement au-delà de leur existence.

---

## 10. Note pour ATLAS PC

NeoTreescan est le plus proche des quatre dépôts de la forme visée (WPF, séparation
écran/logique, manifeste soigné) et le **pire modèle de données** pour la machine
cible. La synthèse est simple : **reprendre la forme, pas le fond** — et surtout pas le
runtime.

> **Révision du 17/11/2026 — périmètre simplifié.** NeoTreescan n'est plus le modèle
> d'application retenu : le livrable 4 part d'**un seul projet** et, surtout, de l'**API
> standard .NET** plutôt que de son interop Win32. Ce qui reste emprunté se limite à
> trois idées : organisation des écrans, manifeste (`asInvoker`, `PerMonitorV2`, chemins
> longs) et comptage des dossiers illisibles. Son export Excel est hors périmètre.

Voir notation comparée : `docs/05-tableau-comparatif.md`.
