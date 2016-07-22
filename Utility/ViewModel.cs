using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Graphviz4Net.Graphs;
using Graphviz4Net.WPF;
using ViewModelUtility;

namespace integrateOfDataStructure.Utility
{
    public class ViewModelNode : INotifyPropertyChanged,IViewModelNode
    {
        private readonly Graph<ViewModelNode> _graph;
        private string _name;
        private int _id = -1;//默认返回值：""
        private int _bf=int.MinValue;//默认返回值：""

        public ViewModelNode(Graph<ViewModelNode> graph)
        {
            _graph = graph;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                }
            }
        }

        public string Id
        {
            get
            {
                if (_id == -1) return "";
                return _id + "";
            }
            set { _id = int.Parse(value); }
        }

        public string Bf
        {
            get
            {
                if (_bf == int.MinValue) return "";
                return _bf + "";
            }
            set { _bf = int.Parse(value); }
        }

        public Graph<ViewModelNode> Graph
        {
            get { return _graph; }
        }

//二叉树孩子节点位置的标识

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class DiamondArrow
    {
    }

    public class Arrow
    {
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public Graph<ViewModelNode> Graph { get; set; }

        public ViewModel()
        {
            var graph = new Graph<ViewModelNode>();
            Graph = graph;
            Graph.Changed += GraphChanged;
        }

        /// <summary>
        /// 不提交地添加节点
        /// </summary>
        /// <param name="name"></param>
        public void AddVertex(string name)
        {
            ViewModelNode bt = new ViewModelNode(Graph) { Name = name };
            Graph.AddVertex(bt, true);
        }
        /// <summary>
        /// 不提交地添加节点
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public void AddVertex(string name, int id)
        {
            ViewModelNode bt = new ViewModelNode(Graph) { Name = name, Id = id + "" };
            Graph.AddVertex(bt, true);
        }

        /// <summary>
        /// 不提交地添加节点
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bf"></param>
        /// <param name="addWithBf"></param>
        public void AddVertex(string name, int bf,bool addWithBf)
        {
            ViewModelNode bt = new ViewModelNode(Graph) { Name = name, Bf = bf + "" };
            Graph.AddVertex(bt, true);
        }

        /// <summary>
        /// 不提交地添加边
        /// </summary>
        /// <param name="a">起点</param>
        /// <param name="b">终点</param>
        public void AddAdge(string a, string b)
        {
            Graph.AddEdge(new Edge<ViewModelNode>(GetVmVertex(a), GetVmVertex(b)), true);
        }
        /// <summary>
        /// 不提交地添加边
        /// </summary>
        /// <param name="a">起点</param>
        /// <param name="b">终点</param>
        /// <param name="len">权值</param>
        public void AddAdge(string a, string b, string len)
        {
            Edge<ViewModelNode> edge = new Edge<ViewModelNode>(GetVmVertex(a),
                GetVmVertex(b), new Arrow())
            {
                Label = len
            };
            Graph.AddEdge(edge, true);
        }
        /// <summary>
        /// 带提交地删除节点
        /// </summary>
        /// <param name="p"></param>
        public void RemoveNode(string p)
        {
            Graph.RemoveVertexWithEdges(GetVmVertex(p));
        }
        /// <summary>
        /// 不提交地删除节点
        /// </summary>
        /// <param name="p">节点值</param>
        /// <param name="withoutRaiseChange">不提交标志</param>
        public void RemoveNode(string p, bool withoutRaiseChange)
        {
            Graph.RemoveVertexWithEdges(GetVmVertex(p), true);
        }

        /// <summary>
        /// 不提交地修改节点
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        /// <param name="bf">节点平衡因子</param>
        /// <param name="id">节点id</param>
        public void UpdateNode(string oldValue,string newValue,int bf,int id)
        {
            ViewModelNode node=GetVmVertex(oldValue);
            node.Name = newValue;
            node.Id = id.ToString();
            node.Bf = bf.ToString();
        }

        /// <summary>
        /// 带提交地删除边
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public void RemoveEdge(string source, string destination)
        {
            Graph.RemoveEdge(Graph.FindEdge(GetVmVertex(source), GetVmVertex(destination)));
        }
        /// <summary>
        /// 不提交地删除边
        /// </summary>
        /// <param name="source">起点</param>
        /// <param name="destination">终点</param>
        /// <param name="whioutRaiseChange"></param>
        public void RemoveEdge(string source, string destination, bool whioutRaiseChange)
        {
            Graph.RemoveEdge(Graph.FindEdge(GetVmVertex(source), GetVmVertex(destination)), true);
        }

        public void AddMyArrow(Point pStart, Point pEnd,int zIndex,GraphLayout graphLayout)
        {
            graphLayout.AddMyArrow(pStart,pEnd,zIndex);
        }

        public void AddMyArrow(Point start, Point end,Brush brush,double strokeThickness, int zIndex,GraphLayout graphLayout)
        {
            graphLayout.AddMyArrow(start, end,brush,strokeThickness, zIndex);
        }

        public ViewModelNode GetVmVertex(string name)
        {
            return Graph.AllVertices.First(x => string.CompareOrdinal(x.Name, name) == 0);
        }

        #region 我所添加的对所有数据结构的各种Graphviz操作
        /// <summary>
        /// 获取顶点在Canvas上的位置
        /// </summary>
        /// <param name="data"></param>
        /// <param name="graphLayout"></param>
        /// <returns></returns>
        public Point GetVertexPosition(string data,GraphLayout graphLayout)
        {
            return graphLayout.GetVertexPosition(data);
        }

        public Point GetAPointOnEdge(string start,string end, GraphLayout graphLayout)
        {
            return graphLayout.GetAPointOnEdge(start,end);
        }
        
        /// <summary>
        /// 往指定位置添加顶点
        /// </summary>
        /// <param name="data">节点值</param>
        /// <param name="point">节点位置</param>
        /// <param name="graphLayout">GraphLayout画布对象</param>
        public void AddMyVertex(string data, Point point, GraphLayout graphLayout)
        {
            graphLayout.AddMyNode(data, point);
        }


        /// <summary>
        /// 在指定位置上画一个箭头
        /// </summary>
        /// <param name="point"></param>
        /// <param name="glLayout"></param>
        public void AddMyXInList(Point point, GraphLayout glLayout)
        {
            glLayout.AddMyXInList(point);
        }
        /// <summary>
        /// 在指定位置上画一个箭头
        /// </summary>
        /// <param name="point"></param>
        /// <param name="glLayout"></param>
        public void AddMyX(Point point, GraphLayout glLayout)
        {
            glLayout.AddMyX(point);
        }

        public void AddMyX(Point point, double width, double height, FontFamily fontFamily, Brush brush, double fontSize,
            GraphLayout glLayout)
        {
            glLayout.AddMyX(point,width,height,fontFamily,brush,fontSize);
        }

        /// <summary>
        /// 画一条线
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="color">直线的颜色</param>
        /// <param name="strokeThickness">线的厚度</param>
        /// <param name="glLayout">Graphviz对象</param>
        public void AddMyLine(Point start, Point end, Color color,int strokeThickness, GraphLayout glLayout)
        {
            glLayout.AddMyLine(start, end, color, strokeThickness);
        }

        /// <summary>
        /// 设置画布上节点的ID
        /// </summary>
        /// <param name="value">节点值</param>
        /// <param name="id">节点ID</param>
        public void SetId(string value, int id)
        {
            GetVmVertex(value).Id = id + "";
        }

        /// <summary>
        /// 设置画布上节点的BF
        /// </summary>
        /// <param name="value">节点值</param>
        /// <param name="bf">节点BF</param>
        public void SetBf(int value, int bf)
        {
            GetVmVertex(value.ToString()).Bf = bf.ToString();
        }

        /// <summary>
        /// ViewModel遍历实现
        /// </summary>
        /// <param name="order">遍历顺序</param>
        /// <param name="glLayout">GraphLayout画布对象</param>
        /// <param name="edges">根据遍历顺序得到的边</param>
        /// <param name="orderString">遍历结果</param>
        public void ViewModelTraversal(List<string> edges,List<List<string>> orderString,string order,GraphLayout glLayout)
        {
            glLayout.Traversal(edges, orderString, order);
        }

        /// <summary>
        /// ViewModel寻找路径实现
        /// </summary>
        /// <param name="pathString">遍历结果</param>
        /// <param name="witchStructure"></param>
        /// <param name="glLayout">GraphLayout画布对象</param>
        /// <param name="edges">根据遍历顺序得到的边</param>
        public void ViewModelFindThePath(List<string> edges, List<List<string>> pathString, string witchStructure, GraphLayout glLayout)
        {
            glLayout.FindThePath(edges, pathString, witchStructure);
        }

        public void AddRotateArrow(string left, string root, string right, string rotateMode, GraphLayout glLayout)
        {
            glLayout.AddRotateArrow(left, root, right, rotateMode);
        }

        public void AddQuadraticBezierArrow(Point control, Point start, Point end,Brush brush,double thickness,GraphLayout glLayout)
        {
            glLayout.AddQuadraticBezierArrow(control,start,end,brush,thickness);
        }

        public void AddArrowLineWithText(Point start, Point end, Brush brush, double thickness, string text, TextAlignment textAlignment, GraphLayout glLayout)
        {
            glLayout.AddArrowLineWithText(start,end,brush,thickness,text,textAlignment);
        }

        #endregion 我所添加的对所有数据结构的各种Graphviz操作

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

        
    }
}
