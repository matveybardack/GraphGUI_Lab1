using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    public class SccResult
    {
        public List<List<int>> Components { get; set; } = new List<List<int>>();
        public int ComponentCount => Components.Count;
    }
}
