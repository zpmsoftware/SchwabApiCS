// <copyright file="Accounts.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        // Accounts implementation
        // https://json2csharp.com/
        private const string AccountsBaseUrl = "https://api.schwabapi.com/trader/v1";

        /*
         * 200=good, no errors
         * 400 error message
         * 401 authorization token is invalid or there are no accounts the caller is allowed to view or use for trading that are registered with the provided third party application
         * 403 An error message indicating the caller is forbidden from accessing this service
         * 404 An error message indicating the resource is not found
         * 500 An error message indicating there was an unexpected server error
         * 503 An error message indicating server has a temporary problem responding
          { "message": "string", "errors": [ "string" ] }
        */


        /* GET /accounts/accountNumbers   ====================================================
         * Account numbers in plain text cannot be used outside of headers or request/response bodies. As the first step consumers must invoke this service to retrieve the list of
         * plain text/encrypted value pairs, and use encrypted account values for all subsequent calls for any accountNumber request
         * 
        */

        /// <summary>
        /// List of account nubmers and hash values
        /// </summary>
        /// <param name="apiClient"></param>
        /// <returns></returns>
        public IList<AccountNumber> GetAccountNumbers()
        {
            return WaitForCompletion(GetAccountNumbersAsync());
        }
        
        /// <summary>
        /// List of account nubmers and hash values - async
        /// </summary>
        /// <param name="apiClient"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<IList<AccountNumber>>> GetAccountNumbersAsync()
        {
            return await Get<IList<AccountNumber>>(AccountsBaseUrl + "/accounts/accountNumbers");
        }

        public record AccountNumber(
            string accountNumber,
            string hashValue
         );

        /// <summary>
        /// Get account number hash, required for many api requests
        /// </summary>
        /// <param name="accountNumber">8 digit account number or a account number hash</param>
        /// <returns>account number hash, or blank if invalid account number. If accountNumber is already the hash, returns accountNumber unchanged.</returns>
        public string GetAccountNumberHash(string accountNumber)
        {
            if (accountNumber.Length > 8)
                return accountNumber; // already a hash

            var acct = accountNumberHashs.Where(r => r.accountNumber == accountNumber).SingleOrDefault();
            return acct == null ? "" : acct.hashValue;
        }

        /* ==================================================================
         * GET /accounts/accounts
         * All the linked account information for the user logged in. The balances on these accounts are displayed by default
         * however the positions on these accounts will be displayed based on the "positions" flag
         * 
         * fields=positions
         */

        /// <summary>
        /// Get info for list of accounts
        /// </summary>
        /// <param name="includePositions">true to include positions</param>
        /// <returns>AccountInfo</returns>
        public IList<AccountInfo> GetAccounts(bool includePositions)
        {
            return WaitForCompletion(GetAccountsAsync(includePositions));
        }

        /// <summary>
        /// Get info for list of accounts async
        /// </summary>
        /// <param name="includePositions">true to include positions</param>
        /// <returns>AccountInfo</returns>
        public async Task<ApiResponseWrapper<IList<AccountInfo>?>> GetAccountsAsync(bool includePositions)
        {
            var result = await Get<IList<AccountInfo>?>(AccountsBaseUrl + "/accounts" + (includePositions ? "?fields=positions" : ""));
            if (!result.HasError)
            {
                foreach (var a in result.Data) // accounts list
                {
                    UpdateAccount(a);
                    /*
                    a.securitiesAccount.accountNumberHash = GetAccountNumberHash(a.securitiesAccount.accountNumber);
                    a.securitiesAccount.accountPreferences = userPreferences.accounts.Where(r => r.accountNumber == a.securitiesAccount.accountNumber).Single();
                    */
                }
            }
            return result;
        }


        /// <summary>
        /// Get info one account
        /// </summary>
        /// <param name="apiClient"></param>
        /// <param name="includePositions">true to include positions</param>
        /// <param name="accountNumber">account number or account number hash</param>
        /// <returns>AccountInfo</returns>
        public AccountInfo GetAccount(string accountNumber, bool includePositions)
        {
            return WaitForCompletion(GetAccountAsync(accountNumber, includePositions));
        }

        /// <summary>
        /// Get info one account async
        /// </summary>
        /// <param name="apiClient"></param>
        /// <param name="includePositions">true to include positions</param>
        /// <param name="accountNumber">account number or account number hash</param>
        /// <returns>AccountInfo</returns>
        public async Task<ApiResponseWrapper<AccountInfo?>> GetAccountAsync(string accountNumber, bool includePositions)
        {
            var result = await Get<AccountInfo?>(AccountsBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + (includePositions ? "?fields=positions" : ""));
            if (!result.HasError)
                UpdateAccount(result.Data);
            return result;
        }

        private void UpdateAccount(AccountInfo a)
        {
            a.securitiesAccount.accountNumberHash = accountNumberHashs
                                                    .Where(r => r.accountNumber == a.securitiesAccount.accountNumber).Single().hashValue;
            a.securitiesAccount.accountPreferences = userPreferences.accounts
                                                    .Where(r => r.accountNumber == a.securitiesAccount.accountNumber).Single();
        }

        public class AccountInfo
        {
            public SecuritiesAccount securitiesAccount { get; set; }

            public override string ToString()
            {
                return securitiesAccount.accountNumber
                        + " " + securitiesAccount.accountPreferences.nickName
                       + "   $" + securitiesAccount.currentBalances.liquidationValue.ToString("N2");
            }
        }

        public class SecuritiesAccount
        {
            public string type { get; set; }
            public string accountNumber { get; set; }
            public long roundTrips { get; set; }
            public bool isDayTrader { get; set; }
            public bool isClosingOnlyRestricted { get; set; }
            public bool pfcbFlag { get; set; }
            public InitialBalances initialBalances { get; set; }
            public CurrentBalances currentBalances { get; set; }
            public ProjectedBalances projectedBalances { get; set; }
            public List<Position>? positions { get; set; }

            // userPreference and AccountNumberHash are not in the request response, but are added by the application
            public UserPreferences.Account accountPreferences { get; set; }
            public string accountNumberHash { get; set; }

            public override string ToString()
            {
                return accountNumber + " " + type + " $" + currentBalances.liquidationValue.ToString("N2");
            }

            public class ProjectedBalances
            {
                public decimal availableFunds { get; set; }
                public decimal availableFundsNonMarginableTrade { get; set; }
                public decimal buyingPower { get; set; }
                public decimal dayTradingBuyingPower { get; set; }
                public decimal dayTradingBuyingPowerCall { get; set; }
                public decimal maintenanceCall { get; set; }
                public decimal regTCall { get; set; }
                public bool isInCall { get; set; }
                public decimal stockBuyingPower { get; set; }
            }

            public class Position
            {
                public decimal shortQuantity { get; set; }
                public decimal averagePrice { get; set; }
                public decimal currentDayProfitLoss { get; set; }
                public decimal currentDayProfitLossPercentage { get; set; }
                public decimal longQuantity { get; set; }
                public decimal settledLongQuantity { get; set; }
                public decimal settledShortQuantity { get; set; }
                public decimal agedQuantity { get; set; }
                public Instrument instrument { get; set; }
                public decimal marketValue { get; set; }
                public decimal maintenanceRequirement { get; set; }
                public decimal averageLongPrice { get; set; }
                public decimal taxLotAverageLongPrice { get; set; }
                public decimal longOpenProfitLoss { get; set; }
                public decimal previousSessionLongQuantity { get; set; }
                public decimal currentDayCost { get; set; }
            }

            public class Instrument
            {
                public string assetType { get; set; }
                public string cusip { get; set; }
                public string symbol { get; set; }
                public string description { get; set; }
                public decimal netChange { get; set; }
                public string type { get; set; }
                public DateTime? maturityDate { get; set; }
                public decimal? variableRate { get; set; }
            }

            public class CurrentBalances
            {
                public decimal accruedInterest { get; set; }
                public decimal cashBalance { get; set; }
                public decimal cashReceipts { get; set; }
                public decimal longOptionMarketValue { get; set; }
                public decimal liquidationValue { get; set; }
                public decimal longMarketValue { get; set; }
                public decimal moneyMarketFund { get; set; }
                public decimal savings { get; set; }
                public decimal shortMarketValue { get; set; }
                public decimal pendingDeposits { get; set; }
                public decimal mutualFundValue { get; set; }
                public decimal bondValue { get; set; }
                public decimal shortOptionMarketValue { get; set; }
                public decimal availableFunds { get; set; }
                public decimal availableFundsNonMarginableTrade { get; set; }
                public decimal buyingPower { get; set; }
                public decimal buyingPowerNonMarginableTrade { get; set; }
                public decimal dayTradingBuyingPower { get; set; }
                public decimal equity { get; set; }
                public decimal equityPercentage { get; set; }
                public decimal longMarginValue { get; set; }
                public decimal maintenanceCall { get; set; }
                public decimal maintenanceRequirement { get; set; }
                public decimal marginBalance { get; set; }
                public decimal regTCall { get; set; }
                public decimal shortBalance { get; set; }
                public decimal shortMarginValue { get; set; }
                public decimal sma { get; set; }
            }

            public class InitialBalances
            {
                public decimal accruedInterest { get; set; }
                public decimal availableFundsNonMarginableTrade { get; set; }
                public decimal bondValue { get; set; }
                public decimal buyingPower { get; set; }
                public decimal cashBalance { get; set; }
                public decimal cashAvailableForTrading { get; set; }
                public decimal cashReceipts { get; set; }
                public decimal dayTradingBuyingPower { get; set; }
                public decimal dayTradingBuyingPowerCall { get; set; }
                public decimal dayTradingEquityCall { get; set; }
                public decimal equity { get; set; }
                public decimal equityPercentage { get; set; }
                public decimal liquidationValue { get; set; }
                public decimal longMarginValue { get; set; }
                public decimal longOptionMarketValue { get; set; }
                public decimal longStockValue { get; set; }
                public decimal maintenanceCall { get; set; }
                public decimal maintenanceRequirement { get; set; }
                public decimal margin { get; set; }
                public decimal marginEquity { get; set; }
                public decimal moneyMarketFund { get; set; }
                public decimal mutualFundValue { get; set; }
                public decimal regTCall { get; set; }
                public decimal shortMarginValue { get; set; }
                public decimal shortOptionMarketValue { get; set; }
                public decimal shortStockValue { get; set; }
                public decimal totalCash { get; set; }
                public bool isInCall { get; set; }
                public decimal pendingDeposits { get; set; }
                public decimal marginBalance { get; set; }
                public decimal shortBalance { get; set; }
                public decimal accountValue { get; set; }
            }
        }
    }
}
