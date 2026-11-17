# Livrable 6 — Interface : maquette et spécification

Maquette indicative : **`assets/maquette-atlas-pc.png`** (générée pour ce dossier,
ambiance et proportions uniquement ; les libellés exacts font foi dans ce document).

---

## 1. Cadre d'affichage

| Paramètre | Valeur | Justification |
|---|---|---|
| Résolution cible | **1440 × 900**, 96 DPI | écran du PC cible |
| Fenêtre par défaut | 1360 × 820, **redimensionnable**, minimum 1100 × 700 | marge pour la barre des tâches et le cadre |
| Zone utile (fenêtre maximisée) | 1440 × ~860 | barre des tâches ≈ 40 px |
| Barre latérale | **232 px fixes** | 8 rubriques lisibles sans troncature |
| Barre supérieure | 56 px | titre de rubrique + action principale |
| Zone de contenu | ≈ 1 144 px de large après marges de 32 px | 2 colonnes de 556 px ou 4 cartes de 268 px |
| Densité | 8 px de grille, cartes de 16 px de padding | lisible sans zoom |
| Défilement | vertical uniquement dans la zone de contenu | jamais de défilement horizontal global |
| DPI | manifeste `PerMonitorV2`, mise en page fluide | netteté si l'écran change |

Aucune vue ne doit nécessiter de défilement horizontal à 1440 de large : c'est une
contrainte de conception, pas une préférence.

---

## 2. Structure permanente

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────────┐ ┌────────────────────────────────────────────────────────┐  │
│ │  ◈ ATLAS PC  │ │ Tableau de bord                    [Inventaire partiel] │  │
│ │              │ │ 17/11/2026 09:12 · 412 806 fichiers · 9 min 12 s       │  │
│ │ ▸ Tableau…   │ │                                    [ Lancer un inventaire ]│
│ │   Ordinateur │ ├────────────────────────────────────────────────────────┤  │
│ │   Fichiers   │ │                                                        │  │
│ │   Logiciels  │ │                 zone de contenu                        │  │
│ │   Outils     │ │                 (défilement vertical)                  │  │
│ │   Développ.  │ │                                                        │  │
│ │   Évolution  │ │                                                        │  │
│ │   Rapports   │ │                                                        │  │
│ │              │ │                                                        │  │
│ │ ───────────  │ │                                                        │  │
│ │ 6 Go · HDD   │ │                                                        │  │
│ │ v1.0 · local │ │                                                        │  │
│ └──────────────┘ └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

**Barre latérale** — huit entrées, dans l'ordre du cahier des charges, une seule
active à la fois (fond légèrement teinté + barre d'accent verticale de 3 px à gauche,
comme sur la maquette). En bas, une ligne d'état discrète : mémoire totale, type de
disque, version de l'application, mention « 100 % local ».

**Barre supérieure** — titre de la rubrique, puis une ligne de contexte (date du
dernier inventaire, nombre de fichiers, durée, **badge d'état**), puis l'action
principale à droite. Le badge d'état est présent sur **toutes** les vues :
`Complet` (teal), `Partiel` (ambre), `Annulé` (gris), `Échec` (rouge). Il n'est jamais
seulement coloré : il porte toujours un texte.

**Bandeau de progression** — remplace la ligne de contexte pendant un inventaire :

```
Énumération des fichiers · C:\Users\...\AppData\Local\Microsoft\Edge
[███████████████░░░░░░░░░░░░]  312 418 fichiers · 41 Go · 4 min 08 s     [Annuler]
```

Le bandeau est **dans la page**, pas une boîte de dialogue : l'utilisateur peut
continuer à consulter l'inventaire précédent pendant le scan.

---

## 3. Jetons de conception (design tokens)

### 3.1 Couleurs

| Rôle | Valeur | Usage |
|---|---|---|
| Fond de page | `#F7F5F0` | derrière les cartes |
| Surface | `#FFFFFF` | cartes, tableaux |
| Bordure | `#E3E0D8` | 1 px, jamais de double bordure |
| Texte principal | `#1F2933` | titres, valeurs |
| Texte secondaire | `#6B7280` | libellés, unités |
| Accent | `#2F7F79` | action principale, sélection, séries de graphique |
| Accent doux | `#E6F0EE` | fond de sélection, badge « complet » |
| Ambre | `#B7791F` / fond `#FBF3E2` | badge « partiel », avertissements |
| Rouge | `#A03A2E` / fond `#FBECEA` | échec, zone inaccessible |
| Neutre barres | `#9AA0A6` | séries non accentuées |

Une **seule** couleur d'accent, comme dans Diskplot : moins de couleurs, plus de
lisibilité. Pas de dégradé, pas d'ombre portée (une bordure suffit).

### 3.2 Typographie (Segoe UI, présente sur Windows 10)

| Élément | Taille / graisse |
|---|---|
| Titre de rubrique | 20 px, Semibold |
| Valeur de carte | 22 px, Semibold |
| Libellé de carte | 12 px, Regular, secondaire |
| Titre de panneau | 14 px, Semibold |
| Corps de tableau | 13 px, Regular |
| En-tête de colonne | 12 px, Semibold, secondaire |
| Note, unité | 11-12 px, secondaire |

Hauteur de ligne de tableau : **28 px** (35 lignes visibles dans 980 px de haut, ce
qui suffit avec la virtualisation).

### 3.3 Espacements et formes

- grille de 8 px ; marges de page 32 px ; gouttières 16 px ; padding de carte 16 px ;
- rayon d'angle 6 px ; bordure 1 px ; **aucune ombre** ;
- icônes en trait de 16 px, une seule famille, dessinées en XAML (`Path`), pas d'images.

---

## 4. Écran par écran

### 4.1 Tableau de bord

1. **Quatre cartes de synthèse** (268 × 88) : Processeur · Mémoire · Stockage ·
   Fichiers. Chaque carte : icône, libellé, valeur, sous-ligne optionnelle.
2. **Panneau « Répartition par dossier »** (pleine largeur) : barres horizontales,
   libellé à gauche (160 px), barre, valeur à droite. **Top 8** dossiers.
3. **Deux panneaux côte à côte** : « Types de fichiers » (anneau + légende 5 lignes)
   et « Outils détectés » (6 lignes avec pastille d'état).
4. **Tableau « Inventaires conservés »** : Date · Type · Fichiers · Taille · État ·
   actions. C'est le point d'entrée de la rubrique Évolution.
5. Si le dernier inventaire est **partiel** : bandeau ambre en haut du contenu,
   « 12 dossiers n'ont pas pu être lus — voir le détail ».

### 4.2 Ordinateur

Deux colonnes de fiches, dans cet ordre :

| Colonne gauche | Colonne droite |
|---|---|
| Identité (fabricant, modèle, SKU, châssis, n° de série masqué) | Windows (nom, édition, build, UBR, date d'installation, architecture) |
| Processeur (modèle, cœurs, fréquence, socket) | Mémoire (total, barrettes : slot, capacité, vitesse) |
| Carte graphique (nom, pilote, VRAM avec mention « approx. » si nécessaire) | BIOS (fabricant, version, date) |

Puis un tableau **Disques et partitions** : disque · partition · système de fichiers ·
capacité · libre · % utilisé · type. Une barre de remplissage par volume, dans la
cellule, sur 60 px.

Chaque valeur non disponible s'affiche `—` avec une info-bulle explicative
(« non exposé par le pilote »), **jamais** `0` ni une case vide.

### 4.3 Fichiers

```
┌ Espace analysé : [C:\ ▾]   [Rechercher un nom…]   [Taille min ▾] [Type ▾] ┐
├───────────────────────────┬──────────────────────────────────────────────┤
│ Arborescence (320 px)     │ Tableau des fichiers                         │
│ ▾ C:\            312 Go   │ Nom            Ext.    Taille   Date    Dossier│
│   ▾ Windows       98 Go   │ pagefile.sys   .sys     4,0 Go   12/11   C:\   │
│     ▸ WinSxS      12 Go   │ …                                            │
│   ▸ Utilisateurs  74 Go   │                                              │
├───────────────────────────┴──────────────────────────────────────────────┤
│ 412 806 fichiers · 61 208 dossiers · 312 Go occupés · 12 dossiers masqués│
└──────────────────────────────────────────────────────────────────────────┘
```

- Arborescence **paresseuse** (enfants chargés à l'ouverture), triée par taille
  décroissante, barre de proportion à droite du libellé.
- Tableau **virtualisé**, 6 colonnes maximum, triable par clic sur l'en-tête.
- Aucune colonne « chemin complet » : le dossier courant est dans le fil d'Ariane,
  le chemin complet est en info-bulle. C'est ce qui évite le défilement horizontal.
- Barre d'état permanente : volumes, dossiers inaccessibles, compteur de résultats.

### 4.4 Logiciels

- Bandeau de compteurs : Total · Installés (registre) · Portables (détection
  heuristique) · Mises à jour. Chaque compteur est cliquable et filtre.
- Tableau : Nom · Version · Éditeur · Emplacement · Source · Date d'installation.
- Volet de détail (hauteur 160 px, repliable) sur l'élément sélectionné : clé de
  registre d'origine, taille estimée, architecture.
- Les portables portent une pastille « heuristique » et une info-bulle expliquant la
  méthode de détection. **La confiance est affichée, pas cachée.**

### 4.5 Outils

Six fiches (3 × 2) : Python · pip · Node.js · npm · Git · Tesseract OCR. Chaque fiche :

```
Python                                     ● Détecté
3.11.9                                     Détecté via le PATH
C:\Users\…\AppData\Local\Programs\Python\Python311\python.exe
```

États : `Détecté` (teal), `Non détecté` (gris), `Inaccessible` (ambre),
`Échec de détection` (rouge). Un bouton discret « Relancer la détection » par fiche ;
un bouton global dans la barre.

### 4.6 Développement

- Filtres par type en pastilles : `Tous` · `Node.js` · `Python` · `.NET` · `Git`.
- Tableau : Projet (nom du dossier) · Chemin · Types · Marqueurs · Dernière
  modification · Dépôt Git (oui/non).
- Compteur : « 34 projets · 21 dépôts Git ».
- Aucune analyse de dépendances internes, conformément au cahier des charges : la vue
  s'arrête à la détection des marqueurs.

### 4.7 Évolution

```
┌ Inventaire A : [17/11/2026 09:12 ▾]   →   Inventaire B : [03/11/2026 08:40 ▾] ┐
├ 3 cartes de synthèse : +4,2 Go · +1 204 fichiers · 2 logiciels ajoutés        ┤
├ Onglets : Fichiers ajoutés | Fichiers disparus | Fichiers modifiés |          │
│           Logiciels | Outils | Stockage                                       │
├ Tableau filtré (virtualisé) avec colonnes adaptées à l'onglet                 │
└ Courbe d'évolution du stockage sur les 12 derniers inventaires (facultatif)   ┘
```

- Le sens de comparaison est toujours explicite (`A → B`), jamais implicite.
- Si l'un des inventaires est **allégé** ou **partiel**, l'onglet « Fichiers » affiche
  un bandeau : « comparaison limitée : l'inventaire du 03/11 est allégé (dossiers
  seulement) ».
- Les listes sont plafonnées à l'affichage (500 lignes) avec export du reste en CSV :
  afficher 200 000 lignes n'apporte rien et coûte cher.

### 4.8 Rapports

- Choix de l'inventaire et du contenu (cases à cocher par section).
- Trois cases : `HTML` · `CSV` · `JSON` (PDF absent, ou grisé avec la mention
  « prévu après la V1 »).
- Option « masquer le nom d'utilisateur dans les chemins ».
- Dossier de destination (par défaut `Documents\ATLAS PC\Rapports`).
- Historique des exports réalisés (chemin, date, format, taille).
- Aperçu des 3 premières lignes du rapport avant génération (rassure sur le contenu).

---

## 5. États, retours et règles de comportement

| Situation | Comportement attendu |
|---|---|
| Aucun inventaire | Chaque vue affiche un état vide utile : « Aucun inventaire. [Lancer un inventaire] » |
| Inventaire en cours | Bandeau de progression non modal, annulation possible, navigation possible |
| Inventaire partiel | Badge ambre permanent + bandeau + section « zones non analysées » |
| Inventaire annulé | Conservé seulement s'il contient des données ; sinon supprimé et signalé |
| Erreur de collecte | La section concernée affiche son statut, le reste continue |
| Fichier/dossier disparu depuis l'inventaire | Notifié à l'ouverture (« cet élément n'existe plus »), aucune tentative de modification |
| Comparaison impossible | Message explicite (portées différentes, inventaire allégé, inventaire en échec) |
| Base endommagée | Ouverture en lecture seule + message + proposition de renommer le fichier corrompu |

**Aucune boîte de dialogue modale pendant un inventaire.** Une seule exception :
confirmation de suppression d'un inventaire.

---

## 6. Décision sur le treemap : reporté, avec justification

Trois des quatre dépôts proposent un treemap (NeoTreescan, Diskplot, WinDirStat) ; il
est donc tentant de conclure qu'il est indispensable. Ce dossier ne le retient **pas**
en V1 :

1. **Il n'est pas exigé.** Le cahier des charges demande « interface lisible, moderne,
   avec graphiques et fiches détaillées » ; des barres proportionnelles et un anneau
   y répondent.
2. **Son coût est élevé** pour ce qu'il apporte : le treemap de NeoTreescan fait
   314 lignes pour un rendu à profondeur 4, et celui de WinDirStat occupe plusieurs
   fichiers. C'est un module à concevoir, tester et optimiser, pas un composant
   standard.
3. **Sa lisibilité à 1440 × 900 sans interaction fine est faible** : sans zoom,
   sans info-bulle ni fil d'Ariane, un treemap est une carte de couleurs, pas un
   instrument de mesure. Or ces interactions sont précisément la partie coûteuse.
4. **La question posée par l'utilisateur** (« où est passé mon espace ? ») est mieux
   servie par un classement des dossiers par taille et une arborescence triée.

**Recommandation** : V1 avec barres + arborescence ; réévaluation en fin de V1, avec
un prototype de treemap borné par les pixels (principe de Diskplot : `MIN_AREA`,
cellule « reste ») si le besoin est confirmé à l'usage.

---

## 7. Lisibilité et accessibilité

- Contraste minimum vérifié : texte principal sur surface > 12:1 ; texte secondaire
  > 4,5:1 ; accent sur fond doux > 4,5:1.
- Aucune information portée **uniquement** par la couleur : chaque badge a un texte.
- Taille de police minimale : 11 px, jamais en dessous.
- Navigation clavier complète : `Tab` sur les zones, flèches dans les tableaux,
  `Entrée` pour ouvrir, `Échap` pour annuler un inventaire.
- Raccourcis : `Ctrl+I` lancer un inventaire · `Échap` annuler · `Ctrl+F` rechercher ·
  `Ctrl+E` exporter · `F1` aide contextuelle. **Aucun raccourci destructif.**
- Info-bulles différées de 400 ms, jamais bloquantes.

---

## 8. Correspondance avec les rubriques demandées

| Rubrique du cahier des charges | Vue | Contenu principal |
|---|---|---|
| Tableau de bord | § 4.1 | 4 cartes, répartition, types, outils, historique |
| Ordinateur | § 4.2 | 7 fiches + disques/partitions |
| Fichiers | § 4.3 | arborescence + tableau + filtres |
| Logiciels | § 4.4 | tableau + source + détail |
| Outils | § 4.5 | 6 fiches d'état |
| Développement | § 4.6 | projets + marqueurs + Git |
| Évolution | § 4.7 | A/B + 6 onglets de comparaison |
| Rapports | § 4.8 | HTML/CSV/JSON + historique des exports |

Les huit rubriques du cahier des charges sont couvertes sans ajout fonctionnel.
