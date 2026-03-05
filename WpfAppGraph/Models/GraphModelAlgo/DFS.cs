using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region DFS

        /// <summary>
        /// DFS на стеке
        /// </summary>
        /// <param name="startId">ID стартовой вершины</param>
        /// <param name="targetId">ID целевой вершины (может быть null)</param>
        /// <param name="result">Объект для записи итоговых результатов</param>
        /// <returns>Ленивая коллекция шагов для анимации</returns>
        public IEnumerable<AlgorithmStep> RunDfsIterative(int? startId, int? targetId, SearchResult result)
        {
            // Инициализация структур
            var visited = new HashSet<int>();
            var parentMap = new Dictionary<int, int>();
            var discoveryTime = new Dictionary<int, int>();
            var finishTime = new Dictionary<int, int>();
            var stack = new Stack<int>();

            int timer = 1;
            StringBuilder structBuilder = new StringBuilder();
            bool globalTargetFound = false;

            var allVertices = GetVertices();
            if (startId.HasValue && allVertices.Contains(startId.Value))
            {
                allVertices.Remove(startId.Value);
                allVertices.Insert(0, startId.Value);
            }

            foreach (var root in allVertices)
            {
                // Если вершина уже посещена в предыдущей компоненте
                if (visited.Contains(root))
                    continue;

                stack.Push(root);

                while (stack.Count > 0)
                {
                    int u = stack.Peek();

                    // Начало исследования вершины
                    if (!visited.Contains(u))
                    {
                        visited.Add(u);
                        discoveryTime[u] = timer++;
                        structBuilder.Append($"({u} ");

                        yield return new AlgorithmStep
                        {
                            VertexId = u,
                            NewVertexState = VertexState.Active,
                            IterationInfo = $"{discoveryTime[u]}/-"
                        };

                        if (targetId.HasValue && u == targetId.Value)
                            globalTargetFound = true;
                    }

                    // поиск соседей
                    bool hasUnvisitedNeighbor = false;

                    if (_adjacencyList.ContainsKey(u))
                    {
                        var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();

                        foreach (var edge in neighbors)
                        {
                            // исследование непосещенного соседа
                            int v = edge.To;
                            if (!visited.Contains(v))
                            {
                                parentMap[v] = u;
                                stack.Push(v);
                                hasUnvisitedNeighbor = true;

                                yield return new AlgorithmStep
                                {
                                    EdgeFromId = u,
                                    EdgeToId = v,
                                    NewEdgeType = EdgeType.TreeEdge
                                };

                                break;
                            }
                        }
                    }

                    // конец исследования вершины
                    if (!hasUnvisitedNeighbor)
                    {
                        stack.Pop();
                        finishTime[u] = timer++;
                        structBuilder.Append($") ");

                        yield return new AlgorithmStep
                        {
                            VertexId = u,
                            NewVertexState = VertexState.Finished,
                            IterationInfo = $"{discoveryTime[u]}/{finishTime[u]}"
                        };
                    }
                }
            }

            result.IsTargetFound = globalTargetFound;
            result.ParenthesisStructure = structBuilder.ToString().Trim();

            if (globalTargetFound && targetId.HasValue)
            {
                var tempPath = new List<int>();
                int curr = targetId.Value;
                tempPath.Add(curr);

                while (parentMap.ContainsKey(curr))
                {
                    int p = parentMap[curr];

                    if (_adjacencyList.ContainsKey(p))
                    {
                        var edge = _adjacencyList[p].FirstOrDefault(e => e.To == curr);
                        if (edge != null)
                            result.PathLength += edge.Weight;
                    }

                    curr = p;
                    tempPath.Add(curr);
                }

                // валидность пути
                bool pathIsValid = true;
                if (startId.HasValue && curr != startId.Value)
                {
                    pathIsValid = false;
                }

                if (pathIsValid)
                {
                    tempPath.Reverse();
                    result.Path = tempPath;
                }
                else
                {
                    result.Path.Clear();
                    result.PathLength = 0;
                }
            }
        }

        /// <summary>
        /// Алгоритм dfs с покраской вершин
        /// </summary>
        /// <param name="startId">ID стартовой вершины</param>
        /// <param name="targetId">ID целевой вершины (может быть null)</param>
        /// <param name="result">Объект для записи итоговых результатов</param>
        /// <returns>Ленивая коллекция шагов для анимации</returns>
        public IEnumerable<AlgorithmStep> RunDfsRecursive(int? startId, int? targetId, SearchResult result)
        {
            // Инициализация структур данных
            var visited = new HashSet<int>();
            var parentMap = new Dictionary<int, int>();
            var discoveryTime = new Dictionary<int, int>();
            var finishTime = new Dictionary<int, int>();

            int timer = 1;
            StringBuilder structBuilder = new StringBuilder();
            bool globalTargetFound = false;

            var allVertices = GetVertices();
            if (startId.HasValue && allVertices.Contains(startId.Value))
            {
                allVertices.Remove(startId.Value);
                allVertices.Insert(0, startId.Value);
            }

            // Локальная функция для рекурсивного обхода
            IEnumerable<AlgorithmStep> DfsVisit(int u)
            {
                visited.Add(u);
                discoveryTime[u] = timer++;
                structBuilder.Append($"({u} ");

                yield return new AlgorithmStep
                {
                    VertexId = u,
                    NewVertexState = VertexState.Active,
                    IterationInfo = $"{discoveryTime[u]}/-"
                };

                if (targetId.HasValue && u == targetId.Value)
                {
                    globalTargetFound = true;
                }

                if (_adjacencyList.ContainsKey(u))
                {
                    // Сортировка
                    var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();

                    foreach (var edge in neighbors)
                    {
                        int v = edge.To;

                        // дерево
                        if (!visited.Contains(v))
                        {
                            parentMap[v] = u; // родитель для пути

                            // Анимация перехода
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.TreeEdge
                            };

                            // Рекурсивный спуск
                            foreach (var step in DfsVisit(v))
                            {
                                yield return step;
                            }
                        }
                        else
                        {
                            // Классификация оставшихся ребер
                            EdgeType type = EdgeType.Default;

                            // Это обратное ребро
                            if (!finishTime.ContainsKey(v))
                            {
                                type = EdgeType.BackEdge;
                            }
                            else
                            {
                                // (d[u] < d[v]), то Прямое ребро.
                                // Иначе это перекрестное ребро.
                                if (discoveryTime[u] < discoveryTime[v])
                                    type = EdgeType.ForwardEdge;
                                else
                                    type = EdgeType.CrossEdge;
                            }

                            // Анимация классифицированного ребра
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = type
                            };
                        }
                    }
                }

                finishTime[u] = timer++;
                structBuilder.Append($") ");

                yield return new AlgorithmStep
                {
                    VertexId = u,
                    NewVertexState = VertexState.Finished,
                    IterationInfo = $"{discoveryTime[u]}/{finishTime[u]}"
                };
            }

            // Внешний цикл для несвязных графов
            foreach (var root in allVertices)
            {
                if (!visited.Contains(root))
                {
                    foreach (var step in DfsVisit(root))
                    {
                        yield return step;
                    }
                }
            }

            // Формирование результатов
            result.IsTargetFound = globalTargetFound;
            result.ParenthesisStructure = structBuilder.ToString().Trim();

            if (globalTargetFound && targetId.HasValue)
            {
                var tempPath = new List<int>();
                int curr = targetId.Value;
                tempPath.Add(curr);

                while (parentMap.ContainsKey(curr))
                {
                    int p = parentMap[curr];

                    if (_adjacencyList.ContainsKey(p))
                    {
                        var edge = _adjacencyList[p].FirstOrDefault(e => e.To == curr);
                        if (edge != null)
                            result.PathLength += edge.Weight;
                    }

                    curr = p;
                    tempPath.Add(curr);
                }

                bool pathIsValid = true;
                if (startId.HasValue && curr != startId.Value)
                {
                    pathIsValid = false;
                }

                if (pathIsValid)
                {
                    tempPath.Reverse();
                    result.Path = tempPath;
                }
                else
                {
                    result.Path.Clear();
                    result.PathLength = 0;
                }
            }
        }
        #endregion
    }
}
