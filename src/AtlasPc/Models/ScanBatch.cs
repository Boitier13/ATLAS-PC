using System;

namespace AtlasPc.Models
{
    /// <summary>
    /// Une ligne de fichier en attente d'écriture. Une structure plutôt qu'une classe :
    /// un lot de 500 éléments ne doit pas créer 500 objets sur le tas.
    /// </summary>
    public struct PendingFile
    {
        public string FolderPath;
        public string Name;
        public string Ext;
        public long SizeBytes;
        public DateTime ModifiedUtc;
    }

    /// <summary>
    /// Une ligne de dossier en attente d'écriture. Écrite après ses enfants, avec sa
    /// taille récursive, le nombre de ses fichiers directs et de ses sous-dossiers directs.
    /// </summary>
    public struct PendingFolder
    {
        public string ParentPath;
        public string Path;
        public long SizeBytes;
        public int FilesCount;
        public int DirsCount;
        public DateTime? ModifiedUtc;
    }
}
