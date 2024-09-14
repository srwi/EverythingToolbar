namespace EverythingToolbar.Data
{
    public class Filter
    {
        public string Name { get; set; }
        public string Icon { get; set; } = "";
        public string Search { get; set; } = "";
        public string Macro { get; set; } = "";
        public bool IsMatchCase { get; set; } = false;
        public bool IsMatchWholeWord { get; set; } = false;
        public bool IsMatchPath { get; set; } = false;
        public bool IsRegExEnabled { get; set; } = false;

        public override bool Equals(object obj)
        {
            if (!(obj is Filter item))
            {
                return false;
            }

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

        public string GetSearchPrefix()
        {
            if (string.IsNullOrEmpty(Search))
                return "";

            var modifiers = "";
            if (IsMatchCase != ToolbarSettings.User.IsMatchCase)
                modifiers += IsMatchCase ? "case:" : "nocase:";
            if (IsMatchWholeWord != ToolbarSettings.User.IsMatchWholeWord)
                modifiers += IsMatchWholeWord ? "ww:" : "noww:";
            if (IsMatchPath != ToolbarSettings.User.IsMatchPath)
                modifiers += IsMatchPath ? "path:" : "nopath:";
            if (IsRegExEnabled != ToolbarSettings.User.IsRegExEnabled)
                modifiers += IsRegExEnabled ? "regex:" : "noregex:";

            if (string.IsNullOrEmpty(modifiers))
                return $"{Search} ";
            
            return $"{modifiers}<{Search}> ";
        }
    }
}
