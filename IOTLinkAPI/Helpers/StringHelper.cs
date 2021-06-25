using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IOTLinkAPI.Helpers
{
    /// <summary>
	/// General converter for strings.
	/// </summary>
	public static class StringHelper
    {
        /// <summary>
        /// Convert a string to a boolean Value. Uses Default parameter in case of failure.
        /// </summary>
        /// <param name="Value">String to be converted.</param>
        /// <param name="Default">Default Value to be used if the conversion fails.</param>
        /// <returns>Converted Value or default Value if any failure occurs.</returns>
        public static bool ReadSafeBool(string Value, bool Default)
        {
            try
            {
                bool result = Convert.ToBoolean(Value);
                return result;
            }
            catch (Exception)
            {
                return Default;
            }
        }

        /// <summary>
        /// Convert a string to an integer Value. Uses Default parameter in case of failure.
        /// </summary>
        /// <param name="Value">String to be converted.</param>
        /// <param name="Default">Default Value to be used if the conversion fails.</param>
        /// <returns>Converted Value or default Value if any failure occurs.</returns>
        public static int ReadSafeInt(string Value, int Default)
        {
            int result = Default;
            if (Int32.TryParse(Value, out result))
                return result;
            else
                return Default;
        }

        /// <summary>
        /// Convert a string to a floating Value. Uses Default parameter in case of failure.
        /// </summary>
        /// <param name="Value">String to be converted.</param>
        /// <param name="Default">Default Value to be used if the conversion fails.</param>
        /// <returns>Converted Value or default Value if any failure occurs.</returns>
        public static float ReadSafeFloat(string Value, float Default)
        {
            try
            {
                Value = Value.Replace("f", string.Empty).Replace("F", string.Empty).Trim();

                float result = float.Parse(Value, CultureInfo.InvariantCulture.NumberFormat);
                return result;
            }
            catch (Exception)
            {
                return Default;
            }
        }

        /// <summary>
        /// Convert a string to a double Value. Uses Default parameter in case of failure.
        /// </summary>
        /// <param name="Value">String to be converted.</param>
        /// <param name="Default">Default Value to be used if the conversion fails.</param>
        /// <returns>Converted Value or default Value if any failure occurs.</returns>
        public static double ReadSafeDouble(string Value, double Default)
        {
            try
            {
                Value = Value.Replace("D", string.Empty).Replace("d", string.Empty).Trim();

                double result = double.Parse(Value, CultureInfo.InvariantCulture.NumberFormat);
                return result;
            }
            catch (Exception)
            {
                return Default;
            }
        }

        /// <summary>
        /// Check if the given string is a valid id (a-zA-Z0-9_) string.
        /// </summary>
        /// <param name="Value">String to be valided</param>
        /// <returns>True or false</returns>
        public static bool IsValidID(string Value)
        {
            if (string.IsNullOrEmpty(Value))
                return false;

            Regex r = new Regex("^[a-zA-Z0-9_]*$");

            return r.IsMatch(Value);
        }

        /// <summary>
        /// Parse a string to a version array.
        /// String must be in the following format: X.Y.Z.W
        /// </summary>
        /// <param name="versionArray">Reference to an array to store the parsed values.</param>
        /// <param name="versionStr">Desired string to be parsed.</param>
        public static void ParseVersion(ref int[] versionArray, string versionStr)
        {
            string[] str = versionStr.Split('.');

            for (int i = 0; i < Math.Min(4, str.Length); i++)
            {
                int n = StringHelper.ReadSafeInt(str[i], versionArray[i]);
                versionArray[i] = n;
            }
        }

        /// <summary>
        /// Remove diacritics from the input string.
        /// </summary>
        /// <param name="text">Input string to be sanitized</param>
        /// <returns>String without diacritic marks</returns>
        public static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Convert PascalCase to kebab-case.
        /// Using https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions
        /// </summary>
        /// <param name="value">String using PascalCase</param>
        /// <returns>String using kebab-case</returns>
        public static string PascalToKebabCase(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;

            if (source.Length > 5)
                return Regex.Replace(source, "(?<!^)(?<!/)([A-Z][a-z]|(?<=[a-z])[A-Z])", "-$1", RegexOptions.Compiled).Trim().ToLowerInvariant();

            return source.Trim().ToLowerInvariant();
        }
        /// <summary>
        /// Convert PascalCase to kebab-case.
        /// Using https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions
        /// </summary>
        /// <param name="value">String using PascalCase</param>
        /// <returns>String using kebab-case</returns>
        public static string PascalToSnakeCase(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;

            if (source.Length > 5)
                return Regex.Replace(source, "(?<!^)(?<!/)([A-Z][a-z]|(?<=[a-z])[A-Z])", "_$1", RegexOptions.Compiled).Trim().ToLowerInvariant();

            return source.Trim().ToLowerInvariant();
        }
    }
}
