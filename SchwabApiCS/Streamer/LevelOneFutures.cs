// <copyright file="LevelOneFutures.cs" company="ZPM Software Inc">
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
using static SchwabApiCS.Streamer;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class LevelOneFuturesClass : ServiceClass
        {
            public delegate void LevelOneFuturesCallback(List<LevelOneFuture> data);

            private List<LevelOneFuture> Data = new List<LevelOneFuture>();
            private LevelOneFuturesCallback? Callback = null;
            private List<string> ActiveSymbols = new List<string>(); // only accept streamed data from this list

            public LevelOneFuturesClass(Streamer streamer)
                : base(streamer, Streamer.Services.LEVELONE_FUTURES)
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
                if (Callback == null)
                    throw new SchwabApiException("LevelOneFutures.Request() must happen before a LevelOneFutures.Add().");

                streamer.ServiceAdd(Services.LEVELONE_FUTURES, symbols, ActiveSymbols);
            }

            /// <summary>
            /// remove symbols from streaming list
            /// </summary>
            /// <param name="symbols"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void Remove(string symbols)
            {
                if (Callback == null)
                    throw new SchwabApiException("LevelOneFutures.Request() must happen before a LevelOneFutures.Remove().");

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
                    streamer.ServiceRemove(Services.LEVELONE_FUTURES, symbols);
            }

            /// <summary>
            /// Change fields being streamed
            /// </summary>
            /// <param name="fields"></param>
            /// <exception cref="SchwabApiException"></exception>
            public void View(string fields)
            {
                if (Callback == null)
                    throw new SchwabApiException("LevelOneFutures.Request() must happen before a LevelOneFutures.View().");

                streamer.ServiceView(Services.LEVELONE_FUTURES, fields);
            }

            /// <summary>
            /// pricess received level one futures data
            /// </summary>
            /// <param name="response"></param>
            /// <exception cref="Exception"></exception>
            internal override void ProcessResponse(ResponseMessage.Response response)
            {
                if (response.content.code != 0)
                {
                    throw new Exception(string.Format(
                        "streamer LEVELONE_FUTURES {0} Error: {1} {2} ", response.command, response.content.code, response.content.msg));
                }

                switch (response.command)
                {
                    case "SUBS":
                        Data = new List<LevelOneFuture>(); // clear for new service
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
                            quote = new LevelOneFuture()
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
                    //quote.UpdateProperties(content[i]);
                }
                Callback(Data); // callback to application with updated values
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
        }
    }
}