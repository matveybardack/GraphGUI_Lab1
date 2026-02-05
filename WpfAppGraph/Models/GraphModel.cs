using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public class GraphModel
    {
        // Основное хранилище: Список смежности (Adjacency List)
        // Key: ID вершины, Value: Список исходящих ребер
        private readonly Dictionary<int, List<GraphEdge>> _adjacencyList;
        private readonly HashSet<int> _vertices;

        public GraphModel()
        {
            _adjacencyList = new Dictionary<int, List<GraphEdge>>();
            _vertices = new HashSet<int>();
        }

        #region Работа с классом

        public void AddVertex(int id)
        {
            if (!_vertices.Contains(id))
            {
                _vertices.Add(id);
                _adjacencyList[id] = new List<GraphEdge>();
            }
        }

        public void AddEdge(int from, int to, double weight, bool isDirected)
        {
            AddVertex(from);
            AddVertex(to);

            var edge = new GraphEdge(from, to, weight, isDirected);
            _adjacencyList[from].Add(edge);

            if (!isDirected)
            {
                var reverseEdge = new GraphEdge(to, from, weight, isDirected);
                _adjacencyList[to].Add(reverseEdge);
            }
        }

        public void Clear()
        {
            _adjacencyList.Clear();
            _vertices.Clear();
        }

        public List<int> GetVertices() => _vertices.OrderBy(v => v).ToList();

        #endregion

        #region Матрицы

        public double[,] GetAdjacencyMatrix()
        {
            var vertices = GetVertices();
            int n = vertices.Count;

            var idToIndex = new Dictionary<int, int>();
            for (int i = 0; i < n; i++) idToIndex[vertices[i]] = i;

            double[,] matrix = new double[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    matrix[i, j] = double.PositiveInfinity;

            foreach (var vertexId in vertices)
            {
                if (_adjacencyList.ContainsKey(vertexId))
                {
                    int row = idToIndex[vertexId];
                    foreach (var edge in _adjacencyList[vertexId])
                    {
                        int col = idToIndex[edge.To];
                        if (edge.Weight < matrix[row, col])
                            matrix[row, col] = edge.Weight;
                    }
                }
            }

            return matrix;
        }

        public double[,] GetTransposedAdjacencyMatrix()
        {
            double[,] original = GetAdjacencyMatrix();
            int rows = original.GetLength(0);
            int cols = original.GetLength(1);

            double[,] transposed = new double[cols, rows];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    transposed[j, i] = original[i, j];

            return transposed;
        }

        #endregion

        #region BFS

        /// <summary>
        /// Запускает поиск в ширину (BFS).
        /// </summary>
        /// <param name="startId">ID стартовой вершины</param>
        /// <param name="targetId">ID целевой вершины (может быть null)</param>
        /// <param name="result">Объект для записи итоговых результатов</param>
        /// <returns>Ленивая коллекция шагов для анимации</returns>
        public IEnumerable<AlgorithmStep> RunBfs(int startId, int? targetId, BfsResult result)
        {
            if (!_vertices.Contains(startId)) yield break;

            // Инициализация структур данных
            var queue = new Queue<int>();
            var parentMap = new Dictionary<int, int>(); // Для восстановления пути
            var discoveryTime = new Dictionary<int, int>(); // Время входа (d)
            var finishTime = new Dictionary<int, int>();    // Время выхода (f)
            var visited = new HashSet<int>();

            int timer = 1;
            StringBuilder structBuilder = new StringBuilder();

            // Начало алгоритма
            queue.Enqueue(startId);
            visited.Add(startId);
            discoveryTime[startId] = timer++;

            structBuilder.Append($"({startId} "); // Открываем скобку для структуры

            // Шаг 1: Анимация старта (красим в Active/Visited, пишем время входа)
            yield return new AlgorithmStep
            {
                VertexId = startId,
                NewVertexState = VertexState.Active,
                IterationInfo = $"{discoveryTime[startId]}/-"
            };

            bool targetFound = false;

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();

                // Для красоты анимации: когда достаем из очереди, можно подсвечивать как "текущий обрабатываемый"
                // Но в BFS вершина обычно красится при добавлении. Оставим логику:
                // Visited (серый) - в очереди, Finished (черный) - обработан.

                if (targetId.HasValue && u == targetId.Value)
                {
                    targetFound = true;
                    // Если цель найдена, можно прерывать поиск, если нам нужен только путь.
                    // Если нужно построить полное дерево обхода компоненты связности - убираем break.
                    // Для задачи поиска пути обычно прерывают.
                }

                if (_adjacencyList.ContainsKey(u))
                {
                    // Сортируем соседей для детерминированности (не обязательно, но полезно для UI)
                    var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();

                    foreach (var edge in neighbors)
                    {
                        int v = edge.To;
                        if (!visited.Contains(v))
                        {
                            visited.Add(v);
                            parentMap[v] = u; // Запоминаем откуда пришли
                            discoveryTime[v] = timer++;
                            structBuilder.Append($"({v} ");

                            queue.Enqueue(v);

                            // Шаг 2: Анимация открытия соседа (ребро + вершина)
                            yield return new AlgorithmStep
                            {
                                VertexId = v,
                                NewVertexState = VertexState.Visited, // Добавили в очередь
                                IterationInfo = $"{discoveryTime[v]}/-",
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.TreeEdge // Ребро дерева обхода
                            };

                            // Если нашли цель прямо сейчас
                            if (targetId.HasValue && v == targetId.Value)
                            {
                                targetFound = true;
                                // Можно сделать yield break здесь, если хотим мгновенно остановиться
                            }
                        }
                    }
                }

                // Завершение обработки вершины u
                finishTime[u] = timer++;
                structBuilder.Append($") "); // Закрываем скобку

                // Шаг 3: Вершина полностью обработана
                yield return new AlgorithmStep
                {
                    VertexId = u,
                    NewVertexState = VertexState.Finished,
                    IterationInfo = $"{discoveryTime[u]}/{finishTime[u]}"
                };
            }

            // --- Формирование результатов ---
            result.IsTargetFound = targetFound;
            result.ParenthesisStructure = structBuilder.ToString().Trim();

            // Восстановление пути, если цель была задана и найдена
            if (targetFound && targetId.HasValue)
            {
                int curr = targetId.Value;
                result.Path.Add(curr);

                while (curr != startId)
                {
                    if (!parentMap.ContainsKey(curr)) break; // На всякий случай

                    int p = parentMap[curr];

                    // Считаем вес ребра
                    var edge = _adjacencyList[p].First(e => e.To == curr);
                    result.PathLength += edge.Weight;

                    curr = p;
                    result.Path.Add(curr);
                }
                result.Path.Reverse(); // Путь от старта к цели
            }
        }

        #endregion

        #region DFS

        // Вспомогательный метод для заполнения итогового результата (путь и строка)
        private void FillResult(DfsResult result, int? targetId, bool targetFound, StringBuilder sb, Dictionary<int, int> parentMap, Dictionary<int, List<GraphEdge>> adj)
        {
            result.ParenthesisStructure = sb.ToString().Trim();
            result.IsTargetFound = targetFound;

            if (targetFound && targetId.HasValue)
            {
                int curr = targetId.Value;
                result.Path.Add(curr);
                while (parentMap.ContainsKey(curr))
                {
                    int p = parentMap[curr];

                    // Ищем вес
                    if (adj.ContainsKey(p))
                    {
                        var edge = adj[p].FirstOrDefault(e => e.To == curr);
                        if (edge != null) result.PathLength += edge.Weight;
                    }

                    curr = p;
                    result.Path.Add(curr);
                }
                result.Path.Reverse();
            }
        }

        /// <summary>
        /// Вариант 1: Итеративный DFS (на стеке).
        /// Имитирует рекурсию с помощью стека для построения скобочной структуры.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunDfsIterative(int? startId, int? targetId, DfsResult result)
        {
            // Подготовка структур
            var visited = new HashSet<int>();
            var parentMap = new Dictionary<int, int>();
            var discoveryTime = new Dictionary<int, int>();
            var finishTime = new Dictionary<int, int>();

            // Стек хранит ID вершины. 
            // Мы не удаляем вершину сразу, а ждем обработки всех детей для времени выхода.
            var stack = new Stack<int>();

            int timer = 1;
            StringBuilder structBuilder = new StringBuilder();
            bool targetFound = false;

            // Определяем порядок обхода компонент связности
            // Если startId задан, начинаем с него. Потом идем по остальным непосещенным.
            var allVertices = GetVertices();
            if (startId.HasValue)
            {
                // Перемещаем стартовую вершину в начало списка для приоритета
                allVertices.Remove(startId.Value);
                allVertices.Insert(0, startId.Value);
            }

            foreach (var root in allVertices)
            {
                if (visited.Contains(root)) continue;
                if (targetFound && targetId.HasValue) break; // Если цель найдена и нам нужен только путь

                stack.Push(root);

                while (stack.Count > 0)
                {
                    int u = stack.Peek(); // Смотрим, но пока не извлекаем

                    // 1. Вход в вершину (White -> Gray)
                    if (!visited.Contains(u))
                    {
                        visited.Add(u);
                        discoveryTime[u] = timer++;
                        structBuilder.Append($"({u} ");

                        yield return new AlgorithmStep
                        {
                            VertexId = u,
                            NewVertexState = VertexState.Active, // Gray
                            IterationInfo = $"{discoveryTime[u]}/-"
                        };

                        if (targetId.HasValue && u == targetId.Value)
                        {
                            targetFound = true;
                            // Не прерываем сразу, чтобы корректно закрыть скобки текущего стека?
                            // Для поиска пути обычно прерывают, но для структуры лучше завершить ветку.
                            // Давайте прервем поиск новых веток, но размотаем стек.
                        }
                    }

                    // 2. Поиск следующего непосещенного соседа
                    bool hasUnvisitedNeighbor = false;

                    if (_adjacencyList.ContainsKey(u) /*&& (!targetFound || targetId == null)*/) // Если нашли цель, в глубину не идем
                    {
                        // Сортируем для детерминированности
                        var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();

                        foreach (var edge in neighbors)
                        {
                            int v = edge.To;
                            if (!visited.Contains(v))
                            {
                                parentMap[v] = u;
                                stack.Push(v); // Кладем в стек и немедленно переходим к ней (break)
                                hasUnvisitedNeighbor = true;

                                // Анимация ребра дерева (Tree Edge)
                                yield return new AlgorithmStep
                                {
                                    EdgeFromId = u,
                                    EdgeToId = v,
                                    NewEdgeType = EdgeType.TreeEdge
                                };
                                break; // Уходим в глубину (эмуляция рекурсии)
                            }
                            else
                            {
                                // Классификация остальных ребер (для красоты)
                                // Если сосед серый (в стеке и без finishTime) -> Обратное ребро
                                // Если сосед черный (есть finishTime) -> Прямое или перекрестное
                                // (Здесь упрощенная проверка, полная будет в методе 3 цветов)
                            }
                        }
                    }

                    // 3. Выход из вершины (Gray -> Black), если нет непосещенных соседей
                    if (!hasUnvisitedNeighbor)
                    {
                        stack.Pop(); // Теперь извлекаем
                        finishTime[u] = timer++;
                        structBuilder.Append($") ");

                        yield return new AlgorithmStep
                        {
                            VertexId = u,
                            NewVertexState = VertexState.Finished, // Black
                            IterationInfo = $"{discoveryTime[u]}/{finishTime[u]}"
                        };
                    }
                }
            }

            FillResult(result, targetId, targetFound, structBuilder, parentMap, _adjacencyList);
        }

        /// <summary>
        /// Вариант 2: Рекурсивный DFS ("Алгоритм 3 цветов").
        /// Использует системный стек вызовов. 
        /// Цвета: Default(White) -> Active(Gray) -> Finished(Black).
        /// </summary>
        public IEnumerable<AlgorithmStep> RunDfsRecursive(int? startId, int? targetId, DfsResult result)
        {
            var visited = new HashSet<int>();     // Множество "Серых" и "Черных"
            var finished = new HashSet<int>();    // Множество "Черных" (для классификации ребер)

            var parentMap = new Dictionary<int, int>();
            var discoveryTime = new Dictionary<int, int>();
            var finishTime = new Dictionary<int, int>();

            int timer = 1;
            StringBuilder structBuilder = new StringBuilder();
            bool targetFound = false;

            // Подготовка очереди обхода (сначала выбранный старт, потом остальные)
            var allVertices = GetVertices();
            if (startId.HasValue)
            {
                allVertices.Remove(startId.Value);
                allVertices.Insert(0, startId.Value);
            }

            // Внешний цикл по компонентам связности
            foreach (var u in allVertices)
            {
                if (!visited.Contains(u))
                {
                    if (targetFound && targetId.HasValue) break;

                    // Запуск рекурсивной функции (через yield foreach пробрасываем шаги)
                    foreach (var step in DfsVisit(u))
                    {
                        yield return step;
                    }
                }
            }

            // Локальная рекурсивная функция
            IEnumerable<AlgorithmStep> DfsVisit(int u)
            {
                // === WHITE -> GRAY ===
                visited.Add(u); // Visited но не Finished = Gray
                discoveryTime[u] = timer++;
                structBuilder.Append($"({u} ");

                yield return new AlgorithmStep
                {
                    VertexId = u,
                    NewVertexState = VertexState.Active, // Gray
                    IterationInfo = $"{discoveryTime[u]}/-"
                };

                if (targetId.HasValue && u == targetId.Value)
                {
                    targetFound = true;
                    // Не делаем yield break, чтобы позволить рекурсии свернуться и проставить черные цвета
                }

                if (_adjacencyList.ContainsKey(u))
                {
                    var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();
                    foreach (var edge in neighbors)
                    {
                        // Если нашли цель и хотим остановить исследование СОСЕДЕЙ (но текущий путь закроем)
                        //if (targetFound && targetId.HasValue) break;

                        int v = edge.To;
                        if (!visited.Contains(v)) // White
                        {
                            parentMap[v] = u;

                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.TreeEdge
                            };

                            // Рекурсивный вызов
                            foreach (var step in DfsVisit(v)) yield return step;
                        }
                        else
                        {
                            // === КЛАССИФИКАЦИЯ РЕБЕР (Бонус) ===
                            EdgeType type = EdgeType.Default;

                            if (!finished.Contains(v)) // Сосед Gray -> Back Edge (Обратное)
                            {
                                type = EdgeType.BackEdge;
                            }
                            else // Сосед Black
                            {
                                // Если d[u] < d[v] -> Forward (Прямое), иначе Cross (Перекрестное)
                                if (discoveryTime[u] < discoveryTime[v]) type = EdgeType.ForwardEdge;
                                else type = EdgeType.CrossEdge;
                            }

                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = type
                            };
                        }
                    }
                }

                // === GRAY -> BLACK ===
                finished.Add(u);
                finishTime[u] = timer++;
                structBuilder.Append($") ");

                yield return new AlgorithmStep
                {
                    VertexId = u,
                    NewVertexState = VertexState.Finished, // Black
                    IterationInfo = $"{discoveryTime[u]}/{finishTime[u]}"
                };
            }

            FillResult(result, targetId, targetFound, structBuilder, parentMap, _adjacencyList);
        }

        #endregion
    }
}