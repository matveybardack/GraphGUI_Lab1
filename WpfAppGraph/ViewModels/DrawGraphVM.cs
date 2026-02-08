using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Data;
using System.Linq;
using System.Windows;
using WpfAppGraph.Models;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.ViewModels
{
    public partial class DrawGraphVM : ObservableObject
    {
        public GraphCanvasVM GraphCanvas { get; } = new GraphCanvasVM();
        private readonly GraphModel _graphModel = new GraphModel();

        [ObservableProperty]
        private GraphTool _currentTool = GraphTool.AddVertex; 

        [ObservableProperty]
        private object _activeDialog;

        [ObservableProperty]
        private int _vertexCount;

        [ObservableProperty]
        private int _edgeCount;

        [ObservableProperty]
        private DataView _adjacencyMatrixView;

        private VertexViewModel _firstSelectedVertex;

        public DrawGraphVM()
        {
            GraphCanvas.CanvasClicked += OnCanvasClicked;
            GraphCanvas.VertexClicked += OnVertexClicked;
        }

        /// <summary>
        /// Добавление вершины
        /// </summary>
        /// <param name="point"> координаты добавления на холст </param>
        private void OnCanvasClicked(Point point)
        {
            if (CurrentTool == GraphTool.AddEdge && _firstSelectedVertex != null)
            {
                ResetSelection();
                return;
            }

            if (CurrentTool == GraphTool.AddVertex)
            {
                var newVertexVm = GraphCanvas.AddVertex(point.X, point.Y);
                _graphModel.AddVertex(newVertexVm.Id);

                UpdateStats();
            }
        }

        /// <summary>
        /// Добавление ребра / установка цели
        /// </summary>
        /// <param name="vertex"> вершина, на которую нажали </param>
        private void OnVertexClicked(VertexViewModel vertex)
        {
            switch (CurrentTool)
            {
                case GraphTool.AddVertex:
                    break;

                case GraphTool.SetTarget:
                    HandleSetTarget(vertex);
                    break;

                case GraphTool.AddEdge:
                    HandleAddEdge(vertex);
                    break;
            }
        }

        /// <summary>
        /// Установка цели
        /// </summary>
        /// <param name="vertex"></param>
        private void HandleSetTarget(VertexViewModel vertex)
        {
            var oldTarget = GraphCanvas.Vertices.FirstOrDefault(v => v.State == VertexState.Target);
            oldTarget?.State = VertexState.Default;

            if (oldTarget == vertex) return;

            vertex.State = VertexState.Target;
        }

        /// <summary>
        /// Добавление ребра
        /// </summary>
        /// <param name="clickedVertex"> выбранная вершина </param>
        private void HandleAddEdge(VertexViewModel clickedVertex)
        {
            if (_firstSelectedVertex == null)
            {
                _firstSelectedVertex = clickedVertex;
                _firstSelectedVertex.State = VertexState.Selected; // Подсвечивание первой вершины
            }

            else if (_firstSelectedVertex == clickedVertex)
            {
                ResetSelection(); //TODO: Добавление петли
            }
            else
            {
                OpenEdgeDialog(_firstSelectedVertex, clickedVertex);
            }
        }

        /// <summary>
        /// Открытие диалогового окна
        /// </summary>
        /// <param name="source"> откуда </param>
        /// <param name="target"> куда </param>
        private void OpenEdgeDialog(VertexViewModel source, VertexViewModel target)
        {
            var dialogVm = new EdgeDialogVM(
                onConfirm: (weight, isDirected) =>
                {
                    CreateEdge(source, target, weight, isDirected);
                    CloseDialog();
                },
                onCancel: () =>
                {
                    CloseDialog();
                    ResetSelection();
                }
            );

            ActiveDialog = dialogVm;
        }

        /// <summary>
        /// Создвние ребра
        /// </summary>
        /// <param name="source"> вершина, из которой выходит </param>
        /// <param name="target"> вершина, в которую идет </param>
        /// <param name="weight"> вес </param>
        /// <param name="isDirected"> является ли ориентированным </param>
        private void CreateEdge(VertexViewModel source, VertexViewModel target, double weight, bool isDirected)
        {
            GraphCanvas.AddEdge(source, target, weight, isDirected);
            _graphModel.AddEdge(source.Id, target.Id, weight, isDirected);

            ResetSelection();
            UpdateStats();
        }

        private void CloseDialog() => ActiveDialog = null;

        /// <summary>
        /// Снятие выделения первой вершины
        /// </summary>
        private void ResetSelection()
        {
            if (_firstSelectedVertex != null)
            {
                if (_firstSelectedVertex.State != VertexState.Target)
                    _firstSelectedVertex.State = VertexState.Default;

                _firstSelectedVertex = null;
            }
        }

        [RelayCommand]
        private void ResetGraph()
        {
            GraphCanvas.ClearGraph();
            _graphModel.Clear();
            ResetSelection();
            UpdateStats();
        }

        /// <summary>
        /// Построение матрицы смежности
        /// </summary>
        private void BuildAdjacencyMatrixDataTable()
        {
            var vertices = _graphModel.GetVertices();
            var matrix = _graphModel.GetAdjacencyMatrix();
            int n = vertices.Count;

            var table = new DataTable();

            table.Columns.Add("v", typeof(string));
            foreach (var vertexId in vertices)
                table.Columns.Add(vertexId.ToString(), typeof(string));

            for (int i = 0; i < n; i++)
            {
                var row = table.NewRow();
                row[0] = vertices[i].ToString(); // Заголовок ряда

                for (int j = 0; j < n; j++)
                {
                    double val = matrix[i, j];
                    row[j + 1] = double.IsPositiveInfinity(val) ? "-" : val.ToString("0.#");
                }
                table.Rows.Add(row);
            }

            AdjacencyMatrixView = table.DefaultView;
        }

        /// <summary>
        /// Обновление холста
        /// </summary>
        private void UpdateStats()
        {
            VertexCount = GraphCanvas.Vertices.Count;
            EdgeCount = GraphCanvas.Edges.Count;
            BuildAdjacencyMatrixDataTable();
        }

        public GraphModel GetGraphModel() => _graphModel;
    }
}