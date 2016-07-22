using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using Graphviz4Net.WPF;
using integrateOfDataStructure.Dialogs;
using integrateOfDataStructure.Utility;

namespace integrateOfDataStructure
{
    /// <summary>
    /// 图的节点类
    /// </summary>
    public class GraphNode                                  //顶点类型
    {
        public int No;                                      //顶点编号
        public string Data;
    };

    /// <summary>
    /// 图的邻接矩阵类
    /// </summary>
    public class GraphMatrix
    {
        public GraphMatrix()
        {
        }
        public GraphMatrix(int nodesNum)
        {
            Matrix = new string[nodesNum, nodesNum];
            GraphNodes = new GraphNode[nodesNum];
        }
        public string[,] Matrix;                            //邻接矩阵的边数组，假设权值为整数
        public int NodeCount;                               //顶点数
        public int EdgeCount;                               //边数
        public GraphNode[] GraphNodes;                      //存放顶点信息
    };

    /// <summary>
    /// 图的基本运算类
    /// </summary>
    public class GraphClass
    {
        const int Maxv = 50;                                //最大顶点个数
        const int Inf = int.MaxValue;                       //用INF表示∞
        public static ViewModel Vm = new ViewModel();
        private GraphLayout _graphLayout;
        public GraphMatrix GMatrix;     //图的邻接矩阵存储结构
        private string _initializedValues = "";// 随机初始化的所有节点的值
        private string _initializedEdges = "";

        //计时器相关
        private readonly DispatcherTimer _insertNodeIntoEdgeTimer = new DispatcherTimer();
        private int _insertClock;                                 //动画的步子
        private string _startNodeData;                      //全局变量(供计时函数使用):起点
        private string _endNodeData;                        //全局变量(供计时函数使用):终点                        
        private string _thisNodeData;                       //全局变量(供计时函数使用):所要添加的节点值或边的权值
        private int _startToTargetWeight;
        private int _targetToEndWeight;

        private readonly Hashtable _graphHashtable = new Hashtable();     //图结构中用于快速存取节点用的哈希表
        public List<LogVo> Maplogs = new List<LogVo>();
        public int LogId;

        public GraphClass()
        {
            GMatrix = new GraphMatrix(Maxv);
            _insertNodeIntoEdgeTimer.Tick += insertNodeIntoEdgeTimer_Tick;
            _insertNodeIntoEdgeTimer.Interval = new TimeSpan(0, 0, 1);
        }
        #region 图的基本操作
        private void Expand()
        {
            GraphNode[] larger = new GraphNode[GMatrix.GraphNodes.Length * 2];
            string[,] mLarger = new string[larger.Length, larger.Length];
            for (int i = 0; i < GMatrix.GraphNodes.Length; i++)
            {
                larger[i] = GMatrix.GraphNodes[i];
                for (int j = 0; j < GMatrix.GraphNodes.Length; j++)
                    mLarger[i, j] = GMatrix.Matrix[i, j];
            }
            GMatrix.GraphNodes = larger;
            GMatrix.Matrix = mLarger;
        }

        //------------图的基本运算算法------------
        private void CreateMGraph(int nodeCount, int edgeCount, string[,] matrix, string[] vnodes)
        {//通过相关数据建立邻接矩阵
            int i, j;
            GMatrix.NodeCount = nodeCount; GMatrix.EdgeCount = edgeCount;
            for (i = 0; i < GMatrix.NodeCount; i++)
                for (j = 0; j < GMatrix.NodeCount; j++)
                    GMatrix.Matrix[i, j] = matrix[i, j];
            for (i = 0; i < GMatrix.NodeCount; i++)
            {
                GMatrix.GraphNodes[i].Data = vnodes[i];
                GMatrix.GraphNodes[i].No = i;
            }
        }
        private void CreateMGraph(int nodeCount, int edgeCount, string[,] matrix, GraphNode[] vnodes)
        {   //通过相关数据建立邻接矩阵
            int i, j;
            GMatrix.NodeCount = nodeCount; GMatrix.EdgeCount = edgeCount;
            for (i = 0; i < GMatrix.NodeCount; i++)
                for (j = 0; j < GMatrix.NodeCount; j++)
                    GMatrix.Matrix[i, j] = matrix[i, j];
            for (i = 0; i < GMatrix.NodeCount; i++)
            {
                GMatrix.GraphNodes[i].Data = vnodes[i].Data;
                GMatrix.GraphNodes[i].No = i;
            }
        }

        private string DispMGraph()
        {//输出图的邻接矩阵
            string mystr = "";
            int i, j;
            for (i = 0; i < GMatrix.NodeCount; i++)
            {
                for (j = 0; j < GMatrix.NodeCount; j++)
                    if (GMatrix.Matrix[i, j].Equals(Inf))
                        mystr += string.Format("{0,-3}", "∞");
                    else
                        mystr += string.Format("{0,-4}", GMatrix.Matrix[i, j]);
                mystr += "\r\n";
            }
            Debug.WriteLine(mystr);
            return mystr;
        }

        private bool HasEdge(int start, int end)
        {   //判断2个顶点之间是否存在边
            if (GMatrix.Matrix[start, end].Equals(null))
                return true;
            return false;
        }

        /// <summary>
        /// 根据data值获取顶点的id
        /// </summary>
        /// <param name="data">节点data</param>
        /// <returns>节点id</returns>
        private int GetVNodeIdByData(string data)
        {
            for (int i = 0; i < GMatrix.NodeCount; i++)
            {
                if (GMatrix.GraphNodes[i].Data.Equals(data))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 根据顶点id值获取节点data
        /// </summary>
        /// <param name="id">节点id</param>
        /// <returns>节点data</returns>
        private string GetVNodeDataById(int id)
        {
            return GMatrix.GraphNodes[id].Data;
        }

        #endregion 图的基本操作

        #region 计时委托或与其同等级的实现函数

        #region 添加/插入顶点的实现函数
        private void AddVNode(string value)
        {   //添加顶点
            GraphNode node = new GraphNode { Data = value };
            Vm.AddVertex(node.Data);
            if (Size() >= GMatrix.GraphNodes.Length)
                Expand();
            int position = GMatrix.NodeCount;
            GMatrix.GraphNodes[position] = node;
            GMatrix.NodeCount++;
            node.No = position;
            _graphHashtable.Add(value, node);
            _graphHashtable.Add(value + "Id", GMatrix.NodeCount - 1);
        }

        private void AddVNode(string value, bool withoutRaiseChanged)
        {   //添加顶点
            if (_graphHashtable.Contains(value))
            {
                throw new Exception("图中已存在节点：" + value);
            }
            GraphNode node = new GraphNode { Data = value };
            Vm.AddVertex(node.Data);
            if (Size() >= GMatrix.GraphNodes.Length)
                Expand();
            int position = GMatrix.NodeCount;
            GMatrix.GraphNodes[position] = node;
            GMatrix.NodeCount++;
            node.No = position;
            _graphHashtable.Add(value, node);
            _graphHashtable.Add(value + "Id", GMatrix.NodeCount - 1);
        }

        private void insertNodeIntoEdgeTimer_Tick(object sender, EventArgs e)
        {
            Point start;
            Point end;
            switch (_insertClock)
            {
                case 0:
                    AddVNode(_thisNodeData);
                    Vm.Graph.RaiseChangedByTheCustom();
                    break;
                case 1:
                    Vm.AddMyX(Vm.GetAPointOnEdge(_startNodeData, _endNodeData, _graphLayout), _graphLayout);
                    start = Vm.GetVertexPosition(_startNodeData, _graphLayout);
                    end = Vm.GetVertexPosition(_thisNodeData, _graphLayout);

                    CoordinateTransform(ref start, ref end);
                    Vm.AddMyArrow(start, end, 1, _graphLayout);//                    
                    break;
                case 2:
                    start = Vm.GetVertexPosition(_thisNodeData, _graphLayout);
                    end = Vm.GetVertexPosition(_endNodeData, _graphLayout);
                    CoordinateTransform(ref start, ref end);
                    Vm.AddMyArrow(start, end, 0, _graphLayout);
                    break;
                case 3:
                    AddEdgeWithDirection(_startNodeData, _thisNodeData, _startToTargetWeight+"");
                    AddEdgeWithDirection(_thisNodeData, _endNodeData, _targetToEndWeight+"");
                    RemoveEdgeWithDirection(_startNodeData, _endNodeData, true);

                    Vm.Graph.RaiseChangedByTheCustom();
                    //归零操作：
                    _insertClock = -1;
                    _insertNodeIntoEdgeTimer.Stop();
                    _thisNodeData = null;
                    _startNodeData = null;
                    _endNodeData = null;
                    break;
            }
            _insertClock++;
        }

        //这个函数纯粹是为了让箭头的起点与终点位置更和谐
        private void CoordinateTransform(ref Point start, ref Point end)
        {
            Double xDifference = start.X - end.X; //x1-x2
            Double yDifference = start.Y - end.Y; //y1-y2
            if (Math.Abs(xDifference / yDifference) > 1)
            {//在315度到45度或135度到225度之间
                if (xDifference < 0)
                {//在315度到45度之间
                    start.X += 10;
                    start.Y = start.Y;
                    end.X -= 10;
                    end.Y = end.Y;
                }
                else
                {//在135度到225度之间
                    start.X -= 10;
                    start.Y = start.Y;
                    end.X += 10;
                    end.Y = end.Y;
                }
            }
            else
            {//在45度到135度或225度到315度之间
                if (yDifference < 0)
                {//在45度到135度之间
                    start.X = start.X;
                    start.Y += 10;
                    end.X = end.X;
                    end.Y -= 10;
                }
                else
                {//在225度到315度之间
                    start.X = start.X;
                    start.Y -= 10;
                    end.X = end.X;
                    end.Y += 10;
                }
            }
        }

        #endregion 添加/插入顶点的实现函数

        #region 删除顶点的实现函数
        private void RemoveVNode(string data)
        {
            //删除顶点
            int position = (int)_graphHashtable[data + "Id"];
            #region 更新哈希表
            List<string> keyList = new List<string>();
            foreach (DictionaryEntry de in _graphHashtable)
            {
                if (de.Key.ToString().Contains(data))
                    keyList.Add(de.Key.ToString());
            }
            foreach (string key in keyList)
            {
                _graphHashtable.Remove(key);
            }
            #endregion

            #region 从矩阵与节点数组中删除一个节点
            //先调整关系矩阵
            for (int i = 0; i < Size(); i++)
                for (int j = 0; j < Size(); j++)//将关系矩阵向内紧缩（顶点将要移动）
                {
                    if (i > position && j > position)
                        GMatrix.Matrix[i - 1, j - 1] = GMatrix.Matrix[i, j];
                    else if (i > position)
                        GMatrix.Matrix[i - 1, j] = GMatrix.Matrix[i, j];
                    else if (j > position)
                        GMatrix.Matrix[i, j - 1] = GMatrix.Matrix[i, j];
                }
            for (int i = 0; i < Size(); i++)    //紧缩以后，最后一个顶点已经没有意义了,将其置null
            {
                GMatrix.Matrix[Size() - 1, i] = null;
                GMatrix.Matrix[i, Size() - 1] = null;
            }
            //再调整顶点数组
            for (int i = position; i < Size() - 1; i++)//保证数组连续性
                GMatrix.GraphNodes[i] = GMatrix.GraphNodes[i + 1];
            GMatrix.GraphNodes[Size() - 1] = null;
            GMatrix.NodeCount--;
            #endregion
            Vm.RemoveNode(data, true);                                    //删除Graphviz中的顶点
        }

        private void RemoveVNode(string data, bool withoutRaiseChanged)
        {
            RemoveVNode((int)_graphHashtable[data + "Id"], true);
        }

        private void RemoveVNode(int position, bool withoutRaiseChanged)
        {   //删除顶点
            string data = GetVNodeDataById(position);
            #region 更新哈希表
            List<string> keyList = new List<string>();
            foreach (DictionaryEntry de in _graphHashtable)
            {
                if (de.Key.ToString().Contains(GetVNodeDataById(position)))
                    keyList.Add(de.Key.ToString());
            }
            foreach (string key in keyList)
            {
                _graphHashtable.Remove(key);
            }
            #endregion
            #region 从矩阵与节点数组中删除一个节点
            //先调整关系矩阵
            for (int i = 0; i < Size(); i++)
                for (int j = 0; j < Size(); j++)//将关系矩阵向内紧缩（顶点将要移动）
                {
                    if (i > position && j > position)
                        GMatrix.Matrix[i - 1, j - 1] = GMatrix.Matrix[i, j];
                    else if (i > position)
                        GMatrix.Matrix[i - 1, j] = GMatrix.Matrix[i, j];
                    else if (j > position)
                        GMatrix.Matrix[i, j - 1] = GMatrix.Matrix[i, j];
                }
            for (int i = 0; i < Size(); i++)    //紧缩以后，最后一个顶点已经没有意义了（移到倒数第二个了）
            {
                GMatrix.Matrix[Size() - 1, i] = null;
                GMatrix.Matrix[i, Size() - 1] = null;
            }
            //在调整顶点数组
            for (int i = position; i < Size() - 1; i++)//保证数组连续性
                GMatrix.GraphNodes[i] = GMatrix.GraphNodes[i + 1];
            GMatrix.GraphNodes[Size() - 1] = null;
            GMatrix.NodeCount--;
            #endregion
            Vm.RemoveNode(data, true);                                    //删除Graphviz中的顶点
        }
        #endregion 删除顶点的实现函数

        #region 添加边的实现函数
        private void AddEdgeWithDirection(int startId, int endId, string len)
        {   //在两个指定下标的节点之间添加一条边
            string start = GetVNodeDataById(startId);
            string end = GetVNodeDataById(endId);
            AddEdgeWithDirection(start, end, len);
        }

        private void AddEdgeWithDirection(int startId, int endId, string len, bool withoutRaiseChanged)
        {   //在两个指定下标的节点之间添加一条边
            string start = GetVNodeDataById(startId);
            string end = GetVNodeDataById(endId);
            AddEdgeWithDirection(start, end, len, true);
        }

        private void AddEdgeWithDirection(string start, string end, string len)
        {
            Vm.AddAdge(start, end, len);

            int startId = GetVNodeIdByData(start);
            int endId = GetVNodeIdByData(end);
            GMatrix.Matrix[startId, endId] = len;
            GMatrix.EdgeCount++;

            _graphHashtable.Add(start + " " + end, GMatrix.Matrix[startId, endId]);
        }

        private void AddEdgeWithDirection(string start, string end, string len, bool withoutRaiseChanged)
        {
            Vm.AddAdge(start, end, len);

            int startId = GetVNodeIdByData(start);
            int endId = GetVNodeIdByData(end);
            GMatrix.Matrix[startId, endId] = len;
            GMatrix.EdgeCount++;

            _graphHashtable.Add(start + " " + end, GMatrix.Matrix[startId, endId]);
        }
        #endregion 添加边的实现

        #region 删除有向边的实现函数
        private void RemoveEdgeWithDirection(int startNodeId, int endNodeId)
        {   //删除两个指定下标顶点之间的边
            string startNode = GetVNodeDataById(startNodeId);
            string endNode = GetVNodeDataById(endNodeId);
            Vm.RemoveEdge(startNode, endNode);
            GMatrix.Matrix[startNodeId, endNodeId] = null;
            GMatrix.EdgeCount--;

            _graphHashtable.Remove(startNode + " " + endNode);
        }

        private void RemoveEdgeWithDirection(int startNodeId, int endNodeId, bool withoutRaiseChanged)
        {   //删除两个指定下标顶点之间的边
            string startNode = GetVNodeDataById(startNodeId);
            string endNode = GetVNodeDataById(endNodeId);
            Vm.RemoveEdge(startNode, endNode, true);
            GMatrix.Matrix[startNodeId, endNodeId] = null;
            GMatrix.EdgeCount--;

            _graphHashtable.Remove(startNode + " " + endNode);
        }

        private void RemoveEdgeWithDirection(string start, string end, bool withoutRaiseChanged)
        {   //删除两个指定下标顶点之间的边
            RemoveEdgeWithDirection(GetVNodeIdByData(start), GetVNodeIdByData(end), true);
        }
        #endregion 删除有向边的实现函数

        #endregion 计时委托或与其同等级的实现函数

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
        /// 日志操作函数
        /// </summary>
        /// <param name="select"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="data"></param>
        /// <param name="isLog"></param>
        /// <returns></returns>
        private int WritingLogAdd_Remove(int select, int source, int destination, string data, bool isLog)
        {
            LogId++;
            int row = LogId;//行号
            //按钮或命令行操作
            if (isLog.Equals(false))
            {
                if (select == 1)//添加Vertex
                {
                    Maplogs.Add(new LogVo(LogId, "插入顶点", source, destination, data));
                }
                else if (select == 2)//删除Vertex
                {
                    Maplogs.Add(new LogVo(LogId, "删除顶点", source, destination, data));
                }
                else if (select == 3)//添加Edge
                {
                    Maplogs.Add(new LogVo(LogId, "添加边", source, destination, data));
                }
                else if (select == 4)//删除Edge
                {
                    Maplogs.Add(new LogVo(LogId, "删除边", source, destination, data));
                }
            }
            //日志操作
            else
            {
                switch (select)
                {
                    case 1:
                        {
                            if (source != -1 || destination != -1)
                            {
                                Maplogs.Add(new LogVo(LogId, "插入顶点", source, destination, data));
                                AddVNode(data, true);
                                RemoveEdgeWithDirection(source, destination, true);
                                AddEdgeWithDirection(source, (int)_graphHashtable[data + "Id"], "1", true);
                                AddEdgeWithDirection(GetVNodeIdByData(data), destination, "1", true);
                            }
                            else
                            {
                                Maplogs.Add(new LogVo(LogId, "插入顶点", source, destination, data));
                                AddVNode(data, true);
                            }
                        }
                        break;
                    case 2:
                        {
                            Maplogs.Add(new LogVo(LogId, "删除顶点", source, destination, data));
                            RemoveVNode(data, true);
                        }
                        break;
                    case 3:
                        {
                            Maplogs.Add(new LogVo(LogId, "添加边", source, destination, data));
                            AddEdgeWithDirection(source, destination, data, true);
                        }
                        break;
                    case 4:
                        {
                            Maplogs.Add(new LogVo(LogId, "删除边", source, destination, data));
                            RemoveEdgeWithDirection(source, destination, true);
                        }
                        break;
                }
            }
            return row;
        }

        public void LogInit(string values, string hideString)
        {
            string[] vals=values.Split(' ');
            string[] edges = hideString.Split(' ');
            foreach (string vertex in vals)
            {
                AddVNode(vertex);
            }
            foreach (string edge in edges)
            {
                string[] e=edge.Split('_');
                AddEdgeWithDirection(int.Parse(e[0]), int.Parse(e[1]), e[2] , true);
            }
            LogId++;
            Maplogs.Add(new LogVo(LogId, "初始化(" + vals.Length + ")", "", values, hideString));
        }

        /// <summary>
        /// 跳转函数之向图中插入一个节点
        /// </summary>
        /// <param name="start">起点值</param>
        /// <param name="end">终点值</param>
        /// <param name="element">要插入节点的值</param>
        private void InsertVNodeBetweenEdge(string start, string end, string element, int startToTarget, int targetToEnd)
        {
            _startNodeData = start;
            _endNodeData = end;
            _thisNodeData = element;
            _startToTargetWeight = startToTarget;
            _targetToEndWeight = targetToEnd;
            _insertNodeIntoEdgeTimer.Start();
        }


        #endregion 数据结构内部操作函数

        #region 数据结构对外界所提供的函数
        /// <summary>
        /// 获取图节点的数量
        /// </summary>
        /// <returns>节点数</returns>
        public int Size()
        {   //返回图的顶点个数
            return GMatrix.NodeCount;
        }
        /// <summary>
        /// 获取图的边的数量
        /// </summary>
        /// <returns></returns>
        public int GetEdgeCount()
        {   //返回图的边数
            return GMatrix.EdgeCount;
        }
        /// <summary>
        /// 是否为空
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return Size() == 0;
        }

        public void RandomInit(string vertexSize, string edgeSize)
        {
            Regex reg = new Regex(@"^[1-9]\d*$");
            if (!reg.IsMatch(vertexSize) || !reg.IsMatch(edgeSize))
                throw new Exception("您的输入有误,请输入大于0的整数！");
            int vertexNum = int.Parse(vertexSize);
            int edgeNum = int.Parse(edgeSize);
            int tMaxEdges = vertexNum * (vertexNum - 1);
            if (edgeNum > tMaxEdges)
            {
                MessageBox.Show(
                    "您的输入有误！\n由于顶点数为：" + vertexNum + "，有向边最大数为：" + +vertexNum + "x" + (vertexNum - 1),
                    "请输入不大于" + vertexNum + "x" + (vertexNum - 1) + "的整数！", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!IsEmpty())
            {
                if (MessageBox.Show("初始化操作会清空之前的图，是否继续？", "确认窗口", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    Clear();
                }
                else return;
            }
            _initializedValues = "";
            _initializedEdges = "";
            string[] vertexs = GetRamdomList(vertexNum).Split(' ');
            foreach (string vertex in vertexs)
            {
                AddVNode(vertex);
                _initializedValues += vertex+" ";
            }
            _initializedValues=_initializedValues.Substring(0,_initializedValues.Length-1);
            Random random = new Random();
            while (edgeNum >= 1)
            {
                int i = random.Next(0, vertexNum);
                int j = random.Next(0, vertexNum);
                if (GMatrix.Matrix[i, j] != null || i == j) continue;
                int len = random.Next(0, 20);
                AddEdgeWithDirection(i, j, len + "", true);
                _initializedEdges += i + "_" + j + "_" + len + " ";
                edgeNum--;
            }
            _initializedEdges = _initializedEdges.Substring(0, _initializedEdges.Length - 1);
            Vm.Graph.RaiseChangedByTheCustom();
            LogId++;
            Maplogs.Add(new LogVo(LogId, "初始化(" + vertexSize + ")", "", _initializedValues,_initializedEdges));
        }
        /// <summary>
        /// 添加/插入一个顶点
        /// </summary>
        /// <param name="startNode">起点</param>
        /// <param name="endNode">终点</param>
        /// <param name="thisNodeValue">要添加节点的值</param>
        /// <returns>日志操作数</returns>
        public int AddNode(string startNode, string endNode, string thisNodeValue, GraphLayout graphLayout)
        {
            _graphLayout = graphLayout;
            if (startNode.Equals("") && endNode.Equals(""))
            {//添加一个节点
                if (_graphHashtable.Contains(thisNodeValue))
                {
                    throw new Exception("图中已存在节点：" + thisNodeValue);
                }
                AddVNode(thisNodeValue);
                Vm.Graph.RaiseChangedByTheCustom();
                return WritingLogAdd_Remove(1, -1, -1, thisNodeValue, false);
            }
            if (!startNode.Equals("") && !endNode.Equals(""))
            {
                #region 验证处理
                if (_graphHashtable.ContainsKey(thisNodeValue))
                    throw new Exception("图中已存在节点：" + thisNodeValue);
                if (!_graphHashtable.ContainsKey(startNode) || !_graphHashtable.ContainsKey(endNode))
                    throw new Exception("您选择的起点或终点不存在");
                if (!_graphHashtable.ContainsKey(startNode + " " + endNode) && _graphHashtable.ContainsKey(endNode + " " + startNode))
                    throw new Exception("请检查您选择的边的方向，应为：" + endNode + "->" + startNode);
                MapInsertEdges mapInsertEdges = new MapInsertEdges
                {
                    Weight1 = { Text = startNode + "->" + thisNodeValue + "的权值为:" },
                    Weight2 = { Text = thisNodeValue + "->" + endNode + "的权值为:" }
                };
                mapInsertEdges.ShowDialog();
                #endregion 验证处理

                int startToTarget = DialogData.StartToTargetWeight;
                int targetToEnd = DialogData.TargetToEndWeight;
                int startId = GetVNodeIdByData(startNode);
                int endId = GetVNodeIdByData(endNode);
                InsertVNodeBetweenEdge(startNode, endNode, thisNodeValue, startToTarget, targetToEnd);
                Vm.Graph.RaiseChangedByTheCustom();
                return WritingLogAdd_Remove(1, startId, endId, thisNodeValue, false);
            }
            throw new Exception("请选择一条边以插入您要的节点");
        }
        /// <summary>
        /// 删除顶点
        /// </summary>
        /// <param name="data"></param>
        /// <returns>日志操作数</returns>
        public int RemoveNode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new Exception("请输入要删除节点的data值");
            }
            if (!_graphHashtable.ContainsKey(data))
                throw new Exception("您选择的节点不存在！");
            RemoveVNode(data);
            Vm.Graph.RaiseChangedByTheCustom();
            int row = WritingLogAdd_Remove(2, -1, -1, data, false);
            return row;
        }

        /// <summary>
        /// 添加边
        /// </summary>
        /// <param name="startNode">起点</param>
        /// <param name="endNode">终点</param>
        /// <param name="label">权值</param>
        /// <returns>日志操作数</returns>
        public int AddEdge(string startNode, string endNode, string label)
        {
            #region 做一些验证
            if (string.IsNullOrEmpty(label))
            {
                throw new Exception("请输入边的权值");
            }
            if (string.IsNullOrEmpty(startNode))
            {
                throw new Exception("请输入起点的data值");
            }
            if (string.IsNullOrEmpty(endNode))
            {
                throw new Exception("请输入终点的data值");
            }
            if (!_graphHashtable.ContainsKey(startNode) || !_graphHashtable.ContainsKey(endNode))
                throw new Exception("您选择的起点或终点不存在");
            if (_graphHashtable.ContainsKey(startNode + " " + endNode))
                throw new Exception("起点与终点之间已存在一条弧");
            #endregion
            int startId = GetVNodeIdByData(startNode);
            int endId = GetVNodeIdByData(endNode);

            AddEdgeWithDirection(startId, endId, label, true);
            Vm.Graph.RaiseChangedByTheCustom();
            int row = WritingLogAdd_Remove(3, startId, endId, label, false);
            return row;
        }
        /// <summary>
        /// 删除边
        /// </summary>
        /// <param name="startNode">起点</param>
        /// <param name="endNode">终点</param>
        /// <returns>日志操作数</returns>
        public int RemoveEdge(string startNode, string endNode)
        {
            RemoveEdgeWithDirection(startNode, endNode, true);
            Vm.Graph.RaiseChangedByTheCustom();
            int row = WritingLogAdd_Remove(4, GetVNodeIdByData(startNode), GetVNodeIdByData(endNode), null, false);
            return row;
        }
        /// <summary>
        /// 回退操作
        /// </summary>
        /// <param name="_step">步子</param>
        public void BackTo(string _step)
        {
            if (string.IsNullOrEmpty(_step))
                return;
            if (int.Parse(_step) > LogId || int.Parse(_step) <= 0)
            {
                throw new Exception("请输入正确的步数！");
            }
            int step = int.Parse(_step);
            List<LogVo> temp = new List<LogVo>();
            for (int i = 0; i < Maplogs.Count; i++)
            {
                if(Maplogs[i].Action.Contains("初始化"))
                    temp.Add(new LogVo(Maplogs[i].LogId, Maplogs[i].Action, Maplogs[i].Selectdata, Maplogs[i].Data, Maplogs[i].HideString));
                else
                temp.Add(new LogVo(Maplogs[i].LogId, Maplogs[i].Action, Maplogs[i].SelectId, Maplogs[i].TargetId, Maplogs[i].Data));
            }
            Maplogs.Clear();
            Vm.Graph.Clear(true);
            LogId = 0;
            _graphHashtable.Clear();

            for (int i = 0; i < step; i++)
            {
                if (temp[i].Action.Contains("初始化")){
                    Clear();
                    LogInit(temp[i].Data,temp[i].HideString);
                    continue;
                }

                switch (temp[i].Action)
                {
                    case ("插入顶点"):
                        WritingLogAdd_Remove(1, temp[i].SelectId, temp[i].TargetId, temp[i].Data, true);
                        break;
                    case ("删除顶点"):
                        WritingLogAdd_Remove(2, temp[i].SelectId, temp[i].TargetId, temp[i].Data, true);
                        break;
                    case ("添加边"):
                        WritingLogAdd_Remove(3, temp[i].SelectId, temp[i].TargetId, temp[i].Data, true);
                        break;
                    case ("删除边"):
                        WritingLogAdd_Remove(4, temp[i].SelectId, temp[i].TargetId, temp[i].Data, true);
                        break;
                }
            }
            Vm.Graph.RaiseChangedByTheCustom();
        }

        #region 命令行操作
        public void Command_lineOperation(string commandLine)
        {
            try
            {
                if (commandLine != "")
                {
                    string[] cmd = commandLine.ToLower().Split(' ');
                    List<string> cmds = new List<string>(cmd);
                    switch (cmds[0])
                    {
                        case "add":
                            {
                                Regex reg = new Regex(@"^(-1|[0-9]*)$");
                                if (cmds.Count == 8 && (cmds[1].Equals("vertex") || cmds[1].Equals("edge")) && cmds[2].Equals("from") && cmds[3] != null && cmds[5] != null && cmds[4].Equals("to") && (cmds[6].Equals("value") || cmds[6].Equals("weight")) && cmds[7] != null)
                                {
                                    switch (cmds[1])
                                    {
                                        case "vertex":
                                            {
                                                if (cmds[3].Equals("null") && cmds[5].Equals("null"))
                                                {
                                                    cmds[3] = "";
                                                    cmds[5] = "";
                                                }
                                                AddNode(cmds[3], cmds[5], cmds[7], _graphLayout);
                                            }
                                            break;
                                        case "edge":
                                            {

                                                string start = cmds[3];
                                                string end = cmds[5];
                                                AddEdge(start, end, cmds[7]);
                                            }
                                            break;
                                    }
                                }
                                else CommandPrompt();
                            }
                            break;
                        case "delete":
                            {
                                Regex reg = new Regex(@"^[0-9]*$");
                                if (cmds.Count == 4 && cmds[1].Equals("vertex") && cmds[2].Equals("value") && cmds[3] != null)
                                {
                                    RemoveNode(cmds[3]);
                                }
                                else if (cmds.Count == 6 && cmds[1].Equals("edge") && cmds[2].Equals("from") && cmds[3] != null && cmds[4].Equals("to") && cmds[5] != null)
                                {
                                    RemoveEdge(cmds[3], cmds[5]);
                                }
                                else CommandPrompt();
                            }
                            break;
                        case "back":
                            {
                                Regex reg = new Regex(@"^[0-9]*$");
                                if (cmds.Count == 4 && cmds[1].Equals("to") && cmds[2].Equals("step") && reg.IsMatch(cmds[3]))
                                {
                                    BackTo(cmds[3]);
                                }
                                else
                                {
                                    CommandPrompt();
                                }
                            }
                            break;
                        default:
                            CommandPrompt();
                            break;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void CommandPrompt()
        {
            throw new Exception("请输入如下命令：\nAdd vertex from A to B value *\nAdd vertex from null to null value *\nDelete vertex value *\nAdd edge from A to B weight *\nDelete edge from A to B\n Back to step *\n其中A、B为节点的数据\nvalue 为string,weight、step为int");
        }
        #endregion

        public void Clear()
        {
            Vm.Graph.Clear(true);
            _graphHashtable.Clear();
            GMatrix = new GraphMatrix(Maxv);
        }

        #endregion 数据结构对外界所提供的函数

    }

}
