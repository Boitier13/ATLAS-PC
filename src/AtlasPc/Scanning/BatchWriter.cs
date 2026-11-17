using System;
using System.Collections.Generic;
using System.Diagnostics;
using AtlasPc.Data;
using AtlasPc.Models;

namespace AtlasPc.Scanning
{
    /// <summary>
    /// Accumule les lignes et les envoie par lots : toutes les 500 entrées ou toutes les
    /// 2 secondes, au premier des deux. Un parcours de plusieurs heures ne doit ni perdre
    /// son travail à la première coupure, ni retenir des centaines de milliers de lignes
    /// en mémoire.
    /// </summary>
    public sealed class BatchWriter
    {
        private readonly ScanStore _store;
        private readonly long _inventoryId;
        private readonly int _batchSize;
        private readonly TimeSpan _maxDelay;

        private readonly List<PendingFile> _files = new List<PendingFile>();
        private readonly List<PendingFolder> _folders = new List<PendingFolder>();
        private readonly Stopwatch _sinceFlush = Stopwatch.StartNew();

        public BatchWriter(ScanStore store, long inventoryId, int batchSize, TimeSpan maxDelay)
        {
            if (store == null) throw new ArgumentNullException("store");
            if (batchSize <= 0) throw new ArgumentOutOfRangeException("batchSize");

            _store = store;
            _inventoryId = inventoryId;
            _batchSize = batchSize;
            _maxDelay = maxDelay;
        }

        /// <summary>Nombre de lignes effectivement écrites depuis le début.</summary>
        public long WrittenRows { get; private set; }

        /// <summary>Lignes en attente : c'est la seule mémoire occupée par le parcours.</summary>
        public int PendingRows
        {
            get { return _files.Count + _folders.Count; }
        }

        public void AddFile(string folderPath, string name, string ext, long sizeBytes, DateTime modifiedUtc)
        {
            _files.Add(new PendingFile
            {
                FolderPath = folderPath,
                Name = name,
                Ext = ext,
                SizeBytes = sizeBytes,
                ModifiedUtc = modifiedUtc
            });

            if (_files.Count >= _batchSize || _sinceFlush.Elapsed >= _maxDelay) Flush();
        }

        public void AddFolder(PendingFolder folder)
        {
            _folders.Add(folder);

            if (_files.Count + _folders.Count >= _batchSize || _sinceFlush.Elapsed >= _maxDelay) Flush();
        }

        /// <summary>Écrit ce qui reste. Appelé à la fin de chaque racine et à l'annulation.</summary>
        public void Flush()
        {
            if (_files.Count == 0 && _folders.Count == 0) return;

            _store.WriteBatch(_inventoryId, _files, _folders);

            WrittenRows += _files.Count + _folders.Count;
            _files.Clear();
            _folders.Clear();
            _sinceFlush.Restart();
        }
    }
}
