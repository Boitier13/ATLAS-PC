# Livrable 9 — Risques, limites et points restant à vérifier

---

## 1. Registre des risques

Échelle d'impact : 1 = gêne mineure, 5 = remise en cause du projet.
Échelle de probabilité : 1 = improbable, 5 = quasi certain.

| # | Risque | Prob. | Impact | Criticité | Parade prévue | Vérifié à |
|---|---|---|---|---|---|---|
| R1 | **Windows 10 22H2 hors support** (depuis le 14/10/2025 ; ESU jusqu'en 2028) et runtime .NET 8/9 hors support depuis le 10/11/2026 | 5 | 3 | **15** | cibler .NET Framework 4.8 (composant du système) ; documenter la situation ; ne pas embarquer de runtime obsolète | M0 |
| R2 | **Démarrage à froid trop lent** sur disque mécanique (WPF, chargement des assemblies, antivirus) | 3 | 4 | **12** | .NET Framework (assemblies du système déjà en cache) ; pas d'auto-extraction ; mesure en M0 avec point d'arrêt | M0 |
| R3 | **Durée de scan inacceptable** (5-15 min estimés pour un volume entier) | 4 | 3 | **12** | un seul travailleur, énumération groupée, écritures par lots ; affichage d'une progression crédible ; possibilité d'inventaire partiel | M3 |
| R4 | **Dépassement mémoire** si l'on dérive vers un modèle en arbre | 2 | 5 | **10** | interdiction de l'arbre de fichiers en mémoire ; fichier de décision ; test de mémoire automatisé à chaque build | M3 |
| R5 | **Zones inaccessibles sans administrateur** : l'inventaire est structurellement incomplet | 5 | 3 | **15** | comptage et affichage permanent des dossiers illisibles ; section « zones non analysées » ; aucune prétention de complétude | M3, M8 |
| R6 | **Faux positifs sur les applications portables** | 4 | 2 | **8** | heuristique conservatrice, marquage `confidence = low`, aucune recherche exhaustive de `.exe` | M5 |
| R7 | **Détection des outils peu fiable** (hors PATH, `--version` lent ou bloqué) | 3 | 3 | **9** | ordre de préférence des méthodes, délai de 3 s, statut explicite par outil, jamais bloquant | M5 |
| R8 | **WMI lent ou indisponible** sur un PC ancien (service dégradé, requête qui pend) | 3 | 4 | **12** | délai par requête, capture typée, repli documenté, aucune requête `Win32_Product` | M1 |
| R9 | **Croissance de la base** au fil des inventaires | 3 | 3 | **9** | rétention 3 détaillés + 9 allégés, purge au-delà de 12, affichage de la taille occupée | M2 |
| R10 | **Corruption de la base** après arrêt brutal ou disque plein | 2 | 4 | **8** | WAL + `synchronous = NORMAL` ; détection d'intégrité à l'ouverture ; message et proposition de renommage du fichier | M2 |
| R11 | **Antivirus** ralentissant fortement l'énumération ou bloquant l'EXE non signé | 4 | 2 | **8** | attente réaliste dans les mesures ; documentation ; pas d'auto-extraction ; signature hors périmètre V1 | M9 |
| R12 | **Encodage et chemins exotiques** (accents, caractères non BMP, chemins > 260) dans les CSV/HTML | 3 | 2 | **6** | UTF-8 avec BOM pour les CSV, échappement HTML systématique, préfixe `\\?\` à l'énumération | M3, M8 |
| R13 | **Rendu WPF dégradé** sur GPU intégré ancien (rendu logiciel) | 2 | 2 | **4** | aucun effet graphique, aucun dégradé animé, aucun `Effect` ; listes virtualisées | M4 |
| R14 | **Contamination de licence** (copie involontaire de code GPL de WinDirStat) | 2 | 5 | **10** | interdiction de copie, registre d'inspiration, relecture, licence figée avant le premier commit | continu |
| R15 | **Dérive du périmètre** (treemap, PDF, réseau, nettoyage, temps réel) | 4 | 3 | **12** | liste explicite des refus (livrable 4, § 11) ; toute addition passe par une justification écrite | continu |
| R16 | **Promesse de complétude** implicite dans l'interface ou les rapports | 3 | 4 | **12** | badge d'état permanent, section « limites » obligatoire, vocabulaire contrôlé (« détecté », « non analysé ») | M4, M8 |
| R17 | **Données personnelles dans les chemins** (nom d'utilisateur, arborescence personnelle) | 4 | 3 | **12** | base strictement locale, aucune télémétrie, option de masquage à l'export, doc de confidentialité | M8 |
| R18 | **Provider SQLite incompatible** avec `net48` | 2 | 3 | **6** | test de fumée en M0 ; deux providers candidats ; couche d'accès isolée derrière `IInventoryRepository` | M0 |
| R19 | **Mémoire insuffisante** parce que d'autres applications tournent (2 Go libres seulement) | 3 | 3 | **9** | budget < 250 Mo, pas de cache d'images, mode discret, message si mémoire faible | M9 |
| R20 | **Comparaison fausse** sur les dates (heure d'été, fuseau, résolution de 2 s du système de fichiers) | 2 | 4 | **8** | stockage en UTC ISO-8601 ; comparaison sur (taille, mtime) ; tolérance documentée ; tests de référence | M7 |

### Les cinq risques à surveiller en priorité

1. **R1 / R5** — deux horizons hors support (OS et runtime) et une incomplétude
   structurelle : ce sont des **faits**, pas des défauts. Ils doivent être écrits
   dans l'interface et dans les rapports.
2. **R2** — le démarrage à froid est le premier contact avec l'outil. Un point
   d'arrêt en M0 est prévu, avec une alternative chiffrée.
3. **R3** — la durée du scan conditionne l'usage réel : si un inventaire prend 20
   minutes, l'utilisateur n'en fera qu'un.
4. **R4** — le retour à un modèle en arbre est la dérive la plus probable, parce
   qu'elle est plus simple à écrire. Le test de mémoire automatisé est la parade.
5. **R14 / R15** — les deux risques de gouvernance : licence et périmètre.

---

## 2. Limites assumées du produit

Ce qu'ATLAS PC **ne verra pas** ou **ne saura pas**, et qui devra être dit tel quel :

### 2.1 Limites liées aux privilèges (sans administrateur)

- dossiers ACL-refusés : `System Volume Information`, `$Recycle.Bin`, certains
  dossiers d'autres profils utilisateurs, `Windows\CSC`, certaines zones d'`AppData` ;
- compteurs SMART / fiabilité des disques (`MSFT_StorageReliabilityCounter`), souvent
  réservés à l'administrateur ;
- état BitLocker, TPM détaillé, Secure Boot dans certains cas ;
- lecture du MFT (donc pas de scan « instantané » d'un volume NTFS).

**Conséquence** : les totaux d'espace peuvent être **inférieurs** à l'espace réellement
occupé rapporté par Windows. C'est normal et doit être affiché comme tel
(« 312 Go analysés sur 340 Go occupés — 12 dossiers inaccessibles »).

### 2.2 Limites liées à Windows et aux fournisseurs

- VRAM réelle des GPU intégrés (WMI est peu fiable) → marquée `approx.` ;
- topologie des canaux mémoire, rang des barrettes, fréquence réelle en charge ;
- fréquence turbo du CPU, température, tension ;
- type de média des disques (`SSD`/`HDD` souvent `Unknown` en WMI) ;
- type de connexion et dimensions physiques des écrans, HDR, EDID complet ;
- version exacte d'un outil qui n'expose ni métadonnées de fichier ni `--version`
  exploitable.

### 2.3 Limites liées aux fichiers

- taille allouée estimée pour les fichiers compressés/sparse si
  `GetCompressedFileSize` échoue ; les liens physiques (hardlinks) sont comptés une
  fois par nom, comme chez Diskplot et NeoTreescan — donc `WinSxS` paraît plus gros
  qu'il ne l'est ;
- fichiers en cours d'écriture : taille au moment de l'énumération, instantanée ;
- points de reparse (jonctions, liens symboliques) : listés, **non suivis** — donc non
  comptés deux fois, mais aussi non explorés ;
- volumes réseau : lisibles si l'utilisateur en a le droit, mais lents ; hors périmètre
  V1 recommandé.

### 2.4 Limites méthodologiques

- **la détection n'est pas une preuve** : un exécutable trouvé dans un dossier n'est
  pas nécessairement « une application portable » ;
- **la comparaison dépend de la conservation** : au-delà de 3 inventaires détaillés,
  la comparaison porte sur les dossiers, pas sur les fichiers ;
- **un inventaire est une photographie**, pas un historique continu : ce qui est créé
  puis supprimé entre deux inventaires est invisible ;
- **aucune détection de versions obsolètes, aucun diagnostic** : c'est un choix du
  cahier des charges, pas un oubli.

---

## 3. Zones non analysées par la présente étude

Pour respecter la règle « ne pas prétendre avoir tout examiné », voici ce qui n'a
**pas** été fait :

| Élément | Raison |
|---|---|
| Exécution des quatre logiciels | environnement Linux, pas de machine Windows disponible |
| Mesures de performance réelles | idem |
| Audit ligne à ligne de WinDirStat | volume trop important (≈ 175 fichiers) ; seuls le moteur, la file, le modèle, l'export et l'élévation ont été lus |
| Décodage MFT complet (`FinderNtfs.cpp`) | seules les structures et le chemin d'ouverture ont été vérifiés |
| Modules de récupération de fichiers de WinDirStat | hors périmètre |
| Sources distantes (S3, Azure, WebDAV, WSL, MTP) | hors périmètre |
| Traductions, installeurs, scripts de release | hors périmètre |
| Tests automatisés de WinDirStat | scripts non exécutés |
| `dupworker.ts` de Diskplot au-delà de l'algorithme de hachage | hors périmètre |
| Interface Android de Diskplot | hors périmètre |
| Rendu visuel réel des quatre interfaces | analyse du code et des captures du dépôt seulement |
| Code source de System Inspector intégralement | 2 470 lignes : les couches de collecte, de rapport et d'interface ont été lues, la normalisation de chaque champ n'a pas été auditée |
| Valeur juridique des licences | analyse documentaire, pas un avis juridique |

---

## 4. Points à vérifier avant de figer l'architecture

Classés par ordre d'impact décroissant. Chacun indique **comment** le vérifier et
**ce qui change** selon le résultat.

| # | Point à vérifier | Méthode | Si le résultat est défavorable |
|---|---|---|---|
| 1 | **Nombre réel de fichiers et de dossiers** du disque cible | `dir /s /a` sur C: (une fois), ou premier scan de M3 | > 1,5 M fichiers : durcir la rétention, envisager de ne stocker que les fichiers au-delà d'un seuil, revoir le budget de durée |
| 2 | **Démarrage à froid et mémoire** de la coquille WPF sur `net48` | mesure en M0, 5 essais après redémarrage | > 3 s ou > 130 Mo : examiner la variante `.NET 10` auto-contenue, ou une coquille WinForms |
| 3 | **Compatibilité du provider SQLite** avec `net48` | test de fumée M0 (création, 100 k insertions, requête) | choisir `System.Data.SQLite` (domaine public) au lieu de `Microsoft.Data.Sqlite` |
| 4 | **Durée d'un scan complet** sans administrateur | mesure en M3 sur le volume C: | > 20 min : proposer des portées plus fines par défaut (profil utilisateur, dossiers de projets) et documenter |
| 5 | **Comportement du GPU intégré** avec WPF à 1440 × 900 | test visuel M0/M4, `RenderCapability.Tier` | rendu logiciel : supprimer toute transparence et tout effet, envisager WinForms |
| 6 | **Accès réels aux classes WMI** en utilisateur standard sur cette machine | exécution de M1, relevé des `collect_status` | champs refusés : documenter, ou proposer un mode élevé optionnel pour la seule collecte matérielle |
| 7 | **Sémantique de `mtime`** sur le volume cible (résolution, heure d'été) | test de référence M7 | écart de 2 s : tolérance de comparaison, documentée |
| 8 | **Taille d'un inventaire détaillé** réel | mesure M2 | > 70 Mo : réduire les colonnes stockées par fichier, ou passer les fichiers sous un seuil en agrégat |
| 9 | **Précision de la détection des portables** | confrontation manuelle sur la machine cible | trop de faux positifs : désactiver la détection portable par défaut |
| 10 | **`Get-Volume` / `Get-Partition`** (si l'on s'en inspire) en utilisateur standard | test M1 | repli `Win32_LogicalDisk` seul, ce qui suffit |
| 11 | **Droits de lecture du registre** `HKLM\...\Uninstall\WOW6432Node` | test M5 | repli sur `HKLM` 64 bits uniquement, avec mention |
| 12 | **Effet de l'antivirus** sur la durée | mesure M9 avec et sans exclusion | documenter, ne pas demander d'exclusion |

---

## 5. Questions ouvertes pour le commanditaire

Ces questions ne bloquent pas l'étude mais conditionnent des choix de mise en œuvre :

1. **ATLAS PC sera-t-il distribué** (amis, association, GitHub) ou strictement
   personnel ? Cela détermine la licence (MIT/Apache vs usage privé) et le niveau
   d'exigence sur les mentions légales.
2. **L'administrateur est-il totalement interdit, ou seulement non requis ?** Un mode
   élevé **optionnel** changerait la couverture (zones protégées, MFT) sans violer
   l'esprit de la règle.
3. **Le disque cible fait-il 160, 250 ou 500 Go, et quel est son taux d'occupation ?**
   Cela dimensionne la durée du scan et la base.
4. **Faut-il inventorier d'autres volumes** (D:, clé USB, partage réseau) ou seulement
   `C:` ? Le modèle le permet, l'interface doit le dire.
5. **Quel niveau de détail sur les fichiers** est réellement attendu : tous les
   fichiers, ou les dossiers plus les fichiers significatifs ? C'est l'arbitrage
   central entre durée, taille de base et valeur de la comparaison.
6. **Une archive annuelle** des inventaires est-elle nécessaire (export d'un
   inventaire complet en JSON, hors base) ?

---

## 6. Risques de projet

| Risque | Parade |
|---|---|
| Sous-estimation du moteur de scan (M3 est le jalon le plus incertain) | prévoir la marge ; livrer d'abord un scan de portée réduite, fonctionnel de bout en bout |
| Tentation de tout construire avant de mesurer | seuils et points d'arrêt posés en M0 et vérifiés à chaque jalon |
| Ennui de la phase de recette | matrice écrite à l'avance (livrable 8, § 2, M9) |
| Perte d'intérêt après la V1 | la V1 doit être utilisable et utile dès M4 (scan + consultation), pas seulement à M9 |
| Développement sur une autre machine que la cible | toute mesure de performance doit être faite **sur l'OptiPlex 780**, jamais sur une machine de développement |

---

## 7. Conclusion

Le projet est **faisable**, avec trois réserves qui doivent être acceptées
explicitement :

1. **l'inventaire ne sera jamais complet** sans administrateur — l'outil doit le dire,
   pas le masquer ;
2. **un inventaire de volume entier prendra plusieurs minutes** sur ce disque — c'est
   le prix de la sobriété ;
3. **la plateforme est en fin de vie** (Windows 10 22H2 hors support standard, .NET
   Framework lié au système) — ATLAS PC doit donc rester simple et portable, sans
   dépendance à un runtime qui disparaîtra.

Ces trois réserves sont la raison d'être des choix du livrable 4 : elles ne sont pas
des défauts de conception, elles en sont le fondement.
