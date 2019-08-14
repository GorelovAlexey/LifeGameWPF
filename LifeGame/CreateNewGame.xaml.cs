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

namespace LifeGame
{
    /// <summary>
    /// Логика взаимодействия для CreateNewGame.xaml
    /// </summary>
    public partial class CreateNewGame : Window
    {
        public CreateNewGame()
        {
            InitializeComponent();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            int width = 2;
            int height = 2;
            bool error = false;
            string errMsg = "";
            try
            {
                width = int.Parse(Width.Text);
            }
            catch (Exception)
            {
                Width.Text = "";
                error = true;
                errMsg += "Width";
            }

            try
            {
                height = int.Parse(Height.Text);
            }
            catch (Exception)
            {
                Height.Text = "";
                error = true;
                errMsg += ", Height";
            }
            
            bool wrapping = (bool)WrapEdges.IsChecked;


            if (width < 2 || width > 5000)
            {
                error = true;
                errMsg += "Width (needs to be in (2,5000) range)";
            }

            if (height < 2 || width > 5000)
            {
                error = true;
                errMsg += "Height (needs to be in (2,5000) range)";
            }

            if (error)
            {
                MessageBox.Show($"Wrong values for {errMsg}");
            }
            else
            {
                ((MainWindow)Application.Current.MainWindow).SetupNewGame(width, height, wrapping);
                Close();
            }


        }
    }
}
