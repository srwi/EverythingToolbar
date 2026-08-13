namespace EverythingToolbar.Core.Data
{
    public class Filter
    {
        public string Name { get; init; } = "";
        public string Icon { get; init; } = "";
        public string Search { get; init; } = "";
        public string Macro { get; init; } = "";
        public bool IsMatchCase { get; init; }
        public bool IsMatchWholeWord { get; init; }
        public bool IsMatchPath { get; init; }
        public bool IsRegExEnabled { get; init; }
        public bool IsMatchDiacritics { get; init; }
        public bool IsMatchPrefix { get; init; }
        public bool IsMatchSuffix { get; init; }
        public bool IsIgnorePunctuation { get; init; }
        public bool IsIgnoreWhitespace { get; init; }
        public SortBy? SortBy { get; init; }
        public bool SortDescending { get; init; }

        public override bool Equals(object? obj)
        {
            if (obj is not Filter item)
                return false;

            return Name.Equals(item.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public string GetSearchPrefix(
            bool currentIsMatchCase,
            bool currentIsMatchWholeWord,
            bool currentIsMatchPath,
            bool currentIsRegExEnabled,
            bool currentIsMatchDiacritics,
            bool currentIsMatchPrefix,
            bool currentIsMatchSuffix,
            bool currentIsIgnorePunctuation,
            bool currentIsIgnoreWhitespace,
            bool supportsEverything15
        )
        {
            if (string.IsNullOrEmpty(Search))
                return "";

            var modifiers = "";
            if (IsMatchCase != currentIsMatchCase)
                modifiers += IsMatchCase ? "case:" : "nocase:";
            if (IsMatchWholeWord != currentIsMatchWholeWord)
                modifiers += IsMatchWholeWord ? "ww:" : "noww:";
            if (IsMatchPath != currentIsMatchPath)
                modifiers += IsMatchPath ? "path:" : "nopath:";
            if (IsRegExEnabled != currentIsRegExEnabled)
                modifiers += IsRegExEnabled ? "regex:" : "noregex:";
            if (IsMatchDiacritics != currentIsMatchDiacritics && !IsRegExEnabled)
                modifiers += IsMatchDiacritics ? "diacritics:" : "nodiacritics:";

            if (supportsEverything15)
            {
                if (IsMatchPrefix != currentIsMatchPrefix && !IsRegExEnabled)
                    modifiers += IsMatchPrefix ? "prefix:" : "noprefix:";
                if (IsMatchSuffix != currentIsMatchSuffix && !IsRegExEnabled)
                    modifiers += IsMatchSuffix ? "suffix:" : "nosuffix:";
                if (IsIgnorePunctuation != currentIsIgnorePunctuation && !IsRegExEnabled)
                    modifiers += IsIgnorePunctuation ? "ignore-punctuation:" : "no-ignore-punctuation:";
                if (IsIgnoreWhitespace != currentIsIgnoreWhitespace && !IsRegExEnabled)
                    modifiers += IsIgnoreWhitespace ? "ignore-whitespace:" : "no-ignore-whitespace:";
            }

            if (string.IsNullOrEmpty(modifiers))
                return $"{Search} ";

            return $"{modifiers}<{Search}> ";
        }
    }
}
