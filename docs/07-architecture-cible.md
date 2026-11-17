# Livrable 4 — Architecture simplifiée et justification

> **Révision du 17 novembre 2026.** Ce document remplace l'architecture à sept
> projets du dossier initial. Tout ce qui n'est pas nécessaire à la V1 a été retiré :
> le détail des suppressions est dans [`00-lisez-moi.md`](00-lisez-moi.md) § 5.

---

## 1. Cinq principes, et rien d'autre

1. **Un seul projet.** Aucune couche, aucun projet de bibliothèque, aucune interface
   abstraite qui n'ait au moins deux implémentations réelles.
2. **Une seule technologie** : WPF sur .NET Framework 4.8, cible unique `net48`.
3. **Un seul travailleur de scan**, écriture en base par lots.
4. **Lecture seule** sur les fichiers et sur Windows.
5. **Le code le plus court possible** pour chaque fonction : si une fonctionnalité
   tient en 30 lignes sans abstraction, elle fait 30 lignes sans abstraction.

Le critère d'arbitrage n'est plus « quelle est la meilleure pratique » mais :
**« qu'est-ce qui rend ce logiciel plus simple à écrire, à comprendre et à corriger ? »**

---

## 2. Décision technologique

| Option | Verdict | Raison |
|---|---|---|
| **WPF · .NET Framework 4.8 · `net48`** | **Retenue** | présent dans Windows 10 22H2, aucun runtime à installer, démarrage rapide sur disque mécanique, un seul projet, WPF fournit listes virtualisées et styles |
| WPF · .NET 10 auto-contenu | Écartée | +150-250 Mo sur le disque, démarrage à froid plus lent, publication à gérer, aucun besoin fonctionnel |
| WinForms · `net48` | Écartée | plus léger, mais tableaux virtualisés et mise en page adaptative à écrire à la main |

**Pourquoi pas de multi-ciblage.** Le dossier initial proposait `net48;net10.0-windows`
pour « préparer une migration ». C'était une complexité gratuite : elle impose des
polyfills, deux jeux de dépendances et deux validations, pour un besoin qui n'existe
pas. Si la machine est un jour remplacée, la bascule se fera en une décision, pas en
permanence.

**Une seule dépendance externe** : `System.Data.SQLite.Core` (SQLite est dans le
domaine public, le fournisseur ADO.NET aussi). Aucune bibliothèque d'interface, de MVVM,
de graphiques ou de JSON.

---

## 3. Un seul projet, découpé en dossiers

```
src/AtlasPc/
  Models/       objets de données (aucune logique)
  Data/         SQLite : création du schéma, écriture par lots, lectures, comparaison
  Scanning/     parcours des fichiers
  Collecting/   matériel, logiciels, outils, projets
  ViewModels/   logique de présentation
  Views/        XAML
  Controls/     petits contrôles réutilisables (carte, barre, badge)
  Themes/       couleurs, typographie, styles
```

| Dossier | Responsabilité | Ce qu'il ne fait pas |
|---|---|---|
| `Models` | structures de données pures (`ComputerInfo`, `FileEntry`, `ComparisonResult`…) | pas d'accès disque, pas de SQL |
| `Data` | une classe `Database` (ouverture, `PRAGMA`, schéma) et trois classes de requêtes | pas de logique métier |
| `Scanning` | `FileScanner` : parcours, totaux, progression, annulation | ne connaît ni WPF ni SQL (il reçoit un `Action` d'écriture de lot) |
| `Collecting` | quatre classes : `ComputerCollector`, `SoftwareCollector`, `ToolCollector`, `ProjectDetector` | pas d'interface, pas de fabrique |
| `ViewModels` | exposition des données aux vues, commandes | pas de SQL direct |
| `Views` | XAML uniquement | pas de logique |
| `Controls`, `Themes` | présentation | — |

**Règles, vérifiables en relecture :**

- aucun `using System.Windows` hors de `ViewModels`, `Views`, `Controls` ;
- aucun SQL hors de `Data` ;
- aucune API d'écriture de fichier ni de registre dans `Scanning` et `Collecting` ;
- aucune interface déclarée pour un seul implémenteur.

---

## 4. Un inventaire, en cinq étapes

```
1. Créer la ligne `inventory` (status = running)
2. Collecter le matériel et Windows            → table `computer`, `disk`, `volume`
3. Collecter logiciels, outils, projets        → tables `software`, `tool`, `project`
4. Parcourir les fichiers, écrire par lots     → tables `folder`, `file`, `scan_issue`
5. Clôturer : totaux, status complete/partial  → retour à l'interface
```

L'étape 4 est la seule longue. Les étapes 2 et 3 s'exécutent avant pour que
l'utilisateur voie un résultat en quelques secondes.

---

## 5. Le moteur de fichiers

### 5.1 API utilisée : le standard .NET

```csharp
var di = new DirectoryInfo(chemin);
foreach (FileSystemInfo info in di.EnumerateFileSystemInfos())
{
    if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;   // jamais suivi
    if (info is DirectoryInfo sousDossier) { ... }
    else { var f = (FileInfo)info; /* f.Length, f.LastWriteTimeUtc */ }
}
```

**Pourquoi c'est le bon choix ici :**
- `EnumerateFileSystemInfos` remplit les objets `FileSystemInfo` **à partir du tampon
  d'énumération** du système : `Length` et `LastWriteTimeUtc` sont déjà en mémoire,
  sans appel supplémentaire par fichier ;
- aucune signature P/Invoke, aucune structure à déclarer, aucun handle à libérer ;
- le code tient en une boucle de 40 lignes, lisible et testable.

**Ce qui est abandonné par rapport au dossier initial** : `FindFirstFileExW`,
`FIND_FIRST_EX_LARGE_FETCH`, les `SafeHandle`, les structures `WIN32_FIND_DATA`, le
préfixe `\\?\`. Tout cela était justifié pour un analyseur professionnel ; ici, c'est
du code à maintenir pour un gain non démontré.

**Contingence, explicitement prévue et bornée.** Si et seulement si les mesures de
l'étape 3 montrent que le parcours est trop lent (seuil : plus de 20 minutes pour le
volume `C:`), ou que les chemins longs échouent en masse, alors seulement on remplace
l'énumération par un `FindFirstFileExW` **dans une seule classe**
(`Scanning/NativeDirectoryEnumerator.cs`), sans toucher au reste. Cette décision se
prend sur mesure, pas par principe.

### 5.2 Parcours

- profondeur d'abord, **un seul thread**, en récursion (la profondeur suit celle de
  l'arborescence, quelques dizaines de niveaux) ;
- à chaque dossier rencontré : insertion d'une ligne `folder` **après** traitement des
  enfants, pour connaître sa taille totale ;
- à chaque fichier : ajout au lot courant ;
- lot écrit en base toutes les **500 entrées** ou toutes les **2 secondes**.

### 5.3 Mémoire

| Poste | Ordre de grandeur |
|---|---|
| pile de récursion + chemins courants | < 1 Mo |
| lot en cours (500 entrées) | < 1 Mo |
| cache SQLite (`PRAGMA cache_size = -8000`) | 8 Mo |
| processus WPF au repos | 80-120 Mo (estimation) |
| **pic visé** | **< 200 Mo** |

Aucun fichier n'est conservé en mémoire : l'interface lit la base, jamais le scan.

### 5.4 Progression et annulation

- compteurs (fichiers, dossiers, octets, chemin courant) publiés toutes les **500 ms** ;
- `CancellationToken` vérifié **à chaque dossier** ;
- trois issues : `complete`, `partial` (arrêt demandé, données conservées),
  `cancelled` (rien d'exploitable, ligne supprimée) ;
- un dossier illisible produit **une** ligne `scan_issue` et n'interrompt rien.

### 5.5 Choix assumés, avec leur conséquence

| Choix | Conséquence acceptée |
|---|---|
| pas de parallélisme | durée plus longue, mais disque mécanique préservé et code simple |
| pas de taille allouée | « espace occupé » = somme des tailles de fichiers ; écart possible avec Windows sur les fichiers compressés/sparse |
| pas d'attributs stockés | impossible de filtrer « fichiers cachés » en V1 |
| chemins longs dépendants du système | un dossier trop profond peut être signalé en erreur plutôt qu'analysé |

Ces quatre points sont **documentés dans l'application** (rubrique Fichiers, note de
bas de tableau), pas cachés.

---

## 6. Les collectes annexes

### 6.1 Ordinateur (WMI, une classe)

| Donnée | Source |
|---|---|
| fabricant, modèle | `Win32_ComputerSystem` |
| CPU (nom, cœurs) | `Win32_Processor` |
| RAM totale | `Win32_ComputerSystem.TotalPhysicalMemory` |
| GPU | `Win32_VideoController.Name` |
| BIOS (éditeur, version) | `Win32_BIOS` |
| Windows (nom, version, build, architecture) | `Win32_OperatingSystem` + registre `CurrentVersion` |
| disques physiques | `Win32_DiskDrive` |
| volumes | `Win32_LogicalDisk` (type 3) |

Une seule classe, sept requêtes, un `try/catch` par requête, un délai de 10 s par
requête (`EnumerationOptions.Timeout`). Toute valeur absente est `NULL` et affichée
`—` : jamais `0`, jamais une valeur inventée.

**Reporté** (retiré du périmètre) : barrettes mémoire détaillées, partitions,
chipset, carte mère, Secure Boot, TPM, SMART, moniteurs, batterie, réseau.

### 6.2 Logiciels

Trois clés de registre `Uninstall` (HKLM 64 bits, HKLM `WOW6432Node`, HKCU), en
lecture seule. Champs retenus : nom, version, éditeur, emplacement, date
d'installation. Filtres : pas de nom → ignoré ; `SystemComponent = 1` → ignoré ;
`ParentKeyName` présent → ignoré (mises à jour).

**Aucune détection de logiciels portables.** C'était l'élément le plus incertain du
périmètre : heuristique, faux positifs, explication à l'utilisateur. Retiré.

### 6.3 Outils

Six outils, une seule classe : Python, pip, Node.js, npm, Git, Tesseract.

1. résolution du chemin : `PATH`, puis dossiers d'installation connus ;
2. version : `FileVersionInfo` (gratuit) ; à défaut, exécution de `--version` avec un
   délai de **3 s**, sans shell.

Statut par outil : `Détecté`, `Non détecté`, `Échec`. Un outil absent n'empêche jamais
les autres d'être détectés.

### 6.4 Projets

Détection **pendant** le parcours de fichiers, sans lecture supplémentaire :
`package.json` → Node.js ; `requirements.txt` ou `pyproject.toml` → Python. Un booléen
`is_git` indique la présence d'un dossier `.git` dans le même répertoire.

Deux garde-fous : ne rien détecter à l'intérieur de `node_modules`, `venv`, `.venv`,
`site-packages`, `dist`, `build`, `target` ; un dossier portant deux marqueurs produit
**une** ligne avec les deux types.

Aucune analyse de dépendances, aucun contenu de `.git` lu.

---

## 7. Persistance

Un fichier SQLite, `%LOCALAPPDATA%\ATLAS PC\atlas.db`, **dix tables** décrites dans
[`08-modele-sqlite.md`](08-modele-sqlite.md).

- comparaison des fichiers par **chemin complet normalisé** (dossier + nom), sans
  identifiant technique, sans hachage ;
- aucune statistique pré-calculée : les totaux du tableau de bord sont des requêtes
  `SUM`/`COUNT` exécutées à l'ouverture, sur des index ;
- **aucune rétention automatique** : l'utilisateur voit la liste des inventaires, leur
  taille, et supprime ce qu'il veut ;
- aucune image binaire, aucun contenu de fichier, aucun hash de contenu.

---

## 8. Interface

Sept rubriques : Tableau de bord, Ordinateur, Fichiers, Logiciels, Outils,
Développement, Évolution. Détail et maquette : [`09-interface.md`](09-interface.md).

Aucune vue « Rapports » : la fonction d'export est **retirée**, pas reportée.

---

## 9. Lecture seule, par construction

| Mesure | Moyen |
|---|---|
| fichiers | jamais ouverts en écriture ; seules les métadonnées sont lues |
| registre | `OpenSubKey(..., writable:false)` ; aucun `SetValue`, `DeleteSubKey` |
| WMI | requêtes de propriétés uniquement ; **jamais** `Win32_Product` |
| processus | uniquement `--version`, sans shell, avec délai |
| écritures | limitées à `%LOCALAPPDATA%\ATLAS PC\` |
| preuve | un **test** échoue si `Scanning` ou `Collecting` contient un appel d'écriture ; vérification au moniteur de processus en fin de projet |

---

## 10. Ce que l'architecture refuse, définitivement

| Élément | Motif |
|---|---|
| rapport HTML/CSV/JSON, export | retiré du périmètre |
| treemap, anneau, graphiques élaborés | coût de développement et de lisibilité ; barres suffisantes |
| scan MFT, mode administrateur | hors périmètre, exige l'élévation |
| parallélisme, multi-travailleurs | disque mécanique, simplicité |
| taille allouée, clusters, attributs | reportés hors V1 |
| détection des logiciels portables | trop incertain |
| rétention automatique | remplacée par la suppression manuelle |
| multi-ciblage .NET, couches, interfaces | complexité sans bénéfice |
| installeur, désinstalleur, service | un dossier et un exécutable suffisent |
| nettoyage, optimisation, corbeille | contraire à la règle de lecture seule |

---

## 11. Résumé

> ATLAS PC est **un seul projet WPF en C# sur .NET Framework 4.8**, adossé à un
> fichier SQLite de dix tables, dont le moteur parcourt les fichiers **avec les API
> standard de .NET, sur un seul thread**, écrit **par lots de 500**, ne conserve
> **aucun fichier en mémoire**, n'ouvre **jamais** un fichier en écriture, et
> n'annonce une disparition de fichier que si la zone a bien été analysée.

Cette phrase remplace la version à sept projets du dossier initial. Elle est plus
courte parce que le logiciel l'est aussi.
