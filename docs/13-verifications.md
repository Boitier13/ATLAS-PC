# Vérifications de la v2.1 — constats et corrections

**Date** : 17 novembre 2026 · **Portée** : les cinq points demandés, sur la version 2.1
du dossier · **Version produite** : 2.2

> **Méthode et limite, à lire avant le reste.** Aucun code n'existe : ces vérifications
> sont **documentaires**. Elles portent sur ce que les livrables spécifient, pas sur un
> logiciel en fonctionnement. Là où le point demandé exige une mesure (« tester les
> recherches sur un volume important »), ce document fournit le **protocole** et les
> **seuils**, et signale explicitement que le chiffre reste à produire sur l'OptiPlex 780.
> Aucune fonctionnalité n'a été ajoutée ; les corrections consistent à préciser, durcir
> ou retirer.

## Synthèse

| # | Point vérifié | Verdict | Correction apportée |
|---|---|---|---|
| 1 | Inventaire sur disques choisis, recherches sans rescan | **Écart partiel** | l'absence de relecture disque n'était écrite nulle part → spécifiée et rendue testable |
| 2 | Dossiers inaccessibles enregistrés individuellement, jamais de fausses suppressions | **Écart réel** | un scan **annulé** pouvait encore produire de fausses suppressions → règle durcie |
| 3 | Recherches nom exact / préfixe / sous-chaîne à volume | **Écart partiel** | le nom exact n'avait pas de stratégie propre → court-circuit + protocole de mesure |
| 4 | Les 11 tables toutes justifiées | **Une table inutile** | `disk` fusionnée dans `volume` → **10 tables** |
| 5 | Déplacement / renommage = retrait + ajout | **Conforme mais non écrit** | décision explicitée dans 4 livrables + test dédié |

Deux écarts sur cinq : c'est le résultat attendu d'une vérification utile.

---

## 1. Inventaire sur les disques choisis, puis recherches servies par SQLite

**Ce qui était spécifié.** La sélection des disques par cases à cocher (livrable 8, étape
3A) ; une table `scan_root` par disque ; les requêtes de recherche portant sur
`inventory_id` (livrable 5 § 5).

**Constat.** Le mécanisme était correct, mais **l'interdiction de rescanner n'était écrite
nulle part**. Rien n'empêchait un développeur — ou une relecture future — d'ajouter un
« rafraîchissement » au démarrage ou à l'ouverture de la rubrique Fichiers. Sur 400 000
fichiers, cela représenterait plusieurs minutes d'attente pour un résultat identique.

**Corrections**
- livrable 4 § 6.8 : tableau **des accès disque autorisés**, moment par moment (démarrage,
  navigation, recherche, filtres, tableau de bord : aucun accès ; ouverture d'un élément :
  une vérification d'existence) ;
- livrable 5 § 5.8 : la recherche ne lit jamais le disque, et seules les tables `file`,
  `folder`, `scan_root`, `scan_issue` sont interrogées ;
- livrable 6 § 4.9 : même règle côté interface, avec la conséquence utile pour
  l'utilisateur — la recherche fonctionne même si un disque externe a été débranché ;
- livrable 7 : test `Search_NeLitPasLeDisque` — le dossier analysé est **supprimé après le
  scan**, la recherche doit renvoyer les mêmes résultats. C'est une preuve par
  comportement, pas une intention ;
- livrable 8 : nouveaux critères de recette « accès disque hors ouverture d'un élément :
  **zéro** » et « recherche après suppression du dossier analysé : résultats identiques ».

**Ce qui reste à vérifier en vrai** : que le test échoue bien si quelqu'un ajoute un
`Directory.Exists` dans un ViewModel. À contrôler à l'étape 3B.

---

## 2. Dossiers inaccessibles enregistrés un par un, sans fausse suppression

**Ce qui était spécifié.** Une ligne `scan_issue` par dossier en échec ; une garde de
comparaison qui exclut tout fichier dont le dossier — ou un ancêtre — est en erreur
(livrable 5 § 6.3).

**Constat — l'écart le plus sérieux de cette vérification.** La garde ne bloquait que les
disques au statut `inaccessible` ou `cancelled`. Or le livrable 4 § 5.4 rangeait un
**parcours annulé** dans `partial`. Conséquence : après une annulation à mi-parcours,
tous les fichiers des dossiers **jamais visités** apparaissaient comme **supprimés**
(suppression « confirmée »), alors que ces dossiers n'avaient aucune ligne `scan_issue`.
C'était précisément ce que le cahier des charges interdit. Aggravant : le lot en cours
(jusqu'à 500 fichiers) pouvait être abandonné, créant un second motif de fausse
suppression.

**Corrections**
1. **Quatre statuts distincts** (livrable 5 § 3.1) : `complete`, `partial` (parcours
   terminé **avec** erreurs), `cancelled` (parcours interrompu), `inaccessible`.
2. **Règle durcie** (livrable 5 § 6.3) : une suppression n'est confirmée que si les
   **trois** conditions sont réunies — disque `complete` ou `partial`, aucune ligne
   `scan_issue` sur le dossier ou un ancêtre, fichier absent. La condition 1 est
   désormais écrite en SQL (`NOT EXISTS (… status IN ('complete','partial'))` → non
   vérifiable).
3. **Annulation propre** (livrable 4 § 5.4) : le lot en cours est **écrit**, puis un
   `scan_issue` de nature `cancelled_before` est enregistré pour le dossier en cours et
   les dossiers restants, et la racine passe en `cancelled`. Ceinture et bretelles
   assumées sur la règle la plus importante du cahier des charges.
4. **Colonne `scan_issue.kind`** : `access_denied`, `io_error`, `path_too_long`,
   `cancelled_before`, `unexpected` — pour que l'interface explique la cause au lieu
   d'afficher un code.
5. **Tests** : `Scanner_AnnuleEtEcritLeLotEnCours`,
   `Scanner_MarqueLesDossiersNonVisites`,
   `Comparison_NApprouveAucuneSuppressionSurUnScanAnnule`.
6. **Recette** : « fausse annonce de suppression sur un disque interrompu : **zéro** ».

**Ce qui reste volontairement inchangé** : un fichier réellement supprimé entre deux
inventaires est bien annoncé comme supprimé. L'outil ne devient pas muet, il devient
prudent là où il ne peut pas savoir.

---

## 3. Recherches par nom exact, préfixe et sous-chaîne sur un gros volume

**Ce qui était spécifié.** Deux stratégies : préfixe (index, < 100 ms) et sous-chaîne
(parcours de l'index des noms, < 1,5 s), avec contingence FTS5 mesurée.

**Constat.** Le **nom exact** — le cas le plus fréquent quand on cherche « où est
`rapport-2026.pdf` » — n'avait aucune stratégie propre : il tombait dans le parcours de
sous-chaîne, donc dans le cas le plus lent, alors qu'une égalité sur l'index est
immédiate. Aucun protocole de mesure multi-volumes n'existait non plus : impossible de
savoir à partir de quel volume le seuil serait franchi.

**Corrections**
- livrable 4 § 6.4 et livrable 5 § 5.5 : **trois cas, deux stratégies**, avec le
  court-circuit `name = :saisie COLLATE NOCASE` exécuté d'abord. C'est une optimisation de
  requête, **pas** une option d'interface : rien de nouveau à l'écran ;
- trois seuils : exact **< 20 ms**, préfixe **< 100 ms**, sous-chaîne **< 1,5 s** ;
- livrable 8 § 5 : **protocole de mesure** — 4 cas (exact, préfixe, sous-chaîne,
  sous-chaîne sans résultat) × 3 volumes (100 k / 400 k / 1 M) × 2 états (à froid après
  redémarrage, à chaud), avec relevé de la durée perçue ;
- livrable 5 § 9 : vérifier au `EXPLAIN QUERY PLAN` que le filtre Emplacement utilise bien
  `ix_file_folder` (l'optimisation de `LIKE` avec un paramètre lié n'est pas garantie sur
  toutes les versions).

**Ce qui n'a pas pu être fait.** Aucune mesure : pas de machine Windows ici, et pas de
logiciel à mesurer. Les valeurs 20 ms / 100 ms / 1,5 s sont des **objectifs de
conception**, cohérents avec la taille de l'index (nom seul, ~10-15 Mo pour 400 000
fichiers, gardé en cache par `PRAGMA cache_size = -16000`), pas des résultats.

**Estimation à assumer si la contingence s'applique.** Une table FTS5 en trigrammes
ajouterait, selon la longueur moyenne des noms, de l'ordre de **40 à 80 Mo** par
inventaire et **30 à 60 %** de durée d'insertion en plus. C'est pourquoi elle reste une
contingence mesurée, jamais un choix par défaut.

---

## 4. Les tables SQLite sont-elles toutes justifiées ?

**Méthode.** Pour chaque table : quelle fonction elle sert, et ce qui casse si on la
supprime.

| Table | Fonction servie | Si supprimée |
|---|---|---|
| `inventory` | historique, comparaison | plus d'historique : la fonction Évolution disparaît |
| `scan_root` | statut par disque, couverture, garde anti-fausse-suppression | impossible de dire qu'un disque est partiel ou interrompu ; la condition 1 de la garde disparaît |
| `computer` | rubrique Ordinateur | rubrique vidée |
| `volume` | volumes, remplissage, disques physiques | rubriques Ordinateur et Tableau de bord amputées |
| `software` | rubrique Logiciels | rubrique vidée |
| `tool` | rubrique Outils | rubrique vidée |
| `project` | rubrique Développement | rubrique vidée |
| `folder` | arbre, totaux, nombre de fichiers et de sous-dossiers (point 9 de la demande) | arbre reconstruit par découpage de chaînes, dossiers vides perdus, comptages à recalculer |
| `file` | recherche, comparaison, tableau de bord | plus rien |
| `scan_issue` | une ligne par dossier en échec, garde de comparaison | fausses suppressions en série |
| ~~`disk`~~ | modèle et taille du disque physique | **rien** : information déjà portée par `volume` |

**Constat.** Dix tables sont indispensables au sens strict. La onzième, `disk`, ne servait
qu'à afficher le modèle et la taille du disque physique — affichage possible depuis
`volume` en dupliquant trois colonnes sur 1 à 4 lignes. Elle a donc été **fusionnée** :
`volume` gagne `disk_index`, `disk_model`, `disk_size_bytes`.

**Correction** : le modèle passe à **10 tables** (livrable 5 § 2 et § 3, livrable 4 § 8,
livrable 7 § 5, livrable 9). Contrepartie assumée et écrite : un disque physique **sans
aucun volume monté** n'apparaît plus dans la rubrique Ordinateur — cas rare, sans effet
sur les fonctions essentielles.

**Deux dénormalisations examinées et conservées**, parce qu'elles servent une demande
explicite :

| Donnée dupliquée | Pourquoi elle reste |
|---|---|
| `folder.files_count` / `dirs_count` | le point 9 de la demande exige le nombre de fichiers et de sous-dossiers par dossier ; les calculer à la volée coûterait un agrégat par nœud d'arbre |
| `folder.size_bytes` (cumul) | tri de l'arbre par taille et affichage immédiat, sans agrégat récursif |

Aucune table de statistiques, de correspondance de déplacements, de hachage ou de
rétention n'a été introduite.

---

## 5. Déplacement et renommage : retrait + ajout, sans reconnaissance

**Ce qui était spécifié.** La comparaison joint les fichiers sur `(folder_path, name)`
(livrable 5 § 6). Un fichier dont le nom change n'est donc apparié avec rien.

**Constat.** Le **comportement était correct**, mais **la décision n'était écrite nulle
part**. Un développeur — ou une relecture future — pouvait légitimement croire à un oubli
et ajouter une heuristique (taille + date identiques) ; c'eût été du travail, du risque de
faux positifs et une fonctionnalité non demandée. Le risque de dérive était réel.

**Corrections**
- livrable 5 § 6.4 : décision explicite, avec trois raisons (problème difficile,
  résultat parfois faux, non demandé) et le cas du renommage **de dossier** ;
- livrable 4 § 11 : ajout dans la table des refus définitifs ;
- livrable 6 § 5.6 : mention **à l'écran**, en bas de l'onglet *Supprimés* ;
- livrable 9 : limite assumée, avec la précision qu'il s'agit d'une décision et non d'un
  défaut ;
- livrable 7 : test `Comparison_TraiteUnRenommageCommeRetraitPlusAjout` — même taille,
  même date, nom différent → exactement 1 supprimé + 1 ajouté ;
- livrable 8 § 9 : ligne dans « ce que le plan évite ».

**Effet de bord à connaître, et à ne pas masquer** : renommer un dossier contenant
10 000 fichiers produit 10 000 suppressions et 10 000 ajouts. L'affichage étant plafonné à
500 lignes par onglet, l'interface indique le total et propose de filtrer par dossier —
c'est un cas où le plafond est visible, pas silencieux.

---

## 6. Ce qui reste à vérifier sur la machine cible

Ces cinq points sont **documentaires** ; les mesures appartiennent aux étapes 3A et 3B.

| # | À mesurer | Étape | Seuil de décision |
|---|---|---|---|
| 1 | Durée d'un inventaire complet de `C:` | 3A | > 20 min → énumérateur Win32 |
| 2 | Pic mémoire pendant le scan | 3A | > 200 Mo → revoir le lot et le cache |
| 3 | Taille réelle de la base | 3A | > 150 Mo → `folder_id` dans `file` |
| 4 | Les trois recherches sur 100 k / 400 k / 1 M | 3B | sous-chaîne > 1,5 s → FTS5 |
| 5 | Lecture disque pendant une recherche | 3B | toute lecture → à supprimer |
| 6 | Fausses suppressions après annulation | 5 | toute ligne confirmée → bloquant |

## 7. Fichiers modifiés par cette vérification

| Fichier | Modification |
|---|---|
| `docs/07-architecture-cible.md` | 4 statuts de racine, annulation propre, § 6.4 trois cas de recherche, § 6.8 accès disque, 10 tables |
| `docs/08-modele-sqlite.md` | fusion de `disk`, `scan_issue.kind`, garde durcie (§ 6.3), § 6.4 renommage, § 5.8 pas de lecture disque |
| `docs/09-interface.md` | disque interrompu, § 4.9 accès disque, mention renommage, disques non comparables |
| `docs/10-arborescence-projet.md` | `DiskInfo` retirée, 3 tests ajoutés, 10 tables |
| `docs/11-plan-de-developpement.md` | critères 3A (statuts, annulation), protocole de mesure § 5, recette (5 critères ajoutés) |
| `docs/12-risques-et-limites.md` | R2 reformulé et abaissé, R9b, limite renommage, points 2 et 6b |
| `docs/00-lisez-moi.md` | § 5.10, chiffres, version 2.2 |
| `README.md` | 10 tables, version 2.2 |

---

## 8. Audit de cohérence de la v2.2 (17/11, après corrections)

Sept contrôles automatiques et manuels sur les quatorze documents.

| # | Contrôle | Résultat | Correction |
|---|---|---|---|
| 1 | Nombre de tables (07, 08, 10, 11, 12, 00, README) | conforme : **10** partout ; les mentions de 11 ou 20 sont historiques et datées | — |
| 2 | Somme des durées du plan | 3 + 3 + 5 + 5 + 3 + 4 = **23 j**, conforme au total annoncé | — |
| 3 | Statuts de racine (`complete`/`partial`/`cancelled`/`inaccessible`) dans 07, 08, 09, 12 | conforme | — |
| 4 | Seuils (20 ms / 100 ms / 1,5 s ; 130/200/250 Mo ; 120 Mo de base) | conformes d'un document à l'autre | — |
| 5 | Nombre de tests annoncé (7 § 4, 8 § 5, 00 § 6) | **incohérent** : 25 tests réels, annoncés 23 puis 17 | corrigé en **25 tests, dont 11 sur la rubrique Fichiers** |
| 6 | Nombre de fichiers du modèle (10 § 2) | **incohérent** : 13 fichiers listés, 12 annoncés ; total 56 et non 55 | corrigé |
| 7 | Références croisées « livrable N § x », liens, tableaux Markdown | toutes résolues ; deux `||` non échappés dans des tableaux (07 § 6.5, 08 § 5.5) | échappés en `\|\|` |

Deux corrections mineures supplémentaires : la description de `ScanIssue.cs` mentionne
désormais la **nature** de l'incident, et le risque R2 parle de **trois** tests dédiés au
lieu de deux.

**Conclusion de l'audit** : la v2.2 est cohérente. Les seuls écarts trouvés portaient sur
des comptages de documentation, jamais sur une règle technique — ce qui est le bon signe :
les corrections de fond (statut `cancelled`, garde à trois conditions, fusion de `disk`)
sont passées sans laisser de contradiction derrière elles.

