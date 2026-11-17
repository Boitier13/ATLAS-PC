# Livrable 9 — Risques, limites et points à vérifier

> **Révision du 17 novembre 2026.** Les risques liés aux éléments supprimés (exports,
> treemap, multi-ciblage, rétention automatique, ports et interfaces) ont été retirés.
> Trois risques nouveaux apparaissent, propres à la simplification : l'API standard de
> parcours, la taille de la base et la suppression manuelle.

---

## 1. Registre des risques

| # | Risque | Prob. | Impact | Criticité | Parade | Vérifié à |
|---|---|---|---|---|---|---|
| R1 | **Windows 10 22H2 hors support** (14/10/2025 ; ESU jusqu'en 2028) et .NET Framework lié au cycle de l'OS | 5 | 3 | **15** | cibler `net48`, ne dépendre d'aucun runtime externe ; documenter | étape 1 |
| R2 | **Zones inaccessibles sans administrateur** : l'inventaire est structurellement incomplet | 5 | 3 | **15** | compteur permanent, liste des zones, badge `Partiel`, **garde sur les suppressions** | étape 3 |
| R3 | **Fausse annonce de suppression** dans une zone non analysée | 3 | 5 | **15** | requête de comparaison filtrée par `scan_issue` ; test dédié ; catégorie « non vérifiable » | étape 5 |
| R4 | **Parcours standard trop lent** sur disque mécanique | 3 | 4 | **12** | mesure à l'étape 3 ; contingence `FindFirstFileExW` dans une seule classe | étape 3 |
| R5 | **Durée d'inventaire longue** (5-15 min estimés pour `C:`) | 4 | 3 | **12** | progression crédible, annulation rapide, inventaire partiel conservable | étape 3 |
| R6 | **Base volumineuse** (60-80 Mo par inventaire, chemin répété) et **utilisateur qui ne supprime pas** | 3 | 3 | **9** | taille affichée, avertissement au-delà de 10 inventaires, suppression en un clic | étapes 3 et 5 |
| R7 | **Démarrage à froid trop lent** (WPF, antivirus) | 3 | 3 | **9** | `net48` sans auto-extraction ; mesure et point de décision à l'étape 1 | étape 1 |
| R8 | **WMI lent ou indisponible** | 3 | 3 | **9** | délai de 10 s par requête, `try/catch` par requête, valeurs `—` | étape 2 |
| R9 | **Chemins longs** (> 260 caractères) refusés par l'API standard | 3 | 2 | **6** | signalés en `scan_issue`, jamais silencieux ; contingence Win32 si massif | étape 3 |
| R10 | **Écart entre « espace occupé » et Windows** (pas de taille allouée) | 4 | 2 | **8** | note explicite dans la vue Fichiers ; vocabulaire « somme des tailles » | étape 3 |
| R11 | **Détection d'outils peu fiable** (hors `PATH`, `--version` bloqué) | 3 | 2 | **6** | délai de 3 s, statut explicite par outil, jamais bloquant | étape 4 |
| R12 | **Régression de périmètre** (quelqu'un redemande exports, treemap, portables) | 4 | 3 | **12** | liste des refus (livrable 4, § 10) ; toute demande passe par une décision écrite | continu |
| R13 | **Contamination de licence** (copie de code GPL de WinDirStat) | 2 | 5 | **10** | aucun copier-coller ; registre d'inspiration (livrable 3) | continu |
| R14 | **Requêtes du tableau de bord trop lentes** sans statistiques pré-calculées | 3 | 2 | **6** | index ; mesure à l'étape 3 ; parade = filtrer/plafonner, **pas** réintroduire des tables d'agrégats | étape 3 |
| R15 | **Corruption de la base** après arrêt brutal ou disque plein | 2 | 3 | **6** | WAL, `synchronous = NORMAL`, contrôle d'intégrité à l'ouverture, message de renommage | étape 3 |
| R16 | **Suppression involontaire d'un inventaire** | 3 | 3 | **9** | confirmation explicite indiquant ce qui sera perdu | étape 5 |
| R17 | **Écriture accidentelle** sur les fichiers ou le registre | 2 | 5 | **10** | aucune API d'écriture, test par réflexion, audit Process Monitor | étape 5 |
| R18 | **Plus de 1,5 million de fichiers** sur le volume cible | 2 | 3 | **6** | mesurer à l'étape 3 ; réduire les plafonds d'affichage, jamais le schéma | étape 3 |

### Les quatre risques à surveiller

1. **R3 — fausse annonce de suppression.** C'est le seul risque qui peut faire dire à
   l'outil quelque chose de **faux**. Il justifie à lui seul un test dédié.
2. **R2 / R1 — deux horizons hors support.** Ce sont des faits, pas des défauts : ils
   doivent être écrits dans l'application, pas contournés.
3. **R4 / R5 — la vitesse.** Si un inventaire prend 25 minutes, l'utilisateur n'en fera
   qu'un. La mesure est obligatoire à l'étape 3.
4. **R12 / R13 — la gouvernance.** Le périmètre et la licence sont plus difficiles à
   tenir que le code.

---

## 2. Limites assumées du produit

### Sans administrateur

- dossiers refusés (ACL) : `System Volume Information`, `$Recycle.Bin`, certains
  profils, une partie de `AppData` ;
- aucune information SMART, BitLocker, TPM détaillé ;
- aucun scan MFT (donc pas de scan « instantané »).

**Conséquence affichée** : les totaux peuvent être inférieurs à ce que Windows
rapporte, et le compteur de zones non analysées est toujours visible.

### Sur les fichiers

- **pas de taille allouée** : « espace occupé » = somme des tailles de fichiers. Sur un
  volume contenant des fichiers compressés ou sparse, l'écart avec Windows est réel ;
- **pas d'attributs** : impossible de distinguer visuellement les fichiers cachés ou
  système ;
- liens physiques comptés une fois par nom (comme tous les outils de ce type) ;
- points de reparse listés mais **non suivis** ;
- fichiers en cours d'écriture : taille au moment de l'énumération ;
- chemins de plus de 260 caractères : peuvent échouer et sont alors signalés ;
- volumes réseau : non prévus en V1.

### Sur les logiciels

- **seuls les logiciels déclarés au registre** ; aucune application portable, aucun
  logiciel installé « à la main » sans clé `Uninstall` ;
- les entrées `SystemComponent` et les mises à jour sont volontairement filtrées ;
- les dates d'installation issues de MSI sont parfois absentes ou approximatives.

### Sur les outils

- un outil installé mais absent du `PATH` et hors des dossiers connus n'est pas trouvé ;
- la version peut être indisponible (ni métadonnée, ni `--version` exploitable).

### Sur les projets

- seuls les marqueurs `package.json`, `requirements.txt`, `pyproject.toml` sont
  reconnus : un projet sans marqueur est invisible ;
- un dépôt Git sans marqueur Python/Node n'apparaît pas ;
- aucune analyse de dépendances, aucun contenu de `.git` n'est lu.

### Sur l'historique

- un inventaire est une **photographie** : ce qui est créé puis supprimé entre deux
  inventaires est invisible ;
- la comparaison exige **la même racine** ;
- aucun export : l'historique ne vit que dans la base, sur la machine.

---

## 3. Zones non analysées par cette étude

| Élément | Raison |
|---|---|
| Exécution des quatre logiciels étudiés | environnement Linux, aucune machine Windows |
| Mesures de performance réelles | idem ; tous les chiffres de durée sont des estimations |
| Audit ligne à ligne de WinDirStat | volume trop important (≈ 175 fichiers) ; moteur, file, modèle et export seuls |
| Décodage MFT de WinDirStat | hors périmètre (MFT exclu d'ATLAS PC) |
| Sources distantes de WinDirStat (S3, Azure, WSL, MTP) | hors périmètre |
| Tests automatisés des quatre dépôts | non exécutés |
| Interface Android de Diskplot | hors périmètre |
| Rendu visuel réel des quatre interfaces | analyse du code et des captures des dépôts |
| Normalisation de chaque champ de System Inspector | non auditée ligne à ligne |
| Portée juridique des licences | analyse documentaire, pas un avis juridique |

---

## 4. Points à vérifier avant de figer l'architecture

Par ordre d'impact décroissant. Chaque point indique **comment** vérifier et **ce qui
change** selon le résultat.

| # | Point | Méthode | Si défavorable |
|---|---|---|---|
| 1 | **Nombre réel de fichiers et dossiers** du disque cible | `dir /s /a` sur `C:`, ou premier scan réel | > 1,5 M : revoir les plafonds d'affichage et l'estimation de taille de base |
| 2 | **Durée du parcours standard .NET** sur ce disque | mesure à l'étape 3 | > 20 min : ajouter l'énumérateur Win32 dans une classe dédiée (décision tracée) |
| 3 | **Compatibilité `System.Data.SQLite.Core` avec `net48`** | test de fumée : création, 100 k insertions, agrégation | choisir `Microsoft.Data.Sqlite` |
| 4 | **Démarrage à froid et mémoire** de la coquille WPF | 5 essais après redémarrage, étape 1 | > 5 s / > 200 Mo : alléger le thème, sinon WinForms |
| 5 | **Durée des requêtes du tableau de bord** sur 400 000 lignes | mesure à l'étape 3 | > 1 s (seuil du livrable 8) : filtrer et plafonner, **pas** d'agrégats pré-calculés |
| 6 | **Chemins longs** : combien échouent sur ce disque | compteur `scan_issue` après un scan | échecs massifs : activer les chemins longs dans `app.config` puis, si insuffisant, Win32 |
| 7 | **Rendu WPF sur le GPU intégré** à 1440 × 900 | test visuel étape 1 | rendu logiciel : supprimer toute transparence, sinon WinForms |
| 8 | **Taille réelle d'une base d'inventaire** | mesure à l'étape 3 | > 100 Mo : envisager `folder_id` dans `file` (petite migration de schéma) |
| 9 | **Sémantique de la date de modification** (résolution, heure d'été) | test de référence étape 5 | écart : tolérance documentée dans la comparaison |
| 10 | **Accès WMI** en utilisateur standard sur cette machine | exécution étape 2 | champs refusés : documenter, ne pas élargir les privilèges |

---

## 5. Questions ouvertes pour le commanditaire

1. **Le volume à analyser est-il uniquement `C:`** ou faut-il choisir un dossier
   arbitraire ? Le modèle le permet ; l'interface doit le dire clairement.
2. **Combien d'inventaires** souhaitez-vous conserver en pratique ? Cela détermine
   l'avertissement de taille, pas le mécanisme (qui est manuel).
3. **L'administrateur est-il totalement interdit** ou seulement non requis ? Un mode
   élevé optionnel changerait la couverture, mais ajoute une fonctionnalité : hors V1
   par défaut.
4. **ATLAS PC sera-t-il distribué** ou strictement personnel ? Cela fixe la licence
   (MIT dans les deux cas proposés) et le niveau de documentation utilisateur.
5. **La taille allouée est-elle nécessaire** pour vos arbitrages, ou la somme des
   tailles suffit-elle ? Elle est hors V1 ; la réponse conditionne un ajout ultérieur.

---

## 6. Risques de projet

| Risque | Parade |
|---|---|
| L'étape 3 dérape (c'est la plus lourde) | livrer d'abord une version limitée à un dossier, utilisable de bout en bout |
| Mesures faites sur une autre machine que la cible | toute mesure de durée et de mémoire se fait sur l'OptiPlex 780 |
| Documentation qui diverge du code | ce dossier est mis à jour à la fin de chaque étape, pas à la fin du projet |
| Ennui de la recette | matrice écrite dès l'étape 1 |
| Ajout discret de fonctionnalités | toute addition passe par la liste des refus du livrable 4 |

---

## 7. Conclusion

Le projet est **faisable**, avec trois réserves à accepter explicitement :

1. **l'inventaire ne sera jamais complet** sans administrateur, et l'outil doit le
   dire — y compris en refusant de conclure à une suppression ;
2. **un inventaire complet prendra plusieurs minutes** sur ce disque ;
3. **la plateforme est en fin de vie** (Windows 10 22H2 hors support standard,
   .NET Framework lié au système), ce qui plaide pour un logiciel simple, sans
   dépendance externe.

Ces trois réserves ne sont pas des défauts de conception : elles sont la raison des
choix du livrable 4 — un seul projet, un seul thread, une seule base de dix tables, et
aucune fonctionnalité qui ne serve pas directement l'inventaire.
