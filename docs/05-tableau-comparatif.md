# Livrable 2 — Tableau comparatif des techniques et fonctionnalités

Comparaison établie **sur le code examiné**, aux commits indiqués dans
`docs/00-lisez-moi.md`. Aucune mesure de performance n'a été faite : la colonne
« performance » ne contient que des faits de conception, pas des chronomètres.

---

## 1. Socle technique

| Critère | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| Langage principal | C# | TypeScript | PowerShell (+ C# pour le lanceur) | C++23 |
| Interface | WPF (XAML) | HTML/CSS/React dans Electron | WinForms en PowerShell | Win32/ATL, framework maison |
| Runtime requis | **.NET 9 auto-contenu** (≈ 65 Mo d'EXE) | Chromium embarqué | Windows PowerShell 5.1 + .NET Framework 4.x (présents) | aucun (CRT statique) |
| Cible / binaire | `net9.0-windows`, `win-x64` uniquement | `win32` + Linux/macOS/Android | AnyCPU | x64, x86, ARM64 |
| Support du runtime à la date du dossier | **.NET 9 hors support depuis le 10/11/2026** | Electron maintenu | PowerShell 5.1 = composant de Windows | MSVC v145, C++23 |
| Nombre de projets | 2 (+1 outil) | 1 application, 4 dossiers de code | 1 script + 1 lanceur | 1 solution, ~175 fichiers |
| Volume de code | ≈ 2 000 lignes C# | ≈ 8 600 lignes TS/TSX | 2 470 lignes PS + 370 lignes C# | ≈ 175 fichiers, dizaines de milliers de lignes |
| Tests automatisés | **aucun** | **script présent, fichiers absents** | scripts de validation + tests simulés | scripts PowerShell de recette |
| Licence | Apache-2.0 | MIT | MIT | **GPL-3.0-or-later** |
| Facilité de transposition vers C# | **très forte** | moyenne (concepts seulement) | forte (requêtes, pas le code) | **nulle** (licence + langage) |

---

## 2. Moteur de scan

| Critère | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| API d'énumération | `FindFirstFileExW` + `FindExInfoBasic` + `FIND_FIRST_EX_LARGE_FETCH` | `GetFileInformationByHandleEx(FileFullDirectoryInfo)` via koffi | — (aucun scan de fichiers) | `NtQueryDirectoryFile` (4 Mio local / 64 Kio distant), + MFT brut |
| Accès aux métadonnées | une entrée à la fois | **par lots de plusieurs centaines** | — | par lots |
| Chemins longs | préfixe `\\?\` | préfixe `\\?\` / `\\?\UNC\` | — | `MakeLongPathCompatible` |
| Points de reparse | listés, jamais suivis | listés, jamais suivis | — | listés, typés (symlink / junction / mount / cloud) |
| Taille allouée | **estimée** (`RoundUpToBlock 4096`) | réelle (allocation rapportée par l'API) | — | réelle (cluster lu par `GetDiskFreeSpace`) |
| Erreurs d'énumération | compteur + liste de chemins | `F_ERR` par entrée + `ScanIssue` (max 200) | — | `ScanOutcome` à 7 états, propagation jusqu'à l'export |
| Parallélisme | `Parallel.ForEach` sur les 2 premiers niveaux, `ProcessorCount` | pool `max(2, min(12, cœurs))`, lots de 24 | — | 4 threads par défaut, jusqu'à `max(16, cœurs)` |
| Pause | non | non (seulement stop/cancel) | — | **oui** |
| Annulation | par dossier (`CancellationToken`) | par dossier, avec `stop` (garde) / `cancel` (jette) | — | par dossier + `CancelSynchronousIo` |
| Progression | timer 200 ms | message 120 ms + aperçu ≈ 1×/s | — | indicateur par dossier |
| Reprise après interruption | **non** | non | — | non |

---

## 3. Modèle de données et mémoire

| Critère | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| Représentation | arbre d'objets | **tableaux typés en colonnes** | modèle de champs par section | arbre d'objets `CItem` |
| Coût mémoire par fichier | élevé (objet + **2 chaînes**, dont le chemin complet) | ≈ 60 octets (annoncé par le projet) | sans objet | moyen (nœud + nom + vecteur) |
| Mémoire bornée ? | **non** | oui, par redimensionnement | sans objet | non |
| Aggrégation des tailles | `RollupSelf` récursif après coup | **une passe arrière** sur tableaux | — | mise à jour ascendante atomique pendant le scan |
| Détail par fichier en base | aucun | instantané : dossiers + fichiers ≥ 8 Mio | aucun | export CSV/JSON complet, à la demande |
| Contenu des fichiers lu ? | **jamais** | **oui**, pour les doublons (SHA-1) | non | oui, pour les doublons (hachage configurable) |

---

## 4. Couverture fonctionnelle du cahier des charges ATLAS PC

Légende : ● fourni · ◐ partiel · ○ absent

| Fonction V1 | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| 1. Ordinateur (fabricant, modèle, CPU, RAM, GPU, disques, partitions, Windows, BIOS) | ○ | ○ | ● | ○ (partiel, via WMI pour quelques propriétés) |
| 2. Fichiers (chemins, extensions, tailles, dates, espace occupé) | ● | ● | ○ | ● |
| 3. Logiciels installés / portables / éditeurs | ○ | ○ | ○ | ○ |
| 4. Outils de dev (Python, pip, Node, npm, Git, Tesseract) | ○ | ○ | ○ | ○ |
| 5. Projets (package.json, requirements.txt, pyproject.toml, dépôts Git) | ○ | ○ | ○ | ○ |
| 6. Rapports HTML / CSV / JSON | ◐ (Excel seulement) | ○ | ◐ (TXT/JSON/HTML/ZIP, pas de CSV) | ◐ (CSV/JSON, pas de HTML) |
| 7. Historique multi-inventaires + comparaison | ○ | ◐ (instantanés, seuil 8 Mio, dossiers) | ○ | ○ |
| Interface à menu latéral | ○ (barre d'outils + onglets) | ○ (barre + panneaux) | ○ (fenêtre unique) | ○ (menus classiques) |
| Lecture seule garantie | ● | ◐ (supprime vers la corbeille) | ● | ◐ (nettoie, déplace, supprime) |
| Sans administrateur | ● (mode admin optionnel) | ● | ● | ● (mode élevé optionnel) |
| Hors ligne / sans compte | ● | ● | ● | ● |

**Aucun des quatre ne couvre les fonctions 3, 4 et 5.** Ce sont donc des
développements entièrement neufs, et les plus spécifiques à ATLAS PC.

---

## 5. Points forts / points faibles par dépôt

### NeoTreescan
- **+** Architecture Core/UI propre, C#/WPF directement transposable, interop Win32
  correcte et lisible, manifeste exemplaire (`asInvoker`, `PerMonitorV2`, long paths),
  licence permissive.
- **−** Runtime hors support, mémoire non bornée (chemin dupliqué par fichier),
  aucune persistance ni comparaison, aucun test, estimation fausse de l'allocation,
  MFT factice, export Excel uniquement.

### Diskplot
- **+** Le meilleur modèle mémoire des quatre, arrêt `stop`/`cancel` réfléchi,
  aperçu borné, treemap borné par les pixels, instantanés + purge, licence MIT.
- **−** Plateforme Electron inadaptée à 6 Go/disque mécanique, diff limité aux
  dossiers et fichiers ≥ 8 Mio, format binaire non interrogeable, tests annoncés mais
  absents, aucune couverture matériel/logiciels.

### System Inspector
- **+** Meilleure méthodologie de collecte (statut, confiance, source, durée,
  délai borné, dégradation gracieuse), documentation des limites exemplaire, HTML
  autonome, lecture seule, sans admin.
- **−** PowerShell non transposable, WinForms en positions absolues, fenêtre 780 × 650
  fixe, aucun fichier ni logiciel, latence des requêtes CIM en série.

### WinDirStat
- **+** Moteur le plus abouti (deux stratégies, pause, `CancelSynchronousIo`, états
  d'incomplétude), interface éprouvée, exports CSV/JSON, taille de cluster réelle.
- **−** GPL-3.0-or-later (aucune copie), C++/Win32 impossible à reproduire seul,
  arbre complet en mémoire, 4 threads par défaut, périmètre fonctionnel très
  éloigné d'ATLAS PC, aucune comparaison d'inventaires.

---

## 6. Notation pour ATLAS PC

Critères pondérés selon les contraintes du projet. Les notes sont un **jugement
argumenté** sur une échelle de 1 à 5, pas une mesure.

| Critère (poids) | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| A. Adéquation au PC cible — 25 | 3 | 1 | 4 | 3 |
| B. Couverture du cahier des charges — 25 | 2 | 1 | 2 | 1 |
| C. Valeur d'inspiration technique — 20 | 5 | 5 | 4 | 4 |
| D. Facilité de transposition vers C#/WPF — 15 | 5 | 3 | 3 | 1 |
| E. Licence et réutilisation — 10 | 5 | 5 | 5 | 1 |
| F. Maturité, tests, documentation — 5 | 2 | 3 | 4 | 5 |
| **Total pondéré / 100** | **72** | **52** | **69** | **46** |

Justification des notes les plus discutables :

- **A (adéquation matérielle)** : System Inspector obtient 4 parce qu'il fonctionne
  sans runtime supplémentaire, sans admin et avec une empreinte modérée ; Diskplot
  obtient 1 à cause du moteur Chromium sur 6 Go et disque mécanique ; NeoTreescan et
  WinDirStat obtiennent 3 (l'un démarre lentement depuis un EXE de 65 Mo, l'autre est
  léger mais paramétré pour des machines musclées).
- **B (couverture)** : personne ne dépasse 2/5 ; System Inspector couvre en
  profondeur une seule des huit rubriques, NeoTreescan couvre entièrement la rubrique
  « Fichiers » et une partie des rapports.
- **E (licence)** : 1 pour WinDirStat n'est pas un jugement sur le logiciel mais sur
  l'impossibilité de réutiliser quoi que ce soit.

### Conclusion du classement

**NeoTreescan** est le meilleur **modèle d'application**, **System Inspector** le
meilleur **modèle de collecte**, **Diskplot** le meilleur **modèle de mémoire et
d'historique**, **WinDirStat** la meilleure **référence de moteur** — et aucun n'est
un point de départ réutilisable tel quel.

La recommandation qui suit (livrable 4) combine ces quatre apports sans copier de
code : structure d'application de NeoTreescan, contrat de collecte de System
Inspector, sobriété mémoire de Diskplot, rigueur d'énumération de WinDirStat.
