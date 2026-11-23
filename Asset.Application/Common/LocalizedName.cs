using System.Globalization;

namespace Asset.Application.Common
{
    // Small helpers for the Arabic name columns (CategoryNameAr, TypeNameAr, DepartmentNameAr, LocationNameAr, FullNameAr).
    public static class LocalizedName
    {
        // True when the request came with "Accept-Language: ar".
        // UseRequestLocalization (Program.cs) copies that header into CurrentUICulture for every request.
        public static bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        // The name to show for the current language.
        // Falls back to English when the Arabic name was never filled in (the column is NULL).
        // Use it ONLY in code that is not cached in Redis (AI answers, error messages).
        // Cached queries return BOTH names and the frontend picks one, so one cached copy serves both languages.
        public static string Pick(string english, string? arabic)
        {
            return IsArabic && !string.IsNullOrWhiteSpace(arabic) ? arabic : english;
        }

        // What we save in a NameAr column: trimmed text, or NULL when the user left it empty.
        // Why NULL and not "": the unique index is "WHERE NameAr IS NOT NULL", so two rows
        // with "" would break it, while any number of NULLs is allowed.
        public static string? TrimOrNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

/*
    Request (Accept-Language: ar)
              │
              ▼
    CurrentUICulture = "ar"  ──▶  Pick("Laptop", "لابتوب")  →  "لابتوب"
                                  Pick("Laptop", null)      →  "Laptop"   (no Arabic yet → English)
*/
