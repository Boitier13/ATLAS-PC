# Livrable 6 — Interface : sept rubriques, la recherche au centre

Maquettes : **`assets/maquette-fichiers.png`** (la rubrique centrale) et
**`assets/maquette-atlas-pc.png`** (tableau de bord). Les libellés exacts font foi dans
ce document.

> **Révision du 17 novembre 2026 (3ᵉ).** La rubrique Fichiers est un **explorateur avec
> recherche** et fixe le niveau d'exigence de l'interface. La vérification du 17/11 a
> ajouté trois précisions : aucun accès disque hors ouverture d'un élément (§ 4.9), un
> disque interrompu ne peut produire aucune suppression (§ 4.6), et un renommage est
> présenté comme un retrait + un ajout (§ 5.6).

---

## 1. Cadre d'affichage

| Paramètre | Valeur |
|---|---|
| Écran cible | 1440 × 900, 96 DPI |
| Fenêtre par défaut | 1360 × 820, redimensionnable, minimum 1120 × 700 |
| Barre latérale | 232 px fixes, **7 entrées** |
| Barre supérieure | 56 px : titre + contexte + action principale |
| Zone de contenu | ≈ 1 144 px après marges de 32 px |
| Densité | grille de 8 px, cartes 16 px de padding, lignes de tableau 28 px |
| Défilement | vertical dans la zone de contenu ; **aucun** défilement horizontal |

---

## 2. Structure permanente

```
┌───────────────────────────────────────────────────────────────────────────┐
│ ┌────────────┐ ┌───────────────────────────────────────────────────────┐  │
│ │ ◈ ATLAS PC │ │ Fichiers              [Rechercher…]  [Lancer un inv.] │  │
│ │            │ ├───────────────────────────────────────────────────────┤  │
│ │ ▸ Tableau  │ │                                                       │  │
│ │   Ordinateur│ │              zone de contenu                          │  │
│ │   Fichiers │ │                                                       │  │
│ │   Logiciels│ │                                                       │  │
│ │   Outils   │ │                                                       │  │
│ │   Dévelop. │ │                                                       │  │
│ │   Évolution│ │                                                       │  │
│ │ ────────── │ │                                                       │  │
│ │ 100 % local│ │                                                       │  │
│ └────────────┘ └───────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

- **7 entrées**, une seule active (fond teinté `accent-50` + barre d'accent 3 px).
- La **zone de recherche** de la barre supérieure est permanente à partir de la
  rubrique Fichiers : elle garde la même place sur les six autres rubriques, où elle
  lance la recherche et bascule sur Fichiers.

---

## 3. Couleurs et typographie — les nuances

### 3.1 Neutres et accent

| Rôle | Valeur |
|---|---|
| Fond de page | `#F7F5F0` |
| Surface | `#FFFFFF` |
| Surface alternée (lignes paires) | `#FBFAF7` |
| Bordure | `#E3E0D8` |
| Bordure forte | `#CFCABE` |
| Texte principal | `#1F2933` |
| Texte secondaire | `#6B7280` |
| Texte désactivé | `#A6ABB3` |

**Rampe d'accent** (teal) — c'est elle qui donne la nuance sans casser la sobriété :

| Jeton | Valeur | Usage |
|---|---|---|
| `accent-50` | `#EAF3F1` | fond de l'entrée de menu active, en-têtes de tableau |
| `accent-100` | `#D3E7E3` | survol, sélection de ligne |
| `accent-300` | `#8FC0B9` | barres secondaires |
| `accent-500` | `#2F7F79` | bouton principal, barres principales, liens |
| `accent-700` | `#1F5D58` | survol du bouton principal, texte sur fond clair |

### 3.2 Couleurs de familles de fichiers

Elles servent de **pastille de 8 px** devant le nom et de **fond de badge** (teinte à
12 %) sur la colonne Type. Elles ne colorent jamais une ligne entière.

| Famille | Couleur | Fond du badge | Extensions (extrait) |
|---|---|---|---|
| Documents | `#2F6FA0` | `#E7EFF6` | pdf, docx, xlsx, pptx, txt, odt, csv |
| Images | `#7A5AA6` | `#F0ECF7` | jpg, png, gif, bmp, tif, webp, svg |
| Vidéos | `#B5563F` | `#F8EBE8` | mp4, mkv, avi, mov, wmv |
| Audio | `#B7791F` | `#FBF3E2` | mp3, wav, flac, m4a, ogg |
| Archives | `#8A6A3E` | `#F5EFE6` | zip, 7z, rar, iso, gz |
| Code | `#2F7F79` | `#E6F0EE` | cs, js, ts, py, json, xml, html, css, sql |
| Exécutables | `#5C6570` | `#ECEEEF` | exe, dll, msi, sys, bat, ps1 |
| Autres | `#9AA0A6` | `#F2F3F4` | tout le reste, y compris sans extension |

Le dictionnaire famille → extensions est **statique dans le code** (~30 familles,
majoritairement les 8 ci-dessus) : aucune table, aucune requête supplémentaire.

### 3.3 Couleurs d'état

| État | Texte | Fond | Emploi |
|---|---|---|---|
| Complet | `#2F7F79` | `#E6F0EE` | inventaire ou disque entièrement analysé |
| Partiel | `#B7791F` | `#FBF3E2` | zones non analysées |
| Inaccessible / Annulé | `#A03A2E` | `#FBECEA` | disque refusé, inventaire annulé |
| Information | `#2F6FA0` | `#E7EFF6` | note neutre (ex. « inventaire du 03/11 ») |

### 3.4 Remplissage des disques

Une barre par volume, dont la couleur **porte un sens** :

| Taux occupé | Couleur |
|---|---|
| < 70 % | `accent-500` `#2F7F79` |
| 70-85 % | `#B7791F` |
| > 85 % | `#A03A2E` |

### 3.5 Rampe des barres

Dans le panneau « Répartition par dossier » et le tableau de bord, les barres suivent
une rampe de cinq teintes dégressives — `#2F7F79`, `#4A918B`, `#6BA8A1`, `#8FC0B9`,
`#B3D3CE` — plutôt qu'un gris unique : la hiérarchie se lit d'un coup d'œil, sans
légende.

### 3.6 Typographie (Segoe UI, fournie par Windows)

| Élément | Taille |
|---|---|
| Titre de rubrique | 20 px Semibold |
| Valeur de carte | 22 px Semibold |
| Libellé de carte | 12 px secondaire |
| Titre de panneau | 14 px Semibold |
| Corps de tableau | 13 px |
| En-tête de colonne | 12 px Semibold secondaire |
| Chemin (colonne `Chemin`, fil d'Ariane) | 12 px, `#6B7280` |

Aucune ombre portée, aucun dégradé, aucune transparence (compatibilité GPU ancien).

---

## 4. Rubrique **Fichiers** — la fonction centrale

### 4.1 Disposition

```
┌ Fichiers ─────────────────────────────────── [⌕ Rechercher un fichier ou un dossier] ┐
│ Disques :  ● C: 312 Go (complet)   ● D: 148 Go (partiel)   ○ E: non analysé          │
│ C:\ › Users › william › Documents › Factures                        [Copier le chemin]│
├──────────────────────┬──────────────────────────────────────────────────────────────┤
│ DOSSIERS        312 Go│ [Extension ▾] [Type ▾] [Taille ▾] [Date ▾] [Emplacement ▾]  │
│ ▾ C:\           312 Go│ 1 204 résultats (200 affichés) · ⚠ 12 zones non analysées    │
│   ▾ Users        74 Go│ ┌────┬──────────────┬──────────┬────────┬──────────┬────────┐│
│     ▾ william    61 Go│ │ ●  │ Nom          │ Type     │ Taille │ Modifié  │ Chemin ││
│       ▸ Documents 12 Go│ ├────┼──────────────┼──────────┼────────┼──────────┼────────┤│
│       ▸ Images    9 Go│ │ ●  │ 2026-03.pdf  │ Document │ 1,2 Mo │ 12/11/26 │ C:\…   ││
│   ▸ Windows      98 Go│ │ ●  │ budget.xlsx  │ Tableur  │ 84 Ko  │ 03/11/26 │ C:\…   ││
│   ▸ Program Files 31 Go│ │ …  │              │          │        │          │        ││
├──────────────────────┴──────────────────────────────────────────────────────────────┤
│ C:\Users\william\Documents\Factures · 148 fichiers · 7 sous-dossiers · 1,4 Go        │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

Cinq zones, dans cet ordre de lecture :

1. **Zone de recherche**, en haut à droite de la barre de titre, large (420 px) et
   toujours au même endroit. `Ctrl+F` y place le curseur.
2. **Sélecteur de disques** : une pastille d'état par disque (`●` complet, `●` partiel,
   `○` non analysé), taille, et le nom du volume en info-bulle. Un clic filtre sur ce
   disque ; « Tous » est l'état par défaut.
3. **Fil d'Ariane**, cliquable à chaque niveau, avec un bouton *Copier le chemin* à
   droite.
4. **Arbre** (320 px) à gauche, **résultats** à droite, avec sa barre de filtres.
5. **Ligne d'état** en bas : dossier courant, nombre de fichiers, de sous-dossiers,
   taille, et le rappel de couverture.

### 4.2 L'arbre

- nœuds chargés **au dépliage** uniquement ;
- chaque nœud : nom, taille cumulée alignée à droite, chevron ; l'info-bulle donne
  « 4 812 fichiers · 48 sous-dossiers · modifié le 12/11/2026 » ;
- un dossier non analysé porte une pastille ambre et un texte `non analysé` en
  info-bulle ;
- tri par taille décroissante (le plus gros d'abord), option « ordre alphabétique » ;
- le nœud sélectionné est teinté `accent-50` avec la barre d'accent à gauche.

### 4.3 Le tableau des résultats

| Colonne | Largeur | Contenu |
|---|---|---|
| ● | 24 px | pastille de famille (couleur du § 3.2) |
| Nom | 260 px | nom, tronqué à droite, info-bulle = nom complet |
| Type | 110 px | libellé de famille sur fond teinté (`Document`, `Image`…) |
| Taille | 90 px | aligné à droite, formaté (`1,2 Mo`), tri numérique |
| Modifié | 100 px | `12/11/2026 14:32` |
| Chemin | reste | dossier, **élidé au milieu**, info-bulle = chemin complet |

- **pagination de 200 lignes** au défilement, jamais de chargement global ;
- tri sur les colonnes Nom, Type, Taille, Modifié, Chemin (le nom complète toujours le
  tri pour rester déterministe) ;
- ligne survolée `accent-50`, sélection `accent-100` ;
- **double-clic** : ouvre l'élément ; **menu contextuel** : *Ouvrir* · *Ouvrir
  l'emplacement* · *Copier le chemin* · *Copier le nom* ;
- la colonne Chemin reste lisible : les chemins très longs sont élidés au **milieu**
  (`C:\Users\…\Factures\2026-03.pdf`) et complets en info-bulle.

### 4.4 Les filtres

Cinq listes déroulantes compactes (150 px chacune), sur une seule ligne :

| Filtre | Contenu | Comportement |
|---|---|---|
| Extension | liste des extensions présentes, avec compteur | choix multiple, case à cocher |
| Type | les 8 familles du § 3.2 | choix multiple, pastille de couleur |
| Taille | `< 10 Ko`, `10 Ko - 1 Mo`, `1 - 100 Mo`, `100 Mo - 1 Go`, `> 1 Go`, personnalisé | une seule valeur |
| Date | Aujourd'hui, 7 jours, 30 jours, cette année, personnalisé | une seule valeur |
| Emplacement | dossier courant, disque courant, tous les disques | une seule valeur |

Chaque filtre actif prend la teinte `accent-50` avec un liseré `accent-300` et une croix
de retrait. Un bouton *Tout effacer* apparaît dès qu'un filtre est actif. Le compteur
« 1 204 résultats (200 affichés) » est placé à droite de la ligne de filtres, suivi de la
mention de couverture.

### 4.5 La recherche

- une seule zone, un seul comportement visible : l'utilisateur tape, les résultats
  arrivent ; la stratégie est invisible. Trois cas sont servis sans que l'utilisateur ait
  à choisir : **nom exact** (immédiat), **préfixe** (immédiat), **sous-chaîne** (avec
  indicateur si cela dépasse une seconde) ;
- pendant une recherche de plus d'une seconde : un liseré animé sur la zone et un bouton
  *Annuler* — l'interface ne se fige jamais ;
- saisie de 1 ou 2 caractères : mention « recherche large, résultats limités » ;
- `Échap` efface la recherche, `Ctrl+F` y revient ;
- la recherche porte sur **tous les disques** de l'inventaire choisi, sauf si le filtre
  Emplacement en décide autrement — et l'interface le dit dans le compteur.

### 4.6 Couverture : cinq affichages, jamais un total trompeur

| Situation | Affichage |
|---|---|
| un disque partiel | pastille ambre + `⚠ 12 zones non analysées` cliquable (ouvre la liste) |
| disque non accessible | pastille rouge + `non accessible`, disque non coché par défaut |
| dossier non lisible | ligne rouge dans le tableau des sous-dossiers, `Accès refusé` |
| inventaire annulé / en cours | bandeau : « inventaire incomplet — les résultats ne couvrent pas tout le disque » |
| disque interrompu (`cancelled`) | pastille rouge : « parcours interrompu », et l'onglet Évolution refuse d'y conclure une suppression |

Le nombre de résultats est **toujours** accompagné de cette mention ; aucun écran
n'affiche un total nu.

### 4.7 Ouverture d'un élément

- soit le chemin existe encore : ouverture immédiate (dossier → Explorateur, fichier →
  application associée) ;
- soit il a disparu : message discret sous la barre d'outils — « ce fichier n'existe plus
  depuis l'inventaire du 03/11/2026 » — et aucune tentative d'ouverture ;
- jamais de boîte de dialogue bloquante, jamais d'erreur système brute.

### 4.8 États vides

| État | Message et action |
|---|---|
| aucun inventaire | « Aucun inventaire. Lancez-en un pour pouvoir chercher vos fichiers. » + bouton |
| recherche sans résultat | « Aucun élément ne correspond à “xyz” dans `C:` et `D:` » + *Effacer les filtres* |
| dossier vide | « Ce dossier ne contient aucun fichier. » (distinct de « non analysé ») |
| disque non analysé | « `E:` n'a pas été analysé. » + *Lancer un inventaire* |

---

### 4.9 Aucune relecture du disque

Le démarrage de l'application, l'ouverture de la rubrique, la navigation, la recherche,
les filtres, la pagination et le tableau de bord **ne touchent jamais au disque** : tout
est lu dans SQLite. Le seul accès est la vérification d'existence au moment d'ouvrir un
élément (§ 4.7), et il n'a lieu que sur action explicite.

Conséquence pour l'utilisateur : après un inventaire, la recherche est instantanée et
fonctionne même si un disque externe a été débranché — les résultats affichés décrivent
l'inventaire, pas l'état présent du disque.

## 5. Les six autres rubriques

### 5.1 Tableau de bord

1. **Quatre cartes**, chacune avec une icône sur fond teinté (quatre teintes
   différentes : teal, bleu, violet, ambre) : Processeur · Mémoire · Stockage · Fichiers.
2. **Disques et remplissage** : une barre par volume, couleur selon le taux (§ 3.4).
3. **Répartition par dossier** : barres horizontales, rampe du § 3.5, top 8.
4. **Types de fichiers** : 6 lignes avec pastille de famille et barre fine.
5. **Inventaires** : date · disques · fichiers · taille · état · **Supprimer**.
6. Bandeau ambre si le dernier inventaire est partiel, avec lien vers la liste des zones.

### 5.2 Ordinateur

| Gauche | Droite |
|---|---|
| Identité (fabricant, modèle) | Windows (nom, version, build, architecture, date d'installation) |
| Processeur (nom, cœurs) | Mémoire (RAM totale) |
| Carte graphique (nom) | BIOS (éditeur, version) |

Puis un tableau **Disques et volumes** : disque · volume · système de fichiers ·
capacité · libre · % utilisé (barre colorée). Valeur absente : `—` + info-bulle.

### 5.3 Logiciels

Compteur « 247 logiciels installés », tableau Nom · Version · Éditeur · Emplacement ·
Date, recherche par nom.

### 5.4 Outils

Six fiches 3 × 2 : nom, version, chemin, état (`Détecté` teal · `Non détecté` gris ·
`Échec` rouge). Bouton *Relancer la détection*.

### 5.5 Développement

Pastilles de filtre `Tous` · `Python` · `Node.js`, tableau Projet · Chemin · Type ·
Dépôt Git, compteurs.

### 5.6 Évolution

Sélecteurs A et B (avec les disques couverts et l'état de chacun), quatre blocs de
synthèse (+ajoutés · −supprimés · modifiés · espace), quatre onglets : *Ajoutés*,
*Supprimés*, *Modifiés*, *Espace occupé*.

Trois règles d'affichage :

1. l'onglet *Supprimés* sépare les **suppressions confirmées** des **disparitions non
   vérifiables**, chaque ligne portant la zone responsable ;
2. un disque `cancelled` ou `inaccessible` dans l'inventaire B ne produit **aucune**
   suppression confirmée : ses lignes sont toutes en « non vérifiable », avec le motif ;
3. un disque présent dans un seul des deux inventaires est listé comme **non comparable**
   — jamais compté comme supprimé.

Et une mention permanente en bas de l'onglet *Supprimés* :

> Un **déplacement** ou un **renommage** apparaît ici comme une suppression, et dans
> l'onglet *Ajoutés* comme un fichier nouveau. ATLAS PC ne tente pas de les reconnaître.

C'est une décision de V1, écrite à l'écran, pas une limite cachée.

---

## 6. États et comportements transverses

| Situation | Comportement |
|---|---|
| inventaire en cours | bandeau non modal ; navigation possible ; annulation possible |
| erreur de collecte | la section concernée affiche `—`, le reste continue |
| base absente ou illisible | message clair + proposition de recréer la base (renommage) |
| suppression d'inventaire | confirmation indiquant ce qui sera perdu |
| ouverture d'un élément absent | message informatif, non bloquant |

Aucune boîte modale pendant un inventaire.

---

## 7. Ce qui reste hors de l'interface

| Absent | Motif |
|---|---|
| rubrique « Rapports », bouton « Exporter » | fonction retirée du périmètre |
| treemap, anneau, camembert, courbe | coût et lisibilité ; barres suffisantes |
| recherche dans le contenu, OCR | hors périmètre V1 |
| renommer, déplacer, supprimer, corbeille | lecture seule |
| détection de doublons | hors périmètre V1 |
| options, thèmes au choix | un thème clair unique |

---

## 8. Lisibilité et accessibilité

- contraste : texte principal > 12:1, secondaire > 4,5:1 ; les couleurs de famille ne
  sont **jamais** seules porteuses d'information (la colonne Type écrit le libellé) ;
- police minimale 11 px ;
- navigation clavier complète : `Tab`, flèches dans l'arbre et les tableaux, `Entrée`
  pour ouvrir, `Échap` pour annuler un scan ou vider la recherche ;
- raccourcis : `Ctrl+F` rechercher · `Ctrl+C` copier le chemin de la ligne · `Ctrl+I`
  lancer un inventaire · `Échap` annuler. Aucun raccourci destructif ;
- mode contraste élevé de Windows : les teintes de fond sont remplacées par des
  bordures, la pastille par un libellé.
