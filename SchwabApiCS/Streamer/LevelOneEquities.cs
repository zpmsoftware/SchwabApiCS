// <copyright file="LevelOneEquities.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.StreamerRequests;
using System.Security.Authentication;
using System.Windows.Controls;
using static SchwabApiCS.Streamer.LevelOneEquity;
using static SchwabApiCS.Streamer.ResponseMessage;
using Accessibility;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class LevelOneEquitiesClass : ServiceClass
        {
            public delegate void EquitiesCallback(List<LevelOneEquity> data);
            private List<LevelOneEquity> Data = new List<LevelOneEquity>();
            private EquitiesCallback? Callback = null;
            private List<string> ActiveSymbols = new List<string>(); // only accept streamed data from this list

            public LevelOneEquitiesClass(Streamer streamer)
                : base(streamer, Streamer.Services.LEVELONE_EQUITIES)
            {
            }

            /// <summary>
            /// Level 1 Equities Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquity.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, EquitiesCallback callback)
            {
                ActiveSymbols = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // new list
                streamer.ServiceRequest(Services.LEVELONE_EQUITIES, symbols, fields);
                Callback = callback;
            }

            /// <summary>
            /// Add symbols to streaming list (existing EquitiesRequest())
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void Add(string symbols)
            {
                if (Callback == null)
                    throw new SchwabApiException("LevelOneEquities.Request() must happen before a LevelOneEquities.Add().");

                streamer.ServiceAdd(Services.LEVELONE_EQUITIES, symbols, ActiveSymbols);
            }

            /// <summary>
            /// remove symbols from streaming list
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void Remove(string symbols)
            {
                if (Callback == null)
                    throw new SchwabApiException("LevelOneEquities.Request() must happen before a LevelOneEquities.Remove().");

                var list = symbols.Split(',').Select(r => r.Trim()).Distinct().ToList(); // add list
                symbols = "";
                foreach (var s in list)
                {
                    if (ActiveSymbols.Contains(s))
                    {
                        ActiveSymbols.Remove(s);
                        symbols += "," + s;
                        var i = Data.Where(r => r.key == s).SingleOrDefault();
                        if (i != null)
                            Data.Remove(i); // don't process anymore
                    }
                }

                if (symbols.Length > 0)
                    streamer.ServiceRemove(Services.LEVELONE_EQUITIES, symbols);
            }

            /// <summary>
            /// Change fields being streamed
            /// </summary>
            /// <param name="fields"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void View(string fields)
            {
                if (Callback == null)
                    throw new SchwabApiException("LevelOneEquities.Request() must happen before a LevelOneEquities.View().");

                streamer.ServiceView(Services.LEVELONE_EQUITIES, fields);
            }

            /// <summary>
            /// process received level one equities data
            /// </summary>
            /// <param name="response"></param>
            /// <exception cref="Exception"></exception>
            internal override void ProcessResponse(ResponseMessage.Response response)
            {
                // { "response":[{ "service":"LEVELONE_EQUITIES","command":"SUBS","requestid":"1","SchwabClientCorrelId":"6abed7d7-e984-bcc4-9b7f-455bbd11f13d",
                // "timestamp":1718745000244,"content":{ "code":0,"msg":"SUBS command succeeded"} }]}

                if (response.content.code != 0)
                {
                    throw new Exception(string.Format(
                        "streamer LEVELONE_EQUITIES {0} Error: {1} {2} ", response.command, response.content.code, response.content.msg));
                }

                switch (response.command)
                {
                    case "SUBS":
                        Data = new List<LevelOneEquity>(); // clear for new service
                        break;
                    case "ADD":
                        break;
                    case "UNSUBS":
                        break;

                    default:
                        break;
                }
            }

            internal override void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                for (var i = 0; i < d.content.Count; i++)
                {
                    var q = d.content[i];
                    var symbol = q.key;

                    if (!ActiveSymbols.Contains(symbol))
                        continue;  // this one has been removed, but some results my come through for a bit.

                    var quote = Data.Where(r => r.key == symbol).SingleOrDefault();
                    if (quote == null)
                    {
                        try
                        {
                            quote = new LevelOneEquity()
                            {
                                key = symbol,
                                delayed = q.delayed,
                                cusip = q.cusip,
                                assetMainType = q.assetMainType,
                                assetSubType = q.assetSubType
                            };
                            Data.Add(quote);
                        }
                        catch (Exception e)
                        {
                            var xx = 1;
                        }
                    }
                    quote.timestamp = SchwabApi.ApiDateTime_to_DateTime(d.timestamp);
                    quote.UpdateProperties(content[i]);
                }
                Callback(Data); // callback to application with updated values
            }

        }

        public class LevelOneEquity
        {
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
                        allFields = string.Join(", ", Enumerable.Range(0, count));
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
            public void UpdateProperties(Newtonsoft.Json.Linq.JObject data)
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
                            case (int)Fields.DividendDate: DividendDate = (DateTime)d.Value; break;
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
                            case (int)Fields.PostMarketNetChange: HardToBorrowRate = (double)d.Value; break;
                            case (int)Fields.PostMarketPercentChange: HardToBorrowRate = (double)d.Value; break;
                        }
                    }
                }

            }

            public string key { get; set; }
            public bool delayed { get; set; }
            public string assetMainType { get; set; }
            public string assetSubType { get; set; }
            public string cusip { get; set; }
            public DateTime timestamp { get; set; }

            // numbered items
            public string Symbol { get; set; } = "";
            public double BidPrice { get; set; } = 0;
            public double AskPrice { get; set; } = 0;
            public double LastPrice { get; set; } = 0;
            public int BidSize { get; set; } = 0;
            public int AskSize { get; set; } = 0;
            public string AskID { get; set; } = "";
            public string BidID { get; set; } = "";
            public long TotalVolume { get; set; } = 0;
            public long LastSize { get; set; } = 0;
            public double HighPrice { get; set; } = 0;
            public double LowPrice { get; set; } = 0;
            public double ClosePrice { get; set; } = 0;
            public string ExchangeID { get; set; } = "";
            public bool? Marginable { get; set; }
            public string Description { get; set; } = "";
            public string LastID { get; set; } = "";
            public double OpenPrice { get; set; } = 0;
            public double NetChange { get; set; } = 0;
            public double Week52High { get; set; } = 0;
            public double Week52Low { get; set; } = 0;
            public double PERatio { get; set; } = 0;
            public double AnnualDividendAmount { get; set; } = 0;
            public double DividendYield { get; set; } = 0;
            public double NAV { get; set; } = 0;
            public string ExchangeName { get; set; } = "";
            public DateTime? DividendDate { get; set; }
            public bool? RegularMarketQuote { get; set; }
            public bool? RegularMarketTrade { get; set; }
            public double RegularMarketLastPrice { get; set; } = 0;
            public long RegularMarketLastSize { get; set; } = 0;
            public double RegularMarketNetChange { get; set; } = 0;
            public string SecurityStatus { get; set; } = "";
            public double MarkPrice { get; set; } = 0;
            public DateTime? QuoteTime { get; set; }
            public DateTime? TradeTime { get; set; }
            public DateTime? RegularMarketTradeTime { get; set; }
            public DateTime? BidTime { get; set; }
            public DateTime? AskTime { get; set; }
            public string AskMicId { get; set; } = "";
            public string BidMicId { get; set; } = "";
            public string LastMicId { get; set; } = "";
            public double NetPercentChange { get; set; } = 0;
            public double RegularMarketPercentChange { get; set; } = 0;
            public double MarkPriceNetChange { get; set; } = 0;
            public double MarkPricePercentChange { get; set; } = 0;
            public int HardToBorrowQuantity { get; set; } = 0;
            public double HardToBorrowRate { get; set; } = 0;
            public int? HardToBorrow { get; set; }
            public int? Shortable { get; set; }
            public double PostMarketNetChange { get; set; } = 0;
            public double PostMarketPercentChange { get; set; } = 0;

        }
    }
}
