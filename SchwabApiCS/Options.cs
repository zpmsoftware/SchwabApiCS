// <copyright file="Options.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json.Linq;

namespace SchwabApiCS
{
    partial class SchwabApi
    {
        public enum ContractType { CALL, PUT, ALL };
        public enum OptionStrategy { SINGLE, ANALYTICAL, COVERED, VERTICAL, CALENDAR, STRANGLE, STRADDLE, BUTTERFLY, CONDOR, DIAGONAL, COLLAR, ROLL };
        public enum OptionRange { ITM, NTM, OTM };
        public enum OptionExpMonth { JAN, FEB, MAR, APR, MAY, JUN, JUL, AUG, SEP, OCT, NOV, DEC, ALL };
        public enum OptionEntitlement { PN, NP, PP };


        /// <summary>
        /// Get option chain for a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="type"></param>
        /// <param name="strikeCount"></param>
        /// <returns>OptionChain</returns>
        public OptionChain GetOptionChain(string symbol, ContractType type, int? strikeCount=null)
        {
            return WaitForCompletion(GetOptionChainAsync(symbol, type, strikeCount));
        }

        public async Task<ApiResponseWrapper<OptionChain>> GetOptionChainAsync(string symbol, ContractType type, int? strikeCount = null)
        {
            var p = new OptionChainParameters() { contractType = type, strikeCount=strikeCount };
            return await GetOptionChainAsync(symbol, p);
        }

        public OptionChain GetOptionChain(string symbol, OptionChainParameters p)
        {
            return WaitForCompletion(GetOptionChainAsync(symbol, p));
        }

        /// <summary>
        /// Get option chain for a symbol async
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="type"></param>
        /// <param name="strikeCount"></param>
        /// <returns>Task<ApiResponseWrapper<OptionChain>></returns>
        public async Task<ApiResponseWrapper<OptionChain>> GetOptionChainAsync(string symbol, OptionChainParameters p)
        {
            var url = MarketDataBaseUrl + "/chains?symbol=" + symbol;

            // optional parameters:
            if (p.contractType != null)
                url += "&contractType=" + p.contractType.ToString();
            if (p.strikeCount != null)
                url += "&strikeCount=" + p.strikeCount.ToString();
            if (p.includeUnderlyingQuote != null)
                url += "&includeUnderlyingQuote=" + p.includeUnderlyingQuote.ToString().ToLower();
            if (p.interval != null)
                url += "&interval=" + p.interval.ToString();
            if (p.strike != null)
                url += "&strike=" + p.strike.ToString();
            if (p.fromDate != null)
                url += "&fromDate=" + ((DateTime)p.fromDate).ToString("yyyy-MM-dd");
            if (p.toDate != null)
                url += "&toDate=" + ((DateTime)p.toDate).ToString("yyyy-MM-dd");
            if (p.volatility != null)
                url += "&volatility=" + p.volatility.ToString();
            if (p.underlyingPrice != null)
                url += "&underlyingPrice=" + p.underlyingPrice.ToString();
            if (p.interestRate != null)
                url += "&interestRate=" + p.interestRate.ToString();
            if (p.daysToExpiration != null)
                url += "&daysToExpiration=" + p.daysToExpiration.ToString();
            if (p.optionType != null)
                url += "&optionType=" + p.optionType.ToString();
            if (p.strategy != null)
                url += "&strategy=" + p.strategy.ToString();
            if (p.range != null)
                url += "&range=" + p.range.ToString();
            if (p.expMonth != null)
                url += "&expMonth=" + p.expMonth.ToString();
            if (p.entitlement != null)
                url += "&entitlement=" + p.entitlement.ToString();

            var result =  await Get<string>(url);
            if (result.HasError)
                return new ApiResponseWrapper<OptionChain>(default, result.HasError, result.ResponseCode, result.ResponseText);

            try
            {
                OptionChain? oc;
                OptionChain.Option? option;
                var j = JObject.Parse(result.Data);
                var jc = j["callExpDateMap"];
                var jp = j["putExpDateMap"];

                j.Remove("callExpDateMap");
                j.Remove("putExpDateMap");
                try
                {
                    oc = Newtonsoft.Json.JsonConvert.DeserializeObject<OptionChain>(j.ToString(), jsonSettings);
                }
                catch (Exception ex)
                {
                    oc = Newtonsoft.Json.JsonConvert.DeserializeObject<OptionChain>(j.ToString());
                }

                foreach (var eDate in jc)
                { // expiration date
                    foreach (var strikes in eDate) // strike
                    {
                        foreach (var strike in strikes)
                        {
                            try
                            {
                                option = Newtonsoft.Json.JsonConvert.DeserializeObject<OptionChain.Option>(strike.First.First.ToString(), jsonSettings);
                            }
                            catch (Exception ex)
                            {
                                option = Newtonsoft.Json.JsonConvert.DeserializeObject<OptionChain.Option>(strike.First.First.ToString());
                            }
                            oc.calls.Add(option);
                        }
                    }
                }

                foreach (var eDate in jp)
                { // expiration date
                    foreach (var strikes in eDate) // strike
                    {
                        foreach (var strike in strikes)
                        {
                            try
                            {
                                option = Newtonsoft.Json.JsonConvert.DeserializeObject<OptionChain.Option>(strike.First.First.ToString(), jsonSettings);
                            }
                            catch (Exception ex)
                            {
                                option = Newtonsoft.Json.JsonConvert.DeserializeObject<OptionChain.Option>(strike.First.First.ToString());
                            }
                            oc.puts.Add(option);
                        }
                    }
                }
                return new ApiResponseWrapper<OptionChain>(oc, result.HasError, result.ResponseCode, result.ResponseText);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public class OptionChainParameters
        {
            /// <summary>
            /// Contract Type: CALL, PUT, ALL
            /// </summary>
            public ContractType? contractType { get; set; }

            /// <summary>
            /// The Number of strikes to return above and below the at-the-money price
            /// strikeCount=4 will return 4 call options and 4 put options
            /// </summary>
            public int? strikeCount { get; set; }

            /// <summary>
            /// Underlying quotes to be included
            /// </summary>
            public bool? includeUnderlyingQuote { get; set; }

            /// <summary>
            /// Strike interval for spread strategy chains (see strategy param)
            /// </summary>
            public decimal? interval { get; set; }

            /// <summary>
            /// Strike Price
            /// </summary>
            public decimal? strike { get; set; }

            /// <summary>
            /// From expiration date (use for filtering)
            /// </summary>
            public DateTime? fromDate { get; set; }

            /// <summary>
            /// To expiration date (use for filtering)
            /// </summary>
            public DateTime? toDate { get; set; }

            /// <summary>
            /// Volatility to use in calculations. Applies only to ANALYTICAL strategy chains (see strategy param)
            /// </summary>
            public decimal? volatility { get; set; }

            /// <summary>
            /// Underlying price to use in calculations. Applies only to ANALYTICAL strategy chains (see strategy param)
            /// </summary>
            public decimal? underlyingPrice { get; set; }

            /// <summary>
            /// Interest rate to use in calculations. Applies only to ANALYTICAL strategy chains (see strategy param)
            /// </summary>
            public decimal? interestRate { get; set; }

            /// <summary>
            /// Days to expiration to use in calculations. Applies only to ANALYTICAL strategy chains (see strategy param)
            /// </summary>
            public int? daysToExpiration { get; set; }

            /// <summary>
            /// ?
            /// </summary>
            public string? optionType { get; set; }

            /// <summary>
            /// OptionChain strategy. Default is SINGLE. ANALYTICAL allows the use of volatility, 
            /// underlyingPrice, interestRate, and daysToExpiration params to calculate theoretical values.
            /// </summary>
            public OptionStrategy? strategy { get; set; }

            /// <summary>
            /// Range(ITM/NTM/OTM etc.)
            /// </summary>
            public OptionRange? range { get; set; }

            /// <summary>
            /// Expiration month: JAN, ... DEC, ALL
            /// </summary>
            public OptionExpMonth? expMonth { get; set; }

            /// <summary>
            /// Applicable only if its retail token, entitlement of client PP-PayingPro, NP-NonPro and PN-NonPayingPro
            /// </summary>
            public OptionEntitlement? entitlement { get; set; }
        }

        public class OptionChain
        {
            public string symbol { get; set; }
            public string status { get; set; }
            public Underlying underlying { get; set; }
            public string strategy { get; set; }
            public double interval { get; set; }
            public bool isDelayed { get; set; }
            public bool isIndex { get; set; }
            public double interestRate { get; set; }
            public double underlyingPrice { get; set; }
            public double volatility { get; set; }
            public double daysToExpiration { get; set; }
            public int numberOfContracts { get; set; }
            public string assetMainType { get; set; }
            public string assetSubType { get; set; }
            public bool isChainTruncated { get; set; }
            public List<Option> calls { get; set; } = new List<Option>();
            public List<Option> puts { get; set; } = new List<Option>();

            public class Underlying
            {
                public string symbol { get; set; }
                public string description { get; set; }
                public double change { get; set; }
                public double percentChange { get; set; }
                public double close { get; set; }
                public long quoteTime { get; set; }
                public long tradeTime { get; set; }
                public double bid { get; set; }
                public double ask { get; set; }
                public double last { get; set; }
                public double mark { get; set; }
                public double markChange { get; set; }
                public double markPercentChange { get; set; }
                public int bidSize { get; set; }
                public int askSize { get; set; }
                public double highPrice { get; set; }
                public double lowPrice { get; set; }
                public double openPrice { get; set; }
                public long totalVolume { get; set; }
                public string exchangeName { get; set; }
                public double fiftyTwoWeekHigh { get; set; }
                public double fiftyTwoWeekLow { get; set; }
                public bool delayed { get; set; }


                private DateTime? _quoteTime = null;
                public DateTime? QuoteTime { get { return SchwabApi.GetDate(quoteTime, ref _quoteTime); } }

                private DateTime? _tradeTime = null;
                public DateTime? TradeTime { get { return SchwabApi.GetDate(tradeTime, ref _tradeTime); } }
            }

            public class Option
            {
                public override string ToString()
                {
                    return description.ToString();
                }

                public string putCall { get; set; }
                public string symbol { get; set; }
                public string description { get; set; }
                public string exchangeName { get; set; }
                public double bid { get; set; }
                public double ask { get; set; }
                public double last { get; set; }
                public double mark { get; set; }
                public int bidSize { get; set; }
                public int askSize { get; set; }
                public string bidAskSize { get; set; }
                public int lastSize { get; set; }
                public double highPrice { get; set; }
                public double lowPrice { get; set; }
                public double openPrice { get; set; }
                public double closePrice { get; set; }
                public long totalVolume { get; set; }
                public long tradeTimeInLong { get; set; }
                public long quoteTimeInLong { get; set; }
                public double netChange { get; set; }
                public double volatility { get; set; }
                public double delta { get; set; }
                public double gamma { get; set; }
                public double theta { get; set; }
                public double vega { get; set; }
                public double rho { get; set; }
                public int openInterest { get; set; }
                public double timeValue { get; set; }
                public double theoreticalOptionValue { get; set; }
                public double theoreticalVolatility { get; set; }
                public List<OptionDeliverablesList> optionDeliverablesList { get; set; }
                public double strikePrice { get; set; }
                public DateTime expirationDate { get; set; }
                public int daysToExpiration { get; set; }
                public string expirationType { get; set; }
                public long lastTradingDay { get; set; }
                public double multiplier { get; set; }
                public string settlementType { get; set; }
                public string deliverableNote { get; set; }
                public double percentChange { get; set; }
                public double markChange { get; set; }
                public double markPercentChange { get; set; }
                public double intrinsicValue { get; set; }
                public double extrinsicValue { get; set; }
                public string optionRoot { get; set; }
                public string exerciseType { get; set; }
                public double high52Week { get; set; }
                public double low52Week { get; set; }
                public bool nonStandard { get; set; }
                public bool pennyPilot { get; set; }
                public bool inTheMoney { get; set; }
                public bool mini { get; set; }


                private DateTime? _lastTradingDay = null;
                public DateTime? LastTradingDay { get { return SchwabApi.GetDate(lastTradingDay, ref _lastTradingDay); } }

                private DateTime? _tradeTime = null;
                public DateTime? TradeTime { get { return SchwabApi.GetDate(tradeTimeInLong, ref _tradeTime); } }

                private DateTime? _quoteTime = null;
                public DateTime? QuoteTime { get { return SchwabApi.GetDate(quoteTimeInLong, ref _quoteTime); } }
            }

            public class OptionDeliverablesList
            {
                public string symbol { get; set; }
                public string assetType { get; set; }
                public double deliverableUnits { get; set; }
            }
        }



        // =======   ====================================================================================
        public List<Expiration> GetOptionExpirationChain(string symbol)
        {
            return WaitForCompletion(GetOptionExpirationChainAsync(symbol));
        }

        public async Task<ApiResponseWrapper<List<Expiration>>> GetOptionExpirationChainAsync(string symbol)
        {
            var url = string.Format("{0}/expirationchain?symbol={1}", MarketDataBaseUrl, symbol);

            var result = await Get<ExpirationList>(url);
            if (result.HasError)
                return new ApiResponseWrapper<List<Expiration>>(default, result.HasError, result.ResponseCode, result.ResponseText);
            return new ApiResponseWrapper<List<Expiration>>(result.Data.expirationList, result.HasError, result.ResponseCode, result.ResponseText); 
        }

        private class ExpirationList
        {
            public List<Expiration> expirationList { get; set; }
        }
        public class Expiration
        {
            public DateTime expirationDate { get; set; }
            public int daysToExpiration { get; set; }
            public string expirationType { get; set; }
            public string settlementType { get; set; }
            public string optionRoots { get; set; }
            public bool standard { get; set; }
        }
    }
}
