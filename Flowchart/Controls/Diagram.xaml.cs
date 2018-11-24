using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Flowchart
{
    /// <summary>
    /// Interaction logic for Diagram.xaml
    /// </summary>
    [ContentProperty(nameof(Children))]
    public partial class Diagram : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<DiagramGridChangedEventArgs> DiagramSizeChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Children of the root grid.
        /// </summary>
        public UIElementCollection Children
        {
            get { return (UIElementCollection)GetValue(ChildrenProperty.DependencyProperty); }
            private set { SetValue(ChildrenProperty, value); }
        }

        private Node _draggedNode = null;
        public Node DraggedNode
        {
            get => _draggedNode;
            set
            {
                if (value != _draggedNode)
                {
                    _draggedNode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DraggedNode)));
                }
            }
        }

        /// <summary>
        /// Amount of grid rows in the diagram.
        /// </summary>
        public int Rows
        {
            get => RootGrid.RowDefinitions.Count;
            set
            {
                if (value < 1)
                    return;

                // TODO
                if (value > 64)
                    return;

                int delta = value - Rows;
                if (delta == 0) return;
                DiagramSizeChanged?.Invoke(this, new DiagramGridChangedEventArgs(delta, 0));

                if (delta > 0)
                    for (int i = 0; i < delta; i++)
                        RootGrid.RowDefinitions.Add(new RowDefinition());

                else
                    for (int i = 0; i < -delta; i++)
                        RootGrid.RowDefinitions.RemoveAt(Rows - 1);
            }
        }

        /// <summary>
        /// Amount of grid columns in the diagram.
        /// </summary>
        public int Columns
        {
            get => RootGrid.ColumnDefinitions.Count;
            set
            {
                if (value < 1)
                    return;

                // TODO
                if (value > 64)
                    return;

                int delta = value - Columns;
                if (delta == 0) return;
                DiagramSizeChanged?.Invoke(this, new DiagramGridChangedEventArgs(0, delta));

                if (delta > 0)
                    for (int i = 0; i < delta; i++)
                        RootGrid.ColumnDefinitions.Add(new ColumnDefinition());

                else
                    for (int i = 0; i < -delta; i++)
                        RootGrid.ColumnDefinitions.RemoveAt(Columns - 1);
            }
        }

        public Diagram()
        {
            InitializeComponent();
            DataContext = this;
            Children = RootGrid.Children;
        }

        // Source: https://stackoverflow.com/a/9186486/
        public static readonly DependencyPropertyKey ChildrenProperty =
            DependencyProperty.RegisterReadOnly(
                nameof(Children),
                typeof(UIElementCollection),
                typeof(Diagram),
                new PropertyMetadata());


        private void RootGrid_Drop(object sender, DragEventArgs e)
        {
            UpdateDragDrop(sender, e);
            // TODO: handle size constraints
        }

        private void UpdateDragDrop(object sender, DragEventArgs e)
        {
            Node node = (Node)e.Data.GetData(nameof(Node));
            Point gridPosition = GetPositionInGrid(e.GetPosition(RootGrid));

            node.Column = (int)gridPosition.X - (node.ColumnSpan - 1);
            node.Row = (int)gridPosition.Y;
        }

        /// <summary>
        /// Gets the row and column of <paramref name="point"/> in <see cref="RootGrid"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private Point GetPositionInGrid(Point point)
        {
            int row = 0;
            int column = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in RootGrid.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            foreach (var columnDefinition in RootGrid.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                column++;
            }

            return new Point(column, row);
        }
    }

    /// <summary>
    /// Provides data on the change whenever <see cref="Diagram"/>'s dimensions are changed.
    /// </summary>
    public class DiagramGridChangedEventArgs
    {
        public DiagramGridChangedEventArgs(int rowDelta, int columnDelta)
        {
            this.RowDelta = rowDelta;
            this.ColumnDelta = columnDelta;
        }

        public int RowDelta { get; }
        public int ColumnDelta { get; }
    }
}
