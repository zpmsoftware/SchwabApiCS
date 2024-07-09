using System;
using static SchwabApiCS.SchwabApi;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public abstract class Service
        {
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

            protected Streamer streamer;

            internal Services service { get; init; }
            public string ServiceName { get; init; }
            public string ReferenceName { get; init; }

            protected List<string> ActiveSymbols = new List<string>(); // only accept streamed data from this list



            internal Service(Streamer streamer, Services service, string referenceName)
            {
                this.streamer = streamer;
                this.service = service;
                this.ServiceName = service.ToString();
                ReferenceName = referenceName;
            }

            internal Service(Streamer streamer, string referenceName)  // need to keep Service Private for Books
            {
                this.streamer = streamer;
                ReferenceName = referenceName;
            }

            /// <summary>
            /// process received data
            /// </summary>
            /// <param name="response"></param>
            /// <exception cref="Exception"></exception>
            internal void ProcessResponse(ResponseMessage.Response response)
            {
                if (response.content.code != 0)
                {
                    throw new Exception(string.Format(
                        "streamer service" + service.ToString() + " {0} Error: {1} {2} ", response.command, response.content.code, response.content.msg));
                }

                switch (response.command)
                {
                    case "SUBS":
                        ProcessResponseSUBS(response);
                        break;
                    case "ADD":
                        ProcessResponseADD(response);
                        break;
                    case "UNSUBS":
                        ProcessResponseUNSUBS(response);
                        break;
                    case "LOGIN":
                        ProcessResponseLOGIN(response);
                        break;

                    default:
                        ThrowReponseException(response);
                        break;
                }
            }

            private void ThrowReponseException(ResponseMessage.Response response)
            {
                throw new SchwabApiException(
                    string.Format("streamer service {0} not expecing {1} response. content.code={2}, content.msg={3}",
                                  ServiceName, response.command, response.content.code, response.content.msg)
                );
            }

            internal virtual void ProcessResponseSUBS(ResponseMessage.Response response)
            {
                ThrowReponseException(response); // expect this to be overridden
            }

            internal virtual void ProcessResponseADD(ResponseMessage.Response response)
            {
                // do nothing by default
            }

            internal virtual void ProcessResponseUNSUBS(ResponseMessage.Response response)
            {
                // do nothing by default
            }
            
            internal virtual void ProcessResponseLOGIN(ResponseMessage.Response response)
            {
                ThrowReponseException(response); // expect this to be overridden
            }

            internal virtual void ProcessData(Streamer.DataMessage.DataItem d, dynamic content)
            {
                throw new Exception("Streamer service " + ServiceName + " not expecting Data Message: " + d.ToString());
            }

            internal virtual void Notify(Streamer.NotifyMessage.Notify notify)
            {
                throw new Exception("Streamer service " + ServiceName + " not expecting Notify Message: " + notify.ToString());
            }

            internal abstract void RemoveFromData(string symbol);

            public void SetActiveSymbols(string symbols)
            {
                ActiveSymbols = symbols.ToUpper().Split(',').Select(r => r.Trim()).Distinct().ToList(); // new list
            }

            /// <summary>
            /// Remove symbols from ActiveSymbols list and remove from streaming data list
            /// </summary>
            /// <param name="symbols"></param>
            /// <returns>list if actual symbols that were in list</returns>
            public string ActiveSymbolsRemove(string symbols)
            {
                var list = symbols.Split(',').Select(r => r.Trim()).Distinct().ToList(); // add list
                symbols = "";
                foreach (var s in list)
                {
                    if (ActiveSymbols.Contains(s))
                    {
                        ActiveSymbols.Remove(s);
                        symbols += "," + s;
                        RemoveFromData(s);
                    }
                }
                return symbols; // returns list of symbols actually found.
            }

            public void CallbackCheck(object? callback, string callerName = "")
            {
                if (callback == null)
                    throw new SchwabApiException(ReferenceName + ".Request() must happen before " + ReferenceName + "." + callerName + "().");
            }
        }

    }
}