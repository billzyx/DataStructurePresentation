using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Graphviz4Net.WPF;
using integrateOfDataStructure.Utility;

namespace integrateOfDataStructure
{
    public class ChildrenList<T> : List<T> where T : MTreeNode
    {
        public void AddAChild(MTreeNode thisNode, T item)
        {
            item.Parent = thisNode;
            thisNode.NChildren++;
            item.Level = thisNode.Level + 1;
            Add(item);
            MultiTree.Vm.AddAdge(thisNode.Data, item.Data,string.Empty);
        }

        public void InsertAChild(MTreeNode thisNode, int index, T item)
        {
            item.Parent = thisNode;
            thisNode.NChildren++;
            item.Level = thisNode.Level + 1;
            Insert(index, item);
            MultiTree.Vm.AddAdge(thisNode.Data, item.Data,string.Empty);
        }

        public void RemoveAChild(MTreeNode parent, int index)
        {
            parent.NChildren--;
            RemoveAt(index);
        }

        public void RemoveAt_ASubtree(MTreeNode parentNode, int index, MultiTree multiTree)
        {
            ArrayList arrayList = multiTree.Mark(parentNode.Children[index]);
            while (arrayList.Count > 0)
            {
                MultiTree.Vm.RemoveNode(arrayList[0].ToString(), true);
                MTreeNode node = (MTreeNode)multiTree.MTreeHashTable[arrayList[0].ToString()];
                node.Parent.Children.Remove(node);
                node.Parent.NChildren--;
                multiTree.MTreeHashTable.Remove(arrayList[0].ToString());
                arrayList.RemoveAt(0);
            }
        }
    }

    ///简化版节点,用于在回退里的初始化操作中记录随机初始化了怎么样一颗多叉树
    public class InitMTreeNode
    {
        public InitMTreeNode()
        {
            Children = new List<InitMTreeNode>();
        }
        public InitMTreeNode(string data)
        {
            Data = data;
            Children = new List<InitMTreeNode>();
        }
        public string Data;
        public List<InitMTreeNode> Children;
    }

    public class MTreeNode
    {
        private string _data;//节点数据
        private int _level = -1;// 记录该节点在多叉树中的层数

        public MTreeNode(string data)
        {
            Children = new ChildrenList<MTreeNode>();
            Data = data;
        }

        public string Data
        {
            get { return _data; }
            set
            {
                _data = value;
                MultiTree.Vm.AddVertex(value);
            }
        }

        public int NChildren { get; set; }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public ChildrenList<MTreeNode> Children { get; set; }

        /// <summary>
        /// 父节点访问器
        /// </summary>
        public MTreeNode Parent { get; set; }
    }

    public class MultiTree
    {
        public static ViewModel Vm = new ViewModel();
        private GraphLayout _graphLayout;
        private MTreeNode _head;
        public MTreeNode Head
        {
            get { return _head; }
            set
            {
                _head = value;
                if (value != null)//如果不是,清空多叉树操作
                    _head.Level = 0;//初始化根节点的深度
            }
        }

        private string _initializedValues = "";// 随机初始化的所有节点的值
        private readonly List<InitMTreeNode> _initMtreeHeads;//用于记录随机初始化了的多少颗多叉树
        private readonly Hashtable _initNodeHashtable = new Hashtable();

        public int LogId;
        public Hashtable MTreeHashTable = new Hashtable();//多叉树节点存储容器——哈希表
        public Hashtable MTreeLogs = new Hashtable();//多叉树日志对象

        //计时器相关：
        private readonly DispatcherTimer _childInsertTimer = new DispatcherTimer();
        private readonly DispatcherTimer _removeTimer = new DispatcherTimer();
        private DispatcherTimer _timer;//用于完成多线程覆盖操作

        private int _childInsertClock = 1;//插入孩子计时器
        private string _fatherData;//全局变量：当前操作的父节点内容
        private int _childIndex;//全局变量：当前操作的孩子节点位置
        private string _childData;//全局变量：当前操作的孩子内容
        private List<string> _removePostOrderString;//遍历删除的节点顺序

        public MultiTree()
        {
            _initMtreeHeads = new List<InitMTreeNode>();
            _childInsertTimer.Tick += ChildInsertTimer_Tick;
            _childInsertTimer.Interval = new TimeSpan(0, 0, 1);
            _removeTimer.Tick += RemoveTimer_Tick;
            _removeTimer.Interval = new TimeSpan(0, 0, 1);
        }

        #region 多叉树的基本操作
        /// <summary>
        /// 深度优先遍历查找
        /// </summary>
        /// <param name="data">查找内容</param>
        /// <param name="head">当前节点（从首节点开始）</param>
        /// <returns>目标节点</returns>
        private MTreeNode search_node_r(string data, MTreeNode head)
        {
            MTreeNode temp = null;
            if (head != null)
            {
                if (data.Equals(head.Data))
                {
                    temp = head; //如果名字匹配
                }
                else //如果不匹配，则查找其子节点
                {
                    for (int i = 0; i < head.NChildren && temp == null; i++)
                    {
                        temp = search_node_r(data, head.Children[i]);
                    }
                }
            }
            return temp;
        }

        /// <summary>
        /// 从文件中读取多叉树数据，并建立多叉树
        /// </summary>
        /// <param name="head">多叉树根节点</param>
        /// <param name="filePath">文件路径</param>
        public void read_file(ref MTreeNode head, string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                //一行行读取直至为NULL
                string strLine;
                while ((strLine = sr.ReadLine()) != null)
                {
                    string[] strings = strLine.Split(' ');
                    var data = strings[0];
                    var n = int.Parse(strings[1]);
                    MTreeNode temp;
                    if (head == null) //若为空
                    {
                        //让temp和head引用同一块内存空间
                        temp = head = new MTreeNode(data); //生成一个新节点
                    }
                    else
                    {
                        temp = search_node_r(data, head);
                        //这里默认数据文件是正确的，一定可以找到与data匹配的节点
                        //如果不匹配，那么应该忽略本行数据
                    }
                    //找到节点后，对子节点进行处理
                    temp.NChildren = n;
                    for (int i = 0; i < n; i++)
                    {
                        var child = strings[i + 2];
                        temp.Children.AddAChild(temp, new MTreeNode(child));
                    }
                }
            }
        }

        /// <summary>
        /// 实现函数1
        /// 将多叉树中的节点，按照深度进行输出
        /// 实质上实现的是层次优先遍历
        /// </summary>
        /// <param name="head">首节点</param>
        private void F1(MTreeNode head)
        {
            MTreeNode tMTreeNode;
            Queue<MTreeNode> queue = new Queue<MTreeNode>(100); //将队列初始化大小为100
            Stack<MTreeNode> stack = new Stack<MTreeNode>(100); //将栈初始化大小为100
            head.Level = 0; //根节点的深度为0

            //将根节点入队列
            queue.Enqueue(head);

            //对多叉树中的节点的深度值level进行赋值
            //采用层次优先遍历方法，借助于队列
            while (queue.Count != 0) //如果队列q不为空
            {
                tMTreeNode = queue.Dequeue(); //出队列
                for (int i = 0; i < tMTreeNode.NChildren; i++)
                {
                    tMTreeNode.Children[i].Level = tMTreeNode.Level + 1; //对子节点深度进行赋值：父节点深度加1
                    queue.Enqueue(tMTreeNode.Children[i]); //将子节点入队列
                }
                stack.Push(tMTreeNode); //将p入栈
            }

            while (stack.Count != 0) //不为空
            {
                tMTreeNode = stack.Pop(); //弹栈
                Debug.WriteLine("   {0} {1}\n", tMTreeNode.Level, tMTreeNode.Data);
            }
        }

        /// <summary>
        /// 实现函数2
        /// 找到从根节点到叶子节点路径上节点名字字母个数最大的路径
        /// 实质上实现的是深度优先遍历
        /// </summary>
        /// <param name="head">首节点</param>
        /// <param name="str">临时字符串</param>
        /// <param name="strBest">从根节点到叶子节点路径上节点名字字母个数最大的路径</param>
        /// <param name="level">当前深度</param>
        private void F2(MTreeNode head, string str, ref string strBest, int level)
        {
            if (head == null) return;
            var tmp = str + head.Data;

            if (head.NChildren == 0)
            {
                if (strBest == null || tmp.Length > strBest.Length)
                {
                    strBest = tmp;
                }
            }
            for (var i = 0; i < head.NChildren; i++)
            {
                F2(head.Children[i], tmp, ref strBest, level + 1);
            }
        }

        #region 获取字符串链表遍历结果

        /// <summary>
        /// 深度优先遍历
        /// </summary>
        /// <param name="head">多叉树首节点</param>
        private List<List<string>> DepthFirstTraversal(MTreeNode head)
        {
            _slist = new List<List<string>>();
            return PreOrder(head, 0);
        }
        /// <summary>
        /// 广度优先遍历也就是层序遍历
        /// </summary>
        /// <param name="head">首节点</param>
        private List<List<string>> BreadthFirstSearch(MTreeNode head)
        {
            List<List<string>> traversalString = new List<List<string>>();
            Queue<MTreeNode> queue = new Queue<MTreeNode>(100); //将队列初始化大小为100
            //将根节点入队列
            queue.Enqueue(head);
            //采用层次优先遍历方法，借助于队列
            while (queue.Count != 0) //如果队列q不为空
            {
                MTreeNode tMTreeNode = queue.Dequeue();
                for (int i = 0; i < tMTreeNode.NChildren; i++)
                {
                    queue.Enqueue(tMTreeNode.Children[i]); //将子节点入队列
                }
                List<string>tList=new List<string>{tMTreeNode.Data};
                traversalString.Add(tList);
            }
            return traversalString;
        }
        #endregion 获取字符串链表遍历结果

        #region 先序输出(用于插入到指定位置之后存在兄弟节点的情况)
        private List<List<string>> _slist = new List<List<string>>();
        /// <summary>
        /// 变异的先序输出
        /// </summary>
        /// <param name="father">目标节点</param>
        /// <param name="childIndex">孩子节点的索引</param>
        /// <returns>返回的目标节点值+所有指定位置之后的所有节点值</returns>
        private List<List<string>> PreOrder(MTreeNode father, int childIndex)
        {
            if (father != null)
            {
                List<string> tList=new List<string> {father.Data};
                _slist.Add(tList);
                for (int i = childIndex; i < father.NChildren; i++)
                {
                    PreOrder(father.Children[i], 0);//只有第一次是从childIndex的位置开始的，以后每次都是从头开始
                }
            }
            return _slist;
        }
        #endregion 先序输出

        private List<string> _traversalString;
        /// <summary>
        /// 获取每个节点内容的后序输出
        /// </summary>
        /// <param name="father">多叉树节点对象</param>
        /// <returns>字符串链表</returns>
        private List<string> GetPostOrderString(MTreeNode father)
        {
            if (father != null)
            {
                for (int i = 0; i < father.NChildren; i++)
                {
                    GetPostOrderString(father.Children[i]);
                }
                _traversalString.Add(father.Data);
            }
            return _traversalString;
        }

        private void AddLaterSubtree(MTreeNode mTreeNode, int childIndex)
        {
            if (mTreeNode != null)
            {
                if (childIndex == 0) //过滤掉用户指定目标节点
                {
                    Vm.AddVertex(mTreeNode.Data);
                    Vm.AddAdge(mTreeNode.Parent.Data, mTreeNode.Data,"");
                }
            }
            Debug.Assert(mTreeNode != null, "mTreeNode != null");
            for (int i = childIndex; i < mTreeNode.NChildren; i++)
            {
                AddLaterSubtree(mTreeNode.Children[i], 0);
            }
        }

        private ArrayList _markArray; //用作记录删除节点内容的字典，由于markchild是递归的所以只能设置为全局变量
        /// <summary>
        /// 以后序深度优先遍历标记某个节点的所有子节点的值
        /// </summary>
        /// <param name="node">当前子树的首节点</param>
        /// <returns>返回当前子树所有节点的后序记录集</returns>
        public ArrayList Mark(MTreeNode node)
        {
            _markArray = new ArrayList();
            //当为空的一种情况
            MarkChild(node);
            return _markArray;
        }

        private void MarkChild(MTreeNode node)
        {
            if (node != null)
            {
                for (int i = 0; i < node.NChildren; i++)
                {
                    MarkChild(node.Children[i]);
                }
                _markArray.Add(node.Data);
            }
        }

        /// <summary>
        /// 获取当前节点的深度
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns></returns>
        private int Getdepth(MTreeNode node)
        {
            int depth = 1;
            MTreeNode p = node;
            while (p.Parent != null)
            {
                p = p.Parent;
                depth++;
            }
            return depth;
        }
        #endregion 多叉树的基本操作

        #region 计时委托
        void timer_Tick(object sender, EventArgs e)
        {
            if (_graphLayout.GetLayoutDirector().GetDotGraph() != null)
            {
                Point start = Vm.GetVertexPosition(_fatherData, _graphLayout);
                Point end = Vm.GetVertexPosition(_childData, _graphLayout);
                Vm.AddMyLine(start, end, Colors.White, 5, _graphLayout);
                _timer.Stop();
            }
        }
        private void ChildInsertTimer_Tick(object sender, EventArgs e)
        {
            switch (_childInsertClock)
            {
                case 1:
                    {
                        MTreeNode childNode = new MTreeNode(_childData);
                        MTreeHashTable.Add(_childData, childNode);
                        MTreeNode father = (MTreeNode)MTreeHashTable[_fatherData];
                        MTreeNode child = (MTreeNode)MTreeHashTable[_childData];
                        if (_childIndex > father.NChildren)
                        {
                            _childIndex = father.NChildren;
                        }
                        if (_childIndex >= 0 && _childIndex < father.NChildren)
                        {//加这一段是因为Graphviz自身存在的布局算法中的一些缺陷
                            //也就是节点位置的添加每次只能以追加的形式加入到孩子列表的末尾
                            _slist = new List<List<string>>();
                            Vm.RemoveNode(child.Data, true);//先删掉“case 1”中添加的节点
                            List<List<string>> allLaterChildren = PreOrder(father, _childIndex);
                            allLaterChildren.RemoveAt(0);//截取掉allLaterChildren链表中的首节点

                            foreach (var vertex in allLaterChildren)
                            {
                                Vm.RemoveNode(vertex[0], true);
                            }
                            Vm.AddVertex(child.Data);
                            father.Children.InsertAChild(father, _childIndex, child);//添加一条边
                            AddLaterSubtree(father, _childIndex + 1);
                            Vm.Graph.RaiseChangedByTheCustom();
                            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                            _timer.Tick += timer_Tick;
                            _timer.Start();

                            break;
                        }
                        father.Children.AddAChild(father, child);
                        Vm.Graph.RaiseChangedByTheCustom();
                        _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                        _timer.Tick += timer_Tick;
                        _timer.Start();
                    }
                    break;
                case 2:
                    {
                        Point father = Vm.GetVertexPosition(_fatherData, _graphLayout);
                        father.Y = father.Y + 21 / 2;//一个节点的高度是21.所以除以2
                        Point child = Vm.GetVertexPosition(_childData, _graphLayout);
                        child.Y = child.Y - 21 / 2;
                        Vm.AddMyArrow(father, child,0, _graphLayout);
                    }
                    break;
                case 3:
                    {
                        Vm.Graph.RaiseChangedByTheCustom();
                        //关计时器
                        _childInsertTimer.Stop();
                        _childInsertClock = 0;
                        _fatherData = null;
                        _childData = null;
                    }
                    break;
            }
            _childInsertClock++;
        }

        // 删除节点
        private void DoDelete(string data)
        {
            //哈希表删除
            MTreeNode selectedNode = (MTreeNode)MTreeHashTable[data];
            ArrayList arrayList = Mark(selectedNode);
            if (selectedNode != Head)
            {
                for (int i = selectedNode.Parent.NChildren - 1; i >= 0; i--)
                {
                    if (selectedNode == selectedNode.Parent.Children[i])
                    {
                        selectedNode.Parent.Children.RemoveAt_ASubtree(selectedNode.Parent, i, this);
                        break;
                    }
                }
            }
            else
            {
                //整颗树节点的删除
                //Head = null;
                LogId = -1;
                //MTreeHashTable.Clear();
                //_initNodeHashtable.Clear();
                //_initMtreeHeads.Clear();
                //整颗
                for (int i = arrayList.Count - 1; i >= 0; i--)
                {
                    Vm.RemoveNode(arrayList[i].ToString(), true);
                }
            }
        }

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
                    string nodeData = _removePostOrderString[_i];
                    if (nodeData.Equals(Head.Data))
                    {//如果只剩一个根节点就直接删除
                        Clear();
                        _initMtreeHeads.Clear();
                        Vm.Graph.RaiseChangedByTheCustom();
                        _i++;
                        return;
                    }
                    MTreeNode currentNode = (MTreeNode)MTreeHashTable[nodeData];
                    if (currentNode.Parent != null)
                    {
                        MTreeNode fatherNode = currentNode.Parent;
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
                    string nodeData = _removePostOrderString[_i];
                    Vm.RemoveNode(nodeData, true);
                    MTreeNode selectedNode = (MTreeNode)MTreeHashTable[nodeData];

                    for (int i = 0; i < selectedNode.Parent.NChildren; i++)
                    {
                        if (selectedNode.Parent.Children[i] == selectedNode)
                        {
                            selectedNode.Parent.Children.RemoveAChild(selectedNode.Parent, i);
                            break;
                        }
                    }
                    MTreeHashTable.Remove(nodeData);

                    Vm.Graph.RaiseChangedByTheCustom();
                    _j = 0;
                    _i++;
                }
            }
        }
        #endregion 计时委托

        #region 数据结构内部操作函数
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
            return values;
        }

        /// <summary>
        /// 打乱一个链表的顺序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputList"></param>
        /// <returns></returns>
        private List<T> DisruptTheOrder<T>(List<T> inputList)
        {
            //Copy to a array
            T[] copyArray = new T[inputList.Count];
            inputList.CopyTo(copyArray);

            //Add range
            List<T> copyList = new List<T>();
            copyList.AddRange(copyArray);

            //Set outputList and random
            List<T> outputList = new List<T>();
            Random rd = new Random(DateTime.Now.Millisecond);

            while (copyList.Count > 0)
            {
                //Select an index and item
                int rdIndex = rd.Next(0, copyList.Count - 1);
                T remove = copyList[rdIndex];

                //remove it from copyList and add it to output
                copyList.Remove(remove);
                outputList.Add(remove);
            }
            return outputList;
        }

        /// <summary>
        /// 递归随机初始化多叉树
        /// </summary>
        /// <param name="father">父节点</param>
        /// <param name="fatherInitNode">父节点记录节点</param>
        private void RecursionRandom(MTreeNode father, InitMTreeNode fatherInitNode)
        {
            Random random = new Random();
            if (Getdepth(father) >= random.Next(4, 7))
                return;
            if (_initLength != 0 && _currentInitNodeNum < _initLength)
            {
                int n = random.Next(0, 6);                      //有六种情况，节点数分别为：0、1、2、3、4、5
                if (n == 0 && _currentInitNodeNum <= 8)         //如果随机出来没有节点了，并且当前小于两个节点
                    n = random.Next(0, 6);                      //则：再随机一遍（为了随机出来的多叉树更好看）
                string[] datas;
                MTreeNode child;
                InitMTreeNode initNode;
                List<int> indexSorting = new List<int>();       //索引排序
                switch (n)
                {
                    case 0://0个孩子
                        break;
                    case 1://1个孩子
                        datas = GetRamdomList(1).Split(' ');
                        while (MTreeHashTable.ContainsKey(datas[0]))
                        {
                            datas = GetRamdomList(1).Split(' ');
                        }
                        for (int i = 0; i < datas.Length; i++)
                        {
                            child = new MTreeNode(datas[i]);
                            MTreeHashTable.Add(child.Data, child);
                            father.Children.AddAChild(father, child);
                            #region 同步记录
                            initNode = new InitMTreeNode(datas[i]);
                            _initNodeHashtable.Add(initNode.Data, initNode);
                            fatherInitNode.Children.Add(initNode);
                            _initializedValues += " " + datas[i];
                            #endregion
                            _currentInitNodeNum++;
                            indexSorting.Add(i);
                        }
                        indexSorting = DisruptTheOrder(indexSorting);//打乱顺序的索引排序
                        foreach (int index in indexSorting)
                        {
                            RecursionRandom((MTreeNode)MTreeHashTable[datas[index]], (InitMTreeNode)_initNodeHashtable[datas[index]]);
                        }
                        break;
                    case 2: //2个孩子
                        datas = GetRamdomList(2).Split(' ');
                        while (MTreeHashTable.ContainsKey(datas[0]) || MTreeHashTable.ContainsKey(datas[1]))
                        {
                            datas = GetRamdomList(2).Split(' ');
                        }
                        for (int i = 0; i < datas.Length; i++)
                        {
                            child = new MTreeNode(datas[i]);
                            MTreeHashTable.Add(child.Data, child);
                            father.Children.AddAChild(father, child);
                            #region 同步记录
                            initNode = new InitMTreeNode(datas[i]);
                            _initNodeHashtable.Add(initNode.Data, initNode);
                            fatherInitNode.Children.Add(initNode);
                            _initializedValues += " " + datas[i];
                            #endregion
                            _currentInitNodeNum++;
                            indexSorting.Add(i);
                        }
                        indexSorting = DisruptTheOrder(indexSorting);//打乱顺序的索引排序
                        foreach (int index in indexSorting)
                        {
                            RecursionRandom((MTreeNode)MTreeHashTable[datas[index]], (InitMTreeNode)_initNodeHashtable[datas[index]]);
                        }
                        break;
                    case 3://3个孩子
                        datas = GetRamdomList(3).Split(' ');
                        while (MTreeHashTable.ContainsKey(datas[0]) || MTreeHashTable.ContainsKey(datas[1]) || MTreeHashTable.ContainsKey(datas[2]))
                        {
                            datas = GetRamdomList(3).Split(' ');
                        }
                        for (int i = 0; i < datas.Length; i++)
                        {
                            child = new MTreeNode(datas[i]);
                            MTreeHashTable.Add(child.Data, child);
                            father.Children.AddAChild(father, child);
                            #region 同步记录
                            initNode = new InitMTreeNode(datas[i]);
                            _initNodeHashtable.Add(initNode.Data, initNode);
                            fatherInitNode.Children.Add(initNode);
                            _initializedValues += " " + datas[i];
                            #endregion
                            _currentInitNodeNum++;
                            indexSorting.Add(i);
                        }
                        indexSorting = DisruptTheOrder(indexSorting);//打乱顺序的索引排序
                        foreach (int index in indexSorting)
                        {
                            RecursionRandom((MTreeNode)MTreeHashTable[datas[index]], (InitMTreeNode)_initNodeHashtable[datas[index]]);
                        }
                        break;
                    case 4: //4个孩子
                        datas = GetRamdomList(4).Split(' ');
                        while (MTreeHashTable.ContainsKey(datas[0]) || MTreeHashTable.ContainsKey(datas[1]) || MTreeHashTable.ContainsKey(datas[2]) || MTreeHashTable.ContainsKey(datas[3]))
                        {
                            datas = GetRamdomList(4).Split(' ');
                        }
                        for (int i = 0; i < datas.Length; i++)
                        {
                            child = new MTreeNode(datas[i]);
                            MTreeHashTable.Add(child.Data, child);
                            father.Children.AddAChild(father, child);
                            #region 同步记录
                            initNode = new InitMTreeNode(datas[i]);
                            _initNodeHashtable.Add(initNode.Data, initNode);
                            fatherInitNode.Children.Add(initNode);
                            _initializedValues += " " + datas[i];
                            #endregion
                            _currentInitNodeNum++;
                            indexSorting.Add(i);
                        }
                        indexSorting = DisruptTheOrder(indexSorting);//打乱顺序的索引排序
                        foreach (int index in indexSorting)
                        {
                            RecursionRandom((MTreeNode)MTreeHashTable[datas[index]], (InitMTreeNode)_initNodeHashtable[datas[index]]);
                        }
                        break;
                    case 5://5个孩子
                        datas = GetRamdomList(5).Split(' ');
                        while (MTreeHashTable.ContainsKey(datas[0]) || MTreeHashTable.ContainsKey(datas[1]) || MTreeHashTable.ContainsKey(datas[2]) || MTreeHashTable.ContainsKey(datas[3]) || MTreeHashTable.ContainsKey(datas[4]))
                        {
                            datas = GetRamdomList(5).Split(' ');
                        }
                        for (int i = 0; i < datas.Length; i++)
                        {
                            child = new MTreeNode(datas[i]);
                            MTreeHashTable.Add(child.Data, child);
                            father.Children.AddAChild(father, child);
                            #region 同步记录
                            initNode = new InitMTreeNode(datas[i]);
                            _initNodeHashtable.Add(initNode.Data, initNode);
                            fatherInitNode.Children.Add(initNode);
                            _initializedValues += " " + datas[i];
                            #endregion
                            _currentInitNodeNum++;
                            indexSorting.Add(i);
                        }
                        indexSorting = DisruptTheOrder(indexSorting);//打乱顺序的索引排序
                        foreach (int index in indexSorting)
                        {
                            RecursionRandom((MTreeNode)MTreeHashTable[datas[index]], (InitMTreeNode)_initNodeHashtable[datas[index]]);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 插入孩子操作
        /// </summary>
        /// <param name="targetValue">目标节点</param>
        /// <param name="index">孩子位置</param>
        /// <param name="childData">孩子节点值</param>
        /// <param name="singleAnimotionLog">0:单步；1：动画；2日志操作</param>
        private void ChildInsert(string targetValue, int index, string childData, int singleAnimotionLog)
        {
            if (singleAnimotionLog == 1)
            {
                _fatherData = targetValue;
                _childIndex = index;
                _childData = childData;
                _childInsertTimer.Start();
            }
            else if (singleAnimotionLog == 2)
            {
                MTreeNode father = (MTreeNode)MTreeHashTable[targetValue];
                MTreeNode child = new MTreeNode(childData);
                MTreeHashTable.Add(childData, child);
                if (index > father.NChildren)
                {
                    index = father.NChildren;
                }
                if (index >= 0 && index < father.NChildren)
                {//加这一段是因为Graphviz自身存在的布局算法中的一些缺陷
                    //也就是节点位置的添加每次只能以追加的形式加入到孩子列表的末尾
                    _slist = new List<List<string>>();
                    List<List<string>> allLaterChildren = PreOrder(father, index);
                    allLaterChildren.RemoveAt(0);//截取掉allLaterChildren链表中的首节点
                    foreach (var vertex in allLaterChildren)
                    {
                        Vm.RemoveNode(vertex[0], true);
                    }
                    Vm.AddVertex(child.Data);
                    father.Children.InsertAChild(father, index, child);//添加一条边
                    AddLaterSubtree(father, _childIndex + 1);
                }
                else
                {
                    father.Children.AddAChild(father, child);
                }
            }
        }



        /// <summary>
        /// 遍历删除
        /// </summary>
        /// <param name="selNode"></param>
        private void StartRemove(string selNode)
        {
            _traversalString = new List<string>();
            _removePostOrderString = GetPostOrderString((MTreeNode)MTreeHashTable[selNode]);
            _removeTimer.Start();
        }

        /// <summary>
        /// 用先序深度遍历来实现回退操作中的初始化操作
        /// </summary>
        /// <param name="initNodeFather"></param>
        /// <returns></returns>
        private void PreOrderInit(InitMTreeNode initNodeFather)
        {
            if (initNodeFather != null)
            {
                foreach (InitMTreeNode iniNode in initNodeFather.Children)
                {
                    MTreeNode childMTreeNode = new MTreeNode(iniNode.Data);
                    MTreeHashTable.Add(childMTreeNode.Data, childMTreeNode);
                    MTreeNode father = (MTreeNode)MTreeHashTable[initNodeFather.Data];
                    father.Children.AddAChild(father, childMTreeNode);
                    PreOrderInit(iniNode);//只有第一次是从childIndex的位置开始的，以后每次都是从头开始
                }
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
                MTreeNode node = (MTreeNode)MTreeHashTable[value[0]];
                if (node.Parent == null) continue;
                var edge = node.Parent.Data + " " + node.Data;
                edges.Add(edge);
            }
            return edges;
        }
        #endregion

        #region 数据结构对外界所提供的函数
        /// <summary>
        /// 添加根节点
        /// </summary>
        /// <param name="data"></param>
        public void AddRoot(string data)
        {
            if (data.Equals("")) return;
            if (Head != null)
            {
                throw new Exception("根节点已存在！");
            }
            Head = new MTreeNode(data);
            Vm.Graph.RaiseChangedByTheCustom();
            MTreeHashTable.Add(data, Head);
            LogId++;
            MTreeLogs.Add(LogId + "", new LogVo(LogId, "创建根节点", "", data));
        }

        /// <summary>
        /// 返回多叉树的节点数量
        /// </summary>
        /// <returns></returns>
        public int Size()
        {
            return MTreeHashTable.Count;
        }

        /// <summary>
        /// 多叉树的清除操作
        /// </summary>
        public void Clear()
        {
            Vm.Graph.Clear(true);
            Head = null;
            MTreeHashTable.Clear();
            if (_initNodeHashtable != null)
                _initNodeHashtable.Clear();
        }

        #region 随机生成多叉树
        private int _initLength;//初始长度
        private int _currentInitNodeNum;//当前初始化的节点数量
        public void RandomInit()
        {
            _initLength = 0;
            _currentInitNodeNum = 0;
            if (Size() > 0)
            {
                Clear();
            }
            Random random = new Random();
            _initLength = random.Next(20, 101);
            string[] root = GetRamdomList(1).Split(' ');//随机生成根节点
            Head = new MTreeNode(root[0]);
            MTreeHashTable.Add(Head.Data, Head);
            #region 记录初始化的节点
            InitMTreeNode initHead = new InitMTreeNode(root[0]);
            _initMtreeHeads.Add(initHead);
            _initNodeHashtable.Add(initHead.Data, initHead);
            _initializedValues = root[0];
            #endregion
            _currentInitNodeNum++;
            RecursionRandom(Head, initHead);
            Vm.Graph.RaiseChangedByTheCustom();
            LogId++;
            MTreeLogs.Add(LogId + "", new LogVo(LogId, "初始化(" + _currentInitNodeNum + ")", "", _initializedValues));
        }
        #endregion 随机生成多叉树

        public void AddAChild(string targetValue, int index, string childData, GraphLayout graphLayout)
        {
            _graphLayout = graphLayout;
            if (!targetValue.Equals("") && !childData.Equals(""))
            {
                if (!MTreeHashTable.ContainsKey(targetValue))
                    throw new Exception("您选择的节点不存在");
                if (MTreeHashTable.ContainsKey(childData))
                    throw new Exception("节点内容重复");
                if (index < 0)
                    throw new Exception("孩子位置格式为非负整数");
                MTreeNode target = (MTreeNode)MTreeHashTable[targetValue];
                if (index > target.NChildren)
                {
                    if (MessageBox.Show("孩子位置超出索引范围,是否改为追加到最后?\n即：\"index=" + target.NChildren + "\";\n选择取消重新输入孩子位置", "孩子位置超出范围", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        ChildInsert(targetValue, target.NChildren, childData, 1);
                        index = target.NChildren;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                    ChildInsert(targetValue, index, childData, 1);
                LogId++;

                MTreeLogs.Add(LogId + "", new LogVo(LogId, "插入第" + (index + 1) + "个孩子", targetValue, childData));
            }
            else if (targetValue.Equals(""))
                throw new Exception("请选择要为其插入孩子的目标节点");
            else
                throw new Exception("请输入孩子节点的内容");
        }

        public void DeleteSelNode(string selNode)
        {
            if (string.IsNullOrEmpty(selNode))
            {
                throw new Exception("请输入要删除节点的data值");
            }
            if (!MTreeHashTable.ContainsKey(selNode))
            {
                throw new Exception("该节点不存在");
            }
            if (selNode.Equals(Head.Data))
            {
                if (MessageBox.Show("删除根节点将清空日志记录，继续吗？", "确认信息",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                    return;
            }
            //DoDelete(selNode);
            StartRemove(selNode);

            LogId++;
            Vm.Graph.RaiseChangedByTheCustom();
            if (LogId == 0)
            {
                MTreeLogs.Clear();
                return;
            }
            MTreeLogs.Add(LogId + "", new LogVo(LogId, "删除节点", "", selNode));
        }

        public void Traversal(string theWayOfTraverse, GraphLayout graphLayout)
        {
            List<List<string>> list;
            List<string> edges;
            switch (theWayOfTraverse)
            {
                case "深度优先":
                    list = DepthFirstTraversal(Head);
                    edges = GetTranversalEdges(list);
                    Vm.ViewModelTraversal(edges, list, theWayOfTraverse, graphLayout);
                    break;
                case "广度优先":
                    list = BreadthFirstSearch(Head);
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
            Hashtable tMTreeLogs = new Hashtable();
            List<InitMTreeNode> tInitHeads = new List<InitMTreeNode>();
            int j = 0;//用来索引_initMtreeHeads链表中的头结点
            for (int i = 1; i <= int.Parse(step); i++)
            {
                LogVo logVo = (LogVo)MTreeLogs[i + ""];
                tMTreeLogs.Add(i + "", logVo);
                if (logVo.Action.Contains("初始化"))
                {
                    tInitHeads.Add(_initMtreeHeads[j]);
                    j++;
                }
            }
            _initMtreeHeads.Clear();
            foreach (InitMTreeNode head in tInitHeads)
            {
                _initMtreeHeads.Add(head);
            }
            Clear();
            j = 0;//用来索引日志中的“初始化”命令
            for (int i = 1; i <= int.Parse(step); i++)
            {
                LogVo tlogVo = (LogVo)MTreeLogs[i + ""];
                if (tlogVo.Action.Equals("创建根节点"))
                {
                    Head = new MTreeNode(tlogVo.Data);
                    MTreeHashTable.Add(tlogVo.Data, Head);
                }
                else if (tlogVo.Action.Contains("插入第"))//插入第i个孩子
                {
                    int index = int.Parse(Regex.Replace(tlogVo.Action, @"[\u4E00-\u9FA5]*", ""));//得到日志中的数字
                    ChildInsert(tlogVo.Selectdata, index, tlogVo.Data, 2);
                }
                else if (tlogVo.Action.Contains("初始化"))
                {
                    if (j < _initMtreeHeads.Count)
                        Clear();
                    Head = new MTreeNode(_initMtreeHeads[j].Data);
                    MTreeHashTable.Add(_initMtreeHeads[j].Data, Head);
                    PreOrderInit(_initMtreeHeads[j]);
                    j++;
                }
            }
            LogId = int.Parse(step);
            //Treelogs = temp;
            MTreeLogs = tMTreeLogs;
            Vm.Graph.RaiseChangedByTheCustom();
        }

        public void RefreshCanvas()
        {
            Vm.AddVertex(" ");//人工加入一个点，强行触发RaiseChanged事件
            Vm.RemoveNode(" ", true);
            Vm.Graph.RaiseChangedByTheCustom();
        }
        #endregion 数据结构对外界所提供的函数
    }

}
