using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
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

        #region SCC algo
        /// <summary>
        /// Поиск сильносвязанных компонентов (SCC) алгоритмом Косарайю.
        /// </summary>
        /// <param name="result">Список списков из групп компонентов</param>
        /// <returns>Ленивая коллекция шагов для анимации</returns>
        public IEnumerable<AlgorithmStep> RunFindStronglyConnectedComponents(SccResult result)
        {
            var vertices = GetVertices();
            var visited = new HashSet<int>();
            var stack = new Stack<int>();

            // Первый проход: DFS на оригинальном графе
            foreach (var v in vertices)
            {
                if (!visited.Contains(v))
                {
                    FillOrder(v, visited, stack);
                }
            }

            double[,] matrixT = GetTransposedAdjacencyMatrix();

            var idToIndex = new Dictionary<int, int>();
            for (int i = 0; i < vertices.Count; i++) idToIndex[vertices[i]] = i;

            visited.Clear();

            // Второй проход: DFS по транспонированному графу
            while (stack.Count > 0)
            {
                int v = stack.Pop();

                if (!visited.Contains(v))
                {
                    var component = new List<int>();

                    DfsTransposed(v, visited, component, matrixT, vertices, idToIndex);

                    result.Components.Add(component);

                    foreach (var vertexId in component)
                    {
                        yield return new AlgorithmStep
                        {
                            VertexId = vertexId,
                            NewVertexState = VertexState.Finished,
                            IterationInfo = $"SCC #{result.Components.Count}"
                        };
                    }
                }
            }

            // Классификация ребер по группам компонентов
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
                // Если вершина не попала ни в один компонент (на всякий)
                if (!vertexToComponentIndex.ContainsKey(u)) continue;

                foreach (var edge in kvp.Value)
                {
                    int v = edge.To;
                    if (vertexToComponentIndex.ContainsKey(v))
                    {
                        // лежат ли U и V в одном компоненте
                        if (vertexToComponentIndex[u] == vertexToComponentIndex[v])
                        {
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = u,
                                EdgeToId = v,
                                NewEdgeType = EdgeType.BackEdge
                            };
                        }
                        else
                        {
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

        /// <summary>
        /// Вспомогательный метод для прямого прохода
        /// </summary>
        /// <param name="u"> вершина </param>
        /// <param name="visited"> множество посещенных </param>
        /// <param name="stack"> стек посещений </param>
        private void FillOrder(int u, HashSet<int> visited, Stack<int> stack)
        {
            visited.Add(u);

            if (_adjacencyList.TryGetValue(u, out var edges))
                foreach (var edge in edges)
                    if (!visited.Contains(edge.To))
                        FillOrder(edge.To, visited, stack);

            stack.Push(u);
        }

        /// <summary>
        /// Вспомогательный метод для обратного прохода по матрице
        /// </summary>
        /// <param name="u"> вершина </param>
        /// <param name="visited"> множество посещенных </param>
        /// <param name="component"> группа компонентов </param>
        /// <param name="matrixT"> транспанированная матрица </param>
        /// <param name="allVertices"></param>
        /// <param name="idToIndex"></param>
        private void DfsTransposed(int u, HashSet<int> visited, List<int> component, double[,] matrixT, List<int> allVertices, Dictionary<int, int> idToIndex)
        {
            visited.Add(u);
            component.Add(u);

            int uIndex = idToIndex[u];
            int n = allVertices.Count;

            for (int i = 0; i < n; i++)
            {
                if (!double.IsPositiveInfinity(matrixT[uIndex, i]))
                {
                    int v = allVertices[i];
                    if (!visited.Contains(v))
                    {
                        DfsTransposed(v, visited, component, matrixT, allVertices, idToIndex);
                    }
                }
            }
        }
        #endregion
    }
}
