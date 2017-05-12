using Crux;
using Crux.BasicStrategy;
using Crux.BfxREST;
using Crux.BfxWS;
using Crux.OkcoinFIX;
using Crux.OkcoinREST;
using Crux.OkcoinWS;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CruxGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowVM MainWindowViewModel;

        private static string StatisticsLogFile = "stats.log";

        private IMarketAPI MarketTerminal;
        private string MarketKeyFile;

        private Statistics StrategyStatistics;
        private TradeStrategy Strategy;

        public MainWindow()
        {
            MainWindowViewModel = new MainWindowVM();
            InitializeComponent();
            DataContext = MainWindowViewModel;
            Log.LogStream = GUIConsoleController;
            SetDefaultValues();
        }

        private void SetDefaultValues()
        {
            MainWindowViewModel.StartButtonText = "Start Strategy";

            LoadStatisticsFile();

            // Set market list
            MarketListBox.Items.Clear();
            MainWindowViewModel.MarketAPIList = Enum.GetNames(typeof(EMarketAPI)).Select(s => s.Replace("_", " "));

            // Default log level 2
            MenuItemLogLevel2.IsChecked = true;
        }

        private void LoadStatisticsFile()
        {
            StrategyStatistics = new Statistics();
            // Import existing statistics and apply them to the graph
            if (File.Exists(StatisticsLogFile))
            {
                StrategyStatistics.Import(StatisticsLogFile);
            }
            PLGraphController.SetStatistics(StrategyStatistics);
            if (StrategyStatistics.Snapshots.Count > 0)
            {
                var snapshot = StrategyStatistics.Snapshots.Last();
                MainWindowViewModel.PortfolioValue = snapshot.PortfolioValue;
                MainWindowViewModel.PortfolioFiat = snapshot.Fiat;
                MainWindowViewModel.PortfolioAsset = snapshot.Security;
                MainWindowViewModel.AssetPrice = snapshot.SecurityPrice;
                MainWindowViewModel.BenchmarkPrice = snapshot.BenchmarkPrice;
            }
            else
            {
                MainWindowViewModel.PortfolioValue = 0;
                MainWindowViewModel.PortfolioFiat = 0;
                MainWindowViewModel.PortfolioAsset = 0;
                MainWindowViewModel.AssetPrice = 0;
                MainWindowViewModel.BenchmarkPrice = 0;
            }
            StrategyStatistics.SnapshotEvent += delegate (object sender, PortfolioSnapshot snapshot)
            {
                MainWindowViewModel.PortfolioValue = snapshot.PortfolioValue;
                MainWindowViewModel.PortfolioFiat = snapshot.Fiat;
                MainWindowViewModel.PortfolioAsset = snapshot.Security;
                MainWindowViewModel.AssetPrice = snapshot.SecurityPrice;
                MainWindowViewModel.BenchmarkPrice = snapshot.BenchmarkPrice;
            };
        }

        private void StartTrading()
        {
            // Setup Market API
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".txt";
            fileDialog.Filter = "Text documents (.txt)|*.txt";

            var result = fileDialog.ShowDialog();
            if (result == true)
            {
                MarketKeyFile = fileDialog.FileName;
            }
            else
            {
                StartButton.IsChecked = false;
                return;
            }
            MarketTerminal = CreateMarketAPI();
            if (MarketTerminal == null)
            {
                StartButton.IsChecked = false;
                return;
            }

            Thread.Sleep(1000);
            // Setup Trade Strategy
            if (Strategy == null)
            {
                Strategy = new MeanReversalStrategy(MarketTerminal, TimeSpan.FromMinutes(15), TimePeriod.ONE_HOUR, 24, StrategyStatistics);
            }
            Strategy.Start(true);
            MarketListBox.IsEnabled = false;
        }

        private void StopTrading()
        {
            Strategy?.Stop();
            MarketListBox.IsEnabled = true;
        }

        private IMarketAPI CreateMarketAPI()
        {
            var selected = (string)MarketListBox.SelectedItem;
            if (selected == null || string.IsNullOrWhiteSpace(MainWindowViewModel.TradeSymbol))
            {
                return null;
            }
            var selectedEnum = (EMarketAPI)Enum.Parse(typeof(EMarketAPI), selected.Replace(" ", "_"));
            switch (selectedEnum)
            {
                case EMarketAPI.Bitfinex_REST:
                    return new BfxMarketRESTAPI(MarketKeyFile, MainWindowViewModel.TradeSymbol);
                case EMarketAPI.Bitfinex_Websocket:
                    return new BfxMarketWSAPI(MarketKeyFile, MainWindowViewModel.TradeSymbol);
                case EMarketAPI.Okcoin_FIX:
                    return new OKCMarketFIXAPI(MarketKeyFile, MainWindowViewModel.TradeSymbol, true, true);
                case EMarketAPI.Okcoin_REST:
                    return new OKCMarketRESTAPI(MarketKeyFile, MainWindowViewModel.TradeSymbol);
                case EMarketAPI.Okcoin_Websocket:
                    return new OKCMarketWSAPI(MarketKeyFile, MainWindowViewModel.TradeSymbol, true, true);
                default:
                    return null;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopTrading();
            StrategyStatistics.Export(StatisticsLogFile);
        }

        private void StartButton_Checked(object sender, RoutedEventArgs e)
        {
            if (Strategy?.Trading == true)
            {
                return;
            }

            MainWindowViewModel.StartButtonText = "Stop Strategy...";
            Log.Write($"Started trading on {(string)MarketListBox.SelectedItem}", 1);
            StartTrading();
        }

        private void StartButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Strategy == null || !Strategy.Trading)
            {
                MainWindowViewModel.StartButtonText = "Start Strategy";
                return;
            }

            var result = MessageBox.Show(this, "Stop current strategy?", "Stop Current Strategy", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                StartButton.IsChecked = true;
                return;
            }

            MainWindowViewModel.StartButtonText = "Start Strategy";
            Log.Write("Stopped trading", 1);
            StopTrading();
        }

        private void LogLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.LogLevel = ((ListBox)sender).SelectedIndex;
        }

        private void NewStatisticsFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.AddExtension = true;
            fileDialog.DefaultExt = ".log";
            fileDialog.Filter = "Log file (.log)|*.log";

            var result = fileDialog.ShowDialog();
            if (result == true)
            {
                StatisticsLogFile = fileDialog.FileName;
                LoadStatisticsFile();
            }
        }

        private void OpenStatisticsFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".log";
            fileDialog.Filter = "Log file (.log)|*.log";

            var result = fileDialog.ShowDialog();
            if (result == true)
            {
                StatisticsLogFile = fileDialog.FileName;
                LoadStatisticsFile();
            }
        }

        private void SaveStatisticsFile_Click(object sender, RoutedEventArgs e)
        {
            StrategyStatistics.Export(StatisticsLogFile);
        }

        private void ClearStatistics_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this, "Are you sure you want to clear current series of statistics?", "Clear statistics", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                StrategyStatistics.Clear();
                MainWindowViewModel.PortfolioValue = 0;
                MainWindowViewModel.PortfolioFiat = 0;
                MainWindowViewModel.PortfolioAsset = 0;
                MainWindowViewModel.AssetPrice = 0;

                PLGraphController.Clear();
            }
        }

        private void MenuItemLogLevel0_Checked(object sender, RoutedEventArgs e)
        {
            Log.LogLevel = 0;
        }

        private void MenuItemLogLevel1_Checked(object sender, RoutedEventArgs e)
        {
            Log.LogLevel = 1;
        }

        private void MenuItemLogLevel2_Checked(object sender, RoutedEventArgs e)
        {
            Log.LogLevel = 2;
        }

        private void MenuItemLogLevel3_Checked(object sender, RoutedEventArgs e)
        {
            Log.LogLevel = 3;
        }
    }
}
