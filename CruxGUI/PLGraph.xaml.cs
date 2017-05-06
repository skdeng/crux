using Crux;
using LiveCharts.Defaults;
using System;
using System.Windows.Controls;

namespace CruxGUI
{
    /// <summary>
    /// Interaction logic for PLGraph.xaml
    /// </summary>
    public partial class PLGraph : UserControl
    {
        PLGraphVM PLGraphModel;

        /// <summary>
        /// One data point every hour
        /// </summary>
        public PLGraph()
        {
            InitializeComponent();
            PLGraphModel = new PLGraphVM();
            DataContext = PLGraphModel;

            PLGraphModel.DateFormatter = point => new DateTime((long)point).ToString("MM/dd HH:mm");
            PLGraphModel.AxisStart = DateTime.Now.AddHours(-24).Ticks;
            PLGraphModel.AxisEnd = DateTime.Now.AddHours(2).Ticks;
        }

        public void SetStatistics(Statistics strategyStats)
        {
            strategyStats.SnapshotEvent += delegate (object sender, PortfolioSnapshot snapshot)
            {
                AddPL(snapshot.CumulativePL, snapshot.BenchmarkCumulativePL, snapshot.Time);
            };

            foreach (var snapshot in strategyStats.Snapshots)
            {
                AddPL(snapshot.CumulativePL, snapshot.BenchmarkCumulativePL, snapshot.Time);
            }
        }

        public void AddPL(double strategyPL, double benchmarkPL, DateTime time)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                PLGraphModel.StrategyPL.Values.Add(new ObservablePoint(time.Ticks, strategyPL));
                PLGraphModel.BenchmarkPL.Values.Add(new ObservablePoint(time.Ticks, benchmarkPL));
            });
        }

        public void Clear()
        {
            PLGraphModel.Clear();
        }
    }
}
