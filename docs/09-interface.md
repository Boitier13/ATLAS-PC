# Livrable 6 — Interface : sept rubriques, sans complexité

Maquette : **`assets/maquette-atlas-pc.png`** (ambiance et proportions ; les libellés
exacts font foi dans ce document).

> **Révision du 17 novembre 2026.** La rubrique « Rapports » est **supprimée** (aucune
> fonction d'export). Le treemap et l'anneau sont **supprimés** — pas reportés. Les
> graphiques se limitent à des barres horizontales.

---

## 1. Cadre d'affichage

| Paramètre | Valeur |
|---|---|
| Écran cible | 1440 × 900, 96 DPI |
| Fenêtre par défaut | 1360 × 820, redimensionnable, minimum 1100 × 700 |
| Barre latérale | 232 px fixes, **7 entrées** |
| Barre supérieure | 56 px : titre + contexte + action principale |
| Zone de contenu | ≈ 1 144 px après marges de 32 px |
| Densité | grille de 8 px, cartes 16 px de padding |
| Défilement | vertical uniquement dans la zone de contenu |

Aucune vue ne doit provoquer de défilement horizontal à 1440 de large.

---

## 2. Structure permanente

```
┌───────────────────────────────────────────────────────────────────────────┐
│ ┌────────────┐ ┌───────────────────────────────────────────────────────┐  │
│ │ ◈ ATLAS PC │ │ Tableau de bord     [Dernier inventaire : …] [Lancer] │  │
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

- **7 entrées**, dans l'ordre du cahier des charges révisé. Une seule active à la fois
  (fond teinté + barre d'accent de 3 px).
- Pied de barre latérale : « 100 % local » et la version. Rien d'autre.
- **Badge d'état** à côté de la date : `Complet` (teal), `Partiel` (ambre),
  `Annulé` (gris). Toujours avec un texte, jamais seulement une couleur.
- **Bandeau de progression** pendant un inventaire, à la place de la ligne de contexte :

```
Énumération · C:\Users\...\AppData\Local\Microsoft\Edge
[███████████░░░░░░░░░]  312 418 fichiers · 41 Go · 4 min 08 s      [Annuler]
```

Non modal : l'utilisateur peut consulter l'inventaire précédent pendant le scan.

---

## 3. Jetons de conception

### Couleurs

| Rôle | Valeur |
|---|---|
| Fond de page | `#F7F5F0` |
| Surface | `#FFFFFF` |
| Bordure | `#E3E0D8` |
| Texte principal | `#1F2933` |
| Texte secondaire | `#6B7280` |
| Accent | `#2F7F79` |
| Accent doux | `#E6F0EE` |
| Ambre (partiel) | `#B7791F` / fond `#FBF3E2` |
| Rouge (échec) | `#A03A2E` / fond `#FBECEA` |
| Barres neutres | `#9AA0A6` |

Une seule couleur d'accent. Pas de dégradé, pas d'ombre portée.

### Typographie (Segoe UI, fournie par Windows)

| Élément | Taille |
|---|---|
| Titre de rubrique | 20 px Semibold |
| Valeur de carte | 22 px Semibold |
| Libellé de carte | 12 px secondaire |
| Titre de panneau | 14 px Semibold |
| Corps de tableau | 13 px |
| En-tête de colonne | 12 px Semibold secondaire |

Hauteur de ligne : 28 px.

---

## 4. Les sept écrans

### 4.1 Tableau de bord

1. **Quatre cartes** : Processeur · Mémoire · Stockage · Fichiers.
2. **Panneau « Répartition par dossier »** : barres horizontales, top 8 des dossiers
   de premier niveau, valeur à droite.
3. **Panneau « Types de fichiers »** : les 6 extensions les plus volumineuses, en
   lignes avec une barre fine — **pas d'anneau, pas de camembert**.
4. **Tableau « Inventaires »** : Date · Fichiers · Taille · État · **Supprimer**.
   C'est là que l'utilisateur gère son historique.
5. Si le dernier inventaire est partiel : bandeau ambre
   « 12 dossiers n'ont pas pu être lus ».

### 4.2 Ordinateur

Deux colonnes de fiches :

| Gauche | Droite |
|---|---|
| Identité (fabricant, modèle) | Windows (nom, version, build, architecture, date d'installation) |
| Processeur (nom, cœurs) | Mémoire (RAM totale) |
| Carte graphique (nom) | BIOS (éditeur, version) |

Puis un tableau **Disques et volumes** : disque · volume · système de fichiers ·
capacité · libre · % utilisé (avec une barre fine dans la cellule).

Une valeur absente s'affiche `—` avec une info-bulle ; jamais `0`, jamais vide.

### 4.3 Fichiers

```
┌ Espace analysé : [C:\ ▾]     [Rechercher un nom…]                        ┐
├───────────────────────────┬──────────────────────────────────────────────┤
│ Dossiers (320 px)         │ Fichiers                                     │
│ ▾ C:\            312 Go   │ Nom           Ext.   Taille   Modifié        │
│   ▾ Windows       98 Go   │ pagefile.sys  .sys   4,0 Go   12/11/2026     │
│     ▸ WinSxS      12 Go   │ …                                            │
│   ▸ Utilisateurs  74 Go   │                                              │
├───────────────────────────┴──────────────────────────────────────────────┤
│ 412 806 fichiers · 61 208 dossiers · 312 Go · 12 dossiers non analysés   │
└──────────────────────────────────────────────────────────────────────────┘
```

- Arborescence **paresseuse**, construite depuis `parent_path`, triée par taille.
- Tableau **virtualisé**, 5 colonnes, triable.
- Recherche par nom : filtre `LIKE`, plafonné à 500 résultats.
- Ligne d'état permanente. Le compteur « dossiers non analysés » est **cliquable** et
  ouvre la liste des zones en erreur.
- Pas de colonne « chemin complet » : le dossier courant est dans l'en-tête, le chemin
  est en info-bulle.

### 4.4 Logiciels

- Compteur : « 247 logiciels installés ».
- Tableau : Nom · Version · Éditeur · Emplacement · Date d'installation.
- Recherche par nom.
- Aucun filtre par source : il n'y a qu'une source (le registre).

### 4.5 Outils

Six fiches, 3 × 2 :

```
Python                                    ● Détecté
3.11.9
C:\Users\…\Python311\python.exe
```

États : `Détecté` · `Non détecté` · `Échec`. Un bouton global « Relancer la
détection ».

### 4.6 Développement

- Filtres en pastilles : `Tous` · `Python` · `Node.js`.
- Tableau : Projet · Chemin · Type · Dépôt Git (oui / —).
- Compteur : « 12 projets Python · 8 projets Node.js · 14 dépôts Git ».

Aucune vue de dépendances, aucune analyse de `.git`.

### 4.7 Évolution

```
┌ Inventaire A : [03/11/2026 08:40 ▾]   →   Inventaire B : [17/11/2026 09:12 ▾] ┐
├ 4 blocs :  +1 204 fichiers   ·   −318 fichiers   ·   97 modifiés   ·   +4,2 Go  ┤
├ Onglets : Ajoutés | Supprimés | Modifiés | Espace occupé                       ┤
├ Tableau filtré (virtualisé), plafonné à 500 lignes                             ┘
```

- Le sens `A → B` est toujours affiché.
- L'onglet « Supprimés » sépare **suppressions confirmées** et **disparitions non
  vérifiables** (avec la zone responsable). C'est l'application directe de la règle.
- Si l'un des inventaires est **partiel**, un bandeau le rappelle en haut de l'onglet.
- L'onglet « Espace occupé » affiche le delta global puis le delta par dossier de
  premier niveau.
- Si les deux inventaires n'ont pas la même racine (`C:\` vs `D:\`), la comparaison est
  refusée avec un message explicite.

---

## 5. États et comportements

| Situation | Comportement |
|---|---|
| Aucun inventaire | état vide utile : « Aucun inventaire. [Lancer un inventaire] » |
| Inventaire en cours | bandeau non modal, navigation possible, annulation possible |
| Inventaire partiel | badge ambre partout + compteur de zones + onglet dédié |
| Erreur de collecte | la section concernée affiche `—`, le reste continue |
| Base absente ou illisible | message clair, proposition de recréer la base (renommage du fichier) |
| Suppression d'inventaire | confirmation, puis suppression en cascade |

Aucune boîte de dialogue modale pendant un inventaire.

---

## 6. Ce qui n'est pas dans l'interface

| Absent | Motif |
|---|---|
| Rubrique « Rapports », bouton « Exporter » | fonction retirée du périmètre |
| Treemap, anneau, camembert, courbe | coût de développement et de lisibilité |
| Filtres par taille / date / type avancés | non demandés |
| Options, préférences, thèmes au choix | un thème clair unique suffit |
| Statistiques de performance du scan | non demandées |
| Boutons de nettoyage, corbeille, optimisation | contraire à la lecture seule |

La seule visualisation est **la barre horizontale** : dans les cartes du tableau de
bord, dans le panneau de répartition, et dans les cellules de pourcentage. Un seul
composant à écrire, une seule chose à tester.

---

## 7. Lisibilité

- Contraste : texte principal > 12:1, secondaire > 4,5:1.
- Aucune information uniquement colorée : chaque badge porte un texte.
- Police minimale 11 px.
- Navigation clavier : `Tab`, flèches dans les tableaux, `Entrée` pour ouvrir,
  `Échap` pour annuler un inventaire.
- Raccourcis : `Ctrl+I` lancer · `Échap` annuler · `Ctrl+F` rechercher.
  Aucun raccourci destructif.
