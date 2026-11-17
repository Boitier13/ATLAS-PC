# ATLAS PC — Dossier technique préalable

Étude technique **sans programmation** pour un logiciel Windows d'inventaire local
(matériel, Windows, fichiers, logiciels, outils de développement, projets), avec
historique et comparaison d'inventaires.

> **Version 2.2 — 17 novembre 2026.** Périmètre simplifié (un seul projet, sept
> rubriques, cinq étapes) et **priorité donnée à la rubrique Fichiers** : explorer,
> rechercher, filtrer, ouvrir, copier un chemin. Ce qui a été retiré l'a été
> **supprimé**, pas reporté — liste et motifs dans
> [`docs/00-lisez-moi.md`](docs/00-lisez-moi.md) § 5 ; ce que cette priorité ajoute et
> rétablit est au § 5.9, et les vérifications finales au § 5.10.

**Machine cible** : Dell OptiPlex 780 · Windows 10 22H2 x64 · 6 Go de RAM · disque dur
mécanique · 1440 × 900 · hors ligne · sans abonnement · sans droits administrateur.

> Ce dépôt contient un **dossier de décision**, pas du code. Aucun fichier source n'a
> été écrit, aucun dépôt étudié n'a été modifié.

---

## Lire le dossier

| Document | Contenu |
|---|---|
| [`docs/00-lisez-moi.md`](docs/00-lisez-moi.md) | Sommaire, méthode, **synthèse, décision et ce qui a été supprimé** — commencer ici |
| [`docs/01-analyse-neotreescan.md`](docs/01-analyse-neotreescan.md) | Analyse du dépôt NeoTreescan (Apache-2.0, C#/WPF) |
| [`docs/02-analyse-diskplot.md`](docs/02-analyse-diskplot.md) | Analyse du dépôt Diskplot (MIT, Electron/TypeScript) |
| [`docs/03-analyse-system-inspector.md`](docs/03-analyse-system-inspector.md) | Analyse du dépôt LouisMahdi System Inspector (MIT, PowerShell) |
| [`docs/04-analyse-windirstat.md`](docs/04-analyse-windirstat.md) | Analyse du dépôt WinDirStat (GPL-3.0-or-later, C++) |
| [`docs/05-tableau-comparatif.md`](docs/05-tableau-comparatif.md) | Comparaison technique, fonctionnelle et notation pondérée |
| [`docs/06-licences-et-risques.md`](docs/06-licences-et-risques.md) | Licences, compatibilité, inspiration vs copie |
| [`docs/07-architecture-cible.md`](docs/07-architecture-cible.md) | Architecture recommandée et justification de chaque choix |
| [`docs/08-modele-sqlite.md`](docs/08-modele-sqlite.md) | Schéma (10 tables), requêtes de navigation et de recherche, comparaison, rétention manuelle |
| [`docs/09-interface.md`](docs/09-interface.md) | Maquette et spécification des sept rubriques |
| [`docs/10-arborescence-projet.md`](docs/10-arborescence-projet.md) | Organisation prévue du futur code source |
| [`docs/11-plan-de-developpement.md`](docs/11-plan-de-developpement.md) | Cinq étapes (dont 3A scan / 3B rubrique Fichiers), tests, critères |
| [`docs/12-risques-et-limites.md`](docs/12-risques-et-limites.md) | Risques, limites, points à vérifier |
| [`docs/13-verifications.md`](docs/13-verifications.md) | **Vérifications de la v2.1** : cinq points, constats, corrections |

Maquettes : [`assets/maquette-fichiers.png`](assets/maquette-fichiers.png) (la rubrique centrale) et [`assets/maquette-atlas-pc.png`](assets/maquette-atlas-pc.png) (tableau de bord)

---

## Décision recommandée en bref

| Sujet | Choix | Raison dominante |
|---|---|---|
| Projet | **un seul projet WPF**, dossiers par responsabilité | moins de couches, moins de fichiers |
| Runtime | **.NET Framework 4.8** (`net48`), cible **unique**, projet au format SDK | présent dans Windows 10 22H2, aucun runtime à installer, démarrage à froid rapide sur disque mécanique |
| Interface | **WPF**, thème clair sobre, `DataGrid` virtualisé | mature, DPI-aware, pas de dépendance graphique lourde |
| Base | **SQLite, 10 tables** | fichier unique, recherche et comparaison en SQL, aucun service |
| Scan | **un seul travailleur**, API standard .NET, écriture par lots de 500 | disque mécanique bi-cœur, mémoire bornée |
| Recherche | **index sur le nom**, deux niveaux (préfixe indexé, puis sous-chaîne) | rapide sans index plein texte ; FTS5 seulement si la mesure l'exige |
| Priorité | la **rubrique Fichiers** concentre l'effort (étape 3B, 5 jours) | c'est la fonction qui justifie le logiciel |
| Historique | comparaison par **chemin complet normalisé**, suppression **manuelle** | comparaison fichier par fichier, sans hachage ni purge automatique |
| Renommages | retrait + ajout, **aucune** reconnaissance de déplacement | décision de V1, sans heuristique |
| Licence du futur projet | **MIT** (ou Apache-2.0 si publication communautaire) | compatible avec les inspirations permises, exclut toute copie GPL |

Les quatre dépôts étudiés ne couvrent **aucun** la fonction « logiciels installés /
outils de développement / projets », et **aucun** ne propose d'historique comparable
fichier par fichier : ces parties sont donc à concevoir, et c'est ce que fait ce
dossier.

Le logiciel est **local, gratuit, hors ligne, en lecture seule** sur les fichiers et sur
Windows. Il ne désinstalle rien, ne nettoie rien, n'exporte rien.

---

## Structure du dépôt

```
docs/      le dossier technique (9 livrables + index)
assets/    maquette de l'interface
```

Les seuls fichiers créés par cette étude sont documentaires. Le code source du futur
ATLAS PC n'existe pas encore : son arborescence est *proposée* dans
`docs/10-arborescence-projet.md`.
