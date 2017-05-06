using System;
using System.Windows;

namespace Crux
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string WindowTitle
        {
            get
            {
                return $"Project Crux - {DateTime.Now}";
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
