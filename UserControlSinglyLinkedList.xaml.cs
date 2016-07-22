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
    /// UserControl2.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlSinglyLinkedList
    {
        readonly SinglyLinkedList _list;
        readonly string[] _titles = { "LogId", "操作", "索引", "内容" };
        readonly DispatcherTimer _shrinkTimer = new DispatcherTimer();
        private int _shrinkTime;
        private DispatcherTimer _timer;//定时器
        private int _tick;//时钟的滴答
        private int _tickTo;//滴答的终点
        private int _stateOfInsertOrRemoveOrDefault;// 标识当前状态为插入还是删除 0:默认状态 1:当前为插入状态 2:当前为删除状态
        private bool _isSingleStep = true;//演示方式的开关——true:单步演示,false:动画演示

        public UserControlSinglyLinkedList()
        {
            DataContext = SinglyLinkedList.Vm;
            InitializeComponent();
            _list = new SinglyLinkedList(GraphLayout);
            DrawTheTitle();
            UiScaleSlider.MouseDoubleClick += RestoreScalingFactor;
            _shrinkTimer.Tick += ShrinkTick;
            _shrinkTimer.Interval = new TimeSpan(0, 0, 1);
        }

        #region 事件处理函数
        /// <summary>
        /// 随机初始化按钮事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void btn_create_ramdom(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_list.IsEmpty())
                {
                    if (MessageBox.Show("初始化操作会清空之前的链表，是否继续？", "确认窗口", MessageBoxButton.YesNo,
                        MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        _list.Clear();
                    }
                    else return;
                }
                _list.RandomInit(TxtCreateSize.Text.Trim());
                //绘制日志
                DrawLogs(_list.LogId);

                AssignValueTo_tb_remove_value(0);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// 插入按钮事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void btn_insert_k_th(object sender, RoutedEventArgs e)
        {
            try
            {
                _list.InsertTo(TbLocins.Text.Trim(), TxtInsVal.Text.Trim(), GraphLayout, _isSingleStep);
                //当前为插入状态
                _stateOfInsertOrRemoveOrDefault = 1;
                //绘制日志
                DrawLogs(_list.LogId);
                //滚动条移动
                if (_isSingleStep.Equals(true))
                {
                    SvGraphvizShow.ScrollToVerticalOffset(SinglyLinkedList.Vm.AddMyPoint.Y - 20 * 1.7); //20是一个顶点的高度
                    //局部放大
                    DoubleAnimation sliderAnimation = new DoubleAnimation(1, 3, new Duration(TimeSpan.FromSeconds(0.8)),
                        FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);
                }
                else
                {
                    SvGraphvizShow.ScrollToVerticalOffset(SinglyLinkedList.Vm.ViewModelGetMyNode(TxtInsVal.Text.Trim(), int.Parse(TbLocins.Text.Trim()), _list.GraphLayout).Y - 20 * 1.7); //20是一个顶点的高度
                    //局部放大
                    DoubleAnimation sliderAnimation = new DoubleAnimation(1, 3, new Duration(TimeSpan.FromSeconds(0.8)),
                        FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);
                    
                    _shrinkTimer.Start();
                }
                AssignValueTo_tb_remove_value(1);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        /// <summary>
        /// 删除按钮事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void btn_remove(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RbRemoveVal.IsChecked == true)//根据节点值删除
                    _list.Remove(TbRemoveValue.Text.Trim(), GraphLayout, _isSingleStep);
                else //根据节点位置删除
                {
                    var location = int.Parse(TbRemoveLocation.Text.Trim());
                    _list.Remove(location, GraphLayout, _isSingleStep);
                }
                //当前为删除状态
                _stateOfInsertOrRemoveOrDefault = 2;
                //绘制日志
                DrawLogs(_list.LogId);

                if (_isSingleStep.Equals(true))
                {
                    SvGraphvizShow.ScrollToVerticalOffset(SinglyLinkedList.Vm.AddMyPoint.Y - 20 * 1.7); //20是一个顶点的高度
                    //局部放大
                    DoubleAnimation sliderAnimation = new DoubleAnimation(1, 3, new Duration(TimeSpan.FromSeconds(0.8)),
                        FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);
                }
                else
                {
                    SvGraphvizShow.ScrollToVerticalOffset(SinglyLinkedList.Vm.AddMyPoint.Y - 20 * 1.7); //20是一个顶点的高度
                    //局部放大
                    DoubleAnimation sliderAnimation = new DoubleAnimation(1, 3, new Duration(TimeSpan.FromSeconds(0.8)),
                        FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);
                    _shrinkTimer.Start();
                }
                AssignValueTo_tb_remove_value(1);
                AssignValueTo_tb_remove_value(2);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnSearch.Content.Equals("Search"))
                {
                    _list.Search(TxtSearchVal.Text.Trim(), GraphLayout);
                    _tickTo=_list.Locate(TxtSearchVal.Text.Trim());
                    _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                    _timer.Tick += timer_Tick;
                    _timer.Start();
                }
                else
                {
                    _list.RefreshCanvas();
                    BtnSearch.Content = "Search";
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }

        }

       

        private void tb_back_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && TbBack.Text.Trim() != "")
            {
                try
                {
                    _list.BackTo(TbBack.Text.Trim());

                    //重新绘制日志
                    ReDrawLogs();
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }
            }
        }

        private void nextstep_Click(object sender, RoutedEventArgs e)
        {
            if (Nextstep.Content.Equals("完成"))
            {
                //操作完成标识归零
                _stateOfInsertOrRemoveOrDefault = 0;
                //滚动条移动
                SvGraphvizShow.ScrollToTop();
                //局部缩小
                DoubleAnimation sliderAnimation = new DoubleAnimation(2.5, 1, new Duration(TimeSpan.FromSeconds(0.8)), FillBehavior.HoldEnd);
                UiScaleSlider.BeginAnimation(RangeBase.ValueProperty, sliderAnimation);
                Nextstep.Content = "下一步";
            }
            else
            {
                if (_stateOfInsertOrRemoveOrDefault == 0)
                    return;
                _list.GraphLayout = GraphLayout;
                int step = _stateOfInsertOrRemoveOrDefault == 1 ? _list.InsertListNodeStepByStep() : _list.WitchListNodeRemove(true);
                if (step == 0)
                {
                    Nextstep.Content = "完成";
                    DoubleAnimation sliderAnimation = new DoubleAnimation(3, 2.5, new Duration(TimeSpan.FromSeconds(0.8)), FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);
                }
            }
        }
        #endregion 事件处理函数


        #region 界面绘制函数
        private void DrawLogs(int row)
        {
            //单元格绘制
            RowDefinition newRows = new RowDefinition {Height = new GridLength(26)};
            LogGrid.RowDefinitions.Add(newRows);
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                //列属性
                ColumnDefinition newCol = new ColumnDefinition { Width = new GridLength() };
                //像素
                LogGrid.ColumnDefinitions.Add(newCol);
            }

            LogVo tlogVo = (LogVo)_list.ListLogs[row + ""];
            for (int iCol = 0; iCol < _titles.Length; iCol++)
            {
                if (iCol == 0)
                {
                    Border border = new Border
                    {
                        BorderThickness = new Thickness(1, 0, 1, 1),
                        //BorderBrush = Brushes.SlateGray
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
                            Label label = new Label { Content = tlogVo.SelectId + "" };
                            label.SetValue(Grid.ColumnProperty, iCol);
                            label.SetValue(Grid.RowProperty, row);
                            LogGrid.Children.Add(label);
                        }
                        break;
                    case 3:
                        {
                            Label label = new Label();
                            String logcontent = tlogVo.Data;
                            label.Content = logcontent;
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
            SvLogs.ScrollToHorizontalOffset(0);
        }

        private void ReDrawLogs()
        {
            LogGrid.Children.Clear();
            DrawTheTitle();
            for (int i = 1; i <= _list.ListLogs.Count; i++)
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
                Label label = new Label {Content = _titles[iCol]};
                label.SetValue(Grid.ColumnProperty, iCol);
                label.SetValue(Grid.RowProperty, row);
                LogGrid.Children.Add(label);
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

        private void tb_remove_location_GotFocus(object sender, RoutedEventArgs e)
        {
            RbRemoveIndex.IsChecked = true;
        }

        private void tb_remove_value_GotFocus(object sender, RoutedEventArgs e)
        {
            RbRemoveVal.IsChecked = true;
        }

        #endregion 界面绘制函数

        /// <summary>
        /// slider控件缩小时钟事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ShrinkTick(object sender, EventArgs e)
        {
            switch (_shrinkTime)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    DoubleAnimation sliderAnimation = new DoubleAnimation(3, 2.5, new Duration(TimeSpan.FromSeconds(0.8)), FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);
                    break;
                case 3:
                    //滚动条移动
                    SvGraphvizShow.ScrollToTop();
                    //局部缩小
                    sliderAnimation = new DoubleAnimation(2.5, 1, new Duration(TimeSpan.FromSeconds(0.8)), FillBehavior.HoldEnd);
                    UiScaleSlider.BeginAnimation(Slider.ValueProperty, sliderAnimation);

                    //关闭计时器并初始化计时标识
                    _shrinkTimer.Stop();
                    _shrinkTime = -1;
                    break;
            }
            _shrinkTime++;
        }

        /// <summary>
        /// Reverts the scaling factor to 1. 
        /// </summary>
        /// <param name="sender">The Slider object which generated the event</param>
        /// <param name="args"></param>
        void RestoreScalingFactor(object sender, MouseButtonEventArgs args)
        {
            ((Slider)sender).Value = 0.2;
        }

        /// <summary>
        /// The user can scale up/down the UI by using the mouse wheel while holding down
        /// the Ctrl key.
        /// </summary>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs args)
        {
            base.OnPreviewMouseWheel(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                UiScaleSlider.Value += (args.Delta > 0) ? 0.1 : -0.1;
            }
        }

        /// <summary>
        /// Reverts the scaling factor to 1, when the user presses the mouse wheel while 
        /// holding down the Ctrl key.
        /// </summary>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseDown(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (args.MiddleButton == MouseButtonState.Pressed)
                {
                    RestoreScalingFactor(UiScaleSlider, args);
                }
            }
        }

        /// <summary>
        /// 给tb_remove_value赋值
        /// </summary>
        /// <param name="whichflag">初始化(0)、插入(1)或删除(2)标记</param>
        private void AssignValueTo_tb_remove_value(int whichflag)
        {
            switch (whichflag)
            {
                case 0://初始化
                    if (!_list.IsEmpty())
                    {
                        if (_list.GetItem(1) != "")
                        {
                            TbRemoveValue.Text = _list.GetItem(1);
                            TbRemoveLocation.Text = 1 + "";
                        }
                        else
                        {
                            if (_list.IsEmpty())
                            {
                                TbRemoveValue.Text = "";
                                TbRemoveLocation.Text = "";
                            }
                            else//length==1
                            {
                                TbRemoveValue.Text = _list.GetItem(0);
                                TbRemoveLocation.Text = 0 + "";
                            }
                        }
                    }
                    break;
                case 1://插入
                    if (_list.IsEmpty() && TbLocins.Text.Trim().Equals("0"))
                    {//length=0
                        TbRemoveValue.Text = TxtInsVal.Text.Trim();
                        TbRemoveLocation.Text = 0 + "";
                    }
                    else if (_list.GetItem(1) != "")
                    {//length>1
                        if (TbLocins.Text.Trim().Equals(1 + ""))
                        {//插入到位置1
                            TbRemoveValue.Text = TxtInsVal.Text.Trim();
                            TbRemoveLocation.Text = 1 + "";
                        }
                        else if (TbLocins.Text.Trim().Equals(0 + ""))
                        {//插入到位置0
                            TbRemoveValue.Text = _list.GetItem(0);
                            TbRemoveLocation.Text = 1 + "";
                        }
                        else
                        {//插入位置>1
                            TbRemoveValue.Text = _list.GetItem(1);
                            TbRemoveLocation.Text = 1 + "";
                        }
                    }
                    else
                    {//length==1
                        if (TbLocins.Text.Trim().Equals(1 + ""))
                        {//插入到位置1
                            TbRemoveValue.Text = TxtInsVal.Text.Trim();
                            TbRemoveLocation.Text = 1 + "";
                        }
                        else
                        {//插入到位置0
                            TbRemoveValue.Text = _list.GetItem(0);
                            TbRemoveLocation.Text = 1 + "";
                        }
                    }

                    break;
                case 2://删除
                    if (_list.IsEmpty())
                        break;
                    if (_list.Length() == 1)
                    {//原来length==1删除后length==0
                        TbRemoveValue.Text = "";
                        TbRemoveLocation.Text = "";
                    }
                    else if (_list.Length() == 2)
                    {//原来length==2删除后length==1
                        if (TbRemoveLocation.Text.Trim().Equals(0 + ""))
                        {//删除位置0
                            TbRemoveValue.Text = _list.GetItem(1);
                            TbRemoveLocation.Text = 0 + "";
                        }
                        else
                        {//删除位置1
                            TbRemoveValue.Text = _list.GetItem(0);
                            TbRemoveLocation.Text = 0 + "";
                        }
                    }
                    else
                    {//删除后length>1
                        if (TbRemoveLocation.Text.Trim().Equals(1 + ""))
                        {//删除位置1
                            TbRemoveValue.Text = _list.GetItem(2);
                            TbRemoveLocation.Text = 1 + "";
                        }
                        else if (TbRemoveLocation.Text.Trim().Equals(0 + ""))
                        {//删除位置0
                            TbRemoveValue.Text = _list.GetItem(2);
                            TbRemoveLocation.Text = 1 + "";
                        }
                        else
                        {//删除位置>1
                            TbRemoveValue.Text = _list.GetItem(1);
                            TbRemoveLocation.Text = 1 + "";
                        }
                    }
                    break;
            }
        }

#region 内部工具函数

        
        private void timer_Tick(object sender, EventArgs e)
        {

            if (_tick >= _tickTo-1)
            {
                BtnSearch.Content = "搜索完成";
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
