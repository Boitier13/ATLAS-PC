# Livrable 1.3 — Rapport d'analyse : LouisMahdi System Inspector

> Dépôt : `https://github.com/TheLouisMahdi/louis-mahdi-system-inspector`
> Commit examiné : `420b20a6adc9c004e84e65e1e3b439ebe7b752c4` (2026-07-30)
> Version : 2.2.0 · Licence : **MIT** · Analyse **statique** (script non exécuté)

---

## 1. Identification

| Élément | Valeur vérifiée | Source |
|---|---|---|
| Nature | Inventaire matériel + Windows, rapports TXT/JSON/HTML/ZIP, lecture seule | `README.md` |
| Langage | **PowerShell 5.1** (script unique de 2 470 lignes) | `src/LouisMahdi.SystemInspector.ps1` (`wc -l`) |
| Interface | **Windows Forms** construit en PowerShell | même fichier, `Show-SystemInspectorGui` ligne 2155 |
| Lanceur | **C# / .NET Framework**, AnyCPU, EXE unique embarquant le script en ressource | `docs/LouisMahdiHost.cs` (370 lignes) |
| Compilation | `csc.exe` intégré à Windows, via un `.bat` | `Build_LouisMahdi_System_Inspector_ONE_FILE.bat` |
| Version / schéma | 2.2.0 / schéma `2.1` | lignes 19-20 du script |
| Tests | `Run-MockTests.ps1`, `Validate-Source.ps1`, `Validate-Launcher.ps1`, matrice manuelle | `tests/` |
| Documentation | 9 documents dans `docs/` (modèle de données, limites, compatibilité, priorités) | `docs/` |
| CI | `.github/workflows/ci.yml` | `.github/` |

C'est le dépôt **le mieux documenté** des quatre : `docs/DATA_MODEL.md`,
`docs/ADMIN_AND_LIMITATIONS.md`, `docs/COMPATIBILITY.md` et `CODE_QUALITY.txt`
décrivent explicitement les compromis. Cette culture de la traçabilité est
directement pertinente pour ATLAS PC, qui doit « signaler les limites et les zones
non analysées ».

---

## 2. Architecture réelle

```
LouisMahdiHost.cs  (C#)                 ← EXE, extrait le .ps1 en temp, vérifie son SHA-256,
   │                                      lance powershell.exe -STA -File
   └── LouisMahdi.SystemInspector.ps1   ← collecte, modèle, rapports, GUI WinForms
         ├── Invoke-SafeQuery / Invoke-CimQuery     (couche d'accès résiliente)
         ├── New-InventoryField / -TextField / -NumericField / -InventorySection
         ├── Collect-*Section  ×13                  (une fonction par domaine)
         ├── Write-TextReport / -JsonReport / -HtmlReport
         └── Show-SystemInspectorGui                (WinForms)
```

Le découpage est **fonctionnel** et non **en couches** : un seul fichier, un seul
espace de noms, mais une discipline de nommage très nette (`Collect-`, `Get-`,
`New-`, `Write-`). Pour ATLAS PC, c'est un modèle de **découpage des collecteurs** et
non de structure de solution.

---

## 3. Couche de collecte : le point le plus intéressant

### 3.1 Chaque requête est isolée et instrumentée

`src/LouisMahdi.SystemInspector.ps1`, `Invoke-SafeQuery` (ligne 208) :

- mesure la durée (`Stopwatch`) ;
- exécute le bloc dans un `try/catch` ;
- renvoie **toujours** un objet uniforme :
  `{ Items, Status, ErrorMessage, ExceptionType, HResult, DurationMs, Source }` ;
- en cas d'échec, journalise dans `$script:DeveloperLog` sans interrompre le rapport.

`Invoke-CimQuery` (ligne 245) ajoute `OperationTimeoutSec = $script:QueryTimeoutSec`
(**12 s**, ligne 28) et prévoit le repli si le paramètre n'existe pas
(`catch [ParameterBindingException]` puis nouvel appel sans le paramètre).

C'est **exactement** le contrat qu'il faut à ATLAS PC : une collecte ne doit jamais
faire échouer l'inventaire entier, et un fournisseur lent doit être borné dans le
temps. Sur un PC ancien où WMI peut prendre plusieurs secondes par requête, ce
plafond n'est pas un détail.

### 3.2 Modèle de données : statut et confiance par champ

`docs/DATA_MODEL.md` décrit un champ portant :

`DisplayName, RawValue, NormalizedValue, Unit, Source, SourceProperty,
CollectionStatus, Confidence, ErrorMessage, IsSensitive, IsInferred, IsRedacted,
DisplayValue`

et **dix états de collecte** : `Available`, `NotAvailable`, `NotSupported`,
`PermissionDenied`, `QueryFailed`, `InvalidValue`, `NotApplicable`, `Disconnected`,
`VendorToolUnavailable`, `Redacted`.

C'est une réponse directe à la règle « ne pas prétendre qu'un inventaire est complet
si des éléments sont inaccessibles ». **ATLAS PC doit reprendre ce principe**
(dans une version simplifiée, cf. `docs/08-modele-sqlite.md`).

### 3.3 Les treize collecteurs

Liste vérifiée (lignes 2006-2018) : Report Summary, Computer, Windows, Processor,
Physical Memory, Graphics Adapters, Displays and Monitors, Motherboard, BIOS and
Firmware, Security, Physical Storage, Volumes and Partitions, Battery — puis Warnings,
Metadata et Footer.

Exemples d'accès, vérifiés dans le code :

| Section | Sources réelles |
|---|---|
| Computer (l. 994) | `Win32_ComputerSystem`, `Win32_ComputerSystemProduct`, `Win32_SystemEnclosure`, `Win32_BaseBoard`, `Win32_OperatingSystem`, variable `COMPUTERNAME` |
| Windows (l. 1052) | `Win32_OperatingSystem` + registre `HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion` (ProductName, EditionID, DisplayVersion, CurrentBuildNumber, UBR) |
| Volumes (l. 1723) | `Get-Volume`, `Get-Partition`, repli `Win32_LogicalDisk` (`DriveType=3`), `Win32_PageFileUsage`, `Get-BitLockerVolume` en mode Extended |
| Batterie (l. 1795) | rapport XML généré par `powercfg` |
| GPU | `Win32_VideoController`, registre matériel, `nvidia-smi` **si déjà présent** |

Deux détails de qualité à souligner :
- le repli `Win32_LogicalDisk` est prévu quand `Get-Volume` est absent, **et signalé
  par un avertissement** plutôt que silencieux (l. 1729) ;
- la section Volumes recalcule les pourcentages et **supprime** le calcul si
  `Free > Size` au lieu d'afficher un chiffre impossible.

### 3.4 Ce que ce dépôt ne fait pas

Vérifié par absence, et confirmé par la liste des sections :

- **aucun logiciel installé** (pas de lecture des clés `Uninstall`, pas de
  `Win32_Product` — heureusement, cette dernière classe est lente et déclenche des
  réparations MSI) ;
- **aucun fichier, aucun dossier, aucune taille** ;
- **aucun outil de développement** (ni Python, ni Node, ni Git, ni Tesseract) ;
- **aucun projet** ;
- **aucune base de données, aucun historique, aucune comparaison**.

Sur les sept rubriques d'ATLAS PC, il n'en couvre donc qu'une seule en profondeur :
« Ordinateur ».

---

## 4. Rapports

> Décrit ici pour l'analyse du dépôt. **ATLAS PC ne comporte plus de fonction de
> rapport** (voir `docs/00-lisez-moi.md` § 5.6).

- `Write-TextReport` : sections lisibles, états explicites, pied de page.
- `Write-JsonReport` : schéma stable, valeurs brutes **et** normalisées, source,
  statut, confiance, avertissements.
- `Write-HtmlReport` (l. 2110) : HTML autonome, CSS embarqué, aucune ressource
  externe — donc lisible hors ligne. C'est le bon modèle pour le rapport HTML
  d'ATLAS PC.
- `Export-SystemInventory` (l. 2139) : dossier horodaté
  `LouisMahdi_System_Report_<HOST>_<date>/`, puis `Compress-Archive` en ZIP.
- Un journal `Developer_Diagnostics.jsonl` optionnel.

**Limite** : le rapport HTML est une longue liste de sections ; il n'y a ni synthèse
visuelle, ni graphique, ni comparaison. Pour ATLAS PC, la mise en page devra être
pensée pour 1440 × 900 et pour la lecture d'un inventaire de plusieurs centaines de
milliers de lignes.

---

## 5. Interface

`Show-SystemInspectorGui` (l. 2155 et suivantes) construit une fenêtre **780 × 650
taille fixe** (`MinimumSize` identique), thème sombre, en WinForms, **avec des
positions et tailles absolues** (`Location=New-Object Drawing.Point(...)`,
`Size=New-Object Drawing.Size(...)`) pour chaque contrôle.

Conséquences :
- rendu prévisible sur 1440 × 900 (la fenêtre est petite, elle ne s'étire pas) ;
- aucune adaptabilité : pas de `TableLayoutPanel`, pas d'ancrage global, pas de
  redimensionnement utile ;
- coût de maintenance élevé : chaque pixel est codé en dur dans un script de 2 470
  lignes.

C'est utilisable comme **référence de palette et d'ambiance** (fond `#0D1117`,
surfaces `#161B22`, accent vert `#3FB950`), pas comme modèle de mise en page.

---

## 6. Distribution et privilèges

| Sujet | Constat | Source |
|---|---|---|
| Élévation | **jamais demandée automatiquement** ; les cas nécessitant l'admin sont documentés | `docs/ADMIN_AND_LIMITATIONS.md` |
| Empreinte système | l'EXE écrit le script et l'icône dans un **dossier temporaire** puis les exécute | `docs/LouisMahdiHost.cs`, `CreateTemporaryDirectory()` |
| Intégrité | SHA-256 attendu du script compilé dans le lanceur (`__SOURCE_SHA256__` remplacé au build) | idem |
| Runtime requis | Windows PowerShell 5.1 + .NET Framework 4.x, tous deux présents sur Windows 10 | `README.md`, `docs/COMPATIBILITY.md` |
| Signature | non signé, SmartScreen signalé | `README.md` |

Deux enseignements pour ATLAS PC :
1. **Ne pas dépendre de PowerShell** pour le moteur : le script est élégant mais
   l'exécution d'un script embarqué ajoute une couche (extraction, politique
   d'exécution, latence de démarrage de PowerShell ≈ 1-2 s) qui n'a pas sa place dans
   une application à interface riche.
2. **Le contrôle d'intégrité au lancement** est une bonne pratique de distribution,
   mais elle concerne un contexte (script extrait) qu'ATLAS PC n'aura pas.

---

## 7. Adéquation au PC cible

| Critère | Constat |
|---|---|
| Coût mémoire | PowerShell + WinForms : modéré, mais chaque requête CIM a un coût fixe élevé. |
| Latence | ~16 requêtes CIM/WMI **séquentielles** ; sur un CPU ancien, plusieurs dizaines de secondes sont plausibles (estimation, non mesurée). Le plafond de 12 s par requête protège du pire. |
| Privilèges | Conforme : fonctionne en utilisateur standard, signale les refus. |
| Rapport | Conforme à l'esprit (statuts, limites, hors ligne, HTML autonome). |
| Licence | MIT : réutilisation possible. |
| Code | PowerShell : **non transposable** dans une application C#/WPF, sauf en réécrivant les requêtes (ce qui est rapide, car une requête CIM est une ligne). |

---

## 8. Licence et implications

- `LICENSE` = **MIT**, `Copyright (c) 2026 LouisMahdi`.
- `PRIVACY.md` documente la censure par défaut des identifiants sensibles
  (nom d'utilisateur, UUID, numéros de série, MAC/IP, SSID) — sujet directement
  pertinent pour ATLAS PC, dont les chemins de fichiers contiennent le nom
  d'utilisateur.
- Réutilisation possible avec conservation de la notice. Comme pour Diskplot, la
  transposition PowerShell → C# est une réécriture : le risque de contamination est
  faible, le risque de **recopie involontaire de structure** existe et se gère par
  une règle simple (réécrire à partir des classes WMI documentées par Microsoft, pas
  à partir du script).

---

## 9. Ce qui est réutilisable comme inspiration

1. **Le contrat de collecte** : chaque source renvoie un objet uniforme avec statut,
   message, type d'exception, code, durée, source. ATLAS PC en garde l'**idée** (un
   résultat par source, jamais d'exception qui remonte) sans en faire une interface :
   un simple type de résultat suffit.
2. **Le plafond de temps par requête** (`OperationTimeoutSec = 12`).
3. **Le repli explicite et signalé** (Get-Volume → Win32_LogicalDisk + avertissement).
4. **Le statut par champ.** Retenu sous une forme minimale : une valeur absente
   s'affiche `—`, et une zone non lue devient une ligne de `scan_issue`.
5. **La liste des limites matérielles réelles** (`docs/ADMIN_AND_LIMITATIONS.md`) :
   VRAM réelle, canaux mémoire, SMART derrière un pont USB, EDID, Secure Boot — à
   recopier comme **documentation des limites d'ATLAS PC**, pas comme code.
6. ~~Le HTML autonome sans ressource externe.~~ **Écarté** : ATLAS PC n'a plus de
   fonction de rapport.

## 10. Ce qu'il ne faut pas reprendre

| Élément | Pourquoi |
|---|---|
| PowerShell comme moteur | Latence, distribution, débogage, pas de typage fort |
| WinForms en positions absolues | Non redimensionnable, maintenance coûteuse |
| Fenêtre 780 × 650 fixe | Sous-exploite 1440 × 900 |
| `Get-CimInstance` en série sans mise en cache | Acceptable ici, mais ATLAS PC doit regrouper et mémoriser |
| Le nommage marketing dans les rapports | Sans objet pour ATLAS PC |

---

## 11. Éléments **non vérifiés**

- Durée réelle d'un inventaire complet sur l'OptiPlex 780.
- Comportement de `Get-Volume` / `Get-Partition` en utilisateur standard (le repli
  existe, mais son déclenchement n'a pas été observé).
- Rendu visuel réel de la fenêtre WinForms (analyse du code seulement).
- Validité des affirmations de `docs/BUILD_COMPATIBILITY_AUDIT.md` et
  `docs/DEFECT_ANALYSIS.md` (lus en survol, non audités ligne à ligne).
- Le contenu exact de `tests/Mock-Test-Matrix.json` (existence vérifiée, contenu non
  analysé en détail).

---

## 12. Note pour ATLAS PC

C'est le dépôt qui apporte le plus **sur le fond méthodologique** : comment
interroger Windows sans mentir sur ce qu'on n'a pas pu lire. C'est aussi celui dont
la technologie est la moins transposable. Verdict : **modèle pour la couche de
collecte, contre-exemple pour l'interface et l'exécution.**

> **Révision du 17/11/2026 — périmètre simplifié.** C'est désormais la **première
> source d'inspiration** du dossier (livrable 2, § 6), parce que la rubrique
> « Ordinateur » pèse plus lourd dans un périmètre réduit. Deux apports précis sont
> retenus : le `try/catch` **par requête** avec délai borné, et l'affichage explicite
> d'une valeur absente. Son rapport HTML autonome, lui, est hors périmètre.

Voir notation comparée : `docs/05-tableau-comparatif.md`.
