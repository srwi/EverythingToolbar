using System.ComponentModel;

namespace EverythingToolbar.Data
{
	class Filter
	{
		public string Name { get; set; }
		public bool IsMatchCase { get; set; }
		public bool IsMatchWholeWord { get; set; }
		public bool IsMatchPath { get; set; }
		public bool IsRegExEnabled { get; set; }
		public string Search { get; set; }
		public string Macro { get; set; }
	}
}
