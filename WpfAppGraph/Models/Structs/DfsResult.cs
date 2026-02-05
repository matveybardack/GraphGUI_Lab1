using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    public class DfsResult
    {
        public List<int> Path { get; set; } = new List<int>(); // Путь до цели (если найдена)
        public double PathLength { get; set; } = 0;            // Длина пути
        public string ParenthesisStructure { get; set; } = string.Empty; // Скобочная формула
        public bool IsTargetFound { get; set; } = false;

        // Для классификации ребер можно добавить счетчики или списки, 
        // но пока визуализации на холсте достаточно.
    }
}
