// <copyright file="Streamer.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. http://mozilla.org/MPL/2.0/.
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.StreamerRequests;
using System.Security.Authentication;
using System.Windows.Interop;
using System.Windows.Documents;
using static SchwabApiCS.Streamer.ResponseMessage;
using static SchwabApiCS.Streamer.LevelOneEquities;
using System.Net.WebSockets;
using static SchwabApiCS.Streamer;
using System.Diagnostics.SymbolStore;


// https://json2csharp.com/
namespace SchwabApiCS
{
    public partial class Streamer
    {
        private UserPreferences.StreamerInfo streamerInfo;
        private SchwabApi schwabApi;
        private WebSocket4Net.WebSocket websocket;
        private long requestid = 0;
        private bool isLoggedIn = false;
        private List<string> requestQueue = new List<string>();

        private List<LevelOneEquities> levelOneEquities = new List<LevelOneEquities>();
        private EquitiesCallback equitiesCallback = null;
        private List<string> activeEquitySymbols = new List<string>(); // only accept streamed data from this list

        public bool IsLoggedIn {  get { return isLoggedIn; } }

        public Streamer(SchwabApi schwabApi)
        {
            this.schwabApi = schwabApi;
            this.streamerInfo = schwabApi.userPreferences.streamerInfo[0]; ;
            this.websocket = new WebSocket4Net.WebSocket(streamerInfo.streamerSocketUrl, sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls | SslProtocols.None);
            System.Net.ServicePointManager.MaxServicePointIdleTime = int.MaxValue;

            websocket.Opened += (s, e) =>
            {
                websocket.Send(LoginRequest());
            };

            websocket.Closed += (socket, evt) =>
            {
                if (requestQueue.Count > 0) // if has something to process, reopen
                    websocket.Open();
            };

            // handle received messages
            websocket.MessageReceived += (socket, messageEvent) =>  // socket == websocket
            {
                try
                {
                    if (messageEvent.Message.StartsWith("{\"notify\":"))
                    {
                        if (messageEvent.Message.Contains("[{\"heartbeat\":")) // {"notify":[{"heartbeat":"1718745624421"}]}
                            return;

                        var notifyMessage = JsonConvert.DeserializeObject<NotifyMessage>(messageEvent.Message);
                        foreach( var n in notifyMessage.notify)
                        {
                            if (n.content.code != 0)
                            {
                                var xx = 1;
                            }
                            switch (n.service)
                            {
                                case "ADMIN":
                                    // {"notify":[{"service":"ADMIN","timestamp":1718801330575,"content":{"code":30,"msg":"Stop streaming due to empty subscription"}}]}
                                    switch (n.content.code) {
                                        case 30: // Stop streaming due to empty subscription
                                            isLoggedIn = false; // start queueing any requests, sending a new request will reopen and login automatically.
                                            break;

                                        default:
                                            break;
                                    }
                                    break;

                                default:
                                    throw new Exception("Streamer notify service " + n.service + " not implemented");
                            }
                        }
                        return;
                    }

                    if (messageEvent.Message.StartsWith("{\"response\":"))
                    {
                        var responses = JsonConvert.DeserializeObject<ResponseMessage>(messageEvent.Message);
                        foreach(var r in  responses.response)
                        {
                            switch (r.service)
                            {
                                case "ADMIN":
                                    // {"response":[{"service":"ADMIN","command":"LOGIN","requestid":"3","SchwabClientCorrelId":"e761bc20-cc7c-d3f4-3318-55845db8e7e5",
                                    //   "timestamp":1718745613708,"content":{"code":0,"msg":"server=s0300dc7-2;status=NP"}}]}
                                    ProcesResponseADMIN(r);
                                    break;

                                case "LEVELONE_EQUITIES":
                                    ProcesResponseLEVELONE_EQUITIES(r);
                                    break;

                                /*
                                case "LEVELONE_OPTIONS": // Change, Level 1 Options Change
                                    break;
                                case "LEVELONE_FUTURES": // Change, Level 1 Futures Change
                                    break;
                                case "LEVELONE_FUTURES_OPTIONS": // Change, Level 1 Futures Options Change
                                    break;
                                case "LEVELONE_FOREX": // Change, Level 1 Forex Change
                                    break;
                                case "NYSE_BOOK": // Whole, Level Two book for Equities Whole
                                    break;
                                case "NASDAQ_BOOK": // Whole, Level Two book for Equities Whole
                                    break;
                                case "OPTIONS_BOOK": // Whole, Level Two book for Options Whole
                                    break;
                                case "CHART_EQUITY": // All Sequence, Chart candle for Equities All Sequence
                                    break;
                                case "CHART_FUTURES": // All Sequence, Chart candle for Futures All Sequence
                                    break;
                                case "SCREENER_EQUITY": // Whole, Advances and Decliners for Equities Whole
                                    break;
                                case "SCREENER_OPTION": // Whole, Advances and Decliners for Options Whole
                                    break;
                                case "ACCT_ACTIVITY": // All Sequence, Get account activity information such as order fills, etc
                                    break;
                                */
                                default:
                                    throw new Exception("Streamer response service " + r.service + " not implemented");
                            }
                        }
                        return;
                    }
                    if (messageEvent.Message.StartsWith("{\"data\":"))
                    {
                        var dataMsg = JsonConvert.DeserializeObject<DataMessage>(messageEvent.Message);
                        var msg = JsonConvert.DeserializeObject<dynamic>(messageEvent.Message);
                        var nn = ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JObject)msg).First).Name;

                        foreach (var d in dataMsg.data)
                        { 
                            switch (d.service)
                            {
                                case "LEVELONE_EQUITIES":
                                    // {"data":
                                    //    [
                                    //      {"service":"LEVELONE_EQUITIES", "timestamp":1718745703677,"command":"SUBS","content":[
                                    //          {"key":"AAPL","delayed":false,"assetMainType":"EQUITY","assetSubType":"COE","cusip":"037833100","1":214.15,"2":214.16},
                                    //          {"key":"SPY","delayed":false,"assetMainType":"EQUITY","assetSubType":"ETF","cusip":"78462F103","1":548.45,"2":548.46},
                                    //        ]
                                    //      }
                                    //    ]
                                    //  }
                                    ProcessDataLEVELONE_EQUITIES(d, msg);
                                    break;
                                default:
                                    throw new Exception("Streamer data service " + d.service + " not implemented");
                                    break;
                            }
                        }
                        return;
                    }

                    var message = JsonConvert.DeserializeObject<dynamic>(messageEvent.Message);
                    var responseType = ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JObject)message).First).Name;
                    throw new Exception("Streamer response type " + responseType + "" + " not implemented");
                }
                catch (Exception ex)
                {
                    throw new Exception("Streamer error: " + ex.Message);
                }
            };

            // handle socket errors
            websocket.Error += (s, e) =>
            {
                try
                {
                    websocket.Dispose();
                }
                catch (Exception ex)
                {
                    throw new Exception("Streamer error: " + e.Exception.Message + "\n" + ex.Message);
                }
                throw new Exception("Streamer error: " + e.Exception.Message);
            };

            websocket.Open();
        }

        /// <summary>
        /// convert object to a jsonstring and send request to the websocket
        /// </summary>
        /// <param name="request"></param>
        public void SendRequest(object request)
        {
            var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            SendRequest(req);
        }

        /// <summary>
        /// send request to the websocket
        /// </summary>
        /// <param name="jsonRequest">json string</param>
        public void SendRequest(string jsonRequest)
        {
            if (websocket.State == WebSocket4Net.WebSocketState.Closed)
            {
                isLoggedIn = false;
                websocket.Open();
            }

            if (isLoggedIn)
                websocket.Send(jsonRequest); // send immediately
            else // login not achnowledged yet
                requestQueue.Add(jsonRequest); // add to queue to send when login complete
        }

        private string LoginRequest()
        {
            var request = new Request
            {
                service = "ADMIN",
                requestid = (++requestid).ToString(),
                command = "LOGIN",
                SchwabClientCustomerId = streamerInfo.schwabClientCustomerId,
                SchwabClientCorrelId = streamerInfo.schwabClientCorrelId,
                parameters = new Parameters
                {
                    Authorization = SchwabApi.schwabTokens.AccessToken,
                    SchwabClientChannel = streamerInfo.schwabClientChannel,
                    SchwabClientFunctionId = streamerInfo.schwabClientFunctionId
                }
            };
            var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return req;
        }

        public void LogOut()
        {
            var request = new Request()
            {
                requestid = (++requestid).ToString(),
                service = Services.ADMIN.ToString(),
                command = Commands.LOGOUT.ToString(),
                SchwabClientCustomerId = this.streamerInfo.schwabClientCustomerId,
                SchwabClientCorrelId = this.streamerInfo.schwabClientCorrelId,
            };
            SendRequest(request);
        }

        public void ProcesResponseADMIN(ResponseMessage.Response r)
        {
            switch (r.command)
            {
                case "LOGIN":
                    if (r.content.code == 0)
                    {
                        isLoggedIn = true;
                        while (requestQueue.Count > 0)
                        {
                            websocket.Send(requestQueue[0]);
                            requestQueue.RemoveAt(0);
                        }
                    }
                    else
                        throw new Exception("streamer login failed.");
                    break;

                default:
                    break;
            }
        }

        public void ProcesResponseLEVELONE_EQUITIES(ResponseMessage.Response r)
        {
            // { "response":[{ "service":"LEVELONE_EQUITIES","command":"SUBS","requestid":"1","SchwabClientCorrelId":"6abed7d7-e984-bcc4-9b7f-455bbd11f13d",
            // "timestamp":1718745000244,"content":{ "code":0,"msg":"SUBS command succeeded"} }]}

            if (r.content.code != 0)
            {
                throw new Exception(string.Format(
                    "streamer LEVELONE_EQUITIES {0} Error: {1} {2} ", r.command, r.content.code, r.content.msg));
            }

            switch (r.command)
            {
                case "SUBS":
                    levelOneEquities = new List<LevelOneEquities>(); // clear for new service
                    break;
                case "ADD":
                    break;
                case "UNSUBS":
                    //levelOneEquities = new List<LevelOneEquities>();
                    break;

                default:
                    break;
            }
        }


        public void ProcessDataLEVELONE_EQUITIES(DataMessage.DataItem d, dynamic msg)
        {
            // {"data":[{"service":"LEVELONE_EQUITIES", "timestamp":1718759182782,"command":"SUBS","content":[{"key":"AAPL","delayed":false,"assetMainType":"EQUITY","assetSubType":"COE","cusip":"037833100","1":214,"2":214.02},{"key":"SPY","delayed":false,"assetMainType":"EQUITY","assetSubType":"ETF","cusip":"78462F103","1":548.76,"2":548.8},{"key":"IWM","delayed":false,"assetMainType":"EQUITY","assetSubType":"ETF","cusip":"464287655","1":200.77,"2":200.83}]}]}
            // msg.data[0].timestamp.Value  - long
            // msg.data[0].command.Value  SUBS

            foreach (var q in msg.data[0].content)
            {
                var symbol = q.key.Value;

                if (!activeEquitySymbols.Contains(symbol))
                    continue;  // this one has been removed, but some results my come through for a bit.

                var quote = levelOneEquities.Where(r => r.key == symbol).SingleOrDefault();
                    if (quote == null)
                    {
                    try
                    {
                        quote = new LevelOneEquities()
                        {
                            key = symbol,
                            delayed = q.delayed.Value,
                            cusip = q.cusip.Value,
                            assetMainType = q.assetMainType.Value,
                            assetSubType = q.assetSubType.Value
                        };
                        levelOneEquities.Add(quote);
                    }
                    catch (Exception e)
                    {
                        var xx = 1;
                    }
                }
                quote.timestamp = SchwabApi.ApiDateTime_to_DateTime(d.timestamp);
                quote.UpdateProperties(q);
            }
            equitiesCallback(levelOneEquities); // callback to application with updated values
        }

        public delegate void EquitiesCallback(List<LevelOneEquities> levelOneEquities);

        /// <summary>
        /// Level 1 Equities Request
        /// </summary>
        /// <param name="symbols">comma separated list of symbols</param>
        /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquities.Fields</param>
        /// <param name="callback">method to call whenever values change</param>
        public void EquitiesRequest(string symbols, string fields, EquitiesCallback callback)
        {
            activeEquitySymbols = symbols.ToUpper().Split(',').Select(r=> r.Trim()).Distinct().ToList(); // new list

            var request = new Request
            {
                service = "LEVELONE_EQUITIES",
                requestid = (++requestid).ToString(),
                command = "SUBS",
                SchwabClientCustomerId = streamerInfo.schwabClientCustomerId,
                SchwabClientCorrelId = streamerInfo.schwabClientCorrelId,
                parameters = new Parameters
                {
                    keys = symbols,
                    fields = FieldsSort(fields) // must be in assending order
                }
            };
            var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            equitiesCallback = callback;
            SendRequest(req);
        }

        /// <summary>
        /// Add symbols to streaming list (existing EquitiesRequest())
        /// </summary>
        /// <param name="symbols"></param>
        /// <exception cref="SchwabApiException"></exception>
        public void EquitiesAdd(string symbols)
        {
            if (equitiesCallback == null)
                throw new SchwabApiException("EquitiesRequest() must happen before a EquitiesAdd().");

            var list = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // add list
            symbols = "";
            foreach(var s in list)
            {
                if (!activeEquitySymbols.Contains(s))
                {
                    activeEquitySymbols.Add(s);
                    symbols += "," + s;
                }
            }

            if (symbols.Length > 0)
            {
                var request = new Request
                {
                    service = "LEVELONE_EQUITIES",
                    requestid = (++requestid).ToString(),
                    command = "ADD",
                    SchwabClientCustomerId = streamerInfo.schwabClientCustomerId,
                    SchwabClientCorrelId = streamerInfo.schwabClientCorrelId,
                    parameters = new Parameters
                    {
                        keys = symbols.Substring(1),
                        fields = "0" // at this time its required to send a value but doesn't affect the fields returned
                        // fields = FieldsSort(fields) // must be in assending order
                    }
                };
                var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                SendRequest(req);
            }
        }

        /// <summary>
        /// remove symbols from streaming list
        /// </summary>
        /// <param name="symbols"></param>
        /// <exception cref="SchwabApiException"></exception>
        public void EquitiesRemove(string symbols)
        {
            if (equitiesCallback == null)
                throw new SchwabApiException("EquitiesRequest() must happen before a EquitiesRemove().");

            var list = symbols.Split(',').Select(r => r.Trim()).Distinct().ToList(); // add list
            symbols = "";
            foreach (var s in list)
            {
                if (activeEquitySymbols.Contains(s))
                {
                    activeEquitySymbols.Remove(s);
                    symbols += "," + s;
                    var i = levelOneEquities.Where(r=> r.key == s).SingleOrDefault();
                    if (i != null)
                        levelOneEquities.Remove(i); // don't process anymore
                }
            }

            if (symbols.Length > 0)
            {
                var request = new Request
                {
                    service = "LEVELONE_EQUITIES",
                    requestid = (++requestid).ToString(),
                    command = "UNSUBS",
                    SchwabClientCustomerId = streamerInfo.schwabClientCustomerId,
                    SchwabClientCorrelId = streamerInfo.schwabClientCorrelId,
                    parameters = new Parameters
                    {
                        keys = symbols.Substring(1)
                    }
                };
                var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                SendRequest(req);
            }
        }


        /// <summary>
        /// Change fields being streamed
        /// </summary>
        /// <param name="fields"></param>
        /// <exception cref="SchwabApiException"></exception>
        public void EquitiesView(string fields)
        {
            if (equitiesCallback == null)
                throw new SchwabApiException("EquitiesRequest() must happen before a EquitiesView().");

            var request = new Request
            {
                service = "LEVELONE_EQUITIES",
                requestid = (++requestid).ToString(),
                command = "VIEW",
                SchwabClientCustomerId = streamerInfo.schwabClientCustomerId,
                SchwabClientCorrelId = streamerInfo.schwabClientCorrelId,
                parameters = new Parameters
                {
                    fields = fields
                }
            };
            var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            SendRequest(req);
        }

        enum Services
        {                             // Delevery Type  Description
            ADMIN,                    //
            LEVELONE_EQUITIES,        // Change         Level 1 Equities Change
            LEVELONE_OPTIONS,         // Change         Level 1 Options Change
            LEVELONE_FUTURES,         // Change         Level 1 Futures Change
            LEVELONE_FUTURES_OPTIONS, // Change         Level 1 Futures Options Change
            LEVELONE_FOREX,           // Change         Level 1 Forex Change
            NYSE_BOOK,                // Whole          Level Two book for Equities Whole
            NASDAQ_BOOK,              // Whole          Level Two book for Equities Whole
            OPTIONS_BOOK,             // Whole          Level Two book for Options Whole
            CHART_EQUITY,             // All Sequence   Chart candle for Equities All Sequence
            CHART_FUTURES,            // All Sequence   Chart candle for Futures All Sequence
            SCREENER_EQUITY,          // Whole          Advances and Decliners for Equities Whole
            SCREENER_OPTION,          // Whole          Advances and Decliners for Options Whole
            ACCT_ACTIVITY             // All Sequence   Get account activity information such as order fills, etc
        }

        enum Commands 
        {
            LOGIN,  // Initial request when opening a new connection. This must be successful before sending other commands.
            SUBS,   // Subscribes to a set of symbols or keys for a particular service.
                    //    This overwrites all previously subscribed symbols for that service.
                    //    This is a convenient way to wipe out old subscription list and start fresh, but it's not the most efficient.
                    //    If you only want to add one symbol to 300 already subscribed, use an ADD instead.
            ADD,    // Adds a new symbol for a particular service. This does NOT wipe out previous symbols
                    //    that were already subscribed. It is OK to use ADD for first subscription command instead of SUBS.
            UNSUBS, // This unsubscribes a symbol to a list of subscribed symbol for a particular service.
            VIEW,   // This changes the field subscription for a particular service. It will apply to all symbols for that particular service.
            LOGOUT  // Logs out of the streamer connection. Streamer will close the connection.
        }

        public class StreamerRequests
        {
            public List<Request> requests { get; set; } = new List<Request>();

            public class Request
            {
                public string requestid { get; set; }
                public string service { get; set; }
                public string command { get; set; }
                public string SchwabClientCustomerId { get; set; }
                public string SchwabClientCorrelId { get; set; }
                public Parameters parameters { get; set; } = new Parameters();
            }

            public class Parameters
            {
                public string Authorization { get; set; }          // Access token as found from POST Token endpoint.
                public string SchwabClientChannel { get; set; }    // Identifies the channel as found through the GET User Preferences endpoint.
                public string SchwabClientFunctionId { get; set; } // Identifies the page or source in the channel where quote is being called
                                                                   //    from (5 alphanumeric). Found through the GET User Preferences endpoint.
                [DefaultValue(null)]
                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? goslevel { get; set; }

                [DefaultValue(null)]
                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public string? keys { get; set; }

                [DefaultValue(null)]
                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
                public string? fields { get; set; }
            }

        }

        public class LoginResponse
        {
            public string requestid { get; set; }
            public string service { get; set; }
            public string command { get; set; }
            public string SchwabClientCorrelId { get; set; }
            public DateTime timestamp { get; set; }

            // more ??
        }


        
        public class ResponseMessage
        {
            public List<Response> response { get; set; }

            public class Response
            {
                public string service { get; set; }
                public string command { get; set; }
                public string requestid { get; set; }
                public string SchwabClientCorrelId { get; set; }
                public long timestamp { get; set; }
                public Content content { get; set; }
            }
            public class Content
            {
                public int code { get; set; }
                public string msg { get; set; }
            }
        }


        public class NotifyMessage
        {
            public List<Notify> notify { get; set; }

            public class Notify
            {
                public string service { get; set; }
                public long timestamp { get; set; }
                public Content content { get; set; }
            }
            public class Content
            {
                public int code { get; set; }
                public string msg { get; set; }
            }
        }

        public class DataMessage
        {
            public List<DataItem> data { get; set; }

            public class DataItem
            {
                public string service { get; set; }
                public long timestamp { get; set; }
                public string command { get; set; }
                public List<Content> content { get; set; }
            }

            public class Content
            {
                public string key { get; set; }
                public bool delayed { get; set; }
                public string assetMainType { get; set; }
                public string assetSubType { get; set; }
                public string cusip { get; set; }

                /* - parse these manually
                [JsonProperty("1")]
                public double _1 { get; set; }
                */
            }
        }
    }
}
