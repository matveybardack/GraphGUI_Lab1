using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region Euler cycle

        /// <summary>
        /// Проверяет граф на наличие Эйлерова цикла или пути.
        /// </summary>
        /// <param name="isCycle">Признак наличия цикла (true) или пути (false).</param>
        /// <param name="message">Текстовое описание результата проверки.</param>
        /// <returns>Индекс стартовой вершины для построения маршрута, или -1 при ошибке.</returns>
        public int CheckEulerian(out bool isCycle, out string message)
        {
            isCycle = false;
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                message = "Граф пуст.";
                return -1;
            }

            var degrees = CalculateDegrees(out bool isDirected, out int totalEdges);

            if (totalEdges == 0)
            {
                message = "В графе нет ребер.";
                return -1;
            }

            var activeVertices = vertices.Where(v =>
                (degrees.ContainsKey(v) && (degrees[v].In > 0 || degrees[v].Out > 0))
            ).ToList();

            if (activeVertices.Count == 0)
            {
                message = "Нет активных вершин.";
                return -1;
            }

            if (!IsGraphConnectedBasedOnEdges(activeVertices[0], activeVertices.Count, isDirected))
            {
                message = "Граф несвязен (содержит несколько компонент с ребрами).";
                return -1;
            }

            if (isDirected)
            {
                return CheckDirectedDegrees(vertices, degrees, out isCycle, out message);
            }
            else
            {
                return CheckUndirectedDegrees(vertices, degrees, out isCycle, out message);
            }
        }

        /// <summary>
        /// Построение Эйлерова пути/цикла алгоритмом Флёри.
        /// </summary>
        /// <param name="result">Контейнер для результата выполнения.</param>
        /// <returns>Последовательность шагов для визуализации алгоритма.</returns>
        public IEnumerable<AlgorithmStep> RunFleuryAlgorithm(EulerResult result)
        {
            // Алгоритм предназначен для неориентированных графов
            int startNode = CheckEulerian(out bool isCycle, out string msg);
            result.StatusMessage = msg;

            if (startNode == -1) yield break;

            bool isDirected = _adjacencyList.Values.Any(l => l.Any(e => e.IsDirected));
            if (isDirected)
            {
                result.StatusMessage = "Алгоритм Флёри не гарантирует результат на ориентированном графе. Рекомендуется Хирхольцер.";
            }

            // Рабочая копия графа для удаления рёбер
            var tempAdj = CloneAdjacencyList();
            var path = new List<int> { startNode };
            int u = startNode;

            yield return new AlgorithmStep
            {
                VertexId = u,
                NewVertexState = VertexState.Selected,
                IterationInfo = "Старт"
            };

            while (tempAdj.ContainsKey(u) && tempAdj[u].Count > 0)
            {
                var neighbors = tempAdj[u];
                GraphEdge chosenEdge = null;
                int v = -1;

                if (neighbors.Count == 1)
                {
                    chosenEdge = neighbors[0];
                    v = chosenEdge.To;
                }
                else
                {
                    // Выбор ребра, не являющегося мостом
                    foreach (var edge in neighbors)
                    {
                        if (!IsBridge(u, edge, tempAdj, isDirected))
                        {
                            chosenEdge = edge;
                            v = edge.To;
                            break;
                        }
                    }

                    // Все рёбра — мосты: выбор произвольного
                    if (chosenEdge == null)
                    {
                        chosenEdge = neighbors[0];
                        v = chosenEdge.To;
                    }
                }

                yield return new AlgorithmStep
                {
                    EdgeFromId = u,
                    EdgeToId = v,
                    NewEdgeType = EdgeType.BackEdge,
                    VertexId = v,
                    NewVertexState = VertexState.Active,
                    IterationInfo = $"Переход {u}->{v}"
                };

                // Удаление пройденного ребра
                RemoveEdgeFromTemp(tempAdj, u, v, chosenEdge);

                u = v;
                path.Add(u);
            }

            result.Path = path;
            result.IsSuccess = true;
            result.StatusMessage += " (Завершено)";
        }

        /// <summary>
        /// Построение Эйлерова пути/цикла алгоритмом Хирхольцера.
        /// </summary>
        /// <param name="result">Контейнер для результата выполнения.</param>
        /// <returns>Последовательность шагов для визуализации алгоритма.</returns>
        public IEnumerable<AlgorithmStep> RunHierholzerAlgorithm(EulerResult result)
        {
            int startNode = CheckEulerian(out bool isCycle, out string msg);
            result.StatusMessage = msg;
            if (startNode == -1) yield break;

            var tempAdj = CloneAdjacencyList();
            var stack = new Stack<int>();
            var circuit = new List<int>();

            stack.Push(startNode);

            yield return new AlgorithmStep
            {
                VertexId = startNode,
                NewVertexState = VertexState.Selected,
                IterationInfo = "Старт (Стек)"
            };

            while (stack.Count > 0)
            {
                int u = stack.Peek();

                if (tempAdj.ContainsKey(u) && tempAdj[u].Count > 0)
                {
                    var edge = tempAdj[u][0];
                    int v = edge.To;

                    stack.Push(v);
                    RemoveEdgeFromTemp(tempAdj, u, v, edge);

                    yield return new AlgorithmStep
                    {
                        EdgeFromId = u,
                        EdgeToId = v,
                        NewEdgeType = EdgeType.BackEdge,
                        VertexId = v,
                        NewVertexState = VertexState.Active,
                        IterationInfo = "Вглубь"
                    };
                }
                else
                {
                    // Фиксация вершины в итоговом маршруте
                    int finishedNode = stack.Pop();
                    circuit.Add(finishedNode);

                    yield return new AlgorithmStep
                    {
                        VertexId = finishedNode,
                        NewVertexState = VertexState.Finished,
                        IterationInfo = "В цикл"
                    };
                }
            }

            // Инверсия пути (построение с конца)
            circuit.Reverse();
            result.Path = circuit;
            result.IsSuccess = true;
            result.StatusMessage += " (Завершено)";
        }
        #endregion
        #region Вспомогательные структуры и методы

        // Вспомогательная структура для учёта степеней вершин
        private class VertexDegree { public int In; public int Out; }

        /// <summary>
        /// Вычисляет полустепени захода и исхода для всех вершин графа.
        /// </summary>
        /// <param name="isDirected">Признак ориентированности графа.</param>
        /// <param name="totalEdges">Общее количество рёбер.</param>
        /// <returns>Словарь степеней по идентификаторам вершин.</returns>
        private Dictionary<int, VertexDegree> CalculateDegrees(out bool isDirected, out int totalEdges)
        {
            var degrees = new Dictionary<int, VertexDegree>();
            isDirected = false;
            totalEdges = 0;

            foreach (var v in GetVertices())
                degrees[v] = new VertexDegree();

            foreach (var kvp in _adjacencyList)
            {
                foreach (var edge in kvp.Value)
                {
                    totalEdges++;
                    if (edge.IsDirected) isDirected = true;

                    degrees[kvp.Key].Out++;
                    if (degrees.ContainsKey(edge.To))
                        degrees[edge.To].In++;
                    else
                        degrees[edge.To] = new VertexDegree { In = 1 };
                }
            }
            return degrees;
        }

        /// <summary>
        /// Проверка условий Эйлера для ориентированного графа.
        /// </summary>
        /// <param name="vertices">Список вершин графа.</param>
        /// <param name="degrees">Словарь степеней вершин.</param>
        /// <param name="isCycle">Признак наличия цикла (true) или пути (false).</param>
        /// <param name="message">Текстовое описание результата.</param>
        /// <returns>Индекс стартовой вершины, или -1 при невозможности построения.</returns>
        private int CheckDirectedDegrees(List<int> vertices, Dictionary<int, VertexDegree> degrees, out bool isCycle, out string message)
        {
            int startNodes = 0;
            int endNodes = 0;
            int startNodeCandidate = -1;

            foreach (var v in vertices)
            {
                int diff = degrees[v].Out - degrees[v].In;
                if (diff == 1)
                {
                    startNodes++;
                    startNodeCandidate = v;
                }
                else if (diff == -1)
                {
                    endNodes++;
                }
                else if (diff != 0)
                {
                    message = $"Нарушен баланс степеней в вершине {v} (In:{degrees[v].In}, Out:{degrees[v].Out})";
                    isCycle = false;
                    return -1;
                }
            }

            if (startNodes == 0 && endNodes == 0)
            {
                isCycle = true;
                message = "Найден Эйлеров цикл";
                return vertices.FirstOrDefault(v => degrees[v].Out > 0);
            }
            else if (startNodes == 1 && endNodes == 1)
            {
                isCycle = false;
                message = "Найден Эйлеров путь";
                return startNodeCandidate;
            }

            isCycle = false;
            message = "Невозможно построить Эйлеров путь/цикл.";
            return -1;
        }

        /// <summary>
        /// Проверка условий Эйлера для неориентированного графа.
        /// </summary>
        /// <param name="vertices">Список вершин графа.</param>
        /// <param name="degrees">Словарь степеней вершин.</param>
        /// <param name="isCycle">Признак наличия цикла (true) или пути (false).</param>
        /// <param name="message">Текстовое описание результата.</param>
        /// <returns>Индекс стартовой вершины, или -1 при невозможности построения.</returns>
        private int CheckUndirectedDegrees(List<int> vertices, Dictionary<int, VertexDegree> degrees, out bool isCycle, out string message)
        {
            int oddCount = 0;
            int startNodeCandidate = -1;
            int firstNodeWithEdges = -1;

            foreach (var v in vertices)
            {
                int d = degrees[v].Out;
                if (d > 0 && firstNodeWithEdges == -1) firstNodeWithEdges = v;

                if (d % 2 != 0)
                {
                    oddCount++;
                    startNodeCandidate = v;
                }
            }

            if (oddCount == 0)
            {
                isCycle = true;
                message = "Найден Эйлеров цикл (Undirected).";
                return firstNodeWithEdges != -1 ? firstNodeWithEdges : vertices[0];
            }
            else if (oddCount == 2)
            {
                isCycle = false;
                message = "Найден Эйлеров путь (Undirected).";
                return startNodeCandidate;
            }

            isCycle = false;
            message = $"Нечетных вершин: {oddCount}. Требуется 0 или 2.";
            return -1;
        }

        /// <summary>
        /// Проверка слабой связности графа по рёбрам (игнорируя изолированные вершины).
        /// </summary>
        /// <param name="startNode">Стартовая вершина для обхода.</param>
        /// <param name="expectedCount">Ожидаемое количество активных вершин.</param>
        /// <param name="isDirected">Признак ориентированности графа.</param>
        /// <returns>Признак связности всех рёбер в одной компоненте.</returns>
        private bool IsGraphConnectedBasedOnEdges(int startNode, int expectedCount, bool isDirected)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();

                // Обход исходящих рёбер
                if (_adjacencyList.ContainsKey(u))
                {
                    foreach (var edge in _adjacencyList[u])
                    {
                        if (!visited.Contains(edge.To))
                        {
                            visited.Add(edge.To);
                            queue.Enqueue(edge.To);
                        }
                    }
                }

                // Учёт входящих рёбер для слабой связности ориентированного графа
                if (isDirected)
                {
                    foreach (var kvp in _adjacencyList)
                    {
                        if (visited.Contains(kvp.Key)) continue;
                        if (kvp.Value.Any(e => e.To == u))
                        {
                            visited.Add(kvp.Key);
                            queue.Enqueue(kvp.Key);
                        }
                    }
                }
            }

            return visited.Count == expectedCount;
        }

        /// <summary>
        /// Создаёт глубокую копию списка смежности для временных вычислений.
        /// </summary>
        /// <returns>Клонированная структура смежности.</returns>
        private Dictionary<int, List<GraphEdge>> CloneAdjacencyList()
        {
            var clone = new Dictionary<int, List<GraphEdge>>();
            foreach (var kvp in _adjacencyList)
            {
                clone[kvp.Key] = new List<GraphEdge>();
                foreach (var e in kvp.Value)
                {
                    clone[kvp.Key].Add(new GraphEdge(e.From, e.To, e.Weight, e.IsDirected));
                }
            }
            return clone;
        }

        /// <summary>
        /// Удаляет ребро из временной структуры смежности.
        /// </summary>
        /// <param name="adj">Временный список смежности.</param>
        /// <param name="u">Исходящая вершина.</param>
        /// <param name="v">Целевая вершина.</param>
        /// <param name="specificEdge">Удаляемое ребро.</param>
        private void RemoveEdgeFromTemp(Dictionary<int, List<GraphEdge>> adj, int u, int v, GraphEdge specificEdge)
        {
            if (adj.ContainsKey(u))
            {
                var toRemove = adj[u].FirstOrDefault(e => e.To == v && e.Weight == specificEdge.Weight);
                if (toRemove != null) adj[u].Remove(toRemove);
            }

            // Удаление обратного ребра для неориентированного графа
            if (!specificEdge.IsDirected && adj.ContainsKey(v))
            {
                var backEdge = adj[v].FirstOrDefault(e => e.To == u);
                if (backEdge != null) adj[v].Remove(backEdge);
            }
        }

        /// <summary>
        /// Проверка ребра на принадлежность к мостам (алгоритм Флёри).
        /// </summary>
        /// <param name="u">Исходящая вершина.</param>
        /// <param name="edge">Проверяемое ребро.</param>
        /// <param name="adj">Временный список смежности.</param>
        /// <param name="isDirected">Признак ориентированности графа.</param>
        /// <returns>Признак того, что ребро является мостом.</returns>
        private bool IsBridge(int u, GraphEdge edge, Dictionary<int, List<GraphEdge>> adj, bool isDirected)
        {
            int v = edge.To;

            int countBefore = CountReachable(u, adj);

            RemoveEdgeFromTemp(adj, u, v, edge);

            int countAfter = CountReachable(u, adj);

            // Восстановление ребра после проверки
            adj[u].Add(edge);
            if (!isDirected && adj.ContainsKey(v))
            {
                adj[v].Add(new GraphEdge(v, u, edge.Weight, edge.IsDirected));
            }

            return countAfter < countBefore;
        }

        /// <summary>
        /// Подсчёт количества вершин, достижимых из заданной.
        /// </summary>
        /// <param name="start">Стартовая вершина обхода.</param>
        /// <param name="adj">Структура смежности для обхода.</param>
        /// <returns>Количество достижимых вершин.</returns>
        private int CountReachable(int start, Dictionary<int, List<GraphEdge>> adj)
        {
            var visited = new HashSet<int>();
            var q = new Queue<int>();
            q.Enqueue(start);
            visited.Add(start);

            while (q.Count > 0)
            {
                int u = q.Dequeue();
                if (adj.ContainsKey(u))
                {
                    foreach (var edge in adj[u])
                    {
                        if (!visited.Contains(edge.To))
                        {
                            visited.Add(edge.To);
                            q.Enqueue(edge.To);
                        }
                    }
                }
            }
            return visited.Count;
        }
        #endregion
    }
}