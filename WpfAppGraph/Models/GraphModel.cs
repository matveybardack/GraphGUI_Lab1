using System;
using System.Collections.Generic;
using System.Linq;
using WpfAppGraph.Models.Enums;

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

        public IEnumerable<AlgorithmStep> RunBFS(int startVertexId)
        {
            if (!_vertices.Contains(startVertexId))
                yield break;

            // Множество посещенных вершин
            var visited = new HashSet<int>();
            // Очередь для BFS
            var queue = new Queue<int>();

            // Начальный шаг
            visited.Add(startVertexId);
            queue.Enqueue(startVertexId);

            yield return new AlgorithmStep
            {
                VertexId = startVertexId,
                NewVertexState = VertexState.Active // Серый (в очереди)
            };

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                yield return new AlgorithmStep
                {
                    VertexId = current,
                    NewVertexState = VertexState.Selected // Оранжевый (текущая обрабатываемая)
                };

                // Проходим по всем соседям
                if (_adjacencyList.TryGetValue(current, out var edges))
                {
                    foreach (var edge in edges)
                    {
                        int neighbor = edge.To;

                        // Визуализируем, что мы смотрим на ребро
                        // Здесь мы не меняем тип ребра навсегда, просто показываем процесс?
                        // Или красим в TreeEdge, если идем по нему.

                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);

                            yield return new AlgorithmStep
                            {
                                EdgeFromId = current,
                                EdgeToId = neighbor,
                                NewEdgeType = EdgeType.TreeEdge, // Это ребро дерева обхода

                                VertexId = neighbor,
                                NewVertexState = VertexState.Active // Добавлена в очередь
                            };
                        }
                        else
                        {
                            // Если вершина уже посещена, ребро может быть Cross, Back или Forward (для BFS сложнее классификация)
                            // Для простоты подсветим, что ребро проверено, но вершина уже была
                            yield return new AlgorithmStep
                            {
                                EdgeFromId = current,
                                EdgeToId = neighbor,
                                NewEdgeType = EdgeType.CrossEdge // Условно помечаем как перекрестное/другое
                            };
                        }
                    }
                }

                // Завершили обработку вершины
                yield return new AlgorithmStep
                {
                    VertexId = current,
                    NewVertexState = VertexState.Visited // Голубой (полностью обработана)
                };
            }

            yield return new AlgorithmStep();
        }

        #endregion
    }
}