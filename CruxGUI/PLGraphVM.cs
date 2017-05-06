using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Windows.Media;

namespace CruxGUI
{
    public class PLGraphVM : VMBase
    {
        public SeriesCollection DataSeries { get; set; }

        public LineSeries StrategyPL { get; set; }

        public LineSeries BenchmarkPL { get; set; }

        private double _AxisStart { get; set; }
        public double AxisStart
        {
            get { return _AxisStart; }
            set { _AxisStart = value; OnPropertyChanged("AxisStart"); }
        }

        private double _AxisEnd { get; set; }
        public double AxisEnd
        {
            get { return _AxisEnd; }
            set { _AxisEnd = value; OnPropertyChanged("AxisEnd"); }
        }

        private Func<double, string> _DateFormatter;
        public Func<double, string> DateFormatter
        {
            get { return _DateFormatter; }
            set { _DateFormatter = value; OnPropertyChanged("DateFormatter"); }
        }

        public PLGraphVM()
        {
            DataSeries = new SeriesCollection();
            StrategyPL = new LineSeries()
            {
                Title = "Strategy P/L",
                Values = new ChartValues<ObservablePoint>(),
                Fill = Brushes.Transparent,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                PointGeometrySize = 6
            };
            BenchmarkPL = new LineSeries()
            {
                Title = "Benchmark P/L",
                Values = new ChartValues<ObservablePoint>(),
                Fill = Brushes.Transparent,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2,
                PointGeometrySize = 6
            };

            DataSeries.Add(StrategyPL);
            DataSeries.Add(BenchmarkPL);
        }

        public void Clear()
        {
            StrategyPL.Values.Clear();
            BenchmarkPL.Values.Clear();
        }
    }
}
