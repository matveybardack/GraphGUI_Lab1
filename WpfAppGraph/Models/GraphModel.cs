using System.Text;
using WpfAppGraph.Models.Enums;
using WpfAppGraph.Models.Structs;

namespace WpfAppGraph.Models
{
    public partial class GraphModel
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
    }
}