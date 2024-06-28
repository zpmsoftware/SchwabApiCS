// <copyright file="Transactions.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        private const string TransactionsBaseUrl = "https://api.schwabapi.com/trader/v1";

        public enum TransactionTypes
        {
            TRADE, RECEIVE_AND_DELIVER, DIVIDEND_OR_INTEREST, ACH_RECEIPT, ACH_DISBURSEMENT,
            CASH_RECEIPT, CASH_DISBURSEMENT, ELECTRONIC_FUND, WIRE_OUT, WIRE_IN, JOURNAL, 
            MEMORANDUM, MARGIN_CALL, MONEY_MARKET, SMA_ADJUSTMENT
        }

        /// <summary>
        /// Get a transaction by Id
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="transactionId">aka: activityId</param>
        /// <returns>Transaction</returns>
        public Transaction GetAccountTransaction(string accountNumber, long transactionId)
        {
            return WaitForCompletion(GetAccountTransactionAsync(accountNumber, transactionId));
        }

        /// <summary>
        /// Get a transaction by Id async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="transactionId">aka: activityId</param>
        /// <returns>Task<Transaction></returns>
        public async Task<ApiResponseWrapper<Transaction>> GetAccountTransactionAsync(string accountNumber, long transactionId)
        {
            var url = String.Format("{0}/accounts/{1}/transactions/{2}",
                                    TransactionsBaseUrl, GetAccountNumberHash(accountNumber), transactionId);
            var result = await Get<Transaction>(url);
            return result;
        }


        /// <summary>
        /// Get transactions by account
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="type"></param>
        /// <param name="symbol"></param>
        /// <returns>IList<Transaction></returns>
        public IList<Transaction> GetAccountTransactions(string accountNumber, DateTime fromDate,
                                    DateTime toDate, TransactionTypes type, string? symbol = null)
        {
            return WaitForCompletion(GetAccountTransactionsAsync(accountNumber, fromDate, toDate, type, symbol));
        }

        /// <summary>
        /// Get transactions by account async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="type"></param>
        /// <param name="symbol"></param>
        /// <returns>Task<IList<Transaction>></returns>
        public async Task<ApiResponseWrapper<IList<Transaction>>> GetAccountTransactionsAsync(string accountNumber,
             DateTime fromDate, DateTime toDate, TransactionTypes type, string? symbol = null)
        {
            var types = new TransactionTypes[1] { type };
            return await GetAccountTransactions(accountNumber, fromDate, toDate, types, symbol);
        }

        public async Task<ApiResponseWrapper<IList<Transaction>>> GetAccountTransactions(string accountNumber,
                     DateTime fromDate, DateTime toDate, TransactionTypes[] types, string? symbol = null)
        {
            string typesStr = types[0].ToString();
            for (int i = 1; i<types.Length; i++)
                typesStr += "," + types[i].ToString();

            var url = String.Format("{0}/accounts/{1}/transactions?startDate={2}&endDate={3}&types={4}{5}",
                                    TransactionsBaseUrl,
                                    GetAccountNumberHash(accountNumber),
                                    fromDate.ToUniversalTime().ToString(utcDateFormat),
                                    toDate.ToUniversalTime().ToString(utcDateFormat),
                                    typesStr,
                                    (symbol == null ? "" : "&symbol=" + (string)symbol));
            var result = await Get<IList<Transaction>>(url);

            if (!result.HasError)
            { // sort in date order
                var data = result.Data.OrderBy(r => r.time).ToList();
                return new ApiResponseWrapper<IList<Transaction>>(data, result.HasError, result.ResponseCode, result.ResponseText, result.ResponseMessage);
            }
            return result;

        }

        public class Transaction
        {
            public override string ToString()
            {
                var ti = transferItems.Where(r=>r.feeType == null).ToList(); // exclude fees
                var text = accountNumber + time.ToString("  yyyy-MM-dd HH:mmtt:").ToLower();
                foreach (var t in ti)
                {
                    text += "  " + t.instrument.symbol + "  Qty=" + t.amount.ToString() +
                             (t.price == null ? "" : ", Price=" + ((decimal)t.price).ToString("C2"));
                }
                return text;
            }

            public long activityId { get; set; }
            public DateTime time { get; set; }
            public string accountNumber { get; set; }
            public string type { get; set; }
            public string status { get; set; }
            public string subAccount { get; set; }
            public DateTime tradeDate { get; set; }
            public long positionId { get; set; }
            public long orderId { get; set; }
            public decimal netAmount { get; set; }
            public List<TransferItem> transferItems { get; set; }

            public class Deliverable
            {
                public string assetType { get; set; }
                public string status { get; set; }
                public string symbol { get; set; }
                public long instrumentId { get; set; }
                public decimal closingPrice { get; set; }
                public string type { get; set; }
            }

            public class Instrument
            {
                public string assetType { get; set; }
                public string status { get; set; }
                public string symbol { get; set; }
                public string description { get; set; }
                public long instrumentId { get; set; }
                public decimal closingPrice { get; set; }
                public string type { get; set; }
                public DateTime? expirationDate { get; set; }
                public List<OptionDeliverable> optionDeliverables { get; set; }
                public long? optionPremiumMultiplier { get; set; }
                public string putCall { get; set; }
                public decimal? strikePrice { get; set; }
                public string underlyingSymbol { get; set; }
                public string underlyingCusip { get; set; }
            }

            public class OptionDeliverable
            {
                public string rootSymbol { get; set; }
                public long strikePercent { get; set; }
                public long deliverableNumber { get; set; }
                public decimal deliverableUnits { get; set; }
                public Deliverable deliverable { get; set; }
            }

            public class TransferItem
            {
                public override string ToString()
                {
                    return string.Format("{0}  Qty={1}, Price={2}", 
                                         instrument.symbol,
                                         amount,
                                         price == null ? "n/a" : ((decimal)price).ToString("C2"));
                }

                public Instrument instrument { get; set; }
                public decimal amount { get; set; }
                public decimal cost { get; set; }
                public string feeType { get; set; }
                public decimal? price { get; set; }
                public string positionEffect { get; set; }
            }
        }
    }
}
