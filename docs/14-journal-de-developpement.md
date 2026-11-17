# Journal de développement

Une entrée par étape du plan (`11-plan-de-developpement.md`). Chaque entrée dit : ce qui
a été écrit, ce qui a été décidé pendant l'écriture, **ce qui reste à vérifier sur la
machine cible**, et ce qu'il faut faire à la main.

> **Avertissement de méthode.** Le développement est écrit sur une machine Linux. Un
> projet WPF en `net48` ne se compile et ne s'exécute que sous Windows : **rien n'est
> compilé ni lancé ici**. Les vérifications réellement effectuées ici sont listées ; les
> autres sont explicitement à faire de votre côté.
>
> **Retour du PC (17/11)** : compilation **0 erreur, 0 avertissement** (MSBuild 17.14.60).
> Un défaut de liaison empêchait le lancement, corrigé au § 1.5. La recette sur
> Windows 10 a ensuite été **entièrement réussie** : l'étape 1 est **validée** (§ 1.6).

---

## Étape 1 — Application et interface de base — **validée le 17/11 sur Windows 10**

### 1.1 Ce qui a été écrit

| Élément | Fichier(s) |
|---|---|
| Solution et projet unique | `ATLAS-PC.sln`, `src/AtlasPc/AtlasPc.csproj` (`net48`, C# 7.3, `UseWPF`, un seul paquet) |
| Manifeste et configuration | `app.manifest` (`asInvoker`, `PerMonitorV2`, `longPathAware`), `app.config` (runtime 4.8, chemins longs, redirection SQLite) |
| Démarrage | `App.xaml`, `App.xaml.cs` : ouverture de la base, proposition de renommage si elle est illisible, gestionnaire d'exception globale |
| Schéma | `Data/Schema.sql` : **les 10 tables et 14 index**, idempotent |
| Accès aux données | `Data/Database.cs` (PRAGMA, intégrité, schéma), `Data/SqlDate.cs`, `Data/InventoryStore.cs` (créer, lister, clôturer, supprimer) |
| Modèle | `Models/Inventory.cs` (statuts, formats français) |
| Thème | `Themes/Theme.xaml` : palette avec nuances, typographie, boutons, badges d'état, tableau, barre latérale |
| Coquille | `Views/ShellWindow.xaml(.cs)` : 7 rubriques, badge d'état, bandeau de progression, contenu |
| Tableau de bord | `Views/DashboardView.xaml(.cs)` : 4 cartes, liste des inventaires, état vide |
| Rubriques à venir | `Views/PlaceholderView.xaml(.cs)` : annonce l'étape, **n'affiche aucune donnée simulée** |
| Contrôles | `Controls/StatCard.xaml(.cs)` |
| Infrastructure MVVM | `ViewModels/ObservableObject.cs`, `RelayCommand.cs`, `NavigationItem.cs`, `PlaceholderViewModel.cs`, `ShellViewModel.cs` |
| Dialogues | `Shell/Dialogs.cs` : confirmation de suppression, erreur, proposition de recréer la base |

Total : 15 fichiers C#, 6 fichiers XAML, 1 fichier SQL, 2 fichiers de configuration.

### 1.2 Décisions prises pendant l'écriture

| Décision | Motif |
|---|---|
| **Le schéma complet est créé dès maintenant** | évite toute migration, que le dossier refuse ; une table vide ne coûte rien et le script est idempotent |
| **Statut `running` ajouté** aux deux `CHECK` (`inventory`, `scan_root`) | une ligne doit exister **pendant** le parcours ; le schéma n'autorisait que des états finaux. `running` n'étant ni `complete` ni `partial`, il ne peut produire aucune suppression confirmée : la règle de comparaison n'est pas modifiée |
| **Les lignes `running` sont supprimées au démarrage suivant** (`DeleteAbandoned`) | un inventaire interrompu par un arrêt brutal ne contient rien d'exploitable et ne doit pas apparaître dans l'historique |
| **Bouton « Lancer un inventaire » = simulation explicite** | à l'étape 1, aucun disque n'est lu. La ligne est créée, puis clôturée en `cancelled` avec la note « Simulation de l'étape 1 : aucun disque n'a été parcouru ». Aucune donnée fausse ne peut donc être prise pour un vrai inventaire |
| **`Shell/Dialogs.cs`** plutôt que des `MessageBox` dans les ViewModels | la règle « pas de WPF dans les ViewModels » reste tenue ; `Shell` devient la seule exception, et la règle du livrable 4 a été mise à jour |
| **Un seul `PlaceholderView` pour les six rubriques à venir** | six vues vides identiques seraient du bruit ; chacune sera écrite à son étape et le remplacement disparaîtra à l'étape 5 |
| **Géométries `PathGeometry`** plutôt que `StreamGeometry` à contenu texte | syntaxe XAML documentée, aucune dépendance à un comportement de convertisseur |
| **Pas de projet de tests à cette étape** | il n'y a encore aucune logique à tester ; les premiers tests arrivent à l'étape 3A, comme prévu au livrable 8 |

### 1.3 Ce qui a été vérifié ici (Linux)

| Vérification | Résultat |
|---|---|
| `Schema.sql` exécuté sur une base SQLite réelle | **10 tables, 14 index**, exécution deux fois de suite sans erreur (idempotence) |
| Contraintes `CHECK` | un statut inventé est bien refusé |
| Cascade | supprimer un inventaire supprime bien ses `scan_root` |
| Instructions SQL du `InventoryStore` | insertion, lecture, clôture : syntaxe et résultats validés |
| Fichiers XAML | les 6 fichiers sont du XML bien formé |
| Clés de ressources | 70 clés définies, **aucune référence manquante** |
| C# | accolades et parenthèses équilibrées ; aucun élément de syntaxe postérieur à C# 7.3 (`??=`, `using var`, `record`, `is not`, expression `switch`, `new()`) |
| `x:Class` | chaque vue a son code-behind, espace de noms cohérent |

**Ce qui n'a pas pu être vérifié ici** : compilation, exécution, rendu visuel, démarrage
à froid, mémoire. Compilation, exécution, navigation et persistance ont depuis été
validées sur Windows 10 (§ 1.6). Deux mesures restent à relever : démarrage à froid et
mémoire au repos.

### 1.4 À faire sur le PC (Visual Studio 2022)

**État au 17/11 : protocole appliqué, recette réussie** — voir les résultats au § 1.6.
Les points 1, 2 et 5 du tableau ci-dessous sont confirmés ; les points 3 (démarrage à
froid) et 4 (mémoire au repos) n'ont pas été mesurés.

1. Ouvrir `ATLAS-PC.sln`. Si Visual Studio signale que le projet n'est pas chargé :
   vérifier que la charge de travail **« Développement .NET Desktop »** est installée.
2. Vérifier que le **.NET Framework 4.8 Developer Pack** est présent (il l'est avec
   Visual Studio 2022 à jour).
3. `Générer > Générer la solution` (`Ctrl+Maj+B`).
4. Lancer (`F5`).

Puis renseigner ces cinq points, qui décident de la suite :

| # | À observer | Attendu | Si différent |
|---|---|---|---|
| 1 | Le projet se compile | 0 erreur | m'envoyer la liste des erreurs, avec le fichier et la ligne |
| 2 | La fenêtre s'ouvre | 7 entrées, tableau de bord, aucune exception | m'envoyer le message exact |
| 3 | Démarrage à froid (5 essais après redémarrage) | **< 3 s** | à consigner : c'est le point de décision du plan |
| 4 | Mémoire au repos | **< 130 Mo** | idem |
| 5 | `Lancer un inventaire` | bandeau, progression, puis une ligne `Annulé` avec sa note ; « Supprimer » demande confirmation | décrire ce qui se passe |

Et deux vérifications de forme :

- `%LOCALAPPDATA%\ATLAS PC\atlas.db` existe après le premier lancement ;
- aucun fichier n'est créé ailleurs (à confirmer plus tard au moniteur de processus).

### 1.5 Correction du lancement — liaison TwoWay sur `ProgressPercent`

**Symptôme.** Compilation sans erreur ni avertissement, puis au lancement :

> « Une liaison TwoWay ou OneWayToSource ne peut pas fonctionner sur la propriété en
> lecture seule ProgressPercent de type AtlasPc.ViewModels.ShellViewModel. »

**Contrôle fautif.** `Views/ShellWindow.xaml`, bandeau de progression :

```xml
<ProgressBar Minimum="0" Maximum="100" Value="{Binding ProgressPercent}" ... />
```

**Cause exacte.** `ProgressBar` hérite de `RangeBase`, dont la propriété `Value` est
enregistrée avec `BindsTwoWayByDefault` : toute liaison y est `TwoWay` **si le mode n'est
pas écrit**. `ProgressPercent` est en lecture seule (`private set`), ce qui est voulu :
c'est le parcours qui la met à jour, jamais l'interface. WPF refuse donc la liaison et
lève une exception au chargement de la fenêtre.

**Correction.** `Value="{Binding ProgressPercent, Mode=OneWay}"`, avec un commentaire sur
place. Aucune autre modification : le ViewModel garde sa propriété en lecture seule, le
reste de l'étape 1 est inchangé.

**Pourquoi ce défaut n'apparaissait qu'ici.** `TextBlock.Text` est `OneWay` par défaut :
les treize autres liaisons vers des propriétés en lecture seule (`CurrentTitle`,
`StatusText`, `FilesText`, `LatestInventory`…) ne posaient aucun problème. Seule une
**cible** à mode par défaut `TwoWay` pouvait échouer.

**Audit des 52 liaisons de l'application** (fait après correction) :

| Contrôle à mode `TwoWay` par défaut | Liaison | Verdict |
|---|---|---|
| `ListBox` / `Selector.SelectedItem` | `SelectedSection` | source avec setter public → `Mode=TwoWay` conservé |
| `ProgressBar.Value` | `ProgressPercent` | source en lecture seule → **corrigé en `Mode=OneWay`** |
| `TextBox.Text`, `ToggleButton.IsChecked`, `Slider.Value`, `ComboBox`, `DatePicker` | — | absents du XAML à ce stade |

**Règle ajoutée** à `07-architecture-cible.md` § 3 : sur une cible à mode par défaut
`TwoWay`, le mode est toujours écrit explicitement — `TwoWay` seulement si la propriété
source a un setter public. Elle s'appliquera notamment à la zone de recherche de
l'étape 3B (`TextBox.Text`), où la saisie sera liée en `TwoWay` à une propriété
modifiable.

**Vérifié sur le PC le 17/11** : la fenêtre s'ouvre, la progression s'affiche de 0 à
100 % puis le bandeau disparaît (§ 1.6). Aucune autre erreur de liaison.

### 1.6 Recette sur Windows 10 — 17/11 : **étape 1 validée**

Environnement : Windows 10, Visual Studio 2022, **MSBuild 17.14.60**, solution
`ATLAS-PC.sln` de la branche `arena/f4389f9f-atlas-pc` au commit `2c15ca3`.

| # | Point de recette | Résultat observé | Verdict |
|---|---|---|---|
| 1 | Compilation de `ATLAS-PC.sln` | réussie, **0 avertissement, 0 erreur** | conforme |
| 2 | Ouverture de l'application | aucune erreur, aucune exception | conforme |
| 3 | Barre latérale | les **7 rubriques** s'affichent | conforme |
| 4 | Navigation | les 7 rubriques se chargent et se quittent normalement | conforme |
| 5 | « Lancer un inventaire » | la simulation démarre, bandeau de progression visible | conforme |
| 6 | Progression | barre et bouton « Annuler » affichés, progression puis disparition | conforme |
| 7 | Historique | une ligne par essai, statut **Annulé**, 0 fichier, 0 octet, note de simulation | conforme |
| 8 | Persistance | fermeture puis redémarrage : l'historique est toujours là | conforme |
| 9 | Base de données | `%LOCALAPPDATA%\ATLAS PC\atlas.db` créée, **4 Ko → 112 Ko** | conforme |

**Ce que ce point 7 démontre.** La simulation n'écrit aucune donnée de fichier : la ligne
est créée avec `roots_count = 0`, aucune `scan_root` n'est insérée, les compteurs restent
à zéro, le statut est `cancelled` et la note dit explicitement qu'aucun disque n'a été
parcouru — ou, si l'utilisateur a cliqué « Annuler », qu'il a annulé la simulation.
Aucune ligne de l'historique ne peut donc être prise pour un inventaire réel.

**Ce que le point 8 démontre.** Trois mécanismes sont validés d'un coup : le chemin de la
base, l'idempotence du script de schéma (aucune erreur au second démarrage), et le
nettoyage `DeleteAbandoned` — qui supprime les lignes `running` mais **conserve** les
lignes `cancelled`. C'est le comportement voulu : un inventaire annulé par l'utilisateur
est un fait historique, pas un déchet.

**Ce que le point 9 démontre.** Une base neuve pèse 112 Ko alors qu'elle ne contient que
des lignes d'inventaire. C'est normal et attendu : les 10 tables et 14 index occupent
chacun au moins une page de 4 Ko, et le mode WAL ajoute un journal. **112 Ko est donc le
plancher d'une base ATLAS PC**, quelle que soit la machine. Cela ne change pas l'estimation
du plan (85–110 Mo pour 400 000 fichiers) : ce volume est dominé par les lignes, pas par
le squelette.

**Deux mesures du protocole n'ont pas été communiquées :**

| Mesure | Seuil | État |
|---|---|---|
| Démarrage à froid (5 essais après redémarrage) | < 3 s | **non relevé** — non bloquant, à faire au prochain lancement |
| Mémoire au repos | < 130 Mo | **non relevé** — non bloquant, à faire au prochain lancement |

Elles ne remettent pas en cause la validation fonctionnelle, mais le plan en a fait un
point de décision (au-delà de 5 s ou 200 Mo, le choix de WPF est réexaminé). Elles seront
relevées avant l'étape 3A, sans recompilation : Gestionnaire des tâches au repos, et
chronomètre du double-clic à la fenêtre affichée.

**Décision.** Étape 1 **clôturée** le 17/11. Le code de ce jalon est gelé au commit
`2c15ca3`, correctif de liaison inclus. Aucune modification du code n'a été faite depuis.

### 1.7 Ce qui n'est pas encore là, volontairement

- les six rubriques affichent un texte d'attente, sans données ;
- les quatre cartes du tableau de bord affichent `—` ;
- aucune lecture de disque, de registre ou WMI ;
- pas encore de projet de tests.

C'est conforme au critère de l'étape 1 : *une application qui démarre, se navigue, et
parle à sa base*.
