using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using integrateOfDataStructure.Dialogs;

namespace integrateOfDataStructure
{
    /// <summary>
    /// MapInsertEdges.xaml 的交互逻辑
    /// </summary>
    public partial class MapInsertEdges : Window
    {
        public MapInsertEdges()
        {
            InitializeComponent();
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"^\d*$");
            if (!regex.IsMatch(TxtStartTargetWeight.Text) || !regex.IsMatch(TxtTargetEndWeight.Text))
            {
                MessageBox.Show("请输入整数！");
            }
            DialogData.StartToTargetWeight = int.Parse(TxtStartTargetWeight.Text);
            DialogData.TargetToEndWeight = int.Parse(TxtTargetEndWeight.Text);
            this.Close();
        }
    }
}
