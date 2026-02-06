using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    public class EulerResult
    {
        public List<int> Path { get; set; } = new List<int>(); // Последовательность вершин
        public string StatusMessage { get; set; } = string.Empty; // "Эйлеров цикл", "Эйлеров путь" или ошибка
        public bool IsSuccess { get; set; } = false;
    }
}
