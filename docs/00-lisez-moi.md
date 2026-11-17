# ATLAS PC — Dossier technique préalable

**Étude technique, sans programmation.**
Version du dossier : 1.0 — 17 novembre 2026.

---

## 1. Objet et périmètre

Ce dossier répond au cahier des charges « ATLAS PC » : étudier quatre dépôts GitHub,
comparer leurs méthodes, et arrêter une architecture pour un logiciel Windows
d'inventaire local, gratuit, hors ligne, sans droits administrateur, destiné à un
**Dell OptiPlex 780 — Windows 10 22H2 x64, CPU ancien, 6 Go de RAM, disque dur
mécanique, 1440 × 900**.

Aucune ligne de code applicatif n'a été écrite. Aucun dépôt n'a été modifié.
Les seuls livrables sont des documents et une maquette graphique.

## 2. Méthode réellement employée

| Étape | Ce qui a été fait |
|---|---|
| Clonage | Les quatre dépôts ont été clonés en profondeur 1 puis lus sur disque. |
| Lecture | README, LICENSE/NOTICE, arborescence complète, fichiers sources du moteur de scan, des modèles de données, de la persistance, des exports et de l'interface. |
| Vérification | Chaque constat technique cite un **chemin exact** et, quand c'est utile, un numéro de ligne. |
| Mesure | Aucune : le code n'a **pas** été compilé ni exécuté, et les temps annoncés par les README ne sont pas reproduits ici comme des mesures. |
| Licences | Lecture du texte de licence du dépôt, des en-têtes SPDX fichier par fichier, et des mentions de tiers. |

**Limite majeure de l'étude, à garder en tête :** l'analyse est **statique** et a été
réalisée sur Linux. Aucun binaire Windows n'a été lancé, aucune performance n'a été
mesurée sur l'OptiPlex 780. Tout chiffre de performance figurant ici est soit une
citation explicite du dépôt, soit une estimation clairement étiquetée « estimation ».

### Références exactes des dépôts examinés

| Dépôt | Commit examiné | Date du commit | Version affichée | Licence |
|---|---|---|---|---|
| `neortal-technologies/NeoTreescan` | `40acd1d27b7f8f255ef23341856d66125073dc10` | 2026-04-22 | 1.0.0 | Apache-2.0 |
| `rodrigokiller/diskplot` | `8787cf6b01840cddd263915ffaf286c78c678b05` | 2026-10-17 | 0.4.0 | MIT |
| `TheLouisMahdi/louis-mahdi-system-inspector` | `420b20a6adc9c004e84e65e1e3b439ebe7b752c4` | 2026-07-30 | 2.2.0 | MIT |
| `windirstat/windirstat` | `d4f14bf75f47faacc78e7fa040c9dc75865f1c14` | 2026-11-15 | (voir `Version.h`) | GPL-3.0-or-later |

## 3. Sommaire des livrables

| # | Livrable demandé | Fichier |
|---|---|---|
| — | Index, synthèse et décision | `docs/00-lisez-moi.md` (ce document) |
| 1 | Rapport d'analyse — NeoTreescan | [`docs/01-analyse-neotreescan.md`](01-analyse-neotreescan.md) |
| 1 | Rapport d'analyse — Diskplot | [`docs/02-analyse-diskplot.md`](02-analyse-diskplot.md) |
| 1 | Rapport d'analyse — System Inspector | [`docs/03-analyse-system-inspector.md`](03-analyse-system-inspector.md) |
| 1 | Rapport d'analyse — WinDirStat | [`docs/04-analyse-windirstat.md`](04-analyse-windirstat.md) |
| 2 | Tableau comparatif des techniques et fonctionnalités | [`docs/05-tableau-comparatif.md`](05-tableau-comparatif.md) |
| 3 | Licences et risques de réutilisation | [`docs/06-licences-et-risques.md`](06-licences-et-risques.md) |
| 4 | Architecture recommandée et justification | [`docs/07-architecture-cible.md`](07-architecture-cible.md) |
| 5 | Schéma SQLite et stratégie de comparaison | [`docs/08-modele-sqlite.md`](08-modele-sqlite.md) |
| 6 | Maquette et spécification de l'interface | [`docs/09-interface.md`](09-interface.md) + `assets/maquette-atlas-pc.png` |
| 7 | Arborescence du futur code source | [`docs/10-arborescence-projet.md`](10-arborescence-projet.md) |
| 8 | Plan de développement par étapes | [`docs/11-plan-de-developpement.md`](11-plan-de-developpement.md) |
| 9 | Risques, limites et points à vérifier | [`docs/12-risques-et-limites.md`](12-risques-et-limites.md) |

## 4. Synthèse en une page

### 4.1 Ce que les quatre dépôts apportent — et ce qu'ils n'apportent pas

Aucun des quatre projets **ne fait ATLAS PC**. Ils couvrent chacun un morceau :

- **NeoTreescan** est le plus proche du *style* visé (WPF, C#, moteur de scan
  Win32 propre, interface à menu et tableaux) mais ne fait que de l'espace disque :
  pas de matériel, pas de logiciels, pas d'outils, **pas de base, pas d'historique,
  pas de comparaison**, et pas de tests.
- **Diskplot** apporte la meilleure **idée d'implémentation mémoire** (tableaux typés
  en colonnes, ~60 octets par fichier) et un vrai mécanisme d'**instantanés + diffs**,
  mais son diff est volontairement grossier (seuil de 8 Mio) et sa plateforme
  Electron est inadaptée à 6 Go de RAM / disque dur.
- **System Inspector** est le seul à traiter l'**inventaire matériel et Windows**
  avec une rigueur intéressante (statut de collecte par champ, dégradation gracieuse,
  délai par requête), mais il est en PowerShell + WinForms, sans logiciels ni
  fichiers.
- **WinDirStat** est la référence industrielle du scan (MFT NTFS, file de travail
  multi-thread, annulation par `CancelSynchronousIo`, exports CSV/JSON), mais c'est
  du C++ GPL-3.0 : inspiration uniquement, **aucune copie possible** dans un
  projet non-GPL.

### 4.2 Décision recommandée

| Sujet | Recommandation | Raison dominante |
|---|---|---|
| Runtime | **.NET Framework 4.8 (in-box), projets au format SDK** | Déjà présent sur Windows 10 22H2, aucune installation, démarrage à froid rapide sur disque mécanique. Alternative `.NET 10 LTS` documentée et chiffrée. |
| Interface | **WPF** (XAML), thème clair sobre, `DataGrid` virtualisé | Riche, mature, DPI-aware, présente dans le système ; pas de dépendance graphique lourde. |
| Graphiques | **Dessin natif WPF** (barres, anneau, histogrammes) ; OxyPlot (MIT) seulement si besoin | Évite SkiaSharp/WebView et la mémoire associée. |
| Base | **SQLite** via `Microsoft.Data.Sqlite` + `SQLitePCLRaw` (ou `System.Data.SQLite`) | Fichier unique, requêtes SQL de comparaison, aucun serveur. |
| Scan | **Un seul worker**, énumération `FindFirstFileExW` en flux, écriture SQLite par lots | Disque mécanique : le parallélisme dégrade plus qu'il n'aide ; mémoire bornée. |
| Historique | **Modèle relationnel normalisé par inventaire** (dossiers + fichiers), pas d'image binaire | Comparaison en SQL, taille maîtrisée, pas de recopie de contenu. |
| Licence ATLAS PC | **MIT** recommandée (ou Apache-2.0 si dépôt public) | Compatible avec toutes les inspirations permises ; exclut toute copie GPL. |

### 4.3 Les cinq constats qui pilotent l'architecture

1. **Le PC cible est le facteur limitant, pas les fonctionnalités.** 6 Go de RAM,
   disque mécanique et un CPU ancien imposent un moteur de scan **en flux**, une
   mémoire **constante**, **un** travailleur, et une interface qui ne recharge
   jamais tout l'inventaire en mémoire.
2. **Le modèle « un objet par fichier en RAM »** (NeoTreescan, WinDirStat) est
   inadapté ici : mémoire proportionnelle au nombre de fichiers, et chez
   NeoTreescan le chemin complet est stocké deux fois. Diskplot montre la bonne
   direction avec des tableaux typés (~60 octets par fichier) ; ATLAS PC va plus
   loin en ne gardant **aucun** fichier en mémoire, puisque SQLite les reçoit au fil
   de l'eau. Seuls les totaux par dossier restent en RAM.
3. **Aucun des quatre ne conserve un historique comparables fichier par fichier.**
   C'est la fonctionnalité la plus coûteuse du cahier des charges et elle doit être
   conçue dès le modèle de données, pas ajoutée après.
4. **`.NET 9` (NeoTreescan) n'est pas un choix à recopier** : .NET 8 et .NET 9 sont
   hors support depuis le 10 novembre 2026, et Windows 10 22H2 est lui-même sorti
   du support standard le 14 octobre 2025. Copier ce socle reviendrait à démarrer
   sur une pile déjà non maintenue.
5. **La licence est un critère de conception, pas une formalité.** WinDirStat est
   GPL-3.0-or-later sur 174 fichiers : la contamination est certaine en cas de
   copie. NeoTreescan (Apache-2.0) et les deux MIT permettent une réutilisation
   avec attribution, ce qui change radicalement le coût de développement.

### 4.4 Ce que ce dossier ne tranche pas

Les points qui ne peuvent être décidés sans la machine réelle sont listés et
priorisés dans [`docs/12-risques-et-limites.md`](12-risques-et-limites.md) § 4
(« Points à vérifier avant de figer l'architecture »). Les trois plus importants :
le nombre réel de fichiers du disque cible (dimensionne la base), le comportement
de WPF sur le GPU intégré à 1440 × 900, et la compatibilité effective du provider
SQLite avec `net48`.
