# Livrable 4 — Architecture simplifiée et justification

> **Révision du 17 novembre 2026 (3ᵉ).** Ce document remplace l'architecture à sept
> projets du dossier initial. La **rubrique Fichiers est la fonction centrale** : elle
> structure l'architecture, la base et le plan. La vérification du 17/11 a durci le statut
> d'un disque interrompu (§ 5.4), précisé les trois cas de recherche (§ 6.4) et retiré une
> table (§ 8). Détail des suppressions : [`00-lisez-moi.md`](00-lisez-moi.md) § 5.

---

## 1. Six principes, et rien d'autre

1. **Retrouver un dossier ou un fichier est la raison d'être du logiciel.** Tout le
   reste — matériel, logiciels, outils, projets — est secondaire et ne doit jamais
   dégrader cette fonction.
2. **Un seul projet.** Aucune couche, aucun projet de bibliothèque, aucune interface
   abstraite qui n'ait au moins deux implémentations réelles.
3. **Une seule technologie** : WPF sur .NET Framework 4.8, cible unique `net48`.
4. **Un seul travailleur de scan**, écriture en base par lots.
5. **Lecture seule** sur les fichiers et sur Windows.
6. **Le code le plus court possible** pour chaque fonction : si une fonctionnalité tient
   en 30 lignes sans abstraction, elle fait 30 lignes sans abstraction.

Le critère d'arbitrage n'est plus « quelle est la meilleure pratique » mais :
**« qu'est-ce qui rend la recherche d'un fichier plus fiable, plus rapide et plus
simple à écrire ? »**

---

## 2. Décision technologique

| Option | Verdict | Raison |
|---|---|---|
| **WPF · .NET Framework 4.8 · `net48`** | **Retenue** | présent dans Windows 10 22H2, aucun runtime à installer, démarrage rapide sur disque mécanique, un seul projet, listes virtualisées et styles fournis |
| WPF · .NET 10 auto-contenu | Écartée | +150-250 Mo sur le disque, démarrage à froid plus lent, publication à gérer, aucun besoin fonctionnel |
| WinForms · `net48` | Écartée | plus léger, mais tableaux virtualisés et mise en page adaptative à écrire à la main |

**Pourquoi pas de multi-ciblage.** Le dossier initial proposait `net48;net10.0-windows`
pour « préparer une migration ». Complexité gratuite : polyfills, deux jeux de
dépendances, deux validations, pour un besoin qui n'existe pas.

**Une seule dépendance externe** : `System.Data.SQLite.Core` (SQLite est dans le
domaine public, le fournisseur ADO.NET aussi). Aucune bibliothèque d'interface, de MVVM,
de graphiques ou de JSON.

---

## 3. Un seul projet, découpé en dossiers

```
src/AtlasPc/
  Models/       objets de données (aucune logique)
  Data/         SQLite : schéma, écriture par lots, lectures, recherche, comparaison
  Scanning/     parcours des fichiers
  Collecting/   matériel, logiciels, outils, projets
  Shell/        ouverture d'éléments, presse-papiers  (5 commandes, 2 classes)
  ViewModels/   logique de présentation
  Views/        XAML
  Controls/     petits contrôles réutilisables (carte, barre, badge, pastille)
  Themes/       couleurs, typographie, styles
```

| Dossier | Responsabilité | Ce qu'il ne fait pas |
|---|---|---|
| `Models` | structures de données pures (`ComputerInfo`, `FileRow`, `SearchQuery`, `ComparisonResult`…) | pas d'accès disque, pas de SQL |
| `Data` | `Database` (ouverture, `PRAGMA`, schéma), `ScanStore` (écriture par lots), `BrowseQueries` (navigation), `SearchQueries` (recherche et filtres), `ComparisonQueries` | pas de logique métier |
| `Scanning` | `FileScanner` : parcours, totaux, progression, annulation | ne connaît ni WPF ni SQL (il reçoit un `Action` d'écriture de lot) |
| `Collecting` | quatre classes : `ComputerCollector`, `SoftwareCollector`, `ToolCollector`, `ProjectDetector` | pas d'interface, pas de fabrique |
| `Shell` | `ItemOpener` (ouvrir dans l'Explorateur / lancer un fichier), `ClipboardHelper` | ne modifie, ne déplace, ne supprime **jamais** |
| `ViewModels` | exposition des données aux vues, commandes | pas de SQL direct |
| `Views` | XAML uniquement | pas de logique |
| `Controls`, `Themes` | présentation | — |

**Règles, vérifiables en relecture :**

- aucun `using System.Windows` hors de `ViewModels`, `Views`, `Controls` ;
- aucun SQL hors de `Data` ;
- aucune API d'écriture de fichier ni de registre dans `Scanning`, `Collecting` et
  `Shell` ;
- aucune interface déclarée pour un seul implémenteur.

---

## 4. Un inventaire, en six temps

```
1. Créer la ligne `inventory` (status = running)
2. Collecter le matériel et Windows            → tables `computer`, `disk`, `volume`
3. Collecter logiciels, outils, projets        → tables `software`, `tool`, `project`
4. Pour chaque disque coché par l'utilisateur :
     4a. créer la ligne `scan_root` (status = running)
     4b. parcourir, écrire par lots            → tables `folder`, `file`, `scan_issue`
     4c. clôturer la racine : complete | partial | cancelled | inaccessible
5. Clôturer l'inventaire : totaux, status
6. Retour à l'interface, rubrique Fichiers ouverte
```

Les étapes 2 et 3 passent avant pour que l'utilisateur voie un résultat en quelques
secondes, mais elles sont **court-circuitables** : si le matériel échoue, le scan des
fichiers se lance quand même.

**Pourquoi plusieurs racines.** La recherche doit porter sur **tous les disques
inventoriés**. Un inventaire contient donc 1 à n racines (`C:`, `D:`…), chacune avec son
propre statut. C'est ce qui permet à l'interface de dire « `D:` partiellement
inventorié » au lieu de laisser croire à une couverture totale.

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
préfixe `\\?\`.

**Contingence, explicitement prévue et bornée.** Si les mesures montrent que le parcours
est trop lent (seuil : plus de 20 minutes pour `C:`), ou que les chemins longs échouent
en masse, alors seulement on remplace l'énumération par un `FindFirstFileExW` **dans une
seule classe** (`Scanning/NativeDirectoryEnumerator.cs`), sans toucher au reste.

### 5.2 Parcours

- profondeur d'abord, **un seul thread**, en récursion (quelques dizaines de niveaux) ;
- à chaque dossier rencontré : insertion d'une ligne `folder` **après** traitement des
  enfants, pour connaître sa taille totale ;
- à chaque fichier : ajout au lot courant ;
- lot écrit en base toutes les **500 entrées** ou toutes les **2 secondes** ;
- **un disque après l'autre** : l'interface peut déjà explorer `C:` pendant que `D:`
  n'est pas commencé, mais en l'annonçant.

### 5.3 Mémoire

| Poste | Ordre de grandeur |
|---|---|
| pile de récursion + chemins courants | < 1 Mo |
| lot en cours (500 entrées) | < 1 Mo |
| cache SQLite (`PRAGMA cache_size = -16000`) | 16 Mo |
| processus WPF au repos | 80-120 Mo (estimation) |
| **pic visé** | **< 200 Mo** |

Aucun fichier n'est conservé en mémoire : l'interface lit la base, jamais le scan. Le
cache SQLite passe de 8 à **16 Mo** parce que la recherche par nom repose sur un
parcours d'index, et que garder cet index en cache est précisément ce qui la rend
rapide.

### 5.4 Progression et annulation

- compteurs (fichiers, dossiers, octets, racine courante, chemin courant) publiés toutes
  les **500 ms** ;
- `CancellationToken` vérifié **à chaque dossier** ;
- **quatre statuts par racine**, et ils ne se confondent pas :

| Statut | Sens | Effet sur la comparaison |
|---|---|---|
| `complete` | parcours terminé, aucune zone en erreur | suppressions décidables |
| `partial` | parcours terminé, au moins une zone en erreur | suppressions décidables, garde `scan_issue` appliquée |
| `cancelled` | **parcours interrompu** par l'utilisateur | **aucune suppression confirmée** |
| `inaccessible` | racine absente ou refusée | **aucune suppression confirmée** |

- l'inventaire est `complete` seulement si toutes ses racines le sont ;
- un dossier illisible produit **une** ligne `scan_issue` (une par dossier) et
  n'interrompt rien ;
- **à l'annulation** : le lot en cours est écrit, un `scan_issue` de nature
  `cancelled_before` est enregistré pour le dossier en cours et pour les dossiers restants,
  puis la racine passe en `cancelled`. Ainsi, même interrompu, un scan ne peut pas faire
  croire à des fichiers disparus.

### 5.5 Choix assumés, avec leur conséquence

| Choix | Conséquence acceptée |
|---|---|
| pas de parallélisme | durée plus longue, mais disque mécanique préservé et code simple |
| pas de taille allouée | « espace occupé » = somme des tailles ; écart possible avec Windows |
| pas d'attributs stockés | impossible de filtrer « fichiers cachés » en V1 |
| chemins longs dépendants du système | un dossier trop profond est signalé en erreur plutôt qu'analysé |
| pas d'index plein texte en V1 | la recherche par sous-chaîne parcourt l'index des noms (voir § 6.4) |

Ces points sont **documentés dans l'application**, dans la rubrique Fichiers, pas cachés.

---

## 6. La rubrique Fichiers — fonction centrale

### 6.1 Les onze fonctions demandées, et leur traduction technique

| # | Fonction | Traduction |
|---|---|---|
| 1 | explorer les disques et dossiers en arbre | `folder.parent_path` → enfants d'un nœud, une requête par dépliage |
| 2 | afficher le contenu d'un dossier sélectionné | `SELECT … FROM file WHERE folder_path = ?` + sous-dossiers |
| 3 | recherche par nom complet ou partiel | deux niveaux : préfixe (index) et sous-chaîne (index des noms) |
| 4 | recherche sur tous les disques inventoriés | aucune clause de racine : recherche sur tout `inventory_id` |
| 5 | filtres extension / type / taille / date / emplacement | clauses SQL paramétrées, jamais de filtrage en mémoire |
| 6 | chemin complet de chaque résultat | colonne `Chemin`, milieu élidé, info-bulle + copie |
| 7 | nom, type, taille, date de modification | colonnes du tableau, plus `Dossier` |
| 8 | tri par colonne | `ORDER BY` sur colonnes indexées, `LIMIT` stable |
| 9 | nombre de fichiers et de sous-dossiers | `folder.files_count`, `folder.dirs_count` (calculés au scan) |
| 10 | ouvrir un dossier ou un fichier | `Process.Start` en `UseShellExecute`, après vérification d'existence |
| 11 | copier le chemin complet | `Clipboard.SetText` |

### 6.2 Navigation : trois niveaux, jamais plus

```
[Disques : C: (complet) · D: (partiel)]        ← sélecteur, 1 clic
C:\ › Users › william › Documents › Factures   ← fil d'Ariane, 1 clic par niveau
┌ arbre des dossiers (320 px) ┐ ┌ tableau du dossier courant ┐
```

- l'arbre ne charge un nœud qu'au dépliage (`parent_path = ?`), donc **aucune**
  arborescence complète en mémoire ;
- un nœud affiche `Nom` et la taille cumulée ; l'info-bulle donne fichiers, dossiers,
  date de dernière modification ;
- le fil d'Ariane se reconstruit en remontant `parent_path` (quelques requêtes, jamais
  de récursion) ;
- le double-clic sur une ligne de dossier descend d'un niveau ; sur un fichier, ouvre le
  fichier (§ 6.6).

### 6.3 Le tableau des résultats

- colonnes : **Nom · Type · Taille · Modifié · Chemin** (le chemin en dernier, plus
  large, élidé au milieu : `C:\Users\…\Factures\2026-03.pdf`) ;
- chargement **par pages de 200 lignes** (`LIMIT 200 OFFSET n` sur un tri stable) : rien
  n'est chargé en entier, la mémoire ne dépend pas du nombre de résultats ;
- tri sur toutes les colonnes ; `ORDER BY` toujours complété par `name` pour rester
  déterministe ;
- une pastille de couleur devant le nom indique la famille de type (voir
  [`09-interface.md`](09-interface.md) § 3) ;
- menu contextuel : *Ouvrir*, *Ouvrir l'emplacement*, *Copier le chemin*.

### 6.4 La recherche : trois cas, deux stratégies

L'utilisateur tape dans **une** zone de recherche. Le logiciel choisit la stratégie, sans
rien lui demander.

| Cas | Exemple | Stratégie | Objectif |
|---|---|---|---|
| **Nom exact** | `rapport-2026.pdf` | égalité sur l'index des noms — **court-circuit** : si elle renvoie un résultat, on s'arrête là | < 20 ms |
| **Préfixe** | `rapport` | `LIKE 'rapport%'` = recherche d'intervalle sur `ix_file_name` | < 100 ms |
| **Sous-chaîne** | `apport` | `LIKE '%apport%'` = parcours de l'index des noms (compact : le nom seul, pas la ligne) | < 1,5 s |

Le court-circuit du nom exact est une optimisation de requête, pas une option
d'interface : l'utilisateur tape un nom complet, la réponse est immédiate.

```sql
-- les deux niveaux, une seule requête paramétrée
SELECT f.id, f.name, f.ext, f.size_bytes, f.modified_at, f.folder_path
FROM file f
WHERE f.inventory_id = :inv
  AND f.name LIKE :motif              -- 'fact%'  (niveau 1)  ou  '%actu%' (niveau 2)
ORDER BY f.name
LIMIT 200 OFFSET :decalage;
```

- **Ordre de grandeur attendu** sur l'OptiPlex 780 : exact < 20 ms, préfixe < 100 ms,
  sous-chaîne ≈ 0,2-0,8 s pour 400 000 fichiers si l'index est en cache (16 Mo),
  davantage à froid. Mesure obligatoire à l'étape 3B, sur 100 000 / 400 000 / 1 000 000
  de fichiers (protocole : livrable 8 § 5).
- **Aucune recherche ne bloque l'interface** : elle s'exécute hors du thread d'affichage,
  avec un bouton d'annulation si elle dépasse 1 s.
- **Contingence bornée, comme pour le moteur** : si la mesure à l'étape 3B montre qu'une
  sous-chaîne dépasse **1,5 s** sur la machine cible, on ajoute une table virtuelle
  `file_name_fts` (FTS5, tokenizer `trigram`), alimentée en fin de scan. C'est **une
  seule table ajoutée et une seule requête modifiée** — rien d'autre. Décision prise sur
  mesure, consignée dans ce document.
- **Limite explicite à l'écran** : une saisie de moins de 3 caractères ne peut pas être
  servie par un index ; elle est acceptée, mais l'interface indique « recherche large,
  résultats limités ».

### 6.5 Les filtres

| Filtre | Implémentation | Index |
|---|---|---|
| Extension | `ext IN (:liste)` | `ix_file_ext` |
| Type (famille) | traduit en liste d'extensions **dans le code** (dictionnaire statique de ~30 familles) | idem |
| Taille | `size_bytes BETWEEN :min AND :max` | `ix_file_size` |
| Date de modification | `modified_at BETWEEN :debut AND :fin` (ISO-8601, comparaison de chaînes) | balayage filtré |
| Emplacement | `folder_path = :dossier` ou `LIKE :dossier \|\| '\%'` | `ix_file_folder` |

Les filtres sont **cumulables** et s'appliquent **en SQL**, jamais en mémoire. Un compteur
affiche « 1 204 résultats (500 affichés) » : le nombre total est obtenu par un
`COUNT(*)` sur la même clause, exécuté en parallèle de la page.

Aucun filtre « propriétaire », « attributs » ou « contenu » : non demandés et, pour le
contenu, hors périmètre.

### 6.6 Ouvrir un élément, copier son chemin

```csharp
// dossier : ouvrir dans l'Explorateur
Process.Start("explorer.exe", "\"" + chemin + "\"");
// fichier : ouvrir avec l'application associée
Process.Start(new ProcessStartInfo(chemin) { UseShellExecute = true });
// copier
Clipboard.SetText(chemin);
```

Trois garde-fous :

1. **vérifier que le chemin existe encore** (`File.Exists` / `Directory.Exists`) : un
   inventaire peut avoir plusieurs semaines. S'il a disparu, l'interface l'annonce
   (« ce fichier n'existe plus depuis l'inventaire du 03/11 ») au lieu de lancer une
   erreur système ;
2. **échapper le chemin** entre guillemets : aucun argument n'est concaténé à partir
   d'une saisie libre — le chemin vient de la base, pas du champ de recherche ;
3. **ATLAS PC ne modifie rien.** Ouvrir un fichier lance le programme associé, qui peut
   ensuite être modifié par l'utilisateur : c'est hors du contrôle d'ATLAS PC, et c'est
   écrit dans la note de bas de tableau.

### 6.7 Couverture : ne jamais paraître exhaustif

C'est le point le plus délicat de la rubrique, et il est traité à **six** endroits :

| Situation | Ce que l'interface affiche |
|---|---|
| recherche sur deux disques dont un partiel | bandeau ambre : « `D:` partiellement inventorié — 12 zones non analysées » |
| disque coché mais non analysé (erreur au démarrage du scan) | ligne rouge dans la liste des disques : « non accessible » |
| dossier non lisible dans le dossier courant | ligne d'erreur dans le tableau des sous-dossiers + compteur |
| inventaire annulé | badge `Annulé` et bandeau : « inventaire incomplet, résultats partiels » |
| recherche lancée pendant un scan | mention « inventaire en cours : résultats partiels » |
| disque au parcours interrompu | pastille rouge « parcours interrompu » dans la liste des disques |

Le nombre de résultats est toujours accompagné de la mention de couverture. Un total
n'est jamais présenté comme complet sans cette mention.


### 6.8 Aucune relecture du disque après le premier inventaire

C'est une propriété à garantir, pas une intention :

| Moment | Accès disque |
|---|---|
| démarrage de l'application | **aucun** — l'inventaire le plus récent est lu en base |
| ouverture de la rubrique Fichiers | **aucun** |
| navigation dans l'arbre, contenu d'un dossier | **aucun** |
| recherche, filtres, tri, pagination | **aucun** |
| affichage du tableau de bord et de l'Évolution | **aucun** |
| ouverture d'un élément (action explicite) | **une** vérification d'existence, rien de plus |
| scan | uniquement quand l'utilisateur le demande, sur **les disques qu'il a cochés** |

Un inventaire n'est donc jamais refait automatiquement : une base existante suffit à
toutes les recherches. C'est vérifié par un test qui supprime le dossier analysé après le
scan et vérifie que la recherche renvoie toujours les mêmes résultats.

### 6.9 Ce que la rubrique ne fait pas

Pas de recherche dans le contenu, pas d'OCR, pas de détection de doublons, pas de
renommage, déplacement ou suppression. Ce sont des exclusions **volontaires** de la V1.

---

## 7. Les collectes annexes

### 7.1 Ordinateur (WMI, une classe)

| Donnée | Source |
|---|---|
| fabricant, modèle | `Win32_ComputerSystem` |
| CPU (nom, cœurs) | `Win32_Processor` |
| RAM totale | `Win32_ComputerSystem.TotalPhysicalMemory` |
| GPU | `Win32_VideoController.Name` |
| BIOS (éditeur, version) | `Win32_BIOS` |
| Windows (nom, version, build, architecture) | `Win32_OperatingSystem` + registre `CurrentVersion` |
| volumes | `Win32_LogicalDisk` (type 3) |
| disques physiques (modèle, taille), rattachés à chaque volume | `Win32_DiskDrive` + `Win32_DiskDriveToDiskPartition` / `Win32_LogicalDiskToPartition` |

Une seule classe, sept requêtes, un `try/catch` par requête, un délai de 10 s par
requête (`EnumerationOptions.Timeout`). Toute valeur absente est `NULL` et affichée
`—` : jamais `0`, jamais une valeur inventée.

**Hors périmètre** : barrettes mémoire détaillées, partitions, chipset, carte mère,
Secure Boot, TPM, SMART, moniteurs, batterie, réseau.

### 7.2 Logiciels

Trois clés de registre `Uninstall` (HKLM 64 bits, HKLM `WOW6432Node`, HKCU), en
lecture seule. Champs retenus : nom, version, éditeur, emplacement, date
d'installation. Filtres : pas de nom → ignoré ; `SystemComponent = 1` → ignoré ;
`ParentKeyName` présent → ignoré.

**Aucune détection de logiciels portables** : trop incertaine.

### 7.3 Outils

Six outils, une seule classe : Python, pip, Node.js, npm, Git, Tesseract.

1. résolution du chemin : `PATH`, puis dossiers d'installation connus ;
2. version : `FileVersionInfo` (gratuit) ; à défaut, `--version` avec un délai de **3 s**,
   sans shell.

Statut par outil : `Détecté`, `Non détecté`, `Échec`. Un outil absent n'empêche jamais
les autres d'être détectés.

### 7.4 Projets

Détection **pendant** le parcours de fichiers, sans lecture supplémentaire :
`package.json` → Node.js ; `requirements.txt` ou `pyproject.toml` → Python. Un booléen
`is_git` indique la présence d'un dossier `.git` dans le même répertoire.

Deux garde-fous : ne rien détecter dans `node_modules`, `venv`, `.venv`, `site-packages`,
`dist`, `build`, `target` ; un dossier portant deux marqueurs produit **une** ligne avec
les deux types.

Aucune analyse de dépendances, aucun contenu de `.git` lu.

---

## 8. Persistance

Un fichier SQLite, `%LOCALAPPDATA%\ATLAS PC\atlas.db`, **dix tables** décrites dans
[`08-modele-sqlite.md`](08-modele-sqlite.md). La table `disk` du dossier précédent a été
fusionnée dans `volume` : elle n'était pas indispensable.

- une table `scan_root` par disque analysé : c'est elle qui porte le statut par
  emplacement et qui rend la recherche honnête ;
- comparaison des fichiers par **chemin complet normalisé** (dossier + nom), sans
  identifiant technique, sans hachage ;
- aucune statistique pré-calculée : les totaux sont des requêtes `SUM`/`COUNT` sur index ;
- **aucune rétention automatique** : l'utilisateur supprime ce qu'il veut ;
- aucun contenu de fichier, aucun hash de contenu, aucune image binaire.

---

## 9. Interface

Sept rubriques : Tableau de bord, Ordinateur, **Fichiers**, Logiciels, Outils,
Développement, Évolution. Détail et maquettes : [`09-interface.md`](09-interface.md).

La rubrique Fichiers a droit à plus de moyens que les autres : trois zones (sélecteur de
disques, arbre, tableau), une barre de recherche permanente, cinq filtres, un fil
d'Ariane et un menu contextuel. Les six autres rubriques restent des tableaux et des
fiches.

Aucune vue « Rapports » : la fonction d'export est **retirée**, pas reportée.

---

## 10. Lecture seule, par construction

| Mesure | Moyen |
|---|---|
| fichiers | jamais ouverts en écriture ; seules les métadonnées sont lues |
| ouverture d'un élément | `Process.Start` après contrôle d'existence ; aucun argument construit depuis une saisie libre |
| presse-papiers | seule écriture hors base, sans effet sur les fichiers |
| registre | `OpenSubKey(..., writable:false)` ; aucun `SetValue`, `DeleteSubKey` |
| WMI | requêtes de propriétés uniquement ; **jamais** `Win32_Product` |
| processus | uniquement `--version`, sans shell, avec délai |
| écritures | limitées à `%LOCALAPPDATA%\ATLAS PC\` |
| preuve | un **test** échoue si `Scanning`, `Collecting` ou `Shell` contient un appel d'écriture ; vérification au moniteur de processus en fin de projet |

---

## 11. Ce que l'architecture refuse, définitivement

| Élément | Motif |
|---|---|
| rapport HTML/CSV/JSON, export | retiré du périmètre |
| treemap, anneau, graphiques élaborés | coût de développement et de lisibilité ; barres suffisantes |
| scan MFT, mode administrateur | hors périmètre, exige l'élévation |
| parallélisme, multi-travailleurs | disque mécanique, simplicité |
| taille allouée, clusters, attributs | hors V1 |
| détection des logiciels portables | trop incertaine |
| rétention automatique | remplacée par la suppression manuelle |
| multi-ciblage .NET, couches, interfaces | complexité sans bénéfice |
| installeur, désinstalleur, service | un dossier et un exécutable suffisent |
| nettoyage, optimisation, corbeille | contraire à la règle de lecture seule |
| recherche dans le contenu, OCR, doublons | hors périmètre V1 |
| renommer / déplacer / supprimer des fichiers | contraire à la règle de lecture seule |
| **reconnaissance des déplacements et renommages** | décision de V1 : un renommage est présenté comme une suppression + un ajout, sans heuristique |

---

## 12. Résumé

> ATLAS PC est **un seul projet WPF en C# sur .NET Framework 4.8**, adossé à un fichier
> SQLite de dix tables **dont la fonction centrale est de retrouver un dossier ou un
> fichier** : arbre des disques, tableau paginé, recherche par nom (préfixe indexé, puis
> sous-chaîne sur l'index des noms), cinq filtres SQL, chemin complet, ouverture et copie
> du chemin. Le moteur parcourt les fichiers **avec les API standard de .NET, sur un seul
> thread**, écrit **par lots de 500**, ne conserve **aucun fichier en mémoire**, n'ouvre
> **jamais** un fichier en écriture, et **ne présente jamais un résultat comme complet**
> quand une zone n'a pas été analysée.
