// <copyright file="SchwabApi.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

// Version 7.1.0 - released 2024-07-15
// Version 7.0.0 - released 2024-07-09?
// Version 6.0.2 - released 2024-07-05
// Version 6.0.1 - released 2024-07-04
// Version 6.0.0 - released 2024-07-03
// Version 05 - released 2024-06-28
// Version 04 - released 2024-06-20
// Version 03 - released 2024-06-13

using System;
using System.Text;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace SchwabApiCS
{
    // handy tools:  https://json2csharp.com and https://jsonformatter.org
    // https://kdiff3.sourceforge.net/ - great tool for showing version code changes - compare current release with a new one

    public partial class SchwabApi
    {
        public const string Version = "7.1.0";

        /* ============= Accounts and Trading Production ===============================================================
         *   Method                     Endpoint                                     Description
         *   ** = not implemented
         * ACCOUNTS                     Accounts.cs
         *   GetAccountNumbers()        GET /accounts/accountNumbers                 Get list of account numbers and their encrypted values
         *   GetAccounts()              GET /accounts                                Get linked account(s) balances and positions for the logged in user.
         *   GetAccount()               GET /accounts/{accountNumbers}               Get a specific account balance and positions for the logged in user.
         * 
         * ORDERS                       GetOrders.cs
         *   GetOrders({account#})      GET    /accounts/{account#}/orders           Get all orders for a specific account.
         *   GetOrder()                 GET    /accounts/{account#}/orders/{orderId} Get a specific order by its ID, for a specific account
         *   GetOrders()                GET    /orders                               Get all orders for all accounts
         *   
         *                              Orders/OrderBase.cs
         *   OrderExecuteNew()          POST   /accounts/{account#}/orders           Place order for a specific account - All new order variations use this
         *   OrderExecuteReplace()      POST   /accounts/{account#}/orders/{orderId} Place an order - All replace order variations use this
         *   OrderExecuteDelete()       DELETE /accounts/{account#}/orders/{orderId} Cancel an order for a specific account- All replace order variations should use this
         *   
         *                              See Orders folder for common order types that can be used as examples for more complex orders.
         *   OrderBuySingle()                                                        Place a simple limit or market buy order.
         *   OrderSellSorder()                                                       Place a simple limit or market sell order.
         *   OrderStopLoss()                                                         Place a simple stop loss sell order.
         *   OrderFirstTriggers()       Good example for a more complex order        Place a simple buy/sell order with a triggered second order
         *   
         *   **                         POST   /accounts/{account#}/previewOrder     Preview order for a specific account. **Coming Soon**.
         *  
         * TRANSACTIONS                 Transactions.cs
         *   GetAccountTransactions()   GET  /accounts/{account#}/transactions                  Get all transactions information for a specific account.
         *   GetAccountTransaction(id)  GET  /accounts/{account#}/transactions/{transactionId}  Get user preference information for the logged in user.
         *  
         * USER PREFERENCES             UserPreference.cs
         *   GetUserPreferences()       GET  /UserPreference                         Get user preference information for the logged in user.
         *  
         *  
         * ========== MARKET DATA  ====================================================================================
         * QUOTES                       MarketData.cs
         *   GetQuotes()                GET /quotes                                  Get Quotes by list of symbols.
         *   GetQuote()                 GET /{symbol_id}/quotes                      Get Quote by single symbol.
         *  
         * OPTION CHAINS                Options.cs
         *   GetOptionChain()           GET /chains                                  Get option chain for an optionable Symbol
         *  
         * OPTION EXPERIRATION CHAIN    Options.cs
         *   GetOptionExpirationChain() GET /expirationchain                         Get option expiration chain for an optionable symbol
         *  
         * PRICE HISTORY                MarketData.cs
         *   GetPriceHistory()          GET /pricehistory                            Get PriceHistory for a single symbol and date ranges.
         *  
         * MOVERS                       MarketData.cs
         *   GetMovers()                GET /movers/{index_symbol}                   Get Movers for a specific index.
         *  
         * MARKET HOURS                 MarketData.cs
         *   GetMarketHours()           GET /markets                                 Get Market Hours for all markets.
         *   ** not needed              GET /markets/{market_id}                     Get Market Hours for a single market.
         *  
         * INSTRUMENTS                  Instruments.cs
         *   GetInstrumentsBySymbol()   GET /instruments                             Get Instruments by symbols and projections.
         *   GetInstrumentsByCusipId()  GET /instruments/{cusip_id}                  Get Instrument by specific cusip
         *   
         * ========== STREAMERS  ====================================================================================  
         * AccountActivities
         * LevelOneEquities
         * LevelOneOptions
         * LevelOneFutures
         * LevelOneFuturesOptions -- Not implemented by Schwab yet 
         * LevelOneForexes
         * NasdaqBooks -- level 2 Nasdaq
         * NyseBooks -- level 2 Nyse
         * OptionsBooks -- level 2 options
         * ChartEquities -- minute candles stream
         * ChartFutures -- minute candles stream
         * ScreenerEquities -- Not implemented by Schwab yet
         * ScreenerOptions  -- Not implemented by Schwab yet
         */

        public UserPreferences? userPreferences; // load once

        /// <summary>
        /// Every 7 days user must sign in and reauthorize.
        /// </summary>
        public bool NeedsReAuthorization { get { return schwabTokens.NeedsReAuthorization; } }


        internal static SchwabTokens schwabTokens;
        internal IList<AccountNumber> accountNumberHashs; // load once
        const string utcDateFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        internal static JsonSerializerSettings jsonSettings = new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Error };

        /// <summary>
        /// Schwab API class
        /// </summary>
        /// <param name="schwabTokens"></param>
        public SchwabApi(SchwabTokens schwabTokens)
        {
            SchwabApi.schwabTokens = schwabTokens;

            if (schwabTokens.NeedsReAuthorization)
                throw new SchwabApiException("Tokens needs reauthorization. Should be caught before this!");

            var t = GetUserPreferencesAsync();
            var t2 = GetAccountNumbersAsync();
            Task.WaitAll(t, t2);
            userPreferences = t.Result.Data;
            accountNumberHashs = t2.Result.Data;
        }

        private string _httpClientAccessToken = "";
        private HttpClient _hiddenHttpClient;
        private HttpClient httpClient
        {
            get
            { // check every access to see if expired.
                if (schwabTokens.AccessToken != _httpClientAccessToken)
                { // recreate _hiddenHttpClient is token has changed
                    _httpClientAccessToken = schwabTokens.AccessToken; // will refresh or authorize as needed.
                    _hiddenHttpClient = new HttpClient();
                    _hiddenHttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _httpClientAccessToken);
                }
                return _hiddenHttpClient;
            }
        }

        internal static DateTime? GetDate(string dateStr, ref DateTime? privateDate)
        { // dateStr format = "2024-03-21 00:00:00.0"
            if (privateDate != null)
                return privateDate;
            if (dateStr == null)
                return null;

            privateDate = Convert.ToDateTime(dateStr);
            return privateDate;
        }

        internal static DateTime? GetDate(long dateLong, ref DateTime? privateDate)
        { // dateStr format = "2024-03-21 00:00:00.0"
            if (privateDate != null)
                return privateDate;
            if (dateLong == null)
                return null;

            privateDate = ApiDateTime_to_DateTime(dateLong);
            return privateDate;
        }

        /// <summary>
        /// Method to wait for async operation to complete and return results.
        /// will throw an SchwabApiException error if result.HasError is true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="memberName">This will get the caller's method name, no need to provide, but can be id desired. Used in error messages.</param>
        /// <returns>results of api servise call</returns>
        /// <exception cref="Exception"></exception>
        private T WaitForCompletion<T>(Task<ApiResponseWrapper<T>> task, [CallerMemberName] string memberName = "")
        {
            task.Wait();
            if (task.Result.HasError)
            {
                throw new SchwabApiException<T>(task.Result, (memberName == "" ? "error: " : memberName + " error: ") + task.Result.ResponseCode + " " + task.Result.ResponseText);
            }
            return task.Result.Data;
        }

        /// <summary>
        /// Used to hold data or error info resulting from a async request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class ApiResponseWrapper<T>
        {
            /// <summary>
            /// To hold data or error info resulting from a async request
            /// </summary>
            /// <param name="data"></param>
            /// <param name="hasError"></param>
            /// <param name="responseCode"></param>
            /// <param name="responseText"></param>
            /// <param name="responseMessage"></param>
            public ApiResponseWrapper(T data, bool hasError, int responseCode, string responseText)
            {
                Data = data;
                this.HasError = hasError;
                this.ResponseCode = responseCode;
                this.ResponseText = responseText;
            }
            
            /// <summary>
            /// To hold data or error info resulting from a async request
            /// </summary>
            /// <param name="data"></param>
            /// <param name="hasError"></param>
            /// <param name="responseCode"></param>
            /// <param name="responseText"></param>
            /// <param name="responseMessage"></param>
            public ApiResponseWrapper(T data, bool hasError, int responseCode, string responseText, HttpResponseMessage? responseMessage = null)
            {
                Data = data;
                this.HasError = hasError | (ResponseMessage == null ? false : !responseMessage.IsSuccessStatusCode);
                this.ResponseCode = responseCode;
                this.ResponseText = responseText;
                this.ResponseMessage = responseMessage;
            }

            /// <summary>
            /// To hold data or error info resulting from a async request
            /// </summary>
            /// <param name="data"></param>
            /// <param name="hasError"></param>
            /// <param name="responseCode"></param>
            /// <param name="responseText"></param>
            /// <param name="responseMessage"></param>
            /// <param name="apiException">save any trapped exception and set HasError if it's a show stopper.</param>
            public ApiResponseWrapper(T data, bool hasError, HttpResponseMessage? responseMessage, Exception? apiException = null)
            {
                Data = data;
                this.ResponseMessage = responseMessage;
                this.apiException = apiException;

                if (responseMessage != null)
                {
                    this.HasError = hasError | !responseMessage.IsSuccessStatusCode;
                    this.ResponseCode = (int)responseMessage.StatusCode;
                    this.ResponseText = responseMessage.ReasonPhrase;
                }
                else if (apiException is AggregateException) // must have apiException then
                {
                    var ex = (SchwabApiException)apiException.InnerException;
                    this.HasError = hasError;
                    this.ResponseCode =(int)(ex.Response.StatusCode);
                    this.ResponseText = ex.Message;
                }
            }

            /// <summary>
            /// To hold data or error info resulting from a async request
            /// </summary>
            /// <param name="data"></param>
            /// <param name="hasError"></param>
            /// <param name="responseCode"></param>
            /// <param name="responseText"></param>
            /// <param name="responseMessage"></param>
            /// <param name="apiException">save any trapped exception and set HasError if it's a show stopper.</param>
            public ApiResponseWrapper(T data, bool hasError, int responseCode, string responseText, Exception? apiException = null)
            {
                RawData = data;
                this.HasError = hasError;
                this.ResponseCode = responseCode;
                this.ResponseText = responseText;
                this.apiException = apiException;
            }

            public T RawData { get; set; }
            public bool HasError { get; set; }
            public int ResponseCode { get; set; }
            public string ResponseText { get; set; }
            public HttpResponseMessage? ResponseMessage { get; set; }
            public Exception? apiException { get; set; }


            /// <summary>
            /// When accessing Data, throw an error if there is an error
            /// </summary>
            public T Data {
                get
                {
                    if (HasError)
                    {
                        //if (ResponseMessage != null)
                            throw new SchwabApiException<T>(this, "error: " + ResponseCode + " " + ResponseText);
                        //else
                        //   throw new SchwabApiException(apiException, "error: " + ResponseCode + " " + ResponseText, );
                    }
                    return RawData;
                }
                set { RawData = value; }
            }

            public string Message
            {
                get
                {
                    if (ResponseCode != null)
                        return ResponseCode.ToString() + " " + ResponseText;
                    if (apiException != null)
                        return apiException.Message;
                    return "";
                }
            }

            public string Url
            {
                get
                {
                    if (ResponseMessage == null)
                        return "";
                    return ResponseMessage.RequestMessage.RequestUri.PathAndQuery;
                }
            }


            public string? SchwabClientCorrelId
            {
                get
                {
                    if (ResponseMessage == null)
                        return null;
                    var i = ResponseMessage.Headers.Where(r => r.Key == "Schwab-Client-Correlid").FirstOrDefault();
                    if (i.Value == null) 
                        return null;

                    return i.Value.First<string>();
                }
            }
        }

        /// <summary>
        /// convert string to base64
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns>base64 string</returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        // =========== Date & Time ==================================================================
        /// <summary>
        /// Schwab API start time. add schwab's milliseconds (long) to epoch to get DateTime
        /// </summary>
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-5); // adjust for time zone

        /// <summary>
        /// Convert Schwab API time(long) to DateTime
        /// </summary>
        /// <param name="schwabTime"></param>
        /// <returns></returns>
        public static DateTime ApiDateTime_to_DateTime(long schwabTime)
        {
            return epoch.AddMilliseconds(schwabTime);
        }

        /// <summary>
        /// Convert DateTime to milliseconds since epoch
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>Schwab long, total milliseconds since 1/1/1970 (epoch)</returns>
        public static long DateTime_to_ApiDateTime(DateTime dateTime)
        {

            TimeSpan ts = dateTime.ToUniversalTime() - epoch;
            return (long)Math.Floor(ts.TotalMilliseconds);
        }

        public static DateTime? ConvertDateOnce(long? schwabApiDateTime, ref DateTime? cachedDateTime)
        {
            if (schwabApiDateTime == null)
                return null;

            if (cachedDateTime == null)
                cachedDateTime = SchwabApi.ApiDateTime_to_DateTime((long)schwabApiDateTime);
            return cachedDateTime;
        }

        public static DateTime ConvertDateOnce(long schwabApiDateTime, ref DateTime? cachedDateTime)
        {
            if (cachedDateTime == null)
                cachedDateTime = SchwabApi.ApiDateTime_to_DateTime(schwabApiDateTime);
            return (DateTime)cachedDateTime;
        }

        // =============================================================================

        /// <summary>
        /// method to transform json response string to a json string that can be parsed.
        /// </summary>
        /// <param name="json">json string</param>
        /// <returns>json string</returns>
        public delegate string ResponseTransform(string json);

        /// <summary>
        /// Generic Schwab Get request
        /// </summary>
        /// <typeparam name="T">expected return type. Use string type to return json string response</typeparam>
        /// <param name="url">complete url of request</param>
        /// <param name="responseTransform">optional medthod to tranform json response string before processing</param>
        /// <returns>Task<ApiResponseWrapper<T?>></returns>
        public async Task<ApiResponseWrapper<T>>? Get<T>(string url, ResponseTransform? responseTransform = null)
        {
            string responseString;
            HttpResponseMessage response = null;
            try
            {
                T? data;
                var taskResponse = httpClient.GetAsync(url);
                taskResponse.Wait();
                response = taskResponse.Result;

                if (!response.IsSuccessStatusCode)
                    return new ApiResponseWrapper<T>(default, true, (int)response.StatusCode, response.ReasonPhrase, response);

                responseString = await response.Content.ReadAsStringAsync();

                if (responseString == null)
                    return new ApiResponseWrapper<T>(default, true, (int)response.StatusCode, response.ReasonPhrase + ", null content.", response);

                if (responseTransform != null)
                    responseString = responseTransform(responseString);

                if (typeof(T) == typeof(String))
                    data = (T)Convert.ChangeType(responseString, typeof(T)); // return json string unchanged
                else
                {
                    try
                    {
                        data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseString, jsonSettings);
                    }
                    catch (Exception ex)
                    {
                        data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseString);
                    }
                }

                if (data == null)
                    return new ApiResponseWrapper<T>(default, true, (int)response.StatusCode, response.ReasonPhrase + ", null JsonConvert.", response);

                return new ApiResponseWrapper<T>(data, false, (int)response.StatusCode, response.ReasonPhrase, response);
            }
            catch (Exception ex)
            {
                return new ApiResponseWrapper<T>(default, true, response, ex);
            }
        }


        /// <summary>
        /// Generic Schwab Post request
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="url">complete url of request</param>
        /// <param name="content">data to send with request</param>
        /// <returns>Task<ApiResponseWrapper<T?>></returns>
        public async Task<ApiResponseWrapper<T?>> Post<T>(string url, object? content = null)
        {

            var c = new StringContent(content.ToString(), Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(url, c).Result;

            return new ApiResponseWrapper<T>(default, false, (int)response.StatusCode, response.ReasonPhrase, response);
        }

        /// <summary>
        /// Generic Schwab Put request
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="url">complete url of request</param>
        /// <param name="content">data to send with request</param>
        /// <returns>Task<ApiResponseWrapper<T?>></returns>
        public async Task<ApiResponseWrapper<T?>> Put<T>(string url, object? content = null)
        {

            var c = new StringContent(content.ToString(), Encoding.UTF8, "application/json");
            var response = httpClient.PutAsync(url, c).Result;
            return new ApiResponseWrapper<T>(default, false, (int)response.StatusCode, response.ReasonPhrase, response);
        }

        /// <summary>
        /// Generic Schwab Delete request
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Task<true/false></true></returns>
        public async Task<ApiResponseWrapper<bool>> Delete(string url)
        {
            var task = httpClient.DeleteAsync(url);
            task.Wait();
            var r = task.Result;
            var responseString = await r.Content.ReadAsStringAsync();
            return new ApiResponseWrapper<bool>(r.IsSuccessStatusCode, !r.IsSuccessStatusCode, (int)r.StatusCode, r.ReasonPhrase ?? "", r);

        }

        /// <summary>
        /// Enum Convert, used by GetAssetType, GetDuration, GetSession, etc...
        /// </summary>
        /// <typeparam name="T">Enum type to convert to</typeparam>
        /// <param name="enumStringValue">string value to covert from</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T GetEnum<T>(string enumStringValue)
        {
            foreach (var t in (T[])Enum.GetValues(typeof(T)))
            {
                if (t.ToString() == enumStringValue)
                    return t;
            }
            throw new SchwabApiException("Invalid asset type '" + enumStringValue + "'");
        }

        /// <summary>
        /// Format Symbol for Display
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static string SymbolDisplay(string symbol)
        {
            if (symbol.Contains(' ')) // parse option symbol
            {
                var s = symbol.Substring(0, 6) + "20" + symbol.Substring(6, 2) + "-" + symbol.Substring(8, 2) + "-" + symbol.Substring(10, 2) +
                        (symbol[12] == 'C' ? " Call " : " Put ") + symbol.Substring(13, 5).TrimStart('0') + "." +
                        (symbol[20] == '0' ? symbol.Substring(18,2) : symbol.Substring(18));
                return s;

            }
            return symbol;
        }

        /// <summary>
        /// Check to see if too many symbols
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="maxCount"></param>
        /// <returns>true is too many</returns>
        public static bool SymbolMaxCheck(string symbols, int maxCount)
        {
            return (symbols.Length - symbols.Replace(",", "").Length >= maxCount);
        }

        // =========== Schwab Api Exceptions =========================================================================
        #region SchwabApiExceptions

        public class SchwabApiException : Exception
        {
            public SchwabApiException() { }
            public SchwabApiException(string message) : base(message) { }
            public SchwabApiException(string message, Exception inner) : base(message, inner) { }

            public object? ApiResponse;  // is a type of ApiResponseWrapper<T> for inspecting
            public HttpResponseMessage? Response { get; init; }
            public string? SchwabClientCorrelId { get; init; }

            public override string Message { 
                get
                {
                    var msg = base.Message + "\n\n" +
                           Url.Replace("?", "\n?");
                    if (!String.IsNullOrWhiteSpace(SchwabClientCorrelId))
                           msg += "\n\nSchwabClientCorrelId = " + SchwabClientCorrelId;
                    return msg;
                }
            }

            public string Url { 
                get {
                    if (Response == null)
                        return "";
                    return Response.RequestMessage.RequestUri.PathAndQuery;
                }  
            }

        }

        public class SchwabApiException<T> : SchwabApiException
        {
            public SchwabApiException(ApiResponseWrapper<T> apiResponse)
            {
                this.ApiResponse = apiResponse;
                this.Response = apiResponse.ResponseMessage;
                this.SchwabClientCorrelId = apiResponse.SchwabClientCorrelId;
            }

            public SchwabApiException(ApiResponseWrapper<T> apiResponse, string message)
                : base(message)
            {
                this.ApiResponse = apiResponse;
                this.Response = apiResponse.ResponseMessage;
                this.SchwabClientCorrelId = apiResponse.SchwabClientCorrelId;
            }
        }

        public class SchwabApiAuthorizationException : SchwabApiException
        {
            public SchwabApiAuthorizationException(HttpResponseMessage responseMessage, string message)
                : base(message)
            {
                this.Response = responseMessage;
            }
        }

        public static ExceptionMessageResult ExceptionMessage(Exception ex)
        {
            if (ex is AggregateException)
            {
                if (ex.InnerException is SchwabApiException)
                    return new ExceptionMessageResult("SchwabApiException", ((SchwabApiException)ex.InnerException).Message);

                return new ExceptionMessageResult("Exception", ex.InnerException.Message);
            }
            if (ex is SchwabApiException)
                return new ExceptionMessageResult("SchwabApiException", ex.Message);

            return new ExceptionMessageResult("Exception", ex.Message);
        }

        public class ExceptionMessageResult
        {
            public ExceptionMessageResult(string title, string message)
            {
                Title = title;
                Message = message;
            }

            public string Title { get; set; }
            public string Message { get; set; }
        }

        #endregion SchwabApiExceptions

    }

}
