using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Graphviz4Net.WPF;
using integrateOfDataStructure.Utility;

namespace integrateOfDataStructure
{
    public class BTreeNode
    {
        private string _data;               // 节点数据
        private BTreeNode _left;            // 左子女
        private BTreeNode _right;           // 右子女

        public static int Sid = -1;
        private int _id = -1;
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                BinaryTree.Vm.SetId(_data, value);
            }
        }

        /// <summary>
        /// Data访问器
        /// </summary>
        public string Data
        {
            get { return _data; }
            set
            {
                _data = value;
                BinaryTree.Vm.AddVertex(value);
            }
        }

        /// <summary>
        ///  左子女访问器
        /// </summary>
        public BTreeNode Left
        {
            get { return _left; }
            set
            {
                _left = value;
                if (value != null)
                    BinaryTree.Vm.AddAdge(Data, value.Data, "L");
            }
        }

        /// <summary>
        /// 右子女访问器
        /// </summary>
        public BTreeNode Right
        {
            get
            {
                return _right;
            }
            set
            {
                _right = value;
                if (value != null)
                    BinaryTree.Vm.AddAdge(Data, value.Data, "R");

            }
        }

        /// <summary>
        /// 父节点访问器
        /// </summary>
        public BTreeNode Parent { get; set; }

        public BTreeNode(string data)
        {
            Data = data;
            Id = ++Sid;
        }


    }

    /// <summary>
    /// 简化版的节点，用来做暂时性的快速存取用的
    /// </summary>
    struct BNodeStruct
    {
        public int Id;
        public string Value;
        public BNodeStruct(int id, string value)
        {
            Id = id;
            Value = value;
        }
    }

    ///简化版节点,用于在回退里的初始化操作中记录随机初始化了怎么样一颗多叉树
    public class InitBTreeNode
    {
        public InitBTreeNode(string data)
        {
            Data = data;
        }
        public string Data;
        public InitBTreeNode Left;
        public InitBTreeNode Right;
    }

    /// <summary>
    /// 定义二叉树类
    /// </summary>
    public class BinaryTree
    {
        public static ViewModel Vm = new ViewModel();
        private GraphLayout _graphLayout;
        public BTreeNode Head { get; set; }

        private string _initializedValues = "";// 随机初始化的所有节点的值
        private readonly List<InitBTreeNode> _initBtreeHeads;//用于记录随机初始化了的多少颗多叉树
        private readonly Hashtable _initNodeHashtable = new Hashtable();

        public int LogId;
        public string WhichCommand;
        public Hashtable BTreeLogs = new Hashtable();
        private readonly Hashtable _bTreeHashTable = new Hashtable();

        //计时器相关：
        private readonly DispatcherTimer _leftInsertTimer = new DispatcherTimer();
        private readonly DispatcherTimer _rightInsertTimer = new DispatcherTimer();
        private readonly DispatcherTimer _removeTimer = new DispatcherTimer();
        private DispatcherTimer _multithreadCoverTimer;//用于完成多线程覆盖操作
        private int _lClock = 1;//插入左孩子计时器
        private int _rClock = 1;//插入右孩子计时器
        private static string _fatherData;//全局变量：当前操作的父节点内容
        private static string _childData;//全局变量：当前操作的孩子内容
        private List<List<string>> _removePostOrderString;//遍历删除的节点顺序

        public BinaryTree()
        {
            _initBtreeHeads = new List<InitBTreeNode>();
            _leftInsertTimer.Tick += LeftInsertTimer_Tick;
            _leftInsertTimer.Interval = new TimeSpan(0, 0, 1);
            _rightInsertTimer.Tick += RightInsertTimer_Tick;
            _rightInsertTimer.Interval = new TimeSpan(0, 0, 1);
            _removeTimer.Tick += RemoveTimer_Tick;
            _removeTimer.Interval = new TimeSpan(0, 0, 1);
        }

        #region 二叉树的基本操作
        private List<BNodeStruct> _nslist;
        /// <summary>
        /// 先序(用于往存在右孩子的父节点下插入左孩子的情况)，最好用StartPreOrder
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns></returns>
        private List<BNodeStruct> PreOrder(BTreeNode node)
        {
            if (node != null)
            {
                _nslist.Add(new BNodeStruct(node.Id, node.Data));
                PreOrder(node.Left);
                PreOrder(node.Right);
            }
            return _nslist;
        }

        /// <summary>
        /// 对先序的封装
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns></returns>
        private List<BNodeStruct> StartPreOrder(BTreeNode node)
        {
            _nslist = new List<BNodeStruct>();
            List<BNodeStruct> allRightChidren = PreOrder(node);
            return allRightChidren;
        }

        #region 获取字符串链表遍历结果
        private List<List<string>> _traversalString = new List<List<string>>();
        /// <summary>
        /// 获取每个节点内容的先序输出
        /// </summary>
        /// <param name="node">二叉树节点对象</param>
        /// <returns>字符串链表</returns>
        private List<List<string>> GetPreOrderString(BTreeNode node)
        {
            if (node != null)
            {
                List<string> tList = new List<string> { node.Data };
                _traversalString.Add(tList);
                GetPreOrderString(node.Left);
                GetPreOrderString(node.Right);
            }
            return _traversalString;
        }
        /// <summary>
        /// 获取每个节点内容的中序输出
        /// </summary>
        /// <param name="node">二叉树节点对象</param>
        /// <returns>字符串链表</returns>
        private List<List<string>> GetInOrderString(BTreeNode node)
        {
            if (node != null)
            {
                GetInOrderString(node.Left);
                List<string> tList = new List<string> { node.Data };
                _traversalString.Add(tList);
                GetInOrderString(node.Right);
            }
            return _traversalString;
        }
        /// <summary>
        /// 获取每个节点内容的后序输出
        /// </summary>
        /// <param name="node">二叉树节点对象</param>
        /// <returns>字符串链表</returns>
        private List<List<string>> GetPostOrderString(BTreeNode node)
        {
            if (node != null)
            {
                GetPostOrderString(node.Left);
                GetPostOrderString(node.Right);
                List<string> tList = new List<string> { node.Data };
                _traversalString.Add(tList);
            }
            return _traversalString;
        }
        #endregion 获取字符串链表遍历结果

        /// <summary>
        /// 获取当前节点的深度
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns></returns>
        private int Getdepth(BTreeNode node)
        {
            int depth = 1;
            BTreeNode p = node;
            while (p.Parent != null)
            {
                p = p.Parent;
                depth++;
            }
            return depth;
        }
        #endregion 二叉树的基本操作

        #region 内部操作函数
        private static readonly char[] Chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'R', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        /// <summary>
        /// 获取一个指定字符串数量的随机链表
        /// </summary>
        /// <param name="createSize">指定长度</param>
        /// <returns></returns>
        private string GetRamdomList(int createSize)
        {
            string values = "";
            const int length = 2; //随机字符数目
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
            return values;
        }

        /// <summary>
        /// 添加右子树，由于左插计时器，避免Graphviz固有的先左后右添加顺序
        /// </summary>
        /// <param name="bTreeNode">当前节点</param>
        private void AddRightSubtree(BTreeNode bTreeNode)
        {
            if (bTreeNode != null)
            {
                Vm.AddVertex(bTreeNode.Data);
                Vm.AddAdge(bTreeNode.Parent.Data, bTreeNode.Data, bTreeNode == bTreeNode.Parent.Left ? "L" : "R");
                Vm.SetId(bTreeNode.Data, bTreeNode.Id);
                AddRightSubtree(bTreeNode.Left);
                AddRightSubtree(bTreeNode.Right);
            }
        }

        /// <summary>
        /// 得到遍历出来的边的集合
        /// </summary>
        /// <param name="list">遍历结果字符串</param>
        /// <returns>边的集合</returns>
        private List<string> GetTranversalEdges(List<List<string>> list)
        {
            List<string> edges = new List<string>();
            foreach (var value in list)
            {
                BTreeNode node = (BTreeNode)_bTreeHashTable[value[0]];
                if (node.Parent == null) continue;
                var edge = node.Parent.Data + " " + node.Data;
                edges.Add(edge);
            }
            return edges;
        }

        //用户手动插入左孩子(有动画)
        private void LeftInsert(string data, string insertData)
        {
            _fatherData = data;
            _childData = insertData;
            _leftInsertTimer.Start();
        }

        //根据日志插入左孩子（无动画）
        private void LeftInsert(string data, string insertData, bool lf)
        {
            BTreeNode father = (BTreeNode)_bTreeHashTable[data];
            BTreeNode leftChild = new BTreeNode(insertData);
            _bTreeHashTable.Add(insertData, leftChild);
            if (father.Right != null)
            {
                Vm.RemoveNode(leftChild.Data, true);
                List<BNodeStruct> allRightChidren = StartPreOrder(father.Right);
                foreach (var vertex in allRightChidren)
                {
                    Vm.RemoveNode(vertex.Value, true);
                }
                Vm.AddVertex(leftChild.Data);
                father.Left = leftChild; //添加一条边
                leftChild.Parent = father;
                AddRightSubtree(father.Right);
                return;
            }
            leftChild.Parent = father;
            father.Left = leftChild;
        }

        //用户手动插入右孩子(有动画)
        private void RightInsert(string data, string insertData)
        {
            _fatherData = data;
            _childData = insertData;
            _rightInsertTimer.Start();
        }

        //根据日志插入右孩子（无动画）
        private void RightInsert(string data, string insertData, bool lf)
        {
            BTreeNode father = (BTreeNode)_bTreeHashTable[data];
            BTreeNode rightChild = new BTreeNode(insertData);
            _bTreeHashTable.Add(insertData, rightChild);
            rightChild.Parent = father;
            father.Right = rightChild;
        }

        /// <summary>
        /// 遍历删除
        /// </summary>
        /// <param name="selNode"></param>
        private void StartRemove(string selNode)
        {
            _traversalString = new List<List<string>>();
            _removePostOrderString = GetPostOrderString((BTreeNode)_bTreeHashTable[selNode]);
            _removeTimer.Start();
        }

        /// <summary>
        /// 用先序深度遍历来实现回退操作中的初始化操作
        /// </summary>
        /// <param name="initNodeFather"></param>
        /// <returns></returns>
        private void PreOrderInit(InitBTreeNode initNodeFather)
        {
            if (initNodeFather != null)
            {
                if (initNodeFather.Left != null)
                {
                    BTreeNode childBTreeNode = new BTreeNode(initNodeFather.Left.Data);
                    _bTreeHashTable.Add(childBTreeNode.Data, childBTreeNode);
                    BTreeNode father = (BTreeNode)_bTreeHashTable[initNodeFather.Data];
                    father.Left = childBTreeNode;
                    PreOrderInit(initNodeFather.Left);
                }
                if (initNodeFather.Right != null)
                {
                    BTreeNode childBTreeNode = new BTreeNode(initNodeFather.Right.Data);
                    _bTreeHashTable.Add(childBTreeNode.Data, childBTreeNode);
                    BTreeNode father = (BTreeNode)_bTreeHashTable[initNodeFather.Data];
                    father.Right = childBTreeNode;
                    PreOrderInit(initNodeFather.Right);
                }
            }
        }
        #endregion 内部操作函数

        #region 计时委托或与其同等级的实现函数

        #region 左/右递归随机
        private void RecursionRandom(BTreeNode father, InitBTreeNode fatherInitNode)
        {
            Random random = new Random();
            if (Getdepth(father) >= random.Next(4, 7))
                return;
            if (_initLength != 0 && _currentInitNodeNum < _initLength)
            {
                int n = random.Next(0, 10);
                if (n == 9 && _currentInitNodeNum <= 8)
                    n = random.Next(0, 10);
                string[] datas;
                BTreeNode leftNode;
                BTreeNode rightNode;
                InitBTreeNode initNode;
                switch (n)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4: //随机左+随机右
                        datas = GetRamdomList(2).Split(' ');
                        while (_bTreeHashTable.ContainsKey(datas[0]) || _bTreeHashTable.ContainsKey(datas[1]))
                        {
                            datas = GetRamdomList(2).Split(' ');
                        }
                        leftNode = new BTreeNode(datas[0]);
                        _bTreeHashTable.Add(datas[0], leftNode);
                        father.Left = leftNode; //添加一条边
                        leftNode.Parent = father;
                        #region 同步记录
                        initNode = new InitBTreeNode(datas[0]);
                        _initNodeHashtable.Add(datas[0], initNode);
                        fatherInitNode.Left = initNode;
                        _initializedValues += " " + datas[0];
                        #endregion
                        _currentInitNodeNum++;
                        rightNode = new BTreeNode(datas[1]);
                        _bTreeHashTable.Add(datas[1], rightNode);
                        father.Right = rightNode;
                        rightNode.Parent = father;
                        #region 同步记录
                        initNode = new InitBTreeNode(datas[1]);
                        _initNodeHashtable.Add(datas[1], initNode);
                        fatherInitNode.Right = initNode;
                        _initializedValues += " " + datas[1];
                        #endregion
                        _currentInitNodeNum++;
                        int sorting = random.Next(0, 2);//随机判断先左还是先右
                        if (sorting == 0)
                        {
                            RecursionRandom(leftNode, (InitBTreeNode)_initNodeHashtable[datas[0]]);
                            RecursionRandom(rightNode, (InitBTreeNode)_initNodeHashtable[datas[1]]);
                        }
                        else
                        {
                            RecursionRandom(rightNode, (InitBTreeNode)_initNodeHashtable[datas[1]]);
                            RecursionRandom(leftNode, (InitBTreeNode)_initNodeHashtable[datas[0]]);
                        }
                        break;
                    case 5:
                    case 6: //随机左
                        datas = GetRamdomList(1).Split(' ');
                        while (_bTreeHashTable.ContainsKey(datas[0]))
                        {
                            datas = GetRamdomList(1).Split(' ');
                        }
                        leftNode = new BTreeNode(datas[0]);
                        _bTreeHashTable.Add(datas[0], leftNode);
                        //消除graphviz永远先左后右添加节点的操作
                        if (father.Right != null)
                        {
                            Vm.RemoveNode(leftNode.Data, true);
                            List<BNodeStruct> allRightChidren = StartPreOrder(father.Right);
                            foreach (var vertex in allRightChidren)
                            {
                                Vm.RemoveNode(vertex.Value, true);
                            }
                            Vm.AddVertex(leftNode.Data, leftNode.Id);
                            father.Left = leftNode; //添加一条边
                            leftNode.Parent = father;
                            #region 同步记录
                            initNode = new InitBTreeNode(datas[0]);
                            _initNodeHashtable.Add(initNode.Data, initNode);
                            fatherInitNode.Left = initNode;
                            _initializedValues += " " + datas[0];
                            #endregion
                            AddRightSubtree(father.Right);
                            _currentInitNodeNum++;
                            RecursionRandom(leftNode, initNode);
                            break;
                        }
                        father.Left = leftNode; //添加一条边
                        leftNode.Parent = father;
                        #region 同步记录
                        initNode = new InitBTreeNode(datas[0]);
                        _initNodeHashtable.Add(initNode.Data, initNode);
                        fatherInitNode.Left = initNode;
                        _initializedValues += " " + datas[0];
                        #endregion
                        _currentInitNodeNum++;
                        RecursionRandom(leftNode, initNode);
                        break;
                    case 7:
                    case 8: //随机右
                        datas = GetRamdomList(1).Split(' ');
                        while (_bTreeHashTable.ContainsKey(datas[0]))
                        {
                            datas = GetRamdomList(1).Split(' ');
                        }
                        rightNode = new BTreeNode(datas[0]);
                        _bTreeHashTable.Add(datas[0], rightNode);
                        father.Right = rightNode; //添加一条边
                        rightNode.Parent = father;
                        #region 同步记录
                        initNode = new InitBTreeNode(datas[0]);
                        _initNodeHashtable.Add(datas[0], initNode);
                        fatherInitNode.Right = initNode;
                        _initializedValues += " " + datas[0];
                        #endregion
                        _currentInitNodeNum++;
                        RecursionRandom(rightNode, initNode);
                        break;
                    case 9: //无
                        break;
                }
            }
        }

        #endregion 左/右递归随机

        #region 左/右插计时器
        void timer_Tick(object sender, EventArgs e)
        {
            if (_graphLayout.GetLayoutDirector().GetDotGraph() != null)
            {
                Point start = Vm.GetVertexPosition(_fatherData, _graphLayout);
                Point end = Vm.GetVertexPosition(_childData, _graphLayout);
                Vm.AddMyLine(start, end, Colors.White, 19, _graphLayout);
                _multithreadCoverTimer.Stop();
            }
        }
        private void LeftInsertTimer_Tick(object sender, EventArgs e)
        {
            switch (_lClock)
            {
                case 1:
                    {
                        BTreeNode leftNode = new BTreeNode(_childData);
                        _bTreeHashTable.Add(_childData, leftNode);
                        BTreeNode father = (BTreeNode)_bTreeHashTable[_fatherData];
                        BTreeNode leftChild = (BTreeNode)_bTreeHashTable[_childData];

                        //消除graphviz永远先左后右添加节点的操作
                        if (father.Right != null)
                        {
                            Vm.RemoveNode(leftChild.Data, true);
                            List<BNodeStruct> allRightChidren = StartPreOrder(father.Right);
                            foreach (var vertex in allRightChidren)
                            {
                                Vm.RemoveNode(vertex.Value, true);
                            }
                            Vm.AddVertex(leftChild.Data, leftChild.Id);
                            father.Left = leftChild; //添加一条边
                            leftChild.Parent = father;
                            AddRightSubtree(father.Right);
                            Vm.Graph.RaiseChangedByTheCustom();
                            _multithreadCoverTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                            _multithreadCoverTimer.Tick += timer_Tick;
                            _multithreadCoverTimer.Start();
                            break;
                        }
                        father.Left = leftChild; //添加一条边
                        leftChild.Parent = father;
                        Vm.Graph.RaiseChangedByTheCustom();

                        //实现轮询遮盖
                        _multithreadCoverTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                        _multithreadCoverTimer.Tick += timer_Tick;
                        _multithreadCoverTimer.Start();
                    }
                    break;
                case 2:
                    {
                        Point father = Vm.GetVertexPosition(_fatherData, _graphLayout);
                        father.Y = father.Y + 10;//一个节点的高度是21.所以除以2等于10
                        Point child = Vm.GetVertexPosition(_childData, _graphLayout);
                        child.Y = child.Y - 10;
                        Vm.AddMyArrow(father, child, 0, _graphLayout);
                    }
                    break;
                case 3:
                    {
                        Vm.Graph.RaiseChangedByTheCustom();
                        //关计时器
                        _leftInsertTimer.Stop();
                        _lClock = 0;
                        _fatherData = null;
                        _childData = null;
                    }
                    break;
            }
            _lClock++;
        }
        private void RightInsertTimer_Tick(object sender, EventArgs e)
        {
            switch (_rClock)
            {
                case 1:
                    {
                        BTreeNode rightNode = new BTreeNode(_childData);
                        _bTreeHashTable.Add(_childData, rightNode);
                        BTreeNode father = (BTreeNode)_bTreeHashTable[_fatherData];
                        BTreeNode rightChild = (BTreeNode)_bTreeHashTable[_childData];
                        father.Right = rightChild; //添加一条边
                        rightChild.Parent = father;
                        Vm.Graph.RaiseChangedByTheCustom();

                        //实现轮询遮盖
                        _multithreadCoverTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                        _multithreadCoverTimer.Tick += timer_Tick;
                        _multithreadCoverTimer.Start();
                    }
                    break;
                case 2:
                    {
                        Point father = Vm.GetVertexPosition(_fatherData, _graphLayout);
                        father.Y = father.Y + 10;//一个节点的高度是21.所以除以2等于10
                        Point child = Vm.GetVertexPosition(_childData, _graphLayout);
                        child.Y = child.Y - 10;
                        Vm.AddMyArrow(father, child, 0, _graphLayout);
                    }
                    break;
                case 3:
                    {
                        Vm.Graph.RaiseChangedByTheCustom();
                        //关计时器
                        _rightInsertTimer.Stop();
                        _rClock = 0;
                        _fatherData = null;
                        _childData = null;
                    }
                    break;
            }
            _rClock++;
        }

        #endregion 左/右插计时器

        private int _i;
        private int _j;
        /// <summary>
        /// 删除计时器
        /// </summary>
        /// <param name="sender">消息发送者</param>
        /// <param name="e">它保存事件数据</param>
        private void RemoveTimer_Tick(object sender, EventArgs e)
        {
            if (_i >= _removePostOrderString.Count)
            {//节点删除完毕
                _i = 0;
                _removeTimer.Stop();
            }
            else
            {//删除过程
                if (_j == 0)
                {
                    //画一个叉
                    string nodeData = _removePostOrderString[_i][0];
                    if (nodeData.Equals(Head.Data))
                    {//如果只剩一个根节点就直接删除
                        BTreeNode.Sid = -1;
                        Clear();
                        _initBtreeHeads.Clear();
                        Vm.Graph.RaiseChangedByTheCustom();
                        _i++;
                        return;
                    }
                    BTreeNode currentNode = (BTreeNode)_bTreeHashTable[nodeData];
                    if (currentNode.Parent != null)
                    {
                        BTreeNode fatherNode = currentNode.Parent;
                        Point start = Vm.GetVertexPosition(fatherNode.Data, _graphLayout);
                        Point end = Vm.GetVertexPosition(currentNode.Data, _graphLayout);
                        Point tPoint = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
                        Vm.AddMyX(tPoint, _graphLayout);
                    }
                    _j++;
                }
                else
                {//_j==1
                    //真正的删除
                    string nodeData = _removePostOrderString[_i][0];
                    Vm.RemoveNode(nodeData, true);
                    BTreeNode selectedNode = (BTreeNode)_bTreeHashTable[nodeData];
                    if (selectedNode.Parent.Left == selectedNode)
                        selectedNode.Parent.Left = null;
                    else
                        selectedNode.Parent.Right = null;
                    _bTreeHashTable.Remove(nodeData);

                    Vm.Graph.RaiseChangedByTheCustom();
                    _j = 0;
                    _i++;
                }
            }
        }
        #endregion 计时委托或与其同等级的实现函数

        #region 数据结构对外界所提供的函数
        private int _initLength;
        private int _currentInitNodeNum;
        /// <summary>
        /// 随机生成二叉树
        /// </summary>
        public void RandomInit()
        {
            _initLength = 0;
            _currentInitNodeNum = 0;
            if (Size() > 0)
            {
                Clear();
            }
            BTreeNode.Sid = -1;
            Random random = new Random();
            _initLength = random.Next(10, 64 - 1 + 10);//6层的数量是2的6次方减1，在此基础上再做调整
            string[] root = GetRamdomList(1).Split(' ');//随机生成根节点
            Head = new BTreeNode(root[0]);
            _bTreeHashTable.Add(Head.Data, Head);
            #region 记录初始化的节点
            InitBTreeNode initHead = new InitBTreeNode(root[0]);
            _initBtreeHeads.Add(initHead);
            _initNodeHashtable.Add(root[0], initHead);
            _initializedValues = root[0];
            #endregion
            _currentInitNodeNum++;
            RecursionRandom(Head, initHead);
            Vm.Graph.RaiseChangedByTheCustom();
            LogId++;
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "初始化(" + _currentInitNodeNum + ")", "", _initializedValues));
        }

        public void AddRoot(string data)
        {
            if (Head != null)
            {
                throw new Exception("根节点已存在！");
            }
            Head = new BTreeNode(data);
            Vm.Graph.RaiseChangedByTheCustom();
            _bTreeHashTable.Add(data, Head);
            LogId++;
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "创建根节点", "", data));
        }

        /// <summary>
        /// 主要完成对用户输入数据规范性的验证
        /// </summary>
        /// <param name="parent">目标节点的值</param>
        /// <param name="data">孩子节点的值</param>
        /// <param name="graphLayout">Graphviz的画布对象</param>
        public void AddLeft(string parent, string data, GraphLayout graphLayout)
        {
            _graphLayout = graphLayout;
            Regex reg = new Regex(@"^[a-zA-Z0-9_]+$");
            if (!reg.IsMatch(parent) || !reg.IsMatch(data))
            {
                throw new Exception("节点值只能是字母、数字或下划线");
            }
            if (!parent.Equals("") && !data.Equals(""))
            {
                if (!_bTreeHashTable.ContainsKey(parent))
                {
                    throw new Exception("您选择的节点不存在");
                }
                if (_bTreeHashTable.ContainsKey(data))
                {
                    throw new Exception("节点内容重复");
                }
                BTreeNode p = (BTreeNode)_bTreeHashTable[parent];
                if (p.Left != null)
                {
                    throw new Exception("您选中的节点已存在左孩子");
                }
                LeftInsert(parent, data);
                LogId++;
                BTreeLogs.Add(LogId + "", new LogVo(LogId, "插入左节点", parent, data));
            }
            else if (parent.Equals(""))
                throw new Exception("请选择要插入左孩子的节点");
            else
                throw new Exception("请输入左孩子的内容");
        }

        public void AddRight(string parent, string data, GraphLayout graphLayout)
        {
            _graphLayout = graphLayout;
            if (!parent.Equals("") && !data.Equals(""))
            {
                if (!_bTreeHashTable.ContainsKey(parent))
                {
                    throw new Exception("您选择的节点不存在");
                }
                if (_bTreeHashTable.ContainsKey(data))
                {
                    throw new Exception("节点内容重复");
                }
                BTreeNode p = (BTreeNode)_bTreeHashTable[parent];
                if (p.Right != null)
                {
                    throw new Exception("您选中的节点已存在右孩子");
                }
                RightInsert(parent, data);
                LogId++;
                BTreeLogs.Add(LogId + "", new LogVo(LogId, "插入右节点", parent, data));
            }
            else if (parent.Equals(""))
                throw new Exception("请选择要插入右孩子的节点");
            else
                throw new Exception("请输入右孩子的内容");
        }

        public void DeleteSelNode(string selNode, GraphLayout graphLayout)
        {
            _graphLayout = graphLayout;
            if (string.IsNullOrEmpty(selNode))
            {
                throw new Exception("请输入要删除节点的data值");
            }
            if (!_bTreeHashTable.ContainsKey(selNode))
            {
                throw new Exception("该节点不存在");
            }
            if (selNode.Equals(Head.Data))
            {
                if (MessageBox.Show("删除根节点也将清空日志记录，继续吗？", "确认信息",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                    return;
                LogId = -1;
            }
            //DoDelete(selNode);
            StartRemove(selNode);

            LogId++;
            if (LogId == 0)
            {
                BTreeLogs.Clear();
                return;
            }
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "删除节点", "", selNode));
        }

        public void Traversal(string theWayOfTraverse, GraphLayout graphLayout)
        {
            List<List<string>> list;
            List<string> edges;
            switch (theWayOfTraverse)
            {
                case "先序":
                    _traversalString = new List<List<string>>();
                    list = GetPreOrderString(Head);
                    edges = GetTranversalEdges(list);
                    Vm.ViewModelTraversal(edges, list, theWayOfTraverse, graphLayout);
                    break;
                case "中序":
                    _traversalString = new List<List<string>>();
                    list = GetInOrderString(Head);
                    edges = GetTranversalEdges(list);
                    Vm.ViewModelTraversal(edges, list, theWayOfTraverse, graphLayout);
                    break;
                case "后序":
                    _traversalString = new List<List<string>>();
                    list = GetPostOrderString(Head);
                    edges = GetTranversalEdges(list);
                    Vm.ViewModelTraversal(edges, list, theWayOfTraverse, graphLayout);
                    break;
            }
        }

        public void BackTo(string step)
        {
            if (string.IsNullOrEmpty(step))
                return;
            if (int.Parse(step) > LogId || int.Parse(step) <= 0)
            {
                throw new Exception("请输入正确的步数！");
            }
            Hashtable tBTreeLogs = new Hashtable();
            List<InitBTreeNode> tInitHeads = new List<InitBTreeNode>();
            int j = 0;//用来索引_initBtreeHeads链表中的头结点
            for (int i = 1; i <= int.Parse(step); i++)
            {
                LogVo logVo = (LogVo)BTreeLogs[i + ""];
                tBTreeLogs.Add(i + "", logVo);
                if (logVo.Action.Contains("初始化"))
                {
                    tInitHeads.Add(_initBtreeHeads[j]);
                    j++;
                }
            }
            //让_initBtreeHeads链表回退到step那个时间点时的状态
            _initBtreeHeads.Clear();
            foreach (InitBTreeNode head in tInitHeads)
            {
                _initBtreeHeads.Add(head);
            }
            Clear();
            j = 0;//用来索引日志中的“初始化”命令
            for (int i = 1; i <= int.Parse(step); i++)
            {
                LogVo tlogVo = (LogVo)BTreeLogs[i + ""];
                if (tlogVo.Action.Equals("创建根节点"))
                {
                    Head = new BTreeNode(tlogVo.Data);
                    _bTreeHashTable.Add(tlogVo.Data, Head);
                }
                else if (tlogVo.Action.Equals("插入左节点"))
                {
                    LeftInsert(tlogVo.Selectdata, tlogVo.Data, true);
                }
                else if (tlogVo.Action.Equals("插入右节点"))
                {
                    RightInsert(tlogVo.Selectdata, tlogVo.Data, true);
                }
                else if (tlogVo.Action.Contains("初始化"))
                {
                    if (j < _initBtreeHeads.Count)
                        Clear();
                    Head = new BTreeNode(_initBtreeHeads[j].Data);
                    _bTreeHashTable.Add(_initBtreeHeads[j].Data, Head);
                    PreOrderInit(_initBtreeHeads[j]);
                    j++;
                }
            }
            LogId = int.Parse(step);
            BTreeLogs = tBTreeLogs;
            Vm.Graph.RaiseChangedByTheCustom();
        }

        private void CommandPrompt()
        {
            throw new Exception("请输入如下命令：\ncreate root node a\ninsert left node a to b\ninsert left node a to c\nDelete a\nBack to step 2");
        }

        public void Command_lineOperation(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return;
            }
            string[] cmds = commandLine.Split(' ');
            Regex reg = new Regex(@"^[0-9]*$");
            WhichCommand = null;//消除缓存
            if (cmds.Length == 4 && cmds[0].ToLower().Equals("create") && cmds[1].ToLower().Equals("root") && cmds[2].ToLower().Equals("node") && !string.IsNullOrEmpty(cmds[3]))
            {//创建根节点
                AddRoot(cmds[3]);
                WhichCommand = "create root";
            }
            else if (cmds.Length == 6 && cmds[0].ToLower().Equals("insert") && (cmds[1].ToLower().Equals("left") || cmds[1].ToLower().Equals("right")) && cmds[2].ToLower().Equals("node") && cmds[4].ToLower().Equals("to") && !string.IsNullOrEmpty(cmds[3]) && !string.IsNullOrEmpty(cmds[5]))
            {
                if (cmds[1].ToLower().Equals("left"))
                {
                    AddLeft(cmds[5], cmds[3], _graphLayout);
                    WhichCommand = "insert left";
                }
                else if (cmds[1].ToLower().Equals("right"))
                {
                    AddRight(cmds[5], cmds[3], _graphLayout);
                    WhichCommand = "insert right";
                }
            }
            else if (cmds.Length == 3 && cmds[0].ToLower().Equals("delete") && cmds[1].ToLower().Equals("value") && !string.IsNullOrEmpty(cmds[2]))
            {
                DeleteSelNode(cmds[2], _graphLayout);
                WhichCommand = "delete";
            }
            else if (cmds.Length == 4 && cmds[0].ToLower().Equals("back") && cmds[1].ToLower().Equals("to") && cmds[2].ToLower().Equals("step") && reg.IsMatch(cmds[3]))
            {
                BackTo(cmds[3]);
                WhichCommand = "back to";
            }
            else
            {
                CommandPrompt();
            }
        }

        public int Size()
        {
            return _bTreeHashTable.Count;
        }

        public void Clear()
        {
            Vm.Graph.Clear(true);
            Head = null;
            _bTreeHashTable.Clear();
            if (_initNodeHashtable != null)
                _initNodeHashtable.Clear();
        }

        /// <summary>
        /// 刷新画布
        /// </summary>
        public void RefreshCanvas()
        {
            Vm.AddVertex(" ");//人工加入一个点，强行触发RaiseChanged事件
            Vm.RemoveNode(" ", true);
            Vm.Graph.RaiseChangedByTheCustom();
        }
        #endregion 数据结构对外界所提供的函数
    }
}
