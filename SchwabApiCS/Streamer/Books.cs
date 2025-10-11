// <copyright file="Books.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;
using System.Diagnostics;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class NasdaqBookService : BookService
        {
            public NasdaqBookService(Streamer streamer, string reference)
    :           base(streamer, reference)
            {
                this.service = Services.NASDAQ_BOOK;
                this.ServiceName = this.service.ToString();
            }
        }

        public class NyseBookService : BookService
        {
            public NyseBookService(Streamer streamer, string reference)
                : base(streamer, reference)
            {
                this.service = Services.NYSE_BOOK;
                this.ServiceName = this.service.ToString();
            }
        }

        public class OptionsBookService : BookService
        {
            public OptionsBookService(Streamer streamer, string reference)
                : base(streamer, reference)
            {
                this.service = Services.OPTIONS_BOOK;
                this.ServiceName = this.service.ToString();
            }
        }

        public class BookService : Service
        {
            public delegate void BookCallback(List<Book> data);
            private List<Book> Data = new List<Book>();
            private BookCallback? Callback = null;

            protected BookService(Streamer streamer, string reference)
                : base(streamer, reference)
            {
            }

            /// <summary>
            /// Book Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see NasdaqBook.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, BookCallback callback)
            {
                ActiveSymbols = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // new list
                streamer.ServiceRequest(service, symbols, fields);
                Callback = callback;
            }

            /// <summary>
            /// Add symbols to streaming list (existing NasdaqBook)
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

                //if (symbols.Length > 0)
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
                Data = new List<Book>(); // clear for new service
            }

            internal override void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                for (var i = 0; i < d.content.Count; i++)
                {
                    var text = content[i].ToString().Replace("\r\n", "");
                    var book = Newtonsoft.Json.JsonConvert.DeserializeObject<Book>(text); 

                    if (!ActiveSymbols.Contains(book.Symbol))
                        continue;  // this one has been removed, but some results my come through for a bit.

                    var x = Data.FindIndex(r => r.Symbol == book.Symbol);
                    if (x == -1)
                        Data.Add(book);
                    else
                        Data[x] = book;
                }
                Callback(Data.OrderBy(r=> r.Symbol).ToList()); // callback to application with updated values
            }

            internal override void RemoveFromData(string symbol)
            {
                var i = Data.Where(r => r.Symbol == symbol).SingleOrDefault();
                if (i != null)
                    Data.Remove(i); // don't process anymore
            }
        }

        public class Book
        {
            public enum Fields
            {
                Symbol,             // string   Ticker symbol in upper case.
                MarketSnapshotTime,
                BidSideLevels,
                AskSideLevels
            };

            public override string ToString()
            {
                return string.Format("{0}  {1}, BidLevels={2}, AskLevels{3} ", SymbolDisplay(Symbol),
                                     MarketSnapshotTime, BidSideLevels.Count, AskSideLevels.Count);
            }

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

            /// <summary>
            /// Combine array of fields into comma separated string
            /// </summary>
            /// <param name="fields"></param>
            /// <returns></returns>
            public static string CustomFields(params Fields[] fields)
            {
                return string.Join(",", fields.Select(f => (int)f));
            }


            [JsonProperty("key")]
            public string Symbol { get; set; }

            [JsonProperty("1")]
            public long marketSnapshotTime { get; set; }

            [JsonProperty("2")]
            public List<PriceLevel> BidSideLevels { get; set; }

            [JsonProperty("3")]
            public List<PriceLevel> AskSideLevels { get; set; }


            private DateTime? _marketSnapshotTime = null;
            public DateTime MarketSnapshotTime
            {
                get { return ConvertDateOnce(marketSnapshotTime, ref _marketSnapshotTime); }
                set { _marketSnapshotTime = value; }
            }


            public class PriceLevel
            {
                public override string ToString()
                {
                    return string.Format("{0} {1} ", Price, AggregateSize);
                }

                [JsonProperty("0")]
                public double Price { get; set; }

                [JsonProperty("1")]
                public int AggregateSize { get; set; }

                [JsonProperty("2")]
                public int MarketMakerCount { get; set; }

                [JsonProperty("3")]
                public List<MarketMaker> MarketMakers { get; set; }
            }

            public class MarketMaker
            {
                public override string ToString()
                {
                    return string.Format("{0} {1} {2}", MarketMakerId, Size, QuoteTime);
                }

                [JsonProperty("0")]
                public string MarketMakerId { get; set; }

                [JsonProperty("1")]
                public int Size { get; set; }

                [JsonProperty("2")]
                public long quoteTime { get; set; }

                private DateTime? _quoteTime = null;
                public DateTime QuoteTime
                {
                    get {
                        if (_quoteTime == null) {
                            _quoteTime = DateTime.Today.AddMilliseconds(quoteTime + SchwabApi.TimeZoneAdjust);
                        }
                        return (DateTime)_quoteTime;
                    }
                    set { _quoteTime = value; }
                }
            }
        }
    }

}
