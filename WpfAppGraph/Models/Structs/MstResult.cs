using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    public class MstResult
    {
        public bool IsSuccess { get; set; }
        public string StatusMessage { get; set; }
        public double MstLength { get; set; } = 0;
    }
}
