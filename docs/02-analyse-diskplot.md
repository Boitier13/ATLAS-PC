# Livrable 1.2 — Rapport d'analyse : Diskplot

> Dépôt : `https://github.com/rodrigokiller/diskplot`
> Commit examiné : `8787cf6b01840cddd263915ffaf286c78c678b05` (2026-10-17, tag v0.4.0)
> Licence : **MIT** · Analyse **statique** (code non exécuté)

---

## 1. Identification

| Élément | Valeur vérifiée | Source |
|---|---|---|
| Nature | Analyseur d'espace disque : treemap + arbre, instantanés, diff, doublons | `README.md` |
| Plateforme | **Electron + React 19 + TypeScript**, `electron-vite`, `electron-builder` | `package.json` |
| Version | 0.4.0 | `package.json` |
| Volume | ≈ 8 640 lignes TS/TSX dans `src/` | `wc -l` |
| Dépendances runtime | `electron-updater ^6.8.9`, `koffi ^3.3.2` | `package.json` → `dependencies` |
| Dépendances dev | `electron ^44.5.1`, `react ^19.3.0`, `electron-builder ^26.15.3`, `typescript ^7.0.2` | idem |
| Licences des dépendances (registre npm) | koffi **MIT**, electron **MIT**, react **MIT** | `registry.npmjs.org/<pkg>/latest` |
| Tests | `npm test` pointe sur `src/shared/*.test.ts` et `src/main/*.test.ts`… **aucun fichier `.test.ts` n'existe** dans le dépôt | `package.json` + `find . -name "*.test.ts"` (0 résultat) |
| Plateformes | Windows supporté ; Linux/macOS/Android expérimentaux | `README.md`, `.github/workflows/release.yml` |
| Sécurité Electron | `sandbox: true`, `contextIsolation: true`, preload typé | `src/main/index.ts` (webPreferences), `src/preload/index.ts` |

**Constat de fiabilité** : le script de test du projet ne peut pas s'exécuter en
l'état, faute de fichiers de test. Ce n'est pas anodin : la partie la plus délicate
(calcul de diff, réduction d'instantanés) n'est couverte par aucun test versionné.

---

## 2. Architecture réelle

```
src/main/        index.ts (fenêtres, IPC, cycle de vie) 618 l.
                 scanworker.ts (coordinateur de scan, 311 l.)
                 dirworker.ts  (lecture d'un lot de dossiers, 235 l.)
                 dupworker.ts  (hachage des doublons)
                 snapshots.ts  (écriture/lecture des instantanés)
src/preload/     pont typé IPC → renderer
src/renderer/    React : App, Treemap.tsx, TreeView.tsx, Panels, lib/analysis.ts, lib/treemap.ts
src/shared/      types.ts (contrat de données), table.ts (construction de la table), reduce.ts
site/, android/  site vitrine et build Capacitor (hors périmètre ATLAS PC)
```

Le point remarquable est `src/shared/types.ts` : **le contrat de données entre
workers et interface est un ensemble de tableaux typés**, pas un arbre d'objets.

---

## 3. Le cœur : un modèle « en colonnes »

### 3.1 Structure

`src/shared/types.ts`, interface `ScanTable` :

| Tableau | Type | Rôle |
|---|---|---|
| `parent` | `Int32Array` | index du parent (le parent précède toujours l'enfant) |
| `size` | `Float64Array` | octets occupés sur disque |
| `logical` | `Float64Array` | longueur réelle du fichier |
| `mtime` | `Uint32Array` | secondes epoch |
| `flags` | `Uint8Array` | `F_DIR`, `F_LINK`, `F_ERR`, `F_GONE` |
| `files` | `Uint32Array` | nombre de fichiers du sous-arbre |
| `nameOff` / `names` | `Uint32Array` / `Uint8Array` | tous les noms concaténés en UTF-8 |
| `childStart` / `childList` | `Int32Array` | enfants de chaque dossier, triés par taille |
| `ext` / `exts` | `Uint16Array` / `string[]` | extension indexée |

Le commentaire d'en-tête du fichier annonce « about 60 bytes per file ». **Ordre de
grandeur cohérent** avec les tableaux ci-dessus plus un nom moyen de 20 octets
(estimation arithmétique, non mesurée).

### 3.2 Pourquoi c'est rapide — et pourquoi c'est intéressant pour ATLAS PC

`src/shared/table.ts`, fonction `finishTable` : les tailles sont cumulées par **une
seule passe arrière** (`for (let i = count - 1; i >= 1; i--)`), possible parce que
tout parent précède ses enfants. Les enfants sont ensuite regroupés en une passe
(`childrenOf`). Aucune récursion, aucune allocation par fichier, aucune chaîne par
fichier.

Les tableaux grossissent par doublement (`grow()` / `growNames()` dans
`src/main/scanworker.ts`) puis sont **transférés** entre threads (`tableBuffers`), ce
qui évite une copie.

**Ce qui est transposable à ATLAS PC** : l'idée que le nombre de fichiers ne doit pas
se traduire par un objet par fichier. En C#/SQLite, la variante encore plus sobre est
de ne **rien** garder en mémoire pour les fichiers et d'insérer par lots.

### 3.3 Accès disque

`src/main/dirworker.ts`, fonction `windowsBulk()` : chargement de `koffi`, puis
`CreateFileW` sur le dossier, `GetFileInformationByHandleEx(..., FileFullDirectoryInfo = 14, ...)`
avec un tampon de `256 * 1024` octets. Un appel rapporte des centaines d'entrées avec
taille, allocation et horodatage. Repli `readdir` + `lstat` hors Windows
(`portable()`).

Détails corrects et à retenir :
- préfixe `\\?\` (et `\\?\UNC\`) construit à la main ;
- `FILE_FLAG_BACKUP_SEMANTICS` (`0x02000000`) pour ouvrir un dossier ;
- erreurs traduites en codes portables (`EPERM` pour 5, `ENOENT` pour 2/3, `EBUSY`
  pour 32) ;
- entrées `.` et `..` filtrées explicitement.

### 3.4 Parallélisme, progression, annulation

| Aspect | Implémentation | Fichier |
|---|---|---|
| Taille du pool | `Math.max(2, Math.min(12, availableParallelism()))` | `src/main/scanworker.ts` |
| Granularité | lots de `BATCH = 24` dossiers, `IN_FLIGHT = 2` par worker | idem |
| File de travail | pile `stackIds` / `stackPaths` (profondeur d'abord) | idem |
| Progression | message toutes les **120 ms**, avec les 14 plus gros postes | idem |
| Aperçu vivant | table partielle envoyée ≈ 1×/s, coût plafonné à ~1/10 du temps de scan | idem, `live()` |
| Arrêt | **deux** commandes distinctes : `stop` (garde le partiel) et `cancel` (jette tout) | idem, `port.on("message")` |
| Sécurité d'arrêt | le parent envoie `cancel`, attend **4 s**, puis `terminate()` — « a hard stop while its threads load native code can take the whole app down » | `src/main/index.ts`, `retire()` |

Ce couple `stop`/`cancel` est une bonne idée de conception : ATLAS PC doit distinguer
« annuler et conserver l'inventaire partiel, marqué incomplet » de « abandonner ».

Sur le PC cible, `availableParallelism()` vaudra 2 : le pool sera de 2. Le code
s'adapte donc, mais deux threads sur un disque mécanique restent discutables.

---

## 4. Historique : instantanés et comparaison

C'est **la seule implémentation d'historique** des quatre dépôts, et elle mérite un
examen précis — y compris de ses limites.

### 4.1 Instantanés

`src/main/snapshots.ts` :
- fichier `.dps` = en-tête JSON + tableaux binaires, **compressé en gzip** (niveau 3) ;
- un fichier `.dps.json` compagnon contenant les métadonnées ;
- nom = `sha1(root.toLowerCase())[0..12] + "-" + date` ;
- `KEEP_PER_ROOT = 8` : purge au-delà de 8 instantanés par racine ;
- lecture : le nom de fichier est validé par `^[0-9a-f]{12}-\d+\.dps$` avant ouverture
  (protection contre la traversée de chemin) — bon réflexe.

### 4.2 Ce que contient réellement un instantané

`src/shared/reduce.ts`, `reduceTable()` :

```ts
export const BIG_FILE = 8 * 1024 * 1024;
...
if (i === 0 || t.flags[i] & F_DIR || t.size[i] >= BIG_FILE) { remap[i] = n++; ... }
```

Autrement dit : **tous les dossiers** sont conservés, mais **uniquement les fichiers
≥ 8 Mio**. Les fichiers plus petits n'existent pas dans l'historique.

### 4.3 Ce que la comparaison peut donc dire

`src/renderer/src/lib/analysis.ts`, `compare()` :
- delta par nœud = `taille actuelle - taille de l'instantané`, appariement par **nom**
  dans le même dossier parent ;
- les entrées « nouvelles » sont marquées `isNew` ; les disparues ne sont remontées
  que si `snap.size[o] >= TRACKED_FILE` (8 Mio) ;
- les lignes explicatives sont filtrées sous `FLOOR = 4 Mio` et plafonnées à 400
  lignes, 200 disparitions.

**Conclusion décisive pour ATLAS PC** : ce mécanisme répond à « qu'est-ce qui a
grossi ? » au niveau des dossiers et des gros fichiers. Il ne répond **pas** à
« quels fichiers ont été ajoutés, supprimés ou modifiés ? » pour l'ensemble des
fichiers, ce que le cahier des charges exige (fonctionnalité 7). Il ne conserve ni
version, ni éditeur, ni outil, ni projet.

Autre limite structurelle : un instantané est un **binaire propriétaire**, pas une
base interrogeable. Toute nouvelle question sur l'historique impose de modifier le
format et le code. Une base SQLite, à l'inverse, permet d'ajouter une requête sans
toucher au stockage.

---

## 5. Fonctionnalités annexes examinées

| Fonction | Constat | Chemin |
|---|---|---|
| Doublons | comparaison par taille, puis hash SHA-1 des 16 Kio de tête et de queue, puis hash complet des survivants | `src/main/dupworker.ts` |
| Lecture de contenu | **oui**, les fichiers sont ouverts en lecture (`openSync(path, "r")`) pour les doublons | idem |
| Surveillance « Live » | `fs.watch({ recursive: true })` + re-lecture des dossiers modifiés après 900 ms d'agitation | `src/main/index.ts` |
| Corbeille / suppression | présent côté Electron (`Trash`, actions de la fenêtre) | `src/main/index.ts`, renderer |
| Multi-fenêtres | une session de scan par fenêtre (`sessions` Map) | idem |
| Mise à jour | `electron-updater` | `package.json` |
| Treemap | squarifié, **coût borné par les pixels** : `MIN_AREA = 12`, `MAX_RECTS = 90_000`, repli en cellule « rest » | `src/renderer/src/lib/treemap.ts` |
| Matériel / OS | **aucun** | absence vérifiée |
| Logiciels installés | **aucun** (les « Apps » concernent Android) | `src/main/index.ts` |
| Outils de dev | **aucun** | absence vérifiée |
| Rapports | **aucun export CSV/JSON/HTML** ; le CSV figure seulement au *roadmap* | `README.md` § Roadmap |

Le choix « coût du treemap borné par les pixels, pas par le nombre de fichiers » est
excellent et directement applicable si ATLAS PC ajoute un treemap.

---

## 6. Adéquation au PC cible

| Critère | Constat |
|---|---|
| Mémoire | Electron = processus Chromium complets (principal + renderer + GPU + utilitaires) en plus d'un worker de scan. Sur 6 Go de RAM, c'est le poste le plus coûteux du système. |
| Disque | Un binaire Electron occupe typiquement plusieurs centaines de Mo (dossier `resources/app.asar` + Chromium). Sur disque mécanique, le démarrage à froid se paie en secondes. |
| CPU | Le rendu du treemap sur canvas est borné, mais un moteur Chromium sur un CPU ancien reste lourd. |
| Offline | Aucun appel réseau nécessaire au scan ; `electron-updater` est optionnel. |
| Privilèges | Scan sans admin (sauf le MFT, resté au roadmap). Conforme à la règle ATLAS PC. |
| Licence | MIT : réutilisation possible avec attribution. |
| Réutilisabilité en C# | **Nulle en code** (TypeScript/Electron) ; **forte en idées** (modèle en colonnes, stop/cancel, bornage pixel, instantanés). |

---

## 7. Licence et implications

- `LICENSE` = **MIT**, `Copyright (c) 2026 Rodrigo Sanguanini`.
- MIT : réutilisation, modification, redistribution, usage commercial autorisés, sous
  réserve de conserver la notice de copyright et le texte de licence.
- Aucune clause de brevets, aucune clause de copyleft. Compatible avec un projet
  ATLAS PC sous MIT, Apache-2.0 ou propriétaire.
- Le code étant en TypeScript, une transposition en C# serait de toute façon une
  **réécriture** : c'est la définition même d'une inspiration licite. En revanche,
  copier des blocs TypeScript dans un projet C# n'aurait aucun sens technique.

---

## 8. Ce qui est réutilisable, ce qui ne l'est pas

### À retenir comme inspiration

1. **Modèle de données « en colonnes »** : le nombre de fichiers ne doit pas créer
   d'objets.
2. **Deux sortes d'arrêt** : conserver le partiel / tout jeter.
3. **Aperçu progressif** dont le coût est plafonné par rapport au scan lui-même.
4. **Treemap borné par les pixels** et repli en « reste ».
5. **Instantané compact + purge** : un historique doit avoir une politique de
   rétention, sinon il remplit le disque qu'il prétend surveiller.
6. **Validation du nom de fichier** avant lecture d'un instantané.
7. **Un seul module `shared`** comme contrat entre moteur et interface.

### À ne pas reprendre

| Élément | Pourquoi |
|---|---|
| Electron / React | Inadapté à 6 Go / disque mécanique pour une application de bureau Windows |
| Fichiers d'instantané binaires | Non interrogeables, format à faire évoluer à chaque question |
| Seuil 8 Mio dans l'historique | Ne permet pas la comparaison fichier par fichier exigée |
| `fs.watch` récursif | Surveiller une arborescence entière coûte cher et rate des événements ; hors périmètre V1 |
| Hachage de contenu | Lit les fichiers (contraire à l'esprit « métadonnées seulement » du cahier des charges) |
| Absence de tests malgré un script qui les annonce | À ne pas imiter : le diff doit être testé |

---

## 9. Éléments **non vérifiés**

- Temps de scan réels (les 1,9 s / 10,0 s du README sont des mesures de l'auteur, sur
  Windows 11, cache chaud — non reproduites).
- Empreinte mémoire réelle au repos et pendant un scan.
- Taille exacte du binaire distribué.
- Comportement du watcher sur un volume entier.
- Le rendu du treemap sur un GPU ancien (le rendu est canvas 2D, a priori peu
  dépendant du GPU).

---

## 10. Note pour ATLAS PC

Diskplot est la **meilleure source d'idées sur le moteur et l'historique**, et le
**contre-exemple sur la plateforme**. Sa contribution conceptuelle majeure est double :
prouver qu'un scan peut tenir en quelques dizaines de Mo, et rappeler qu'un historique
sans politique de rétention devient un problème.

Voir notation comparée : `docs/05-tableau-comparatif.md`.
