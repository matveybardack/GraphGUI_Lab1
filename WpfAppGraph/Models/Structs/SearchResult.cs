using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    /// <summary>
    /// Класс для возврата итоговых результатов работы алгоритма
    /// </summary>
    public class SearchResult
    {
        public List<int> Path { get; set; } = new List<int>();
        public double PathLength { get; set; } = 0;
        public string ParenthesisStructure { get; set; } = string.Empty;
        public bool IsTargetFound { get; set; } = false;
    }
}
