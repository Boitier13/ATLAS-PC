# Livrable 8 — Plan de développement

> **Révision du 17 novembre 2026 (3ᵉ).** Le plan garde **cinq étapes**, l'étape 3 étant
> scindée en **3A** (scan et stockage) et **3B** (la rubrique Fichiers). La vérification du
> 17/11 ajoute trois critères mesurables : les trois cas de recherche (§ 5), l'absence de
> relecture du disque, et le traitement d'un renommage comme retrait + ajout (§ 7).

Hypothèse : une personne, à temps partiel. Les durées ordonnent le travail ; ce ne sont
pas des engagements.

---

## 1. Vue d'ensemble

| Étape | Objet | Durée | À la fin, on peut… |
|---|---|---|---|
| **1** | Application et interface de base — **validée le 17/11** | 3 j | lancer ATLAS PC, naviguer dans les 7 rubriques, créer un inventaire vide |
| **2** | Inventaire matériel et Windows | 3 j | consulter la fiche complète du PC |
| **3A** | Scan des fichiers et stockage | 5 j | lancer un inventaire de `C:` et `D:` et le voir progresser |
| **3B** | **Rubrique Fichiers** : exploration, recherche, filtres, ouverture | 5 j | **retrouver un fichier par son nom, filtrer, ouvrir, copier son chemin** |
| **4** | Logiciels, outils et projets | 3 j | consulter les logiciels, les 6 outils, les projets Python/Node |
| **5** | Évolution, comparaison et recette | 4 j | comparer deux inventaires et vérifier toute la matrice |
| | **Total** | **23 j** | |

Le chemin critique est **3A → 3B** : neuf jours, plus du tiers du projet, sur la seule
fonction qui justifie le logiciel.

```mermaid
flowchart LR
    E1[1 · socle] --> E2[2 · matériel]
    E2 --> E3A[3A · scan + base]
    E3A --> E3B[3B · rubrique Fichiers]
    E3B --> E4[4 · logiciels/outils/projets]
    E4 --> E5[5 · évolution + recette]
```

Chaque étape produit une application qui démarre et une fonction utilisable. Si le
projet s'arrêtait après 3B, ATLAS PC serait déjà utile.

---

## 2. Étape 1 — Application et interface de base (3 j) — **validée le 17/11**

| Livrable | Détail |
|---|---|
| Solution et projet unique | `net48`, `WinExe`, `UseWPF`, un seul paquet `System.Data.SQLite.Core` |
| `ShellWindow` | barre latérale 7 entrées, barre supérieure, zone de contenu, badge d'état |
| `Themes/Theme.xaml` | neutres, rampe d'accent, **couleurs de familles**, couleurs d'état |
| Vues des rubriques à venir | une vue de remplacement unique (`PlaceholderView`) portant le titre et l'état vide de la rubrique ; chaque vue réelle la remplace à son étape |
| `Database.cs` + `Schema.sql` | création de la base et du **schéma complet** (10 tables, idempotent) : pas de migration |
| `InventoryStore` | créer, lister, clôturer, supprimer un inventaire ; statut `running` pendant le parcours |
| Bandeau de progression | factice, pour valider sa place dans la mise en page |

**Tests** : navigation, création et suppression d'inventaire, persistance après
redémarrage — **réussis le 17/11**. Démarrage à froid et mémoire au repos : non relevés.

**Statut.** Étape clôturée le 17/11 sur Windows 10 (MSBuild 17.14.60, 0 erreur,
0 avertissement). Recette détaillée : `14-journal-de-developpement.md` § 1.6. Le code du
jalon est gelé au commit `2c15ca3`.

**Critères de sortie**
- démarrage à froid **< 3 s** ; mémoire au repos **< 130 Mo** ;
- base créée dans `%LOCALAPPDATA%\ATLAS PC\` ;
- la fenêtre se redimensionne sans casser la mise en page.

> **Point de décision.** Si le démarrage dépasse 5 s ou la mémoire 200 Mo, deux options
> sont évaluées **avant** l'étape 2 : rester en WPF en allégeant le thème (moins de
> styles, pas de transparence), ou passer en WinForms.

---

## 3. Étape 2 — Inventaire matériel et Windows (3 j) — **validée le 17/11**

| Livrable | Détail |
|---|---|
| `ComputerCollector` | **10 appels WMI** (8 classes + 2 associations) + registre, un `try/catch` par appel, 10 s par appel, budget 30 s |
| Tables `computer`, `volume` | écriture d'un inventaire matériel, transaction unique |
| Mémoire | **installée** (somme des modules `Win32_PhysicalMemory`) et **utilisable** (`TotalPhysicalMemory`) stockées et affichées séparément |
| Threads | `cpu_threads` affiché à part des cœurs physiques |
| Vue **Ordinateur** | 6 cartes + tableau disques/volumes avec barre de remplissage colorée + note de collecte |
| Valeurs absentes | `—` + info-bulle, jamais `0` |

**Statut d'un inventaire de l'étape 2.** Matériel lu, fichiers non parcourus : l'inventaire
est clôturé en **`partial`**, avec une note qui donne le nombre d'appels WMI réussis, les
incidents et la phrase « Fichiers non inventoriés ». Aucune ligne de l'historique ne peut
être lue comme un inventaire complet.

**Critères de sortie** : collecte **< 15 s** ; aucune exception non gérée si WMI est
indisponible ; exécution en utilisateur standard ; 100 % des champs affichés ont une
valeur ou un `—` justifié.

---

## 4. Étape 3A — Scan des fichiers et stockage (5 j)

| Livrable | Détail |
|---|---|
| `FileScanner` | `DirectoryInfo.EnumerateFileSystemInfos`, DFS, un thread, une racine après l'autre |
| `BatchWriter` + `ScanStore` | lots de 500 entrées ou 2 s, transaction par lot |
| Tables `folder`, `file`, `scan_issue` | avec les index du livrable 5, dont `ix_file_name` |
| `ProjectDetector` | branché dans la boucle (aucune lecture supplémentaire) |
| Sélection des disques | cases à cocher, **choisies par l'utilisateur** ; un inventaire ne démarre que sur les disques cochés |
| Statut par racine | `complete`, `partial`, `cancelled`, `inaccessible` — et l'annulation écrit le lot en cours puis marque les dossiers restants |
| Progression et annulation | bandeau, `Échap`, états par racine |

**Tests** : totaux exacts sur une arborescence générée, points de reparse non suivis,
**un dossier inaccessible = une ligne `scan_issue`**, annulation → lot en cours écrit +
dossiers restants marqués `cancelled_before` + racine `cancelled`, clôture par racine
(deux disques = deux statuts), pic mémoire, garde-fou de lecture seule.

**Critères de sortie**
- `C:` analysé entièrement sans administrateur, statut explicite ;
- pic mémoire **< 200 Mo** ; annulation prise en compte **< 500 ms** ;
- base **< 120 Mo** pour 400 000 fichiers ;
- **durée mesurée et consignée** (seuil d'alerte : 20 minutes).

> **Décision conditionnelle (moteur).** Si la durée dépasse 20 minutes, ou si les chemins
> longs échouent en masse, on ajoute alors — et seulement alors — un énumérateur
> `FindFirstFileExW` dans **une seule classe**. Décision et mesure documentées.

---

## 5. Étape 3B — La rubrique Fichiers (5 j)

C'est l'étape qui décide de la valeur du logiciel. Elle se découpe en cinq demi-journées
identifiables.

| Jour | Livrable | Détail |
|---|---|---|
| **J1** | Navigation | `BrowseQueries`, `FilesViewModel` : liste des disques avec statut, arbre paresseux, fil d'Ariane, contenu d'un dossier (sous-dossiers + fichiers paginés) |
| **J2** | Tableau et tri | colonnes Nom/Type/Taille/Modifié/Chemin, pastilles de famille, pagination de 200, tri sur toutes les colonnes, ligne d'état du dossier |
| **J3** | Recherche | `SearchQueries`, `SearchViewModel` : préfixe puis sous-chaîne, exécution hors du thread d'affichage, annulation, compteur « x résultats (200 affichés) », mention de couverture |
| **J4** | Filtres | extension, famille, taille, date, emplacement ; cumul, « tout effacer », pastilles actives, comptage |
| **J5** | Actions et finitions | `ItemOpener` (ouverture vérifiée), `ClipboardHelper` (copier le chemin / le nom), menu contextuel, états vides, libellés, revue visuelle contre la maquette |

**Tests** (les 11 de la rubrique Fichiers, livrable 7 § 4) : préfixe et sous-chaîne sur
le même jeu attendu, casse, pagination sans doublon ni trou, chaque filtre et leur
combinaison, restriction au dossier courant, couverture signalée, ouverture refusée sur
chemin disparu, absence d'API d'écriture dans `Shell`.

**Critères de sortie**

| Critère | Seuil |
|---|---|
| Recherche par **nom exact** (court-circuit d'index) | < 20 ms |
| Recherche par préfixe | < 100 ms |
| Recherche par sous-chaîne (400 000 fichiers) | **< 1,5 s**, à froid et à chaud |
| Application d'un filtre (page) | < 300 ms |
| Dépliage d'un dossier de 5 000 fichiers | < 400 ms pour la première page |
| Figement de l'interface pendant une recherche | aucun > 200 ms |
| Mémoire avec 10 000 résultats parcourus | **< 250 Mo** (pagination oblige) |
| Affichage d'un total sans mention de couverture | **zéro** |
| Démarrage d'une application/l'Explorateur hors ATLAS PC | uniquement sur action explicite |

### Protocole de mesure des trois recherches

À exécuter sur l'OptiPlex 780, base réelle, application lancée, une seule fois par
configuration. Résultat consigné dans ce document.

| Cas | Saisie | 100 000 fichiers | 400 000 fichiers | 1 000 000 fichiers |
|---|---|---|---|---|
| Nom exact | un nom complet existant | à froid / à chaud | à froid / à chaud | à froid / à chaud |
| Préfixe | les 5 premiers caractères | idem | idem | idem |
| Sous-chaîne | 4 caractères pris au milieu | idem | idem | idem |
| Sous-chaîne sans résultat | chaîne inexistante | idem | idem | idem |

- « à froid » : premier lancement après redémarrage, cache système vidé par le
  redémarrage ;
- « à chaud » : deuxième exécution immédiate ;
- on relève la **durée perçue** (du dernier caractère saisi à l'affichage de la première
  page), pas seulement le temps SQL ;
- à 1 000 000 de fichiers, si la sous-chaîne dépasse 1,5 s à chaud, la contingence FTS5
  s'applique sans discussion.

> **Décision conditionnelle (recherche).** Si la mesure de la sous-chaîne dépasse
> **1,5 s** sur la machine cible, on ajoute la table virtuelle `file_name_fts`
> (FTS5, `tokenize='trigram'`), alimentée en fin de scan. C'est **une table et une
> requête** de plus : le schéma des 10 tables ne change pas, aucune autre partie du
> logiciel n'est touchée. La mesure et la décision sont consignées.

---

## 6. Étape 4 — Logiciels, outils et projets (3 j)

| Livrable | Détail |
|---|---|
| `SoftwareCollector` | 3 clés `Uninstall`, filtres documentés |
| `ToolCollector` | 6 outils, `PATH` + dossiers connus, `FileVersionInfo` puis `--version` (3 s) |
| Vues **Logiciels**, **Outils**, **Développement** | tableaux, fiches, filtres en pastilles |

**Critères de sortie** : logiciels **< 3 s** ; outils **< 5 s** avec commandes, **< 1 s**
sans ; aucun faux positif de projet sur une arborescence piégée (`node_modules`, `venv`,
`dist`) ; **aucune régression** de la durée du scan (< 2 %).

---

## 7. Étape 5 — Évolution, comparaison et recette (4 j)

| Livrable | Détail |
|---|---|
| `ComparisonQueries` | ajoutés, supprimés (+ non vérifiables), modifiés, espace occupé |
| Vue **Évolution** | sélecteurs A/B avec disques couverts, 4 blocs, 4 onglets, refus si disques différents |
| Suppression d'inventaire | bouton + confirmation, taille affichée, avertissement au-delà de 10 |
| Matrice de recette | exécutée sur le PC cible et consignée |
| Audit de lecture seule | **Process Monitor** pendant un inventaire complet et pendant une session de recherche |
| Distribution | dossier compressé, `README` de prise en main, notes de version |

**Tests** : jeu de référence (20 fichiers créés, 3 modifiés, 2 supprimés) → résultat
exactement conforme ; **zone non analysée** → « disparitions non vérifiables », jamais
« supprimés » ; **disque annulé** → aucune suppression confirmée ; **renommage** → 1
supprimé + 1 ajouté, sans reconnaissance de déplacement ; disques différents →
« non comparable » avec message ; inventaire `partial` → bandeau et comportement
cohérent.

**Recette globale**

| Critère | Seuil |
|---|---|
| Démarrage à froid | < 3 s |
| Mémoire au repos | < 130 Mo |
| Pic mémoire pendant un scan | < 200 Mo |
| Mémoire après 10 000 résultats consultés | < 250 Mo |
| Gel de l'interface | aucun > 200 ms |
| Annulation d'un scan | < 500 ms |
| Recherche par sous-chaîne | < 1,5 s |
| Inventaire complet sans admin | terminé, statut par disque expliqué |
| Base par inventaire | < 120 Mo |
| Comparaison de deux inventaires | < 3 s |
| Écritures hors `%LOCALAPPDATA%\ATLAS PC\` | **zéro** |
| Fausse annonce de suppression en zone non analysée | **zéro** |
| Fausse annonce de suppression sur un disque interrompu | **zéro** |
| Total de résultats affiché sans mention de couverture | **zéro** |
| Accès disque hors ouverture d'un élément | **zéro** |
| Recherche après suppression du dossier analysé | résultats identiques |

---

## 8. Stratégie de test, en une page

| Niveau | Moyen | Ce qu'on couvre |
|---|---|---|
| Unitaire | xUnit ou NUnit, sur classes réelles | totaux, filtres, familles, pagination, détection de projets, requêtes de comparaison |
| Intégration | dossiers temporaires réels + base temporaire | scan, annulation, navigation, recherche, comparaison |
| Performance | chronomètres sur la machine cible | démarrage, scan, recherche, filtres, pagination |
| Système | machine cible | états, valeurs matérielles, couverture |
| Conformité | Process Monitor + test par réflexion | lecture seule, y compris l'ouverture d'éléments |
| Visuel | captures comparées aux deux maquettes | cohérence de l'interface |

Pas d'interfaces ni de doublures : les tests utilisent le vrai code sur de vraies
données temporaires.

---

## 9. Ce que le plan évite

| Tentation | Pourquoi elle est écartée |
|---|---|
| Commencer par les six rubriques secondaires | elles sont faciles et ne décident de rien |
| Livrer la rubrique Fichiers sans fil ni recherche | ce serait un tableau d'inventaire, exactement ce qu'il ne faut pas |
| Ajouter FTS5 « pour être sûr » | coût de stockage réel ; décision prise sur mesure, pas par principe |
| Charger tous les résultats pour les compter | la pagination et le `COUNT` séparé suffisent |
| Détecter les déplacements et renommages | décision de V1 : retrait + ajout, sans heuristique (§ 7) |
| Filtrer en mémoire | les filtres sont du SQL, sinon la pagination devient fausse |
| Ajouter le treemap en étape 3B | non exigé, coûteux |
| Optimiser avant de mesurer | les seuils sont posés à l'étape 1 et vérifiés à chaque étape |
| Développer ailleurs que sur la cible | toute mesure doit être faite sur l'OptiPlex 780 |

---

## 10. Après la V1

Aucune fonctionnalité n'est planifiée. Si l'usage réel le justifie, les candidats
seraient, dans cet ordre : index plein texte si la recherche se révèle trop lente,
taille allouée réelle, recherche dans le contenu. **Aucun n'est prévu, aucun n'est à
moitié implémenté dans la V1.**
