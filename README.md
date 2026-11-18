# ATLAS PC

Application Windows d’inventaire du matériel et des logiciels d’un ordinateur.

## État actuel

- Interface React, TypeScript et Vite.
- Intégration du conteneur natif Tauri 2.
- Les données affichées dans cette version restent des données de démonstration.
- La collecte réelle des informations Windows et la persistance locale ne sont pas encore implémentées.

## Développement dans le navigateur

```bash
npm ci
npm run dev
```

## Application de bureau Tauri

Prérequis : Node.js, Rust et les dépendances de compilation Windows requises par Tauri 2.

```bash
npm ci
npm run tauri:dev
```

Compilation Windows :

```bash
npm run tauri:build
```

Les paquets générés sont placés dans `src-tauri/target/release/bundle/`.
