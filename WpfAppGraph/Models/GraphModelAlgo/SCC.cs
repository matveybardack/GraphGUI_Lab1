using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
    {
        #region Создание матриц

        /// <summary>
        /// Формирует матрицу смежности графа.
        /// </summary>
        /// <returns>Двумерный массив весов рёбер (PositiveInfinity для отсутствующих рёбер).</returns>
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
        /// Формирует транспонированную матрицу смежности.
        /// </summary>
        /// <returns>Матрица с переставленными строками и столбцами относительно исходной.</returns>
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
        /// Поиск сильносвязных компонентов алгоритмом Косарайю.
        /// </summary>
        /// <param name="result">Контейнер для результата: список компонент связности.</param>
        /// <returns>Последовательность шагов для визуализации алгоритма.</returns>
        public IEnumerable<AlgorithmStep> RunFindStronglyConnectedComponents(SccResult result)
        {
            var vertices = GetVertices();
            var visited = new HashSet<int>();
            var stack = new Stack<int>();

            // Первый проход: порядок завершения вершин
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

            // Второй проход: выделение компонент на транспонированном графе
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

            // Классификация рёбер: внутри компонент и между компонентами
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
                if (!vertexToComponentIndex.ContainsKey(u)) continue;

                foreach (var edge in kvp.Value)
                {
                    int v = edge.To;
                    if (vertexToComponentIndex.ContainsKey(v))
                    {
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
        /// Рекурсивный обход для заполнения стека порядком завершения вершин.
        /// </summary>
        /// <param name="u">Текущая вершина обхода.</param>
        /// <param name="visited">Множество уже посещённых вершин.</param>
        /// <param name="stack">Стек для сохранения порядка завершения.</param>
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
        /// Обход транспонированного графа для выделения компоненты связности.
        /// </summary>
        /// <param name="u">Стартовая вершина обхода.</param>
        /// <param name="visited">Множество уже посещённых вершин.</param>
        /// <param name="component">Список для сбора вершин текущей компоненты.</param>
        /// <param name="matrixT">Транспонированная матрица смежности.</param>
        /// <param name="allVertices">Список всех вершин графа (для индексации).</param>
        /// <param name="idToIndex">Отображение ID вершины в индекс матрицы.</param>
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