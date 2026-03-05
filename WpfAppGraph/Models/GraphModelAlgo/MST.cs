using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region MST Algorithms (Kruskal & Prim)

        /// <summary>
        /// Построение минимального остовного дерева алгоритмом Крускала.
        /// </summary>
        /// <param name="result">Контейнер для результата выполнения.</param>
        /// <returns>Последовательность шагов для визуализации алгоритма.</returns>
        public IEnumerable<AlgorithmStep> RunKruskalAlgorithm(MstResult result)
        {
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                result.StatusMessage = "Граф пуст";
                yield break;
            }

            // Уникальные рёбра: фильтрация дублей для неориентированного графа
            var allEdges = new List<GraphEdge>();
            foreach (var kvp in _adjacencyList)
            {
                foreach (var edge in kvp.Value)
                {
                    if (edge.From < edge.To)
                    {
                        allEdges.Add(edge);
                    }
                }
            }

            allEdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            var dsu = new DisjointSet(vertices);
            result.MstLength = 0;

            yield return new AlgorithmStep
            {
                IterationInfo = $"Найдено {allEdges.Count} уникальных ребер. Сортировка..."
            };

            int edgesCount = 0;
            foreach (var edge in allEdges)
            {
                yield return new AlgorithmStep
                {
                    EdgeFromId = edge.From,
                    EdgeToId = edge.To,
                    NewEdgeType = EdgeType.ForwardEdge,
                };

                // Добавление ребра только при отсутствии цикла
                if (dsu.Find(edge.From) != dsu.Find(edge.To))
                {
                    dsu.Union(edge.From, edge.To);

                    result.MstLength += edge.Weight;
                    edgesCount++;

                    yield return new AlgorithmStep
                    {
                        EdgeFromId = edge.From,
                        EdgeToId = edge.To,
                        NewEdgeType = EdgeType.TreeEdge,
                        VertexId = edge.To,
                        NewVertexState = VertexState.Active,
                    };

                    yield return new AlgorithmStep
                    {
                        VertexId = edge.From,
                        NewVertexState = VertexState.Active
                    };
                }
                else
                {
                    // Пропуск ребра, замыкающего цикл
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = edge.From,
                        EdgeToId = edge.To,
                        NewEdgeType = EdgeType.Default,
                    };
                }
            }

            // Проверка полноты остова
            if (edgesCount == vertices.Count - 1 || (vertices.Count == 1 && edgesCount == 0))
            {
                result.IsSuccess = true;
                result.StatusMessage = $"Остов построен. Вес: {result.MstLength}";
            }
            else
            {
                // Несвязный граф: результат — остовный лес
                result.IsSuccess = true;
                result.StatusMessage = $"Построен остовный лес (граф несвязен). Вес: {result.MstLength}";
            }
        }

        /// <summary>
        /// Построение минимального остовного дерева алгоритмом Прима.
        /// </summary>
        /// <param name="result">Контейнер для результата выполнения.</param>
        /// <returns>Последовательность шагов для визуализации алгоритма.</returns>
        public IEnumerable<AlgorithmStep> RunPrimAlgorithm(MstResult result)
        {
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                result.StatusMessage = "Граф пуст";
                yield break;
            }

            result.MstLength = 0;

            var visited = new HashSet<int>();
            var edgeCandidates = new List<GraphEdge>();

            int startNode = vertices[0];
            visited.Add(startNode);

            if (_adjacencyList.ContainsKey(startNode))
                edgeCandidates.AddRange(_adjacencyList[startNode]);

            yield return new AlgorithmStep
            {
                VertexId = startNode,
                NewVertexState = VertexState.Selected,
                IterationInfo = "Старт"
            };

            while (visited.Count < vertices.Count && edgeCandidates.Count > 0)
            {
                // Поиск минимального ребра к непосещённой вершине
                GraphEdge bestEdge = null;
                double minWeight = double.MaxValue;

                for (int i = 0; i < edgeCandidates.Count; i++)
                {
                    var e = edgeCandidates[i];
                    if (visited.Contains(e.To)) continue;

                    if (e.Weight < minWeight)
                    {
                        minWeight = e.Weight;
                        bestEdge = e;
                    }
                }

                // Завершение при исчерпании кандидатов (несвязный граф)
                if (bestEdge == null) break;

                int newNode = bestEdge.To;
                visited.Add(newNode);
                result.MstLength += bestEdge.Weight;

                yield return new AlgorithmStep
                {
                    EdgeFromId = bestEdge.From,
                    EdgeToId = bestEdge.To,
                    NewEdgeType = EdgeType.TreeEdge,
                    VertexId = newNode,
                    NewVertexState = VertexState.Active,
                };

                // Пополнение списка кандидатов новыми рёбрами
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

        #region Вспомогательная структура

        /// <summary>
        /// Система непересекающихся множеств с эвристиками.
        /// </summary>
        private class DisjointSet
        {
            private Dictionary<int, int> _parent = new Dictionary<int, int>();
            private Dictionary<int, int> _rank = new Dictionary<int, int>();

            /// <summary>
            /// Инициализация DSU: каждая вершина — отдельное множество.
            /// </summary>
            /// <param name="vertices">Список вершин графа.</param>
            public DisjointSet(List<int> vertices)
            {
                foreach (var v in vertices)
                {
                    _parent[v] = v;
                    _rank[v] = 0;
                }
            }

            /// <summary>
            /// Поиск корня множества с применением сжатия пути.
            /// </summary>
            /// <param name="v">Искомая вершина.</param>
            /// <returns>Корневой представитель множества.</returns>
            public int Find(int v)
            {
                if (_parent[v] != v)
                    _parent[v] = Find(_parent[v]);
                return _parent[v];
            }

            /// <summary>
            /// Объединение двух множеств с использованием ранговой эвристики.
            /// </summary>
            /// <param name="v1">Первая вершина.</param>
            /// <param name="v2">Вторая вершина.</param>
            public void Union(int v1, int v2)
            {
                int root1 = Find(v1);
                int root2 = Find(v2);

                if (root1 != root2)
                {
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