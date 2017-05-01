#Project Crux
----------
Cryptocurrency Algorithmic Trading Framework in C#

###Sub-Projects
####BackTest
Contains HistoricalDataAPI which implements the same interface as other market APIs. This allows us to pass it to any trading strategies and evaluate its performance on past price data.

###Requirements (all libraries can be downloaded from NuGet)
- Visual Studio 2015
- [QuickFIX API](http://quickfixn.org/)
- [WebSocket4Net](http://websocket4net.codeplex.com/)
- [Newtonsoft.Json](http://www.newtonsoft.com/json)

###Exchanges
__Exchange classes must implement MarketAPI interface__

The programm currently supports the following exchanges:
- [OKCoin International REST API](https://www.okcoin.com/rest_getStarted.html)
- [OKCoin International Websocket API](https://www.okcoin.com/ws_getStarted.html)
- [OKCoin International FIX API](https://www.okcoin.com/about/fix_getStarted.html)
- [Bitfinex Websocket API 2.0](https://bitfinex.readme.io/v2/docs/ws-general)

### Trading Algorithms/Strategies
__Strategies must inherit the TradeStrategy abstract class__
For obvious reasons, strategies are not included

### Sub-Folders
__Scripts__
MATLAB and Python scripts used to quickly backtest simple strategy ideas

__Data__
Historical data files (not uploaded due to large size), data processing scripts, 