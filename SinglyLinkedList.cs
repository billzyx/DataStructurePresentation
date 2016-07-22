using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Graphviz4Net.Graphs;
using Graphviz4Net.WPF;
using integrateOfDataStructure.Utility;
using Arrow = integrateOfDataStructure.Utility.Arrow;

namespace integrateOfDataStructure
{
    public class ListNode
    {
        private string _data;//数据域
        private ListNode _next;//引用域
        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                SinglyLinkedList.Vm.SetId(_data, value);
            }
        }

        /// <summary>
        /// 构造器，数据值为输入数据值
        /// </summary>
        /// <param name="val"></param>
        public ListNode(string val)
        {
            Data = val;
            _next = null;
        }

        /// <summary>
        /// 构造器，数据值为系统默认值
        /// </summary>
        public ListNode()
        {
            _data = default(string);
            _next = null;
        }
        /// <summary>
        /// 数据域属性
        /// </summary>
        public string Data
        {
            get { return _data; }
            set
            {
                _data = value;
                if (SinglyLinkedList.Re == false)
                {
                    SinglyLinkedList.Vm.ViewModelAddNode(value, true);
                }
            }
        }

        /// <summary>
        /// 引用域属性
        /// </summary>
        public ListNode Next
        {
            get { return _next; }
            set
            {
                if (SinglyLinkedList.Re == false)
                {
                    if (_next != null)
                    {
                        SinglyLinkedList.Vm.ViewModelRemoveEdge(_data, _next._data, true);
                    }
                    _next = value;
                    if (value != null)
                    {
                        SinglyLinkedList.Vm.ViewModelAddEdge(Data, value._data, true);
                    }
                }
                else
                {
                    _next = value;
                }
            }
        }
    }

    public class SinglyLinkedList
    {
        public static ListViewModel Vm = new ListViewModel();
        public static bool Re;//锁住Graphviz
        /// <summary>
        /// 单链表的头节点
        /// </summary>
        public ListNode Head { get; set; }
        public int LogId;
        public Hashtable ListLogs = new Hashtable();

        /// <summary>
        /// 构造器，构造具有空指针的头节点并初始化各种计时器
        /// </summary>
        public SinglyLinkedList(GraphLayout graphLayout)
        {
            Head = null;
            GraphLayout = graphLayout;
            _addTimer.Tick += AddTick;
            _addTimer.Interval = new TimeSpan(0, 0, 1);

            _insertTimer.Tick += InsertTick;
            _insertTimer.Interval = new TimeSpan(0, 0, 1);

            _removeTimer.Tick += RemoveIntermediateTick;
            _removeTimer.Interval = new TimeSpan(0, 0, 1);

            _removeHeadTimer.Tick += RemoveHeadTick;
            _removeHeadTimer.Interval = new TimeSpan(0, 0, 1);

            _removeTailTimer.Tick += RemoveTailTick;
            _removeTailTimer.Interval = new TimeSpan(0, 0, 1);
        }
        readonly DispatcherTimer _addTimer = new DispatcherTimer();
        readonly DispatcherTimer _insertTimer = new DispatcherTimer();
        readonly DispatcherTimer _removeTimer = new DispatcherTimer();
        readonly DispatcherTimer _removeHeadTimer = new DispatcherTimer();
        readonly DispatcherTimer _removeTailTimer = new DispatcherTimer();
        private DispatcherTimer _multithreadCoverTimer;//用于完成多线程覆盖操作

        string _value = "1";
        /// <summary>
        /// 要操作节点的位置
        /// </summary>
        int _pos;
        /// <summary>
        /// 动画演示操作中用到的计时器
        /// </summary>
        int _time;
        bool _returnbool;
        ListNode _newNode;
        ListNode _currNode;
        ListNode _preNode;
        /// <summary>
        /// 随机初始化的所有节点的值
        /// </summary>
        private string _initializedValues = "";

        public GraphLayout GraphLayout { get; set; }

        #region 单链表的基本操作
        /// <summary>
        /// 获取一个搜索遍历结果
        /// </summary>
        /// <param name="value">要搜索的节点</param>
        /// <returns>搜索遍历的字符串</returns>
        /// GetPreOrderString
        public List<List<string>> GetSearchTraverseString(string value)
        {
            ListNode currNode = Head;
            List<List<string>> traversalString = new List<List<string>>();
            while (currNode != null && !currNode.Data.Equals(value))
            {
                var tList = new List<string> { currNode.Data, currNode.Id.ToString() };
                traversalString.Add(tList);
                currNode = currNode.Next;
            }
            if (currNode != null && currNode.Data.Equals(value))
            {
                var tList = new List<string> { currNode.Data, currNode.Id.ToString() };
                traversalString.Add(tList);
            }
            return traversalString;
        }
        #endregion 单链表的基本操作

        #region 单链表的内部操作函数
        private List<string> GetTranversalEdges(List<List<string>> list)
        {
            List<string> edges = new List<string>();
            for (int i = 0; i < list.Count - 1; i++)
            {
                var edge = list[i][0] + " " + list[i + 1][0];
                edges.Add(edge);
            }
            return edges;
        }

        private void RefreshListNodesPos()
        {
            ListNode currNode = Head;
            for (int i = 0; currNode != null; i++)
            {
                currNode.Id = i;
                currNode = currNode.Next;
            }
        }

        private void StartAdd(string value)
        {
            _value = value;
            _addTimer.Start();
        }

        private void StartInsert(int pos, string item)
        {
            _pos = pos;
            _value = item;
            _insertTimer.Start();
        }

        private void StartMyInsert(int pos, string item, GraphLayout graphLayout, bool isSingleStep)
        {
            _pos = pos;
            _value = item;
            GraphLayout = graphLayout;
            if (isSingleStep)//单步插入
                InsertListNodeStepByStep();
            else
                _insertTimer.Start();
        }

        /// <summary>
        /// 启动删除计时器
        /// </summary>
        /// <param name="item">删除的值</param>
        /// <param name="pos">删除位置</param>
        /// <param name="graphLayout">graphviz对象</param>
        /// <param name="isSingleStep">是否单步演示</param>
        private void StartRemove(string item, int pos, GraphLayout graphLayout, bool isSingleStep)
        {
            _pos = pos;
            _value = item;
            GraphLayout = graphLayout;
            WitchListNodeRemove(isSingleStep);
        }

        /// <summary>
        /// 选择具体要删除的事件
        /// </summary>
        /// <param name="isSingleStep">是否单步演示</param>
        public int WitchListNodeRemove(bool isSingleStep)
        {
            int tStep = -1;//tStep==-1代表选择了动画演示
            if (_pos > 0)
                Vm.AddMyPoint = new Point(Vm.ViewModelGetNode(_pos - 1, GraphLayout).X, GraphLayout.Canvas.Height - Vm.ViewModelGetNode(_pos - 1, GraphLayout).Y + 30);
            else
                Vm.AddMyPoint = new Point(Vm.ViewModelGetNode(_pos, GraphLayout).X, GraphLayout.Canvas.Height - Vm.ViewModelGetNode(_pos, GraphLayout).Y + 30);
            if (isSingleStep.Equals(true))
            {
                //单步删除
                if (_pos == 0)
                {
                    //首节点
                    tStep = RemoveHeadListNodeStepByStep();
                }
                else if (_pos == Length() - 1)
                {
                    //尾节点
                    tStep = RemoveTailListNodeStepByStep();
                }
                else
                {
                    //中间节点
                    tStep = RemoveIntermediateListNodeStepByStep();
                }
            }
            else
            {
                //动画删除
                if (_pos == 0)
                {
                    //首节点
                    _removeHeadTimer.Start();
                }
                else if (_pos == Length() - 1)
                {
                    //尾节点
                    _removeTailTimer.Start();
                }
                else
                {
                    //中间节点
                    _removeTimer.Start();
                }
            }
            return tStep;
        }
        #endregion 单链表的内部操作函数

        #region 插入/删除计时器
        //多线程遮盖计时器
        void _multithreadCoverTimer_Tick(object sender, EventArgs e)
        {
            if (GraphLayout.GetLayoutDirector().GetDotGraph() != null)
            {
                Point start = Vm.GetVertexPosition(_currNode.Data, GraphLayout);
                Point end = Vm.GetVertexPosition(_newNode.Data, GraphLayout);
                Vm.AddMyLine(start, end, Colors.White, 19, GraphLayout);
                _multithreadCoverTimer.Stop();
            }
        }
        //特殊的遮盖计时器——插入首节点
        void _multithreadHeadCoverTimer_Tick(object sender, EventArgs e)
        {
            if (GraphLayout.GetLayoutDirector().GetDotGraph() != null)
            {
                Point start = Vm.GetVertexPosition(_newNode.Data, GraphLayout);
                Point end = Vm.GetVertexPosition(_newNode.Next.Data, GraphLayout);
                Vm.AddMyLine(start, end, Colors.White, 19, GraphLayout);
                _multithreadCoverTimer.Stop();
            }
        }

        /// <summary>
        /// 以动画形式往链表末尾追加节点的计时器事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void AddTick(object sender, EventArgs e)
        {
            switch (_time)
            {
                case 0:
                    {
                        _newNode = new ListNode(_value);
                        if (Head == null)
                        {
                            Head = _newNode;
                            _time = 0;
                            _addTimer.Stop();
                            Vm.RaiseChanged();
                            return;
                        }
                        _currNode = Head;
                        while (_currNode.Next != null)
                        {
                            _currNode = _currNode.Next;
                        }
                        _currNode.Next = _newNode;

                        RefreshListNodesPos();
                        //defailt操作：
                        Vm.RaiseChanged();

                        //实现轮询遮盖
                        _multithreadCoverTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                        _multithreadCoverTimer.Tick += _multithreadCoverTimer_Tick;
                        _multithreadCoverTimer.Start();
                    } break;
                case 1:
                    {
                        //defailt操作：
                        _time = -1; _addTimer.Stop();
                        Vm.RaiseChanged();
                    } break;
            }
            _time++;
        }

        private int _step;
        /// <summary>
        /// 以单步形式插入节点事件
        /// </summary>
        /// <returns>步子——step</returns>
        public int InsertListNodeStepByStep()
        {
            switch (_step)
            {
                case 0:
                    {
                        if (IsEmpty() || _pos < 0)
                        {
                            Debug.WriteLine("The list is empty");
                            _step = -1;
                        }
                        else if (_pos == 0)//特殊情况：插入首节点step0
                        {
                            _newNode = new ListNode(_value);
                            Vm.RaiseChanged();
                        }
                        else
                        {
                            Vm.ViewModelAddMyNode(_value, _pos, GraphLayout);
                        }
                    } break;
                case 1:
                    {
                        if (_pos == 0)//特殊情况：插入首节点step1
                        {
                            _newNode.Next = Head;
                            Head = _newNode;
                            RefreshListNodesPos();
                            Vm.RaiseChanged();
                            _step = -1;
                        }
                        else
                        {
                            Vm.ViewModelAddMyEdgeTo(_pos, GraphLayout);
                        }
                    } break;
                case 2:
                    {
                        Vm.ViewModelAddMyX(_pos, GraphLayout);
                        Vm.ViewModelAddMyEdgeFrom(_pos, GraphLayout);
                    } break;
                case 3:
                    {
                        _newNode = new ListNode(_value);
                        if (_pos == 1)
                        {
                            _currNode = Head.Next;
                            Head.Next = _newNode;
                            _newNode.Next = _currNode;
                        }
                        else
                        {
                            _currNode = Head.Next;
                            _preNode = Head;
                            int index = 1;
                            while (_currNode.Next != null && index < _pos)
                            {
                                _preNode = _currNode;
                                _currNode = _currNode.Next;
                                ++index;
                            }

                            if (index == _pos)
                            {
                                _newNode.Next = _currNode;
                                _returnbool = true;
                            }
                            else if (index + 1 == _pos)
                            {
                                _currNode.Next = _newNode;
                                _returnbool = true;
                                _step = -1;
                            }
                            _preNode.Next = _newNode;
                        }
                        _step = -1;
                        RefreshListNodesPos();
                        Vm.RaiseChanged();
                    } break;
            }
            _step++;
            return _step;
        }

        /// <summary>
        /// 以动画形式插入节点事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        public void InsertTick(object sender, EventArgs e)
        {
            switch (_time)
            {
                case 0:
                    {
                        if (IsEmpty() || _pos < 0)
                        {
                            Debug.WriteLine("The list is empty");
                            _time = -1;
                        }
                        else if (_pos == 0)//特殊情况：插入首节点time0
                        {
                            _newNode = new ListNode(_value);
                            _newNode.Next = Head;
                            Head = _newNode;
                            //实现轮询遮盖
                            _multithreadCoverTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                            _multithreadCoverTimer.Tick += _multithreadHeadCoverTimer_Tick;
                            _multithreadCoverTimer.Start();
                            Vm.RaiseChanged();
                        }
                        else
                        {
                            Vm.ViewModelAddMyNode(_value, _pos, GraphLayout);
                        }
                    } break;

                case 1:
                    {
                        if (_pos == 0)//特殊情况：插入首节点time1
                        {
                            RefreshListNodesPos();
                            Vm.RaiseChanged();
                            _time = -1;
                            _insertTimer.Stop();
                        }
                        else
                        {
                            Vm.ViewModelAddMyEdgeTo(_pos, GraphLayout);
                        }
                    } break;
                case 2:
                    {
                        Vm.ViewModelAddMyX(_pos, GraphLayout);
                        Vm.ViewModelAddMyEdgeFrom(_pos, GraphLayout);
                    } break;

                case 3:
                    {
                        _newNode = new ListNode(_value);
                        if (_pos == 1)
                        {
                            _currNode = Head.Next;
                            Head.Next = _newNode;
                            _newNode.Next = _currNode;
                        }
                        else
                        {
                            _currNode = Head.Next;
                            _preNode = Head;
                            int index = 1;
                            while (_currNode.Next != null && index < _pos)
                            {
                                _preNode = _currNode;
                                _currNode = _currNode.Next;
                                ++index;
                            }
                            if (index == _pos)
                            {
                                _newNode.Next = _currNode;
                                _returnbool = true;
                            }
                            else if (index + 1 == _pos)
                            {
                                _currNode.Next = _newNode;
                                _returnbool = true;
                                _time = -1;
                            }
                            _preNode.Next = _newNode;
                        }
                        //直接退出计时器
                        _time = -1;
                        _insertTimer.Stop();
                        RefreshListNodesPos();
                        Vm.RaiseChanged();
                    } break;
            }
            _time++;
        }
        #region 节点删除计时器事件

        Point _tRemovePoint1;

        /// <summary>
        /// 以动画形式删除节点计时器事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void RemoveIntermediateTick(object sender, EventArgs e)
        {
            switch (_time)
            {
                case 0:
                    {
                        Vm.ViewModelAddMyCurvedArrow(_pos - 1, _pos + 1, GraphLayout, Brushes.Black, 1);
                        Vm.ViewModelAddMyX(_pos, GraphLayout);
                    } break;
                case 1:
                    {

                        //使用遮盖方式实现演示
                        double width = Vm.ViewModelGetWidthOfNode(_pos, GraphLayout) * 1.2;
                        double height = (Vm.ViewModelGetNode(_pos - 1, GraphLayout).Y - Vm.ViewModelGetNode(_pos + 1, GraphLayout).Y - Vm.ViewModelGetHeightOfNode(_pos, GraphLayout));
                        Point tPoint = new Point(Vm.ViewModelGetNode(_pos, GraphLayout).X + Vm.ViewModelGetWidthOfNode(_pos, GraphLayout) / 2, Vm.ViewModelGetNode(_pos, GraphLayout).Y - Vm.ViewModelGetHeightOfNode(_pos, GraphLayout) / 2);
                        Vm.ViewModelAddMyRectangle(tPoint, width, height * 3 / 4, GraphLayout);
                        Vm.ViewModelAddMyCurvedArrow(_pos - 1, _pos + 1, GraphLayout, Brushes.White, 1.6);
                        Vm.ViewModelAddMyLine(new Point(tPoint.X, tPoint.Y - height / 2), new Point(tPoint.X, tPoint.Y + height / 2), GraphLayout);
                        _tRemovePoint1 = Vm.ViewModelAddMyLeftNode(_value, tPoint, GraphLayout);
                        Vm.ViewModelAddMyLeftEdgeTo(_tRemovePoint1, Vm.ViewModelGetNode(_pos + 1, GraphLayout), GraphLayout);
                    } break;
                case 2:
                    {
                        Point tPoint = new Point((_tRemovePoint1.X + Vm.ViewModelGetNode(_pos + 1, GraphLayout).X) / 2, (_tRemovePoint1.Y + Vm.ViewModelGetNode(_pos + 1, GraphLayout).Y) / 2);
                        Vm.ViewModelAddMyX(tPoint, GraphLayout);
                    } break;
                case 3:
                    {
                        _preNode = Head;
                        _currNode = Head.Next;
                        while (_currNode.Next != null && !_value.Equals(_currNode.Data))
                        {
                            _preNode = _currNode;
                            _currNode = _currNode.Next;
                        }
                        if (_value.Equals(_currNode.Data))
                        {
                            _preNode.Next = _currNode.Next;
                        }
                        else
                        {
                            Debug.WriteLine("The position is out of range");
                        }
                        Vm.ViewModelRemoveNode(_value, true);
                        RefreshListNodesPos();
                        //Vm.RaiseChanged();

                        //this.RefreshListNodesPos();
                        Vm.RaiseChanged();
                        //default操作
                        _tRemovePoint1 = new Point();
                        _time = -1; _removeTimer.Stop();
                    } break;
            }
            _time++;
        }

        /// <summary>
        /// 以动画形式删除首节点计时器事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void RemoveHeadTick(object sender, EventArgs e)
        {
            switch (_time)
            {
                case 0:
                    {
                        Vm.ViewModelAddMyX(_pos + 1, GraphLayout);
                    } break;
                case 1:
                    {
                        Head = Head.Next;
                        //_returnint = 0;
                        Vm.ViewModelRemoveNode(_value, true);
                        RefreshListNodesPos();
                        Vm.RaiseChanged();
                        _time = -1; _removeHeadTimer.Stop();
                    } break;
            }
            _time++;
        }

        /// <summary>
        /// 以动画形式删除尾节点计时器事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void RemoveTailTick(object sender, EventArgs e)
        {
            switch (_time)
            {
                case 0:
                    {
                        Vm.ViewModelAddMyX(_pos, GraphLayout);
                    } break;
                case 1:
                    {
                        _preNode = Head;
                        _currNode = Head.Next;
                        while (_currNode.Next != null && !_value.Equals(_currNode.Data))
                        {
                            _preNode = _currNode;
                            _currNode = _currNode.Next;
                        }
                        if (_value.Equals(_currNode.Data))
                        {
                            _preNode.Next = _currNode.Next;
                        }
                        else
                        {
                            Debug.WriteLine("The position is out of range");
                        }
                        Vm.ViewModelRemoveNode(_value, true);
                        RefreshListNodesPos();
                        Vm.RaiseChanged();

                        //default操作
                        _time = -1; _removeTailTimer.Stop();
                    } break;
            }
            _time++;
        }
        #endregion

        #region 单步删除节点事件
        /// <summary>
        /// 单步删除除首节点和尾节点意外的节点事件
        /// </summary>
        /// <returns>删除步子</returns>
        public int RemoveIntermediateListNodeStepByStep()
        {
            switch (_step)
            {
                case 0:
                    {
                        Vm.ViewModelAddMyCurvedArrow(_pos - 1, _pos + 1, GraphLayout, Brushes.Black, 1);
                        Vm.ViewModelAddMyX(_pos, GraphLayout);
                    } break;
                case 1:
                    {
                        //使用遮盖方式实现演示
                        double width = Vm.ViewModelGetWidthOfNode(_pos, GraphLayout) * 1.2;
                        double height = (Vm.ViewModelGetNode(_pos - 1, GraphLayout).Y - Vm.ViewModelGetNode(_pos + 1, GraphLayout).Y - Vm.ViewModelGetHeightOfNode(_pos, GraphLayout));
                        Point tPoint = new Point(Vm.ViewModelGetNode(_pos, GraphLayout).X + Vm.ViewModelGetWidthOfNode(_pos, GraphLayout) / 2, Vm.ViewModelGetNode(_pos, GraphLayout).Y - Vm.ViewModelGetHeightOfNode(_pos, GraphLayout) / 2);
                        Vm.ViewModelAddMyRectangle(tPoint, width, height * 3 / 4, GraphLayout);
                        Vm.ViewModelAddMyCurvedArrow(_pos - 1, _pos + 1, GraphLayout, Brushes.White, 1.6);
                        Vm.ViewModelAddMyLine(new Point(tPoint.X, tPoint.Y - height / 2), new Point(tPoint.X, tPoint.Y + height / 2), GraphLayout);

                        _tRemovePoint1 = Vm.ViewModelAddMyLeftNode(_value, tPoint, GraphLayout);
                        Vm.ViewModelAddMyLeftEdgeTo(_tRemovePoint1, Vm.ViewModelGetNode(_pos + 1, GraphLayout), GraphLayout);


                    } break;
                case 2:
                    {
                        Point tPoint = new Point((_tRemovePoint1.X + Vm.ViewModelGetNode(_pos + 1, GraphLayout).X) / 2, (_tRemovePoint1.Y + Vm.ViewModelGetNode(_pos + 1, GraphLayout).Y) / 2);
                        Vm.ViewModelAddMyX(tPoint, GraphLayout);
                    } break;
                case 3:
                    {
                        _preNode = Head;
                        _currNode = Head.Next;
                        while (_currNode.Next != null && !_value.Equals(_currNode.Data))
                        {
                            _preNode = _currNode;
                            _currNode = _currNode.Next;
                        }
                        if (_value.Equals(_currNode.Data))
                        {
                            _preNode.Next = _currNode.Next;
                        }
                        else
                        {
                            Debug.WriteLine("The position is out of range");
                        }
                        Vm.ViewModelRemoveNode(_value, true);
                        RefreshListNodesPos();
                        Vm.RaiseChanged();
                        //default操作
                        _tRemovePoint1 = new Point();
                        _step = -1;
                    } break;
            }
            _step++;
            return _step;
        }

        /// <summary>
        /// 单步删除首节点
        /// </summary>
        /// <returns>删除步子</returns>
        public int RemoveHeadListNodeStepByStep()
        {
            switch (_step)
            {
                case 0:
                    {
                        Vm.ViewModelAddMyX(_pos + 1, GraphLayout);
                    } break;
                case 1:
                    {
                        Head = Head.Next;
                        Vm.ViewModelRemoveNode(_value, true);
                        RefreshListNodesPos();
                        Vm.RaiseChanged();
                        _step = -1; _removeHeadTimer.Stop();
                    } break;
            }
            _step++;
            return _step;
        }

        /// <summary>
        /// 单步删除尾节点
        /// </summary>
        /// <returns>删除步子</returns>
        public int RemoveTailListNodeStepByStep()
        {
            switch (_step)
            {
                case 1:
                    {
                        Vm.ViewModelAddMyX(_pos, GraphLayout);
                    } break;
                case 2:
                    {
                        _preNode = Head;
                        _currNode = Head.Next;
                        while (_currNode.Next != null && !_value.Equals(_currNode.Data))
                        {
                            _preNode = _currNode;
                            _currNode = _currNode.Next;
                        }
                        if (_value.Equals(_currNode.Data))
                        {
                            _preNode.Next = _currNode.Next;
                        }
                        else
                        {
                            Debug.WriteLine("The position is out of range");
                        }
                        Vm.ViewModelRemoveNode(_value, true);
                        RefreshListNodesPos();
                        Vm.RaiseChanged();

                        //default操作
                        _step = -1; _removeTailTimer.Stop();
                    } break;
            }
            _step++;
            return _step;
        }
        #endregion 单步删除节点事件
        #endregion 插入/删除计时器

        #region 数据结构对外界所提供的函数
        public void RefreshCanvas()
        {
            Vm.ViewModelAddNode(" ", true);//人工加入一个点，强行触发RaiseChanged事件
            Vm.ViewModelRemoveNode(" ", true);
            Vm.Graph.RaiseChangedByTheCustom();
        }

        /// <summary>
        /// 求单链表长度
        /// </summary>
        /// <returns></returns>
        public int Length()
        {
            ListNode currNode = Head;
            int length = 0;
            while (currNode != null)
            {
                ++length;
                currNode = currNode.Next;
            }
            return length;
        }

        /// <summary>
        /// 清空单链表
        /// </summary>
        public void Clear()
        {
            Vm.Graph.Clear(true);
            Head = null;
        }

        /// <summary>
        /// 判断单链表是否为空
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return (Head == null);
        }
        #endregion 数据结构对外界所提供的函数

        /// <summary>
        /// 在单链表末尾添加新元素
        /// </summary>
        /// <param name="value">节点值</param>
        private int AddTailListNode(string value)
        {
            int index = Length();
            StartAdd(value);
            Save("insert value " + value);
            return index;
        }
        /// <summary>
        /// 回退函数：在单链表末尾添加新元素
        /// </summary>
        /// <param name="value">节点值</param>
        /// <param name="backSimbol">回退标识</param>
        /// <returns>追加到链表末尾</returns>
        private int AddTailListNode(string value, bool backSimbol)
        {
            Save("insert value " + value);
            int index = Length();
            _newNode = new ListNode(value);
            _currNode = null;
            if (Head == null)
            {
                Head = _newNode;
                return 0;
            }
            _currNode = Head;
            while (_currNode.Next != null)
            {
                _currNode = _currNode.Next;
            }
            _currNode.Next = _newNode;
            return index;
        }



        /// <summary>
        /// 在指定位置插入元素
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool Insert(int pos, string item)
        {
            if (IsEmpty() || pos > Length())
            { return false; }
            StartInsert(pos, item);
            Save("insert value " + item + " to index " + pos);
            return true;
        }

        public bool MyInsert(int pos, string item, GraphLayout graphLayout, bool isSingleStep)
        {
            if (IsEmpty() || pos > Length() || pos < 0)
                return false;
            StartMyInsert(pos, item, graphLayout, isSingleStep);
            Save("insert value " + item + " to index " + pos);
            return true;
        }

        public bool Insert(int pos, string item, bool a)
        {
            Save("insert value " + item + " to index " + pos);
            if (IsEmpty() || pos > Length())
            { return false; }
            _returnbool = false;
            if (IsEmpty() || pos < 0)
            {
                Debug.WriteLine("The list is empty");
                _returnbool = false;
                return false;
            }
            _newNode = new ListNode(item);
            if (pos == 0)
            {
                _newNode.Next = Head;
                _returnbool = true;
                Head = _newNode;
                return true;
            }
            _currNode = Head.Next;
            _preNode = Head;
            int index = 1;
            while (_currNode.Next != null && index < pos)
            {
                _preNode = _currNode;
                _currNode = _currNode.Next;
                ++index;
            }
            if (index == pos)
            {
                _newNode.Next = _currNode;
                _returnbool = true;
            }
            else if (index + 1 == pos)
            {
                _currNode.Next = _newNode;
                _returnbool = true;
                return true;
            }
            if (_returnbool)
            {
                _preNode.Next = _newNode;
            }
            else
            {
                Debug.WriteLine("The position is error");
                _returnbool = false;
            }
            return true;
        }

        /// <summary>
        /// 删除指定节点值的元素
        /// </summary>
        /// <param name="item">节点值</param>
        /// <param name="graphLayout">graphviz对象</param>
        /// <param name="isSingleStep"></param>
        /// <returns>所删除节点的节点位置</returns>
        private int DoRemove(string item, GraphLayout graphLayout, bool isSingleStep)
        {
            int l = Locate(item);
            if (l == -1) return l;
            StartRemove(item, l, graphLayout, isSingleStep);
            Save("delete value " + item);
            return l;
        }

        /// <summary>
        /// 删除指定索引位置的元素
        /// </summary>
        /// <param name="pos">节点位置</param>
        /// <param name="graphLayout">Graphviz图层对象</param>
        /// <param name="isSingleStep">是否单步演示</param>
        /// <returns>所删除节点的节点值</returns>
        private string DoRemove(int pos, GraphLayout graphLayout, bool isSingleStep)
        {
            string item = GetItem(pos);
            if (item == "") return item;
            StartRemove(item, pos, graphLayout, isSingleStep);
            Save("delete value " + item);
            return item;
        }

        /// <summary>
        /// 专属于日志删除节点操作
        /// </summary>
        /// <param name="item">节点值</param>
        /// <param name="isFlagOfLogOperation">日志重载操作标志</param>
        /// <returns>删除节点的位置</returns>
        private int DoRemove(string item, bool isFlagOfLogOperation)
        {
            Save("delete value " + item);
            int l = Locate(item);
            if (IsEmpty())
            {
                Debug.WriteLine("The Link is empty");
                return -1;
            }
            if (item.Equals(Head.Data))
            {
                Head = Head.Next;
                Vm.ViewModelRemoveNode(item);
                return 0;
            }
            _preNode = Head;
            _currNode = Head.Next;
            while (_currNode.Next != null && !item.Equals(_currNode.Data))
            {
                _preNode = _currNode;
                _currNode = _currNode.Next;
            }
            if (item.Equals(_currNode.Data))
            {
                _preNode.Next = _currNode.Next;
            }
            else
            {
                Debug.WriteLine("The position is out of range");
            }
            return l;
        }

        public void Redraw()
        {
            Vm.Graph.Clear(true);
            if (IsEmpty())
            {
                Debug.WriteLine("The list is empty");
                return;
            }
            ListNode currNode = Head;
            Vm.ViewModelAddNode(currNode.Data, true);
            while (currNode.Next != null)
            {
                Vm.ViewModelAddNode(currNode.Next.Data, true);
                Vm.ViewModelAddEdge(currNode.Data, currNode.Next.Data, true);
                currNode = currNode.Next;
            }
            RefreshListNodesPos();
            Vm.RaiseChanged();
        }
        /// <summary>
        /// 根据值查找
        /// </summary>
        /// <param name="value"></param>
        /// <returns>如果找不到则返回-1</returns>
        public int Locate(string value)
        {
            if (IsEmpty())
            {
                return -1;
            }
            ListNode currNode = Head;
            int pos = 0;
            while (currNode != null && !currNode.Data.Equals(value))
            {
                currNode = currNode.Next;
                ++pos;
            }
            if (currNode == null)
                return -1;
            return pos;
        }

        /// <summary>
        /// 返回该索引所对应的值
        /// </summary>
        /// <param name="index">索引值</param>
        /// <returns>String</returns>
        public string GetItem(int index)
        {
            if (IsEmpty() || index < 0)
                return "";
            var currNode = Head;
            for (int i = 0; i < index && currNode != null; i++)
            {
                currNode = currNode.Next;
            }
            if (currNode == null)
                return "";
            return currNode.Data;
        }

        readonly List<string> _innerLogList = new List<string>();
        private void Save(string s)
        {
            _innerLogList.Add(s);
        }

        public void Back(int step)
        {
            Clear();
            Re = true;

            int i = 1;
            List<string> q = new List<string>();
            _innerLogList.ForEach(delegate(string name)
            {
                q.Add(name);
            });
            _innerLogList.Clear();
            q.ForEach(delegate(string name)
            {
                if (i <= step)
                {
                    string[] cmd = name.Split(' ');
                    List<string> cmds = new List<string>(cmd);
                    switch (cmds[0])
                    {
                        case "insert":
                            {
                                Regex reg = new Regex(@"^[0-9]*$");
                                if (cmds.Count == 6 && cmds[1].Equals("value") && cmds[2] != null && cmds[3].Equals("to") && cmds[4].Equals("index") && reg.IsMatch(cmds[5]))
                                {
                                    CommandVo cv = new CommandVo
                                    {
                                        Operation = cmds[0],
                                        Data = cmds[2],
                                        TargetId = int.Parse(cmds[5])
                                    };
                                    Insert(cv.TargetId, cv.Data, true);
                                }
                                else if (cmds.Count == 3 && cmds[1].Equals("value") && cmds[2] != null)
                                {
                                    CommandVo cv = new CommandVo
                                    {
                                        Operation = cmds[0],
                                        Data = cmds[2]
                                    };
                                    AddTailListNode(cv.Data, true);
                                }
                            }
                            break;
                        case "delete":
                            {
                                if (cmds.Count == 3 && cmds[1].Equals("value") && cmds[2] != null)
                                {
                                    CommandVo cv = new CommandVo
                                    {
                                        Operation = cmds[0],
                                        Data = cmds[2]
                                    };
                                    DoRemove(cv.Data, true);
                                }
                            }
                            break;
                        case "initialize":
                            {//其实无需验证：因为单链表内用了两个list来存储logs，一个提供给界面。一个是内部用的，由程序员定义
                                if (!this.IsEmpty())
                                    this.Clear();
                                if (cmd[2] != "")
                                {
                                    Re = true;
                                    string[] strs = cmd[2].Split('_');
                                    foreach (string value in strs)
                                    {
                                        if (value != null)
                                        {
                                            ListNode newNode = new ListNode(value);
                                            if (Head == null)
                                            {
                                                Head = newNode;
                                            }
                                            else
                                            {
                                                ListNode currNode = Head;
                                                while (currNode.Next != null)
                                                {
                                                    currNode = currNode.Next;
                                                }
                                                currNode.Next = newNode;
                                            }
                                        }
                                    }
                                    Save("initialize " + cmd[1] + " " + cmd[2]);//重新记录日志
                                }
                            }
                            break;
                    }
                    i++;
                }
            });
            q.Clear();
            Redraw();
            Re = false;
        }

        /// <summary>
        /// 根据制定值随机初始化单链表
        /// </summary>
        /// <param name="createSize"></param>
        /// <returns></returns>
        public void RandomInit(string createSize)
        {
            Regex reg = new Regex(@"^[1-9]\d*$");
            if (!reg.IsMatch(createSize))
                throw new Exception("您的输入有误,请输入大于0的整数！");
            if (!IsEmpty())
            {
                Clear();
            }
            string[] strs = GetRamdomList(int.Parse(createSize)).Split(' ');
            foreach (string value in strs)
            {
                if (value != null)
                {
                    ListNode newNode = new ListNode(value);
                    if (Head == null)
                    {
                        Head = newNode;
                    }
                    else
                    {
                        ListNode currNode = Head;
                        while (currNode.Next != null)
                        {
                            currNode = currNode.Next;
                        }
                        currNode.Next = newNode;
                        RefreshListNodesPos();
                    }
                }
            }
            //日志操作
            LogId++;
            ListLogs.Add(LogId + "", new LogVo(LogId, "初始化(" + createSize + ")", -1, _initializedValues));
            Save("initialize " + createSize + " " + _initializedValues.Replace(' ', '_'));

            Vm.RaiseChanged();
        }
        #region Create 函数的具体实现类
        private static readonly char[] Chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'R', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

        private string GetRamdomList(int createSize)
        {
            string values = "";
            int length = 2;//随机字符数目
            int count = createSize;
            Hashtable ht = new Hashtable(150);
            Random rd = new Random();
            for (int i = 0; i < count; i++)
            {
                string s = "";
                for (int j = 0; j < length; j++)
                {
                    s = s + (Chars[rd.Next(0, Chars.Length)]);
                }
                if (ht.ContainsValue(s)) i--;
                else
                {
                    ht.Add(i.ToString(), s);
                    if (i == count - 1)
                        values += s;
                    else
                    {
                        values += s + " ";
                    }
                }
            }
            _initializedValues = values;
            return values;
        }
        #endregion


        /// <summary>
        /// 插入操作
        /// </summary>
        /// <param name="location">插入节点位置</param>
        /// <param name="value">节点值</param>
        /// <param name="graphLayout">Graphviz对象</param>
        /// <param name="isSingleStep">是否单步演示</param>
        public void InsertTo(string location, string value, GraphLayout graphLayout, bool isSingleStep)
        {
            if (Locate(value) != -1)
            {
                throw new Exception("输入重复！");
            }
            Regex reg = new Regex(@"^-?[0-9]*$");
            if (reg.IsMatch(location) || location.Equals(""))
            {
                if (int.Parse(location) > Length() || int.Parse(location) < 0)
                {
                    throw new Exception("                索引超出范围!\n链表索引范围为从0开始到链表长度减1\n   或者您还可选择追加到链表末尾处");
                }
                int index;
                if (location.Equals("") || int.Parse(location) == Length())
                {
                    index = AddTailListNode(value);
                }
                else
                {
                    index = int.Parse(location);
                    MyInsert(index, value, graphLayout, isSingleStep);
                }
                LogId++;
                ListLogs.Add(LogId + "", new LogVo(LogId, "插入", index, value));
            }
        }

        /// <summary>
        /// 根据节点值删除
        /// </summary>
        /// <param name="value">节点值</param>
        /// <param name="graphLayout">Graphviz对象</param>
        /// <param name="isSingleStep">是否单步演示</param>
        public void Remove(string value, GraphLayout graphLayout, bool isSingleStep)
        {
            int index = DoRemove(value, graphLayout, isSingleStep);
            if (index == -1)
            {
                throw new Exception("不存在该节点！");
            }
            LogId++;
            ListLogs.Add(LogId + "", new LogVo(LogId, "删除", index, value));
        }

        /// <summary>
        /// 根据节点的索引位置删除
        /// </summary>
        /// <param name="index">索引值</param>
        /// <param name="graphLayout"></param>
        /// <param name="isSingleStep"></param>
        public void Remove(int index, GraphLayout graphLayout, bool isSingleStep)
        {
            string value = DoRemove(index, graphLayout, isSingleStep);
            if (value.Equals(""))
            {
                throw new Exception("索引越界,删除失败!");
            }
            LogId++;
            ListLogs.Add(LogId + "", new LogVo(LogId, "删除", index, value));
        }

        public void Search(string value, GraphLayout graphLayout)
        {
            Regex regex = new Regex(@"^[a-zA-Z]*$");
            if (!regex.IsMatch(value))
            {
                throw new Exception("您的输入有误，请输入当前链表中已存在的节点值！");
            }
            if (Locate(value) == -1)
            {
                throw new Exception("当前链表中不存在您要的节点");
            }
            List<List<string>> traversalString = GetSearchTraverseString(value);
            List<string> edges = GetTranversalEdges(traversalString);
            Vm.ViewModelSearch(edges, traversalString, "单链表", graphLayout);
        }

        /// <summary>
        /// 回退操作
        /// </summary>
        /// <param name="sstep"></param>
        public void BackTo(string sstep)
        {
            if (LogId < 1)
                throw new Exception("已无法再向前回退了,请注意您的输入！");
            if (LogId == 1)
            {
                throw new Exception("已经回退至第1步！");
            }
            int step = int.Parse(sstep);
            if (step > LogId && step < 1)
                throw new Exception("所选步数超出范围");
            Back(step);
            //日志操作
            Hashtable temp = new Hashtable();
            for (int i = 1; i <= step; i++)
            {
                temp.Add(i + "", (LogVo)ListLogs[i + ""]);
            }
            ListLogs = temp;
            LogId = step;
        }

        #region 答辩演示代码：
        public void insert(string value, int pos)
        {
            ListNode p = Head;
            ListNode node = new ListNode(value);
            int i = 0;
            if (pos == 0)//插入表头
            {
                node.Next = p;
            }
            while (i < pos - 1)//找到要插入的前置节点
            {
                p = p.Next;
                i++;
            }
            if (pos == Length() - 1)//插入表尾
            {
                node.Next = null;
                p.Next = node;
            }
            node.Next = p.Next;
            p.Next = node;
        }
        #endregion 答辩演示代码
    }


    public class ListViewModel : INotifyPropertyChanged
    {
        public Graph<ViewModelNode> Graph { get; set; }

        public ListViewModel()
        {
            Graph<ViewModelNode> graph = new Graph<ViewModelNode>();
            Graph = graph;
            Graph.Changed += GraphChanged;
        }

        public Point AddMyPoint;

        public void ViewModelAddNode(string name)
        {
            var p = new ViewModelNode(Graph) { Name = name };
            Graph.AddVertex(p);
        }

        public void ViewModelAddNode(string name, bool withoutRaiseChanged)
        {
            var p = new ViewModelNode(Graph) { Name = name };
            Graph.AddVertex(p, true);
        }

        public void ViewModelAddMyNode(string name, int pos, GraphLayout glLayout)
        {
            AddMyPoint = glLayout.AddMyNodeInList(name, pos);
        }

        public Point ViewModelAddMyLeftNode(string name, Point point, GraphLayout glLayout)
        {
            AddMyPoint = glLayout.AddMyLeftNode(name, point);
            return AddMyPoint;
        }

        public Point ViewModelGetNode(int pos, GraphLayout glLayout)
        {
            return glLayout.GetPointOfNode(pos);
        }

        public Point ViewModelGetMyNode(string name, int pos, GraphLayout glLayout)
        {
            return glLayout.GetPointOfMyNode(name, pos);
        }

        public void ViewModelAddMyRectangle(Point point, double width, double height, GraphLayout glLayout)
        {
            glLayout.AddMyWhiteRectangle(point, width, height);
        }

        public void ViewModelAddMyLine(Point pStart, Point pEnd, GraphLayout glLayout)
        {
            glLayout.AddMyLine(pStart, pEnd);
        }

        public double ViewModelGetWidthOfNode(int pos, GraphLayout glLayout)
        {
            return glLayout.GetWidthOfNode(pos);
        }

        public double ViewModelGetHeightOfNode(int pos, GraphLayout glLayout)
        {
            return glLayout.GetHeightOfNode(pos);
        }

        internal void ViewModelAddMyEdgeFrom(int pos, GraphLayout glLayout)
        {
            glLayout.AddMyArrowFrom(pos);
        }

        internal void ViewModelAddMyEdgeTo(int pos, GraphLayout glLayout)
        {
            glLayout.AddMyArrowToInList(pos);
        }

        internal void ViewModelAddMyLeftEdgeTo(Point point1, Point point2, GraphLayout glLayout)
        {
            glLayout.AddMyLeftArrowToInList(point1, point2);
        }

        public void ViewModelAddMyX(int pos, GraphLayout glLayout)
        {
            glLayout.AddMyXInList(pos);
        }

        public void ViewModelAddMyX(Point point, GraphLayout glLayout)
        {
            glLayout.AddMyXInList(point);
        }

        public void ViewModelAddMyX(Point point1, Point point2, GraphLayout glLayout)
        {
            glLayout.AddMyXInList(point1, point2);
        }

        public void ViewModelAddMyCurvedArrow(int pos1, int pos2, GraphLayout glLayout, Brush brush, double strokeThickness)
        {
            glLayout.AddMyCurvedArrow(pos1, pos2, brush, strokeThickness);
        }

        public void RaiseChanged()
        {
            Graph.RaiseChangedByTheCustom();
        }

        public void ViewModelAddEdge(string last, string name)
        {
            Graph.AddEdge(
               new Edge<ViewModelNode>
                   (GetVmVertex(last),
                   GetVmVertex(name),
               new Arrow()));
        }
        //不带提交地画一条边
        public void ViewModelAddEdge(string last, string name, bool withoutRaiseChanged)
        {
            Graph.AddEdge(
               new Edge<ViewModelNode>
                   (GetVmVertex(last),
                   GetVmVertex(name),
               new Arrow()), true);
        }
        public void ViewModelRemoveEdge(string a, string b)
        {
            Graph.RemoveEdge(Graph.FindEdge(GetVmVertex(a), GetVmVertex(b)));
        }
        public void ViewModelRemoveEdge(string a, string b, bool withoutRaiseChanged)
        {
            Graph.RemoveEdge(Graph.FindEdge(GetVmVertex(a), GetVmVertex(b)), true);
        }
        public void ViewModelRemoveNode(string p)
        {
            Graph.RemoveVertexWithEdges(GetVmVertex(p));
        }

        public void ViewModelRemoveNode(string p, bool withoutRaiseChanged)
        {
            Graph.RemoveVertexWithEdges(GetVmVertex(p), true);
        }

        public void ViewModelSearch(List<string> edges, List<List<string>> traversalString, string witchStructure, GraphLayout glLayout)
        {
            glLayout.Search(edges, traversalString, witchStructure);
        }

        private ViewModelNode GetVmVertex(string name)
        {
            return Graph.AllVertices.First(x => string.CompareOrdinal(x.Name, name) == 0);
        }

        public void SetId(string value, int id)
        {
            GetVmVertex(value).Id = id + "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void GraphChanged(object sender, GraphChangedArgs e)
        {
            RaisePropertyChanged("AllNodes");
        }

        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #region 我搬照过来的，为了以后可以共用
        /// <summary>
        /// 获取顶点在Canvas上的位置
        /// </summary>
        /// <param name="data"></param>
        /// <param name="graphLayout"></param>
        /// <returns></returns>
        public Point GetVertexPosition(string data, GraphLayout graphLayout)
        {
            return graphLayout.GetVertexPosition(data);
        }
        /// <summary>
        /// 画一条线
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="color">直线的颜色</param>
        /// <param name="strokeThickness">线的厚度</param>
        /// <param name="glLayout">Graphviz对象</param>
        public void AddMyLine(Point start, Point end, Color color, int strokeThickness, GraphLayout glLayout)
        {
            glLayout.AddMyLine(start, end, color, strokeThickness);
        }
        #endregion 我搬照过来的，为了以后可以共用



    }

}
