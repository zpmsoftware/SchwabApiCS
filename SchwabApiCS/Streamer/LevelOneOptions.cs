// <copyright file="LevelOneOptions.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using static SchwabApiCS.SchwabApi;


namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class LevelOneOptionsService : Service
        {
            public delegate void LevelOneOptionsCallback(List<LevelOneOption> data);

            private List<LevelOneOption> Data = new List<LevelOneOption>();
            private LevelOneOptionsCallback? Callback = null;

            public LevelOneOptionsService(Streamer streamer, string reference)
                : base(streamer, Service.Services.LEVELONE_OPTIONS, reference)
            {
            }

            /// <summary>
            /// Level One Options Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneOptions.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, LevelOneOptionsCallback callback)
            {
                ActiveSymbols = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // new list

                streamer.ServiceRequest(Services.LEVELONE_OPTIONS, symbols, fields);
                Callback = callback;
            }

            /// <summary>
            /// Add symbols to streaming list (existing OptionsRequest())
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
                Data = new List<LevelOneOption>(); // clear for new service
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
                            quote = new LevelOneOption()
                            {
                                key = symbol,
                                Symbol = symbol, // doesn't seem to pick up field 0
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

        public class LevelOneOption
        {
            public enum Fields  // Level 1 Options Fields
            {
                Symbol,                 // string   Ticker symbol in upper case.
                Description,            // string   A company, index or fund name. Once per day descriptions are loaded from the database at 7:29:50 AM ET.
                BidPrice,               // double   Current Bid Price
                AskPrice,               // double   Current Ask Price
                LastPrice,              // double   Price at which the last trade was matched
                HighPrice,              // double   Day's high trade price
                LowPrice,               // double   Day's low trade price
                ClosePrice,             // double   Previous day's closing price
                TotalVolume,            // long     Aggregated shares traded throughout the day, including pre/post market hours.
                OpenInterest,           // int      
                Volatility,             // double   Option Risk/Volatility Measurement/Implied
                IntrinsicValue,         // double   The value an option would have if it were exercised today. Basically, the intrinsic value is the amount by
                                        //          which the strike price of an option is profitable or in-the-money as compared to the underlying stock's price
                ExpirationYear,         // int
                Multiplier,             // double
                Digits,                 // int      Number of decimal places
                OpenPrice,              // double   Day's Open Price
                                        //          According to industry standard, only regular session trades set the open If a stock does not trade during
                                        //          the regular session, then the open price is 0. In the premarket session, open is blank because premarket
                                        //          session trades do not set the open. Open is set to ZERO at 7:28 ET.
                BidSize,                // int      Number of contracts for bid
                AskSize,                // int      Number of contracts for ask
                LastSize,               // long     Number of contracts traded with last trade
                NetChange,              // double   LastPrice - ClosePrice.  If close is zero, change will be zero
                StrikePrice,            // double   Contract strike price
                ContractType,           // 1 char
                Underlying,             // string
                ExpirationMonth,        // int
                Deliverables,           // string
                TimeValue,              // double
                ExpirationDay,          // int
                DaysToExpiration,       // int
                Delta,                  // double
                Gamma,                  // double
                Theta,                  // double
                Vega,                   // double
                Rho,                    // double
                SecurityStatus,         // string
                TheoreticalOptionValue, // double
                UnderlyingPrice,        // double
                UVExpirationType,       // char
                MarkPrice,              // double   Mark Price
                QuoteTime,              // long     Last quote time in milliseconds since Epoch
                TradeTime,              // Long     Last trade time in milliseconds since Epoch
                Exchange,               // char     Exchange character
                ExchangeName,           // string   Display name of exchange
                LastTradingDay,         // long     Last Trading Day
                SettlementType,         // char     Settlement type
                NetPercentChange,       // double   Net Percentage Change
                MarkPriceNetChange,     // double   Mark price net change
                MarkPricePercentChange, // double   Mark price percentage change
                ImpliedYield,           // double
                IsPennyPilot,           // boolean
                OptionRoot,             // string
                Week52High,             // double
                Week52Low,              // double
                IndicativeAskPrice,     // double   Only valid for index options (0 for all other options)
                IndicativeBidPrice,     // double   Only valid for index options (0 for all other options)
                IndicativeQuoteTime,    // long     The latest time the indicative bid/ask prices updated in milliseconds since Epoch
                                        //          Only valid for index options (0 for all other options)
                ExerciseType            // char
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
                            Fields.Description,
                            Fields.BidPrice,
                            Fields.AskPrice,
                            Fields.LastPrice,
                            Fields.HighPrice,
                            Fields.LowPrice,
                            Fields.ClosePrice,
                            Fields.TotalVolume,
                            Fields.OpenPrice,
                            Fields.NetChange,
                            Fields.Delta,
                            Fields.Theta,
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

            public override string ToString()
            {
                return key + timestamp.ToString("  h:mm:ss tt");
            }

            /// <summary>
            /// Update LevelOneOptions object with streamed data
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
                            case (int)Fields.Description: Description = (string)d.Value; break;
                            case (int)Fields.BidPrice: BidPrice = (double)d.Value; break;
                            case (int)Fields.AskPrice: AskPrice = (double)d.Value; break;
                            case (int)Fields.LastPrice: LastPrice = (double)d.Value; break;
                            case (int)Fields.HighPrice: HighPrice = (double)d.Value; break;
                            case (int)Fields.LowPrice: LowPrice = (double)d.Value; break;
                            case (int)Fields.ClosePrice: ClosePrice = (double)d.Value; break;
                            case (int)Fields.TotalVolume: TotalVolume = (long)d.Value; break;
                            case (int)Fields.OpenInterest: OpenInterest = (int)d.Value; break;
                            case (int)Fields.Volatility: Volatility = (double)d.Value; break;
                            case (int)Fields.IntrinsicValue: IntrinsicValue = (double)d.Value; break;
                            case (int)Fields.ExpirationYear: ExpirationYear = (int)d.Value; break;
                            case (int)Fields.Multiplier: Multiplier = (double)d.Value; break;
                            case (int)Fields.Digits: Digits = (int)d.Value; break;
                            case (int)Fields.OpenPrice: OpenPrice = (double)d.Value; break;
                            case (int)Fields.BidSize: BidSize = (int)d.Value; break;
                            case (int)Fields.AskSize: AskSize = (int)d.Value; break;
                            case (int)Fields.LastSize: LastSize = (long)d.Value; break;
                            case (int)Fields.NetChange: NetChange = (double)d.Value; break;
                            case (int)Fields.StrikePrice: StrikePrice = (double)d.Value; break;
                            case (int)Fields.ContractType: ContractType = (string)d.Value; break;
                            case (int)Fields.Underlying: Underlying = (string)d.Value; break;
                            case (int)Fields.ExpirationMonth: ExpirationMonth = (int)d.Value; break;
                            case (int)Fields.Deliverables: Deliverables = (string)d.Value; break;
                            case (int)Fields.TimeValue: TimeValue = (double)d.Value; break;
                            case (int)Fields.ExpirationDay: ExpirationDay = (int)d.Value; break;
                            case (int)Fields.DaysToExpiration: DaysToExpiration = (int)d.Value; break;
                            case (int)Fields.Delta: Delta = (double)d.Value; break;
                            case (int)Fields.Gamma: Gamma = (double)d.Value; break;
                            case (int)Fields.Theta: Theta = (double)d.Value; break;
                            case (int)Fields.Vega: Vega = (double)d.Value; break;
                            case (int)Fields.Rho: Rho = (double)d.Value; break;
                            case (int)Fields.SecurityStatus: SecurityStatus = (string)d.Value; break;
                            case (int)Fields.TheoreticalOptionValue: TheoreticalOptionValue = (double)d.Value; break;
                            case (int)Fields.UnderlyingPrice: UnderlyingPrice = (double)d.Value; break;
                            case (int)Fields.UVExpirationType: UVExpirationType = (string)d.Value; break;
                            case (int)Fields.MarkPrice: MarkPrice = (double)d.Value; break;
                            case (int)Fields.QuoteTime: QuoteTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.TradeTime: TradeTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.Exchange: Exchange = (string)d.Value; break;
                            case (int)Fields.ExchangeName: ExchangeName = (string)d.Value; break;
                            case (int)Fields.LastTradingDay: LastTradingDay = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.SettlementType: SettlementType = (string)d.Value; break;
                            case (int)Fields.NetPercentChange: NetPercentChange = (double)d.Value; break;
                            case (int)Fields.MarkPriceNetChange: MarkPriceNetChange = (double)d.Value; break;
                            case (int)Fields.MarkPricePercentChange: MarkPricePercentChange = (double)d.Value; break;
                            case (int)Fields.ImpliedYield: ImpliedYield = (double)d.Value; break;
                            case (int)Fields.IsPennyPilot: IsPennyPilot = (bool)d.Value; break;
                            case (int)Fields.OptionRoot: OptionRoot = (string)d.Value; break;
                            case (int)Fields.Week52High: Week52High = (double)d.Value; break;
                            case (int)Fields.Week52Low: Week52Low = (double)d.Value; break;
                            case (int)Fields.IndicativeAskPrice: IndicativeAskPrice = (double)d.Value; break;
                            case (int)Fields.IndicativeBidPrice: IndicativeBidPrice = (double)d.Value; break;
                            case (int)Fields.IndicativeQuoteTime: IndicativeQuoteTime = SchwabApi.ApiDateTime_to_DateTime((long)d.Value); break;
                            case (int)Fields.ExerciseType: ExerciseType = (string)d.Value; break;
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
            public string Description { get; set; } = "";
            public double BidPrice { get; set; } = 0;
            public double AskPrice { get; set; } = 0;
            public double LastPrice { get; set; } = 0;
            public double HighPrice { get; set; } = 0;
            public double LowPrice { get; set; } = 0;
            public double ClosePrice { get; set; } = 0;
            public long TotalVolume { get; set; } = 0;
            public int OpenInterest { get; set; } = 0;
            public double Volatility { get; set; } = 0;
            public double IntrinsicValue { get; set; } = 0;
            public int ExpirationYear { get; set; } = 0;
            public double Multiplier { get; set; } = 0;
            public int Digits { get; set; } = 0;
            public double OpenPrice { get; set; } = 0;
            public int BidSize { get; set; } = 0;
            public int AskSize { get; set; } = 0;
            public long LastSize { get; set; } = 0;
            public double NetChange { get; set; } = 0;

            public double StrikePrice { get; set; } = 0;
            public string ContractType { get; set; } = "";
            public string Underlying { get; set; } = "";
            public int ExpirationMonth { get; set; } = 0;
            public string Deliverables { get; set; } = "";

            public double TimeValue { get; set; } = 0;
            public int ExpirationDay { get; set; } = 0;
            public int DaysToExpiration { get; set; } = 0;
            public double Delta { get; set; } = 0;
            public double Gamma { get; set; } = 0;
            public double Theta { get; set; } = 0;
            public double Vega { get; set; } = 0;
            public double Rho { get; set; } = 0;
            public string SecurityStatus { get; set; } = "";
            public double TheoreticalOptionValue { get; set; } = 0;
            public double UnderlyingPrice { get; set; } = 0;
            public string UVExpirationType { get; set; } = "";

            public double MarkPrice { get; set; } = 0;
            public DateTime? QuoteTime { get; set; }
            public DateTime? TradeTime { get; set; }
            public string Exchange { get; set; } = "";
            public string ExchangeName { get; set; } = "";

            public DateTime? LastTradingDay { get; set; } = null;
            public string SettlementType { get; set; } = "";

            public double NetPercentChange { get; set; } = 0;
            public double MarkPriceNetChange { get; set; } = 0;
            public double MarkPricePercentChange { get; set; } = 0;
            public double ImpliedYield { get; set; } = 0;
            public bool IsPennyPilot { get; set; } = false;
            public string OptionRoot { get; set; } = "";
            public double Week52High { get; set; } = 0;
            public double Week52Low { get; set; } = 0;
            public double IndicativeAskPrice { get; set; } = 0;
            public double IndicativeBidPrice { get; set; } = 0;
            public DateTime? IndicativeQuoteTime { get; set; } = null;
            public string ExerciseType { get; set; } = "";
        }
    }
}

