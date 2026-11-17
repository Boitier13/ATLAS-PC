# ATLAS PC — Dossier technique préalable

Étude technique **sans programmation** pour un logiciel Windows d'inventaire local
(matériel, Windows, fichiers, logiciels, outils de développement, projets), avec
historique et comparaison d'inventaires.

**Machine cible** : Dell OptiPlex 780 · Windows 10 22H2 x64 · 6 Go de RAM · disque dur
mécanique · 1440 × 900 · hors ligne · sans abonnement · sans droits administrateur.

> Ce dépôt contient un **dossier de décision**, pas du code. Aucun fichier source n'a
> été écrit, aucun dépôt étudié n'a été modifié.

---

## Lire le dossier

| Document | Contenu |
|---|---|
| [`docs/00-lisez-moi.md`](docs/00-lisez-moi.md) | Sommaire, méthode, **synthèse et décision** — commencer ici |
| [`docs/01-analyse-neotreescan.md`](docs/01-analyse-neotreescan.md) | Analyse du dépôt NeoTreescan (Apache-2.0, C#/WPF) |
| [`docs/02-analyse-diskplot.md`](docs/02-analyse-diskplot.md) | Analyse du dépôt Diskplot (MIT, Electron/TypeScript) |
| [`docs/03-analyse-system-inspector.md`](docs/03-analyse-system-inspector.md) | Analyse du dépôt LouisMahdi System Inspector (MIT, PowerShell) |
| [`docs/04-analyse-windirstat.md`](docs/04-analyse-windirstat.md) | Analyse du dépôt WinDirStat (GPL-3.0-or-later, C++) |
| [`docs/05-tableau-comparatif.md`](docs/05-tableau-comparatif.md) | Comparaison technique, fonctionnelle et notation pondérée |
| [`docs/06-licences-et-risques.md`](docs/06-licences-et-risques.md) | Licences, compatibilité, inspiration vs copie |
| [`docs/07-architecture-cible.md`](docs/07-architecture-cible.md) | Architecture recommandée et justification de chaque choix |
| [`docs/08-modele-sqlite.md`](docs/08-modele-sqlite.md) | Schéma de base, requêtes de comparaison, rétention |
| [`docs/09-interface.md`](docs/09-interface.md) | Maquette et spécification des huit rubriques |
| [`docs/10-arborescence-projet.md`](docs/10-arborescence-projet.md) | Organisation prévue du futur code source |
| [`docs/11-plan-de-developpement.md`](docs/11-plan-de-developpement.md) | Jalons, tests, critères de validation |
| [`docs/12-risques-et-limites.md`](docs/12-risques-et-limites.md) | Risques, limites, points à vérifier |
| [`docs/13-registre-inspiration.md`](docs/13-registre-inspiration.md) | Registre à tenir pendant le développement |

Maquette indicative : [`assets/maquette-atlas-pc.png`](assets/maquette-atlas-pc.png)

---

## Décision recommandée en bref

| Sujet | Choix | Raison dominante |
|---|---|---|
| Runtime | **.NET Framework 4.8** (`net48`), projets au format SDK | présent dans Windows 10 22H2, aucun runtime à installer, démarrage à froid rapide sur disque mécanique |
| Interface | **WPF**, thème clair sobre, `DataGrid` virtualisé | mature, DPI-aware, pas de dépendance graphique lourde |
| Base | **SQLite** | fichier unique, comparaison en SQL, aucun service |
| Scan | **un seul travailleur**, énumération en flux, écriture par lots | disque mécanique bi-cœur, mémoire bornée |
| Historique | **modèle relationnel normalisé** par inventaire | comparaison fichier par fichier, sans recopier le contenu |
| Licence du futur projet | **MIT** (ou Apache-2.0 si publication communautaire) | compatible avec les inspirations permises, exclut toute copie GPL |

Les quatre dépôts étudiés ne couvrent **aucun** la fonction « logiciels installés /
outils de développement / projets », et **aucun** ne propose d'historique comparable
fichier par fichier : ces parties sont donc à concevoir, et c'est ce que fait ce
dossier.

---

## Structure du dépôt

```
docs/      le dossier technique (9 livrables + index + registre)
assets/    maquette de l'interface
```

Les seuls fichiers créés par cette étude sont documentaires. Le code source du futur
ATLAS PC n'existe pas encore : son arborescence est *proposée* dans
`docs/10-arborescence-projet.md`.
