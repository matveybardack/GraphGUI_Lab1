using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region Tree Traversals (Iterative)

        /// <summary>
        /// Прямой обход дерева / графа (NLR). Узел посещается ДО своих потомков.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunPreOrderIterative(int? startId, int? targetId, SearchResult result)
        {
            var context = InitTraversalContext(startId);

            foreach (var root in context.AllVertices)
            {
                if (context.Visited.Contains(root)) continue;

                foreach (var step in PushNode(root, context, -1)) yield return step;

                while (context.Stack.Count > 0)
                {
                    int u = context.Stack.Peek();
                    var children = context.NodeChildren[u];
                    int i = context.ChildIndex[u];

                    // PRE-ORDER логика: Посещаем себя немедленно (при первом взгляде на узел)
                    if (!context.HasVisitedSelf[u])
                    {
                        context.StructBuilder.Append($"{u} ");
                        if (targetId.HasValue && u == targetId.Value) context.GlobalTargetFound = true;
                        context.HasVisitedSelf[u] = true;
                    }

                    if (i < children.Count)
                    {
                        var edge = children[i];
                        int v = edge.To;
                        context.ChildIndex[u]++; // Сдвигаем индекс для следующего возврата

                        if (!context.Visited.Contains(v))
                        {
                            yield return new AlgorithmStep { EdgeFromId = u, EdgeToId = v, NewEdgeType = EdgeType.TreeEdge };
                            foreach (var step in PushNode(v, context, u)) yield return step;
                        }
                    }
                    else
                    {
                        // Все дети обработаны
                        foreach (var step in PopNode(context)) yield return step;
                    }
                }
            }

            FinalizeTreeTraversalResult(startId, targetId, context, result);
        }

        /// <summary>
        /// Центрированный обход дерева / графа (LNR). Узел посещается ПОСЛЕ первого потомка, но ДО остальных.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunInOrderIterative(int? startId, int? targetId, SearchResult result)
        {
            var context = InitTraversalContext(startId);

            foreach (var root in context.AllVertices)
            {
                if (context.Visited.Contains(root)) continue;

                foreach (var step in PushNode(root, context, -1)) yield return step;

                while (context.Stack.Count > 0)
                {
                    int u = context.Stack.Peek();
                    var children = context.NodeChildren[u];
                    int i = context.ChildIndex[u];

                    // IN-ORDER логика: Посещаем себя после того, как вернулись из 1-го ребенка (i > 0),
                    // либо если детей нет вообще (children.Count == 0)
                    if (!context.HasVisitedSelf[u])
                    {
                        if (children.Count == 0 || i > 0)
                        {
                            context.StructBuilder.Append($"{u} ");
                            if (targetId.HasValue && u == targetId.Value) context.GlobalTargetFound = true;
                            context.HasVisitedSelf[u] = true;
                        }
                    }

                    if (i < children.Count)
                    {
                        var edge = children[i];
                        int v = edge.To;
                        context.ChildIndex[u]++;

                        if (!context.Visited.Contains(v))
                        {
                            yield return new AlgorithmStep { EdgeFromId = u, EdgeToId = v, NewEdgeType = EdgeType.TreeEdge };
                            foreach (var step in PushNode(v, context, u)) yield return step;
                        }
                    }
                    else
                    {
                        foreach (var step in PopNode(context)) yield return step;
                    }
                }
            }

            FinalizeTreeTraversalResult(startId, targetId, context, result);
        }

        /// <summary>
        /// Обратный обход дерева / графа (LRN). Узел посещается ПОСЛЕ всех своих потомков.
        /// </summary>
        public IEnumerable<AlgorithmStep> RunPostOrderIterative(int? startId, int? targetId, SearchResult result)
        {
            var context = InitTraversalContext(startId);

            foreach (var root in context.AllVertices)
            {
                if (context.Visited.Contains(root)) continue;

                foreach (var step in PushNode(root, context, -1)) yield return step;

                while (context.Stack.Count > 0)
                {
                    int u = context.Stack.Peek();
                    var children = context.NodeChildren[u];
                    int i = context.ChildIndex[u];

                    if (i < children.Count)
                    {
                        var edge = children[i];
                        int v = edge.To;
                        context.ChildIndex[u]++;

                        if (!context.Visited.Contains(v))
                        {
                            yield return new AlgorithmStep { EdgeFromId = u, EdgeToId = v, NewEdgeType = EdgeType.TreeEdge };
                            foreach (var step in PushNode(v, context, u)) yield return step;
                        }
                    }
                    else
                    {
                        // POST-ORDER логика: Посещаем себя после завершения ВСЕХ детей
                        if (!context.HasVisitedSelf[u])
                        {
                            context.StructBuilder.Append($"{u} ");
                            if (targetId.HasValue && u == targetId.Value) context.GlobalTargetFound = true;
                            context.HasVisitedSelf[u] = true;
                        }

                        foreach (var step in PopNode(context)) yield return step;
                    }
                }
            }

            FinalizeTreeTraversalResult(startId, targetId, context, result);
        }

        #endregion

        #region Helpers for Traversals

        // Вспомогательный класс-контекст для передачи состояния между методами
        private class TraversalContext
        {
            public List<int> AllVertices;
            public HashSet<int> Visited = new();
            public Dictionary<int, int> ParentMap = new();
            public Dictionary<int, int> DiscoveryTime = new();
            public Dictionary<int, int> FinishTime = new();
            public Stack<int> Stack = new();

            public Dictionary<int, List<GraphEdge>> NodeChildren = new();
            public Dictionary<int, int> ChildIndex = new();
            public Dictionary<int, bool> HasVisitedSelf = new();

            public int Timer = 1;
            public bool GlobalTargetFound = false;
            public StringBuilder StructBuilder = new();
        }

        private TraversalContext InitTraversalContext(int? startId)
        {
            var context = new TraversalContext
            {
                AllVertices = GetVertices()
            };

            if (startId.HasValue && context.AllVertices.Contains(startId.Value))
            {
                context.AllVertices.Remove(startId.Value);
                context.AllVertices.Insert(0, startId.Value);
            }
            return context;
        }

        private IEnumerable<AlgorithmStep> PushNode(int node, TraversalContext ctx, int parent)
        {
            ctx.Visited.Add(node);
            ctx.Stack.Push(node);
            if (parent != -1) ctx.ParentMap[node] = parent;

            ctx.ChildIndex[node] = 0;
            ctx.HasVisitedSelf[node] = false;

            // Заранее определяем порядок дочерних элементов
            ctx.NodeChildren[node] = _adjacencyList.ContainsKey(node)
                ? _adjacencyList[node].OrderBy(e => e.To).Where(e => !ctx.Visited.Contains(e.To)).ToList()
                : new List<GraphEdge>();

            ctx.DiscoveryTime[node] = ctx.Timer++;

            yield return new AlgorithmStep
            {
                VertexId = node,
                NewVertexState = VertexState.Active,
                IterationInfo = $"{ctx.DiscoveryTime[node]}/-"
            };
        }

        private IEnumerable<AlgorithmStep> PopNode(TraversalContext ctx)
        {
            int u = ctx.Stack.Pop();
            ctx.FinishTime[u] = ctx.Timer++;

            yield return new AlgorithmStep
            {
                VertexId = u,
                NewVertexState = VertexState.Finished,
                IterationInfo = $"{ctx.DiscoveryTime[u]}/{ctx.FinishTime[u]}"
            };
        }

        private void FinalizeTreeTraversalResult(int? startId, int? targetId, TraversalContext ctx, SearchResult result)
        {
            result.IsTargetFound = ctx.GlobalTargetFound;
            result.ParenthesisStructure = ctx.StructBuilder.ToString().Trim();

            if (ctx.GlobalTargetFound && targetId.HasValue)
            {
                var tempPath = new List<int>();
                int curr = targetId.Value;
                tempPath.Add(curr);

                while (ctx.ParentMap.ContainsKey(curr))
                {
                    int p = ctx.ParentMap[curr];

                    if (_adjacencyList.ContainsKey(p))
                    {
                        var edge = _adjacencyList[p].FirstOrDefault(e => e.To == curr);
                        if (edge != null)
                            result.PathLength += edge.Weight;
                    }

                    curr = p;
                    tempPath.Add(curr);
                }

                // Проверка валидности
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
