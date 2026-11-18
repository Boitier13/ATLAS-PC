# ATLAS PC — première passe de refonte

Branche source : `refonte/interface-atlas`
Commit source : `ec3f00f`

## Modifications
- Police globale : Segoe UI, avec replis Windows standards.
- Thème clair choisi par défaut ; le thème sombre reste disponible.
- Refonte de la vue d'ensemble : hiérarchie typographique, espacements, cartes, tableau et panneaux d'identification.
- Signalement explicite du mode démonstration et de la nature fictive des données fournies.
- Le bouton « Nouvel inventaire » ne crée plus une entrée fictive ni n'annonce une collecte réussie : il informe que la collecte réelle n'est pas encore disponible.
- Barre d'état renommée « Mode démonstration ».

## Vérification
La compilation n'a pas pu être exécutée dans cet environnement : les dépendances npm (React, Lucide, Vite et types TypeScript) ne sont pas installées et l'installation npm a expiré. Il faudra lancer `npm install`, puis `npm run lint` et `npm run build` dans l'environnement du projet.

Aucune modification du dépôt GitHub ni de la branche `main` n'a été effectuée.
