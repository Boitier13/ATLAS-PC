# Livrable 8 — Plan de développement en cinq étapes

> **Révision du 17 novembre 2026.** Dix jalons deviennent **cinq**. Chaque étape
> produit une application qui démarre et une fonctionnalité réellement utilisable.

Hypothèse : une personne, à temps partiel. Les durées ordonnent le travail ; ce ne sont
pas des engagements.

---

## 1. Vue d'ensemble

| Étape | Objet | Durée | À la fin, on peut… |
|---|---|---|---|
| **1** | Application et interface de base | 3 j | lancer ATLAS PC, naviguer dans les 7 rubriques, créer un inventaire vide en base |
| **2** | Inventaire matériel et Windows | 3 j | consulter la fiche complète du PC (CPU, RAM, disques, volumes, Windows, BIOS) |
| **3** | Inventaire des fichiers et stockage | 6 j | lancer un inventaire complet de `C:`, explorer dossiers et fichiers, voir les volumes occupés |
| **4** | Logiciels, outils et projets | 3 j | consulter les logiciels installés, les 6 outils, les projets Python/Node |
| **5** | Historique, comparaison et vérifications | 4 j | comparer deux inventaires et savoir exactement ce qui a changé |
| | **Total** | **19 j** | |

Le chemin critique est l'**étape 3** : c'est là que se jouent la durée du scan, la
mémoire et la fluidité de l'interface.

```mermaid
flowchart LR
    E1[1 · socle] --> E2[2 · matériel]
    E2 --> E3[3 · fichiers + SQLite]
    E3 --> E4[4 · logiciels/outils/projets]
    E4 --> E5[5 · historique + recette]
```

---

## 2. Étape 1 — Application et interface de base (3 j)

**Objectif** : avoir une application qui tourne, se navigue, et parle à la base.

| Livrable | Détail |
|---|---|
| Solution et projet unique | `net48`, `WinExe`, `UseWPF`, un seul paquet `System.Data.SQLite.Core` |
| `ShellWindow` | barre latérale 7 entrées, barre supérieure, zone de contenu, badge d'état |
| `Themes/Theme.xaml` | couleurs, typographie, styles de tableau, de bouton, de carte |
| 7 vues vides | chacune avec son état vide et son titre |
| `Database.cs` + `Schema.sql` | création de la base, table `inventory` |
| `InventoryStore` | créer, lister, supprimer un inventaire |
| `ScanProgress` + bandeau | bandeau de progression factice, pour valider la place dans la mise en page |

**Tests**
- démarrage à froid, 5 essais après redémarrage ;
- navigation entre les 7 rubriques, aucun défilement horizontal à 1440 × 900 ;
- création puis suppression d'un inventaire, en cascade.

**Critères de sortie**
- démarrage à froid **< 3 s** ;
- mémoire au repos **< 130 Mo** ;
- la base est créée dans `%LOCALAPPDATA%\ATLAS PC\` ;
- la fenêtre se redimensionne sans casser la mise en page.

> **Point de décision.** Si le démarrage dépasse 5 s ou la mémoire 200 Mo, deux
> options sont évaluées **avant** l'étape 2 : rester en WPF en allégeant le thème, ou
> passer en WinForms. Le choix est consigné dans ce document.

---

## 3. Étape 2 — Inventaire matériel et Windows (3 j)

| Livrable | Détail |
|---|---|
| `ComputerCollector` | 7 requêtes WMI + registre `CurrentVersion`, un `try/catch` par requête, délai 10 s |
| Tables `computer`, `disk`, `volume` | écriture d'un inventaire matériel |
| Vue **Ordinateur** | 6 fiches + tableau disques/volumes |
| Affichage des valeurs absentes | `—` + info-bulle, jamais `0` |

**Tests**
- exécution réelle sur la machine cible, valeurs comparées aux valeurs connues ;
- test unitaire des auxiliaires (formatage des octets, composition du numéro de build) ;
- test d'absence : sur une machine où une requête échoue, les autres sections restent
  renseignées.

**Critères de sortie**
- collecte matérielle **< 15 s** ;
- aucune exception non gérée si WMI est indisponible ;
- exécution en utilisateur standard, manifeste `asInvoker` ;
- 100 % des champs affichés ont une valeur ou un `—` justifié.

---

## 4. Étape 3 — Inventaire des fichiers et stockage SQLite (6 j)

C'est l'étape la plus longue et la plus risquée.

| Livrable | Détail |
|---|---|
| `FileScanner` | `DirectoryInfo.EnumerateFileSystemInfos`, DFS, un thread |
| `BatchWriter` + `ScanStore` | lots de 500 entrées ou 2 s, transaction par lot |
| Tables `folder`, `file`, `scan_issue` | avec les index du livrable 5 |
| `ProjectDetector` | branché dans la boucle (aucune lecture supplémentaire) |
| Vue **Fichiers** | arborescence paresseuse, tableau virtualisé, recherche, compteur d'incidents |
| Tableau de bord réel | 4 cartes, répartition par dossier, types de fichiers, liste des inventaires |
| Progression et annulation | bandeau, `Échap`, états `complete` / `partial` / `cancelled` |

**Tests**
- totaux de dossier exacts sur une arborescence générée (comparaison avec un calcul de
  référence) ;
- points de reparse non suivis ;
- dossier inaccessible → une ligne `scan_issue`, pas d'arrêt ;
- annulation → inventaire `partial`, données cohérentes, base réutilisable ;
- mémoire : suivi du pic pendant un scan complet, assertion **< 200 Mo** ;
- garde-fou de lecture seule : le test échoue si une API d'écriture apparaît dans
  `Scanning`.

**Critères de sortie**
- volume `C:` analysé entièrement sans administrateur ;
- pic mémoire **< 200 Mo** ;
- annulation prise en compte **< 500 ms** ;
- base **< 80 Mo** pour 400 000 fichiers ;
- les trois requêtes du tableau de bord répondent en **< 1 s** ;
- **durée mesurée et consignée** (seuil d'alerte : 20 minutes).

> **Décision conditionnelle.** Si la durée dépasse 20 minutes, ou si les chemins longs
> échouent en masse, on ajoute alors — et seulement alors — un énumérateur
> `FindFirstFileExW` dans **une seule classe**, sans toucher au reste. La décision et
> sa mesure sont documentées.

---

## 5. Étape 4 — Logiciels, outils et projets (3 j)

| Livrable | Détail |
|---|---|
| `SoftwareCollector` | 3 clés `Uninstall`, filtres documentés |
| `ToolCollector` | 6 outils, `PATH` + dossiers connus, `FileVersionInfo` puis `--version` (3 s) |
| Vue **Logiciels** | tableau + recherche |
| Vue **Outils** | 6 fiches avec statut |
| Vue **Développement** | filtres Python / Node, tableau, compteur de dépôts Git |

**Tests**
- filtrage du registre : entrées sans nom, `SystemComponent`, `ParentKeyName` ;
- `ProjectDetector` sur une arborescence piégée (`node_modules` avec 50 `package.json`,
  `venv`, `dist`, `target`) → **aucun** faux projet ;
- un outil absent des 6 ne bloque pas les 5 autres ;
- deux inventaires consécutifs sans changement produisent **zéro** différence sur les
  logiciels.

**Critères de sortie**
- logiciels collectés **< 3 s** ;
- outils détectés **< 5 s** avec commandes, **< 1 s** sans ;
- aucun faux positif de projet sur l'arborescence piégée ;
- aucune régression de durée du scan (surcoût < 2 %).

---

## 6. Étape 5 — Historique, comparaison et vérifications finales (4 j)

| Livrable | Détail |
|---|---|
| `ComparisonQueries` | ajoutés, supprimés (+ non vérifiables), modifiés, espace occupé |
| Vue **Évolution** | sélecteurs A/B, 4 blocs de synthèse, 4 onglets |
| Suppression d'inventaire | bouton + confirmation, taille affichée, avertissement au-delà de 10 |
| Matrice de recette | exécutée sur le PC cible et consignée |
| Audit de lecture seule | **Process Monitor** pendant un inventaire complet |
| Distribution | dossier compressé, `README` de prise en main, notes de version |

**Tests**
- **jeu de référence** : 20 fichiers créés, 3 modifiés, 2 supprimés entre deux
  inventaires → résultat exactement conforme à l'attendu ;
- **zone non analysée** : un dossier rendu illisible entre A et B → ses fichiers
  apparaissent en « disparitions non vérifiables », **jamais** en « supprimés » ;
- racines différentes → comparaison refusée avec un message ;
- inventaire `partial` → bandeau et comportement cohérent ;
- performance : comparaison de deux inventaires de 400 000 fichiers **< 3 s**.

**Critères de sortie (recette globale)**

| Critère | Seuil |
|---|---|
| Démarrage à froid | < 3 s |
| Mémoire au repos | < 130 Mo |
| Pic mémoire pendant un scan | < 200 Mo |
| Gel de l'interface | aucun > 200 ms |
| Annulation | < 500 ms |
| Inventaire complet de `C:` sans admin | terminé, `complete` ou `partial` expliqué |
| Base par inventaire | < 80 Mo |
| Comparaison de deux inventaires | < 3 s |
| Écritures hors `%LOCALAPPDATA%\ATLAS PC\` | **zéro** |
| Fausse annonce de suppression en zone non analysée | **zéro** |

---

## 7. Stratégie de test, en une page

| Niveau | Moyen | Ce qu'on couvre |
|---|---|---|
| Unitaire | xUnit ou NUnit, sur fonctions et classes réelles | calcul des totaux, filtrage du registre, détection de projets, requêtes de comparaison |
| Intégration | dossiers temporaires réels + base temporaire | scan complet, annulation, comparaison de bout en bout |
| Système | machine cible | durée, mémoire, états, valeurs matérielles |
| Conformité | Process Monitor + test par réflexion | lecture seule |
| Visuel | captures comparées à la maquette | cohérence de l'interface |

Pas d'interfaces ni de doublures : les tests utilisent le vrai code sur de vraies
données temporaires. C'est plus court à écrire et plus proche du comportement réel.

---

## 8. Ce que le plan évite

| Tentation | Pourquoi elle est écartée |
|---|---|
| Commencer par l'interface complète | le risque est dans le moteur et la base |
| Ajouter le treemap en étape 3 | non exigé, coûteux |
| Écrire des exports « pour plus tard » | la fonction est retirée du périmètre |
| Optimiser avant de mesurer | les seuils sont posés à l'étape 1 et vérifiés à chaque étape |
| Multi-cibler .NET dès le départ | complexité sans bénéfice démontré |
| Développer ailleurs que sur la cible | toute mesure doit être faite sur l'OptiPlex 780 |

---

## 9. Après la V1

Aucune fonctionnalité n'est planifiée. Si l'usage réel le justifie, les candidats
seraient, dans cet ordre : taille allouée réelle, filtres avancés dans la vue Fichiers,
export d'un inventaire. **Aucun n'est prévu, aucun n'est à moitié implémenté dans la
V1.**
