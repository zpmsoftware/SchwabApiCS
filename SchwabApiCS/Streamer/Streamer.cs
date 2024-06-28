// <copyright file="Streamer.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.StreamerRequests;
using System.Security.Authentication;
using static SchwabApiCS.Streamer;


// https://json2csharp.com/
namespace SchwabApiCS
{
    public partial class Streamer
    {
        public bool IsLoggedIn {  get { return isLoggedIn; } }
        public LevelOneEquitiesClass LevelOneEquities;
        public LevelOneOptionsClass LevelOneOptions;
        public AccountActivityClass AccountActivities;

        private LevelOneFuturesClass LevelOneFutures; // not ready yet

        private List<ServiceClass> ServiceList = new List<ServiceClass>();
        private UserPreferences.StreamerInfo streamerInfo;
        private SchwabApi schwabApi;
        private WebSocket4Net.WebSocket websocket;
        private long requestid = 0;
        internal bool isLoggedIn = false;
        private List<string> requestQueue = new List<string>();

        public Streamer(SchwabApi schwabApi)
        {
            ServiceList.Add(LevelOneEquities = new LevelOneEquitiesClass(this));
            ServiceList.Add(LevelOneOptions = new LevelOneOptionsClass(this));
            ServiceList.Add(LevelOneFutures = new LevelOneFuturesClass(this));
            ServiceList.Add(AccountActivities = new AccountActivityClass(this));

            ServiceList.Add(new AdminClass(this));

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
                            var service = ServiceList.Where(s => s.ServiceName == n.service).SingleOrDefault();
                            if (service == null)
                                throw new Exception("Streamer response service " + n.service + " not implemented");

                            service.Notify(n);
                        }
                        return;
                    }

                    if (messageEvent.Message.StartsWith("{\"response\":"))
                    {
                        var responses = JsonConvert.DeserializeObject<ResponseMessage>(messageEvent.Message);

                        foreach (var r in  responses.response)
                        {
                            var service = ServiceList.Where(s => s.ServiceName == r.service).SingleOrDefault();
                            if (service == null)
                                throw new Exception("Streamer response service " + r.service + " not implemented");

                            service.ProcessResponse(r);
                        }
                        return;
                    }
                    if (messageEvent.Message.StartsWith("{\"data\":"))
                    {
                        var dataMsg = JsonConvert.DeserializeObject<DataMessage>(messageEvent.Message);
                        var msg = JsonConvert.DeserializeObject<dynamic>(messageEvent.Message);

                        for(var i=0; i<dataMsg.data.Count; i++)
                        {
                            var d = dataMsg.data[i];
                            var service = ServiceList.Where(s => s.ServiceName == d.service).SingleOrDefault();
                            if (service == null)
                                throw new Exception("Streamer data service " + d.service + " not implemented");

                            service.ProcessData(d, msg.data[i].content);
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

        /// <summary>
        /// Sewrvice Request
        /// </summary>
        /// <param name="service">service name</param>
        /// <param name="symbols">comma separated list of symbols</param>
        /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquities.Fields</param>
        private void ServiceRequest(Services service, string symbols, string fields)
        {
            var request = new Request
            {
                service = service.ToString(),
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
            SendRequest(req);
        }

        /// <summary>
        /// Add symbols to service
        /// </summary>
        /// <param name="symbols"></param>
        /// <exception cref="SchwabApiException"></exception>
        private void ServiceAdd(Services service, string symbols, List<string> activeSymbols)
        {
            var list = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // add list
            symbols = "";
            foreach (var s in list)
            {
                if (!activeSymbols.Contains(s))
                {
                    activeSymbols.Add(s);
                    symbols += "," + s;
                }
            }
            if (symbols.Length > 0)
            {
                var request = new Request
                {
                    service = service.ToString(),
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
        private void ServiceRemove(Services service, string symbols)
        {
            var request = new Request
            {
                service = service.ToString(),
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


        /// <summary>
        /// Change fields being streamed
        /// </summary>
        /// <param name="fields"></param>
        /// <exception cref="SchwabApiException"></exception>
        private void ServiceView(Services service, string fields)
        {
            var request = new Request
            {
                service = service.ToString(),
                requestid = (++requestid).ToString(),
                command = "VIEW",
                SchwabClientCustomerId = streamerInfo.schwabClientCustomerId,
                SchwabClientCorrelId = streamerInfo.schwabClientCorrelId,
                parameters = new Parameters
                {
                    fields = FieldsSort(fields) // must be in assending order
                }
            };
            var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            SendRequest(req);
        }

        internal enum Services
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

        public abstract class ServiceClass
        {
            protected Streamer streamer;
            public string ServiceName { get; init; }
            internal ServiceClass(Streamer streamer, Streamer.Services service)
            {
                this.streamer = streamer;
                this.ServiceName = service.ToString();
            }

            internal virtual void ProcessResponse(ResponseMessage.Response response)
            {
                throw new Exception("Streamer service " + ServiceName + " not expecting Response Message: " + response.ToString());
            }

            internal virtual void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                throw new Exception("Streamer service " + ServiceName + " not expecting Data Message: " + d.ToString());
            }

            internal virtual void Notify(NotifyMessage.Notify notify)
            {
                throw new Exception("Streamer service " + ServiceName + " not expecting Notify Message: " + notify.ToString());
            }
        }

        public static string FieldsSort(string fields)
        {
            var f = fields.Split(',');
            Array.Sort(f, (s1, s2) =>
            { // sort numeric strings in numeric order.  Leading 0 is not allowd!
                if (s1.Length == s2.Length)
                    return s1.CompareTo(s2);
                return (s1.Length >= s2.Length) ? 1 : -1; // longer one is always larger
            });
            return string.Join(",", f);
        }
    }
}
