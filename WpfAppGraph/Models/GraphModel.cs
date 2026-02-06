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

        #region Basic opers

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

        #region Matrix

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

        #region SCC algo
        /// <summary>
        /// Поиск сильносвязанных компонентов (SCC) алгоритмом Косарайю.
        /// Использует матрицу смежности для работы с транспонированным графом.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunFindStronglyConnectedComponents(SccResult result)
        {
            var vertices = GetVertices();
            var visited = new HashSet<int>();
            var stack = new Stack<int>();

            // 1. Первый проход: DFS на оригинальном графе, заполняем стек по времени выхода
            foreach (var v in vertices)
            {
                if (!visited.Contains(v))
                {
                    FillOrder(v, visited, stack);
                }
            }

            // 2. Получаем транспонированную матрицу для обратного прохода
            // (Используем ваш готовый метод)
            double[,] matrixT = GetTransposedAdjacencyMatrix();

            // Маппинг ID -> Индекс в матрице (так как матрица работает с индексами 0..N)
            var idToIndex = new Dictionary<int, int>();
            for (int i = 0; i < vertices.Count; i++) idToIndex[vertices[i]] = i;

            visited.Clear();

            // 3. Второй проход: Извлекаем из стека и запускаем DFS по транспонированному графу
            while (stack.Count > 0)
            {
                int v = stack.Pop();

                if (!visited.Contains(v))
                {
                    var component = new List<int>();

                    // DFS по транспонированной матрице
                    DfsTransposed(v, visited, component, matrixT, vertices, idToIndex);

                    result.Components.Add(component);

                    // --- Анимация найденного компонента ---

                    // Шаг А: Красим вершины компонента в "Finished" (или Visited)
                    foreach (var vertexId in component)
                    {
                        yield return new AlgorithmStep
                        {
                            VertexId = vertexId,
                            NewVertexState = VertexState.Finished, // Обозначаем, что вершина классифицирована
                            IterationInfo = $"SCC #{result.Components.Count}"
                        };
                    }
                }
            }

            // 4. Классификация ребер
            // Проходим по всем ребрам оригинального графа.
            // Если from и to принадлежат ОДНОМУ компоненту — красим ребро.

            // Создаем карту: ID вершины -> Индекс компонента
            var vertexToComponentIndex = new Dictionary<int, int>();
            for (int i = 0; i < result.Components.Count; i++)
            {
                foreach (var v in result.Components[i])
                {
                    vertexToComponentIndex[v] = i;
                }
            }

            foreach (var kvp in _adjacencyList)
            {
                int u = kvp.Key;
                // Если вершина не попала ни в один компонент (такого быть не должно), пропускаем
                if (!vertexToComponentIndex.ContainsKey(u)) continue;

                foreach (var edge in kvp.Value)
                {
                    int v = edge.To;
                    if (vertexToComponentIndex.ContainsKey(v))
                    {
                        // Проверка: лежат ли U и V в одном компоненте
                        if (vertexToComponentIndex[u] == vertexToComponentIndex[v])
                        {
                            // Это ребро внутри SCC. По заданию красим его (например, в BackEdge).
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.BackEdge // Используем красный цвет (или какой у вас BackEdge)
                            };
                        }
                        else
                        {
                            // Ребро между разными компонентами (конденсация графа).
                            // Можно покрасить в Default или ForwardEdge
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.Default
                            };
                        }
                    }
                }
            }
        }

        // Вспомогательный метод для прямого прохода
        private void FillOrder(int u, HashSet<int> visited, Stack<int> stack)
        {
            visited.Add(u);

            if (_adjacencyList.ContainsKey(u))
            {
                foreach (var edge in _adjacencyList[u])
                {
                    if (!visited.Contains(edge.To))
                    {
                        FillOrder(edge.To, visited, stack);
                    }
                }
            }

            stack.Push(u); // Добавляем в стек ПОСЛЕ обработки детей (время выхода)
        }

        // Вспомогательный метод для обратного прохода по матрице
        private void DfsTransposed(int u, HashSet<int> visited, List<int> component, double[,] matrixT, List<int> allVertices, Dictionary<int, int> idToIndex)
        {
            visited.Add(u);
            component.Add(u);

            int uIndex = idToIndex[u];
            int n = allVertices.Count;

            for (int i = 0; i < n; i++)
            {
                // Проверяем наличие ребра в ТРАНСПОНИРОВАННОЙ матрице
                // matrixT[row, col] != Infinity
                if (!double.IsPositiveInfinity(matrixT[uIndex, i]))
                {
                    int v = allVertices[i]; // Получаем ID вершины по индексу
                    if (!visited.Contains(v))
                    {
                        DfsTransposed(v, visited, component, matrixT, allVertices, idToIndex);
                    }
                }
            }
        }
        #endregion

        #region Euler cycle
        /// <summary>
        /// Проверяет, возможен ли Эйлеров путь или цикл.
        /// Возвращает стартовую вершину или -1, если невозможно.
        /// out isCycle: true - цикл, false - путь.
        /// </summary>
        public int CheckEulerian(out bool isCycle, out string message)
        {
            isCycle = false;
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                message = "Граф пуст";
                return -1;
            }

            // 1. Проверка на связность (игнорируем изолированные вершины без ребер)
            // Упрощенно: считаем степени
            var degrees = new Dictionary<int, int>(); // Для неориентированного: степень
            var inDegree = new Dictionary<int, int>(); // Для ориентированного
            var outDegree = new Dictionary<int, int>();

            bool isDirected = false;

            // Анализируем ребра
            foreach (var u in vertices)
            {
                degrees[u] = 0; inDegree[u] = 0; outDegree[u] = 0;
            }

            int edgeCount = 0;
            foreach (var kvp in _adjacencyList)
            {
                foreach (var edge in kvp.Value)
                {
                    edgeCount++;
                    if (edge.IsDirected) isDirected = true;

                    outDegree[kvp.Key]++;
                    if (inDegree.ContainsKey(edge.To)) inDegree[edge.To]++;

                    // Для неориентированного логика
                    degrees[kvp.Key]++;
                    if (degrees.ContainsKey(edge.To)) degrees[edge.To]++;
                }
            }

            if (edgeCount == 0)
            {
                message = "Нет ребер";
                return -1;
            }

            // TODO: По-хорошему нужно запустить BFS/DFS и проверить, 
            // что все вершины с degree > 0 принадлежат одной компоненте связности.
            // Опустим это для краткости, алгоритм сам застрянет, если граф несвязный.

            if (isDirected)
            {
                // Условия для ориентированного графа
                int startNodes = 0, endNodes = 0;
                int startNode = vertices[0];

                foreach (var v in vertices)
                {
                    if (outDegree[v] == inDegree[v]) continue;
                    else if (outDegree[v] - inDegree[v] == 1) { startNodes++; startNode = v; }
                    else if (inDegree[v] - outDegree[v] == 1) { endNodes++; }
                    else
                    {
                        message = "Степени вершин не удовлетворяют условию Эйлера (Directed)";
                        return -1;
                    }
                }

                if (startNodes == 0 && endNodes == 0)
                {
                    isCycle = true;
                    message = "Найден Эйлеров цикл (Directed)";
                    // Ищем любую вершину с ребрами как старт
                    return vertices.FirstOrDefault(v => outDegree[v] > 0);
                }
                else if (startNodes == 1 && endNodes == 1)
                {
                    isCycle = false;
                    message = "Найден Эйлеров путь (Directed)";
                    return startNode;
                }
                else
                {
                    message = "Невозможно построить путь (нарушен баланс степеней)";
                    return -1;
                }
            }
            else // Неориентированный
            {
                // Учитываем, что в _adjacencyList неориентированные ребра дублируются (A->B и B->A).
                // Поэтому degrees[] посчитан корректно (реальная степень * 2? Нет, мы просто считаем исходящие записи).
                // В вашей модели AddEdge добавляет запись и туда и сюда. 
                // Значит кол-во записей в списке смежности вершины = реальной степени вершины.

                int oddCount = 0;
                int startNode = vertices[0];

                foreach (var v in vertices)
                {
                    // Считаем размер списка смежности
                    int deg = _adjacencyList.ContainsKey(v) ? _adjacencyList[v].Count : 0;
                    if (deg % 2 != 0)
                    {
                        oddCount++;
                        startNode = v;
                    }
                }

                if (oddCount == 0)
                {
                    isCycle = true;
                    message = "Найден Эйлеров цикл (Undirected)";
                    // Берем любую вершину с ребрами
                    return vertices.FirstOrDefault(v => _adjacencyList.ContainsKey(v) && _adjacencyList[v].Count > 0);
                }
                else if (oddCount == 2)
                {
                    isCycle = false;
                    message = "Найден Эйлеров путь (Undirected)";
                    return startNode;
                }
                else
                {
                    message = $"Нечетных вершин: {oddCount}. Требуется 0 или 2.";
                    return -1;
                }
            }
        }

        // --- Алгоритм Флёри ---
        public IEnumerable<AlgorithmStep> RunFleuryAlgorithm(EulerResult result)
        {
            int startNode = CheckEulerian(out bool isCycle, out string msg);
            result.StatusMessage = msg;

            if (startNode == -1) yield break;

            // Копируем граф, так как будем удалять ребра
            var tempAdj = CloneAdjacencyList();

            // Если граф ориентированный, Fleury сложнее (нужно не просто мосты искать).
            // Но в рамках задачи упростим: считаем, что Fleury применяем к неориентированному.
            // Если пользователь нарисовал стрелки - алгоритм может работать некорректно без адаптации.

            var path = new List<int>();
            int u = startNode;
            path.Add(u);

            yield return new AlgorithmStep { VertexId = u, NewVertexState = VertexState.Selected, IterationInfo = "Start" };

            int edgeCount = tempAdj.Values.Sum(l => l.Count);
            // Для неориентированного графа edgeCount будет 2 * кол-во ребер.

            while (tempAdj.ContainsKey(u) && tempAdj[u].Count > 0)
            {
                // Ищем ребро для перехода
                int v = -1;
                GraphEdge chosenEdge = null;

                var neighbors = tempAdj[u];

                // 1. Пробуем найти ребро, которое НЕ является мостом
                foreach (var edge in neighbors)
                {
                    bool isBridge = IsBridge(u, edge.To, tempAdj);

                    // Если не мост или нет другого выбора (осталось 1 ребро)
                    if (!isBridge || neighbors.Count == 1)
                    {
                        v = edge.To;
                        chosenEdge = edge;
                        break;
                    }
                }

                // Если почему-то не нашли (сложный случай в ориентированном), берем первое
                if (v == -1 && neighbors.Count > 0)
                {
                    chosenEdge = neighbors[0];
                    v = chosenEdge.To;
                }

                if (chosenEdge != null)
                {
                    // Анимация прохода
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = u,
                        EdgeToId = v,
                        NewEdgeType = EdgeType.BackEdge, // Красим в "использованное"
                        VertexId = v,
                        NewVertexState = VertexState.Active
                    };

                    // Удаляем ребро из временного графа
                    RemoveEdgeFromTemp(tempAdj, u, v, chosenEdge.IsDirected);

                    u = v;
                    path.Add(u);
                }
                else
                {
                    break;
                }
            }

            result.Path = path;
            result.IsSuccess = true;
        }

        // --- Алгоритм Эйлера (Иерахольцера / Hierholzer - через стек) ---
        public IEnumerable<AlgorithmStep> RunHierholzerAlgorithm(EulerResult result)
        {
            int startNode = CheckEulerian(out bool isCycle, out string msg);
            result.StatusMessage = msg;
            if (startNode == -1) yield break;

            var tempAdj = CloneAdjacencyList();
            var stack = new Stack<int>();
            var circuit = new List<int>();

            stack.Push(startNode);

            // Визуально подсвечиваем старт
            yield return new AlgorithmStep { VertexId = startNode, NewVertexState = VertexState.Selected, IterationInfo = "Start" };

            while (stack.Count > 0)
            {
                int u = stack.Peek();

                if (tempAdj.ContainsKey(u) && tempAdj[u].Count > 0)
                {
                    // Идем вглубь
                    var edge = tempAdj[u][0]; // Берем любое ребро
                    int v = edge.To;

                    stack.Push(v);

                    // Удаляем ребро
                    RemoveEdgeFromTemp(tempAdj, u, v, edge.IsDirected);

                    // Анимация "Идем вперед"
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = u,
                        EdgeToId = v,
                        NewEdgeType = EdgeType.BackEdge, // Помечаем как пройденное
                        VertexId = v,
                        NewVertexState = VertexState.Active
                    };
                }
                else
                {
                    // Тупик, добавляем в путь и откатываемся
                    int val = stack.Pop();
                    circuit.Add(val);

                    // Анимация "Откат" (можно красить вершину в Finished)
                    yield return new AlgorithmStep
                    {
                        VertexId = val,
                        NewVertexState = VertexState.Finished,
                        IterationInfo = $"P{circuit.Count}" // Индекс в пути
                    };
                }
            }

            // Hierholzer строит путь с конца
            circuit.Reverse();
            result.Path = circuit;
            result.IsSuccess = true;
        }


        // --- Вспомогательные методы ---

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

        private void RemoveEdgeFromTemp(Dictionary<int, List<GraphEdge>> adj, int u, int v, bool isDirected)
        {
            // Удаляем u -> v
            var edgeU = adj[u].FirstOrDefault(e => e.To == v);
            if (edgeU != null) adj[u].Remove(edgeU);

            if (!isDirected)
            {
                // Удаляем v -> u
                if (adj.ContainsKey(v))
                {
                    var edgeV = adj[v].FirstOrDefault(e => e.To == u);
                    if (edgeV != null) adj[v].Remove(edgeV);
                }
            }
        }

        // Проверка, является ли ребро u-v мостом
        private bool IsBridge(int u, int v, Dictionary<int, List<GraphEdge>> adj)
        {
            // Считаем достижимые вершины до удаления
            int countBefore = CountReachable(u, adj);

            // Временно удаляем ребро (считаем его неориентированным для Флёри)
            // Примечание: Флёри в классике для неориентированных графов.
            var edgeUV = adj[u].FirstOrDefault(e => e.To == v);

            bool isDirected = edgeUV != null && edgeUV.IsDirected;

            RemoveEdgeFromTemp(adj, u, v, isDirected);

            int countAfter = CountReachable(u, adj);

            // Возвращаем ребро обратно
            adj[u].Add(new GraphEdge(u, v, 1, isDirected));
            if (!isDirected && adj.ContainsKey(v))
            {
                adj[v].Add(new GraphEdge(v, u, 1, isDirected));
            }

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
    }
}