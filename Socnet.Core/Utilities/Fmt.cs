using System.Globalization;

namespace Socnet.Core.Utilities
{
    /// <summary>
    /// Culture-invariant number formatting and parsing. Socnet.se always uses '.' as decimal separator,
    /// independent of the locale of the machine it runs on.
    /// </summary>
    public static class Fmt
    {
        private const NumberStyles DoubleStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        /// <summary>
        /// Formats a double value the same way as double.ToString() does in an invariant culture.
        /// </summary>
        public static string D(double value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses a double value (culture-invariant). Returns false if the string is not a number.
        /// </summary>
        public static bool TryParseDouble(string s, out double value) => double.TryParse(s, DoubleStyle, CultureInfo.InvariantCulture, out value);

        /// <summary>
        /// Parses an integer (culture-invariant). Returns false if the string is not an integer.
        /// </summary>
        public static bool TryParseInt(string s, out int value) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
