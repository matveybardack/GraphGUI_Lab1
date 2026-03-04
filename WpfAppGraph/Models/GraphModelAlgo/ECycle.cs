using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region Euler cycle

        /// <summary>
        /// Проверяет, является ли граф Эйлеровым (содержит цикл или путь).
        /// Возвращает стартовую вершину или -1, если условие не выполнено.
        /// </summary>
        public int CheckEulerian(out bool isCycle, out string message)
        {
            isCycle = false;
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                message = "Граф пуст.";
                return -1;
            }

            // 1. Получаем степени вершин и список активных вершин (имеющих ребра)
            var degrees = CalculateDegrees(out bool isDirected, out int totalEdges);

            if (totalEdges == 0)
            {
                message = "В графе нет ребер.";
                return -1;
            }

            // Вершины, у которых есть хотя бы одно ребро (входящее или исходящее)
            var activeVertices = vertices.Where(v =>
                (degrees.ContainsKey(v) && (degrees[v].In > 0 || degrees[v].Out > 0))
            ).ToList();

            if (activeVertices.Count == 0)
            {
                message = "Нет активных вершин.";
                return -1;
            }

            // 2. Проверка связности (Важно! Все ребра должны быть в одной компоненте)
            // Запускаем BFS от первой активной вершины
            if (!IsGraphConnectedBasedOnEdges(activeVertices[0], activeVertices.Count, isDirected))
            {
                message = "Граф несвязен (содержит несколько компонент с ребрами).";
                return -1;
            }

            // 3. Проверка степеней
            if (isDirected)
            {
                return CheckDirectedDegrees(vertices, degrees, out isCycle, out message);
            }
            else
            {
                return CheckUndirectedDegrees(vertices, degrees, out isCycle, out message);
            }
        }

        // --- Алгоритм Флёри (Fleury) ---
        public IEnumerable<AlgorithmStep> RunFleuryAlgorithm(EulerResult result)
        {
            // Флёри классически работает на неориентированных графах.
            // На ориентированных понятие "мост" сложнее, и жадный алгоритм может зайти в тупик.
            int startNode = CheckEulerian(out bool isCycle, out string msg);
            result.StatusMessage = msg;

            if (startNode == -1) yield break;

            // Проверка на ориентированность (предупреждение или ограничение)
            bool isDirected = _adjacencyList.Values.Any(l => l.Any(e => e.IsDirected));
            if (isDirected)
            {
                result.StatusMessage = "Алгоритм Флёри не гарантирует результат на ориентированном графе. Рекомендуется Хирхольцер.";
            }

            // Клонируем структуру смежности, так как будем удалять ребра
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

                // Если есть только один путь, выбора нет
                if (neighbors.Count == 1)
                {
                    chosenEdge = neighbors[0];
                    v = chosenEdge.To;
                }
                else
                {
                    // Ищем ребро, которое НЕ является мостом
                    foreach (var edge in neighbors)
                    {
                        if (!IsBridge(u, edge, tempAdj, isDirected))
                        {
                            chosenEdge = edge;
                            v = edge.To;
                            break;
                        }
                    }

                    // Если все ребра - мосты (такое бывает в конце пути), берем любое
                    if (chosenEdge == null)
                    {
                        chosenEdge = neighbors[0];
                        v = chosenEdge.To;
                    }
                }

                // Анимация прохода по ребру
                yield return new AlgorithmStep
                {
                    EdgeFromId = u,
                    EdgeToId = v,
                    NewEdgeType = EdgeType.BackEdge, // Красим как пройденное
                    VertexId = v,
                    NewVertexState = VertexState.Active,
                    IterationInfo = $"Переход {u}->{v}"
                };

                // "Сжигаем" мост за собой
                RemoveEdgeFromTemp(tempAdj, u, v, chosenEdge);

                u = v;
                path.Add(u);
            }

            result.Path = path;
            result.IsSuccess = true;
            result.StatusMessage += " (Завершено)";
        }

        // --- Алгоритм Хирхольцера (Hierholzer) ---
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
                    // 1. Идем вперед: берем первое попавшееся ребро
                    var edge = tempAdj[u][0];
                    int v = edge.To;

                    stack.Push(v);

                    // Удаляем ребро из временного графа
                    RemoveEdgeFromTemp(tempAdj, u, v, edge);

                    // Анимация: спуск вглубь
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = u,
                        EdgeToId = v,
                        NewEdgeType = EdgeType.BackEdge, // Помечаем используемым
                        VertexId = v,
                        NewVertexState = VertexState.Active,
                        IterationInfo = "Вглубь"
                    };
                }
                else
                {
                    // 2. Тупик: переносим вершину из стека в итоговый путь
                    int finishedNode = stack.Pop();
                    circuit.Add(finishedNode);

                    // Анимация: возврат/добавление в цикл
                    yield return new AlgorithmStep
                    {
                        VertexId = finishedNode,
                        NewVertexState = VertexState.Finished, // Помечаем как обработанную
                        IterationInfo = "В цикл"
                    };
                }
            }

            // Хирхольцер строит путь с конца, разворачиваем
            circuit.Reverse();
            result.Path = circuit;
            result.IsSuccess = true;
            result.StatusMessage += " (Завершено)";
        }

        #region Helpers & Validations

        // Структура для подсчета степеней
        private class VertexDegree { public int In; public int Out; }

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
                        degrees[edge.To] = new VertexDegree { In = 1 }; // Защита, если вершины нет в keys
                }
            }
            return degrees;
        }

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
                message = "Найден Эйлеров цикл (Directed).";
                // Для цикла стартом может быть любая вершина с исходящими ребрами
                return vertices.FirstOrDefault(v => degrees[v].Out > 0);
            }
            else if (startNodes == 1 && endNodes == 1)
            {
                isCycle = false;
                message = "Найден Эйлеров путь (Directed).";
                return startNodeCandidate;
            }

            isCycle = false;
            message = "Невозможно построить Эйлеров путь/цикл.";
            return -1;
        }

        private int CheckUndirectedDegrees(List<int> vertices, Dictionary<int, VertexDegree> degrees, out bool isCycle, out string message)
        {
            // В неориентированном графе (в модели) ребра обычно дублируются (A->B и B->A).
            // Но degrees.Out считает количество записей в списке смежности, что равно реальной степени вершины.

            int oddCount = 0;
            int startNodeCandidate = -1;
            int firstNodeWithEdges = -1;

            foreach (var v in vertices)
            {
                int d = degrees[v].Out; // degree
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

        // Проверка связности только по ребрам (игнорируем изолированные вершины)
        private bool IsGraphConnectedBasedOnEdges(int startNode, int expectedCount, bool isDirected)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            // Для неориентированного графа BFS прост.
            // Для ориентированного, чтобы проверить слабую связность (достаточную для Эйлера при правильных степенях),
            // нужно игнорировать направление ребер.

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();

                // Проходим по всем соседям (как исходящим, так и входящим для проверки слабой связности)
                // Исходящие:
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

                // Входящие (дорогой поиск, но нужен для проверки слабой связности, если граф ориентирован)
                // Если граф неориентирован, они уже учтены в списках смежности (так как дублируются)
                if (isDirected)
                {
                    foreach (var kvp in _adjacencyList)
                    {
                        if (visited.Contains(kvp.Key)) continue; // Уже посетили источник
                        // Если есть ребро от kvp.Key к u
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

        // --- Вспомогательные для алгоритмов ---

        private Dictionary<int, List<GraphEdge>> CloneAdjacencyList()
        {
            var clone = new Dictionary<int, List<GraphEdge>>();
            foreach (var kvp in _adjacencyList)
            {
                clone[kvp.Key] = new List<GraphEdge>();
                foreach (var e in kvp.Value)
                {
                    // Важно копировать свойства, но для алгоритма нам нужны ссылки или копии.
                    // Здесь делаем копию объекта ребра.
                    clone[kvp.Key].Add(new GraphEdge(e.From, e.To, e.Weight, e.IsDirected));
                }
            }
            return clone;
        }

        private void RemoveEdgeFromTemp(Dictionary<int, List<GraphEdge>> adj, int u, int v, GraphEdge specificEdge)
        {
            // Удаляем u -> v
            if (adj.ContainsKey(u))
            {
                // Удаляем конкретный экземпляр или первое совпадение по ID/структуре
                var toRemove = adj[u].FirstOrDefault(e => e.To == v && e.Weight == specificEdge.Weight);
                if (toRemove != null) adj[u].Remove(toRemove);
            }

            // Если граф неориентированный, удаляем обратное ребро v -> u
            if (!specificEdge.IsDirected && adj.ContainsKey(v))
            {
                var backEdge = adj[v].FirstOrDefault(e => e.To == u); // Вес должен совпадать, если это мультиграф
                if (backEdge != null) adj[v].Remove(backEdge);
            }
        }

        // Проверка на мост для Флёри
        private bool IsBridge(int u, GraphEdge edge, Dictionary<int, List<GraphEdge>> adj, bool isDirected)
        {
            int v = edge.To;

            // 1. Считаем достижимость до удаления
            int countBefore = CountReachable(u, adj);

            // 2. Временно удаляем ребро
            RemoveEdgeFromTemp(adj, u, v, edge);

            // 3. Считаем после
            int countAfter = CountReachable(u, adj);

            // 4. Возвращаем ребро обратно (важно вернуть корректно)
            adj[u].Add(edge);
            if (!isDirected && adj.ContainsKey(v))
            {
                // Создаем обратное, так как мы его удалили. 
                // В идеале нужно было сохранить и обратное, но для простоты создадим копию с обратными координатами
                adj[v].Add(new GraphEdge(v, u, edge.Weight, edge.IsDirected));
            }

            // Если количество достижимых вершин уменьшилось -> это мост
            return countAfter < countBefore;
        }

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
        #endregion
    }
}