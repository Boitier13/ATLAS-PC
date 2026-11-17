# ATLAS PC — Dossier technique préalable (V1 simplifiée)

**Étude technique, sans programmation.**
Version 2.2 — 17 novembre 2026. Remplace les versions 1.0, 2.0 et 2.1 du même jour.

---

## 1. Objet et périmètre

Ce dossier prépare un logiciel Windows d'inventaire local : matériel, Windows,
fichiers, logiciels installés, outils de développement, projets Python/Node.js, avec
historique et comparaison de deux inventaires.

**Machine cible** : Dell OptiPlex 780 · Windows 10 22H2 x64 · 6 Go de RAM · disque dur
mécanique · 1440 × 900 · hors ligne · gratuit · sans droits administrateur.

**Deux consignes ont façonné ce dossier.**

1. *Simplifier* (version 2.0) : architecture ramenée à un seul projet, base réduite,
   plan resserré. Ce qui a été retiré l'a été **franchement** — supprimé, pas déplacé
   dans une rubrique « plus tard ». Détail et motifs au § 5.
2. *Faire de la rubrique Fichiers la fonction centrale* (version 2.1) : explorer,
   **rechercher**, filtrer, ouvrir, copier. Conséquence : **une** table de plus
   (`scan_root`, statut par disque), **deux** index de recherche, une étape de
   développement supplémentaire (3A/3B) et une interface plus riche pour cette seule
   rubrique. Détail au § 5.9.

Le **dossier** est complet et validé. Le **développement a commencé** :

- **étape 1** (coquille et base de données) : **validée sur Windows 10 le 17/11** —
  compilation sans erreur ni avertissement, sept rubriques navigables, historique
  persistant ;
- **étape 2** (matériel, Windows et volumes, 10 appels WMI en lecture seule) :
  **validée sur Windows 10 le 17/11** — mémoire installée et utilisable distinctes,
  persistance confirmée.

Le suivi, les décisions et les guides de recette sont dans
[`14-journal-de-developpement.md`](14-journal-de-developpement.md). L'étape 3A n'est pas
commencée. Aucun dépôt étudié n'a été modifié.

---

## 2. Méthode

| Étape | Ce qui a été fait |
|---|---|
| Clonage | les quatre dépôts clonés en profondeur 1 et lus sur disque |
| Lecture | README, licences, arborescence, moteur de scan, modèles, persistance, exports, interface |
| Vérification | chaque constat cite un **chemin exact** et, quand c'est utile, une ligne |
| Mesure | **aucune** : code non compilé, non exécuté, aucune performance mesurée |

**Limite à garder en tête** : l'analyse est **statique**, réalisée sous Linux. Aucun
binaire Windows n'a été lancé. Tout chiffre de durée ou de mémoire est une
**estimation** clairement signalée.

### Références exactes des dépôts examinés

| Dépôt | Commit | Date | Version | Licence |
|---|---|---|---|---|
| `neortal-technologies/NeoTreescan` | `40acd1d2` | 2026-04-22 | 1.0.0 | Apache-2.0 |
| `rodrigokiller/diskplot` | `8787cf6b` | 2026-10-17 | 0.4.0 | MIT |
| `TheLouisMahdi/louis-mahdi-system-inspector` | `420b20a6` | 2026-07-30 | 2.2.0 | MIT |
| `windirstat/windirstat` | `d4f14bf7` | 2026-11-15 | 2.10.0 | GPL-3.0-or-later |

---

## 3. Sommaire

| # | Livrable demandé | Fichier |
|---|---|---|
| — | Index, synthèse, **ce qui a été supprimé** | ce document |
| 1 | Analyse des quatre dépôts | [`01`](01-analyse-neotreescan.md) · [`02`](02-analyse-diskplot.md) · [`03`](03-analyse-system-inspector.md) · [`04`](04-analyse-windirstat.md) |
| 2 | Tableau comparatif | [`05-tableau-comparatif.md`](05-tableau-comparatif.md) |
| 3 | Licences et risques de réutilisation | [`06-licences-et-risques.md`](06-licences-et-risques.md) |
| 4 | Architecture simplifiée | [`07-architecture-cible.md`](07-architecture-cible.md) |
| 5 | Modèle SQLite réduit | [`08-modele-sqlite.md`](08-modele-sqlite.md) |
| 6 | Interface (7 rubriques) | [`09-interface.md`](09-interface.md) + `assets/maquette-fichiers.png` et `assets/maquette-atlas-pc.png` |
| 7 | Arborescence du projet | [`10-arborescence-projet.md`](10-arborescence-projet.md) |
| 8 | Plan en cinq étapes | [`11-plan-de-developpement.md`](11-plan-de-developpement.md) |
| 9 | Risques, limites, points à vérifier | [`12-risques-et-limites.md`](12-risques-et-limites.md) |
| — | **Vérifications de la v2.1** : constats et corrections | [`13-verifications.md`](13-verifications.md) |
| — | **Journal de développement** (le code commence) | [`14-journal-de-developpement.md`](14-journal-de-developpement.md) |

---

## 4. Synthèse et décision

### 4.1 Le périmètre final, en une table

| Rubrique | Contenu de la V1 |
|---|---|
| **Tableau de bord** | 4 chiffres, répartition par dossier, types de fichiers, liste des inventaires |
| **Ordinateur** | fabricant, modèle, CPU, RAM, GPU, BIOS, Windows, disques, volumes |
| **Fichiers** *(fonction centrale)* | **exploration** en arbre des disques et dossiers · **contenu** du dossier sélectionné · **recherche** par nom complet ou partiel sur tous les disques · **filtres** extension / type / taille / date / emplacement · **tri** par colonne · **chemin complet** · nombre de fichiers et de sous-dossiers · **ouvrir** un élément · **copier** son chemin · statut de couverture toujours affiché |
| **Logiciels** | logiciels déclarés au registre (nom, version, éditeur, emplacement, date) |
| **Outils** | Python, pip, Node.js, npm, Git, Tesseract : chemin, version, statut |
| **Développement** | projets Python et Node.js par fichiers caractéristiques, indicateur Git |
| **Évolution** | deux inventaires : ajoutés, supprimés, modifiés, espace occupé |

### 4.2 Décision technique

| Sujet | Choix | Raison dominante |
|---|---|---|
| Projet | **un seul projet WPF**, dossiers par responsabilité | moins de couches, moins de fichiers, plus lisible |
| Runtime | **.NET Framework 4.8, cible unique `net48`** | présent dans Windows 10 22H2, aucun runtime à installer, démarrage rapide sur disque dur |
| Base | **SQLite, 10 tables**, un seul paquet NuGet | fichier unique, recherche et comparaison en SQL, aucune installation |
| Racines | **1 à n disques par inventaire**, un statut par disque | la recherche doit couvrir tous les disques, et l'outil doit dire lequel est incomplet |
| Recherche | **index sur le nom**, deux niveaux (préfixe, puis sous-chaîne sur l'index) | rapide sans index plein texte ; FTS5 seulement si la mesure l'exige |
| Parcours | **API standard .NET** (`DirectoryInfo.EnumerateFileSystemInfos`), **un thread**, écriture par lots de 500 | simplicité ; Win32 seulement si la mesure l'exige |
| Mémoire | **aucun fichier conservé en mémoire** | pic visé < 200 Mo sur 6 Go |
| Historique | comparaison par **chemin complet normalisé**, suppression **manuelle** | simplicité et maîtrise par l'utilisateur |
| Renommages | **retrait + ajout**, sans reconnaissance de déplacement | décision de V1 : pas d'heuristique, pas de faux positifs |
| Licence du futur projet | **MIT** | compatible avec toutes les inspirations permises, exclut la copie GPL |

### 4.3 Les quatre constats qui pilotent cette version

1. **Le PC cible impose la sobriété** : 6 Go, disque mécanique, CPU ancien. Un thread,
   pas d'arbre en mémoire, démarrage rapide.
2. **Aucun des quatre dépôts ne fait ATLAS PC.** Ils couvrent chacun un morceau ; les
   fonctions « logiciels / outils / projets » et « comparaison fichier par fichier »
   restent entièrement à écrire.
3. **Le dossier initial était surdimensionné** : sept projets, vingt tables, dix
   jalons, des exports, un treemap. Rien de tout cela n'était exigé par le besoin.
4. **Deux horizons sont hors support** (Windows 10 22H2 depuis le 14/10/2025 ; .NET 8
   et 9 depuis le 10/11/2026). La bonne réponse n'est pas d'empiler les couches, mais
   de dépendre du minimum : le framework présent dans le système.

### 4.4 Ce que le dossier ne tranche pas

Les points qui exigent la machine réelle sont listés dans
[`12-risques-et-limites.md`](12-risques-et-limites.md) § 4. Les deux plus importants :
**le nombre réel de fichiers** (dimensionne la base et la durée) et **la vitesse du
parcours standard .NET** (déclenche ou non l'ajout d'un énumérateur Win32).

---

## 5. Ce qui a été supprimé, et pourquoi

Cette section est le cœur de la révision. **Rien de ce qui suit n'est reporté** : c'est
retiré du dossier, du périmètre et du plan — sauf mention explicite « reporté (demandé) ».

### 5.1 Architecture

| Supprimé | Remplacé par | Motif |
|---|---|---|
| 6 projets (`Core`, `Data`, `Scan`, `Collectors`, `Reports`, `App`) | **1 projet**, 8 dossiers | les frontières entre projets étaient théoriques : rien n'empêchait d'appeler `Data` depuis `Core` |
| Multi-ciblage `net48;net10.0-windows` | **`net48` seul** | deux jeux de dépendances, polyfills, double validation, pour un besoin inexistant |
| 8 interfaces (`ICollector`, `IScanEngine`, `IFileSystem`, `IWmiQuery`, `IRegistryReader`, `IProcessRunner`, `IInventoryRepository`, `IReportWriter`) | **aucune interface** | un seul implémenteur chacun ; les tests utilisent le vrai code sur des dossiers temporaires |
| Couche `Services` | les ViewModels appellent `Data` | une couche qui ne fait que transmettre |
| Couche `Ports` | — | conséquence directe de la suppression des interfaces |
| `Directory.Build.props`, analyseurs, `LangVersion` avancée | `.editorconfig` seul | configuration pour deux projets |

### 5.2 Base de données

| Supprimé | Remplacé par | Motif |
|---|---|---|
| 20 tables | **10 tables** (v2.0) ; `scan_root` ajoutée en v2.1, `disk` fusionnée en v2.2 | |
| `hw_computer`, `hw_cpu`, `hw_memory_module`, `hw_gpu`, `hw_disk`, `hw_partition`, `hw_volume`, `hw_bios`, `os_info` (9 tables) | **2 tables** : `computer`, `volume` | une ligne de matériel n'a pas besoin de neuf tables ; l'ancienne table `disk` est fusionnée dans `volume` depuis la v2.2 |
| `inventory_scope` (multi-racines) | `inventory.root_path`, puis **`scan_root`** en v2.1 | une seule racine suffisait tant qu'il n'y avait pas de recherche ; voir § 5.9 |
| `collect_status` et `field_note` (traçabilité par champ) | `—` affiché, `scan_issue` pour les erreurs de fichiers | coût de mise en œuvre très supérieur au bénéfice pour un outil personnel |
| `file_type_stat`, `age_bucket_stat`, `size_bucket_stat` (statistiques pré-calculées) | **requêtes `GROUP BY` à la demande** | quelques centaines de lignes de code en moins, à mesurer |
| `path_hash` (FNV-1a 64 bits) | **`folder_path` + `name`** avec index `COLLATE NOCASE` | « identification trop sophistiquée » ; la jointure sur deux colonnes est plus simple et couvre le besoin |
| `folder.parent_id` + CTE récursive | **`folder.parent_path`** | l'arborescence se construit avec une colonne texte, sans récursion SQL |
| Politique de rétention automatique (3 détaillés / 9 allégés / purge) | **suppression manuelle** par l'utilisateur | mécanisme complexe, surprenant, et à expliquer |
| Colonnes `allocated_bytes`, `cluster_size`, `attributes` | **retirées** | reportées (demandé) |
| `software.identity`, `confidence`, `is_update` | nom + champs utiles seulement | l'identité textuelle n'était utile que pour un diff fin |
| `project.markers`, `kinds` (multi-valeurs) | `kind` + `is_git` | deux types de projets, un booléen |
| Table `schema_version` + migrations | création du schéma en une fois, version stockée dans `Database.cs` | une seule version de schéma en V1 |

### 5.3 Inventaire des fichiers

| Supprimé | Remplacé par | Motif |
|---|---|---|
| `FindFirstFileExW`, `FIND_FIRST_EX_LARGE_FETCH`, `SafeFindHandle`, structures `WIN32_FIND_DATA`, préfixe `\\?\` | **`DirectoryInfo.EnumerateFileSystemInfos`** | API standard, 40 lignes, métadonnées déjà en cache ; Win32 réservé à une contingence mesurée |
| Taille allouée, `GetCompressedFileSize`, taille de cluster | **retirés** | reportés (demandé) |
| Attributs de fichier stockés | **retirés** | reportés (demandé) |
| Parallélisme, 1-2 travailleurs | **un seul thread** | disque mécanique, simplicité |
| `FIND_FIRST_EX_LARGE_FETCH` et pilotage de tampon | — | propriété de l'API qui n'est plus utilisée |
| Détection des liens physiques | — | hors périmètre |

### 5.4 Collectes

| Supprimé | Motif |
|---|---|
| **Détection des logiciels portables** (heuristique, `confidence`, raccourcis) | trop incertaine : faux positifs, explication difficile, valeur faible |
| Partitions, barrettes mémoire détaillées, carte mère, chipset, SMART, TPM, Secure Boot, moniteurs, batterie, réseau | détails secondaires (demandé) |
| **Analyse des dépôts Git** (branche, remote, commits) | non demandée ; seul un indicateur « présence de `.git` » est conservé |
| Analyse des dépendances (contenu de `package.json`, `requirements.txt`) | explicitement exclue |
| Détection d'autres marqueurs (`*.csproj`, `Cargo.toml`, `go.mod`, `.sln`) | hors périmètre : Python et Node.js seulement |
| Mode administrateur, `SeBackupPrivilege`, scan MFT | hors périmètre, exige l'élévation |
| Requêtes WMI de secours et replis multiples | un `try/catch` par requête suffit |

### 5.5 Historique et comparaison

| Supprimé | Remplacé par | Motif |
|---|---|---|
| Instantanés binaires compressés (modèle Diskplot) | **lignes SQLite** | interrogeable, pas de format propriétaire |
| Hachage de chemin, appariement par nom dans le parent, seuils de 4/8 Mio | **jointure sur chemin complet normalisé** | plus simple, et couvre **tous** les fichiers, pas seulement les gros |
| Allègement automatique des inventaires anciens | suppression manuelle | voir § 5.2 |
| Comparaison à trois inventaires, courbes de tendance | **deux inventaires** | demandé |
| `VACUUM` automatique | rien | inutile sur ce volume |

### 5.6 Interface

| Supprimé | Motif |
|---|---|
| **Rubrique « Rapports »** | « aucune fonction de rapport ou d'exportation » |
| **Module d'export HTML / CSV / JSON**, masquage des chemins à l'export | même raison ; le module entier disparaît, pas seulement le bouton |
| **Treemap** | coût de développement élevé, non exigé, faible lisibilité sans interactions coûteuses |
| **Anneau / camembert** | remplacé par des lignes de barres |
| Courbe d'évolution du stockage | non demandée |
| Filtres par propriétaire, par attributs | sans source fiable en V1 ; les filtres taille / date / type / emplacement, eux, sont **exigés** et présents |
| Écran d'options, choix de thème | un thème clair unique suffit |
| Raccourcis multiples | trois raccourcis |

### 5.7 Plan et documentation

| Supprimé | Remplacé par / motif |
|---|---|
| Plan en 10 jalons (M0 → M9) | **5 étapes** (3 + 3 + 6 + 3 + 4 jours) |
| Jalon dédié aux rapports (M8) | supprimé avec la fonction |
| Jalon M0 « socle et vérifications » séparé | **fondu dans l'étape 1**, qui produit une application utilisable |
| `docs/13-registre-inspiration.md` (document entier) | **5 lignes** ajoutées au livrable 3 : un document de plus pour une règle de gouvernance était disproportionné |
| `THIRD-PARTY-NOTICES.md` | une seule dépendance (SQLite, domaine public) : aucune obligation de notice |
| Dossier `tools/`, `build/`, scripts de publication | une commande `dotnet publish` documentée dans le README |
| Scénario « fork de WinDirStat » (2 pages) | ramené à 3 lignes dans le livrable 3 |
| Notations et tableaux redondants entre livrables | un seul tableau comparatif (livrable 2) |

### 5.8 Ce que la simplification ne touche pas

Trois règles restent **non négociables**, parce qu'elles viennent du cahier des charges
et non de la technique :

- **lecture seule** sur les fichiers et sur Windows ;
- **ne jamais annoncer la disparition** d'un fichier dans une zone non analysée ;
- **signaler** les éléments inaccessibles plutôt que de les ignorer.

Ce sont les trois seules choses que la V1 ne peut pas simplifier.

### 5.9 Ce que la version 2.1 **ajoute**, et ce qu'elle rétablit

| Ajout | Motif | Coût |
|---|---|---|
| Table **`scan_root`** (11ᵉ table) : un statut par disque | la recherche doit couvrir **tous** les disques inventoriés et l'interface doit dire lequel est incomplet | +20 lignes de SQL, +1 requête |
| Index **`ix_file_name`** (et `ix_file_size`) | rendre la recherche par nom rapide **sans** index plein texte | +30-45 Mo par inventaire |
| Dossier **`Shell/`** : `ItemOpener`, `ClipboardHelper` | ouvrir un élément et copier son chemin, en restant en lecture seule | 2 fichiers, ~120 lignes |
| **Filtres** extension / type / taille / date / emplacement | exigés par le cahier des charges révisé | ~80 lignes de SQL + 5 contrôles |
| **Familles de types** (dictionnaire statique + couleurs) | lisibilité du tableau de résultats | 1 fichier, sans table |
| Scission de l'étape 3 en **3A** (scan) et **3B** (rubrique Fichiers) | la fonction centrale mérite son propre jalon et ses propres critères | +4 jours (23 j au total) |
| **Nuances de couleurs** (familles, états, remplissage disques, rampe de barres) | lisibilité ; le gris uniforme rendait la maquette plate | ~40 lignes de `Theme.xaml` |

**Ce que la version 2.1 rétablit, contre la version 2.0.** Deux éléments avaient été
retirés à tort, parce que le périmètre « simplifié » d'alors ne les prévoyait pas :

| Rétabli | Pourquoi la v2.0 l'avait retiré | Pourquoi il revient |
|---|---|---|
| **Plusieurs racines par inventaire** | inutile tant que la recherche n'existait pas | la recherche doit porter sur tous les disques |
| **Filtres avancés** (taille, date, type, emplacement) | « non demandés » | ils sont explicitement demandés |

En revanche, **rien de ce qui suit ne revient** : exports, treemap, recherche dans le
contenu, OCR, détection de doublons, hachage, rétention automatique, multi-ciblage,
couches et interfaces.

**Coût total de la version 2.1 : +4 jours** (23 au lieu de 19) et **+1 table**
(11 au lieu de 10) — pour la fonction qui justifie le logiciel.

### 5.10 Ce que la vérification de la v2.2 a changé

Les cinq points demandés sont détaillés dans [`13-verifications.md`](13-verifications.md).
En résumé :

| Vérification | Verdict | Changement |
|---|---|---|
| 1. Inventaire sur disques choisis, recherches sans rescan | écart partiel | **aucune relecture du disque** spécifiée (4 livrables) + un test qui le prouve |
| 2. Dossiers inaccessibles, pas de fausse suppression | **écart réel** | un scan **annulé** produisait encore de fausses suppressions : statut `cancelled` distinct, lot écrit, dossiers restants signalés, garde durcie |
| 3. Trois recherches à volume | écart partiel | **nom exact** servi par un court-circuit d'index + protocole de mesure sur 3 volumes |
| 4. Justification des tables | **une table inutile** | `disk` fusionnée dans `volume` → **10 tables** |
| 5. Renommage = retrait + ajout | conforme mais non écrit | décision explicitée, affichée à l'écran, et testée |

**Aucune fonctionnalité ajoutée.** Une table en moins, deux règles durcies, un test en
plus par règle.

---

## 6. Effet chiffré des deux révisions

| Indicateur | v1.0 | v2.0 | v2.1 | **v2.2** |
|---|---|---|---|---|
| Projets | 6 + 5 tests | 1 + 1 test | 1 + 1 test | **1 + 1 test** |
| Interfaces déclarées | 8 | 0 | 0 | **0** |
| Tables SQLite | 20 | 10 | 11 | **10** |
| Dépendances NuGet | 3 | 1 | 1 | **1** |
| Jalons | 10 | 5 | 5 (3 scindée) | **5** (3A/3B) |
| Durée estimée | 31-43 j | 19 j | 23 j | **23 j** |
| Rubriques d'interface | 8 | 7 | 7 | **7**, dont Fichiers très au-dessus |
| Fonctions de recherche | non prévues | simples | 11 | **11**, dont le nom exact servi par index |
| Tests prévus (rubrique Fichiers) | 0 | 0 | 14 | **11** (25 au total) |
| Formats d'export | 3 | 0 | 0 | **0** |
| Documents du dossier | 14 | 13 | 13 | **14** (dont ce rapport de vérification) |
| Maquettes | 1 | 1 | 2 | **2** |

---

## 7. Ce qui reste à décider

Trois décisions appartiennent au commanditaire, pas à l'étude
([`12-risques-et-limites.md`](12-risques-et-limites.md) § 5) :

1. les **disques** à inventorier par défaut, et la possibilité d'en ajouter un plus tard
   (disque externe, clé USB) ;
2. l'espace occupé : la taille allouée est-elle nécessaire malgré tout, ou la somme des
   tailles suffit-elle ;
3. le projet sera-t-il distribué (ce qui fixe le niveau de documentation utilisateur).
