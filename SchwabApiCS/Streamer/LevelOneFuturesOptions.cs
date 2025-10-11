// <copyright file="LevelOneFuturesOptions.cs" company="ZPM Software Inc">
// Copyright © 2025 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using static SchwabApiCS.SchwabApi;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class LevelOneFuturesOptionsService : Service
        {
            public delegate void LevelOneFuturesOptionsCallback(List<LevelOneFutureOption> data);

            private List<LevelOneFutureOption> Data = new List<LevelOneFutureOption>();
            private LevelOneFuturesOptionsCallback? Callback = null;

            public LevelOneFuturesOptionsService(Streamer streamer, string reference)
                : base(streamer, Service.Services.LEVELONE_FUTURES_OPTIONS, reference)
            {
            }

            /// <summary>
            /// Level 1 Futures Options Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneFutures.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, LevelOneFuturesOptionsCallback callback)
            {
                ActiveSymbols = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // new list
                streamer.ServiceRequest(Services.LEVELONE_FUTURES_OPTIONS, symbols, fields);
                Callback = callback;
            }

            /// <summary>
            /// Add symbols to streaming list (existing FuturesRequest())
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void Add(string symbols)
            {
                CallbackCheck(Callback, "Add");
                streamer.ServiceAdd(service, symbols, ActiveSymbols);
            }

            /// <summary>
            /// remove symbols from streaming list
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void Remove(string symbols)
            {
                CallbackCheck(Callback, "Remove");
                symbols = ActiveSymbolsRemove(symbols);
                streamer.ServiceRemove(service, symbols);
                Callback(Data);
            }

            /// <summary>
            /// Change fields being streamed
            /// </summary>
            /// <param name="fields"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void View(string fields)
            {
                CallbackCheck(Callback, "View");
                streamer.ServiceView(service, fields);
            }

            internal override void ProcessResponseSUBS(ResponseMessage.Response response)
            {
                Data = new List<LevelOneFutureOption>(); // clear for new service
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
                            quote = new LevelOneFutureOption()
                            {
                                key = symbol,
                                Symbol = symbol,
                                delayed = q.delayed,
                                cusip = q.cusip,
                                assetMainType = q.assetMainType,
                                assetSubType = q.assetSubType
                            };
                            Data.Add(quote);
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                    }
                    quote.timestamp = SchwabApi.ApiDateTime_to_DateTime(d.timestamp);
                    quote.UpdateProperties(content[i]);
                }
                Callback(Data); // callback to application with updated values
            }

            internal override void RemoveFromData(string symbol)
            {
                var i = Data.Where(r => r.key == symbol).SingleOrDefault();
                if (i != null)
                    Data.Remove(i); // don't process anymore
            }
        }

        public class LevelOneFutureOption
        {
            public enum Fields  // Level 1 Future Options Fields
            {
                Symbol = 0,                 // string   Ticker symbol in upper case.
                BidPrice = 1,               // double   Current Bid Price
                AskPrice = 2,               // double   Current Ask Price
                LastPrice = 3,              // double   Price at which the last trade was matched
                BidSize = 4,                // int      Number of contracts for bid
                AskSize = 5,                // int      Number of contracts for ask
                BidID = 6,                  // char     Exchange with the bid, Currently "?" for unknown as all quotes are CME
                AskID = 7,                  // char     Exchange with the ask, Currently "?" for unknown as all quotes are CME
                TotalVolume = 8,            // long     Aggregated shares traded throughout the day, including pre/post market hours.
                LastSize = 9,               // long     Number of contracts traded with last trade
                QuoteTime = 10,             // long     Last quote time in milliseconds since Epoch
                TradeTime = 11,             // Long     Last trade time in milliseconds since Epoch
                HighPrice = 12,             // double   Day's high trade price
                LowPrice = 13,              // double   Day's low trade price
                ClosePrice = 14,            // double   Previous day's closing price
                LastID = 15,                // char     Exchange where last trade was executed, Currently "?" for unknown as all quotes are CME
                Description = 16,           // string   Description of the product
                OpenPrice = 17,             // double   Day's Open Price
                OpenInterest = 18,          // double
                Mark = 19,                  // double   Mark-to-Market value is calculated daily using current prices to determine profit/loss
                                            //          If lastprice is within spread, value = lastprice else value = (bid + ask) / 2
                Tick = 20,                  // double   Minimum price movement, Minimum price increment of contract
                TickAmount = 21,            // double   Minimum amount that the price of the market can change, Tick * multiplier field
                FutureMultiplier = 22,      // double   Point value
                FutureSettlementPrice = 23, // double   Closing Price
                UnderlyingSymbol = 24,      // string   Underlying symbol
                StrikePrice = 25,           // double   strike price
                FutureExpirationDate = 26,  // long     Expiration date of this contract, Milliseconds since epoch
                ExpirationStyle = 27,       // string
                ContractType = 28,          // char
                SecurityStatus = 29,        // string   Indicates a symbol's current trading status: Normal, Halted, Closed
                Exchange = 30,              // char     Exchange character
                ExchangeName = 31           // string   Display name of exchange
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
                            Fields.Mark,
                            Fields.StrikePrice,
                            Fields.FutureExpirationDate
                        );
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

            public override string ToString()
            {
                return key + timestamp.ToString("  h:mm:ss tt");
            }

            /// <summary>
            /// Update LevelOneFutureOptions object with streamed data
            /// </summary>
            /// <param name="data">streamed data</param>
            public void UpdateProperties(Newtonsoft.Json.Linq.JObject data)
            {
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
                            case (int)Fields.BidID: BidID = (string)d.Value; break;
                            case (int)Fields.AskID: AskID = (string)d.Value; break;
                            case (int)Fields.TotalVolume: TotalVolume = (long)d.Value; break;
                            case (int)Fields.LastSize: LastSize = (long)d.Value; break;
                            case (int)Fields.QuoteTime: QuoteTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.TradeTime: TradeTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.HighPrice: HighPrice = (double)d.Value; break;
                            case (int)Fields.LowPrice: LowPrice = (double)d.Value; break;
                            case (int)Fields.ClosePrice: ClosePrice = (double)d.Value; break;
                            case (int)Fields.LastID: LastID = (string)d.Value; break;
                            case (int)Fields.Description: Description = (string)d.Value; break;
                            case (int)Fields.OpenPrice: OpenPrice = (double)d.Value; break;
                            case (int)Fields.OpenInterest: OpenInterest = (int)d.Value; break;
                            case (int)Fields.Mark: Mark = (double)d.Value; break;
                            case (int)Fields.Tick: Tick = (double)d.Value; break;
                            case (int)Fields.TickAmount: TickAmount = (double)d.Value; break;
                            case (int)Fields.FutureMultiplier: FutureMultiplier = (double)d.Value; break;
                            case (int)Fields.FutureSettlementPrice: FutureSettlementPrice = (double)d.Value; break;
                            case (int)Fields.UnderlyingSymbol: UnderlyingSymbol = (string)d.Value; break;
                            case (int)Fields.StrikePrice: StrikePrice = (double)d.Value; break;
                            case (int)Fields.FutureExpirationDate: FutureExpirationDate = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.ExpirationStyle: ExpirationStyle = (string)d.Value; break;
                            case (int)Fields.ContractType: ContractType = (string)d.Value; break;
                            case (int)Fields.SecurityStatus: SecurityStatus = (string)d.Value; break;
                            case (int)Fields.Exchange: Exchange = (string)d.Value; break;
                            case (int)Fields.ExchangeName: ExchangeName = (string)d.Value; break;
                            default:
                                break;
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
            public string BidID { get; set; } = "";
            public string AskID { get; set; } = "";
            public long TotalVolume { get; set; } = 0;
            public long LastSize { get; set; } = 0;
            public DateTime? QuoteTime { get; set; }
            public DateTime? TradeTime { get; set; }
            public double HighPrice { get; set; } = 0;
            public double LowPrice { get; set; } = 0;
            public double ClosePrice { get; set; } = 0;
            public string LastID { get; set; } = "";
            public string Description { get; set; } = "";
            public double OpenPrice { get; set; } = 0;
            public double OpenInterest { get; set; } = 0;

            public double Mark { get; set; } = 0;
            public double Tick { get; set; } = 0;
            public double TickAmount { get; set; } = 0;

            public double FutureMultiplier { get; set; } = 0;
            public double FutureSettlementPrice { get; set; } = 0;
            public string UnderlyingSymbol { get; set; } = "";
            public double StrikePrice { get; set; } = 0;
            public DateTime? FutureExpirationDate { get; set; } = null;
            public string ExpirationStyle { get; set; } = "";
            public string ContractType { get; set; } = "";
            public string SecurityStatus { get; set; } = "";
            public string Exchange { get; set; } = "";
            public string ExchangeName { get; set; } = "";
        }
    }
}
