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
    public class AvlTreeNode
    {
        private int _data;                  // 节点数据
        private AvlTreeNode _left;          // 左子女
        private AvlTreeNode _right;         // 右子女
        private int _bf;

        /// <summary>
        /// 构造函数的重载
        /// </summary>
        /// <param name="data">节点值</param>
        public AvlTreeNode(int data)
        {
            Data = data;
        }

        /// <summary>
        /// 构造函数的重载
        /// </summary>
        /// <param name="data">节点值</param>
        /// <param name="isFlag">是否同步Grapviz</param>
        public AvlTreeNode(int data, bool isFlag)
        {
            if (isFlag)
            {//同步Graphviz
                Data = data;
            }
            else
            {//不同步
                _data = data;
            }
        }

        /// <summary>
        /// Data访问器
        /// </summary>
        public int Data
        {
            get { return _data; }
            set
            {
                _data = value;
                AvlTree.Vm.AddVertex(value.ToString());
            }
        }

        /// <summary>
        ///  左子女访问器
        /// </summary>
        public AvlTreeNode Left
        {
            get { return _left; }
            set
            {
                if (value != null)
                {

                    _left = value;
                    AvlTree.Vm.AddAdge(Data.ToString(), value.Data.ToString(), "L");
                }
                else
                {
                    _left = null;
                }
            }
        }

        /// <summary>
        /// 右子女访问器
        /// </summary>
        public AvlTreeNode Right
        {
            get
            {
                return _right;
            }
            set
            {
                if (value != null)
                {
                    _right = value;
                    AvlTree.Vm.AddAdge(Data.ToString(), value.Data.ToString(), "R");
                }
                else
                    _right = null;
            }
        }

        /// <summary>
        /// 父节点访问器
        /// </summary>
        public AvlTreeNode Parent { get; set; }

        public int Bf
        {
            get { return _bf; }
            set
            {
                _bf = value;
                AvlTree.Vm.SetBf(_data, value);
            }
        }

        public void SetData(int data)
        {
            _data = data;
        }

        public void SetLeft(AvlTreeNode left)
        {
            _left = left;
        }

        public void SetRight(AvlTreeNode right)
        {
            _right = right;
        }

        public void SetBf(int bf)
        {
            _bf = bf;
        }
    }

    /// <summary>
    /// 简化版的节点，用来做暂时性的快速存取用的
    /// </summary>
    struct AvlNodeStruct
    {
        public int Value;
        public int Bf;
        public AvlNodeStruct(int value, int bf)
        {
            Value = value;
            Bf = bf;
        }
    }

    public class AvlTree
    {
        //成员变量
        public static ViewModel Vm = new ViewModel();
        private GraphLayout _graphLayout;
        private AvlTreeNode _head;//头指针

        private readonly AvlTreeNode[] _path = new AvlTreeNode[32];//记录访问路径上的结点
        private int _p;//表示当前访问到的结点在_path上的索引

        public int LogId { get; set; }
        public Hashtable BTreeLogs = new Hashtable();
        private readonly Hashtable _avlTreeHashTable = new Hashtable();

        //计时器相关
        private readonly DispatcherTimer _addTimer = new DispatcherTimer();
        private readonly DispatcherTimer _removeTimer = new DispatcherTimer();
        private DispatcherTimer _timer;
        private int _tick;
        private int _value;//当前操作的节点值

        public AvlTree(GraphLayout graphLayout)
        {
            _graphLayout = graphLayout;
            _addTimer.Tick += AddTimer_Tick;
            _addTimer.Interval = new TimeSpan(0, 0, 1);
            _removeTimer.Tick += RemoveTimer_Tick;
            _removeTimer.Interval = new TimeSpan(0, 0, 1);
        }

        /// <summary>
        /// 头指针访问器
        /// </summary>
        AvlTreeNode Head
        {
            get { return _head; }
        }

        #region 平衡二叉树的基本操作
        private List<AvlNodeStruct> _nslist;
        /// <summary>
        /// 先序(用于往存在右孩子的父节点下插入左孩子的情况)，最好用StartPreOrder
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns></returns>
        private List<AvlNodeStruct> PreOrder(AvlTreeNode node)
        {
            if (node != null)
            {
                _nslist.Add(new AvlNodeStruct(node.Data, node.Bf));
                PreOrder(node.Left);
                PreOrder(node.Right);
            }
            return _nslist;
        }

        /// <summary>
        /// 对先序的封装
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns>删除成功与否</returns>
        private List<AvlNodeStruct> StartPreOrder(AvlTreeNode node)
        {
            _nslist = new List<AvlNodeStruct>();
            List<AvlNodeStruct> allChidren = PreOrder(node);
            return allChidren;
        }

        /// <summary>
        /// //添加一个元素
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool Add(int value)
        {
            //如果是空树，则新结点成为二叉排序树的根
            if (_head == null)
            {
                _head = new AvlTreeNode(value) { Bf = 0 };
                return true;
            }
            #region 完成Path的建立
            _p = 0;
            AvlTreeNode prev = null;//prev为上一次访问的结点
            AvlTreeNode current = _head;//current为当前访问结点
            while (current != null)
            {
                _path[_p++] = current; //将路径上的结点插入数组
                //如果插入值已存在，则插入失败
                if (current.Data == value)
                {
                    return false;
                }
                prev = current;
                //当插入值小于当前结点，则继续访问左子树，否则访问右子树
                current = (value < prev.Data) ? prev.Left : prev.Right;
            }
            current = new AvlTreeNode(value) { Bf = 0 }; //创建新结点
            if (value < prev.Data) //如果插入值小于双亲结点的值
            {
                prev.Left = current; //成为左孩子
            }
            else //如果插入值大于双亲结点的值
            {
                prev.Right = current; //成为右孩子
            }
            _path[_p] = current; //将新元素插入数组path的最后
            #endregion 完成Path的建立

            #region 修改插入点至根结点路径上各结点的平衡因子
            while (_p > 0)
            {   //bf表示平衡因子的改变量，当新结点插入左子树，则平衡因子+1
                //当新结点插入右子树，则平衡因子-1
                var bf = (value < _path[_p - 1].Data) ? 1 : -1;
                _path[--_p].Bf += bf; //改变当前父结点的平衡因子
                bf = _path[_p].Bf; //获取当前结点的平衡因子
                //判断当前结点平衡因子，如果为0表示该子树已平衡，不需再回溯
                //而改变祖先结点平衡因子，此时添加成功，直接返回
                if (bf == 0)
                {
                    return true;
                }
                else if (bf == 2 || bf == -2) //需要旋转的情况
                {
                    RotateSubTree(bf);
                    return true;
                }
            }
            #endregion 修改插入点至根结点路径上各结点的平衡因子
            return true;
        }

        /// <summary>
        /// 删除指定值
        /// </summary>
        /// <param name="value">当前节点</param>
        /// <returns>删除成功与否</returns>
        private bool Remove(int value)
        {
            #region 完成Path的建立,找到节点就删除
            _p = -1;
            //parent表示双亲结点，node表示当前结点
            AvlTreeNode node = _head;
            //寻找指定值所在的结点
            while (node != null)
            {
                _path[++_p] = node;
                //如果找到，则调用RemoveNode方法删除结点
                if (value == node.Data)
                {
                    RemoveNode(node);//现在_p指向被删除结点
                    return true; //返回true表示删除成功
                }
                node = value < node.Data ? node.Left : node.Right;//否则继续查找
            }
            #endregion 完成Path的建立
            return false; //返回false表示删除失败
        }

        // 删除指定结点
        private void RemoveNode(AvlTreeNode node)
        {
            AvlTreeNode tmp;
            if (node.Left != null && node.Right != null)
            {//当被删除结点存在左右子树时
                tmp = node.Left; //获取左子树
                _path[++_p] = tmp;
                while (tmp.Right != null) //获取node的中序遍历前驱结点，并存放于tmp中
                {   //找到左子树中的最右下结点
                    tmp = tmp.Right;
                    _path[++_p] = tmp;
                }
                node.SetData(tmp.Data);//用中序遍历前驱结点的值代替被删除结点的值
                //以下相当于删除点
                if (_path[_p - 1] == node)
                {
                    _path[_p - 1].SetLeft(tmp.Left);
                }
                else
                {
                    _path[_p - 1].SetRight(tmp.Left);
                }
            }
            else //当只有左子树或右子树或为叶子结点时
            {   //首先找到惟一的孩子结点
                tmp = node.Left;
                if (tmp == null) //如果只有右孩子或没孩子
                {
                    tmp = node.Right;
                }
                if (_p > 0)//当删除的不是根结点时
                {
                    if (_path[_p - 1].Left == node)
                    {   //如果被删结点是左孩子
                        _path[_p - 1].SetLeft(tmp);
                    }
                    else
                    {   //如果被删结点是右孩子
                        _path[_p - 1].SetRight(tmp);
                    }
                }
                else  //当删除的是根结点时
                {
                    _head = tmp;
                }
            }
            //删除完后进行旋转，现在p指向实际被删除的结点
            int data = node.Data;
            while (_p > 0)
            {   //bf表示平衡因子的改变量，当删除的是左子树中的结点时，平衡因子-1
                //当删除的是右子树的孩子时，平衡因子+1
                int bf = (data <= _path[_p - 1].Data) ? -1 : 1;
                _path[_p - 1].SetBf(_path[_p - 1].Bf + bf);//改变当前父结点的平衡因子
                --_p;
                bf = _path[_p].Bf; //获取当前结点的平衡因子
                if (bf != 0) //如果bf==0，表明高度降低，继续向上回溯
                {
                    //如果bf为1或-1则说明高度未变，停止回溯，如果为2或-2，则进行旋转
                    //当旋转后高度不变，则停止回溯
                    if (bf == 1 || bf == -1 || !RotateSubTree(bf))
                    {
                        break;
                    }
                }
            }
        }

        //旋转以root为根的子树，当高度改变，则返回true；高度未变则返回false
        private bool RotateSubTree(int bf)
        {
            bool tallChanged = true;
            AvlTreeNode root = _path[_p]; //这里的root指的是最小不平衡子树的根节点
            AvlTreeNode newRoot = null;
            if (bf == 2) //当平衡因子为2时需要进行旋转操作
            {
                int leftBf = root.Left.Bf;
                if (leftBf == -1) //LR型旋转
                {
                    newRoot = Lr(root);
                }
                else if (leftBf == 1)
                {
                    newRoot = Ll(root); //LL型旋转
                }
                else //当旋转根左孩子的bf为0时，只有删除时才会出现
                {
                    newRoot = Ll(root);
                    tallChanged = false;
                }
            }
            if (bf == -2) //当平衡因子为-2时需要进行旋转操作
            {
                int rightBf = root.Right.Bf; //获取旋转根右孩子的平衡因子
                if (rightBf == 1)
                {
                    newRoot = Rl(root); //RL型旋转
                }
                else if (rightBf == -1)
                {
                    newRoot = Rr(root); //RR型旋转
                }
                else //当旋转根左孩子的bf为0时，只有删除时才会出现
                {
                    newRoot = Rr(root);
                    tallChanged = false;
                }
            }

            //更改新的子树根
            if (_p > 0)
            {
                if (root.Data < _path[_p - 1].Data)
                {
                    _path[_p - 1].SetLeft(newRoot);
                }
                else
                {
                    _path[_p - 1].SetRight(newRoot);

                }
            }
            else
            {
                _head = newRoot; //如果旋转根为AVL树的根，则指定新AVL树根结点
            }
            return tallChanged;
        }
        //root为旋转根，rootPrev为旋转根双亲结点
        private AvlTreeNode Ll(AvlTreeNode root) //LL型旋转，返回旋转后的新子树根
        {
            AvlTreeNode rootNext = root.Left;
            rootNext.Parent = root.Parent;
            root.SetLeft(rootNext.Right);
            if (rootNext.Right != null)
                rootNext.Right.Parent = root;
            rootNext.SetRight(root);
            root.Parent = rootNext;
            if (rootNext.Bf == 1)
            {
                root.SetBf(0);
                rootNext.SetBf(0);
            }
            else //rootNext.BF==0的情况，删除时用
            {
                root.SetBf(1);
                rootNext.SetBf(-1);
            }
            return rootNext; //rootNext为新子树的根
        }
        private AvlTreeNode Lr(AvlTreeNode root) //LR型旋转，返回旋转后的新子树根
        {
            AvlTreeNode rootNext = root.Left;
            AvlTreeNode newRoot = rootNext.Right;
            newRoot.Parent = root.Parent;
            root.SetLeft(newRoot.Right);
            if (newRoot.Right != null)
                newRoot.Right.Parent = root;
            rootNext.SetRight(newRoot.Left);
            if (newRoot.Left != null)
                newRoot.Left.Parent = rootNext;
            newRoot.SetLeft(rootNext);
            rootNext.Parent = newRoot;
            newRoot.SetRight(root);
            root.Parent = newRoot;
            switch (newRoot.Bf) //改变平衡因子
            {
                case 0:
                    root.SetBf(0);
                    rootNext.SetBf(0);
                    break;
                case 1:
                    root.SetBf(-1);
                    rootNext.SetBf(0);
                    break;
                case -1:
                    root.SetBf(0);
                    rootNext.SetBf(1);
                    break;
            }
            newRoot.SetBf(0);
            return newRoot; //newRoot为新子树的根
        }
        private AvlTreeNode Rr(AvlTreeNode root) //RR型旋转，返回旋转后的新子树根
        {
            AvlTreeNode rootNext = root.Right;
            rootNext.Parent = root.Parent;
            root.SetRight(rootNext.Left);
            if (rootNext.Left != null)
                rootNext.Left.Parent = root;
            rootNext.SetLeft(root);
            root.Parent = rootNext;
            if (rootNext.Bf == -1)
            {
                root.SetBf(0);
                rootNext.SetBf(0);
            }
            else //rootNext.BF==0的情况，删除时用
            {
                root.SetBf(-1);
                rootNext.SetBf(1);
            }
            return rootNext; //rootNext为新子树的根
        }
        private AvlTreeNode Rl(AvlTreeNode root) //RL型旋转，返回旋转后的新子树根
        {
            AvlTreeNode rootNext = root.Right;
            AvlTreeNode newRoot = rootNext.Left;
            newRoot.Parent = root.Parent;
            root.SetRight(newRoot.Left);
            if (newRoot.Left != null)
                newRoot.Left.Parent = root;
            rootNext.SetLeft(newRoot.Right);
            if (newRoot.Right != null)
                newRoot.Right.Parent = rootNext;
            newRoot.SetRight(rootNext);
            rootNext.Parent = newRoot;
            newRoot.SetLeft(root);
            root.Parent = newRoot;
            switch (newRoot.Bf) //改变平衡因子
            {
                case 0:
                    root.SetBf(0);
                    rootNext.SetBf(0);
                    break;
                case 1:
                    root.SetBf(0);
                    rootNext.SetBf(-1);
                    break;
                case -1:
                    root.SetBf(1);
                    rootNext.SetBf(0);
                    break;
            }
            newRoot.SetBf(0);
            return newRoot; //newRoot为新子树的根
        }
        #endregion 平衡二叉树的基本操作

        #region 内部操作函数
        private int StartAdd(int data, bool isStepThrough)
        {
            int step = -1;//默认是-1表示默认是动画演示
            _value = data;
            if (isStepThrough)
            {
                step = AddStepByStep();
            }
            else
            {
                _addTimer.Start();
            }
            return step;
        }

        private int StartRemove(int data, bool isStepThrough)
        {
            int step = -1;//默认是-1表示默认是动画演示
            _value = data;
            if (isStepThrough)
            {
                step = RemoveStepByStep();
            }
            else
            {
                _removeTimer.Start();
            }
            return step;
        }

        /// <summary>
        /// 添加ViewModel子树
        /// 1、由于左插计时器，避免Graphviz固有的先左后右添加顺序
        /// 2、同时也用于旋转后根据根节点进行Graphviz节点的绘制
        /// </summary>
        /// <param name="avlTreeNode">当前节点</param>
        private void AddVmSubtree(AvlTreeNode avlTreeNode)
        {
            if (avlTreeNode != null)
            {
                Vm.AddVertex(avlTreeNode.Data.ToString());
                Vm.AddAdge(avlTreeNode.Parent.Data.ToString(), avlTreeNode.Data.ToString(), avlTreeNode == avlTreeNode.Parent.Left ? "L" : "R");
                Vm.SetBf(avlTreeNode.Data, avlTreeNode.Bf);
                AddVmSubtree(avlTreeNode.Left);
                AddVmSubtree(avlTreeNode.Right);
            }
        }

        /// <summary>
        /// 根据根节点画Graphviz树
        /// </summary>
        /// <param name="avlTreeNode">当前节点</param>
        private void DrawVmTree(AvlTreeNode avlTreeNode)
        {
            if (avlTreeNode != null && avlTreeNode == _head)
            {
                Vm.AddVertex(avlTreeNode.Data.ToString());
                Vm.SetBf(avlTreeNode.Data, avlTreeNode.Bf);
                DrawVmTree(avlTreeNode.Left);
                DrawVmTree(avlTreeNode.Right);
            }
            else if (avlTreeNode != null)
            {
                Vm.AddVertex(avlTreeNode.Data.ToString());
                Vm.AddAdge(avlTreeNode.Parent.Data.ToString(), avlTreeNode.Data.ToString(), avlTreeNode == avlTreeNode.Parent.Left ? "L" : "R");
                Vm.SetBf(avlTreeNode.Data, avlTreeNode.Bf);
                DrawVmTree(avlTreeNode.Left);
                DrawVmTree(avlTreeNode.Right);
            }
        }

        private List<List<string>> GetPathString(AvlTreeNode[] path, int length)
        {
            List<List<string>> pathString = new List<List<string>>();
            for (int i = 0; i < length; i++)
            {
                pathString.Add(new List<string> { path[i].Data.ToString() });
            }

            return pathString;
        }

        private List<string> GetPathEdges(List<List<string>> list)
        {
            List<string> edges = new List<string>();
            foreach (var value in list)
            {
                AvlTreeNode node = (AvlTreeNode)_avlTreeHashTable[int.Parse(value[0])];
                if (node.Parent == null) continue;
                var edge = node.Parent.Data + " " + node.Data;
                edges.Add(edge);
            }
            return edges;
        }

        #region Graphviz层重画
        private void GraphvizReDraw()
        {
            Vm.Graph.Clear(true);
            if (Head != null)
            {
                Vm.AddVertex(Head.Data.ToString());
            }
            RecurGraphivizReDraw(Head);
        }

        private void RecurGraphivizReDraw(AvlTreeNode node)
        {
            if (node.Left != null)
            {
                Vm.AddVertex(node.Left.Data.ToString());
                Vm.AddAdge(node.Data.ToString(), node.Left.Data.ToString());
                RecurGraphivizReDraw(node.Left);
            }
            if (node.Right != null)
            {
                Vm.AddVertex(node.Right.Data.ToString());
                Vm.AddAdge(node.Data.ToString(), node.Right.Data.ToString());
                RecurGraphivizReDraw(node.Right);
            }
        }
        #endregion Graphviz层重画

        #endregion 内部操作函数

        #region 计时委托或与其同等级的实现函数
        private void InitAdd(int value)
        {
            //如果是空树，则新结点成为二叉排序树的根
            if (_head == null)
            {
                _head = new AvlTreeNode(value, false);
                _head.SetBf(0);
                _avlTreeHashTable.Add(value, _head);
                return;
            }
            #region 完成Path的建立
            _p = 0;
            AvlTreeNode prev = null;//prev为上一次访问的结点
            AvlTreeNode current = _head;//current为当前访问结点
            while (current != null)
            {
                _path[_p++] = current; //将路径上的结点插入数组
                //如果插入值已存在，则插入失败
                if (current.Data == value)
                {
                    throw new Exception("插入值已存在");
                }
                prev = current;
                //当插入值小于当前结点，则继续访问左子树，否则访问右子树
                current = (value < prev.Data) ? prev.Left : prev.Right;
            }
            current = new AvlTreeNode(value, false); //创建新结点
            current.SetBf(0);
            _avlTreeHashTable.Add(value, current);

            if (value < prev.Data) //如果插入值小于双亲结点的值
            {
                prev.SetLeft(current);//成为左孩子
                current.Parent = prev;
            }
            else //如果插入值大于双亲结点的值
            {
                prev.SetRight(current);//成为右孩子
                current.Parent = prev;
            }
            _path[_p] = current; //将新元素插入数组path的最后
            #endregion 完成Path的建立

            #region 修改插入点至根结点路径上各结点的平衡因子
            while (_p > 0)
            {   //bf表示平衡因子的改变量，当新结点插入左子树，则平衡因子+1
                //当新结点插入右子树，则平衡因子-1
                var bf = (value < _path[_p - 1].Data) ? 1 : -1;
                _path[_p - 1].SetBf(_path[_p - 1].Bf + bf); //改变当前父结点的平衡因子
                _p--;
                bf = _path[_p].Bf; //获取当前结点的平衡因子
                //判断当前结点平衡因子，如果为0表示该子树已平衡，不需再回溯
                //而改变祖先结点平衡因子，此时添加成功，直接返回
                if (bf == 0)
                {
                    break;
                }
                if (bf == 2 || bf == -2) //需要旋转的情况
                {
                    RotateSubTree(bf);
                    break;
                }
            }
            #endregion 修改插入点至根结点路径上各结点的平衡因子
        }


        int _tbf;


        /// <summary>
        /// 添加节点的计时器
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void AddTimer_Tick(object sender, EventArgs e)
        {
            switch (_tick)
            {
                case 0://完成Path的建立,并更新Parh路径上节点的Bf值
                    _p = 0;
                    AvlTreeNode prev = null;//prev为上一次访问的结点
                    AvlTreeNode current = _head;//current为当前访问结点
                    while (current != null)
                    {
                        _path[_p++] = current; //将路径上的结点插入数组
                        prev = current;
                        //当插入值小于当前结点，则继续访问左子树，否则访问右子树
                        current = (_value < prev.Data) ? prev.Left : prev.Right;
                    }
                    current = new AvlTreeNode(_value) { Bf = 0 }; //创建新结点
                    _avlTreeHashTable.Add(_value, current);
                    if (_value < prev.Data) //如果插入值小于双亲结点的值
                    {
                        if (prev.Right != null)
                        {
                            Vm.RemoveNode(current.Data.ToString(), true);
                            List<AvlNodeStruct> allRightChidren = StartPreOrder(prev.Right);
                            foreach (var vertex in allRightChidren)
                            {
                                Vm.RemoveNode(vertex.Value.ToString(), true);
                            }
                            Vm.AddVertex(current.Data.ToString(), current.Bf, true);
                            prev.Left = current; //添加一条边
                            current.Parent = prev;
                            AddVmSubtree(prev.Right);
                        }
                        else
                        {
                            prev.Left = current; //成为左孩子
                            current.Parent = prev;
                        }
                    }
                    else //如果插入值大于双亲结点的值
                    {
                        prev.Right = current; //成为右孩子
                        current.Parent = prev;
                    }
                    _path[_p] = current; //将新元素插入数组path的最后
                    while (_p > 0)
                    {   //bf表示平衡因子的改变量，当新结点插入左子树，则平衡因子+1
                        //当新结点插入右子树，则平衡因子-1
                        _tbf = (_value < _path[_p - 1].Data) ? 1 : -1;
                        _path[--_p].Bf += _tbf; //改变当前父结点的平衡因子
                        _tbf = _path[_p].Bf; //获取当前结点的平衡因子
                        //判断当前结点平衡因子，如果为0表示该子树已平衡，不需再回溯
                        //而改变祖先结点平衡因子，此时添加成功，直接返回
                        if (_tbf == 0)
                        {
                            _addTimer.Stop();
                            _tick = -1;
                            _value = 0;
                            break;
                        }
                        if (_tbf == 2 || _tbf == -2) //需要旋转的情况
                        {
                            AvlTreeNode pNode = _path[_p].Parent;
                            while (pNode != null)
                            {
                                pNode.Bf += (_value < pNode.Data) ? 1 : -1;
                                pNode = pNode.Parent;
                            }
                            break;
                        }
                    }
                    if (_tbf == 1 || _tbf == -1) //path到顶的时候，如果当前节点tbf是1或-1则直接退出
                    {
                        _addTimer.Stop();
                        _tick = -1;
                        _value = 0;
                    }
                    Vm.Graph.RaiseChangedByTheCustom();
                    _tick++;
                    break;
                case 1://修改插入点至根结点路径上各结点的平衡因子
                    if (_tbf == 2 || _tbf == -2) //需要旋转的情况
                    {
                        #region 复位操作：root以上节点的Bf值的复位
                        AvlTreeNode pNode = _path[_p].Parent;
                        while (pNode != null)
                        {
                            pNode.Bf += (_value < pNode.Data) ? -1 : +1;
                            pNode = pNode.Parent;
                        }
                        #endregion 复位操作
                        #region ViewMode删除：删除除最小不平衡子树的根节点以外的所有节点
                        //不直接删除最小不平衡子树的根节点是因为避免左插入的时候再重新做删除操作
                        if (_path[_p].Left != null)
                        {
                            List<AvlNodeStruct> allLeftChidren = StartPreOrder(_path[_p].Left);
                            foreach (var vertex in allLeftChidren)
                            {
                                Vm.RemoveNode(vertex.Value.ToString(), true);
                            }
                        }
                        if (_path[_p].Right != null)
                        {
                            List<AvlNodeStruct> allRightChidren = StartPreOrder(_path[_p].Right);
                            foreach (var vertex in allRightChidren)
                            {
                                Vm.RemoveNode(vertex.Value.ToString(), true);
                            }
                        }
                        #endregion ViewMode删除
                        RotateSubTree(_tbf);//旋转
                        #region 根据旋转后的结果绘图
                        AvlTreeNode newRoot;
                        if (_p > 0)
                        {
                            newRoot = _path[_p].Data < _path[_p - 1].Data ? _path[_p - 1].Left : _path[_p - 1].Right;
                        }
                        else
                        {
                            newRoot = _head; //如果旋转根为AVL树的根，则指定新AVL树根结点
                        }
                        Vm.UpdateNode(_path[_p].Data.ToString(), newRoot.Data.ToString(), newRoot.Bf, -1);
                        if (newRoot.Left != null)
                            AddVmSubtree(newRoot.Left);
                        if (newRoot.Right != null)
                            AddVmSubtree(newRoot.Right);
                        #endregion 根据旋转后的结果绘图
                        Vm.Graph.RaiseChangedByTheCustom();
                        _addTimer.Stop();
                        _tick = 0;
                        _value = 0;
                    }
                    break;
            }
        }

        #region 单步添加
        private int _step;
        AvlTreeNode _prev;//_prev为上一次访问的结点
        AvlTreeNode _current;//_current为当前访问结点
        private DispatcherTimer _waitTimer;
        private int _howLong;//_waitTimer需要等待的时间

        public int AddStepByStep()
        {
            switch (_step)
            {
                case 0://寻找位置
                    _p = 0;
                    _prev = null;//prev为上一次访问的结点
                    _current = _head;//current为当前访问结点
                    while (_current != null)
                    {
                        _path[_p++] = _current; //将路径上的结点插入数组
                        _prev = _current;
                        //当插入值小于当前结点，则继续访问左子树，否则访问右子树
                        _current = (_value < _prev.Data) ? _prev.Left : _prev.Right;
                    }

                    List<List<string>> list = GetPathString(_path, _p);
                    List<string> edges = GetPathEdges(list);
                    if (edges.Count > 0)
                    {
                        Vm.ViewModelFindThePath(edges, list, "平衡二叉树", _graphLayout);

                        _howLong = edges.Count - 1;
                        _waitTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                        _waitTimer.Tick += WaitTimer_Tick;
                        _waitTimer.Start();
                        return 1;
                    }
                    else
                    {
                        DrawAdd();
                    }
                    break;
                case 1://添加
                    {
                        DrawAdd();
                    } break;
                case 2://LR与RL画小旋箭头 LLRR画大旋箭头 
                    {
                        if (_tbf == 2 || _tbf == -2) //需要旋转的情况
                        {
                            AvlTreeNode root = _path[_p]; //这里的root指的是最小不平衡子树的根节点
                            if (_tbf == 2) //当平衡因子为2时需要进行旋转操作
                            {
                                int leftBf = root.Left.Bf;
                                if (leftBf == -1) //LR型旋转
                                {
                                    Vm.AddRotateArrow(root.Left.Right.Data.ToString(), root.Left.Data.ToString(), root.Data.ToString(), "LR1", this._graphLayout);//LR
                                    _step++;
                                }
                                else if (leftBf == 1)
                                {
                                    Vm.AddRotateArrow(root.Left.Left.Data.ToString(), root.Left.Data.ToString(), root.Data.ToString(), "LL", this._graphLayout);//LL
                                    _step = 5;
                                }
                                else //当旋转根左孩子的bf为0时，只有删除时才会出现
                                {
                                    //LlArrow(root);
                                }
                            }
                            if (_tbf == -2) //当平衡因子为-2时需要进行旋转操作
                            {
                                int rightBf = root.Right.Bf; //获取旋转根右孩子的平衡因子
                                if (rightBf == 1)
                                {
                                    Vm.AddRotateArrow(root.Right.Left.Data.ToString(), root.Right.Data.ToString(), root.Data.ToString(), "RL1", this._graphLayout);//RL
                                    _step++;
                                }
                                else if (rightBf == -1)
                                {
                                    Vm.AddRotateArrow(root.Right.Right.Data.ToString(), root.Right.Data.ToString(), root.Data.ToString(), "RR", this._graphLayout);//RR
                                    _step = 5;
                                }
                                else //当旋转根左孩子的bf为0时，只有删除时才会出现
                                {
                                    //RrArrow(root);
                                }
                            }
                        }
                    }
                    break;
                case 3://LR与RL局部旋转
                    {
                        AvlTreeNode root = _path[_p]; //这里的root指的是最小不平衡子树的根节点
                        if (_tbf == 2) //当平衡因子为2时需要进行旋转操作
                        {
                            int leftBf = root.Left.Bf;
                            if (leftBf == -1) //LR型旋转
                            {
                                //Vm.AddRotateArrow(root.Left.Right.Data.ToString(), root.Left.Data.ToString(), root.Data.ToString(), "LR1", this._graphLayout);//LR
                                Vm.RemoveNode(root.Left.Data.ToString(), true);
                                Vm.RemoveNode(root.Left.Right.Data.ToString(), true);
                                Vm.AddVertex(root.Left.Right.Data.ToString(), 1, true);
                                Vm.AddAdge(root.Data.ToString(), root.Left.Right.Data.ToString(), "L");
                                Vm.AddVertex(root.Left.Data.ToString(), 0, true);
                                Vm.AddAdge(root.Left.Right.Data.ToString(), root.Left.Data.ToString(), "L");
                                Vm.Graph.RaiseChangedByTheCustom();
                            }
                        }
                        if (_tbf == -2) //当平衡因子为-2时需要进行旋转操作
                        {
                            int rightBf = root.Right.Bf; //获取旋转根右孩子的平衡因子
                            if (rightBf == 1)
                            {
                                //Vm.AddRotateArrow(root.Right.Left.Data.ToString(), root.Right.Data.ToString(), root.Data.ToString(), "RL1", this._graphLayout);//RL
                                Vm.RemoveNode(root.Right.Data.ToString(), true);
                                Vm.RemoveNode(root.Right.Left.Data.ToString(), true);
                                Vm.AddVertex(root.Right.Left.Data.ToString(), -1, true);
                                Vm.AddAdge(root.Data.ToString(), root.Right.Left.Data.ToString(), "R");
                                Vm.AddVertex(root.Right.Data.ToString(), 0, true);
                                Vm.AddAdge(root.Right.Left.Data.ToString(), root.Right.Data.ToString(), "R");
                                Vm.Graph.RaiseChangedByTheCustom();
                            }
                        }
                        _step++;
                    } break;

                case 4://LR与RL画大旋箭头
                    {
                        if (_tbf == 2 || _tbf == -2) //需要旋转的情况
                        {
                            AvlTreeNode root = _path[_p]; //这里的root指的是最小不平衡子树的根节点
                            if (_tbf == 2) //当平衡因子为2时需要进行旋转操作
                            {
                                int leftBf = root.Left.Bf;
                                if (leftBf == -1) //LR型旋转
                                {
                                    Vm.AddRotateArrow(root.Left.Data.ToString(), root.Left.Right.Data.ToString(), root.Data.ToString(), "LR2", this._graphLayout);//LR
                                    _step++;
                                }
                                else if (leftBf == 1)
                                {
                                    Vm.AddRotateArrow(root.Left.Left.Data.ToString(), root.Left.Data.ToString(), root.Data.ToString(), "LL", this._graphLayout);//LL
                                    _step = 5;
                                }
                                else //当旋转根左孩子的bf为0时，只有删除时才会出现
                                {
                                    //LlArrow(root);
                                }
                            }
                            if (_tbf == -2) //当平衡因子为-2时需要进行旋转操作
                            {
                                int rightBf = root.Right.Bf; //获取旋转根右孩子的平衡因子
                                if (rightBf == 1)
                                {
                                    Vm.AddRotateArrow(root.Right.Left.Data.ToString(), root.Right.Data.ToString(), root.Data.ToString(), "RL2", this._graphLayout);//RL
                                    _step++;
                                }
                                else if (rightBf == -1)
                                {
                                    Vm.AddRotateArrow(root.Right.Right.Data.ToString(), root.Right.Data.ToString(), root.Data.ToString(), "RR", this._graphLayout);//RR
                                    _step = 5;
                                }
                                else //当旋转根左孩子的bf为0时，只有删除时才会出现
                                {
                                    //RrArrow(root);
                                }
                            }
                        }
                    }
                    break;
                case 5:
                    {//旋转
                        if (_tbf == 2 || _tbf == -2) //需要旋转的情况
                        {
                            #region 复位操作：root以上节点的Bf值的复位
                            AvlTreeNode pNode = _path[_p].Parent;
                            while (pNode != null)
                            {
                                pNode.Bf += (_value < pNode.Data) ? -1 : +1;
                                pNode = pNode.Parent;
                            }
                            #endregion 复位操作
                            #region ViewMode删除：删除除最小不平衡子树的根节点以外的所有节点
                            //不直接删除最小不平衡子树的根节点是因为避免左插入的时候再重新做删除操作
                            if (_path[_p].Left != null)
                            {
                                List<AvlNodeStruct> allLeftChidren = StartPreOrder(_path[_p].Left);
                                foreach (var vertex in allLeftChidren)
                                {
                                    Vm.RemoveNode(vertex.Value.ToString(), true);
                                }
                            }
                            if (_path[_p].Right != null)
                            {
                                List<AvlNodeStruct> allRightChidren = StartPreOrder(_path[_p].Right);
                                foreach (var vertex in allRightChidren)
                                {
                                    Vm.RemoveNode(vertex.Value.ToString(), true);
                                }
                            }
                            #endregion ViewMode删除：删除除最小不平衡子树的根节点以外的所有节点
                            RotateSubTree(_tbf);//旋转
                            #region 根据旋转后的结果绘图
                            AvlTreeNode newRoot;
                            if (_p > 0)
                            {
                                newRoot = _path[_p].Data < _path[_p - 1].Data ? _path[_p - 1].Left : _path[_p - 1].Right;
                            }
                            else
                            {
                                newRoot = _head; //如果旋转根为AVL树的根，则指定新AVL树根结点
                            }
                            Vm.UpdateNode(_path[_p].Data.ToString(), newRoot.Data.ToString(), newRoot.Bf, -1);
                            if (newRoot.Left != null)
                                AddVmSubtree(newRoot.Left);
                            if (newRoot.Right != null)
                                AddVmSubtree(newRoot.Right);
                            #endregion 根据旋转后的结果绘图
                            Vm.Graph.RaiseChangedByTheCustom();
                            _step = 0;
                            _value = 0;
                        }
                    }
                    break;
            }
            return _step;
        }

        private int _i;//
        void WaitTimer_Tick(object sender, EventArgs e)
        {
            if (_i >= _howLong)
            {
                _i = 0;
                _step++;
                _waitTimer.Stop();
            }
            else
            {
                _i++;
            }
        }


        private void DrawAdd()
        {
            _current = new AvlTreeNode(_value) { Bf = 0 }; //创建新结点
            _avlTreeHashTable.Add(_value, _current);
            if (_value < _prev.Data) //如果插入值小于双亲结点的值
            {
                if (_prev.Right != null)
                {
                    Vm.RemoveNode(_current.Data.ToString(), true);
                    List<AvlNodeStruct> allRightChidren = StartPreOrder(_prev.Right);
                    foreach (var vertex in allRightChidren)
                    {
                        Vm.RemoveNode(vertex.Value.ToString(), true);
                    }
                    Vm.AddVertex(_current.Data.ToString(), _current.Bf, true);
                    _prev.Left = _current; //添加一条边
                    _current.Parent = _prev;
                    AddVmSubtree(_prev.Right);
                }
                else
                {
                    _prev.Left = _current; //成为左孩子
                    _current.Parent = _prev;
                }
            }
            else //如果插入值大于双亲结点的值
            {
                _prev.Right = _current; //成为右孩子
                _current.Parent = _prev;
            }
            _path[_p] = _current; //将新元素插入数组path的最后
            while (_p > 0)
            {   //bf表示平衡因子的改变量，当新结点插入左子树，则平衡因子+1
                //当新结点插入右子树，则平衡因子-1
                _tbf = (_value < _path[_p - 1].Data) ? 1 : -1;
                _path[--_p].Bf += _tbf; //改变当前父结点的平衡因子
                _tbf = _path[_p].Bf; //获取当前结点的平衡因子
                //判断当前结点平衡因子，如果为0表示该子树已平衡，不需再回溯
                //而改变祖先结点平衡因子，此时添加成功，直接返回
                if (_tbf == 0)
                {
                    //_addTimer.Stop();
                    _step = -1;
                    _value = 0;
                    break;
                }
                if (_tbf == 2 || _tbf == -2) //需要旋转的情况
                {
                    AvlTreeNode pNode = _path[_p].Parent;
                    while (pNode != null)
                    {
                        pNode.Bf += (_value < pNode.Data) ? 1 : -1;
                        pNode = pNode.Parent;
                    }
                    break;
                }
            }
            if (_tbf == 1 || _tbf == -1) //path到顶的时候，如果当前节点tbf是1或-1则直接退出
            {
                _step = -1;
                _value = 0;
            }
            Vm.Graph.RaiseChangedByTheCustom();
            _step++;
        }
        #endregion 单步添加

        /// <summary>
        /// 删除节点的计时器
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void RemoveTimer_Tick(object sender, EventArgs e)
        {
            switch (_tick)
            {
                case 0:
                    #region 数据结构层的删除
                    AvlTreeNode node = (AvlTreeNode)_avlTreeHashTable[_value];
                    AvlTreeNode tmp;
                    if (node.Left != null && node.Right != null)
                    {//当被删除结点存在左右子树时
                        tmp = node.Left; //获取左子树
                        _path[++_p] = tmp;
                        while (tmp.Right != null) //获取node的中序遍历前驱结点，并存放于tmp中
                        {   //找到左子树中的最右下结点
                            tmp = tmp.Right;
                            _path[++_p] = tmp;
                        }
                        node.SetData(tmp.Data);//用中序遍历前驱结点的值代替被删除结点的值
                        //以下相当于删除点
                        if (_path[_p - 1] == node)
                        {
                            _path[_p - 1].SetLeft(tmp.Left);
                        }
                        else
                        {
                            _path[_p - 1].SetRight(tmp.Left);
                        }
                    }
                    else //当只有左子树或右子树或为叶子结点时
                    {   //首先找到惟一的孩子结点
                        tmp = node.Left;
                        if (tmp == null) //如果只有右孩子或没孩子
                        {
                            tmp = node.Right;
                        }
                        if (_p > 0)//当删除的不是根结点时
                        {
                            if (_path[_p - 1].Left == node)
                            {   //如果被删结点是左孩子
                                _path[_p - 1].SetLeft(tmp);
                            }
                            else
                            {   //如果被删结点是右孩子
                                _path[_p - 1].SetRight(tmp);
                            }
                        }
                        else  //当删除的是根结点时
                        {
                            _head = tmp;
                        }
                    }
                    #endregion 数据结构层的删除
                    #region Graphviz层的删除
                    if (node.Parent != null)
                    {//如果删除节点有父节点
                        Vm.RemoveNode(node.Data.ToString(), true);
                        List<AvlNodeStruct> allChidren = StartPreOrder(node.Parent);
                        foreach (var vertex in allChidren)
                        {
                            if (vertex.Value != node.Parent.Data)
                                Vm.RemoveNode(vertex.Value.ToString(), true);
                        }
                        if (node.Parent.Left != null)
                            AddVmSubtree(node.Parent.Left);
                        if (node.Parent.Right != null)
                            AddVmSubtree(node.Parent.Right);
                    }
                    else
                    {//如果删除节点没有父节点（根节点被删除）
                        Vm.Graph.Clear(true);
                        if (_head != null)
                        {
                            Vm.AddVertex(_head.Data.ToString());
                            Vm.SetBf(_head.Data, _head.Bf);
                            if (_head.Left != null)
                                AddVmSubtree(_head.Left);
                            if (_head.Right != null)
                                AddVmSubtree(_head.Right);
                        }
                    }
                    Vm.Graph.RaiseChangedByTheCustom();
                    #endregion Graphviz层的删除
                    _tick++;
                    break;
                case 1:
                    _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                    _timer.Tick += removeCase1Timer_Tick;
                    _timer.Start();

                    //关闭计时器
                    _removeTimer.Stop();
                    _tick = 0;
                    break;
            }
        }

        void removeCase1Timer_Tick(object sender, EventArgs e)
        {
            if (_p > 0)
            {
                AvlTreeNode node = (AvlTreeNode)_avlTreeHashTable[_value];
                //删除完后依次向上进行旋转，现在p指向实际被删除的结点
                int data = node.Data;

                //bf表示平衡因子的改变量，当删除的是左子树中的结点时，平衡因子-1
                //当删除的是右子树的孩子时，平衡因子+1
                int bf = (data <= _path[_p - 1].Data) ? -1 : 1;
                _path[_p - 1].SetBf(_path[_p - 1].Bf + bf);//改变当前父结点的平衡因子
                --_p;
                bf = _path[_p].Bf; //获取当前结点的平衡因子
                if (bf != 0) //如果bf==0，表明高度降低，继续向上回溯
                {
                    //如果bf为1或-1则说明高度未变，停止回溯，如果为2或-2，则进行旋转
                    //当旋转后高度不变，则停止回溯
                    if (bf == 1 || bf == -1 || !RotateSubTree(bf))
                    {
                        //关闭计时器
                        _timer.Stop();
                        _value = 0;
                    }
                    #region Graphviz图层的旋转
                    if (bf == 2 || bf == -2)
                    {//全部重头开始绘图
                        AvlTreeNode childTreeRoot = _path[_p];
                        if (childTreeRoot.Parent != null)
                        {//如果最小不平衡子树根节点节点有父节点
                            List<AvlNodeStruct> allChidren = StartPreOrder(childTreeRoot.Parent);
                            foreach (var vertex in allChidren)
                            {
                                if (vertex.Value != childTreeRoot.Parent.Data)
                                    Vm.RemoveNode(vertex.Value.ToString(), true);
                            }
                            if (childTreeRoot.Parent.Left != null)
                                AddVmSubtree(childTreeRoot.Parent.Left);
                            if (childTreeRoot.Parent.Right != null)
                                AddVmSubtree(childTreeRoot.Parent.Right);
                        }
                        else
                        {//如果最小不平衡子树没有父节点（即当前最小不平衡子树就是整棵树）
                            Vm.Graph.Clear(true);
                            if (_head != null)
                            {
                                Vm.AddVertex(_head.Data.ToString());
                                Vm.SetBf(_head.Data, _head.Bf);
                                if (_head.Left != null)
                                    AddVmSubtree(_head.Left);
                                if (_head.Right != null)
                                    AddVmSubtree(_head.Right);
                            }
                        }
                        Vm.Graph.RaiseChangedByTheCustom();
                    }
                    #endregion Graphviz图层的旋转
                }
            }
            else
            {
                _timer.Stop();
                _value = 0;
            }
        }

        #region 单步删除
        /// <summary>
        /// 单步删除计时器
        /// </summary>
        /// <returns></returns>
        public int RemoveStepByStep()
        {
            switch (_step)
            {
                case 0:
                    List<List<string>> list = GetPathString(_path, _p + 1);
                    List<string> edges = GetPathEdges(list);
                    if (edges.Count > 0)
                    {
                        Vm.ViewModelFindThePath(edges, list, "平衡二叉树", _graphLayout);

                        _howLong = edges.Count - 1;
                        _waitTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                        _waitTimer.Tick += WaitDelTimer_Tick;
                        _waitTimer.Start();
                        return 1;
                    }
                    else
                    {//删除中的特例：中序前驱代替root
                        Vm.AddMyX(Vm.GetVertexPosition(_value.ToString(), this._graphLayout), 60, 80, new FontFamily("Microsoft YaHei UI Light"), Brushes.IndianRed, 50, this._graphLayout);
                        _step++;
                    }
                    break;
                case 1:
                    AvlTreeNode tnode = (AvlTreeNode)_avlTreeHashTable[_value];
                    AvlTreeNode ttmp;
                    if (tnode.Left != null && tnode.Right != null)
                    {//当被删除结点存在左右子树时
                        ttmp = tnode.Left; //获取左子树
                        while (ttmp.Right != null) //获取node的中序遍历前驱结点，并存放于tmp中
                        {
                            //找到左子树中的最右下结点
                            ttmp = ttmp.Right;
                        }

                        //画箭头：
                        Point start = Vm.GetVertexPosition(ttmp.Data.ToString(), this._graphLayout);
                        start.Y -= 15;
                        Point end = Vm.GetVertexPosition(tnode.Data.ToString(), this._graphLayout);
                        end.Y += 20;

                        Vm.AddArrowLineWithText(start, end, Brushes.Blue, 1.5, "替换", TextAlignment.Center, this._graphLayout);
                    }
                    else //当只有左子树或右子树或为叶子结点时
                    {
                        //首先找到惟一的孩子结点
                        ttmp = tnode.Left;
                        if (ttmp == null) //如果只有右孩子或没孩子
                        {
                            //ttmp = tnode.Right;
                            if (tnode.Right != null)
                            {//只有右孩子
                                ttmp = tnode.Right;
                            }
                            else
                            {//是叶子节点
                                //直接删掉
                                RemoveANode();
                                _step = 3;
                                return _step;
                            }
                        }
                        //以下为只有左孩子|只有右孩子
                        if (Head != tnode) //当删除的不是根结点时
                        {
                            //删除节点父节点指针指向删除节点的子节点
                            Point start = Vm.GetVertexPosition(tnode.Parent.Data.ToString(), this._graphLayout);
                            Point end = Vm.GetVertexPosition(ttmp.Data.ToString(), this._graphLayout);
                            Vm.AddMyArrow(start,end,Brushes.Black,1.5,1,_graphLayout);
                        }
                        else //当删除的是根结点时
                        {
                            //直接删除
                            RemoveANode();
                            _step = 3;
                        }
                    }

                    break;
                case 2:
                    RemoveANode();
                    
                    Vm.Graph.RaiseChangedByTheCustom();

                    _tick++;
                    break;
                case 3:
                    _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                    _timer.Tick += removeCase1Timer_Tick;
                    _timer.Start();

                    //关闭计时器
                    _removeTimer.Stop();
                    _tick = 0;
                    break;
            }
            return _step;
        }

        private void RemoveANode()
        {
            #region 数据结构层的删除
            AvlTreeNode node = (AvlTreeNode)_avlTreeHashTable[_value];
            AvlTreeNode tmp;

            if (node.Left != null && node.Right != null)
            {//当被删除结点存在左右子树时
                tmp = node.Left; //获取左子树
                _path[++_p] = tmp;
                while (tmp.Right != null) //获取node的中序遍历前驱结点，并存放于tmp中
                {
                    //找到左子树中的最右下结点
                    tmp = tmp.Right;
                    _path[++_p] = tmp;
                }
                node.SetData(tmp.Data); //用中序遍历前驱结点的值代替被删除结点的值
                //以下相当于删除点
                if (_path[_p - 1] == node)
                {
                    _path[_p - 1].SetLeft(tmp.Left);
                }
                else
                {
                    _path[_p - 1].SetRight(tmp.Left);
                }
            }
            else //当只有左子树或右子树或为叶子结点时
            {
                //首先找到惟一的孩子结点
                tmp = node.Left;
                if (tmp == null) //如果只有右孩子或没孩子
                {
                    tmp = node.Right;
                }
                if (_p > 0) //当删除的不是根结点时
                {
                    if (_path[_p - 1].Left == node)
                    {
                        //如果被删结点是左孩子
                        _path[_p - 1].SetLeft(tmp);
                    }
                    else
                    {
                        //如果被删结点是右孩子
                        _path[_p - 1].SetRight(tmp);
                    }
                }
                else //当删除的是根结点时
                {
                    _head = tmp;
                }
            }

            #endregion 数据结构层的删除

            #region Graphviz层的删除 ——已注释

            //if (node.Parent != null)
            //{
            //    //如果删除节点有父节点
            //    Vm.RemoveNode(node.Data.ToString(), true);
            //    List<AvlNodeStruct> allChidren = StartPreOrder(node.Parent);
            //    foreach (var vertex in allChidren)
            //    {
            //        if (vertex.Value != node.Parent.Data)
            //            Vm.RemoveNode(vertex.Value.ToString(), true);
            //    }
            //    if (node.Parent.Left != null)
            //        AddVmSubtree(node.Parent.Left);
            //    if (node.Parent.Right != null)
            //        AddVmSubtree(node.Parent.Right);
            //}
            //else
            //{
            //    //如果删除节点没有父节点（根节点被删除）
            //    Vm.Graph.Clear(true);
            //    if (_head != null)
            //    {
            //        Vm.AddVertex(_head.Data.ToString());
            //        Vm.SetBf(_head.Data, _head.Bf);
            //        if (_head.Left != null)
            //            AddVmSubtree(_head.Left);
            //        if (_head.Right != null)
            //            AddVmSubtree(_head.Right);
            //    }
            //}
            //Vm.Graph.RaiseChangedByTheCustom();

            #endregion Graphviz层的删除

            //重绘
            GraphvizReDraw();
        }

        void WaitDelTimer_Tick(object sender, EventArgs e)
        {
            if (_i >= _howLong)
            {
                _i = 0;
                _step++;
                _waitTimer.Stop();
                Vm.AddMyX(Vm.GetVertexPosition(_value.ToString(), this._graphLayout), 60, 80, new FontFamily("Microsoft YaHei UI Light"), Brushes.IndianRed, 50, this._graphLayout);
            }
            else
            {
                _i++;
            }
        }
        #endregion

        #endregion 计时委托或与其同等级的实现函数

        #region 平衡二叉树对外提供的方法
        public int Size()
        {
            return _avlTreeHashTable.Count;
        }

        public void Clear()
        {
            Vm.Graph.Clear(true);
            _head = null;
            _avlTreeHashTable.Clear();
            //if (_initNodeHashtable != null)
            //    _initNodeHashtable.Clear();
        }

        public void RandomInit(string createSize)
        {
            Regex regex = new Regex(@"^[1-9]\d*$");
            if (!regex.IsMatch(createSize))
                throw new Exception("您的输入有误,初始化大小必须为大于0的整数");
            if (int.Parse(createSize) > 100)
                throw new Exception("考虑到性能与显示上的问题，规定最大初始化节点数量为100！");
            if (Size() > 0)
            {
                Clear();
            }
            Random random = new Random();
            int initLength = int.Parse(createSize);
            string initializedValues = "";
            for (int i = 0; i < initLength; i++)
            {
                int data = random.Next(-999, 1000);
                while (_avlTreeHashTable.ContainsKey(data))
                {
                    data = random.Next(-999, 1000);
                }
                //Add(data);
                InitAdd(data);
                initializedValues += data + " ";
            }
            DrawVmTree(_head);
            Vm.Graph.RaiseChangedByTheCustom();
            LogId++;
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "初始化(" + createSize + ")", "", initializedValues));
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

        public void Traversal(string order, GraphLayout graphLayout)
        {
        }

        public void AddRoot(string data)
        {
            if (_head != null)
                throw new Exception("根节点已存在！");
            Regex regex = new Regex(@"^\-?[0-9]+$");
            if (!regex.IsMatch(data))
                throw new Exception("请输入整数");
            _head = new AvlTreeNode(int.Parse(data)) { Bf = 0 };
            Vm.Graph.RaiseChangedByTheCustom();
            _avlTreeHashTable.Add(int.Parse(data), _head);
            LogId++;
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "创建根节点", "", data));
        }

        public int AddChild(string data, bool isStepThrough)
        {
            Regex regex = new Regex(@"^\-?[0-9]+$");
            if (!regex.IsMatch(data))
            {
                throw new Exception("节点值只能是整数！");
            }
            if (_avlTreeHashTable.ContainsKey(int.Parse(data)))
            {
                throw new Exception("插入失败：插入值已存在!");
            }
            //Add(int.Parse(data));
            int step = StartAdd(int.Parse(data), isStepThrough);
            LogId++;
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "插入节点", "", data));
            return step;
        }

        public int DeleteSelNode(string selValue, bool isStepThrough)
        {
            if (string.IsNullOrEmpty(selValue))
            {
                throw new Exception("请输入要删除节点的data值");
            }
            Regex regex = new Regex(@"^\-?[0-9]+$");
            if (!regex.IsMatch(selValue))
            {
                throw new Exception("您输入的节点值不是整数！");
            }
            if (!_avlTreeHashTable.ContainsKey(int.Parse(selValue)))
            {
                throw new Exception("该节点不存在");
            }
            int step = -1;
            #region 完成Path的建立,找到节点就删除
            _p = -1;
            //parent表示双亲结点，node表示当前结点
            AvlTreeNode node = _head;
            //寻找指定值所在的结点
            while (node != null)
            {
                _path[++_p] = node;
                //如果找到，则调用RemoveNode方法删除结点
                if (int.Parse(selValue) == node.Data)
                {
                    step = StartRemove(int.Parse(selValue), isStepThrough);//现在_p指向被删除结点
                    break;
                }
                node = int.Parse(selValue) < node.Data ? node.Left : node.Right;//否则继续查找
            }
            #endregion 完成Path的建立
            LogId++;
            BTreeLogs.Add(LogId + "", new LogVo(LogId, "删除节点", "", selValue));
            return step;
        }


        #endregion 平衡二叉树对外提供的方法
    }
}