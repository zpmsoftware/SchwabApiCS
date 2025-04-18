// <copyright file="Instruments.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        // Enum defining search methods for instruments; used in API query projection parameter
        public enum SearchBy { symbol_search, symbol_regex, desc_search, desc_regex, search, fundamental }

        /// <summary>
        /// Get instruments by symbols and projections
        /// </summary>
        /// <param name="symbols">Comma-separated list of symbols (e.g., "AAPL,MSFT").</param>
        /// <param name="searchBy">Defines search method; 'fundamental' includes extra fundamental data.</param>
        /// <returns>List of Instrument objects or null if the request fails.</returns>
        public List<Instrument> GetInstrumentsBySymbol(string symbols, SearchBy searchBy)
        {
            return WaitForCompletion(GetInstrumentsBySymbolAsync(symbols, searchBy));
        }

        /// <summary>
        /// Asynchronously get instruments by symbols and projections.
        /// </summary>
        /// <param name="symbols">Comma-separated list of symbols (e.g., "AAPL,MSFT").</param>
        /// <param name="searchBy">Defines search method; 'fundamental' includes extra fundamental data.</param>
        /// <returns>Task with ApiResponseWrapper containing a list of Instrument objects.</returns>
        public async Task<ApiResponseWrapper<List<Instrument>>> GetInstrumentsBySymbolAsync(string symbols, SearchBy searchBy)
        {
            if (string.IsNullOrEmpty(symbols))
                throw new ArgumentException("Symbols cannot be null or empty.", nameof(symbols));

            var url = string.Format("{0}/instruments?symbol={1}&projection={2}", MarketDataBaseUrl,
                                    Uri.EscapeDataString(symbols), searchBy.ToString().Replace("_","-"));

            var result = await Get<Instruments>(url);

            return new ApiResponseWrapper<List<Instrument>>(result.Data.instruments, false, result.ResponseCode, result.ResponseText);
        }

        /// <summary>
        /// Get a single instrument by its CUSIP ID.
        /// </summary>
        /// <param name="cusipId">The CUSIP identifier for the instrument.</param>
        /// <returns>The Instrument object or null if not found.</returns>
        public Instrument GetInstrumentByCusipId(string cusipId)
        {
            return WaitForCompletion(GetInstrumentByCusipIdAsync(cusipId));
        }

        /// <summary>
        /// Asynchronously get a single instrument by its CUSIP ID.
        /// </summary>
        /// <param name="cusipId">The CUSIP identifier for the instrument.</param>
        /// <returns>Task with ApiResponseWrapper containing the Instrument object.</returns>
        public async Task<ApiResponseWrapper<Instrument>> GetInstrumentByCusipIdAsync(string cusipId)
        {
            if (string.IsNullOrEmpty(cusipId))
                throw new ArgumentException("Symbols cannot be null or empty.", nameof(cusipId));

            var url = string.Format("{0}/instruments/{1}", MarketDataBaseUrl, cusipId);
            var result = await Get<Instruments>(url);

            if (result.HasError || result.Data?.instruments?.Count == 0)
                return new ApiResponseWrapper<Instrument>(default, true, result.ResponseCode, result.ResponseText);

            // Assumes the first instrument in the list is the target; API returns a single-item list
            return new ApiResponseWrapper<Instrument>(result.Data.instruments[0], false, result.ResponseCode, result.ResponseText);
        }

        // Wrapper class for API response containing a list of instruments
        public class Instruments
        {
            public List<Instrument> instruments { get; set; }
        }
        public class Instrument
        {
            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}", symbol, description, assetType);
            }

            public string cusip { get; set; }
            public string symbol { get; set; }
            public string description { get; set; }
            public string exchange { get; set; }
            public string assetType { get; set; }
            public Fundamental fundamental { get; set; }

            public class Fundamental
            {
                public string symbol { get; set; }
                public decimal high52 { get; set; }
                public decimal low52 { get; set; }
                public decimal dividendAmount { get; set; }
                public decimal dividendYield { get; set; }
                public string dividendDate { get; set; }
                public decimal peRatio { get; set; }
                public decimal pegRatio { get; set; }
                public decimal pbRatio { get; set; }
                public decimal prRatio { get; set; }
                public decimal pcfRatio { get; set; }
                public decimal grossMarginTTM { get; set; }
                public decimal grossMarginMRQ { get; set; }
                public decimal netProfitMarginTTM { get; set; }
                public decimal netProfitMarginMRQ { get; set; }
                public decimal operatingMarginTTM { get; set; }
                public decimal operatingMarginMRQ { get; set; }
                public decimal returnOnEquity { get; set; }
                public decimal returnOnAssets { get; set; }
                public decimal returnOnInvestment { get; set; }
                public decimal quickRatio { get; set; }
                public decimal currentRatio { get; set; }
                public decimal interestCoverage { get; set; }
                public decimal totalDebtToCapital { get; set; }
                public decimal ltDebtToEquity { get; set; }
                public decimal totalDebtToEquity { get; set; }
                public decimal epsTTM { get; set; }
                public decimal epsChangePercentTTM { get; set; }
                public decimal epsChangeYear { get; set; }
                public decimal epsChange { get; set; }
                public decimal revChangeYear { get; set; }
                public decimal revChangeTTM { get; set; }
                public decimal revChangeIn { get; set; }
                public decimal sharesOutstanding { get; set; }
                public decimal marketCapFloat { get; set; }
                public decimal marketCap { get; set; }
                public decimal bookValuePerShare { get; set; }
                public decimal shortIntToFloat { get; set; }
                public decimal shortIntDayToCover { get; set; }
                public decimal divGrowthRate3Year { get; set; }
                public decimal dividendPayAmount { get; set; }
                public string dividendPayDate { get; set; }
                public decimal beta { get; set; }
                public decimal vol1DayAvg { get; set; }
                public decimal vol10DayAvg { get; set; }
                public decimal vol3MonthAvg { get; set; }
                public long avg10DaysVolume { get; set; }
                public long avg1DayVolume { get; set; }
                public long avg3MonthVolume { get; set; }
                public string declarationDate { get; set; }
                public int dividendFreq { get; set; }
                public decimal eps { get; set; }
                public long dtnVolume { get; set; }
                public string nextDividendPayDate { get; set; }
                public string nextDividendDate { get; set; }
                public decimal fundLeverageFactor { get; set; }

                private DateTime? _dividendDate = null;
                public DateTime? DividendDate { get { return GetDate(dividendDate, ref _dividendDate); } }


                private DateTime? _dividendPayDate = null;
                public DateTime? DividendPayDate { get { return GetDate(dividendPayDate, ref _dividendPayDate); } }


                private DateTime? _nextDividendPayDate = null;
                public DateTime? NextDividendPayDate { get { return GetDate(nextDividendPayDate, ref _nextDividendPayDate); } }


                private DateTime? _nextDividendDate = null;
                public DateTime? NextDividendDate { get { return GetDate(nextDividendDate, ref _nextDividendDate); } }


                private DateTime? _declarationDate = null;
                public DateTime? DeclarationDate { get { return GetDate(declarationDate, ref _declarationDate); } }

            }
        }
    }
}
