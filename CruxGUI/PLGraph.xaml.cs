using Crux;
using LiveCharts.Defaults;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CruxGUI
{
    /// <summary>
    /// Interaction logic for PLGraph.xaml
    /// </summary>
    public partial class PLGraph : UserControl
    {
        private PLGraphVM PLGraphModel;
        private LiveCharts.ChartPoint Selected = null;
        private Statistics StrategyStatistics;
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
            StrategyStatistics = strategyStats;

            PLGraphModel.StrategyPL.Values.Clear();
            PLGraphModel.BenchmarkPL.Values.Clear();
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

        private void PLChart_DataClick(object sender, LiveCharts.ChartPoint chartPoint)
        {
            if (Selected != null)
            {

                var firstTime = Math.Min(chartPoint.X, Selected.X);
                var secondTime = Math.Max(chartPoint.X, Selected.X);

                var firstSnapshotIndex = StrategyStatistics.Snapshots.FindIndex(s => s.Time.Ticks == firstTime);
                var secondSnapshotIndex = StrategyStatistics.Snapshots.FindIndex(s => s.Time.Ticks == secondTime);
                var firstSnapshot = StrategyStatistics.Snapshots[firstSnapshotIndex];
                var secondSnapshot = StrategyStatistics.Snapshots[secondSnapshotIndex];

                var selectedSnapshots = StrategyStatistics.Snapshots.GetRange(firstSnapshotIndex, secondSnapshotIndex - firstSnapshotIndex + 1);
                var avgTime = 0;
                for (int i = firstSnapshotIndex; i < secondSnapshotIndex; i++)
                {
                    avgTime += (int)(StrategyStatistics.Snapshots[i + 1].Time - StrategyStatistics.Snapshots[i].Time).TotalSeconds;
                }
                avgTime /= (secondSnapshotIndex - firstSnapshotIndex);
                var periodsPerYear = (int)(TimeSpan.FromDays(365).TotalSeconds / avgTime);
                var sharpeRatio = Statistics.SharpeRatio(periodsPerYear, selectedSnapshots.Select(s => s.PL), selectedSnapshots.Select(s => s.BenchmarkPL));

                var relativePL = (1 + secondSnapshot.CumulativePL) / (1 + firstSnapshot.CumulativePL) - 1;
                var relativeBenchmarkPL = (1 + secondSnapshot.BenchmarkCumulativePL) / (1 + firstSnapshot.BenchmarkCumulativePL) - 1;
                var absolutePL = secondSnapshot.PortfolioValue - firstSnapshot.PortfolioValue;

                string infoString = $"Relative Strategy PL: {relativePL}\nRelative Benchmark PL: {relativeBenchmarkPL}\nPortfolio Value Change: {absolutePL}\nSharpe Ratio: {sharpeRatio}";
                new Thread(new ThreadStart(delegate
                {
                    MessageBox.Show(infoString, "Relative information", MessageBoxButton.OK, MessageBoxImage.Information);
                })).Start();

                Selected = null;
            }
            else
            {
                Selected = chartPoint;
            }
        }
    }
}
