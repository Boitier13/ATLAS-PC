using System;
using System.Collections.Generic;

namespace AtlasPc.Tests
{
    /// <summary>
    /// Exécuteur minimal : une liste de tests nommés, trois assertions, un code de sortie.
    /// Pas de paquet, pas de fichier de configuration, pas de découverte automatique.
    /// </summary>
    internal static class TestRunner
    {
        private static readonly List<KeyValuePair<string, Action>> Tests =
            new List<KeyValuePair<string, Action>>();

        public static void Add(string name, Action test)
        {
            Tests.Add(new KeyValuePair<string, Action>(name, test));
        }

        public static int Run()
        {
            var failed = 0;
            var skipped = 0;

            foreach (var test in Tests)
            {
                try
                {
                    test.Value();
                    Console.WriteLine("  OK     " + test.Key);
                }
                catch (SkipTestException skip)
                {
                    skipped++;
                    Console.WriteLine("  IGNORÉ " + test.Key);
                    Console.WriteLine("         " + skip.Message);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("  ÉCHEC  " + test.Key);
                    Console.WriteLine("         " + ex.GetType().Name + " : " + ex.Message);
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                        Console.WriteLine("         " + ex.StackTrace.Replace("\n", "\n         ").TrimEnd());
                }
            }

            Console.WriteLine();
            Console.WriteLine("  " + (Tests.Count - failed - skipped) + " réussis, " + failed +
                              " échoués, " + skipped + " ignorés.");
            return failed == 0 ? 0 : 1;
        }
    }

    /// <summary>Un test que la machine ne permet pas de faire : il est annoncé, pas masqué.</summary>
    internal sealed class SkipTestException : Exception
    {
        public SkipTestException(string message) : base(message)
        {
        }
    }

    internal static class Assert
    {
        public static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        public static void NotNull(object value, string message)
        {
            if (value == null) throw new Exception(message);
        }

        public static void Equal(long expected, long actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " — attendu " + expected + ", obtenu " + actual);
        }

        /// <summary>Comparaison de chemins : Windows ignore la casse, le test aussi.</summary>
        public static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new Exception(message + " — attendu « " + (expected ?? "null") +
                                    " », obtenu « " + (actual ?? "null") + " »");
        }
    }
}
