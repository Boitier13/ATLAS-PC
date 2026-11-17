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

## 4. Couverture des **sept rubriques** de la V1 simplifiée

Légende : ● fourni · ◐ partiel · ○ absent

| Rubrique V1 | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| 1. **Tableau de bord** | ○ | ◐ (aperçu + treemap) | ◐ (synthèse texte) | ◐ (treemap + liste) |
| 2. **Ordinateur** (fabricant, modèle, CPU, RAM, GPU, BIOS, Windows, disques, volumes) | ○ | ○ | ● | ◐ (quelques propriétés) |
| 3a. **Fichiers** : arbre de dossiers, contenu d'un dossier, tri | ● | ● | ○ | ● |
| 3b. **Recherche par nom** (complet ou partiel) sur tout l'inventaire | ○ (non identifiée) | ○ (non identifiée) | ○ (non identifiée) | ○ (non identifiée) |
| 3c. **Filtres** taille / date / type / emplacement | ○ | ◐ (dossier et seuil de taille) | ○ | ◐ (filtres d'affichage) |
| 3d. **Ouvrir** un élément, copier son chemin | ◐ (ouverture) | ◐ (ouverture) | ○ | ◐ (ouverture) |
| 4. **Logiciels installés** | ○ | ○ | ○ | ○ |
| 5. **Outils de dev** (Python, pip, Node, npm, Git, Tesseract) | ○ | ○ | ○ | ○ |
| 6. **Développement** (projets Python/Node, dépôt Git) | ○ | ○ | ○ | ○ |
| 7. **Évolution** (deux inventaires, ajouts/suppressions/modifs/espace) | ○ | ◐ (dossiers + fichiers ≥ 8 Mio) | ○ | ○ |

| Exigence transverse | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| Menu latéral | ○ (barre d'outils + onglets) | ○ (barre + panneaux) | ○ (fenêtre unique) | ○ (menus classiques) |
| Lecture seule garantie | ● | ◐ (supprime vers la corbeille) | ● | ◐ (nettoie, déplace, supprime) |
| Sans administrateur | ● (mode admin optionnel) | ● | ● | ● (mode élevé optionnel) |
| Hors ligne / sans compte | ● | ● | ● | ● |
| Fonctionne sur 6 Go + disque mécanique | ◐ | ○ | ● | ● |

**Aucun des quatre ne couvre les rubriques 4, 5 et 6.** Ce sont donc des
développements entièrement neufs, et les plus spécifiques à ATLAS PC.

**Sur la recherche (3b)** : aucune fonction de recherche par nom partiel n'a été
identifiée dans le code examiné des quatre dépôts. Cela ne prouve pas son absence de
leurs interfaces, mais **aucun d'eux ne peut servir de modèle** pour la fonction
centrale d'ATLAS PC : la recherche et ses filtres seront conçus à partir du cahier des
charges et des capacités de SQLite, pas d'un exemple existant. C'est le point du dossier
où l'étude préalable apporte le moins — et il faut le dire.
La rubrique 7 n'est approchée que par Diskplot, et à un seuil de 8 Mio qui n'a aucune
raison d'être dans ATLAS PC.

> Les rapports et exports ne figurent plus dans ce tableau : **la fonction est retirée
> du périmètre** (voir `docs/00-lisez-moi.md` § 5.6). Le fait que trois dépôts en
> proposent un n'est plus un argument.

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

## 6. Notation pour ATLAS PC (périmètre simplifié)

Critères pondérés selon les contraintes réelles du projet. Les notes sont un **jugement
argumenté** sur une échelle de 1 à 5, pas une mesure.

| Critère (poids) | NeoTreescan | Diskplot | System Inspector | WinDirStat |
|---|---|---|---|---|
| A. Adéquation au PC cible — 25 | 3 | 1 | 4 | 3 |
| B. Couverture du périmètre réduit — 25 | 2 | 3 | 3 | 2 |
| C. Valeur d'inspiration restante — 20 | 4 | 4 | 4 | 3 |
| D. Facilité de transposition vers **un seul** projet C#/WPF — 15 | 5 | 2 | 3 | 1 |
| E. Licence et réutilisation — 10 | 5 | 5 | 5 | 1 |
| F. Maturité, tests, documentation — 5 | 2 | 3 | 4 | 5 |
| **Total pondéré / 100** | **68** | **55** | **74** | **47** |

### Pourquoi les notes changent par rapport au premier dossier

| Note | v1.0 | v2.0 | Motif |
|---|---|---|---|
| B · NeoTreescan | 2 | 2 | inchangé sur les fichiers, mais son export ne compte plus |
| B · Diskplot | 1 | **3** | c'est le seul qui traite la comparaison d'inventaires, rubrique 7 désormais centrale |
| B · System Inspector | 2 | **3** | la rubrique « Ordinateur » pèse plus lourd dans un périmètre réduit à sept rubriques |
| B · WinDirStat | 1 | 2 | fournit la rubrique Fichiers, rien d'autre |
| C · NeoTreescan | 5 | **4** | son moteur Win32 n'est plus une inspiration retenue : la V1 part de l'API standard |
| C · Diskplot | 5 | **4** | son treemap et ses instantanés binaires sont écartés |
| C · WinDirStat | 4 | **3** | taille de cluster et MFT sortent du périmètre |
| D · Diskplot | 3 | **2** | la transposition depuis une application Electron est moins rentable qu'avant |

**Conséquence** : System Inspector passe devant NeoTreescan. Ce n'est pas une
incohérence avec le premier dossier, c'est l'effet mécanique du changement de
périmètre — la collecte matérielle prend le pas sur le moteur de scan, qui n'est plus
le point difficile.

Justification des notes les plus discutables :

- **A (adéquation matérielle)** : System Inspector obtient 4 parce qu'il fonctionne
  sans runtime supplémentaire, sans admin et avec une empreinte modérée ; Diskplot
  obtient 1 à cause du moteur Chromium sur 6 Go et disque mécanique ; NeoTreescan et
  WinDirStat obtiennent 3 (l'un démarre lentement depuis un EXE de 65 Mo, l'autre est
  léger mais paramétré pour des machines musclées).
- **B (couverture)** : personne ne dépasse 3/5 ; System Inspector couvre en profondeur
  une seule rubrique, Diskplot une seule aussi (l'évolution, partiellement).
- **E (licence)** : 1 pour WinDirStat n'est pas un jugement sur le logiciel mais sur
  l'impossibilité de réutiliser quoi que ce soit.

### Conclusion du classement

**System Inspector** est le meilleur **modèle de collecte**, **NeoTreescan** le modèle
d'application le plus proche, **Diskplot** le seul à avoir pensé la comparaison,
**WinDirStat** la meilleure référence de moteur — et aucun n'est un point de départ
réutilisable tel quel.

La recommandation (livrable 4) emprunte donc trois idées, et aucune ligne de code :
structure d'écran et manifeste de NeoTreescan, méthode de collecte dégradée de System
Inspector, organisation de la comparaison de Diskplot. Le moteur Win32 de NeoTreescan
et de WinDirStat est délibérément **écarté** au profit de l'API standard .NET, quitte à
y revenir si la mesure l'exige.
