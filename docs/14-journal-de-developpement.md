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
> L'**étape 2** est écrite, corrigée puis **validée sur Windows 10 le 17/11** (§ 2.8) :
> compilation, collecte WMI, affichage de la mémoire et persistance confirmés. Le code du
> jalon est gelé au commit `4ffe89f`.
> L'**étape 3A** est commencée : sa **première partie** — stockage et moteur, sans
> interface — est écrite (§ 3). Elle attend sa compilation et ses tests sur le PC.

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
| **Les lignes `running` sont closes au démarrage suivant** (`DeleteAbandoned`, devenu `AbandonRunning` à l'étape 3A) | un inventaire interrompu ne doit pas apparaître comme en cours. Depuis l'étape 3A il n'est plus supprimé mais marqué `cancelled` : l'historique est conservé (voir § 3.3) |
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
nettoyage des lignes `running` — qui les écarte du démarrage mais **conserve** les lignes
`cancelled`. C'est le comportement voulu : un inventaire annulé par l'utilisateur est un
fait historique, pas un déchet. Depuis l'étape 3A ce nettoyage ne supprime plus rien
(§ 3.3).

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

---

## Étape 2 — Inventaire matériel et Windows — **validée le 17/11 sur Windows 10**

### 2.1 Ce qui a été écrit

| Élément | Fichier(s) |
|---|---|
| Collecte | `Collecting/ComputerCollector.cs` : **10 appels WMI** (8 classes + 2 associations) et la lecture du registre |
| Modèles | `Models/ComputerInfo.cs`, `Models/VolumeInfo.cs` (taux d'occupation, niveaux de couleur) |
| Écriture | `Data/InventoryStore.cs` : `SaveHardware`, `GetComputer`, `GetVolumes` |
| Schéma | `Data/Schema.sql` v2 (+ `cpu_threads`, + `ram_usable_bytes`) et `Database.EnsureColumn` |
| Vue | `Views/ComputerView.xaml(.cs)` : 6 cartes, tableau des volumes, note de collecte, état vide |
| ViewModel | `ViewModels/ComputerViewModel.cs` |
| Thème | `Themes/Theme.xaml` : 3 icônes (`IconGpu`, `IconWindows`, `IconBios`) et le style `VolumeBar` |
| Coquille | `ShellViewModel` : collecte réelle, résumé du tableau de bord, barre indéterminée ; `ShellWindow` (barre) ; `DashboardView` (3 cartes renseignées) ; `App.xaml` (gabarit de vue) |
| Projet | `<Reference Include="System.Management" />` |

Six fichiers créés, neuf modifiés, deux colonnes ajoutées à `computer` — aucune autre
table, aucune autre colonne.

### 2.2 Les dix arbitrages, tels qu'appliqués

| # | Décision retenue | Mise en œuvre |
|---|---|---|
| C1 | le bouton « Lancer un inventaire » collecte **réellement** | plus aucune étape simulée : `SimulationSteps` est supprimée |
| C2 | `Task.Run` | la collecte ne bloque pas le thread d'interface |
| C3 | cœurs physiques, threads à part | deux colonnes, deux textes : « 6 cœurs · 12 threads » |
| C4 | mémoire installée seule dans la valeur ; utilisable à part | si elle manque : « mémoire utilisable non lue », jamais de substitution |
| C5 | noms multiples concaténés | `Intel UHD 630 · NVIDIA GTX 1650`, avec le nombre de cartes |
| C6 | volumes fixes uniquement | filtre `DriveType = 3` **dans la requête**, exclusions écrites sous le tableau |
| C7 | nom de Windows corrigé par le registre | build ≥ 22000 → « Windows 11 » même si `ProductName` dit « 10 » |
| C8 | `InventoryStore` conservé | 3 méthodes ajoutées, `Data` reste à 9 fichiers |
| C9 | incidents écrits dans `inventory.note` | note construite par `BuildNote`, visible après redémarrage |
| C10 | budget de 30 s **garanti côté interface** | 10 s par appel + budget interne 30 s + `Task.WhenAny` à 35 s |

### 2.3 Les corrections demandées, et ce qu'elles ont changé

**1. Collecte réelle, statut cohérent.** L'inventaire est créé en `running`, le matériel
est écrit, puis la ligne est clôturée en **`partial`** : le matériel est lu, les fichiers
ne le sont pas. La note le dit mot pour mot (« Fichiers non inventoriés : le parcours des
disques arrive à l'étape 3A »), la vue Ordinateur affiche un bandeau ambre « Collecte
partielle — à lire », et le tableau de bord garde « Fichiers — » avec l'explication. Le
badge d'état affiche donc « Partiel », jamais « Complet ».

**2. Mémoire installée ≠ mémoire utilisable.** Deux colonnes (`ram_bytes`,
`ram_usable_bytes`), deux provenances, deux textes. La provenance exacte a été corrigée au
§ 2.7 : la mémoire installée vient de la **somme des modules**, pas de
`TotalPhysicalMemory`.

**3. Décompte des requêtes WMI clarifié.** Le plan annonçait sept requêtes. Il y en avait
**neuf** — sept classes plus les deux associations qui rattachent un volume à son disque
physique — puis **dix** après la correction de la mémoire (§ 2.7). Le code, la note
d'inventaire, `docs/07` § 7.1 et `docs/11` § 3 emploient le même décompte.

**4. Dates d'installation.** `Win32_OperatingSystem.InstallDate` (date CIM, convertie
depuis l'heure locale vers UTC) désigne l'**image Windows courante** : dernière
installation ou mise à niveau. `CurrentVersion\InstallDate` (DWORD, secondes Unix)
désigne l'installation d'**origine**. ATLAS PC retient le registre, ne se rabat sur WMI
qu'en dernier recours, et l'écrit alors dans la note. La vue précise l'origine de la date
au lieu de présenter les deux comme équivalentes.

### 2.4 Ce qui a été vérifié ici (Linux)

| Vérification | Résultat |
|---|---|
| `Schema.sql` v2 exécuté sur SQLite | 10 tables, colonnes `cpu_threads` et `ram_usable_bytes` présentes |
| **Chemin de mise à niveau v1 → v2** | base sans les deux colonnes, `ALTER TABLE` appliqué, insertion complète ensuite : **une base créée à l'étape 1 n'a pas besoin d'être recréée** |
| Instructions SQL du matériel | `INSERT` computer/volume, `SELECT` de `GetComputer`/`GetVolumes`, `NULL` tolérés : validés |
| Taux d'occupation | 499 000 000 000 / 210 000 000 000 → **57,9 %**, dans la classe « low » |
| XAML | 7 fichiers bien formés ; 74 clés de ressources, **aucune référence manquante** |
| Gabarits | les 3 `DataTemplate` pointent vers des classes existantes |
| C# | 20 fichiers, accolades équilibrées, aucun élément postérieur à C# 7.3 |
| Liaisons | 3 cibles à mode `TwoWay` par défaut, **toutes en mode explicite** : `ProgressBar.Value` (`ProgressPercent`) et `PercentValue` en `OneWay`, `ListBox.SelectedItem` en `TwoWay` |

**Ce qui n'a pas pu être vérifié ici** : compilation, appels WMI réels, rendu, durée de
collecte. Aucun Windows dans cet environnement.

### 2.5 À faire sur le PC — guide de recette

1. `Générer la solution`, puis `F5`.
2. Cliquer **« Lancer un inventaire »** et chronométrer : du clic à l'affichage de la
   fiche. Attendu **< 15 s** (1 à 3 s en pratique).
3. Ouvrir la rubrique **Ordinateur** et comparer avec `msinfo32` :
   - fabricant, modèle, BIOS (éditeur + version) ;
   - processeur, cœurs et threads ;
   - mémoire **installée** (et la mémoire utilisable, qui doit être plus petite) ;
   - Windows : nom, version, build, architecture.
4. Comparer la date d'installation avec `msinfo32` (« Date d'installation ») : elle doit
   correspondre à l'installation d'origine, pas à la dernière mise à jour.
5. Comparer le tableau **Disques et volumes** avec l'Explorateur : capacité, libre, %.
   La barre doit être teal sous 70 %, ambre de 70 à 85 %, rouge au-delà.
6. Revenir au **tableau de bord** : les cartes Processeur, Mémoire et Stockage sont
   renseignées ; Fichiers reste à `—`.
7. Vérifier l'historique : la ligne est en **« Partiel »**, 0 fichier, 0 octet, avec une
   note qui commence par « Matériel et Windows : 10/10 appels WMI ».
8. Fermer, rouvrir : la fiche Ordinateur est toujours là (elle vient de la base, aucune
   collecte n'est refaite au démarrage — la durée d'ouverture ne doit pas changer).

| # | À observer | Attendu | Si différent |
|---|---|---|---|
| 1 | Compilation | 0 erreur, 0 avertissement | m'envoyer fichier + ligne |
| 2 | Collecte | < 15 s, barre indéterminée puis fiche | noter la durée réelle |
| 3 | Valeurs | identiques à `msinfo32` / `dxdiag` | envoyer une capture |
| 4 | Statut | « Partiel », note présente | copier la note |
| 5 | Élévation | aucune fenêtre UAC | signaler |
| 6 | Persistance | fiche identique après redémarrage | signaler |

### 2.6 Correction de compilation — le constructeur du chercheur WMI

**Erreur relevée sur le PC** (MSBuild 17.14.60), au commit `99f3f9c` :

```
ComputerCollector.cs(269,68): error CS1503 : conversion impossible de System.Management.ObjectQuery en string
ComputerCollector.cs(269,90): error CS1503 : conversion impossible de System.Management.EnumerationOptions en string
```

**Ligne fautive** (`Run`, l'appel unique qui sert à toutes les requêtes) :

```csharp
using (var searcher = new ManagementObjectSearcher(new ObjectQuery(wql), options))
```

**Cause exacte.** `ManagementObjectSearcher` n'a pas de constructeur
`(ObjectQuery, EnumerationOptions)`. Le compilateur a donc tenté la seule surcharge à deux
paramètres acceptant un `ObjectQuery` — `(string queryString, …)` — d'où les deux
conversions impossibles. Les sept constructeurs réels, vérifiés dans la source de
référence de `System.Management` :

| Constructeur | Options de délai ? |
|---|---|
| `()` · `(string)` · `(ObjectQuery)` | non |
| `(string scope, string queryString)` | non |
| `(ManagementScope, ObjectQuery)` | non |
| **(string scope, string queryString, EnumerationOptions)** | **oui** |
| **(ManagementScope, ObjectQuery, EnumerationOptions)** | **oui** |

**Correction.** La surcharge `(scope, requête, options)`, avec l'espace de noms local dans
une constante explicite :

```csharp
private const string ScopePath = @"\\.\root\cimv2";
...
using (var searcher = new ManagementObjectSearcher(ScopePath, wql, options))
```

C'est un appel unique : toutes les requêtes passent par `Run`, elles sont donc toutes
corrigées d'un coup. Aucun autre `ManagementObjectSearcher`, `ObjectQuery` ou
`ManagementScope` n'existe dans le code (vérifié par recherche globale).

**Ce qui n'a pas été touché.** Les protections demandées restent en place :

- `EnumerationOptions { Timeout = 10 s, ReturnImmediately = true }` : conservé tel quel ;
- budget interne de 30 s (`watch.Elapsed > QueryBudget`) : conservé ;
- un `try/catch` par appel, qui capture aussi un dépassement de délai survenant pendant
  l'énumération (l'exception est levée dans la boucle, elle remonte au `catch`) ;
- libération : `using` sur le chercheur **et** sur la collection de résultats. Le
  `ManagementScope` créé en interne par cette surcharge n'est **pas** libérable — la
  classe n'implémente pas `IDisposable` (vérifié dans la source), il n'y a donc rien à
  libérer à la main de ce côté.

**Portée de l'information.** Le compilateur n'a signalé que ces deux erreurs, dans un seul
appel : le reste du code de l'étape 2 (modèles, view-model, vue, magasin) passe donc la
vérification de types. La compilation complète et la recette restent à faire sur le PC.

### 2.7 Correction de la mémoire — installée, utilisable, et la dixième requête

**Anomalie relevée sur le PC.** `msinfo32` annonce **6,00 Go installés** et **5,84 Go de
mémoire physique totale** ; ATLAS PC affichait 5,8 Go **pour les deux**. La persistance,
elle, était correcte.

**Cause.** `Win32_ComputerSystem.TotalPhysicalMemory` ne renvoie pas la mémoire installée :
Windows en a déjà déduit la réserve matérielle (circuit graphique intégré, mémoire réservée
par le BIOS). Sur cette machine, 5,84 Go. La mémoire « utilisable » était lue dans
`Win32_OperatingSystem.TotalVisibleMemorySize`, qui donne presque la même valeur — d'où
deux fois 5,8 Go.

**Correction.**

| Notion | Avant | Après |
|---|---|---|
| installée (`ram_bytes`) | `TotalPhysicalMemory` (5,84 Go) | **somme de `Win32_PhysicalMemory.Capacity`** (6,00 Go) |
| utilisable (`ram_usable_bytes`) | `TotalVisibleMemorySize` | `TotalPhysicalMemory` (5,84 Go, la valeur de `msinfo32`) |

Une **dixième** requête WMI a donc été ajoutée : `SELECT Capacity FROM Win32_PhysicalMemory`.
`TotalVisibleMemorySize` n'est plus lu — `docs/07` § 7.1 explique pourquoi les deux
grandeurs de Windows ne coïncident pas.

**Cas d'indisponibilité, tranché strictement.** Aucun module renvoyé, ou un seul module
sans capacité lisible → **aucune valeur** n'est publiée : `—` dans la carte, « modules
mémoire non lus » en dessous, et une ligne d'incident dans la note de l'inventaire. Une
somme partielle serait un chiffre inventé. Si seule la mémoire utilisable manque, elle
s'écrit « mémoire utilisable non lue » : jamais de repli sur l'installée.

**Conséquences vérifiées.**

| Point | Conséquence |
|---|---|
| Nombre d'appels | 9 → **10** ; `QueriesTotal` et la note suivent automatiquement |
| Délais | 10 s par appel inchangé ; budget interne de 30 s inchangé — dix appels peuvent désormais dépasser le budget, les requêtes restantes sont alors **sautées et consignées** |
| Erreurs | même `try/catch` par appel ; l'absence de modules est une ligne d'incident, pas une exception |
| Schéma SQLite | **aucun changement** : `ram_bytes` et `ram_usable_bytes` existaient déjà, seule leur provenance change. Aucune migration, aucune colonne |
| Inventaires existants | leurs valeurs gardent l'ancienne provenance ; **lancer un nouvel inventaire** affiche les bonnes. La vue montre le dernier inventaire |

**Affichage attendu après correction** : « 6 Go » installés, « dont 5,8 Go utilisables ».

### 2.8 Recette sur Windows 10 — 17/11 : **étape 2 validée**

| Point | Résultat |
|---|---|
| Compilation (MSBuild 17.14.60) | réussie après la correction du constructeur WMI (§ 2.6) |
| Collecte WMI | fonctionne ; la fiche matérielle est remplie |
| Affichage de la mémoire | **corrigé** (§ 2.7) : 6 Go installés, 5,8 Go utilisables, deux valeurs distinctes |
| **Persistance** | **confirmée** : fermeture puis réouverture, les données matérielles sont conservées et **aucune collecte ne se relance au démarrage** |
| Statut | « Partiel », avec la note qui dit que les fichiers ne sont pas inventoriés |

Deux chiffres du protocole n'ont pas été communiqués — durée de collecte et pic mémoire.
Ils restent à relever à l'occasion, sans bloquer la suite : le seuil de 15 s est très
au-dessus de ce qu'une collecte WMI de dix appels peut produire.

**Décision.** Étape 2 **clôturée** le 17/11. Le code du jalon est gelé au commit
`4ffe89f`. Trois commits l'ont construite : `99f3f9c` (livraison), `ac6da1e` (correctif de
compilation), `4ffe89f` (correctif de la mémoire).

### 2.9 Ce qui n'est pas encore là, volontairement

- aucune lecture de fichiers : `roots_count` reste à 0, `scan_root` est vide — c'est
  l'objet de l'étape 3A ;
- les rubriques Fichiers, Logiciels, Outils, Développement et Évolution restent des vues
  d'attente ;
- pas de projet de tests : il arrive à l'étape 3A ;
- `Controls/BarRow.xaml` n'a pas été créé : le tableau des volumes porte son libellé dans
  une colonne voisine, une barre seule suffit. Ce contrôle servira au panneau
  « Répartition par dossier » du tableau de bord (étape 3B), avec libellé + barre + valeur
  sur une même ligne.

---

## Étape 3A — Le moteur de fichiers — **première partie écrite le 17/11**

Découpage validé : **deux blocs**. Ce premier bloc livre le stockage et le moteur, **sans
toucher à l'interface** : rien ne change à l'écran, et aucun parcours ne peut encore être
déclenché par l'utilisateur. Le second bloc apportera le dialogue de sélection des disques,
les compteurs, l'annulation et l'intégration.

### 3.1 Ce qui a été écrit

| Fichier | Rôle | Lignes |
|---|---|---|
| `Scanning/FileScanner.cs` | parcours par pile explicite, totaux, incidents, annulation, statuts par racine | 500 |
| `Scanning/BatchWriter.cs` | lots de 500 entrées ou 2 s, vidage à l'annulation | 80 |
| `Data/ScanStore.cs` | tout le SQL du scan, instructions préparées, règle du sous-arbre | 430 |
| `Models/ScanTarget.cs` | disque à parcourir, résultat par racine, résultat global, photo des compteurs | 75 |
| `Models/ScanBatch.cs` | lignes en attente (structures, pas d'objets) | 31 |
| `Models/ScanIssue.cs` | natures d'incident, alignées sur le `CHECK` du schéma | 53 |
| `Models/ScanRootInfo.cs` | lignes `scan_root` et `folder` relues | 59 |
| `tests/AtlasPc.Tests/` | projet console, **2 + 9 = 11 tests**, aucun paquet NuGet | 900 |

Le schéma **n'a pas bougé** : les tables `folder`, `file` et `scan_issue` existaient déjà
depuis l'étape 1, avec leurs index. Aucune migration, aucune base existante invalidée.

### 3.2 Les quatre points précisés avant d'écrire

**1. Une pile explicite, pas de récursion.** La profondeur d'une arborescence est une
donnée du disque, pas une garantie du programme : une chaîne de dossiers pathologique ferait
déborder la pile d'appels et tuer le processus. Le parcours utilise donc une `List<Frame>`
comme pile — un contexte par dossier ouvert, avec l'index de l'enfant suivant. Mémoire
occupée : un contexte par niveau, jamais l'arborescence entière. Plafond de sécurité à
**1 024 niveaux** : au-delà, le dossier reçoit une ligne `scan_issue` de nature
`path_too_long` et n'est pas parcouru — jamais un oubli silencieux.

**2. L'ordre d'écriture ne viole aucune contrainte — vérifié, pas supposé.** Un dossier est
écrit après ses enfants (c'est ce qui donne sa taille récursive). Contrôle fait dans
`Schema.sql` :

| Question | Réponse |
|---|---|
| `file` a-t-elle une clé étrangère vers `folder` ? | **non** — le lien se fait par `folder_path`, en texte |
| `folder` porte-t-elle un `UNIQUE` ou un `CHECK` ? | **non** — seule clé étrangère : `inventory(id)` |
| L'ordre « fichiers puis dossiers » est-il accepté, clés étrangères actives ? | **oui**, vérifié en exécutant l'insertion contre le vrai schéma |

La seule contrainte à respecter est donc celle qui existait déjà : la ligne `inventory`
avant tout le reste, et la ligne `scan_root` avant le parcours de son disque.

**3. À l'annulation, une seule ligne, à l'endroit exact.** Le moteur écrit **une**
`scan_issue` de nature `cancelled_before`, sur le dossier où le parcours s'est arrêté. Il
n'inscrit pas les dossiers restants : il ne les a pas lus, les nommer serait une
affirmation. La protection ne vient pas de là mais du statut de la racine (`cancelled`) et
de la règle du sous-arbre. Les dossiers encore ouverts sont écrits avec leurs **totaux
partiels**, pour qu'aucune ligne de fichier ne désigne un dossier absent de la table.

**4. La protection des comparaisons, par sous-arbre.** Un incident sur `C:\a\b` protège
`C:\a\b` et tout ce qu'il contient, **et rien d'autre** : ni `C:\a\bc`, ni `C:\a` (le parent,
lui, a été lu), ni un voisin. La comparaison porte sur des **segments de chemin**, jamais
sur des préfixes de chaîne : un dossier nommé `50%` ne doit pas devenir un motif. La règle
est écrite en SQL (`ScanStore.IsProtected`) et **testée dès maintenant**, pour que l'étape 5
n'ait pas à la réinventer.

### 3.3 Deux changements dans l'existant, et pourquoi

**a) Une seconde connexion SQLite.** `SQLiteConnection` ne doit pas servir deux threads à
la fois. Le scan écrit avec **sa** connexion, l'interface garde la sienne pour lire ; le
mode WAL autorise un écrivain et des lecteurs simultanés. `Database` gagne
`CreateConnection(cacheKb)` : 8 Mo de cache pour le scan (16 Mo pour l'interface, on ne
double pas), `busy_timeout` à 5 s pour attendre le verrou plutôt que d'échouer sur
« database is locked », et le mode journal n'est **pas** reposé sur la seconde connexion —
il est inscrit dans le fichier, et le reposer ne ferait qu'ajouter une tentative de verrou à
l'ouverture. La toute première chose que fait le projet de tests est de vérifier cette
coexistence (`Stock_DeuxConnexionsSimultanees`).

**b) `DeleteAbandoned` devient `AbandonRunning` : on ne supprime plus, on clôt.** C'est une
conséquence directe de la consigne « conserver intégralement les inventaires existants ». Un
inventaire resté en `running` vient d'une session tuée. Il était supprimé au démarrage
suivant ; il est désormais **marqué `cancelled`**, avec une note, et ses racines aussi. Trois
raisons : l'historique est conservé ; un parcours inachevé ne peut pas passer pour complet ;
et la suppression en cascade de plusieurs centaines de milliers de lignes n'allonge plus le
démarrage. **À valider** : si vous préférez l'ancien comportement, c'est une méthode à
rétablir.

### 3.4 Ce qui a été vérifié ici (Linux)

Aucun compilateur C# sur cette machine : les contrôles sont statiques et documentaires.

| Contrôle | Résultat |
|---|---|
| Les **20 instructions SQL** du code, exécutées contre le vrai `Schema.sql` (SQLite) | **0 refusée** |
| Ordre « fichier avant son dossier », clés étrangères actives | accepté — confirme le point 2 |
| Règle du sous-arbre, 10 cas (dont `%`, `_`, casse, préfixe commun) | **10/10 conformes** |
| Suppression d'un inventaire : cascade sur `file`, `folder`, `scan_issue`, `scan_root` | 4 tables vidées, 0 orpheline |
| Équilibre accolades/parenthèses des **32 fichiers C#** | conformes |
| Constructions C# 8+ dans un projet `LangVersion 7.3` | aucune |
| Paramètres SQL déclarés vs utilisés | 34/34 |

**Non vérifié ici, et c'est le but de la recette :** la compilation, l'exécution des 11
tests, et le comportement réel de `EnumerateFileSystemInfos` sur ce PC.

### 3.5 Recette sur le PC — première partie

1. Ouvrir `ATLAS-PC.sln`. La solution contient maintenant **deux projets** : `AtlasPc` et
   `AtlasPc.Tests`.
2. **Générer la solution** (Ctrl+Maj+B). Attendu : `0 avertissement, 0 erreur`.
   Si MSBuild signale une erreur, me la transmettre telle quelle : je corrige.
3. Clic droit sur `AtlasPc.Tests` → **Définir comme projet de démarrage**, puis F5.
   Une console s'ouvre. Attendu, dans cet ordre :
   - `Stock_DeuxConnexionsSimultanees` **OK** — c'est le point D2, la vérification qui
     conditionne tout le reste ;
   - `Stock_EcritureParLotsEtRelecture` **OK** ;
   - les neuf tests du moteur **OK** ;
   - dernière ligne : `11 réussis, 0 échoués, 0 ignorés` — sauf les deux tests « IGNORÉ »
     expliqués au point suivant.
4. **Deux tests peuvent être « IGNORÉ »**, et c'est prévu :
   - `Scanner_SauteLesPointsDeReparse` si la machine refuse `mklink /J` ;
   - `Scanner_EnregistreLesDossiersIllisibles` si `icacls` refuse de poser le refus d'accès.
   Dans ce cas, le motif est affiché. Ce n'est pas une réussite : dites-le-moi.
5. **Relancer l'application** (`AtlasPc` comme projet de démarrage) : elle doit se comporter
   **exactement comme après l'étape 2**. L'interface n'a pas été touchée : le bouton
   « Lancer un inventaire » collecte toujours le matériel et clôt en « Partiel », en
   annonçant que les fichiers ne sont pas inventoriés. Si quelque chose a changé à l'écran,
   c'est un défaut.
6. Vérifier que l'historique est intact : les inventaires précédents sont toujours là, avec
   leurs dates, et aucun n'a été supprimé.

**Ce que j'attends en retour :** le texte de la console (copier-coller), et le résultat de
la compilation. C'est ce qui autorise la seconde partie.

### 3.6 Ce qui n'est pas encore là, volontairement

- **Aucune interface** : ni dialogue de sélection des disques, ni compteurs, ni `Échap`.
  Ce sera la seconde partie, et rien ne peut déclencher un parcours avant elle.
- **`ProjectDetector`** (marqueurs `package.json`, `requirements.txt`, `.git`) : reporté à
  la seconde partie, avec le branchement dans la boucle. Son absence est sans effet
  aujourd'hui — aucun parcours réel n'a lieu — et il doit être en place **avant** le premier
  vrai scan, puisque le repérage se fait pendant l'énumération, sans seconde lecture.
- **Aucun raccordement au tableau de bord ni à l'historique** : la note d'inventaire dit
  toujours que les fichiers ne sont pas inventoriés, ce qui reste exact.
- **Pas encore mesuré** : durée d'un parcours réel, pic mémoire, taille de la base pour
  400 000 fichiers. Ces trois chiffres appartiennent à la seconde partie, quand un disque
  entier pourra être parcouru.
