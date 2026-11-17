-- ATLAS PC — schéma SQLite v1
-- Créé en une fois : aucune migration prévue (voir docs/08-modele-sqlite.md).
-- Toutes les instructions sont idempotentes : relancer ce script sur une base
-- existante ne détruit rien et ajoute ce qui manque.

-- 1. Inventaires ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory (
    id            INTEGER PRIMARY KEY,
    started_at    TEXT    NOT NULL,
    finished_at   TEXT,
    status        TEXT    NOT NULL
                  CHECK (status IN ('running','complete','partial','cancelled')),
    app_version   TEXT    NOT NULL,
    roots_count   INTEGER NOT NULL DEFAULT 0,
    files_count   INTEGER NOT NULL DEFAULT 0,
    folders_count INTEGER NOT NULL DEFAULT 0,
    total_bytes   INTEGER NOT NULL DEFAULT 0,
    issues_count  INTEGER NOT NULL DEFAULT 0,
    note          TEXT
);

-- 2. Disques analysés : le statut par emplacement --------------------------
CREATE TABLE IF NOT EXISTS scan_root (
    id            INTEGER PRIMARY KEY,
    inventory_id  INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    path          TEXT    NOT NULL,
    drive         TEXT    NOT NULL,
    label         TEXT,
    status        TEXT    NOT NULL
                  CHECK (status IN ('running','complete','partial','cancelled','inaccessible')),
    files_count   INTEGER NOT NULL DEFAULT 0,
    folders_count INTEGER NOT NULL DEFAULT 0,
    total_bytes   INTEGER NOT NULL DEFAULT 0,
    issues_count  INTEGER NOT NULL DEFAULT 0,
    started_at    TEXT,
    finished_at   TEXT,
    error_message TEXT,
    UNIQUE (inventory_id, drive)
);

-- 3. Matériel principal et Windows ------------------------------------------
CREATE TABLE IF NOT EXISTS computer (
    inventory_id  INTEGER PRIMARY KEY REFERENCES inventory(id) ON DELETE CASCADE,
    manufacturer  TEXT,
    model         TEXT,
    cpu_name      TEXT,
    cpu_cores     INTEGER,
    ram_bytes     INTEGER,
    gpu_name      TEXT,
    bios_vendor   TEXT,
    bios_version  TEXT,
    os_name       TEXT,
    os_version    TEXT,
    os_build      TEXT,
    os_arch       TEXT,
    os_install_date TEXT
);

-- 4. Volumes et disques physiques -------------------------------------------
CREATE TABLE IF NOT EXISTS volume (
    id              INTEGER PRIMARY KEY,
    inventory_id    INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    drive           TEXT,
    label           TEXT,
    file_system     TEXT,
    total_bytes     INTEGER,
    free_bytes      INTEGER,
    disk_index      INTEGER,
    disk_model      TEXT,
    disk_size_bytes INTEGER
);

-- 5. Logiciels installés ----------------------------------------------------
CREATE TABLE IF NOT EXISTS software (
    id               INTEGER PRIMARY KEY,
    inventory_id     INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    name             TEXT NOT NULL,
    version          TEXT,
    publisher        TEXT,
    install_location TEXT,
    install_date     TEXT
);

-- 6. Outils de développement ------------------------------------------------
CREATE TABLE IF NOT EXISTS tool (
    id           INTEGER PRIMARY KEY,
    inventory_id INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    kind         TEXT NOT NULL,
    path         TEXT,
    version      TEXT,
    status       TEXT NOT NULL
);

-- 7. Projets ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS project (
    id           INTEGER PRIMARY KEY,
    inventory_id INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    path         TEXT NOT NULL,
    kind         TEXT NOT NULL,
    is_git       INTEGER NOT NULL DEFAULT 0
);

-- 8. Dossiers ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS folder (
    id           INTEGER PRIMARY KEY,
    inventory_id INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    parent_path  TEXT,
    path         TEXT NOT NULL,
    size_bytes   INTEGER NOT NULL DEFAULT 0,
    files_count  INTEGER NOT NULL DEFAULT 0,
    dirs_count   INTEGER NOT NULL DEFAULT 0,
    modified_at  TEXT
);

-- 9. Fichiers ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS file (
    id           INTEGER PRIMARY KEY,
    inventory_id INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    folder_path  TEXT NOT NULL,
    name         TEXT NOT NULL,
    ext          TEXT,
    size_bytes   INTEGER NOT NULL,
    modified_at  TEXT NOT NULL
);

-- 10. Zones non analysées : une ligne par dossier en échec -------------------
CREATE TABLE IF NOT EXISTS scan_issue (
    id           INTEGER PRIMARY KEY,
    inventory_id INTEGER NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    root_drive   TEXT,
    path         TEXT NOT NULL,
    kind         TEXT NOT NULL
                 CHECK (kind IN ('access_denied','io_error','path_too_long',
                                 'cancelled_before','unexpected')),
    code         INTEGER,
    message      TEXT
);

-- Index --------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS ix_inventory_date  ON inventory(started_at DESC);

CREATE INDEX IF NOT EXISTS ix_software_name   ON software(inventory_id, name);
CREATE INDEX IF NOT EXISTS ix_tool_kind       ON tool(inventory_id, kind);
CREATE INDEX IF NOT EXISTS ix_project_path    ON project(inventory_id, path);

CREATE INDEX IF NOT EXISTS ix_folder_path     ON folder(inventory_id, path COLLATE NOCASE);
CREATE INDEX IF NOT EXISTS ix_folder_parent   ON folder(inventory_id, parent_path COLLATE NOCASE);
CREATE INDEX IF NOT EXISTS ix_folder_size     ON folder(inventory_id, size_bytes DESC);

CREATE INDEX IF NOT EXISTS ix_file_name       ON file(inventory_id, name COLLATE NOCASE);
CREATE INDEX IF NOT EXISTS ix_file_key        ON file(inventory_id, folder_path COLLATE NOCASE,
                                                            name COLLATE NOCASE);
CREATE INDEX IF NOT EXISTS ix_file_folder     ON file(inventory_id, folder_path COLLATE NOCASE);
CREATE INDEX IF NOT EXISTS ix_file_ext        ON file(inventory_id, ext, size_bytes DESC);
CREATE INDEX IF NOT EXISTS ix_file_size       ON file(inventory_id, size_bytes DESC);

CREATE INDEX IF NOT EXISTS ix_issue_root      ON scan_issue(inventory_id, root_drive);
CREATE INDEX IF NOT EXISTS ix_issue_path      ON scan_issue(inventory_id, path COLLATE NOCASE);
