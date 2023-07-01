using EverythingToolbar.Properties;

namespace EverythingToolbar.Data
{
    public class Filter
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsMatchCase { get; set; }
        public bool IsMatchWholeWord { get; set; }
        public bool IsMatchPath { get; set; }
        public bool IsRegExEnabled { get; set; }
        public string Search { get; set; }
        public string Macro { get; set; }

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

        public string GetSearchPrefix()
        {
            if (string.IsNullOrEmpty(Search))
                return "";

            var modifiers = "";
            if (IsMatchCase != Settings.Default.isMatchCase)
                modifiers += IsMatchCase ? "case:" : "nocase:";
            if (IsMatchWholeWord != Settings.Default.isMatchWholeWord)
                modifiers += IsMatchWholeWord ? "ww:" : "noww:";
            if (IsMatchPath != Settings.Default.isMatchPath)
                modifiers += IsMatchPath ? "path:" : "nopath:";
            if (IsRegExEnabled != Settings.Default.isRegExEnabled)
                modifiers += IsRegExEnabled ? "regex:" : "noregex:";

            if (string.IsNullOrEmpty(modifiers))
                return $"{Search} ";
            
            return $"{modifiers}<{Search}> ";
        }
    }
}
