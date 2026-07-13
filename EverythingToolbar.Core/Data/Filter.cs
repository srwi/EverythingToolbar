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
            bool currentIsRegExEnabled)
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

            if (string.IsNullOrEmpty(modifiers))
                return $"{Search} ";

            return $"{modifiers}<{Search}> ";
        }
    }
}
