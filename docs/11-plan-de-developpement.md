# Livrable 8 — Plan de développement par étapes

Hypothèse de travail : **une personne**, à temps partiel, sur un projet Windows.
Les durées sont des ordres de grandeur destinés à ordonner le travail et à révéler
les étapes coûteuses — ce ne sont pas des engagements.

Chaque étape est un **incrément utilisable** : à la fin de M1, l'application sait
inventorier un ordinateur ; à la fin de M4, elle sait inventorier un disque. Aucune
étape ne produit du code non exécutable.

---

## 1. Vue d'ensemble

| Jalon | Objet | Durée indicative | Dépend de |
|---|---|---|---|
| M0 | Socle, décisions vérifiées, coquille d'application | 1-2 j | — |
| M1 | Collecte « Ordinateur » et « Windows » | 3-4 j | M0 |
| M2 | Persistance SQLite : schéma, écriture par lots, migrations | 3-4 j | M0 |
| M3 | Moteur de scan fichiers : flux, progression, annulation | 5-7 j | M2 |
| M4 | Vue « Fichiers » et tableau de bord réel | 4-5 j | M1, M3 |
| M5 | Logiciels et outils de développement | 3-4 j | M2 |
| M6 | Projets et marqueurs | 1-2 j | M3 |
| M7 | Historique et comparaison « Évolution » | 4-5 j | M2, M5 |
| M8 | Rapports HTML / CSV / JSON | 3-4 j | M4, M7 |
| M9 | Recette, performance, durcissement, distribution | 4-6 j | toutes |
| **Total** | | **≈ 31-43 jours** | |

Chemin critique : **M2 → M3 → M4**. C'est là que se joue la faisabilité (mémoire,
durée, fluidité de l'interface).

---

## 2. Détail des jalons

### M0 — Socle et vérifications préalables (1-2 j)

**Objectif** : éliminer les incertitudes techniques **avant** d'écrire du métier.

| Livrable | Détail |
|---|---|
| Solution à 6 projets + 5 projets de test | arborescence du livrable 7, `Directory.Build.props` |
| Coquille WPF | sidebar 8 entrées, navigation, thème, badge d'état, états vides |
| Test de fumée SQLite | création de la base, insertion de 100 000 lignes, requête d'agrégation |
| Mesure du démarrage à froid | chronométrage sur le PC cible, à froid, après redémarrage |
| Décision runtime confirmée | `.NET Framework 4.8` conservé **ou** bascule `.NET 10 LTS` documentée |
| Licence, `THIRD-PARTY-NOTICES.md`, registre d'inspiration | créés avant la première ligne de métier |

**Tests**
- `SqliteSmokeTest` : 100 000 insertions en lots de 2 000, < 5 s sur le disque cible.
- Ouverture/fermeture de l'application 5 fois : pas de fuite de handle.
- Rendu de la coquille à 1440 × 900 sans défilement horizontal.

**Critères de sortie**
- démarrage à froid **< 3 s** ;
- mémoire au repos **< 130 Mo** ;
- la base est créée, relue et supprimée sans erreur ;
- la décision de runtime est consignée par écrit (avec les mesures).

> **Point d'arrêt** : si le démarrage dépasse 5 s ou la mémoire 200 Mo au repos, la
> décision de runtime est réexaminée **avant** M1. C'est le seul jalon où un retour
> en arrière est encore bon marché.

---

### M1 — Ordinateur et Windows (3-4 j)

**Objectif** : produire l'inventaire matériel sans administrateur, avec des statuts
honnêtes.

| Livrable | Détail |
|---|---|
| `IWmiQuery` + `WmiReader` | délai par requête, capture d'exception typée, durée mesurée |
| 7 collecteurs | ordinateur, CPU, mémoire, GPU, disques/partitions/volumes, BIOS, OS |
| Tables `hw_*` et `os_info` + `collect_status` | écriture d'un inventaire matériel |
| Vue « Ordinateur » | 7 fiches + tableau des volumes |
| Rapport des valeurs indisponibles | `field_note` alimentée pour VRAM approx., SMART absent, etc. |

**Tests**
- tests unitaires des 7 collecteurs avec `IWmiQuery` simulé (cas : valeur présente,
  valeur nulle, requête en échec, délai dépassé) ;
- test d'intégration sur la machine cible : les valeurs relevées sont comparées aux
  valeurs connues (Dell / OptiPlex 780 / 6 Go / modèle de disque) ;
- test « aucune valeur inventée » : un champ non collecté est `null` **et** possède
  une ligne `collect_status` ou `field_note` expliquant pourquoi.

**Critères de sortie**
- inventaire matériel complet en **< 15 s** ;
- aucune exception non gérée, même si WMI est indisponible ;
- 100 % des champs affichés soit ont une valeur, soit affichent `—` avec explication ;
- exécution en utilisateur standard, manifeste `asInvoker` vérifié.

---

### M2 — Persistance (3-4 j)

**Objectif** : un socle de stockage capable d'absorber 400 000 fichiers sans ralentir
le scan.

| Livrable | Détail |
|---|---|
| Scripts `001-initial.sql`, `002-indexes.sql` | schéma du livrable 5, versionné |
| `SchemaMigrator` | création et mise à niveau idempotentes |
| `FileBatchWriter`, `FolderBatchWriter` | transactions de 2 000 lignes, `synchronous = NORMAL` |
| `StatsWriter` | agrégats `file_type_stat`, `age_bucket_stat`, `size_bucket_stat` |
| `RetentionService` | allègement au-delà de 3 inventaires détaillés, purge au-delà de 12 |
| `DatabaseHealth` | intégrité, taille, entretien |

**Tests**
- insertion de 400 000 fichiers synthétiques : durée totale, taille finale de la base ;
- coupure brutale simulée en cours d'écriture : la base reste lisible (WAL) ;
- migration d'un schéma v1 vers v2 sur une base contenant des données ;
- purge : après passage en `light`, aucune ligne `file` ne subsiste, les dossiers et
  les agrégats sont intacts.

**Critères de sortie**
- 400 000 fichiers écrits en **< 60 s** sur le disque cible ;
- base détaillée **< 70 Mo** ;
- aucune écriture hors du dossier `%LOCALAPPDATA%\ATLAS PC\` (vérifié au moniteur) ;
- les index peuvent être créés après le scan si la mesure montre un gain (décision
  tracée).

---

### M3 — Moteur de scan (5-7 j)

**Objectif** : parcourir un volume entier en mémoire bornée, avec progression et
annulation.

| Livrable | Détail |
|---|---|
| `Kernel32` + `SafeFindHandle` | énumération `FindFirstFileExW`, chemins longs |
| `PathKey` | normalisation + `path_hash` |
| `DirectoryScanEngine` | pile, un travailleur, lots, points de contrôle |
| `ScanOptions` | 1-2 travailleurs, mode discret, exclusions de reparse |
| Gestion d'erreurs | `inventory_error`, agrégation des erreurs identiques |
| Cycle de vie `inventory` | `running` → `complete` / `partial` / `cancelled` |

**Tests**
- tests unitaires du moteur avec `IFileSystem` simulé : arborescence de 1 000 nœuds,
  boucle de jonction, dossier illisible, annulation à mi-parcours ;
- test d'intégration sur une arborescence de test réelle (10 000 fichiers générés) :
  totaux exacts comparés à une référence calculée indépendamment ;
- test de mémoire : suivi du `PrivateMemorySize64` pendant un scan complet, assertion
  **< 250 Mo** ;
- test d'annulation : latence de prise en compte **< 500 ms** ;
- test de reprise impossible : après annulation, l'inventaire est `partial`, jamais
  `complete`.

**Critères de sortie**
- volume C: entier analysé sans administrateur, avec un décompte d'erreurs cohérent ;
- aucun dépassement du budget mémoire ;
- progression rafraîchie au moins toutes les 500 ms, UI fluide pendant le scan ;
- `allocated_bytes` obtenu par `GetCompressedFileSize` (et non par arrondi) ;
- durée mesurée et consignée.

---

### M4 — Vue « Fichiers » et tableau de bord (4-5 j)

**Objectif** : rendre l'inventaire de fichiers réellement consultable.

| Livrable | Détail |
|---|---|
| `FilesView` | arborescence paresseuse + `DataGrid` virtualisé + filtres |
| `DashboardView` | 4 cartes, top 8 dossiers, types de fichiers, outils |
| `BarChart`, `DonutChart` | dessin natif WPF, pas de dépendance |
| Barre d'état | compteurs, dossiers inaccessibles, résultats |
| Bandeau de progression | non modal, annulation |

**Tests**
- défilement d'une grille de 400 000 lignes : mémoire stable (pas de matérialisation) ;
- ouverture d'une vue : **< 300 ms** ;
- changement de rubrique pendant un scan : aucun blocage ;
- test visuel de recette sur 1440 × 900 (capture comparée à la maquette).

**Critères de sortie**
- l'application reste réactive (aucun gel > 200 ms) pendant le parcours d'un volume ;
- mémoire stable pendant le défilement ;
- aucune colonne tronquée, aucun défilement horizontal ;
- toute valeur affichée est cohérente avec la base (vérification croisée par requête).

---

### M5 — Logiciels et outils (3-4 j)

| Livrable | Détail |
|---|---|
| `RegistrySoftwareCollector` | 3 ruches, filtrage documenté, `identity` stable |
| `PortableSoftwareCollector` | heuristique conservatrice, `confidence = low` |
| `ToolLocator` / `ToolVersionReader` | PATH, dossiers connus, `FileVersionInfo`, puis commande (3 s) |
| Vues « Logiciels » et « Outils » | tableaux, fiches d'état, pastilles de confiance |

**Tests**
- collecteur testé avec `IRegistryReader` simulé (entrées sans nom, `SystemComponent`,
  `ParentKeyName`, doublons 32/64 bits) ;
- `identity` stable : deux inventaires successifs sans changement produisent **zéro**
  différence ;
- `ToolVersionReader` : version lue par métadonnées, puis par commande, puis échec
  propre ; un outil absent ne bloque jamais les autres ;
- test « pas de `Win32_Product` » (garde-fou de performance et d'effets de bord).

**Critères de sortie**
- logiciels collectés en **< 3 s** ;
- outils détectés en **< 5 s** avec les commandes, **< 1 s** sans ;
- aucun faux « outil détecté » sur une machine où il est absent ;
- toute détection portable est marquée comme heuristique.

---

### M6 — Projets (1-2 j)

| Livrable | Détail |
|---|---|
| `ProjectDetector` | marqueurs détectés pendant le parcours, exclusions de dossiers |
| Table `project` | un enregistrement par dossier portant au moins un marqueur |
| Vue « Développement » | filtres par type, badges, compteur de dépôts Git |

**Tests**
- arborescence piégée : `node_modules` contenant 50 `package.json`, `venv`,
  `dist`, `target` → **aucun** faux projet ;
- un dossier cumulant `package.json` + `.git` → un seul projet, deux marqueurs ;
- test de non-régression : le détecteur n'ajoute aucune lecture disque (vérification
  par compteur d'appels sur `IFileSystem`).

**Critères de sortie**
- précision : aucun faux positif sur l'arborescence piégée ;
- rappel : tous les projets réels de fixtures sont trouvés ;
- aucun surcoût mesurable sur la durée du scan (< 2 %).

---

### M7 — Historique et comparaison (4-5 j)

| Livrable | Détail |
|---|---|
| Vue « Évolution » | sélecteurs A/B, synthèse, 6 onglets |
| `ComparisonQueries` | ajoutés, disparus, modifiés, stockage, logiciels, outils |
| Gestion des cas limites | portées différentes, inventaire allégé, inventaire partiel |
| `RetentionService` raccordé à l'interface | message clair quand une comparaison est limitée |

**Tests**
- **jeu de référence contrôlé** : 20 fichiers créés, 3 modifiés, 2 supprimés entre deux
  inventaires → le résultat doit être exactement celui attendu (comparaison stricte
  sur listes triées) ;
- portées différentes (`C:\` vs `D:\Projets`) → la comparaison est refusée avec un
  message explicite ;
- inventaire `light` → l'onglet fichiers est désactivé, les autres fonctionnent ;
- performance : comparaison de deux inventaires de 400 000 fichiers **< 3 s**.

**Critères de sortie**
- zéro écart sur le jeu de référence ;
- aucune comparaison « silencieusement partielle » : tout est signalé ;
- l'interface ne charge jamais les 400 000 lignes, seulement les 500 premières + export.

---

### M8 — Rapports (3-4 j)

| Livrable | Détail |
|---|---|
| `HtmlReportWriter` + gabarit + CSS embarqué | rapport autonome, hors ligne |
| `CsvReportWriter` | un fichier par section |
| `JsonReportWriter` | schéma versionné |
| `PathMasker` | option de masquage du nom d'utilisateur |
| Vue « Rapports » | sélection du contenu, destination, historique des exports |

**Tests**
- le HTML s'ouvre dans Edge et dans un navigateur hors ligne, sans requête réseau
  (vérifié au moniteur réseau) ;
- le CSV s'ouvre dans Excel et LibreOffice sans erreur d'encodage (UTF-8 avec BOM) ;
- le JSON est revalidé par un lecteur strict ;
- un inventaire `partial` produit un rapport contenant la section « zones non
  analysées », jamais vide ;
- rapport de 400 000 fichiers : génération **< 10 s**, taille maîtrisée.

**Critères de sortie**
- trois formats générés depuis le **même** modèle (aucun format dérivé d'un autre) ;
- rapport HTML lisible, imprimable, sans dépendance externe ;
- section « limites » toujours présente.

---

### M9 — Recette et durcissement (4-6 j)

| Livrable | Détail |
|---|---|
| Matrice de recette | exécutée sur le PC cible, documentée |
| Mesures finales | démarrage, mémoire, durée de scan, taille de base, temps de comparaison |
| Test de lecture seule | **Process Monitor** : aucune opération d'écriture hors `%LOCALAPPDATA%` |
| Robustesse | base corrompue, disque plein, arrêt brutal, session verrouillée |
| Distribution | ZIP portable, `SHA-256`, notes de version, `THIRD-PARTY-NOTICES.md` |
| Documentation utilisateur | 2 pages : ce que fait ATLAS PC, ce qu'il ne voit pas |

**Critères de sortie (recette globale)**

| Critère | Seuil |
|---|---|
| Démarrage à froid | < 3 s |
| Mémoire au repos | < 130 Mo |
| Mémoire en pic pendant un scan | < 250 Mo |
| Gel de l'interface | aucun > 200 ms |
| Prise en compte de l'annulation | < 500 ms |
| Inventaire complet de C: sans admin | terminé, état `complete` ou `partial` expliqué |
| Base par inventaire détaillé | < 70 Mo |
| Comparaison de deux inventaires | < 3 s |
| Écritures hors dossier applicatif | **zéro** |
| Éléments matériels non collectés | affichés `—` avec explication, jamais `0` |

---

## 3. Stratégie de test

| Niveau | Moyen | Ce qu'on couvre |
|---|---|---|
| Unitaire | NUnit/xUnit + ports simulés (`IFileSystem`, `IWmiQuery`, `IRegistryReader`, `IProcessRunner`) | logique de scan, de collecte, de détection, de comparaison |
| Intégration | base SQLite réelle dans un dossier temporaire | schéma, lots, migrations, rétention, requêtes de comparaison |
| Système | machine cible, arborescences générées | durée, mémoire, états, annulation |
| Sécurité / conformité | Process Monitor + test « garde-fou » des API d'écriture | lecture seule, absence d'écriture parasite |
| Non-régression visuelle | captures comparées à la maquette | cohérence de l'interface |

**Le test le plus important du projet** est le jeu de référence de M7 : une
arborescence créée, modifiée et partiellement supprimée entre deux inventaires, dont
le diff attendu est écrit à la main. Sans ce test, la fonctionnalité la plus
différenciante d'ATLAS PC repose sur des impressions.

---

## 4. Ce que le plan évite volontairement

| Tentation | Pourquoi elle est écartée |
|---|---|
| Commencer par l'interface complète | l'interface est la partie la moins risquée ; le moteur et la base le sont le plus |
| Ajouter le treemap en M4 | coût élevé, non exigé (livrable 6, § 6) |
| Faire du multi-inventaires avant le scan | sans scan, l'historique n'a rien à stocker |
| Optimiser avant de mesurer | les seuils sont posés en M0 puis vérifiés à chaque jalon |
| Supporter PDF, MFT, Linux, réseau | hors cahier des charges ; chaque ajout multiplie les tests |
| Écrire un installeur | un ZIP et un EXE suffisent, et évitent les droits administrateur |

---

## 5. Après la V1 (non planifié, à décider sur l'usage réel)

Dans cet ordre de valeur supposée, et seulement après mesure :

1. **treemap** borné par les pixels (si les barres ne suffisent pas) ;
2. **export PDF** (si l'archivage papier est demandé) ;
3. **comparaison à trois inventaires** (tendance plutôt que diff) ;
4. **mode élevé optionnel** pour voir les zones protégées (avec avertissement) ;
5. **planification** d'un inventaire mensuel (tâche planifiée Windows).

Aucune de ces options n'est nécessaire pour que la V1 réponde au cahier des charges.
