using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region Fundamental Cycles

        /// <summary>
        /// Поиск фундаментальных циклов графа через построение остовного дерева.
        /// </summary>
        /// <param name="result">Контейнер для результата выполнения.</param>
        /// <param name="method">Способ обхода графа для построения остова (BFS/DFS).</param>
        /// <returns>Последовательность шагов для визуализации алгоритма.</returns>
        public IEnumerable<AlgorithmStep> RunFundamentalCyclesAlgorithm(FundamentalCyclesResult result, GraphTraversalType method)
        {
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                result.StatusMessage = "Граф пуст";
                yield break;
            }

            int startNode = vertices[0];

            // Отображение потомок -> родитель для восстановления путей
            var parentMap = new Dictionary<int, int>();
            // Рёбра остова в нормализованном виде (min, max) для неориентированного сравнения
            var treeEdges = new HashSet<(int, int)>();

            result.Components.Clear();
            result.StatusMessage = $"Построение остовного дерева методом {method}...";

            IEnumerable<AlgorithmStep> treeSteps;
            if (method == GraphTraversalType.BFS)
            {
                treeSteps = BuildSpanningTreeBFS(startNode, parentMap, treeEdges);
            }
            else
            {
                treeSteps = BuildSpanningTreeDFS(startNode, parentMap, treeEdges);
            }

            foreach (var step in treeSteps)
            {
                yield return step;
            }

            result.StatusMessage = "Поиск хорд и восстановление циклов...";

            // Обработка рёбер: отсутствие в остове означает хорду
            // Избегание дублирования циклов для неориентированных графов
            var processedChords = new HashSet<(int, int)>();

            foreach (var u in _adjacencyList.Keys)
            {
                foreach (var edge in _adjacencyList[u])
                {
                    int v = edge.To;

                    var edgeKey = u < v ? (u, v) : (v, u);

                    if (treeEdges.Contains(edgeKey))
                        continue;

                    if (processedChords.Contains(edgeKey))
                        continue;

                    // Исключение рёбер остова, хранящихся в обратном порядке
                    if (parentMap.ContainsKey(u) && parentMap[u] == v) continue;
                    if (parentMap.ContainsKey(v) && parentMap[v] == u) continue;

                    processedChords.Add(edgeKey);

                    yield return new AlgorithmStep
                    {
                        EdgeFromId = u,
                        EdgeToId = v,
                        NewEdgeType = EdgeType.BackEdge,
                        IterationInfo = $"Хорда {u}-{v}"
                    };

                    // Цикл = хорда (u,v) + путь в остове между u и v
                    var cyclePath = FindCyclePathInTree(u, v, parentMap);

                    if (cyclePath != null && cyclePath.Count > 0)
                    {
                        result.Components.Add(cyclePath);

                        yield return new AlgorithmStep
                        {
                            IterationInfo = $"Цикл найден ({cyclePath.Count} вершин)",
                        };
                    }
                }
            }

            result.IsSuccess = true;
            result.StatusMessage = $"Готово. Найдено циклов: {result.Components.Count}";
        }

        /// <summary>
        /// Построение остовного дерева обходом в ширину (BFS).
        /// </summary>
        /// <param name="startNode">Стартовая вершина обхода.</param>
        /// <param name="parentMap">Словарь для заполнения: потомок -> родитель.</param>
        /// <param name="treeEdges">Множество для заполнения рёбрами остова.</param>
        /// <returns>Последовательность шагов для визуализации.</returns>
        private IEnumerable<AlgorithmStep> BuildSpanningTreeBFS(int startNode, Dictionary<int, int> parentMap, HashSet<(int, int)> treeEdges)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            visited.Add(startNode);
            queue.Enqueue(startNode);
            parentMap[startNode] = -1;

            yield return new AlgorithmStep { VertexId = startNode, NewVertexState = VertexState.Selected, IterationInfo = "Корень (BFS)" };

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();

                if (_adjacencyList.ContainsKey(u))
                {
                    // Детерминированный порядок обхода соседей
                    foreach (var edge in _adjacencyList[u].OrderBy(e => e.To))
                    {
                        int v = edge.To;
                        if (!visited.Contains(v))
                        {
                            visited.Add(v);
                            parentMap[v] = u;
                            queue.Enqueue(v);

                            treeEdges.Add(u < v ? (u, v) : (v, u));

                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.TreeEdge,
                                VertexId = v,
                                NewVertexState = VertexState.Active,
                                IterationInfo = $"Tree Edge {u}->{v}"
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Построение остовного дерева обходом в глубину (DFS).
        /// </summary>
        /// <param name="startNode">Стартовая вершина обхода.</param>
        /// <param name="parentMap">Словарь для заполнения: потомок -> родитель.</param>
        /// <param name="treeEdges">Множество для заполнения рёбрами остова.</param>
        /// <returns>Последовательность шагов для визуализации.</returns>
        private IEnumerable<AlgorithmStep> BuildSpanningTreeDFS(int startNode, Dictionary<int, int> parentMap, HashSet<(int, int)> treeEdges)
        {
            var visited = new HashSet<int>();
            var stack = new Stack<int>();

            visited.Add(startNode);
            parentMap[startNode] = -1;

            yield return new AlgorithmStep { VertexId = startNode, NewVertexState = VertexState.Selected, IterationInfo = "Корень (DFS)" };

            while (stack.Count > 0)
            {
                int u = stack.Pop();

                if (_adjacencyList.ContainsKey(u))
                {
                    foreach (var edge in _adjacencyList[u])
                    {
                        int v = edge.To;
                        if (!visited.Contains(v))
                        {
                            visited.Add(v);
                            parentMap[v] = u;
                            stack.Push(v);

                            treeEdges.Add(u < v ? (u, v) : (v, u));

                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.TreeEdge,
                                VertexId = v,
                                NewVertexState = VertexState.Active,
                                IterationInfo = $"Tree Edge {u}->{v}"
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Восстановление пути между двумя вершинами в остовном дереве через LCA.
        /// </summary>
        /// <param name="startNode">Первая вершина.</param>
        /// <param name="endNode">Вторая вершина.</param>
        /// <param name="parentMap">Отображение потомок -> родитель для навигации по дереву.</param>
        /// <returns>Список вершин, образующих путь от startNode до endNode.</returns>
        private List<int> FindCyclePathInTree(int startNode, int endNode, Dictionary<int, int> parentMap)
        {
            // Построение путей от вершин к корню дерева
            var path1 = new List<int>();
            int curr = startNode;
            while (curr != -1 && parentMap.ContainsKey(curr))
            {
                path1.Add(curr);
                if (parentMap[curr] == -1) break;
                curr = parentMap[curr];
            }
            if (curr != -1 && !path1.Contains(curr)) path1.Add(curr);

            var path2 = new List<int>();
            curr = endNode;
            while (curr != -1 && parentMap.ContainsKey(curr))
            {
                path2.Add(curr);
                if (parentMap[curr] == -1) break;
                curr = parentMap[curr];
            }
            if (curr != -1 && !path2.Contains(curr)) path2.Add(curr);

            // Поиск ближайшего общего предка через пересечение путей
            int lca = -1;
            var path1Set = new HashSet<int>(path1);
            foreach (var node in path2)
            {
                if (path1Set.Contains(node))
                {
                    lca = node;
                    break;
                }
            }

            if (lca == -1) return new List<int>();

            // Формирование цикла: путь от startNode до LCA
            var cycle = new List<int>();
            foreach (var node in path1)
            {
                cycle.Add(node);
                if (node == lca) break;
            }

            // Добавление пути от LCA до endNode (в обратном порядке)
            var tempPath = new List<int>();
            foreach (var node in path2)
            {
                if (node == lca) break;
                tempPath.Add(node);
            }
            tempPath.Reverse();
            cycle.AddRange(tempPath);

            return cycle;
        }

        #endregion
    }
}