// <copyright file="Screener.cs" company="ZPM Software Inc">
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

        public class ScreenerEquitiesService : ScreenerService // Not implemented by Schwab yet
        {
            public ScreenerEquitiesService(Streamer streamer, string reference)
                : base(streamer, reference)
            {
                this.service = Services.SCREENER_EQUITY;
                this.ServiceName = this.service.ToString();
            }
        }

        public class ScreenerOptionsService : ScreenerService // Not implemented by Schwab yet
        {
            public ScreenerOptionsService(Streamer streamer, string reference)
                : base(streamer, reference)
            {
                this.service = Services.SCREENER_OPTION;
                this.ServiceName = this.service.ToString();
            }
        }

        public class ScreenerService : Service
        {
            public delegate void ScreenerCallback(List<Screener> data);
            private List<Screener> Data = new List<Screener>();
            private ScreenerCallback? Callback = null;

            protected ScreenerService(Streamer streamer, string reference)
                : base(streamer, reference)
            {
            }

            /// <summary>
            /// Screener Equities Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see Screener.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, ScreenerCallback callback)
            {
                SetActiveSymbols(symbols);
                streamer.ServiceRequest(service, symbols, fields);
                Callback = callback;
            }

            /// <summary>
            /// Add symbols to existing streaming list
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
                Data = new List<Screener>(); // clear for new service
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
                            quote = new Screener()
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

        public class Screener
        {
            public enum Fields
            {
                Symbol                 // string   Ticker symbol in upper case.
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
            /// Update Screener object with streamed data
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

        }
    }
}



