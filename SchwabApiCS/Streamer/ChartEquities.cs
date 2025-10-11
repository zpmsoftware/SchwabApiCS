// <copyright file="ChartEquities.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        //
        //
        public class ChartEquitiesService : Service
        {
            public delegate void ChartEquityCallback(List<ChartEquity> data);
            private List<ChartEquity> Data = new List<ChartEquity>();
            private ChartEquityCallback? Callback = null;

            public ChartEquitiesService(Streamer streamer, string referenceName)
                : base(streamer, Service.Services.CHART_EQUITY, referenceName)
            {
            }

            /// <summary>
            /// Chart Equity Request
            /// </summary>
            /// <param name="symbols">comma separated list of symbols</param>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see ChartEquity.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(string symbols, string fields, ChartEquityCallback callback)
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
                Data = new List<ChartEquity>(); // clear for new service
            }

            internal override void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                for (var i = 0; i < d.content.Count; i++)
                {
                    var symbol = d.content[i].key;
                    if (!ActiveSymbols.Contains(symbol))
                        continue;  // this one has been removed, but some results my come through for a bit.

                    var cf = Data.Where(r => r.Symbol == symbol).SingleOrDefault();
                    if (cf == null)
                    {
                        cf = new ChartEquity() { Symbol = symbol };
                        Data.Add(cf);
                    }
                    cf.UpdateProperties(content[i]);
                }
                Callback(Data); // callback to application with updated values
            }

            internal override void RemoveFromData(string symbol)
            {
                var i = Data.Where(r => r.Symbol == symbol).SingleOrDefault();
                if (i != null)
                    Data.Remove(i); // don't process anymore
            }

        }

        public class ChartEquity
        {
            public override string ToString()
            {
                return $"{Symbol} {((DateTime)ChartTime).ToString("MM/dd/yyyy hh:mm t")}";
            }

            public enum Fields 
            {
                Symbol = 0,             // string   Ticker symbol in upper case.
                Sequence = 1,           // long
                OpenPrice = 2,          // double
                HighPrice = 3,          // double
                LowPrice = 4,           // double
                ClosePrice = 5,         // double
                Volume = 6,             // double
                ChartTime = 7,          // long
                ChartDay =8             // int
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
            /// Update ChartEquities object with streamed data
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
                            case (int)Fields.OpenPrice: OpenPrice = (double)d.Value; break;
                            case (int)Fields.HighPrice: HighPrice = (double)d.Value; break;
                            case (int)Fields.LowPrice: LowPrice = (double)d.Value; break;
                            case (int)Fields.ClosePrice: ClosePrice = (double)d.Value; break;
                            case (int)Fields.Volume: Volume = (double)d.Value; break;
                            case (int)Fields.Sequence: Sequence = (long)d.Value; break;

                            case (int)Fields.ChartTime:
                                if (chartTime != (long)d.Value)
                                {
                                    chartTime = (long)d.Value;
                                    _chartTime = null; // force ChartTime to evaluate
                                }
                                break;

                            case (int)Fields.ChartDay: ChartDay = (int)d.Value; break;

                            default:
                                break;
                        }
                    }
                    else if (d.Key == "seq")
                        Seq = (int)d.Value;
                }
            }

            public int Seq { get; set; }  // This should increment for each minute.
            public string Symbol { get; set; }
            public double? OpenPrice { get; set; }
            public double? HighPrice { get; set; }
            public double? LowPrice { get; set; }
            public double? ClosePrice { get; set; }
            public double? Volume { get; set; }
            public long Sequence { get; set; }  
            public long chartTime { get; set; }
            public int ChartDay { get; set; }

            private DateTime? _chartTime = null;
            public DateTime? ChartTime
            {
                get { return ConvertDateOnce(chartTime, ref _chartTime); }
                set { _chartTime = value; }
            }

        }
    }
}

