using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region BFS

        /// <summary>
        /// Запускает поиск в ширину (BFS).
        /// </summary>
        /// <param name="startId">ID стартовой вершины</param>
        /// <param name="targetId">ID целевой вершины (может быть null)</param>
        /// <param name="result">Объект для записи итоговых результатов</param>
        /// <returns>Ленивая коллекция шагов для анимации</returns>
        public IEnumerable<AlgorithmStep> RunBfs(int? startId, int? targetId, SearchResult result)
        {
            var allVertices = GetVertices();

            if (startId.HasValue)
            {
                if (allVertices.Contains(startId.Value))
                {
                    allVertices.Remove(startId.Value);
                    allVertices.Insert(0, startId.Value);
                }
            }

            // Инициализация структур данных
            var queue = new Queue<int>();
            var parentMap = new Dictionary<int, int>();
            var discoveryTime = new Dictionary<int, int>();
            var finishTime = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            int timer = 1;
            StringBuilder structBuilder = new StringBuilder();
            bool globalTargetFound = false;

            foreach (var root in allVertices)
            {
                if (visited.Contains(root))
                    continue;

                // Начало алгоритма для текущей компоненты связности
                queue.Enqueue(root);
                visited.Add(root);
                discoveryTime[root] = timer++;

                structBuilder.Append($"({root} ");

                yield return new AlgorithmStep
                {
                    VertexId = root,
                    NewVertexState = VertexState.Active,
                    IterationInfo = $"{discoveryTime[root]}/-"
                };

                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();

                    if (targetId.HasValue && u == targetId.Value)
                        globalTargetFound = true;

                    if (_adjacencyList.ContainsKey(u))
                    {
                        var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();

                        foreach (var edge in neighbors)
                        {
                            int v = edge.To;
                            if (!visited.Contains(v))
                            {
                                visited.Add(v);
                                parentMap[v] = u; // родитель
                                discoveryTime[v] = timer++;
                                structBuilder.Append($"({v} ");

                                queue.Enqueue(v);

                                yield return new AlgorithmStep
                                {
                                    VertexId = v,
                                    NewVertexState = VertexState.Visited,
                                    IterationInfo = $"{discoveryTime[v]}/-",
                                    EdgeFromId = u,
                                    EdgeToId = v,
                                    NewEdgeType = EdgeType.TreeEdge
                                };

                                if (targetId.HasValue && v == targetId.Value)
                                    globalTargetFound = true;
                            }
                        }
                    }

                    // Завершение обработки вершины
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

                // действительно ли путь идет от startId
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
