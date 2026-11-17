using System;
using System.Globalization;

namespace AtlasPc.Data
{
    /// <summary>
    /// Format de date unique de la base : ISO-8601 UTC, trié et comparable en texte.
    /// </summary>
    internal static class SqlDate
    {
        public const string Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        public static string ToDb(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return utc.ToString(Format, CultureInfo.InvariantCulture);
        }

        public static string ToDb(DateTime? value)
        {
            return value.HasValue ? ToDb(value.Value) : null;
        }

        public static DateTime FromDb(string value)
        {
            DateTime parsed;
            if (!string.IsNullOrEmpty(value) &&
                DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
            {
                return parsed;
            }

            return DateTime.MinValue;
        }
    }
}
