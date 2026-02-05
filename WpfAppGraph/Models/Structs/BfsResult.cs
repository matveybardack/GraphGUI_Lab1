using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    // Класс для возврата итоговых результатов работы алгоритма
    public class BfsResult
    {
        public List<int> Path { get; set; } = new List<int>(); // Найденный путь (ID вершин)
        public double PathLength { get; set; } = 0;            // Длина пути (сумма весов)
        public string ParenthesisStructure { get; set; } = string.Empty; // Скобочная структура
        public bool IsTargetFound { get; set; } = false;
    }
}
