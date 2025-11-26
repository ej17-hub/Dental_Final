using System;

namespace Dental_Final
{
    public static class NameFormatter
    {
        // Formats a patient display name as "First M. Last"
        // - Trims inputs
        // - If middle looks like a full last name and last looks like an initial, swaps them
        // - Normalizes middle to an initial with a dot when present
        public static string FormatPatientDisplayName(string first, string middle, string last)
        {
            first = (first ?? string.Empty).Trim();
            middle = (middle ?? string.Empty).Trim();
            last = (last ?? string.Empty).Trim();

            // detect likely swapped fields: middle long (full last) and last short (initial)
            if (!string.IsNullOrEmpty(middle) && !string.IsNullOrEmpty(last))
            {
                if (middle.Length > 2 && last.Length <= 2)
                {
                    var tmp = last;
                    last = middle;
                    middle = tmp;
                }
            }

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                return string.Empty;

            if (string.IsNullOrEmpty(middle))
                return $"{first} {last}".Trim();

            // make middle a single initial
            var m = middle.Trim().Trim('.');
            if (m.Length > 0)
                m = m.Substring(0, 1).ToUpper();
            return string.IsNullOrEmpty(last) ? $"{first} {m}." : $"{first} {m}. {last}";
        }
    }
}