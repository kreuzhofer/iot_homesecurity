using System.Collections.Generic;

namespace W10Home.Plugin.ETATouch
{

	public class TreeItem
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public List<TreeItem> SubItems { get; set; }
    }
}
