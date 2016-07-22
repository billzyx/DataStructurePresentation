using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using integrateOfDataStructure.Utility;

namespace integrateOfDataStructure
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlMTree
    {
        readonly MultiTree _mtree;
        readonly string[] _titles = { "LogId", "操作", "父节点data", "内容" };
        private DispatcherTimer _timer;//定时器
        private int _tick;//时钟的滴答
        private bool _isSingleStep = true;//演示方式的开关——true:单步演示,false:动画演示

        public UserControlMTree()
        {
            _mtree = new MultiTree();
            DataContext = MultiTree.Vm;
            InitializeComponent();
            DrawTheTitle();
        }

        

        
        #region 事件处理函数
        private void RandomInit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mtree.Size() > 0)
                {
                    if (MessageBox.Show("此操作会重新生成一棵多叉树，是否继续？", "确认窗口", MessageBoxButton.YesNo,
                        MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        _mtree.Clear();
                    }
                    else return;
                }
                //类操作
                _mtree.RandomInit();

                //界面控制
                GridTheFormer.Visibility = Visibility.Hidden;
                GridTheLatter.Visibility = Visibility.Visible;
                TxtSelect.Text = TxtAddRoot.Text.Trim();
                //绘制日志
                DrawLogs(_mtree.LogId);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void BtnAddRoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtAddRoot.Text.Trim() != "")
                {
                    //类操作
                    _mtree.AddRoot(TxtAddRoot.Text.Trim());

                    //界面控制
                    GridTheFormer.Visibility = Visibility.Hidden;
                    GridTheLatter.Visibility = Visibility.Visible;
                    TxtSelect.Text = TxtAddRoot.Text.Trim();
                    //绘制日志
                    DrawLogs(_mtree.LogId);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void btnAddChild_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Regex regex = new Regex("[0-9]{1,}");
                if (!regex.IsMatch(TxtChildIndex.Text.Trim()))
                    throw new Exception("\"孩子位置\"处应该输入数字！");
                _mtree.AddAChild(TxtSelect.Text.Trim(), int.Parse(TxtChildIndex.Text.Trim()), TxtChildData.Text.Trim(), GraphLayout);
                //绘制日志
                DrawLogs(_mtree.LogId);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnDelSelNode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mtree.DeleteSelNode(TbRemoveValue.Text.Trim());
                //绘制日志
                DrawLogs(_mtree.LogId);
                if (_mtree.LogId == 0)
                {//若删除根节点则调回创建根节点按钮
                    GridTheFormer.Visibility = Visibility.Visible;
                    GridTheLatter.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnStartTraversal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnStartTraversal.Content.Equals("开始遍历"))
                {
                    if (_mtree.Size() != 0)
                    {
                        string order = RbDfs.IsChecked.Equals(true) ? "深度优先" : "广度优先";
                        _mtree.Traversal(order, GraphLayout);
                        _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                        _timer.Tick += timer_Tick;
                        _timer.Start();
                    }
                }
                else
                {
                    _mtree.RefreshCanvas();
                    BtnStartTraversal.Content = "开始遍历";
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void tb_back_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    _mtree.BackTo(TbBack.Text.Trim());
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

        #endregion

        #region 界面绘制函数
        //日志表格绘制
        private void DrawLogs(int row)
        {
            if (row == 0)
            {
                ReDrawLogs();
                return;
            }

            //单元格绘制
            RowDefinition newRows = new RowDefinition { Height = new GridLength(26) };
            LogGrid.RowDefinitions.Add(newRows);
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                //列属性
                ColumnDefinition newCol = new ColumnDefinition { Width = new GridLength() };
                //像素
                LogGrid.ColumnDefinitions.Add(newCol);
            }
            LogVo tlogVo = (LogVo)_mtree.MTreeLogs[row + ""];
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
                            Label label = new Label { Content = tlogVo.LogId + "" };
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 1:
                        {
                            Label label = new Label { Content = tlogVo.Action + "" };
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 2:
                        {
                            Label label = new Label { Content = tlogVo.Selectdata + "" };
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 3:
                        {
                            Label label = new Label { Content = tlogVo.Data + "" };
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
        //重绘日志
        private void ReDrawLogs()
        {
            LogGrid.Children.Clear();
            DrawTheTitle();
            for (int i = 1; i <= _mtree.MTreeLogs.Count; i++)
                DrawLogs(i);
        }

        /// 绘制表格标题
        private void DrawTheTitle()
        {
            RowDefinition newRows = new RowDefinition { Height = new GridLength(27) };
            LogGrid.RowDefinitions.Add(newRows);
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                //列属性
                ColumnDefinition newCol = new ColumnDefinition { Width = new GridLength() };
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
                Label label = new Label { Content = _titles[iCol] };
                label.SetValue(Grid.ColumnProperty, iCol);
                label.SetValue(Grid.RowProperty, row);
                LogGrid.Children.Add(label);
            }
        }
        #endregion 界面绘制函数

        #region 内部工具函数
        private void timer_Tick(object sender, EventArgs e)
        {
            if (_tick >= _mtree.Size() - 1)
            {
                BtnStartTraversal.Content = "遍历完成";
                _tick = 0;
                _timer.Stop();
            }
            else
            {
                _tick++;
            }
        }
        #endregion 内部工具函数

    }

}