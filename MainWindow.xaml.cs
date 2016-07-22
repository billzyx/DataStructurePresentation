using System.Windows;

namespace integrateOfDataStructure
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //启用‘Manual’属性后，可以手动设置窗体的显示位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = 10;
            this.Left = 80;
        }
        
        public MainWindow(double x,double y)
        {
            InitializeComponent();
            //启用‘Manual’属性后，可以手动设置窗体的显示位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = x;
            this.Left = y;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //UserControlConcept ucc=new UserControlConcept();
            //UserControlSinglyLinkedList ucsll = new UserControlSinglyLinkedList();
            //UserControlBTree ucbt = new UserControlBTree();
            //UserControlAvlTree ucavlt=new UserControlAvlTree();
            //UserControlMTree ucmt = new UserControlMTree();
            //UserControlMap ucm = new UserControlMap();
            //Item0.Children.Add(ucc);
            //Item1.Children.Add(ucsll);
            //Item2.Children.Add(ucbt);
            //Item3.Children.Add(ucavlt);
            //Item4.Children.Add(ucmt);
            //Item5.Children.Add(ucm);
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            NewWindow nw=new NewWindow(this.Top, this.Left,"概述");
            WelcomWindow.Close();
            nw.ShowDialog();
            

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NewWindow nw = new NewWindow(this.Top, this.Left, "线性表");
            WelcomWindow.Close();
            nw.ShowDialog();
            
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            NewWindow nw = new NewWindow(this.Top, this.Left, "栈和队列");
            WelcomWindow.Close();
            nw.ShowDialog();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            NewWindow nw = new NewWindow(this.Top, this.Left, "二叉树");
            WelcomWindow.Close();
            nw.ShowDialog();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            NewWindow nw = new NewWindow(this.Top, this.Left, "平衡二叉树");
            WelcomWindow.Close();
            nw.ShowDialog();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            NewWindow nw = new NewWindow(this.Top, this.Left, "多叉树");
            WelcomWindow.Close();
            nw.ShowDialog();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            NewWindow nw = new NewWindow(this.Top, this.Left, "图");
            WelcomWindow.Close();
            nw.ShowDialog();
        }

    }
}
