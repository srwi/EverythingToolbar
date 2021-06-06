namespace EverythingToolbar.Data
{
    class Filter
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool? IsMatchCase { get; set; }
        public bool? IsMatchWholeWord { get; set; }
        public bool? IsMatchPath { get; set; }
        public bool? IsRegExEnabled { get; set; }
        public string Search { get; set; }
        public string Macro { get; set; }

        public override bool Equals(object obj)
        {
            Filter item = obj as Filter;

            if (item == null)
            {
                return false;
            }

            return Name.Equals(item.Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
