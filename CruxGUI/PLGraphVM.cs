using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using System;
using System.Windows.Media;

namespace CruxGUI
{
    public class PLGraphVM : VMBase
    {
        public SeriesCollection DataSeries { get; set; }

        public GLineSeries StrategyPL { get; set; }

        public GLineSeries BenchmarkPL { get; set; }

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
            var strategyPLValue = new GearedValues<ObservablePoint>();
            strategyPLValue.WithQuality(Quality.Highest);
            StrategyPL = new GLineSeries()
            {
                Title = "Strategy P/L",
                Values = strategyPLValue,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                PointGeometry = null
            };
            var benchmarkPLValue = new GearedValues<ObservablePoint>();
            benchmarkPLValue.WithQuality(Quality.Highest);
            BenchmarkPL = new GLineSeries()
            {
                Title = "Benchmark P/L",
                Values = benchmarkPLValue,
                Fill = Brushes.Transparent,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2,
                PointGeometry = null
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
