using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    public class FundamentalCyclesResult
    {
        public bool IsSuccess { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public List<List<int>> Components { get; set; } = new List<List<int>>();
    }
}
