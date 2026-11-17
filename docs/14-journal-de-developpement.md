# Journal de développement

Une entrée par étape du plan (`11-plan-de-developpement.md`). Chaque entrée dit : ce qui
a été écrit, ce qui a été décidé pendant l'écriture, **ce qui reste à vérifier sur la
machine cible**, et ce qu'il faut faire à la main.

> **Avertissement de méthode.** Le développement est écrit sur une machine Linux. Un
> projet WPF en `net48` ne se compile et ne s'exécute que sous Windows : **rien n'est
> compilé ni lancé ici**. Les vérifications réellement effectuées ici sont listées ; les
> autres sont explicitement à faire de votre côté.
>
> **Premier retour du PC (17/11)** : la compilation de l'étape 1 réussit, **0 erreur et
> 0 avertissement**. Un défaut de liaison empêchait le lancement ; il est corrigé au
> § 1.5. Une compilation propre ne vaut pas validation du rendu : les points du § 1.4
> restent à observer.

---

## Étape 1 — Application et interface de base (en cours)

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
à froid, mémoire. La compilation a depuis été validée sur le PC (§ 1.4) ; l'exécution
reste à confirmer. Ce sont les critères de sortie de l'étape 1.

### 1.4 À faire sur le PC (Visual Studio 2022)

**État au 17/11** : étapes 1 et 2 ci-dessous **faites** — compilation réussie, sans erreur
ni avertissement. Le lancement échouait sur la liaison du § 1.5, désormais corrigée.
Restent les points 2 à 5 du tableau de contrôle.

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

**Reste à vérifier sur le PC** : les cinq points du § 1.4, plus la valeur affichée dans
le bandeau (elle doit passer de 0 à 100 % puis disparaître).

### 1.6 Ce qui n'est pas encore là, volontairement

- les six rubriques affichent un texte d'attente, sans données ;
- les quatre cartes du tableau de bord affichent `—` ;
- aucune lecture de disque, de registre ou WMI ;
- pas encore de projet de tests.

C'est conforme au critère de l'étape 1 : *une application qui démarre, se navigue, et
parle à sa base*.
