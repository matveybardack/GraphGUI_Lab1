using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public class GraphModel
    {
        private readonly Dictionary<int, List<GraphEdge>> _adjacencyList;
        private readonly HashSet<int> _vertices;

        public GraphModel()
        {
            _adjacencyList = new Dictionary<int, List<GraphEdge>>();
            _vertices = new HashSet<int>();
        }

        #region Базовые операции

        /// <summary>
        /// Добавление вершины в модель
        /// </summary>
        /// <param name="id"> номер вершины </param>
        public void AddVertex(int id)
        {
            if (!_vertices.Contains(id))
            {
                _vertices.Add(id);
                _adjacencyList[id] = new List<GraphEdge>();
            }
        }

        /// <summary>
        /// Добавление ребра в модель.
        /// </summary>
        /// <param name="from"> исходная вершина </param>
        /// <param name="to"> входная вершина </param>
        /// <param name="weight"> вес ребра </param>
        /// <param name="isDirected"> является ли ориентированным </param>
        public void AddEdge(int from, int to, double weight, bool isDirected)
        {
            // Создание новых вершин, если таковых не было (только для модели)
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

        #region Создание матриц

        /// <summary>
        /// Построение матрицы смежности
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Построение транспонированной матрицы смежности.
        /// </summary>
        /// <returns></returns>
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

            // 3. Локальная функция для рекурсивного обхода
            IEnumerable<AlgorithmStep> DfsVisit(int u)
            {
                // --- ВХОД (White -> Gray) ---
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

                // --- ОБРАБОТКА СОСЕДЕЙ ---
                if (_adjacencyList.ContainsKey(u))
                {
                    // Сортировка для детерминированности
                    var neighbors = _adjacencyList[u].OrderBy(e => e.To).ToList();

                    foreach (var edge in neighbors)
                    {
                        int v = edge.To;

                        // Если сосед не посещен — это ребро дерева (Tree Edge)
                        if (!visited.Contains(v))
                        {
                            parentMap[v] = u; // Запоминаем родителя

                            // Анимация перехода по ребру (Tree Edge)
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
                            // === КЛАССИФИКАЦИЯ ОСТАЛЬНЫХ РЕБЕР ===
                            EdgeType type = EdgeType.Default;

                            // Если у соседа нет времени выхода, значит он сейчас в стеке рекурсии (Gray)
                            // Это обратное ребро (цикл)
                            if (!finishTime.ContainsKey(v))
                            {
                                type = EdgeType.BackEdge;
                            }
                            else
                            {
                                // Сосед уже обработан (Black).
                                // Если мы вошли в u раньше, чем в v (d[u] < d[v]), то v — потомок. Прямое ребро.
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

                // --- ВЫХОД (Gray -> Black) ---
                finishTime[u] = timer++;
                structBuilder.Append($") ");

                yield return new AlgorithmStep
                {
                    VertexId = u,
                    NewVertexState = VertexState.Finished,
                    IterationInfo = $"{discoveryTime[u]}/{finishTime[u]}"
                };
            }

            // 4. Внешний цикл запуска (для несвязных графов)
            foreach (var root in allVertices)
            {
                // Пропускаем уже посещенные вершины (из предыдущих компонент)
                if (!visited.Contains(root))
                {
                    foreach (var step in DfsVisit(root))
                    {
                        yield return step;
                    }
                }
            }

            // 5. Формирование результатов
            result.IsTargetFound = globalTargetFound;
            result.ParenthesisStructure = structBuilder.ToString().Trim();

            if (globalTargetFound && targetId.HasValue)
            {
                var tempPath = new List<int>();
                int curr = targetId.Value;
                tempPath.Add(curr);

                // Двигаемся от цели к родителям
                while (parentMap.ContainsKey(curr))
                {
                    int p = parentMap[curr];

                    // Считаем вес ребра
                    if (_adjacencyList.ContainsKey(p))
                    {
                        var edge = _adjacencyList[p].FirstOrDefault(e => e.To == curr);
                        if (edge != null)
                            result.PathLength += edge.Weight;
                    }

                    curr = p;
                    tempPath.Add(curr);
                }

                // Проверяем валидность пути
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