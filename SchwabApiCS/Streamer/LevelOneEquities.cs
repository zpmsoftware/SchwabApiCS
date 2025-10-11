// <copyright file="LevelOneEquities.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System.ComponentModel;
using System.Runtime.CompilerServices;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.LevelOneEquitiesService;


namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class LevelOneEquitiesService : Service
        {
            /// <summary>
            /// called after a LevelOneEquities request has been received and processed.
            /// One or multiple values in List<LevelOneEquity> were changed
            /// </summary>
            /// <param name="data"></param>
            public delegate void EquitiesCallback(List<LevelOneEquity> data);

            /// <summary>
            /// called when a specific LevelOneEquity was changed.
            /// </summary>
            /// <param name="data"></param>
            public delegate void LevelOneEquityCallback(LevelOneEquity data);


            private List<LevelOneEquity> Data = new List<LevelOneEquity>();
            private EquitiesCallback? equitiesCallback = null;
           
            public LevelOneEquitiesService(Streamer streamer, string reference)
                : base(streamer, Service.Services.LEVELONE_EQUITIES, reference)
            {
            }

            /// <summary>
            /// Level One Equities Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquity.Fields</param>
            public List<LevelOneEquity> Request(string symbols, string fields)
            {
                return Request(symbols, fields, null, null);
            }

            /// <summary>
            /// Level One Equities Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquity.Fields</param>
            /// <param name="equitiesCallback">method to call whenever a LevelOneEquity is processed. </param>
            public List<LevelOneEquity> Request(string symbols, string fields, EquitiesCallback equitiesCallback)
            {
                return Request(symbols, fields, equitiesCallback, null);
            }

            /// <summary>
            /// Level One Equities Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquity.Fields</param>
            /// <param name="LevelOneEquityCallback">method to call whenever a LevelOneEquity is processed. </param>
            public List<LevelOneEquity> Request(string symbols, string fields, LevelOneEquityCallback levelOneEquityCallback)
            {
                return Request(symbols, fields, null, levelOneEquityCallback);
            }

            /// <summary>
            /// Level One Equities Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquity.Fields</param>
            /// <param name="equitiesCallback">method to call whenever a LevelOneEquity is processed. </param>
            public List<LevelOneEquity> Request(string symbols, string fields, EquitiesCallback? equitiesCallback, LevelOneEquityCallback? levelOneEquityCallback)
            {
                SetActiveSymbols(symbols);
                streamer.ServiceRequest(service, symbols, fields);
                this.equitiesCallback = equitiesCallback;

                Data = new List<LevelOneEquity>(); // clear for new service
                AddSymbolsToDataList(symbols, levelOneEquityCallback);
                serviceIsActive = true;
                return Data;
            }

            private void AddSymbolsToDataList(string symbols, LevelOneEquityCallback? levelOneEquityCallback)
            {
                var Symbols = symbols.Split(',');
                for (var x = 0; x < Symbols.Length; x++)
                {
                    var s = Symbols[x].Trim();
                    var quote = Data.Where(r => r.key == s).SingleOrDefault();
                    if (quote == null)
                    {
                        quote = new LevelOneEquity();
                        quote.key = s;
                        quote.Symbol = s;
                        quote.Callback = levelOneEquityCallback;
                        Data.Add(quote);
                    }
                }
            }

            /// <summary>
            /// Add symbols to streaming list (existing EquitiesRequest())
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public List<LevelOneEquity> Add(string symbols, LevelOneEquityCallback? levelOneEquityCallback = null)
            {
                CheckServiceIsActive();
                streamer.ServiceAdd(service, symbols, ActiveSymbols);
                AddSymbolsToDataList(symbols, levelOneEquityCallback);
                return Data;
            }

            /// <summary>
            /// remove symbols from streaming list
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public List<LevelOneEquity> Remove(string symbols)
            {
                CheckServiceIsActive();
                symbols = ActiveSymbolsRemove(symbols);
                streamer.ServiceRemove(service, symbols);
                if (equitiesCallback != null)
                    equitiesCallback(Data);
                return Data;
            }

            /// <summary>
            /// Change fields being streamed
            /// </summary>
            /// <param name="fields"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void View(string fields)
            {
                CheckServiceIsActive();
                streamer.ServiceView(service, fields);
            }

            internal override void ProcessResponseSUBS(ResponseMessage.Response response)
            {
                // do nothing
            }

            internal override void ProcessResponseUNSUBS(ResponseMessage.Response response)
            {
                // do nothing
            }

            internal override void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                LevelOneEquity quote;

                for (var i = 0; i < d.content.Count; i++)
                {
                    var q = d.content[i];
                    var symbol = q.key;

                    if (!ActiveSymbols.Contains(symbol))
                        continue;  // this one has been removed, but some results my come through for a bit.

                    try { 
                        quote = Data.Where(r => r.key == symbol).Single();
                    }
                    catch (Exception ex) {
                        throw new Exception("LevelOneEquity.ProcessData quote: " + ex.Message);
                    }

                    if (q.assetMainType != null)  // first time
                    {
                        quote.delayed = q.delayed;
                        quote.cusip = q.cusip;
                        quote.assetMainType = q.assetMainType;
                        quote.assetSubType = q.assetSubType;
                    }
                    quote.timestamp = SchwabApi.ApiDateTime_to_DateTime(d.timestamp);
                    quote.UpdateProperties(content[i]);
                    if (quote.Callback != null)
                        quote.Callback(quote); // callback whenever a LevelOneEquity symbol has been processed
                }
                if (equitiesCallback != null)
                    equitiesCallback(Data); // callback whenever a LevelOneEquities has been processed
            }

            internal override void RemoveFromData(string symbol)
            {
                var i = Data.Where(r => r.key == symbol).SingleOrDefault();
                if (i != null)
                    Data.Remove(i); // don't process anymore
            }
        }

        public class LevelOneEquity : INotifyPropertyChanged
        {
            public override string ToString() { return $"{Symbol} {Description}"; }

            public enum Fields  // Level 1 Equities Fields
            {
                Symbol,                 // string   Ticker symbol in upper case.
                BidPrice,               // double   Current Bid Price
                AskPrice,               // double   Current Ask Price
                LastPrice,              // double   Price at which the last trade was matched
                BidSize,                // int      Number of shares for bid
                AskSize,                // int      Number of shares for ask
                AskID,                  // string   Exchange with the ask
                BidID,                  // string   Exchange with the bid
                TotalVolume,            // long     Aggregated shares traded throughout the day, including pre/post market hours.
                LastSize,               // long     Number of shares traded with last trade
                HighPrice,              // double   Day's high trade price
                LowPrice,               // double   Day's low trade price
                ClosePrice,             // double   Previous day's closing price
                ExchangeID,             // string   Primary "listing" Exchange
                Marginable,             // bool     Stock approved by the Federal Reserve and an investor's broker as being eligible for providing collateral for margin debt.
                Description,            // string   A company, index or fund name. Once per day descriptions are loaded from the database at 7:29:50 AM ET.
                LastID,                 // string   Exchange where last trade was executed
                OpenPrice,              // double   Day's Open Price. According to industry standard, only regular session trades set the open,
                                        //          If a stock does not trade during the regular session, then the open price is 0.
                                        //          In the pre-market session, open is blank because pre-market session trades do not set the open.
                                        //          Open is set to ZERO at 3:30am ET.
                NetChange,              // double   LastPrice - ClosePrice.  If close is zero, change will be zero
                Week52High,             // double   Higest price traded in the past 12 months, or 52 weeks. Calculated by merging intraday high(from fh) and 52-week high (from db)
                Week52Low,              // double   Lowest price traded in the past 12 months, or 52 weeks. Calculated by merging intraday low (from fh) and 52-week low(from db)
                PERatio,                // double   Price-to-earnings ratio. The P/E equals the price of a share of stock, divided by the company's earnings-per-share.
                                        //          Note that the "price of a share of stock" in the definition does update during the day so this field has the potential to stream.
                                        //          However, the current implementation uses the closing price and therefore does not stream throughout the day.
                AnnualDividendAmount,   // double   Annual Dividend Amount
                DividendYield,          // double   Dividend Yield

                NAV,                    // double   Mutual Fund Net Asset Value. Load various times after market close
                ExchangeName,           // string   Display name of exchange
                DividendDate,           // string
                RegularMarketQuote,     // bool     Is last quote a regular quote
                RegularMarketTrade,     // bool     Is last trade a regular trade
                RegularMarketLastPrice, // double   Only records regular trade
                RegularMarketLastSize,  // long     Currently realize/100, only records regular trade
                RegularMarketNetChange, // double   RegularMarketLastPrice - ClosePrice
                SecurityStatus,         // string   Indicates a symbols current trading status, Normal, Halted, Closed
                MarkPrice,              // double   Mark Price
                QuoteTime,              // long     Last time a bid or ask updated in milliseconds since Epoch.
                TradeTime,              // long     Last trade time in milliseconds since Epoch.
                RegularMarketTradeTime, // long     Regular market trade time in milliseconds since Epoch.
                BidTime,                // long     Last bid time in milliseconds since Epoch.
                AskTime,                // long     Last ask time in milliseconds since Epoch.
                AskMicId,               // string   4-chars Market Identifier Code
                BidMicId,               // string   4-chars Market Identifier Code
                LastMicId,              // string   4-chars Market Identifier Code
                NetPercentChange,       // double   Net Percentage Change NetChange / ClosePrice* 100
                RegularMarketPercentChange,//double Regular market hours percentage change: RegularMarketNetChange / ClosePrice* 100
                MarkPriceNetChange,     // double   Mark price net change
                MarkPricePercentChange, // double   Mark price percentage change
                HardToBorrowQuantity,   // int       -1 = NULL, >= 0 is valid quantity
                HardToBorrowRate,       // double   null = NULL, valid range = -99,999.999 to +99,999.999
                HardToBorrow,           // int      -1 = NULL, 1 = true, 0 = false
                Shortable,              // int      -1 = NULL, 1 = true, 0 = false
                PostMarketNetChange,    // double   Change in price since the end of the regular session(typically 4:00pm), PostMarketLastPrice - RegularMarketLastPrice
                PostMarketPercentChange // double   Percent Change in price since the end of the regular session (typically 4:00pm), PostMarketNetChange / RegularMarketLastPrice* 100
            };

            private static string allFields = null;
            /// <summary>
            /// Comma seperated list of all fields
            /// </summary>
            public static string AllFields
            {
                get
                {
                    if (allFields == null)
                    {
                        var count = Enum.GetNames(typeof(Fields)).Length;
                        allFields = string.Join(",", Enumerable.Range(0, count));
                    }
                    return allFields;
                }
            }

            private static string commonFields = null;
            /// <summary>
            /// commonly used fields.
            /// </summary>
            public static string CommonFields
            {
                get
                {
                    if (commonFields == null)
                    {
                        commonFields = CustomFields(  // these must be kept in assending (int) order
                            Fields.Symbol,
                            Fields.BidPrice,
                            Fields.AskPrice,
                            Fields.LastPrice,
                            Fields.TotalVolume,
                            Fields.HighPrice,
                            Fields.LowPrice,
                            Fields.ClosePrice,
                            Fields.Description,
                            Fields.OpenPrice,
                            Fields.NetChange,
                            Fields.MarkPrice,
                            Fields.NetPercentChange);
                    }
                    return commonFields;
                }
            }

            /// <summary>
            /// Combine array of fields into comma separated string
            /// </summary>
            /// <param name="fields"></param>
            /// <returns></returns>
            public static string CustomFields(params Fields[] fields)
            {
                return string.Join(",", fields.Select(f => (int)f));
            }

            /// <summary>
            /// Update LevelOneEquities object with streamed data
            /// </summary>
            /// <param name="data">streamed data</param>
            internal void UpdateProperties(Newtonsoft.Json.Linq.JObject data)
            {
                // "key": "AAPL", "delayed": false, "assetMainType": "EQUITY", "assetSubType": "COE", "cusip": "037833100", "1": 214.17, "2": 214.22 ......

                //timestamp = data.time

                foreach (var d in data)
                {
                    if (d.Key.Length <= 2)  // values 0 to 99
                    {
                        switch (Convert.ToInt32(d.Key))
                        {
                            case (int)Fields.Symbol: Symbol = (string)d.Value; break;
                            case (int)Fields.BidPrice: BidPrice = (double)d.Value; break;
                            case (int)Fields.AskPrice: AskPrice = (double)d.Value; break;
                            case (int)Fields.LastPrice: LastPrice = (double)d.Value; break;
                            case (int)Fields.BidSize: BidSize = (int)d.Value; break;
                            case (int)Fields.AskSize: AskSize = (int)d.Value; break;
                            case (int)Fields.AskID: AskID = (string)d.Value; break;
                            case (int)Fields.BidID: BidID = (string)d.Value; break;
                            case (int)Fields.TotalVolume: TotalVolume = (long)d.Value; break;
                            case (int)Fields.LastSize: LastSize = (long)d.Value; break;
                            case (int)Fields.HighPrice: HighPrice = (double)d.Value; break;
                            case (int)Fields.LowPrice: LowPrice = (double)d.Value; break;
                            case (int)Fields.ClosePrice: ClosePrice = (double)d.Value; break;
                            case (int)Fields.ExchangeID: ExchangeID = (string)d.Value; break;
                            case (int)Fields.Marginable: Marginable = (bool)d.Value; break;
                            case (int)Fields.Description: Description = (string)d.Value; break;
                            case (int)Fields.LastID: LastID = (string)d.Value; break;
                            case (int)Fields.OpenPrice: OpenPrice = (double)d.Value; break;
                            case (int)Fields.NetChange: NetChange = (double)d.Value; break;
                            case (int)Fields.Week52High: Week52High = (double)d.Value; break;
                            case (int)Fields.Week52Low: Week52Low = (double)d.Value; break;
                            case (int)Fields.PERatio: PERatio = (double)d.Value; break;
                            case (int)Fields.AnnualDividendAmount: AnnualDividendAmount = (double)d.Value; break;
                            case (int)Fields.DividendYield: DividendYield = (double)d.Value; break;
                            case (int)Fields.NAV: NAV = (double)d.Value; break;
                            case (int)Fields.ExchangeName: ExchangeName = (string)d.Value; break;
                            case (int)Fields.DividendDate: DividendDate = (string)d.Value == "" ? null : (DateTime)d.Value; break;
                            case (int)Fields.RegularMarketQuote: RegularMarketQuote = (bool)d.Value; break;
                            case (int)Fields.RegularMarketTrade: RegularMarketTrade = (bool)d.Value; break;
                            case (int)Fields.RegularMarketLastPrice: RegularMarketLastPrice = (double)d.Value; break;
                            case (int)Fields.RegularMarketLastSize: RegularMarketLastSize = (long)d.Value; break;
                            case (int)Fields.RegularMarketNetChange: RegularMarketNetChange = (double)d.Value; break;
                            case (int)Fields.SecurityStatus: SecurityStatus = (string)d.Value; break;
                            case (int)Fields.MarkPrice: MarkPrice = (double)d.Value; break;
                            case (int)Fields.QuoteTime: QuoteTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.TradeTime: TradeTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.RegularMarketTradeTime: RegularMarketTradeTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.BidTime: BidTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.AskTime: AskTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.AskMicId: AskMicId = (string)d.Value; break;
                            case (int)Fields.BidMicId: BidMicId = (string)d.Value; break;
                            case (int)Fields.LastMicId: LastMicId = (string)d.Value; break;
                            case (int)Fields.NetPercentChange: NetPercentChange = (double)d.Value; break;
                            case (int)Fields.RegularMarketPercentChange: RegularMarketPercentChange = (double)d.Value; break;
                            case (int)Fields.MarkPriceNetChange: MarkPriceNetChange = (double)d.Value; break;
                            case (int)Fields.MarkPricePercentChange: MarkPricePercentChange = (double)d.Value; break;
                            case (int)Fields.HardToBorrowQuantity: HardToBorrowQuantity = (int)d.Value; break;
                            case (int)Fields.HardToBorrowRate: HardToBorrowRate = (double)d.Value; break;
                            case (int)Fields.HardToBorrow: HardToBorrow = (int?)d.Value; break;
                            case (int)Fields.Shortable: Shortable = (int?)d.Value; break;
                            case (int)Fields.PostMarketNetChange: PostMarketNetChange = (double)d.Value; break;
                            case (int)Fields.PostMarketPercentChange: PostMarketPercentChange = (double)d.Value; break;
                        }
                    }
                }

            }

            /// <summary>
            /// Callback to be called whenever a LevelOneEquity response is processed
            /// </summary>
            public LevelOneEquityCallback? Callback = null;

            public event PropertyChangedEventHandler? PropertyChanged;
            private void Changed([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }


            public string key { get; set; }
            public bool delayed { get; set; }
            public string assetMainType { get; set; }
            public string assetSubType { get; set; }
            public string cusip { get; set; }
            public DateTime timestamp { get; set; }

            // numbered items
            public string Symbol { get; set; } = "";


            // properties with PropertyChanged events

            private double bidPrice = 0;
            public double BidPrice
            {
                get { return bidPrice; }
                set { bidPrice = value; Changed(); }
            }

            private double askPrice = 0;
            public double AskPrice
            {
                get { return askPrice; }
                set { askPrice = value; Changed(); }
            }

            private double lastPrice = 0;
            public double LastPrice
            {
                get { return lastPrice; }
                set { lastPrice = value; Changed(); }
            }


            private int bidSize = 0;
            public int BidSize
            {
                get { return bidSize; }
                set { bidSize = value; Changed(); }
            }


            private int askSize = 0;
            public int AskSize
            {
                get { return askSize; }
                set { askSize = value; Changed(); }
            }


            private string askID = "";
            public string AskID
            {
                get { return askID; }
                set { askID = value; Changed(); }
            }


            private string bidID = "";
            public string BidID
            {
                get { return bidID; }
                set { bidID = value; Changed(); }
            }


            private long totalVolume = 0;
            public long TotalVolume
            {
                get { return totalVolume; }
                set { totalVolume = value; Changed(); }
            }


            private long lastSize = 0;
            public long LastSize
            {
                get { return lastSize; }
                set { lastSize = value; Changed(); }
            }


            private double highPrice = 0;
            public double HighPrice
            {
                get { return highPrice; }
                set { highPrice = value; Changed(); }
            }

            private double lowPrice = 0;
            public double LowPrice
            {
                get { return lowPrice; }
                set { lowPrice = value; Changed(); }
            }

            private double closePrice = 0;
            public double ClosePrice
            {
                get { return closePrice; }
                set { closePrice = value; Changed(); }
            }


            private string exchangeID = "";
            public string ExchangeID
            {
                get { return exchangeID; }
                set { exchangeID = value; Changed(); }
            }


            private bool? marginable;
            public bool? Marginable
            {
                get { return marginable; }
                set { marginable = value; Changed(); }
            }


            private string description = "";
            public string Description
            {
                get { return description; }
                set { description = value; Changed(); }
            }


            private string lastID = "";
            public string LastID
            {
                get { return lastID; }
                set { lastID = value; Changed(); }
            }


            private double openPrice = 0;
            public double OpenPrice
            {
                get { return openPrice; }
                set { openPrice = value; Changed(); }
            }


            private double netChange = 0;
            public double NetChange
            {
                get { return netChange; }
                set { netChange = value; Changed(); }
            }


            private double week52High = 0;
            public double Week52High
            {
                get { return week52High; }
                set { week52High = value; Changed(); }
            }
            

            private double week52Low = 0;
            public double Week52Low
            {
                get { return week52Low; }
                set { week52Low = value; Changed(); }
            }


            private double pERatio = 0;
            public double PERatio
            {
                get { return pERatio; }
                set { pERatio = value; Changed(); }
            }


            private double annualDividendAmount = 0;
            public double AnnualDividendAmount
            {
                get { return annualDividendAmount; }
                set { annualDividendAmount = value; Changed(); }
            }


            private double dividendYield = 0;
            public double DividendYield
            {
                get { return dividendYield; }
                set { dividendYield = value; Changed(); }
            }


            private double nav = 0;
            public double NAV
            {
                get { return nav; }
                set { nav = value; Changed(); }
            }


            private string exchangeName = "";
            public string ExchangeName
            {
                get { return exchangeName; }
                set { exchangeName = value; Changed(); }
            }


            private DateTime? dividendDate;
            public DateTime? DividendDate
            {
                get { return dividendDate; }
                set { dividendDate = value; Changed(); }
            }


            private bool? regularMarketQuote;
            public bool? RegularMarketQuote
            {
                get { return regularMarketQuote; }
                set { regularMarketQuote = value; Changed(); }
            }


            private bool? regularMarketTrade;
            public bool? RegularMarketTrade
            {
                get { return regularMarketTrade; }
                set { regularMarketTrade = value; Changed(); }
            }


            private double regularMarketLastPrice = 0;
            public double RegularMarketLastPrice
            {
                get { return regularMarketLastPrice; }
                set { regularMarketLastPrice = value; Changed(); }
            }


            private long regularMarketLastSize = 0;
            public long RegularMarketLastSize
            {
                get { return regularMarketLastSize; }
                set { regularMarketLastSize = value; Changed(); }
            }


            private double regularMarketNetChange = 0;
            public double RegularMarketNetChange
            {
                get { return regularMarketNetChange; }
                set { regularMarketNetChange = value; Changed(); }
            }


            private string securityStatus = "";
            public string SecurityStatus
            {
                get { return securityStatus; }
                set { securityStatus = value; Changed(); }
            }


            private double markPrice = 0;
            public double MarkPrice
            {
                get { return markPrice; }
                set { markPrice = value; Changed(); }
            }


            private DateTime? quoteTime;
            public DateTime? QuoteTime
            {
                get { return quoteTime; }
                set { quoteTime = value; Changed(); }
            }


            private DateTime? tradeTime;
            public DateTime? TradeTime
            {
                get { return tradeTime; }
                set { tradeTime = value; Changed(); }
            }


            private DateTime? regularMarketTradeTime;
            public DateTime? RegularMarketTradeTime
            {
                get { return regularMarketTradeTime; }
                set { regularMarketTradeTime = value; Changed(); }
            }


            private DateTime? bidTime;
            public DateTime? BidTime
            {
                get { return bidTime; }
                set { bidTime = value; Changed(); }
            }


            private DateTime? askTime;
            public DateTime? AskTime
            {
                get { return askTime; }
                set { askTime = value; Changed(); }
            }


            private string askMicId = "";
            public string AskMicId
            {
                get { return askMicId; }
                set { askMicId = value; Changed(); }
            }


            private string bidMicId = "";
            public string BidMicId
            {
                get { return bidMicId; }
                set { bidMicId = value; Changed(); }
            }


            private string lastMicId = "";
            public string LastMicId
            {
                get { return lastMicId; }
                set { lastMicId = value; Changed(); }
            }


            private double netPercentChange = 0;
            public double NetPercentChange
            {
                get { return netPercentChange; }
                set { netPercentChange = value; Changed(); }
            }


            private double regularMarketPercentChange = 0;
            public double RegularMarketPercentChange
            {
                get { return regularMarketPercentChange; }
                set { regularMarketPercentChange = value; Changed(); }
            }


            private double markPriceNetChange = 0;
            public double MarkPriceNetChange
            {
                get { return markPriceNetChange; }
                set { markPriceNetChange = value; Changed(); }
            }


            private double markPricePercentChange = 0;
            public double MarkPricePercentChange
            {
                get { return markPricePercentChange; }
                set { markPricePercentChange = value; Changed(); }
            }


            private int hardToBorrowQuantity = 0;
            public int HardToBorrowQuantity
            {
                get { return hardToBorrowQuantity; }
                set { hardToBorrowQuantity = value; Changed(); }
            }


            private double hardToBorrowRate = 0;
            public double HardToBorrowRate
            {
                get { return hardToBorrowRate; }
                set { hardToBorrowRate = value; Changed(); }
            }


            private int? hardToBorrow;
            public int? HardToBorrow
            {
                get { return hardToBorrow; }
                set { hardToBorrow = value; Changed(); }
            }


            private int? shortable;
            public int? Shortable
            {
                get { return shortable; }
                set { shortable = value; Changed(); }
            }


            private double postMarketNetChange = 0;
            public double PostMarketNetChange
            {
                get { return postMarketNetChange; }
                set { postMarketNetChange = value; Changed(); }
            }


            private double postMarketPercentChange = 0;
            public double PostMarketPercentChange
            {
                get { return postMarketPercentChange; }
                set { postMarketPercentChange = value; Changed(); }
            }
        }
    }
}
