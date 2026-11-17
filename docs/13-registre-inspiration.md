# Registre d'inspiration — modèle à tenir dès le premier commit

> Ce registre n'est pas une obligation légale en soi : c'est la **preuve documentaire**
> que le code d'ATLAS PC n'a pas été copié depuis les dépôts étudiés. Il est recommandé
> de le remplir au fil du développement, pas après coup.
>
> Règle associée, rappelée dans `docs/06-licences-et-risques.md` § 5 :
> **aucun copier-coller de code depuis les quatre dépôts.** Les idées, méthodes et
> algorithmes sont libres ; leur expression ne l'est pas.

---

## 1. Comment remplir une ligne

| Colonne | Contenu attendu |
|---|---|
| Date | date de la décision |
| Module ATLAS PC | fichier ou fonctionnalité concernée |
| Source consultée | dépôt + chemin exact + commit |
| Idée retenue | en une phrase, **le principe**, pas le code |
| Ce qui n'a pas été repris | ce qui a été volontairement écarté et pourquoi |
| Vérification | « aucune ligne copiée », signé par la personne |

---

## 2. Exemples de lignes déjà justifiées par le dossier d'étude

| Date | Module ATLAS PC | Source consultée | Idée retenue | Non repris | Vérification |
|---|---|---|---|---|---|
| 17/11/2026 | `Atlas.Scan/Native/Kernel32.cs` | NeoTreescan `src/NeoTreescan.Core/Interop/NativeMethods.cs` | usage de `FindFirstFileExW` + `FindExInfoBasic` + `FIND_FIRST_EX_LARGE_FETCH` | code d'import, structures, nommage | aucune ligne copiée |
| 17/11/2026 | `Atlas.Scan/PathHandling/LongPath.cs` | NeoTreescan `Interop/LongPath.cs`, Diskplot `src/main/dirworker.ts` | préfixe `\\?\` avec concaténation manuelle | code, cas limites | aucune ligne copiée |
| 17/11/2026 | `Atlas.Core/Contracts/ICollector.cs` | System Inspector `Invoke-SafeQuery` (l. 208) | un résultat uniforme (statut, message, code, durée) par source | implémentation PowerShell | aucune ligne copiée |
| 17/11/2026 | `Atlas.Scan/Engine/DirectoryScanEngine.cs` | Diskplot `src/main/scanworker.ts` | deux sortes d'arrêt : conserver le partiel / tout jeter | TypeScript, workers | aucune ligne copiée |
| 17/11/2026 | `Atlas.Scan/Engine/ScanOptions.cs` | WinDirStat `Options.h` (l. 18, 309) | un réglage de nombre de travailleurs, borné | valeur par défaut, plafond | aucune ligne copiée |
| 17/11/2026 | `Atlas.Data/Queries/ComparisonQueries.cs` | Diskplot `src/renderer/src/lib/analysis.ts` | comparer par appariement de noms dans le même parent | seuil 8 Mio, format binaire | aucune ligne copiée |
| 17/11/2026 | `Atlas.App/Views/FilesView.xaml` | NeoTreescan `ViewModels/FolderNodeViewModel.cs` | chargement paresseux des enfants de l'arborescence | code MVVM | aucune ligne copiée |

---

## 3. Journal des consultations

À compléter au fur et à mesure. Une consultation de code GPL (WinDirStat) doit être
**déclarée** ici, même si elle n'a produit aucune ligne de code.

| Date | Personne | Dépôt | Fichiers lus | Objet de la lecture | Conclusion |
|---|---|---|---|---|---|
| | | | | | |
