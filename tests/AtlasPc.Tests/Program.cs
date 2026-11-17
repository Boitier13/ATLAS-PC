using System;

namespace AtlasPc.Tests
{
    /// <summary>
    /// Première partie de l'étape 3A : le stockage et le moteur, sans interface.
    /// Lancer ce programme suffit : code de sortie 0 si tout passe, 1 sinon.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch (Exception)
            {
                // Une console qui refuse l'UTF-8 reste utilisable.
            }

            Console.WriteLine("ATLAS PC — moteur de fichiers, première partie de l'étape 3A");
            Console.WriteLine("Dossiers temporaires réels, base SQLite temporaire : aucune doublure.");
            Console.WriteLine();

            StorageTests.Register();
            ScannerTests.Register();

            return TestRunner.Run();
        }
    }
}
