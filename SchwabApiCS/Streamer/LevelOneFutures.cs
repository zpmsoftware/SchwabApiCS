// <copyright file="LevelOneFutures.cs" company="ZPM Software Inc">
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
        public class LevelOneFuturesService : Service  // Not implemented by Schwab yet
        {
            public delegate void LevelOneFuturesCallback(List<LevelOneFuture> data);

            private List<LevelOneFuture> Data = new List<LevelOneFuture>();
            private LevelOneFuturesCallback? Callback = null;

            public LevelOneFuturesService(Streamer streamer, string reference)
                : base(streamer, Service.Services.LEVELONE_FUTURES, reference)
            {
            }

            /// <summary>
            /// Level 1 Futures Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneFutures.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, LevelOneFuturesCallback callback)
            {
                ActiveSymbols = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // new list
                streamer.ServiceRequest(Services.LEVELONE_FUTURES, symbols, fields);
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
                Data = new List<LevelOneFuture>(); // clear for new service
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
                            quote = new LevelOneFuture()
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
                            var xx = 1;
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

        public class LevelOneFuture
        {
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
            public DateTime? QuoteTime { get; set; }
            public DateTime? TradeTime { get; set; }
            public double HighPrice { get; set; } = 0;
            public double LowPrice { get; set; } = 0;
            public double ClosePrice { get; set; } = 0;
            public string ExchangeID { get; set; } = "";
            public string Description { get; set; } = "";
            public string LastID { get; set; } = "";
            public double OpenPrice { get; set; } = 0;
            public double NetChange { get; set; } = 0;
            public double PercentChange { get; set; } = 0;
            public string ExchangeName { get; set; } = "";
            public string SecurityStatus { get; set; } = "";
            public int OpenInterest {  get; set; } = 0;
            public double MarkPrice { get; set; } = 0;



            public enum Fields  // Level 1 Futures Fields
            {
                Symbol,                 // string   Ticker symbol in upper case.
                BidPrice,               // double   Current Bid Price
                AskPrice,               // double   Current Ask Price
                LastPrice,              // double   Price at which the last trade was matched
                BidSize,                // int      Number of shares for bid
                AskSize,                // int      Number of shares for ask
                BidID,                  // string   Exchange with the bid
                AskID,                  // string   Exchange with the ask
                TotalVolume,            // long     Aggregated shares traded throughout the day, including pre/post market hours.
                LastSize,               // long     Number of shares traded with last trade
                QuoteTime,              // long     Last time a bid or ask updated in milliseconds since Epoch.
                TradeTime,              // long     Last trade time in milliseconds since Epoch.
                HighPrice,              // double   Day's high trade price
                LowPrice,               // double   Day's low trade price
                ClosePrice,             // double   Previous day's closing price
                ExchangeID,             // string   Primary "listing" Exchange
                Description,            // string   A company, index or fund name. Once per day descriptions are loaded from the database at 7:29:50 AM ET.
                LastID,                 // string   Exchange where last trade was executed
                OpenPrice,              // double   
                NetChange,              // double   If(close>0) change = last – close else change = 0
                PercentChange,          // double   If(close>0) pctChange = (last – close)/close else pctChange = 0
                ExchangeName,           // string   Display name of exchange
                SecurityStatus,         // string   Indicates a symbols current trading status, Normal, Halted, Closed
                OpenInterest,           // int
                MarkPrice               // double   Mark Price
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
                            Fields.PercentChange);
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
                            case (int)Fields.ExchangeID: ExchangeID = (string)d.Value; break;
                            case (int)Fields.Description: Description = (string)d.Value; break;
                            case (int)Fields.LastID: LastID = (string)d.Value; break;
                            case (int)Fields.OpenPrice: OpenPrice = (double)d.Value; break;
                            case (int)Fields.NetChange: NetChange = (double)d.Value; break;
                            case (int)Fields.PercentChange: PercentChange = (double)d.Value; break;
                            case (int)Fields.ExchangeName: ExchangeName = (string)d.Value; break;
                            case (int)Fields.SecurityStatus: SecurityStatus = (string)d.Value; break;
                            case (int)Fields.OpenInterest: OpenInterest = (int)d.Value; break;
                            case (int)Fields.MarkPrice: MarkPrice = (double)d.Value; break;

                        }
                    }
                }

            }

        }
    }
}