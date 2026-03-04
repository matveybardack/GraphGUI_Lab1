using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region Fundamental Cycles

        /// <summary>
        /// Основной метод запуска поиска фундаментальных циклов.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunFundamentalCyclesAlgorithm(FundamentalCyclesResult result, GraphTraversalType method)
        {
            var vertices = GetVertices();
            if (vertices.Count == 0)
            {
                result.StatusMessage = "Граф пуст";
                yield break;
            }

            int startNode = vertices[0];

            // Структуры для хранения остовного дерева
            // parentMap: child -> parent (чтобы восстанавливать путь)
            var parentMap = new Dictionary<int, int>();
            // treeEdges: хранит пары вершин (u, v), которые вошли в дерево (для быстрой проверки)
            // Используем кортеж (min, max), чтобы для неориентированного графа ребро (u,v) и (v,u) считалось одним и тем же.
            var treeEdges = new HashSet<(int, int)>();

            result.Components.Clear();
            result.StatusMessage = $"Построение остовного дерева методом {method}...";

            // 1. Строим остовное дерево (BFS или DFS)
            IEnumerable<AlgorithmStep> treeSteps;
            if (method == GraphTraversalType.BFS)
            {
                treeSteps = BuildSpanningTreeBFS(startNode, parentMap, treeEdges);
            }
            else
            {
                treeSteps = BuildSpanningTreeDFS(startNode, parentMap, treeEdges);
            }

            // Пробрасываем шаги построения дерева наружу
            foreach (var step in treeSteps)
            {
                yield return step;
            }

            result.StatusMessage = "Поиск хорд и восстановление циклов...";

            // 2. Ищем фундаментальные циклы
            // Проходим по ВСЕМ ребрам графа. Если ребро не в treeEdges — это хорда.
            // Нужно аккуратно перебирать, чтобы не дублировать циклы в неориентированном графе.

            var processedChords = new HashSet<(int, int)>(); // Чтобы не обрабатывать (u,v) и (v,u) дважды

            foreach (var u in _adjacencyList.Keys)
            {
                foreach (var edge in _adjacencyList[u])
                {
                    int v = edge.To;

                    // Формируем уникальный ключ ребра
                    var edgeKey = u < v ? (u, v) : (v, u);

                    // Если это ребро есть в остовном дереве — пропускаем
                    if (treeEdges.Contains(edgeKey))
                        continue;

                    // Если мы уже обработали эту хорду (актуально для неориентированных) — пропускаем
                    if (processedChords.Contains(edgeKey))
                        continue;

                    // Если это "обратное" направление того же ребра в остове (v->u, когда u->v в дереве)
                    // (parentMap хранит u->v, значит v является родителем u или наоборот)
                    if (parentMap.ContainsKey(u) && parentMap[u] == v) continue;
                    if (parentMap.ContainsKey(v) && parentMap[v] == u) continue;

                    // НАЙДЕНА ХОРДА (u, v)
                    processedChords.Add(edgeKey);

                    // Анимация: подсвечиваем хорду
                    yield return new AlgorithmStep
                    {
                        EdgeFromId = u,
                        EdgeToId = v,
                        NewEdgeType = EdgeType.BackEdge, // Цвет хорды
                        IterationInfo = $"Хорда {u}-{v}"
                    };

                    // 3. Восстанавливаем цикл
                    // Цикл = ребро (u,v) + путь в дереве от u до v
                    var cyclePath = FindCyclePathInTree(u, v, parentMap);

                    if (cyclePath != null && cyclePath.Count > 0)
                    {
                        result.Components.Add(cyclePath);

                        // Анимация: подсвечиваем весь найденный цикл
                        // (можно пройтись по вершинам или ребрам цикла)
                        yield return new AlgorithmStep
                        {
                            IterationInfo = $"Цикл найден ({cyclePath.Count} вершин)",
                            // Можно добавить логику подсветки всего пути, если UI поддерживает список ID
                        };

                        // Сброс цвета хорды обратно (опционально, или оставляем как найденную)
                    }
                }
            }

            result.IsSuccess = true;
            result.StatusMessage = $"Готово. Найдено циклов: {result.Components.Count}";
        }

        // --- Реализация BFS для остова ---
        private IEnumerable<AlgorithmStep> BuildSpanningTreeBFS(int startNode, Dictionary<int, int> parentMap, HashSet<(int, int)> treeEdges)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            visited.Add(startNode);
            queue.Enqueue(startNode);
            parentMap[startNode] = -1; // Корень

            yield return new AlgorithmStep { VertexId = startNode, NewVertexState = VertexState.Selected, IterationInfo = "Корень (BFS)" };

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();

                if (_adjacencyList.ContainsKey(u))
                {
                    // Сортируем для детерминизма (опционально)
                    foreach (var edge in _adjacencyList[u].OrderBy(e => e.To))
                    {
                        int v = edge.To;
                        if (!visited.Contains(v))
                        {
                            visited.Add(v);
                            parentMap[v] = u;
                            queue.Enqueue(v);

                            // Добавляем в список ребер дерева
                            treeEdges.Add(u < v ? (u, v) : (v, u));

                            // Анимация добавления ребра дерева
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.TreeEdge, // Цвет ребра дерева
                                VertexId = v,
                                NewVertexState = VertexState.Active, // Помечаем вершину как посещенную
                                IterationInfo = $"Tree Edge {u}->{v}"
                            };
                        }
                    }
                }
            }
        }

        // --- Реализация DFS для остова ---
        private IEnumerable<AlgorithmStep> BuildSpanningTreeDFS(int startNode, Dictionary<int, int> parentMap, HashSet<(int, int)> treeEdges)
        {
            var visited = new HashSet<int>();
            var stack = new Stack<int>();

            stack.Push(startNode);
            // DFS требует пометки visited при извлечении или при добавлении. 
            // Для классического порядка PreOrder помечаем при добавлении (или сразу при извлечении, чтобы не дублировать в стеке).
            // Здесь используем вариант: кладем в стек, если не посещена.

            // Нюанс итеративного DFS: чтобы дерево было красивым ("глубоким"), 
            // соседей часто добавляют в обратном порядке, но это не строго обязательно.

            // Проще: используем visited сразу.
            visited.Add(startNode);
            parentMap[startNode] = -1;

            yield return new AlgorithmStep { VertexId = startNode, NewVertexState = VertexState.Selected, IterationInfo = "Корень (DFS)" };

            while (stack.Count > 0)
            {
                int u = stack.Peek(); // Смотрим, но не извлекаем сразу, чтобы пройти по ветке
                                      // Или классический вариант: Pop -> Push neighbors.

                // Реализуем вариант с Pop, он надежнее для остова
                u = stack.Pop();

                // Если уже обработали (могла попасть в стек несколько раз разными путями)
                // В данной реализации мы проверяем visited ПЕРЕД добавлением в стек, 
                // но для старта это не сработает.

                // Давайте перепишем на классику: Pop -> Visit -> Push unvisited neighbors
                // Но нам нужно ребро (parent), поэтому храним в стеке не просто int, а пару (u, parent).
                // Но мы уже имеем parentMap. 

                // Вернемся к простому:
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

        // --- Вспомогательный метод: Поиск пути между двумя вершинами в дереве (LCA - Lowest Common Ancestor) ---
        private List<int> FindCyclePathInTree(int startNode, int endNode, Dictionary<int, int> parentMap)
        {
            // У нас есть ребро (startNode, endNode), которого нет в дереве.
            // Нам нужно найти путь от startNode до endNode ИСКЛЮЧИТЕЛЬНО по ребрам дерева (через parentMap).

            // 1. Строим путь от startNode до корня
            var path1 = new List<int>();
            int curr = startNode;
            while (curr != -1 && parentMap.ContainsKey(curr)) // -1 корень
            {
                path1.Add(curr);
                if (parentMap[curr] == -1) break;
                curr = parentMap[curr];
            }
            if (curr != -1 && !path1.Contains(curr)) path1.Add(curr); // Добавляем корень

            // 2. Строим путь от endNode до корня
            var path2 = new List<int>();
            curr = endNode;
            while (curr != -1 && parentMap.ContainsKey(curr))
            {
                path2.Add(curr);
                if (parentMap[curr] == -1) break;
                curr = parentMap[curr];
            }
            if (curr != -1 && !path2.Contains(curr)) path2.Add(curr);

            // 3. Ищем LCA (первый общий элемент с конца списков, так как они идут к корню)
            // path1: [u, parent_u, ..., LCA, ..., root]
            // path2: [v, parent_v, ..., LCA, ..., root]

            int lca = -1;

            // Переворачиваем, чтобы корни были в начале индекса
            // path1Rev: [root, ..., LCA, ..., u]
            // path2Rev: [root, ..., LCA, ..., v]
            // Но проще использовать HashSet для поиска пересечения.

            // Найдем LCA через пересечение
            var path1Set = new HashSet<int>(path1);
            foreach (var node in path2)
            {
                if (path1Set.Contains(node))
                {
                    lca = node;
                    break; // Первый встреченный при подъеме от v — это LCA
                }
            }

            if (lca == -1) return new List<int>(); // Пути не пересеклись (разные компоненты связности)

            // 4. Склеиваем итоговый цикл
            // Часть 1: от u до LCA (не включая LCA, чтобы не дублировать)
            var cycle = new List<int>();

            foreach (var node in path1)
            {
                cycle.Add(node);
                if (node == lca) break;
            }

            // Часть 2: от LCA до v (в обратном порядке, т.к. path2 идет вверх)
            // Нам нужно: u -> ... -> LCA -> ... -> v -> u
            // Сейчас cycle: u -> ... -> LCA

            // Берем кусок path2 от v до LCA (исключая LCA, он уже добавлен)
            var tempPath = new List<int>();
            foreach (var node in path2)
            {
                if (node == lca) break;
                tempPath.Add(node);
            }
            // tempPath идет v -> parent_v -> ... 
            // Нам нужно присоединить его в обратном порядке (от LCA к v)
            tempPath.Reverse();
            cycle.AddRange(tempPath);

            // Замыкаем цикл (добавляем v, если его там нет из-за логики выше, но он должен быть началом path2)
            // Путь в массиве cycle сейчас: u -> ... -> LCA -> ... -> parent_v
            // Не хватает самого v в конце?
            // Давайте проверим:
            // path1: u, p_u, LCA
            // path2: v, p_v, LCA
            // Cycle logic: [u, p_u, LCA] + reverse([v, p_v]) -> [u, p_u, LCA, p_v, v]
            // И ребро (v, u) замыкает цикл. 

            // Вернем список вершин в порядке обхода цикла.
            return cycle;
        }

        #endregion
    }
}
