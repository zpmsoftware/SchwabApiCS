// <copyright file="MarketData.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.Json;

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        // Market data implementation
        private const string MarketDataBaseUrl = "https://api.schwabapi.com/marketdata/v1";


        // =====  QUOTES =========================================================================================

        public enum QuoteFields { quote, fundamental, extended, reference, regular };

        /// <summary>
        /// Get Quotes
        /// </summary>
        /// <param name="symbols">Comma seperated list</param>
        /// <param name="indicative">Include indicative symbol quotes for all ETF symbols in request</param>
        /// <param name="fields">any combination of quote (default), fundamental, extended, reference, regular</param>
        /// <returns></returns>
        public List<Quote> GetQuotes(string symbols, bool indicative, string? fields = null)
        {
            return WaitForCompletion(GetQuotesAsync(symbols, indicative, fields));
        }

        /// <summary>
        /// Get Quotes async
        /// </summary>
        /// <param name="symbols">Comma seperated list</param>
        /// <param name="indicative">Include indicative symbol quotes for all ETF symbols in request</param>
        /// <param name="fields">any combination of quote (default), fundamental, extended, reference, regular</param>
        /// <returns>Task<List<Quote>></returns>
        public async Task<ApiResponseWrapper<List<Quote>>> GetQuotesAsync(string symbols, bool indicative, string? fields = null)
        {
            if (SymbolMaxCheck(symbols, 300))
                return new ApiResponseWrapper<List<Quote>>(default, true, 900, "Too many symbols, maximum is 300");

            var url = MarketDataBaseUrl + "/quotes?symbols=" + symbols + "&indicative=" + indicative.ToString().ToLower();
            if (fields != null)
                url += "&fields=" + fields;
            return await Get<List<Quote>>(url, ConvertToQuoteJsonArray);
        }

        /// <summary>
        /// Get Quotes
        /// </summary>
        /// <param name="symbols">Comma seperated list</param>
        /// <param name="fields">any combination of quote (default), fundamental, extended, reference, regular</param>
        /// <returns></returns>
        public List<Quote> GetQuotes(string symbols, string? fields = null)
        {
            return WaitForCompletion(GetQuotesAsync(symbols, fields));
        }

        /// <summary>
        /// Get Quotes async
        /// </summary>
        /// <param name="symbols">Comma seperated list</param>
        /// <param name="fields">any combination of quote (default), fundamental, extended, reference, regular</param>
        /// <returns>Task<List<Quote>></returns>
        public async Task<ApiResponseWrapper<List<Quote>>> GetQuotesAsync(string symbols, string? fields = null)
        {
            if (SymbolMaxCheck(symbols, 300))
                return new ApiResponseWrapper<List<Quote>>(default, true, 900, "Too many symbols, maximum is 300");

            var url = MarketDataBaseUrl + "/quotes?symbols=" + symbols;
            if (fields != null)
                url += "&fields=" + fields;
            return await Get<List<Quote>>(url, ConvertToQuoteJsonArray);
        }

        /// <summary>
        /// Get quote for a single symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fields">blank for all, any combination of quote, fundamental, extended, reference, regular</param>
        /// <returns>Quote</returns>
        public Quote GetQuote(string symbol, string? fields = null)
        {
            return WaitForCompletion(GetQuoteAsync(symbol, fields));
        }

        /// <summary>
        /// Get quote for a single symbol async
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fields">blank for all, any combination of quote, fundamental, extended, reference, regular</param>
        /// <returns>Task<Quote></Quote></returns>
        public async Task<ApiResponseWrapper<Quote>> GetQuoteAsync(string symbol, string? fields = null)
        {
            if (symbol.StartsWith("/")) // symbols that start with / must use Quotes
            {
                var url = MarketDataBaseUrl + "/quotes?symbols=" + symbol;
                if (fields != null)
                    url += "&fields=" + fields;
                return await Get<Quote>(url, ConvertQuotesToQuote);
            }
            else
            {
                var url = MarketDataBaseUrl + "/" + symbol + "/quotes";
                if (fields != null)
                    url += "?fields=" + fields;
                return await Get<Quote>(url, ConvertToQuote);
            }
        }

        private static string ConvertQuotesToQuote(string json)
        {
            var j = JObject.Parse(json);
            return j.First.First.ToString();
        }

        private static string ConvertToQuote(string json)
        {
            var j = JObject.Parse(json);
            return j.First.First.ToString();
        }

        private static string ConvertToQuoteJsonArray(string json)
        {
            json = "[" + json.Substring(1, json.Length - 2) + "]";

            var p = 1;
            do
            {
                var p2 = json.IndexOf(":{", p);
                json = json.Remove(p, p2 - p + 1);
                p = json.IndexOf("}},", p) + 3;
            } while (p != 2);
            return json;
        }

        public class Quote
        {
            public override string ToString()
            {
                if (invalidSymbols != null)
                    return $"Invalid Symbols:  {string.Join(", ", (string[])invalidSymbols)}";

                if (reference.product != null && reference.product != symbol) // future front month
                    return $"{reference.product} - ({symbol}) {assetMainType} {reference.description}, Mark ${quote.mark}";

                return $"{symbol} - {assetMainType} {reference.description}, Mark ${quote.mark}";
             }

            public string assetMainType { get; set; }
            public string assetSubType { get; set; }
            public string quoteType { get; set; }
            public bool realtime { get; set; }
            public long ssid { get; set; }
            public string symbol { get; set; }
            public Fundamental fundamental { get; set; }
            public QuotePrice quote { get; set; }
            public Reference reference { get; set; }
            public Regular regular { get; set; }
            public Extended extended { get; set; }
            public string[] invalidSymbols { get; set; }

            public class Extended
            {
                public decimal askPrice { get; set; }
                public int askSize { get; set; }
                public decimal bidPrice { get; set; }
                public int bidSize { get; set; }
                public decimal lastPrice { get; set; }
                public int lastSize { get; set; }
                public decimal mark { get; set; }
                public long quoteTime { get; set; }
                public long totalVolume { get; set; }
                public long tradeTime { get; set; }

                private DateTime? _quoteTime = null;
                public DateTime QuoteTime  // local time
                {
                    get { return ConvertDateOnce(quoteTime, ref _quoteTime); }
                    set { _quoteTime = value; }
                }

                private DateTime? _tradeTime = null;
                public DateTime TradeTime  // local time
                {
                    get { return ConvertDateOnce(tradeTime, ref _tradeTime); }
                    set { _quoteTime = value; }
                }
            }

            public class Fundamental
            {
                public decimal avg10DaysVolume { get; set; }
                public decimal avg1YearVolume { get; set; }
                public DateTime declarationDate { get; set; }
                public decimal divAmount { get; set; }
                public DateTime divExDate { get; set; }
                public int divFreq { get; set; }
                public decimal divPayAmount { get; set; }
                public DateTime divPayDate { get; set; }
                public decimal divYield { get; set; }
                public decimal eps { get; set; }
                public decimal fundLeverageFactor { get; set; }
                public DateTime lastEarningsDate { get; set; }
                public DateTime nextDivExDate { get; set; }
                public DateTime nextDivPayDate { get; set; }
                public decimal peRatio { get; set; }
            }

            public class QuotePrice
            {
                [JsonProperty("52WeekHigh")]
                public decimal _52WeekHigh { get; set; }

                [JsonProperty("52WeekLow")]
                public decimal _52WeekLow { get; set; }

                public string askMICId { get; set; }
                public decimal askPrice { get; set; }
                public int askSize { get; set; }
                public long askTime { get; set; }
                public string bidMICId { get; set; }
                public decimal bidPrice { get; set; }
                public int bidSize { get; set; }
                public long bidTime { get; set; }
                public decimal closePrice { get; set; }
                public decimal highPrice { get; set; }
                public string lastMICId { get; set; }
                public decimal lastPrice { get; set; }
                public int lastSize { get; set; }
                public decimal lowPrice { get; set; }
                public decimal mark { get; set; }
                public decimal markChange { get; set; }
                public decimal markPercentChange { get; set; }
                public decimal netChange { get; set; }
                public decimal netPercentChange { get; set; }
                public decimal nAV { get; set; } // used for mutual funds, SWVXX - money market
                public decimal openPrice { get; set; }
                public decimal postMarketChange { get; set; }
                public decimal postMarketPercentChange { get; set; }
                public long quoteTime { get; set; }
                public string securityStatus { get; set; }
                public long totalVolume { get; set; }
                public long tradeTime { get; set; }

                // Options
                public decimal? delta { get; set; }
                public decimal? gamma { get; set; }
                public decimal? theta { get; set; }
                public decimal? rho { get; set; }
                public decimal? vega { get; set; }
                public decimal? futurePercentChange { get; set; }
                public decimal? indAskPrice { get; set; }
                public decimal? indBidPrice { get; set; }
                public long? indQuoteTime { get; set; }
                public decimal? impliedYield { get; set; }
                public decimal? moneyIntrinsicValue { get; set; }
                public int? openInterest { get; set; } // futures
                public decimal? theoreticalOptionValue { get; set; }
                public decimal? timeValue { get; set; }
                public decimal? underlyingPrice { get; set; }
                public decimal? volatility { get; set; }

                // futures
                public bool quotedInSession { get; set; } // futures
                public long settleTime { get; set; } // futures
                public decimal tick { get; set; } // futures
                public decimal tickAmount { get; set; } // futures


                private DateTime? _settleTime = null;
                public DateTime SettleTime  // local time
                {
                    get { return ConvertDateOnce(settleTime, ref _settleTime); }
                    set { _settleTime = value; }
                }

                private DateTime? _bidTime = null;
                public DateTime BidTime  // local time
                {
                    get { return ConvertDateOnce(bidTime, ref _bidTime); }
                    set { _quoteTime = value; }
                }

                private DateTime? _askTime = null;
                public DateTime AskTime  // local time
                {
                    get { return ConvertDateOnce(askTime, ref _askTime); }
                    set { _quoteTime = value; }
                }

                private DateTime? _quoteTime = null;
                public DateTime QuoteTime  // local time
                {
                    get { return ConvertDateOnce(quoteTime, ref _quoteTime); }
                    set { _quoteTime = value; }
                }

                private DateTime? _tradeTime = null;
                public DateTime TradeTime  // local time
                {
                    get { return ConvertDateOnce(tradeTime, ref _tradeTime); }
                    set { _quoteTime = value; }
                }
            }

            public class Reference
            {
                public string cusip { get; set; }
                public string description { get; set; }
                public string exchange { get; set; }
                public string exchangeName { get; set; }
                public string? fsiCode { get; set; } // Financial status indicator, https://www.schwab.com/public/schwab/nn/qq/financial_status_indicator.html
                public string? fsiDesc { get; set; }

                public string? otcMarketTier { get; set; }
                public bool? isHardToBorrow { get; set; }
                public bool? isShortable { get; set; }
                public long? htbQuantity { get; set; }
                public decimal? htbRate { get; set; }
                public string contractType { get; set; }
                public int? daysToExpiration { get; set; }
                public string deliverables { get; set; }
                public string exerciseType { get; set; }
                public int? expirationDay { get; set; }
                public int? expirationMonth { get; set; }
                public string expirationType { get; set; }
                public int? expirationYear { get; set; }
                public bool? isPennyPilot { get; set; }
                public long? lastTradingDay { get; set; }
                public decimal? multiplier { get; set; }
                public string settlementType { get; set; }
                public decimal? strikePrice { get; set; }
                public string underlying { get; set; }
                public string underlyingAssetType { get; set; }
                public long? futureExpirationDate { get; set; }
                public bool? futureIsActive { get; set; }
                public decimal? futureMultiplier { get; set; }
                public string? futurePriceFormat { get; set; }
                public decimal? futureSettlementPrice { get; set; }
                public string? futureTradingHours { get; set; }
                public string? product { get; set; }
                public bool? isTradable { get; set; }


                private DateTime? _futureExpirationDate = null;
                public DateTime? FutureExpirationDate  // local time
                {
                    get { return ConvertDateOnce(futureExpirationDate, ref _futureExpirationDate); }
                    set { _futureExpirationDate = value; }
                }
            }

            public class Regular
            {
                public decimal regularMarketLastPrice { get; set; }
                public int regularMarketLastSize { get; set; }
                public decimal regularMarketNetChange { get; set; }
                public decimal regularMarketPercentChange { get; set; }
                public long regularMarketTradeTime { get; set; }

                private DateTime? _regularMarketTradeTime = null;
                public DateTime RegularMarketTradeTime  // local time
                {
                    get { return ConvertDateOnce(regularMarketTradeTime, ref _regularMarketTradeTime); }
                    set { _regularMarketTradeTime = value; }
                }
            }
        }

        // =========== PRICE HISTORY ============================================================================
        /*
         * Schwab API Price History Test				
            Test Ran on 3/2/2025, with start date 3/2/2024 to 3/2/2025				
	                    First Date Returned	    #Candles		
            1-Minute	1/14/2025 (46 days)	    35,902		
            5-Minute	6/17/2024 (258 days)    38,587		
            10-Minute	6/17/2024 (258 days)    19,998		
            15-Minute	6/17/2024 (258 days)    13,416		
            30-Minute	6/17/2024 (258 days)     6,729		
         */

        /// <summary>
        /// Get candles for a symbol
        /// Intraday price history only goes back about trading 200 days, or 260 calendar days.
        /// The end date has to be less than 260 (approximately) days back.
        /// Start date can be earlier, but you won't get the earlier days..
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="periodType"></param>
        /// <param name="period"></param>
        /// <param name="frequencyType"></param>
        /// <param name="frequency"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="needExtendedHoursData"></param>
        /// <param name="needPreviousClose">default is false</param>
        /// <returns>PriceHistory</returns>
        public PriceHistory GetPriceHistory(string symbol, PeriodType periodType, int period, FrequencyType frequencyType, int frequency,
                                            DateTime? startDate, DateTime? endDate, bool needExtendedHoursData, bool needPreviousClose = false)
        {
            return WaitForCompletion(GetPriceHistoryAsync(symbol,periodType, period, frequencyType, frequency, startDate, endDate, needExtendedHoursData, needPreviousClose));
        }

        /// <summary>
        /// Get candles for a symbol async
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="periodType"></param>
        /// <param name="period"></param>
        /// <param name="frequencyType"></param>
        /// <param name="frequency"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="needExtendedHoursData"></param>
        /// <param name="needPreviousClose">default is false</param>
        /// <returns>Task<PriceHistory></returns>
        public async Task<ApiResponseWrapper<PriceHistory>> GetPriceHistoryAsync(string symbol, PeriodType periodType, int period, FrequencyType frequencyType,
                                            int frequency, DateTime? startDate, DateTime? endDate, bool needExtendedHoursData, bool needPreviousClose = false)
        {
            const string fmt = "/pricehistory?symbol={0}&periodType={1}&period={2}&frequencyType={3}&frequency={4}&needExtendedHoursData={5}&needPreviousClose={6}";
            
            var url = string.Format(fmt, symbol.ToUpper(), periodType, period, frequencyType, frequency, needExtendedHoursData.ToString().ToLower(), needPreviousClose.ToString().ToLower());
            if (startDate != null)
                url += "&startDate=" + SchwabApi.DateTime_to_ApiDateTime((DateTime)startDate).ToString();
            if (endDate != null)
                url += "&endDate=" + SchwabApi.DateTime_to_ApiDateTime((DateTime)endDate).ToString();

            var result = await Get<PriceHistory>(MarketDataBaseUrl + url);
            if (!result.HasError && result.Data.candles.Count>0)
            {
                switch (frequencyType)
                {
                    case FrequencyType.daily: // check for last candle with 23 hour
                        var c = result.Data.candles.LastOrDefault();
                        if (c.dateTime.Hour == 23)
                            c.dateTime = c.dateTime.AddHours(1);
                        foreach (var cc in result.Data.candles) // fix for day candles with a time component
                        {
                            if (cc.dateTime.Hour == 1)
                                cc.dateTime = cc.dateTime.AddHours(-1);
                        }
                        break;

                    case FrequencyType.minute: // check for duplicates, sometimes on current day (at night? 1am)
                        // on prior days it returns more than requested, filter those out.
                        if (result.Data.candles.Count > 1)
                        {
                            var candles = result.Data.candles
                                                .Where(r=> (startDate == null || r.dateTime >= startDate) && (endDate == null || r.dateTime <= endDate)) 
                                                .OrderBy(r => r.dateTime).ToList();
                            for (var x = candles.Count-1; x > 0; x--)
                            {
                                if (candles[x-1].dateTime == candles[x].dateTime)
                                    candles.RemoveAt(x);
                            }
                            result.Data.candles = candles;
                        }
                        break;
                }
            }
            return result;
        }

        public enum PeriodType { day, month, year, ytd }
        public enum FrequencyType { minute, daily, weekly, monthly }


        public class Candle
        {
            public double open { get; set; }
            public double high { get; set; }
            public double low { get; set; }
            public double close { get; set; }
            public long volume { get; set; }
            public long datetime { get; set; }  // Schwab API time 

            private DateTime? _dateTime = null;
            public DateTime dateTime  // local time
            {
                get { return ConvertDateOnce(datetime, ref _dateTime); }
                set { _dateTime = value; } // needed to remove time zone diff for daily
            }

            public override string ToString()
            {
                return string.Format("{0}  O:{1}  H:{2}  L:{3}  C:{4}  V:{5}", dateTime, open, high, low, close, volume);
            }
        }

        public class PriceHistory
        {
            public List<Candle> candles { get; set; }
            public string symbol { get; set; }
            public bool empty { get; set; }

            public double? previousClose { get; set; }
            public long? previousCloseDate { get; set; }


            private DateTime? _previousCloseDate = null;
            public DateTime? PreviousCloseDate  // local time
            {
                get { return ConvertDateOnce(previousCloseDate, ref _previousCloseDate); }
                set { _previousCloseDate = value; }
            }

            /// <summary>
            /// Create an hour candle set from current candles. "this.candles" must be a 1,5,10,15,30 minute candle set
            /// </summary>
            /// <returns></returns>
            public List<Candle> HourCandles()
            {
                var hourCandles = candles.GroupBy(r => r.dateTime.AddMinutes(-r.dateTime.Minute)) // remove minutes from the time component.
                                .Select(r => new Candle()
                                {
                                    datetime = SchwabApi.DateTime_to_ApiDateTime(r.Key),
                                    volume = r.Sum(r => r.volume),
                                    high = r.Max(r => r.high),
                                    low = r.Min(r => r.low),
                                    open = r.First<Candle>().open,
                                    close = r.Last<Candle>().close,
                                }).ToList();
                return hourCandles;
            }

            /// <summary>
            /// Create a weekly candle set from current day candles. 
            /// </summary>
            /// <returns></returns>
            public List<Candle> WeeklyCandles()
            {
                var weeklyCcandles = candles.GroupBy(r => r.dateTime.AddDays(1 - (int)r.dateTime.DayOfWeek) ) // change date to Monday date of week
                                .Select(r => new Candle()
                                {
                                    datetime = SchwabApi.DateTime_to_ApiDateTime(r.Key),
                                    volume = r.Sum(r => r.volume),
                                    high = r.Max(r => r.high),
                                    low = r.Min(r => r.low),
                                    open = r.First<Candle>().open,
                                    close = r.Last<Candle>().close,
                                }).ToList();
                return weeklyCcandles;
            }
        }


        // ====== Market Hours ===============================================================================

        public enum Markets { equity, option, bond, future, forex };

        /// <summary>
        /// Get Market hours for a date.
        /// </summary>
        /// <param name="date">if null, then for today.</param>
        /// <returns>MarketHours</returns>
        public MarketHours GetMarketHours(DateTime? date = null)
        {
            return WaitForCompletion(GetMarketHoursAsync(date));
        }

        /// <summary>
        /// Get Market hours for a date - async.
        /// </summary>
        /// <param name="date">if null, then for today.</param>
        /// <returns>Task<MarketHours></returns>
        public async Task<ApiResponseWrapper<MarketHours>> GetMarketHoursAsync(DateTime? date = null)
        {
            var url = MarketDataBaseUrl + "/markets?markets=equity,option,bond,future,forex"   // + Markets.equity.ToString()
                        + (date == null ? "" : "&date=" + ((DateTime)date).ToString("yyyy-MM-dd"));
            var result = await Get<string>(url);
            if (result.HasError)
            {
                return new ApiResponseWrapper<MarketHours>(default, result.HasError, result.ResponseCode, result.ResponseText);
            }

            var j = JObject.Parse(result.Data);

            var marketHours = new MarketHours();

            // found this issue on 2024-06-07. seems non-market days have a different schema.
            // EQ changes to equity, and BON changes to bond.  I reported it as a bug.  Maybe this can be removed in the future
            if (j["equity"]["EQ"] != null)
                marketHours.equity = Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(j["equity"]["EQ"].ToString());
            else
                marketHours.equity = Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(j["equity"]["equity"].ToString());

            if (j["bond"]["BON"] != null)
                marketHours.bond = Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(j["bond"]["BON"].ToString());
            else
                marketHours.bond = Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(j["bond"]["bond"].ToString());

            marketHours.forex = Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(j["forex"]["forex"].ToString());
            foreach (var m in j["option"])
                marketHours.options.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(m.First.ToString()));
            foreach (var m in j["future"])
                marketHours.futures.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<MarketHours.Market>(m.First.ToString()));

            return new ApiResponseWrapper<MarketHours>(marketHours, result.HasError, result.ResponseCode, result.ResponseText);
        }

        public class MarketHours
        {
            public Market equity { get; set; }
            public Market forex { get; set; }
            public Market bond { get; set; }
            public List<Market> options { get; set; } = new List<Market>();
            public List<Market> futures { get; set; } = new List<Market>();

            public class Market
            {
                public string date { get; set; }
                public string marketType { get; set; }
                public string exchange { get; set; }
                public string category { get; set; }
                public string product { get; set; }
                public string productName { get; set; }
                public bool isOpen { get; set; }
                public SessionHours sessionHours { get; set; }
            }

            public class Hours
            {
                public DateTime start { get; set; }
                public DateTime end { get; set; }
            }

            public class SessionHours
            {
                public List<Hours>? regularMarket { get; set; }
                public List<Hours>? preMarket { get; set; }
                public List<Hours>? outcryMarket { get; set; }
                public List<Hours>? postMarket { get; set; }
            }
        }

        // ============ MOVERS ============================================================================

        public enum Indexes { DJI, COMPX, SPX, NYSE, NASDAQ, OTCBB, INDEX_ALL, EQUITY_ALL, OPTION_ALL, OPTION_PUT, OPTION_CALL };
        public enum MoversSort { VOLUME, TRADES, PERCENT_CHANGE_UP, PERCENT_CHANGE_DOWN };
        public enum MoversFrequency { F0, F1, F5, F10, F30, F60 };
        private static string ToString(Indexes index)
        {
            switch(index)
            {
                // DJI$, $COMPX, $SPX
                case Indexes.DJI:
                case Indexes.COMPX:
                case Indexes.SPX:
                    return "$" + index.ToString();
            }
            return index.ToString();
        }

        /// <summary>
        /// Market Movers
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sort"></param>
        /// <param name="frequency"></param>
        /// <returns>List<Mover></returns>
        public List<MoverResult.Mover> GetMovers(Indexes index, MoversSort sort, MoversFrequency frequency = MoversFrequency.F0)
        {
            return WaitForCompletion(GetMoversAsync(index, sort, frequency));
        }

        /// <summary>
        /// Market Movers async
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sort"></param>
        /// <param name="frequency"></param>
        /// <returns>Task<List<Mover>></returns>
        public async Task<ApiResponseWrapper<List<MoverResult.Mover>>> GetMoversAsync(Indexes index, MoversSort sort, MoversFrequency frequency = MoversFrequency.F0)
        {
            var url = string.Format("{0}/movers/{1}?sort={2}&frequency={3}", MarketDataBaseUrl,
                                    ToString(index), sort.ToString(), frequency.ToString().Substring(1));
            var result = await Get<MoverResult>(url);
            if (result.HasError)
                return new ApiResponseWrapper<List<MoverResult.Mover>>(default, true, result.ResponseCode, result.ResponseText);

            return new ApiResponseWrapper<List<MoverResult.Mover>>(result.Data.screeners, false, result.ResponseCode, result.ResponseText);

        }

        public class MoverResult
        {
            public List<Mover> screeners { get; set; }

            public class Mover
            {
                public override string ToString()
                {
                    return string.Format("{0} - {1},  V: {2},  Last: {3},  NetChg: {4}", 
                                         symbol, description, volume, lastPrice.ToString("C2"), netChange.ToString("C2"));
                }

                public string description { get; set; }
                public long volume { get; set; }
                public decimal lastPrice { get; set; }
                public decimal netChange { get; set; }
                public decimal marketShare { get; set; } // MarketShare is the percentage that the company is making up of the index.
                public long totalVolume { get; set; }
                public int trades { get; set; }
                public decimal netPercentChange { get; set; }
                public string symbol { get; set; }
            }
        }
    }
}
