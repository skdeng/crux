using System.Collections.Generic;

namespace CruxGUI
{
    class MainWindowVM : VMBase
    {
        private string _StartButtonText;
        public string StartButtonText
        {
            get { return _StartButtonText; }
            set { _StartButtonText = value; OnPropertyChanged("StartButtonText"); }
        }

        private string _KeyFile;
        public string KeyFile
        {
            get { return KeyFile; }
            set { _KeyFile = value; }
        }

        private double _PortfolioValue;
        public double PortfolioValue
        {
            get { return _PortfolioValue; }
            set { _PortfolioValue = value; OnPropertyChanged("PortfolioValue"); }
        }

        private double _PortfolioFiat;
        public double PortfolioFiat
        {
            get { return _PortfolioFiat; }
            set { _PortfolioFiat = value; OnPropertyChanged("PortfolioFiat"); }
        }

        private double _PortfolioAsset;
        public double PortfolioAsset
        {
            get { return _PortfolioAsset; }
            set { _PortfolioAsset = value; OnPropertyChanged("PortfolioAsset"); }
        }

        private double _AssetPrice;
        public double AssetPrice
        {
            get { return _AssetPrice; }
            set { _AssetPrice = value; OnPropertyChanged("AssetPrice"); }
        }

        private IEnumerable<string> _MarketAPIList;
        public IEnumerable<string> MarketAPIList
        {
            get { return _MarketAPIList; }
            set { _MarketAPIList = value; OnPropertyChanged("MarketAPIList"); }
        }

        public string TradeSymbol { get; set; }

        public MainWindowVM()
        {
        }
    }
}
