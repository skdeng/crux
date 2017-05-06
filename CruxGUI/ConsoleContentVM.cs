using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CruxGUI
{
    public class ConsoleContentVM : VMBase, INotifyPropertyChanged
    {
        private ObservableCollection<ConsoleEntry> _ConsoleOutput = new ObservableCollection<ConsoleEntry>();
        public ObservableCollection<ConsoleEntry> ConsoleOutput
        {
            get
            {
                return _ConsoleOutput;
            }
            set
            {
                _ConsoleOutput = value;
                OnPropertyChanged("ConsoleOutput");
            }
        }
    }

    public class ConsoleEntry
    {
        public string Text { get; set; }
        public string Color { get; set; }
    }
}
