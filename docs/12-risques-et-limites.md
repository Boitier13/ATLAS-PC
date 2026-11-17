# Livrable 9 — Risques, limites et points à vérifier

> **Révision du 17 novembre 2026 (3ᵉ).** Trois risques restent en tête : la **fausse
> exhaustivité**, la **fausse suppression** et la **lenteur de la recherche par
> sous-chaîne**. La vérification du 17/11 a refermé une faille réelle sur le deuxième
> (R2 : un scan **annulé** pouvait encore produire de fausses suppressions) et a retiré
> une table inutile (`disk`).

---

## 1. Registre des risques

| # | Risque | Prob. | Impact | Criticité | Parade | Vérifié à |
|---|---|---|---|---|---|---|
| R1 | **Un résultat présenté comme exhaustif alors que le scan n'a pas tout couvert** | 3 | 5 | **15** | statut par disque (`scan_root`) + `scan_issue` ; mention obligatoire à côté de chaque total ; 5 affichages prévus | 3A, 3B |
| R2 | **Fausse annonce de suppression** — zone non analysée **ou disque interrompu** | 2 | 5 | **10** | une ligne `scan_issue` **par dossier** en échec ; statut `cancelled` distinct de `partial` et bloquant toute suppression ; deux tests dédiés | 3A, 5 |
| R3 | **Recherche par sous-chaîne trop lente** (aucun index ne sert un `LIKE '%x%'` comme intervalle) | 3 | 4 | **12** | parcours de `ix_file_name` (index compact) + pagination ; contingence FTS5 trigram **mesurée** | 3B |
| R4 | **Windows 10 22H2 hors support** (14/10/2025 ; ESU jusqu'en 2028), .NET Framework lié à l'OS | 5 | 3 | **15** | `net48`, aucune dépendance externe, documentation | 1 |
| R5 | **Zones inaccessibles sans administrateur** : l'inventaire est structurellement incomplet | 5 | 3 | **15** | compteur permanent, liste des zones, badge `Partiel`, garde sur les suppressions | 3A |
| R6 | **Parcours standard trop lent** sur disque mécanique | 3 | 4 | **12** | mesure à 3A ; contingence `FindFirstFileExW` dans une classe | 3A |
| R7 | **Durée d'inventaire longue** (5-15 min estimés pour `C:`) | 4 | 3 | **12** | progression crédible, annulation rapide, statut par racine | 3A |
| R8 | **Effets de bord de l'ouverture** : lancer un fichier exécute un programme, et le système peut l'ajouter aux « éléments récents » | 3 | 3 | **9** | ouverture sur action explicite seulement ; chemin issu de la base, jamais d'une saisie ; mention dans la note de bas de tableau | 3B |
| R9 | **Base volumineuse** (85-110 Mo par inventaire, index de recherche) et utilisateur qui ne supprime pas | 4 | 3 | **12** | taille affichée, avertissement au-delà de 10 inventaires, suppression en un clic | 3A, 5 |
| R9b | **Table ou colonne inutile** ajoutée par confort, qui alourdit le schéma et la maintenance | 3 | 2 | **6** | audit des 10 tables (livrable 5 § 2) ; toute table doit citer la fonction qu'elle sert | continu |
| R10 | **Démarrage à froid trop lent** (WPF, antivirus) | 3 | 3 | **9** | `net48` sans auto-extraction ; mesure et point de décision à l'étape 1 | 1 |
| R11 | **WMI lent ou indisponible** | 3 | 3 | **9** | délai de 10 s par requête, `try/catch` par requête, valeurs `—` | 2 |
| R12 | **Régression de périmètre** (treemap, exports, portables, recherche dans le contenu) | 3 | 3 | **9** | liste des refus (livrable 4, § 11) | continu |
| R13 | **Contamination de licence** (copie de code GPL de WinDirStat) | 2 | 5 | **10** | aucun copier-coller ; registre du livrable 3 § 8 | continu |
| R14 | **Lenteur d'affichage d'un très gros dossier** (ex. `System32`, ~ 5 000 fichiers) | 3 | 2 | **6** | pagination de 200 + index `ix_file_folder` ; mesure à 3B | 3B |
| R15 | **`COUNT(*)` trop lent** quand il accompagne chaque page de résultats | 3 | 2 | **6** | comptage exécuté à part, plafonné (« plus de 5 000 »), jamais bloquant | 3B |
| R16 | **Chemins longs** (> 260 caractères) refusés par l'API standard | 3 | 2 | **6** | signalés en `scan_issue`, jamais silencieux ; contingence Win32 si massif | 3A |
| R17 | **Écart entre « espace occupé » et Windows** (pas de taille allouée) | 4 | 2 | **8** | note explicite ; vocabulaire « somme des tailles » | 3A |
| R18 | **Filtres appliqués en mémoire** par mégarde → pagination fausse | 2 | 4 | **8** | tout filtre est une clause SQL ; revue de code + test de pagination filtrée | 3B |
| R19 | **Détection d'outils peu fiable** (hors `PATH`, `--version` bloqué) | 3 | 2 | **6** | délai de 3 s, statut explicite par outil | 4 |
| R20 | **Corruption de la base** après arrêt brutal ou disque plein | 2 | 3 | **6** | WAL, contrôle d'intégrité à l'ouverture, message de renommage | 3A |
| R21 | **Suppression involontaire d'un inventaire** | 3 | 3 | **9** | confirmation indiquant ce qui sera perdu | 5 |
| R22 | **Écriture accidentelle** sur les fichiers ou le registre | 2 | 5 | **10** | aucune API d'écriture ; test par réflexion ; audit Process Monitor | 5 |
| R23 | **Plus de 1,5 million de fichiers** sur les disques ciblés | 2 | 3 | **6** | mesurer à 3A ; réduire les plafonds d'affichage, jamais le schéma | 3A |
| R24 | **`json_each` ou `trigram` indisponible** dans le SQLite embarqué | 2 | 2 | **4** | repli `IN (?,…)` plafonné à 30 extensions ; FTS5 seulement si mesuré nécessaire | 3A, 3B |

### Les quatre risques à surveiller

1. **R1 / R2 — dire la vérité sur la couverture.** Ce sont les seuls risques qui font
   énoncer à l'outil quelque chose de **faux**. Ils justifient des tests dédiés et une
   mention à côté de **chaque** total.
2. **R3 — la vitesse de la recherche.** C'est la promesse centrale : trouver un fichier
   « rapidement ». Un dépassement de 1,5 s déclenche la contingence FTS5, sans débat.
3. **R6 / R7 — la durée du scan.** Si un inventaire prend 25 minutes, l'utilisateur n'en
   fera qu'un, et la recherche ne servira à rien sur des données périmées.
4. **R12 / R13 — la gouvernance.** Le périmètre et la licence sont plus difficiles à
   tenir que le code.

---

## 2. Limites assumées du produit

### Sans administrateur

- dossiers refusés (ACL) : `System Volume Information`, `$Recycle.Bin`, certains
  profils, une partie de `AppData` ;
- aucune information SMART, BitLocker, TPM détaillé ;
- aucun scan MFT (donc pas de scan « instantané »).

**Conséquence affichée** : les totaux peuvent être inférieurs à ceux de Windows, et le
compteur de zones non analysées est toujours visible.

### Sur la recherche

- **la recherche porte sur le nom**, jamais sur le contenu : un document contenant le mot
  cherché mais dont le nom ne le contient pas ne sera pas trouvé ;
- une saisie de **1 ou 2 caractères** ne peut pas être servie par un index : elle est
  acceptée mais annoncée comme large ;
- `COLLATE NOCASE` ne traite que l'ASCII : une différence de casse sur un nom accentué
  n'est pas détectée (sans conséquence pratique sous Windows) ;
- les résultats sont **plafonnés** en affichage (200 par page, quelques milliers de
  résultats parcourables) ; le compteur indique le total réel ;
- aucun tri par pertinence : le tri est celui des colonnes, toujours complété par le nom.

### Sur les fichiers

- **pas de taille allouée** : « espace occupé » = somme des tailles de fichiers ;
- pas d'attributs : impossible de filtrer « fichiers cachés » ou « système » ;
- liens physiques comptés une fois par nom, comme tous les outils de ce type ;
- points de reparse listés mais **non suivis** ;
- fichiers en cours d'écriture : taille au moment de l'énumération ;
- chemins de plus de 260 caractères : peuvent échouer et sont alors signalés ;
- volumes réseau : non prévus en V1 ;
- **ouverture d'un élément** : lance l'Explorateur ou le programme associé. ATLAS PC ne
  modifie rien, mais l'application lancée le peut, et Windows peut enregistrer l'élément
  dans ses « fichiers récents ». C'est un effet du système, hors du contrôle d'ATLAS PC.

### Sur les logiciels, les outils, les projets

- seulement les logiciels déclarés au registre : aucun portable, aucune installation
  « à la main » sans clé `Uninstall` ;
- `SystemComponent` et mises à jour filtrés volontairement ;
- un outil absent du `PATH` et des dossiers connus n'est pas trouvé ;
- seuls `package.json`, `requirements.txt`, `pyproject.toml` sont reconnus : un projet
  sans marqueur est invisible, et un dépôt Git sans marqueur n'apparaît pas ;
- aucune analyse de dépendances, aucun contenu de `.git` lu.

### Sur l'historique

- un inventaire est une **photographie** : ce qui est créé puis supprimé entre deux
  inventaires est invisible ;
- **un déplacement ou un renommage n'est pas reconnu** : il apparaît comme un fichier
  retiré de son ancien emplacement et un fichier nouveau au nouvel emplacement. Décision
  de V1 (livrable 5 § 6.4), écrite dans l'interface : la reconnaissance fiable des
  déplacements est un problème difficile, souvent faux, et non demandé ;
- la comparaison exige des **disques identiques** ; sinon elle est refusée disque par
  disque, avec explication ;
- aucun export : l'historique ne vit que dans la base, sur la machine.

---

## 3. Zones non analysées par cette étude

| Élément | Raison |
|---|---|
| Exécution des quatre logiciels étudiés | environnement Linux, aucune machine Windows |
| Mesures de performance réelles | idem ; tous les chiffres de durée sont des estimations |
| Fonctions de recherche des quatre dépôts | **aucune recherche par nom partiel n'a été identifiée** dans le code examiné ; l'absence n'est pas prouvée pour leurs interfaces complètes |
| Audit ligne à ligne de WinDirStat | volume trop important (≈ 175 fichiers) |
| Décodage MFT de WinDirStat | hors périmètre (MFT exclu) |
| Sources distantes de WinDirStat (S3, Azure, WSL, MTP) | hors périmètre |
| Tests automatisés des quatre dépôts | non exécutés |
| Interface Android de Diskplot | hors périmètre |
| Rendu visuel réel des quatre interfaces | analyse du code et des captures des dépôts |
| Normalisation de chaque champ de System Inspector | non auditée ligne à ligne |
| Portée juridique des licences | analyse documentaire, pas un avis juridique |

---

## 4. Points à vérifier avant de figer l'architecture

| # | Point | Méthode | Si défavorable |
|---|---|---|---|
| 1 | **Nombre réel de fichiers et dossiers** des disques ciblés | `dir /s /a`, ou premier scan réel | > 1,5 M : revoir les plafonds et l'estimation de taille de base |
| 2 | **Durée des trois recherches** (exact, préfixe, sous-chaîne) sur 100 k / 400 k / 1 M de fichiers | protocole du livrable 8 § 5 | > 1,5 s : ajouter `file_name_fts` (FTS5 trigram) |
| 3 | **Version de SQLite embarquée** : `json_each`, `trigram` | `SELECT sqlite_version()` + essais | repli `IN (?,…)` ; FTS5 impossible → tri par pertinence abandonné, pas de recherche plein texte |
| 4 | **Durée du parcours standard .NET** | mesure à 3A | > 20 min : énumérateur Win32 dans une classe dédiée |
| 5 | **Durée de `COUNT(*)`** avec recherche et filtres | mesure à 3B | plafonner le compteur (« plus de 5 000 ») |
| 6 | **Affichage d'un dossier de 5 000 fichiers** | mesure à 3B (`C:\Windows\System32`) | pagination plus petite (100) et index vérifié |
| 6b | **Aucune lecture disque pendant une recherche** | test automatisé : dossier analysé supprimé après le scan, recherche relancée | si des résultats disparaissent, une lecture disque parasite existe : la localiser et la retirer |
| 7 | **Compatibilité `System.Data.SQLite.Core` avec `net48`** | test de fumée : création, 100 k insertions, agrégation | choisir `Microsoft.Data.Sqlite` |
| 8 | **Démarrage à froid et mémoire** de la coquille WPF | 5 essais après redémarrage, étape 1 | > 5 s / > 200 Mo : alléger le thème, sinon WinForms |
| 9 | **Chemins longs** : combien échouent | compteur `scan_issue` | échecs massifs : activer les chemins longs puis, si insuffisant, Win32 |
| 10 | **Comportement de l'ouverture** sur cette machine (programme associé, éléments récents) | essai manuel à 3B | le documenter dans la note de bas de tableau |
| 11 | **Taille réelle d'une base d'inventaire** | mesure à 3A | > 150 Mo : envisager `folder_id` dans `file` (petite migration) |
| 12 | **Accès WMI** en utilisateur standard | exécution étape 2 | champs refusés : documenter, ne pas élargir les privilèges |

---

## 5. Questions ouvertes pour le commanditaire

1. **Quels disques** doivent être inventoriés, et faut-il pouvoir en ajouter un plus tard
   (clé USB, disque externe) ? Le modèle le permet ; l'interface doit le dire.
2. **Combien d'inventaires** souhaitez-vous conserver ? Cela fixe l'avertissement de
   taille, pas le mécanisme (manuel).
3. **La recherche doit-elle pouvoir porter sur un dossier précis** plutôt que sur tout un
   disque par défaut ? Le filtre Emplacement existe ; sa valeur initiale est un choix
   d'usage.
4. **ATLAS PC sera-t-il distribué** ou strictement personnel ? Cela fixe le niveau de
   documentation utilisateur.
5. **La taille allouée est-elle nécessaire** pour vos arbitrages, ou la somme des tailles
   suffit-elle ? Elle est hors V1.

---

## 6. Risques de projet

| Risque | Parade |
|---|---|
| L'étape 3B dérape | les cinq journées sont découpées et livrables ; à la fin de J2, la navigation fonctionne déjà |
| Rubrique Fichiers livrée « en tableau d'inventaire » | la maquette sert de référence, et les onze fonctions du cahier des charges servent de liste de contrôle |
| Mesures faites sur une autre machine | toute mesure se fait sur l'OptiPlex 780 |
| Documentation qui diverge du code | ce dossier est mis à jour à la fin de chaque étape |
| Ajout discret de fonctionnalités | toute addition passe par la liste des refus du livrable 4 § 11 |

---

## 7. Conclusion

Le projet est **faisable**, avec quatre réserves à accepter explicitement :

1. **l'inventaire ne sera jamais complet** sans administrateur, et l'outil doit le dire —
   y compris en refusant de conclure à une suppression ;
2. **un inventaire complet prendra plusieurs minutes** sur ces disques ;
3. **la recherche porte sur les noms, pas sur les contenus** : c'est une limite
   volontaire, pas un défaut caché ;
4. **la plateforme est en fin de vie** (Windows 10 22H2 hors support standard,
   .NET Framework lié au système), ce qui plaide pour un logiciel simple, sans dépendance
   externe.

Ces réserves ne sont pas des défauts de conception : elles sont la raison des choix du
livrable 4 — un seul projet, un seul thread, une base de dix tables toutes justifiées, une
recherche servie par un index de noms, et aucune fonctionnalité qui ne serve pas
directement à retrouver un fichier.
