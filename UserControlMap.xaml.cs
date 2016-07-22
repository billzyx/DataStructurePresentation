using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace integrateOfDataStructure
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlMap
    {
        private readonly GraphClass _map;
        readonly string[] _titles = { "LogId", "操作", "起点Id", "终点Id", "内容" };
        private bool _isSingleStep = true;//演示方式的开关——true:单步演示,false:动画演示

        public UserControlMap()
        {
            _map=new GraphClass();
            DataContext = GraphClass.Vm;
            InitializeComponent();
            DrawTheTitle();
        }
        #region 事件处理函数
        private void BtnRandomInit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _map.RandomInit(CreateVertexSize.Text.Trim(), CreateEdgeSize.Text.Trim());
                //绘制日志
                DrawLogs(_map.LogId);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void BtnAddVertex_Click(object sender, RoutedEventArgs e)
        {//添加顶点

            try
            {
                _map.AddNode(TxtInsertNodeStart.Text.Trim(), TxtInsertNodeEnd.Text.Trim(), TxtNodeNameInsert.Text.Trim(),GraphLayout);
                //绘制日志
                DrawLogs(_map.LogId);
                //界面操作
                if (_map.GMatrix.NodeCount == 1)
                {
                    StackPanelRemoveVertex.Visibility = Visibility.Visible;
                }
                else if (_map.GMatrix.NodeCount == 2)
                {
                    StackPanelAddEdge.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnRemoveSelVertex_Click(object sender, RoutedEventArgs e)
        {   //删除选中结点
            try
            {
                _map.RemoveNode(TxtRemoveVertexValue.Text.Trim());
                //绘制日志
                DrawLogs(_map.LogId);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnAddEdge_Click(object sender, RoutedEventArgs e)
        {//添加边
            try
            {
                _map.AddEdge(TxtNewEdgeStart.Text.Trim(), TxtAddEdgeEnd.Text.Trim(), TxtAddEdgeWeight.Text.Trim());
                //绘制日志
                DrawLogs(_map.LogId);
                //界面操作
                if (_map.GMatrix.EdgeCount == 1)
                {
                    StackPanelRemoveEdge.Visibility = Visibility.Visible;
                    StackPanelAddVertice.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnRemoveEdge_Click(object sender, RoutedEventArgs e)
        {   //删除边
            try
            {
                _map.RemoveEdge(TxtRemoveEdgeStart.Text.Trim(), TxtRemoveEdgeEnd.Text.Trim());

                //绘制日志
                DrawLogs(_map.LogId);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void TxtBackTo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    _map.BackTo(TxtBackTo.Text.Trim());

                    //重新绘制日志
                    ReDrawLogs();
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }
            }
        }

        private void rb_singleStep_Checked(object sender, RoutedEventArgs e)
        {
            Grid1.Visibility = Visibility.Visible;
            Grid2.Visibility = Visibility.Hidden;
            _isSingleStep = true;
        }

        private void rb_animation_Checked(object sender, RoutedEventArgs e)
        {
            Grid1.Visibility = Visibility.Hidden;
            Grid2.Visibility = Visibility.Visible;
            _isSingleStep = false;
        }
        #endregion 事件处理函数

        #region 界面绘制函数
        private void ReDrawLogs()
        {
            LogGrid.Children.Clear();
            DrawTheTitle();
            //绘制日志
            for (int i = 0; i < _map.Maplogs.Count; i++)
                DrawLogs(i + 1);
        }

        /// <summary>
        /// 绘制表格标题
        /// </summary>
        /// <returns></returns>
        private void DrawTheTitle()
        {
            RowDefinition newRows = new RowDefinition {Height = new GridLength(27)};
            LogGrid.RowDefinitions.Add(newRows);
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                //列属性
                ColumnDefinition newCol = new ColumnDefinition {Width = new GridLength()};
                //像素
                LogGrid.ColumnDefinitions.Add(newCol);
            }
            int row = 0;//第一行
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                if (iCol == 0)
                {
                    Border border = new Border
                    {
                        BorderThickness = new Thickness(1, 1, 1, 1),
                        BorderBrush = Brushes.SlateGray
                    };
                    border.SetValue(Grid.ColumnProperty, iCol);
                    border.SetValue(Grid.RowProperty, row);
                    LogGrid.Children.Add(border);
                }
                else
                {
                    Border border = new Border
                    {
                        BorderThickness = new Thickness(0, 1, 1, 1),
                        BorderBrush = Brushes.SlateGray
                    };
                    border.SetValue(Grid.ColumnProperty, iCol);
                    border.SetValue(Grid.RowProperty, row);
                    LogGrid.Children.Add(border);
                }
                Label label = new Label {Content = _titles[iCol]};
                label.SetValue(Grid.ColumnProperty, iCol);
                label.SetValue(Grid.RowProperty, row);
                LogGrid.Children.Add(label);
            }
        }

        private void DrawLogs(int row)
        {
            //单元格绘制
            RowDefinition newRows = new RowDefinition {Height = new GridLength(26)};
            LogGrid.RowDefinitions.Add(newRows);
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                //列属性
                ColumnDefinition newCol = new ColumnDefinition {Width = new GridLength()};
                //像素
                LogGrid.ColumnDefinitions.Add(newCol);
            }

            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                if (iCol == 0)
                {
                    Border border = new Border
                    {
                        BorderThickness = new Thickness(1, 0, 1, 1),
                        BorderBrush = Brushes.SlateGray
                    };
                    border.SetValue(Grid.ColumnProperty, iCol);
                    border.SetValue(Grid.RowProperty, row);
                    LogGrid.Children.Add(border);
                }
                else
                {
                    Border border = new Border
                    {
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        BorderBrush = Brushes.SlateGray
                    };
                    border.SetValue(Grid.ColumnProperty, iCol);
                    border.SetValue(Grid.RowProperty, row);
                    LogGrid.Children.Add(border);
                }

                switch (iCol)
                {
                    case 0:
                        {
                            Label label = new Label {Content = _map.Maplogs[row - 1].LogId + ""};
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 1:
                        {
                            Label label = new Label {Content = _map.Maplogs[row - 1].Action + ""};
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 2:
                        {
                            Label label = new Label {Content = _map.Maplogs[row - 1].SelectId + ""};
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 3:
                        {
                            Label label = new Label {Content = _map.Maplogs[row - 1].TargetId + ""};
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 4:
                        {
                            Label label = new Label {Content = _map.Maplogs[row - 1].Data + ""};
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                }

            }
            if (row > 10)
            {
                LogGrid.Height += newRows.Height.Value;
            }
        }
        #endregion 界面绘制函数
    }
}
