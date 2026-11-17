# Livrable 3 — Analyse des licences et des risques de réutilisation

> Ce document est une analyse technique et documentaire. Il ne constitue pas un
> avis juridique. Pour une distribution commerciale, une revue par un juriste est
> recommandée.

---

## 1. Les quatre licences, telles qu'elles figurent dans les dépôts

| Dépôt | Fichier | Licence exacte | Portée vérifiée |
|---|---|---|---|
| NeoTreescan | `LICENSE` | **Apache License 2.0** | `NOTICE` présent ; `CHANGELOG.md` mentionne un passage **MIT → Apache-2.0** dans `[Unreleased]` |
| Diskplot | `LICENSE` | **MIT** | `Copyright (c) 2026 Rodrigo Sanguanini` ; `package.json` déclare `"license": "MIT"` |
| System Inspector | `LICENSE` | **MIT** | `Copyright (c) 2026 LouisMahdi` |
| WinDirStat | `LICENSE.md` | **GNU GPL v3** | En-tête `SPDX-License-Identifier: GPL-3.0-or-later` sur **174 fichiers** source |

### Licences des composants tiers, vérifiées

| Composant | Utilisé par | Licence | Vérification |
|---|---|---|---|
| ClosedXML 0.105.0 | NeoTreescan | MIT | `NOTICE` du dépôt |
| CommunityToolkit.Mvvm 8.4.2 | NeoTreescan | MIT | `NOTICE` du dépôt |
| .NET / WPF | NeoTreescan | MIT | `NOTICE` du dépôt |
| Electron | Diskplot | MIT | `registry.npmjs.org/electron/latest` |
| React | Diskplot | MIT | `registry.npmjs.org/react/latest` |
| koffi | Diskplot | MIT | `registry.npmjs.org/koffi/latest` |
| Windows PowerShell / .NET Framework | System Inspector | composants Windows | `docs/COMPATIBILITY.md` |
| xxHash | WinDirStat | téléchargé au build | `README.md` § Building |

---

## 2. Ce que chaque licence autorise réellement

### 2.1 MIT (Diskplot, System Inspector)

**Autorise** : utiliser, copier, modifier, fusionner, publier, distribuer,
sous-licencier et vendre, sans restriction, y compris dans un logiciel propriétaire.

**Impose** : inclure la notice de copyright et le texte de licence dans « toutes les
copies ou parties substantielles du logiciel ».

**N'impose pas** : de publier vos modifications, ni de placer votre logiciel sous MIT.

**Risque résiduel** : aucun clause de brevets. En pratique sans importance ici.

### 2.2 Apache-2.0 (NeoTreescan)

**Autorise** : les mêmes libertés que MIT, y compris l'usage dans un produit
propriétaire.

**Impose** :
- conserver `LICENSE` et le contenu de `NOTICE` ;
- **signaler les fichiers modifiés** ;
- conserver les mentions d'attribution et les clauses de garantie ;
- en cas de dépôt d'un brevet sur une contribution, la concession de brevets
  (section 3) s'applique et prend fin en cas de litige en brevets.

**Nuance importante** : la version examinée est Apache-2.0, mais le même projet a été
publié sous MIT auparavant. Si l'on réutilisait du code d'une version antérieure au
changement, c'est la licence de **cette version** qui s'appliquerait. En pratique :
citer le commit d'origine.

**Nuance sur les marques** : la section 6 exclut explicitement l'usage des marques du
concédant. « NeoTreescan » et « Neortal » ne doivent pas apparaître dans ATLAS PC.

### 2.3 GPL-3.0-or-later (WinDirStat)

**Autorise** : tout, y compris l'usage commercial — **à condition** que l'œuvre
distribuée reste sous GPL-3.0 et que le code source correspondant soit fourni.

**Impose en cas de distribution** :
- licence GPL-3.0 (ou ultérieure) sur l'ensemble dérivé ;
- mise à disposition du code source complet de l'ensemble ;
- conservation des mentions et de la licence ;
- interdiction d'ajouter des restrictions supplémentaires (DRM, clauses
  propriétaires) sur le logiciel distribué.

**Ce qui déclenche l'obligation** : la **distribution** (« conveyance ») d'un travail
fondé sur le code. Un usage strictement privé, sans diffusion, ne déclenche pas
l'obligation de publication.

**Conséquence décisive** : la GPL-3.0 est **incompatible** avec une distribution
d'ATLAS PC sous MIT ou Apache-2.0, et *a fortiori* avec une distribution propriétaire.

---

## 3. Matrice de compatibilité pour ATLAS PC

| Code entrant ↓ / Licence d'ATLAS PC → | MIT | Apache-2.0 | Propriétaire | GPL-3.0 |
|---|---|---|---|---|
| MIT (Diskplot, System Inspector) | ✅ | ✅ | ✅ | ✅ |
| Apache-2.0 (NeoTreescan) | ✅ | ✅ | ✅ (avec `NOTICE`) | ✅ |
| GPL-3.0 (WinDirStat) | ❌ | ❌ | ❌ | ✅ (tout le projet passe en GPL) |
| SQLite (domaine public) | ✅ | ✅ | ✅ | ✅ |
| System.Data.SQLite.Core (domaine public) | ✅ | ✅ | ✅ | ✅ |
| Microsoft.Data.Sqlite (MIT, solution de repli) | ✅ | ✅ | ✅ | ✅ |

Rappel du sens de compatibilité : **MIT et Apache-2.0 peuvent entrer dans un projet
GPL** (le projet devient GPL), **l'inverse est impossible**.

---

## 4. Inspiration technique ou copie de code : où est la frontière

### 4.1 Ce qui n'est pas protégeable

- **Les idées, méthodes, algorithmes et fonctionnalités.** Utiliser
  `FindFirstFileExW` avec `FIND_FIRST_EX_LARGE_FETCH`, faire une passe arrière pour
  cumuler les tailles, borner un treemap par les pixels, distinguer « stop » et
  « cancel » : ce sont des idées, libres.
- **Les interfaces de programmation Windows** et leurs paramètres.
- **Les formats de données** et les schémas de base.
- **Les données factuelles** (une taille de fichier, une version).

### 4.2 Ce qui est protégeable

- **Le code source** : sa forme d'expression, ligne à ligne.
- **Les commentaires** originaux, les messages d'erreur rédigés, la documentation.
- **La structure originale** d'un ensemble de fichiers, dans une certaine mesure.
- **Les noms de produits, logos, icônes.**

### 4.3 Les trois seuils de risque, en pratique

| Situation | Qualification | Risque |
|---|---|---|
| Lire le code, comprendre l'algorithme, réécrire une implémentation différente à partir d'une spécification publique | Inspiration licite | **Aucun** |
| Reproduire une structure de fichiers et des signatures très proches, avec des commentaires retirés | Zone grise ; peut être qualifié d'œuvre dérivée | **Moyen** |
| Coller un fichier, une classe, une fonction de 30 lignes, ou traduire ligne à ligne | Copie | **Certain** (MIT/Apache : attribution obligatoire ; GPL : tout le projet contamine) |

### 4.4 Le cas particulier de la traduction

Traduire du TypeScript en C# ou du PowerShell en C# **reste une adaptation** au sens
du droit d'auteur : la traduction d'une œuvre est un acte de reproduction
« sous une autre forme ». Une réécriture indépendante, à partir de la documentation
Microsoft des API concernées, n'en est pas une. La différence n'est pas dans le
résultat compilé, elle est dans le **processus** : d'où la règle ci-dessous.

---

## 5. Règles opérationnelles proposées pour ATLAS PC

1. **Aucun copier-coller depuis les quatre dépôts.** Ni fichier, ni fonction, ni bloc
   de commentaires. Cette règle est simple, vérifiable, et suffit.
2. **Écrire à partir des sources primaires** : documentation Microsoft
   (`FindFirstFileExW`, `GetFileInformationByHandleEx`, classes `Win32_*`),
   spécification SQLite, algorithme de treemap de Bruls/Huizing/van Wijk s'il est
   implémenté un jour.
3. **Tenir un registre d'inspiration** — un tableau de cinq lignes, à la fin de ce
   document (§ 8) : pour chaque apport, la source consultée, ce qui a été retenu
   (l'idée) et la mention « aucune ligne copiée ». C'est la preuve documentaire en cas
   de contestation, et cela protège aussi contre l'oubli.
4. **Interdiction formelle d'ouvrir le code de WinDirStat** pour autre chose que la
   lecture d'idées, et jamais pendant l'écriture d'un module équivalent. Un
   développement dit « en chambre blanche » (clean room) est la seule protection
   robuste : une personne décrit le comportement attendu, une autre l'implémente.
   Compte tenu de la taille du projet ATLAS PC, la règle 1 suffit, mais la règle 4
   doit être écrite.
5. **Vérifier les licences des dépendances avant ajout.** La V1 n'en a qu'une,
   `System.Data.SQLite.Core` (domaine public) : une ligne suffit dans le `README`, il
   n'y a pas de fichier de notices à maintenir.
6. **Attention aux générateurs de code et aux assistants IA** : ils peuvent
   reproduire des extraits sous licence. Tout bloc non trivial doit être relu,
   compris et, en cas de doute, réécrit. Ne pas leur fournir de code GPL comme
   contexte.
7. **Choisir la licence d'ATLAS PC tôt.** Une fois du code GPL entré, le choix est
   irréversible.

---

## 6. Recommandation de licence pour ATLAS PC

| Option | Pour | Contre | Verdict |
|---|---|---|---|
| **MIT** | Maximale simplicité, compatible avec tout, 3 paragraphes | Aucune clause de brevets, aucune protection contre la réappropriation fermée | **Recommandée** si ATLAS PC reste un outil personnel/associatif |
| **Apache-2.0** | Clause de brevets, `NOTICE`, cadre formalisé | Plus verbeux, obligation de signaler les modifications | **Recommandée** si ATLAS PC est publié sur GitHub avec une ambition communautaire |
| **GPL-3.0** | Obligation de partage des améliorations | Empêche la réutilisation par un outil fermé ; incompatible avec l'ajout ultérieur de composants propriétaires | Pertinente seulement si l'on **forke** WinDirStat |

### Le scénario « fork de WinDirStat » examiné et écarté

Techniquement, le chemin le plus court pour obtenir un excellent scan de fichiers
serait de forker WinDirStat (GPL) et d'y ajouter l'inventaire matériel, logiciel et
l'historique. Ce scénario est **écarté** pour trois raisons vérifiées dans ce dossier :

1. le socle est du **C++23 / Win32 / framework UI maison** de plusieurs dizaines de
   milliers de lignes : y ajouter sept rubriques d'interface est hors de portée d'un
   projet solo ;
2. il n'existe **ni couche de persistance interrogeable, ni historique, ni
   comparaison** : tout le cœur du cahier des charges resterait à écrire ;
3. il faudrait intégrer **SQLite en C++** et une couche matérielle/logicielle
   supplémentaire, dans une base de code dont la maintenance est déjà exigeante.

Autrement dit : le gain de licence se paierait d'un coût de développement supérieur
au projet complet en C#.

---

## 7. Synthèse des risques de réutilisation

| Risque | Probabilité | Impact | Parade |
|---|---|---|---|
| Copie involontaire d'un bloc GPL (WinDirStat) | Moyenne si lecture pendant l'écriture | **Élevé** (tout le projet contamine) | règles 1 et 4 + registre du § 8 |
| Oubli d'attribution Apache (NeoTreescan) | Faible | Moyen (non-conformité) | une entrée dans le `README` |
| Réutilisation de code d'une version MIT de NeoTreescan sans le dire | Faible | Faible si attribution | Citer le commit d'origine |
| Usage d'un nom ou logo de tiers | Faible | Moyen (marque) | Aucun élément de branding repris |
| Dépendance ultérieure sous licence incompatible | Faible | Moyen | Revue de licence avant chaque ajout NuGet |
| Code généré par un assistant reproduisant un extrait protégé | **Moyenne** | Moyen | Relecture systématique, réécriture en cas de doute |

**Conclusion** : les quatre licences autorisent l'**étude**. Trois autorisent la
**réutilisation avec attribution** (Apache-2.0, MIT, MIT). Une seule — WinDirStat —
impose de rester à distance du code. Cette asymétrie est le principal enseignement du
livrable 3 : *la meilleure référence technique est précisément celle qu'on ne peut pas
copier.*

---

## 8. Registre d'inspiration (version courte)

Tenir ce tableau à jour suffit ; il remplace le document séparé envisagé en v1.0.

| Apport retenu dans ATLAS PC | Source consultée | Ce qui est repris | Code copié |
|---|---|---|---|
| Structure d'écran, manifeste `asInvoker` + `PerMonitorV2` + long paths | NeoTreescan | idée d'organisation | non |
| Collecte dégradée : `try/catch` par requête, délai, statut explicite | System Inspector | méthode | non |
| Comparaison de deux inventaires, sens A → B, périmètre explicite | Diskplot | méthode | non |
| Comptage des zones non analysées et refus de conclure | WinDirStat (`ScanOutcome`) | idée | non |
| Parcours DFS, non-suivi des points de reparse | documentation Microsoft | API publique | non |

> **Révision du 17 novembre 2026.** Le document `docs/13-registre-inspiration.md` est
> supprimé : le tableau ci-dessus couvre le besoin. Les références à OxyPlot, aux
> exports et au treemap ont été retirées, ces éléments étant hors périmètre.
