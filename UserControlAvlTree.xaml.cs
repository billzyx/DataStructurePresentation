using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using integrateOfDataStructure.Utility;

namespace integrateOfDataStructure
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlAvlTree
    {
        readonly AvlTree _avlTree;
        readonly string[] _titles = { "LogId", "操作", "内容" };
        private DispatcherTimer _timer;//定时器
        private int _tick;//时钟的滴答
        private int _stateOfInsertOrRemoveOrDefault;// 标识当前状态为插入还是删除 0:默认状态 1:当前为插入状态 2:当前为删除状态
        private bool _isStepThrough = true;//演示方式的开关——true:单步演示,false:动画演示

        public UserControlAvlTree()
        {
            DataContext = AvlTree.Vm;
            InitializeComponent();
            _avlTree = new AvlTree(GraphLayout);
            DrawTheTitle();
        }

        #region 事件处理函数
        private void RandomInit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_avlTree.Size() > 0)
                {
                    if (MessageBox.Show("此操作将新建一棵平衡二叉树，是否继续？", "确认窗口", MessageBoxButton.YesNo,
                        MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        _avlTree.Clear();
                    }
                    else return;
                }
                //类操作
                _avlTree.RandomInit(TxtCreateSize.Text.Trim());

                //界面控制
                GridTheFormer.Visibility = Visibility.Hidden;
                GridTheLatter.Visibility = Visibility.Visible;
                TxtSelect.Text = TxtAddRoot.Text.Trim();
                //绘制日志
                DrawLogs(_avlTree.LogId);
                if (_isStepThrough == true)
                {
                    ControlPanel.IsEnabled = false;
                    Nextstep.Content = "完成";
                }
            }
            catch (Exception exception)
            {
                if (exception.Message.Equals("考虑到性能与显示上的问题，规定最大初始化节点数量为100！"))
                {
                    if (MessageBox.Show(exception.Message, "初始化长度最大为100", MessageBoxButton.OK,
                        MessageBoxImage.Information) == MessageBoxResult.OK)
                    {
                        TxtCreateSize.Text = "100";
                        RandomInit_Click(sender, e);
                    }
                }
                else
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
                    _avlTree.AddRoot(TxtAddRoot.Text.Trim());

                    //界面控制
                    GridTheFormer.Visibility = Visibility.Hidden;
                    GridTheLatter.Visibility = Visibility.Visible;
                    TxtSelect.Text = TxtAddRoot.Text.Trim();
                    //绘制日志
                    DrawLogs(_avlTree.LogId);
                    if (_isStepThrough == true)
                    {
                        ControlPanel.IsEnabled = false;
                        Nextstep.Content = "完成";
                    }
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
                int step=_avlTree.AddChild(TxtSelect.Text.Trim(),_isStepThrough);
                //当前为插入状态
                _stateOfInsertOrRemoveOrDefault = 1;
                //绘制日志
                DrawLogs(_avlTree.LogId);
                if (_isStepThrough == true)
                {
                    ControlPanel.IsEnabled = false;
                }
                if (step == 0)
                {
                    Nextstep.Content = "完成";
                }
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
                int step=_avlTree.DeleteSelNode(TbRemoveValue.Text.Trim(), _isStepThrough);
                //当前为删除状态
                _stateOfInsertOrRemoveOrDefault = 2;
                //绘制日志
                DrawLogs(_avlTree.LogId);
                if (_avlTree.Size() == 0)
                {//若删除根节点则调回创建根节点按钮
                    GridTheFormer.Visibility = Visibility.Visible;
                    GridTheLatter.Visibility = Visibility.Hidden;
                }
                if (_isStepThrough == true)
                {
                    ControlPanel.IsEnabled = false;
                }
                if (step == 0)
                {
                    Nextstep.Content = "完成";
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnStartTraversal_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    if (BtnStartTraversal.Content.Equals("开始遍历"))
            //    {
            //        if (_avlTree.Size() != 0)
            //        {
            //            string order;
            //            if (RbPreorder.IsChecked.Equals(true))
            //                order = "先序";
            //            else if (RbInorder.IsChecked.Equals(true))
            //                order = "中序";
            //            else
            //                order = "后序";
            //            _avlTree.Traversal(order, GraphLayout);
            //            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            //            _timer.Tick += timer_Tick;
            //            _timer.Start();
            //        }
            //    }
            //    else
            //    {
            //        _avlTree.RefreshCanvas();
            //        BtnStartTraversal.Content = "开始遍历";
            //    }
            //}
            //catch (Exception ee)
            //{
            //    MessageBox.Show(ee.Message);
            //}
        }

        private void tb_back_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Return)
            //{
            //    try
            //    {
            //        _avlTree.BackTo(TbBack.Text.Trim());
            //        //重新绘制日志
            //        ReDrawLogs();
            //    }
            //    catch (Exception ee)
            //    {
            //        MessageBox.Show(ee.Message);
            //    }
            //}
        }
        
        private void nextstep_Click(object sender, RoutedEventArgs e)
        {
            if (Nextstep.Content.Equals("完成"))
            {
                //操作完成标识归零
                _stateOfInsertOrRemoveOrDefault = 0;
                //滚动条移动
                SvGraphvizShow.ScrollToTop();
                
                Nextstep.Content = "下一步";
                ControlPanel.IsEnabled = true;
            }
            else
            {
                if (_stateOfInsertOrRemoveOrDefault == 0)
                    return;
                int step = _stateOfInsertOrRemoveOrDefault == 1 ? _avlTree.AddStepByStep() : _avlTree.RemoveStepByStep();
                if (step == 0)
                {
                    Nextstep.Content = "完成";
                }
            }
        }

        #endregion 事件处理函数

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
            LogVo tlogVo = (LogVo)_avlTree.BTreeLogs[row + ""];
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
            for (int i = 1; i <= _avlTree.BTreeLogs.Count; i++)
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

        private void rb_singleStep_Checked(object sender, RoutedEventArgs e)
        {
            Grid1.Visibility = Visibility.Visible;
            Grid2.Visibility = Visibility.Hidden;
            _isStepThrough = true;
        }

        private void rb_animation_Checked(object sender, RoutedEventArgs e)
        {
            Grid1.Visibility = Visibility.Hidden;
            Grid2.Visibility = Visibility.Visible;
            _isStepThrough = false;
        }

        #endregion 界面绘制函数


        
    }
}