using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region MST Algorithms (Kruskal & Prim)

        /// <summary>
        /// Алгоритм Крускала.
        /// Сортирует все ребра по весу и добавляет их, если они не образуют цикл (используя DSU).
        /// </summary>
        public IEnumerable<AlgorithmStep> RunKruskalAlgorithm(MstResult result)
        {
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                result.StatusMessage = "Граф пуст";
                yield break;
            }

            // 1. Подготовка: собираем все уникальные ребра
            // В неориентированном графе ребро A-B хранится дважды. Берем только то, где From < To.
            var allEdges = new List<GraphEdge>();
            foreach (var kvp in _adjacencyList)
            {
                foreach (var edge in kvp.Value)
                {
                    // Если граф ориентированный, берем все. Если нет — фильтруем дубликаты.
                    // Предположим для MST граф рассматривается как неориентированный.
                    if (edge.From < edge.To)
                    {
                        allEdges.Add(edge);
                    }
                }
            }

            // Сортируем ребра по возрастанию веса
            allEdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            // Инициализируем DSU (Disjoint Set Union) для всех вершин
            var dsu = new DisjointSet(vertices);
            result.MstLength = 0;

            yield return new AlgorithmStep
            {
                IterationInfo = $"Найдено {allEdges.Count} уникальных ребер. Сортировка..."
            };

            int edgesCount = 0;
            foreach (var edge in allEdges)
            {
                // Визуализация: рассматриваем текущее ребро (можно покрасить в желтый/выделенный)
                yield return new AlgorithmStep
                {
                    EdgeFromId = edge.From,
                    EdgeToId = edge.To,
                    NewEdgeType = EdgeType.ForwardEdge, // Временный цвет "рассматриваем"
                };

                // Проверяем, принадлежат ли вершины разным множествам
                if (dsu.Find(edge.From) != dsu.Find(edge.To))
                {
                    // Объединяем множества
                    dsu.Union(edge.From, edge.To);

                    // Добавляем в результат
                    result.MstLength += edge.Weight;
                    edgesCount++;

                    // Визуализация: ребро добавлено в остов
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = edge.From,
                        EdgeToId = edge.To,
                        NewEdgeType = EdgeType.TreeEdge, // Цвет остова
                        VertexId = edge.To,
                        NewVertexState = VertexState.Active, // Подсвечиваем вершины
                    };

                    // Также подсветим вторую вершину
                    yield return new AlgorithmStep
                    {
                        VertexId = edge.From,
                        NewVertexState = VertexState.Active
                    };
                }
                else
                {
                    // Ребро образует цикл — пропускаем
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = edge.From,
                        EdgeToId = edge.To,
                        NewEdgeType = EdgeType.Default, // Возвращаем обычный цвет (или можно красить в бледный)
                    };
                }
            }

            // Проверка связности (опционально): если edgesCount == vertices.Count - 1, то остов полон
            if (edgesCount == vertices.Count - 1 || (vertices.Count == 1 && edgesCount == 0))
            {
                result.IsSuccess = true;
                result.StatusMessage = $"Остов построен. Вес: {result.MstLength}";
            }
            else
            {
                // Если граф был несвязным, мы получим лес (Minimum Spanning Forest)
                result.IsSuccess = true;
                result.StatusMessage = $"Построен остовный лес (граф несвязен). Вес: {result.MstLength}";
            }
        }

        /// <summary>
        /// Алгоритм Прима.
        /// Начинает с произвольной вершины и жадно добавляет ближайшую вершину к растущему дереву.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunPrimAlgorithm(MstResult result)
        {
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                result.StatusMessage = "Граф пуст";
                yield break;
            }

            result.MstLength = 0;

            // Множество посещенных вершин (включенных в остов)
            var visited = new HashSet<int>();

            // Список кандидатов-ребер: ребра, исходящие из "дерева" во "вне"
            // Для упрощения визуализации используем просто список, хотя PriorityQueue эффективнее.
            var edgeCandidates = new List<GraphEdge>();

            // Начинаем с первой вершины
            int startNode = vertices[0];
            visited.Add(startNode);

            // Добавляем инцидентные ребра стартовой вершины
            if (_adjacencyList.ContainsKey(startNode))
                edgeCandidates.AddRange(_adjacencyList[startNode]);

            yield return new AlgorithmStep
            {
                VertexId = startNode,
                NewVertexState = VertexState.Selected, // Корень остова
                IterationInfo = "Старт"
            };

            // Пока не посетим все вершины (или пока есть доступные ребра для несвязных графов)
            while (visited.Count < vertices.Count && edgeCandidates.Count > 0)
            {
                // 1. Ищем ребро с минимальным весом среди кандидатов, 
                // которое ведет в НЕПОСЕЩЕННУЮ вершину.
                GraphEdge bestEdge = null;
                double minWeight = double.MaxValue;
                int bestEdgeIndex = -1;

                // Фильтруем и ищем минимум вручную для наглядности (или через LINQ)
                // Нам нужно ребро (u, v), где u in visited, v NOT in visited.
                // Так как список candidates содержит ребра "от" visited вершин, проверяем только edge.To

                // Важно: edgeCandidates могут содержать устаревшие ребра (где To уже visited), их надо игнорировать
                for (int i = 0; i < edgeCandidates.Count; i++)
                {
                    var e = edgeCandidates[i];
                    if (visited.Contains(e.To)) continue; // Оба конца уже в дереве

                    if (e.Weight < minWeight)
                    {
                        minWeight = e.Weight;
                        bestEdge = e;
                        bestEdgeIndex = i;
                    }
                }

                // Если не нашли подходящего ребра, значит текущая компонента построена
                if (bestEdge == null) break;

                // 2. Добавляем вершину и ребро в остов
                int newNode = bestEdge.To;
                visited.Add(newNode);
                result.MstLength += bestEdge.Weight;

                // Анимация добавления
                yield return new AlgorithmStep
                {
                    EdgeFromId = bestEdge.From,
                    EdgeToId = bestEdge.To,
                    NewEdgeType = EdgeType.TreeEdge,
                    VertexId = newNode,
                    NewVertexState = VertexState.Active,
                };

                // 3. Добавляем новые ребра от newNode в список кандидатов
                if (_adjacencyList.ContainsKey(newNode))
                {
                    foreach (var edge in _adjacencyList[newNode])
                    {
                        if (!visited.Contains(edge.To))
                        {
                            edgeCandidates.Add(edge);
                        }
                    }
                }
            }

            if (visited.Count == vertices.Count)
            {
                result.IsSuccess = true;
                result.StatusMessage = $"Остов построен. Вес: {result.MstLength}";
            }
            else
            {
                result.IsSuccess = true;
                result.StatusMessage = $"Частичный остов (граф несвязен). Посещено: {visited.Count}/{vertices.Count}";
            }
        }

        #region Helpers (DSU)

        // Вложенный класс для системы непересекающихся множеств
        private class DisjointSet
        {
            private Dictionary<int, int> _parent = new Dictionary<int, int>();
            private Dictionary<int, int> _rank = new Dictionary<int, int>();

            public DisjointSet(List<int> vertices)
            {
                foreach (var v in vertices)
                {
                    _parent[v] = v; // Изначально каждый сам себе родитель
                    _rank[v] = 0;
                }
            }

            public int Find(int v)
            {
                if (_parent[v] != v)
                    _parent[v] = Find(_parent[v]); // Сжатие пути
                return _parent[v];
            }

            public void Union(int v1, int v2)
            {
                int root1 = Find(v1);
                int root2 = Find(v2);

                if (root1 != root2)
                {
                    // Ранговая эвристика
                    if (_rank[root1] < _rank[root2])
                    {
                        _parent[root1] = root2;
                    }
                    else if (_rank[root1] > _rank[root2])
                    {
                        _parent[root2] = root1;
                    }
                    else
                    {
                        _parent[root2] = root1;
                        _rank[root1]++;
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
