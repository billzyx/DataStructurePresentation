using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace integrateOfDataStructure
{
    /// <summary>
    /// list.xaml 的交互逻辑
    /// </summary>
    public partial class NewWindow : Window
    {
        public NewWindow()
        {
            InitializeComponent();
        }

        string dataStructure = null;
        int page = 0;

        public NewWindow(double x,double y,string dataStructure)
        {
            InitializeComponent();
            //启用‘Manual’属性后，可以手动设置窗体的显示位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = x;
            this.Left = y;
            this.dataStructure = dataStructure;
            NewWin.Title = dataStructure;
            Lable_Title.Content = dataStructure;
            
            switch (dataStructure)
            {
                case "概述":
                    
                    break;
                case "线性表":
                    TiYanshi.Visibility = Visibility.Visible;
                    UserControlSinglyLinkedList ucsll = new UserControlSinglyLinkedList();
                    Item1.Children.Add(ucsll);
                    break;
                case "栈和队列":
                    
                    break;
                case "二叉树":
                    TiYanshi.Visibility = Visibility.Visible;
                    UserControlBTree ucbt = new UserControlBTree();
                    Item1.Children.Add(ucbt);
                    break;
                case "平衡二叉树":
                    TiYanshi.Visibility = Visibility.Visible;
                    UserControlAvlTree ucavlt = new UserControlAvlTree();
                    Item1.Children.Add(ucavlt);
                    break;
                case "多叉树":
                    UserControlMTree ucmt = new UserControlMTree();
                    TiYanshi.Visibility = Visibility.Visible;
                    Item1.Children.Add(ucmt);
                    break;
                case "图":
                    TiYanshi.Visibility = Visibility.Visible;
                    UserControlMap ucm = new UserControlMap();
                    Item1.Children.Add(ucm);
                    break;
            }

            LoadNextPage();
        }

        private void LoadNextPage()
        {
            page += 1;
            string text = AppConfig.GetAppConfig(dataStructure + page);
            if(text == null)
            {
                MessageBox.Show("到底了！");
                page -= 1;
            }
            else
            {
                TextBlock_Main_Text.Text = text;
                Image_show.Source = new BitmapImage(new Uri("pic/" + dataStructure + page + ".png", UriKind.Relative)); 
            }          
        }

        private void LoadPreviousPage()
        {
            page -= 1;
            string text = AppConfig.GetAppConfig(dataStructure + page);
            if (text == null)
            {
                MessageBox.Show("到顶了！");
                page += 1;
            }
            else
            {
                TextBlock_Main_Text.Text = text;
                Image_show.Source = new BitmapImage(new Uri("pic/" + dataStructure + page + ".png", UriKind.Relative));
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow(this.Top, this.Left);
            this.Close();
            mw.ShowDialog();
        }

        private void Button_Next_Page_Click(object sender, RoutedEventArgs e)
        {
            LoadNextPage();
        }

        private void Button_Previous_Page_Click(object sender, RoutedEventArgs e)
        {
            LoadPreviousPage();
        }
    }
}
