# Livrable 1.4 — Rapport d'analyse : WinDirStat

> Dépôt : `https://github.com/windirstat/windirstat`
> Commit examiné : `d4f14bf75f47faacc78e7fa040c9dc75865f1c14` (2026-11-15)
> Version : 2.10.0 (`windirstat/Version.h`, `PRD_MAJVER 2` / `PRD_MINVER 10` / `PRD_PATCH 0`)
> Licence : **GPL-3.0-or-later** · Analyse **statique** (code non compilé)

---

## 1. Identification

| Élément | Valeur vérifiée | Source |
|---|---|---|
| Nature | Analyseur d'espace disque + assistant de nettoyage, multi-sources (NTFS, MTP, S3, Azure, WebDAV, WSL) | `README.md` |
| Langage | **C++23** (`stdcpp23`), Win32 + ATL, framework d'interface **maison** | `windirstat/windirstat.vcxproj` (l. 74), `pch.h`, `UiFramework.h` |
| Chaîne de build | MSVC `v145`, SDK 10.0, CRT **statique** (`MultiThreaded`) | `.vcxproj` (l. 41, 113, 132) |
| Volume | **84 `.cpp` + 91 `.h`** ; 38 fichiers dans `Controls/` ; 174 fichiers portent l'en-tête SPDX | `find` + `grep -rh SPDX-License-Identifier` |
| Dépendances | ATL, Windows SDK, `xxHash` téléchargé au premier build | `README.md` § Building, `build.cmd` |
| Manifeste | `supportedOS` Windows 7 → Windows 10+ | `windirstat/res/windirstat.manifest` |
| Tests | scripts PowerShell (`Test-WinDirStat.ps1`, `Test-Recovery.ps1`, `Test-Remote.ps1`, `Test-S3.ps1`, `Test-WslRunner.ps1`) + `RecoveryTests.vcxproj` | `tests/` |
| Distribution | MSI x64/x86/ARM64, Store, winget, choco, scoop, ZIP, 7z | `README.md`, `setup/` |

C'est un projet **industriel** : deux décennies d'existence, plusieurs moteurs de
scan, une interface Win32 écrite à la main, une couverture de sources de stockage
bien au-delà du disque local.

---

## 2. Architecture réelle

```
windirstat/
  WinDirStat.cpp / MainFrame.cpp / Ui*.cpp        ← application Win32 + framework UI maison
  Finder.h                                        ← interface de parcours (FindNext/FindFile)
  FinderBasic.cpp        (NtQueryDirectoryFile)
  FinderNtfs.cpp         (lecture directe du $MFT)
  FinderMtp.cpp / FinderRemote.cpp / FinderWsl.cpp
  Item.h / Item.cpp      ← CItem : l'arbre en mémoire
  WinDirStatModel*.cpp   ← modèle applicatif, orchestration du scan
  BlockingQueue.h        ← file de dossiers + threads (jthread), pause/annulation
  CsvLoader.cpp          ← sauvegarde CSV / JSON et rechargement
  Controls/              ← TreeMap, HilbertMooreTreeMap, Sunburst, FlameGraph, listes
  Views/ Pages/ Dialogs/ Options.*
tests/                   ← scripts PowerShell de recette
```

Le point d'architecture clé est `Finder.h` : **une interface unique** (`FindNext`,
`FindFile`, `GetFileSizePhysical`, `GetFileSizeLogical`, `GetLastWriteTime`,
`GetReparseTag`, `IsReserved`…) implémentée par chaque source. Le moteur de scan ne
sait pas s'il parle à NTFS, à un partage SMB, à un téléphone ou à S3.

Pour ATLAS PC, cela valide une idée : **séparer l'énumérateur de fichiers du moteur
d'inventaire** derrière une interface étroite, pour pouvoir substituer une source
(volume local, dossier, futur partage) sans toucher au reste.

---

## 3. Moteurs de scan — ce qui est vérifié

### 3.1 `FinderBasic` : énumération par `NtQueryDirectoryFile`

`windirstat/FinderBasic.cpp` :

- résolution dynamique de `NtQueryDirectoryFile` dans `ntdll.dll` via
  `GetProcAddress` (pas d'import statique) ;
- **tampon de 4 Mio** pour un volume local, **64 Kio** pour un volume distant
  (`LOCAL_BUFFER_SIZE = 4 * wds::Mi`, `REMOTE_BUFFER_SIZE = 64 * wds::Ki`), avec un
  commentaire explicite : « Larger buffers fail on older network redirectors (issue #631) » ;
- classe d'information `FileIdFullDirectoryInformation` (38) avec repli sur
  `FileFullDirectoryInformation` (2) si `STATUS_INVALID_INFO_CLASS` /
  `STATUS_NOT_SUPPORTED` ;
- distinction nette entre **répertoire épuisé** et **énumération incomplète** :
  `ScanOutcome::Complete` seulement si `ERROR_NO_MORE_FILES`/`ERROR_FILE_NOT_FOUND`
  dans les bonnes conditions, sinon `SetError(...)` ;
- taille de cluster lue via `GetDiskFreeSpace` (donc **réelle**, pas supposée).

Ce dernier point est exactement le défaut identifié chez NeoTreescan (§ 3.3 du
rapport 01). WinDirStat fait ce qu'il faut.

### 3.2 `FinderNtfs` : lecture directe du MFT

`windirstat/FinderNtfs.cpp`, `FinderNtfsContext::LoadRoot` (l. 151) :

```cpp
CreateFile(volumePath /* \\.\C: */, FILE_READ_DATA | FILE_READ_ATTRIBUTES | SYNCHRONIZE, ...
DeviceIoControl(volumeHandle, FSCTL_GET_NTFS_VOLUME_DATA, ...)
CreateFile(volumePath + L"\\$MFT::$DATA", ...
```

Puis lecture des *retrieval pointers* et **décodage des enregistrements bruts** :
structures `FILE_RECORD` (signature `'FILE'`, `UsaOffset`, `Flags`, `BaseFileRecordNumber`…)
et `ATTRIBUTE_RECORD` (`$STANDARD_INFORMATION` 0x10, `$FILE_NAME` 0x30, `$DATA` 0x80,
`$REPARSE_POINT` 0xC0) définies en tête du fichier.

Conséquence : **ce chemin exige l'administrateur** (ouvre le volume brut) et n'est
utilisé que si l'option est active — `Options.h` l. 245 :
`Setting<bool> UseFastScanEngine{ ..., true }` (activée par défaut), mais
`WinDirStatModel.Actions.cpp` l. 1394 conditionne son usage à
`item->IsTypeOrFlag(IT_DRIVE) && COptions::UseFastScanEngine`, et le `LoadRoot`
échoue proprement sans droits suffisants (retour `false`, repli sur le parcours de
dossiers).

**À retenir** : le MFT est la seule manière d'énumérer un volume NTFS nettement plus
vite qu'un parcours de dossiers, **mais** il impose l'administration, un code de
décodage dense et spécifique à NTFS. Pour ATLAS PC, dont la règle est « pas
d'administrateur pour le fonctionnement normal », c'est un **hors-périmètre assumé**
pour la V1, à documenter comme limite et non comme bug.

### 3.3 File de travail, threads, pause, annulation

`windirstat/BlockingQueue.h` :

- `std::vector<std::jthread>` + `std::deque` + mutex/conditions variables ;
- `Push` (les dossiers à visiter), `Pop` (retourne `nullopt` quand tous les threads
  sont inactifs et la file vide : c'est le critère d'arrêt naturel) ;
- `SuspendExecution` / `ResumeExecution` : **pause** réelle, avec attente que tous
  les travailleurs soient endormis ;
- `CancelExecution` → `JoinThreads()` ; `CancelThreadIo()` → **`CancelSynchronousIo`**
  par thread, pour débloquer une I/O synchrone en cours (point fort : sans cela, une
  annulation peut attendre plusieurs secondes sur un disque lent).

Nombre de threads — `windirstat/Options.h` :

```cpp
inline const int MAX_SCANNING_THREADS = std::max(16, static_cast<int>(std::thread::hardware_concurrency()));   // l. 18
inline static Setting<int> ScanningThreads{ OptionsGeneral, L"ScanningThreads", 4, 1, MAX_SCANNING_THREADS, ... }; // l. 309-310
```

Donc : **4 threads par défaut**, réglables de 1 à `max(16, cœurs)`. Sur le PC cible
(2 cœurs) le réglage minimal de 1 est disponible, mais la valeur par défaut de 4 est
inadaptée à un disque mécanique. `WinDirStatModel.Actions.cpp` l. 1378-1380 réduit en
outre à `clamp(1..4)` pour les sources distantes et à 1 pour MTP.

### 3.4 Modèle mémoire

`windirstat/Item.h` : `CItem` est un nœud d'arbre complet avec, par nœud :
`m_parent`, `std::vector<CItem*>`, nom (longueur + tampon), `m_sizePhysical` et
`m_sizeLogical` **atomiques** (mise à jour ascendante depuis plusieurs threads sans
verrou), `m_files`, `m_subdirs`, `m_jobs`, `m_lastChange`, `m_type` (masque de bits
sur 32 bits), `m_attributes`, `m_scanOutcome`. Plus des pseudo-éléments :
`IT_FREESPACE`, `IT_UNKNOWN`, `IT_HLINKS*` (liens physiques), `ITF_SCANERROR`.

Les tailles sont propagées **vers le haut** au fil du scan
(`UpwardAddSizePhysical`, `UpwardAddSizeLogical`), ce qui évite une passe de
consolidation finale. Le prix est un nœud par fichier en mémoire, avec nom et
vecteur d'enfants : **plusieurs dizaines à une centaine d'octets par fichier**
(estimation), soit bien plus que les ~60 octets de Diskplot, mais sans duplication du
chemin complet.

**Pour ATLAS PC** : ce modèle est celui à éviter, non par défaut de qualité, mais
parce qu'il n'apporte rien ici — la base SQLite reçoit les fichiers au fil de l'eau
et l'interface n'affiche jamais 600 000 lignes.

### 3.5 Détection des erreurs d'énumération

`Finder.h` définit un `enum class ScanOutcome` à sept états :
`NotStarted, Complete, Partial, Cancelled, Missing, ConnectionError, EnumerationError`,
et `CItem` porte `ITF_SCANERROR`. Le README est explicite : « An incomplete scan
retains the items already found and marks affected branches » et les modes de
sauvegarde renvoient un **code de sortie non nul** si le scan est incomplet.

C'est **la bonne pratique à reprendre telle quelle dans l'esprit** : un inventaire
partiel reste exploitable, mais il est *marqué* comme partiel jusqu'au bout de la
chaîne (interface, export, code de sortie).

---

## 4. Stockage, exports, comparaison

| Sujet | Constat | Source |
|---|---|---|
| Persistance d'un scan | CSV ou JSON, à la demande | `CsvLoader.cpp`, `SaveResults` (l. 684) |
| Contenu JSON | un objet par élément : nom/chemin, fichiers, dossiers, taille logique, taille physique, attributs, date, type, index, propriétaire optionnel, résultat de scan et message d'erreur | `SaveResultsJson` (l. 644) |
| Rechargement | `CsvLoader.cpp` sait relire un CSV | `CsvLoader.h` |
| Génération JSON | écrite à la main avec `std::format_to` dans un buffer, pas de bibliothèque JSON | `SaveResultsJson` |
| Ligne de commande | `/SaveTo`, `/SaveDupesTo`, `/SavePermsTo`, `/LoadFrom` | `README.md`, `WinDirStat.cpp` |
| **Historique multi-inventaires** | **aucun** : un fichier de sauvegarde = un scan | absence vérifiée |
| **Comparaison entre deux scans** | **aucune** | absence vérifiée |
| Matériel / OS | partiel et indirect (WMI est lié dans `pch.h` via `wbemidl.h` pour des propriétés d'items), pas d'inventaire matériel | `pch.h` |
| Logiciels installés | **aucun** | absence vérifiée |
| Outils de dev / projets | **aucun** | absence vérifiée |

WinDirStat sait donc **exporter** un inventaire de fichiers de manière très complète,
mais ne sait pas **conserver plusieurs inventaires ni les comparer**. Le cahier des
charges ATLAS PC demande précisément ce que ce projet ne fait pas.

---

## 5. Interface

- Framework **maison** (`UiFramework.cpp`, `UiPanes.h`, `UiControls.cpp`,
  `TreeListControl.*`) construit sur Win32/ATL : pas de MFC, pas de WPF. C'est un
  choix assumé de performance et de contrôle du rendu.
- Visualisations : `Controls/TreeMap.cpp`, `Controls/TreeMapLayout.cpp` (algorithme
  de disposition), `Controls/HilbertMooreTreeMap.cpp` (variante Hilbert-Moore),
  `Controls/Sunburst.cpp` (« Multi-layer radial chart with size-proportional sectors
  and indexed hit testing »), `Controls/FlameGraph.cpp`, `Controls/PacMan.cpp`
  (indicateur de progression), `Controls/DrawTextCache.cpp` (cache de texte pour
  éviter de re-mesurer les chaînes à chaque rendu).
- Sous-systèmes remarquables : `IconHandler.cpp` avec **deux files d'icônes**
  (`m_fastQueue` / `m_slowQueue`, `MAX_ICON_THREADS`) pour ne pas bloquer le rendu
  sur l'extraction d'icônes ; `FileWatcherControl`, `ColorButton`, `XYSlider`.

Le soin apporté au **cache de texte** et aux **files d'icônes** est instructif : sur
un CPU ancien, ce sont ces détails qui décident de la fluidité d'une liste, pas le
choix du framework.

---

## 6. Privilèges

- Aucune élévation automatique. `HelpersTasks.cpp` expose `IsElevationActive()`
  (`TokenElevation`), `IsElevationAvailable()` (`TokenElevationTypeLimited`) et
  `RunElevated()` qui passe par `ShellExecuteWrapper(..., L"runas", ...)`
  (l. 361), précédé de `AllowSetForegroundWindow`.
- `HelpersInterface.cpp` l. 517 utilise le même mécanisme `runas`.
- `EnableReadPrivileges()` existe (`HelpersTasks.cpp`, après l. 370) pour activer les
  privilèges de lecture quand le processus est déjà élevé.
- L'interface propose « File > Run Elevated » : l'utilisateur choisit, l'application
  ne force pas.

**Cohérent avec la règle ATLAS PC.** La différence est que WinDirStat propose un mode
élevé *pour aller plus vite* (MFT) et *pour voir plus* (`System Volume Information`),
alors qu'ATLAS PC doit d'abord fonctionner sans.

---

## 7. Licence et implications — le point décisif

- `LICENSE.md` = **GNU GPL version 3** ; les 174 fichiers source portent
  `SPDX-License-Identifier: GPL-3.0-or-later`.
- Conséquences, détaillées dans `docs/06-licences-et-risques.md` :
  - toute **copie de code** (même 20 lignes) dans ATLAS PC fait de l'ensemble une
    œuvre dérivée soumise à la GPL-3.0 ;
  - si ATLAS PC est distribué, il devra l'être sous GPL-3.0, avec le code source
    complet et les mêmes libertés pour les destinataires ;
  - la GPL-3.0 est **incompatible** avec une distribution d'ATLAS PC sous MIT ou
    Apache-2.0 (le sens MIT/Apache → GPL est permis, l'inverse non) ;
  - à l'inverse, **lire le code pour comprendre un algorithme et réécrire une
    implémentation à partir d'une spécification publique** est licite : les idées et
    les algorithmes ne sont pas protégés par le droit d'auteur.
- Conclusion opérationnelle : **WinDirStat est une source de connaissance, jamais une
  source de code.** Pour un projet ATLAS PC qui resterait privé et non distribué, la
  GPL ne créerait pas d'obligation (pas de « conveyance »), mais ce serait un piège
  à retardement si l'outil est un jour publié.

---

## 8. Ce qui est réutilisable comme inspiration

1. **Interface `Finder` étroite** entre moteur et sources de stockage.
2. **`ScanOutcome` à sept états** et propagation de l'état « incomplet » jusqu'à
   l'export et au code de sortie.
3. **Taille de cluster réelle** via `GetDiskFreeSpace`, et distinction
   taille logique / taille physique.
4. **File de travail** avec critère d'arrêt naturel (tous les threads inactifs et
   file vide) et **pause** distincte de l'annulation.
5. **`CancelSynchronousIo`** pour débloquer une I/O en cours.
6. **Tampon d'énumération dimensionné selon local/distant.**
7. **Cache de texte** et files séparées pour les icônes.
8. **Deux formats d'export** (CSV pour l'analyse, JSON pour l'outillage).

## 9. Ce qu'il ne faut pas reprendre

| Élément | Pourquoi |
|---|---|
| Tout code source | GPL-3.0-or-later : contamination certaine |
| C++23 / Win32 / ATL maison | Coût de développement hors de portée pour un projet solo à interface riche |
| 4 threads par défaut, jusqu'à `max(16, cœurs)` | Inadapté à un bi-cœur et à un disque mécanique |
| Lecture du MFT | Exige l'administrateur ; hors périmètre V1 |
| Arbre `CItem` complet en mémoire | Inutile dès lors que SQLite stocke les métadonnées |
| Périmètre fonctionnel (S3, Azure, WebDAV, WSL, MTP, récupération de fichiers supprimés) | Hors cahier des charges ; chaque ajout est un coût de test |

---

## 10. Éléments **non vérifiés**

- Aucune compilation, aucun benchmark : les gains annoncés du MFT ne sont pas mesurés
  ici.
- Le décodage MFT n'a pas été audité au-delà des structures et du chemin d'ouverture.
- Les scripts de test PowerShell n'ont pas été lus intégralement ; on ne peut pas
  affirmer la couverture réelle du moteur de scan par des tests automatisés.
- Les traductions (`res/langs`) et le module de récupération de fichiers n'ont pas
  été examinés.
- Le comportement exact en cas de volume ReFS/FAT (replis) n'a pas été tracé.

---

## 11. Note pour ATLAS PC

WinDirStat est la **référence technique** et le **contre-exemple économique** : un
moteur admirable, une licence interdisant la copie, une base de code impossible à
reproduire seul, et aucune des fonctions « historique / comparaison / logiciels /
outils / projets » qui constituent le cœur d'ATLAS PC.

Voir notation comparée : `docs/05-tableau-comparatif.md`.
