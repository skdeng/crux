using Crux;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace CruxGUI
{
    /// <summary>
    /// Interaction logic for GUIConsole.xaml
    /// </summary>
    public partial class GUIConsole : UserControl, ILogStream
    {
        private ObservableCollection<ConsoleEntry> AllConsoleLogs;
        private ConsoleContentVM ConsoleLog;
        private bool ConsoleScrollToBottom;

        private int MaxLogSize = 10000;

        public GUIConsole()
        {
            AllConsoleLogs = new ObservableCollection<ConsoleEntry>();
            ConsoleLog = new ConsoleContentVM();
            InitializeComponent();
            ConsoleContentList.Items.Clear();
            DataContext = ConsoleLog;
            Write("Starting console...", 2);
            MenuItemScrollToBottom.IsChecked = true;
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
                AllConsoleLogs.Add(new ConsoleEntry() { Text = msg, Color = color });
                if (AllConsoleLogs.Count > MaxLogSize)
                {
                    AllConsoleLogs.RemoveAt(0);
                }
                if (msg.IndexOf(FilterTextBox.Text) >= 0)
                {
                    ConsoleLog.ConsoleOutput.Add(new ConsoleEntry() { Text = msg, Color = color });
                    if (ConsoleLog.ConsoleOutput.Count > MaxLogSize)
                    {
                        ConsoleLog.ConsoleOutput.RemoveAt(0);
                    }
                }
                if (ConsoleScrollToBottom)
                {
                    ConsoleScroller.ScrollToBottom();
                }
            });
        }

        private void ClearConsole_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ConsoleLog.ConsoleOutput.Clear();
        }

        private void ScrollToBottom_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ConsoleScrollToBottom = true;
        }

        private void ScrollToBottom_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ConsoleScrollToBottom = false;
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleLog.ConsoleOutput = new ObservableCollection<ConsoleEntry>(AllConsoleLogs.Where(log => log.Text.IndexOf(FilterTextBox.Text) >= 0).ToList());
        }
    }
}
