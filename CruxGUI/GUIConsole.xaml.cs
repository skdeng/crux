using Crux;
using System.Windows.Controls;

namespace CruxGUI
{
    /// <summary>
    /// Interaction logic for GUIConsole.xaml
    /// </summary>
    public partial class GUIConsole : ScrollViewer, ILogStream
    {
        ConsoleContentVM ConsoleLog;

        public GUIConsole()
        {
            ConsoleLog = new ConsoleContentVM();
            InitializeComponent();
            ConsoleContentList.Items.Clear();
            DataContext = ConsoleLog;
            Write("Starting console...", 2);
        }

        public void Write(string msg, int level)
        {
            string color = "White";
            if (level == 1)
            {
                color = "Yellow";
            }
            else if (level == 0)
            {
                color = "Red";
            }
            App.Current?.Dispatcher.Invoke(delegate
            {
                ConsoleLog.ConsoleOutput.Add(new ConsoleEntry() { Text = msg, Color = color });
                ScrollToBottom();
            });
        }
    }
}
