namespace EverythingToolbar.Data
{
    public class Filter
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Search { get; set; } = "";
        public string Macro { get; set; } = "";
        public bool IsMatchCase { get; set; }
        public bool IsMatchWholeWord { get; set; }
        public bool IsMatchPath { get; set; }
        public bool IsRegExEnabled { get; set; }

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

        public void Reset()
        {
            IsMatchCase = false;
            IsMatchWholeWord = false;
            IsMatchPath = false;
            IsRegExEnabled = false;
            Search = "";
            Macro = "";
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
