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
            public AggregatedBalance aggregatedBalance { get; set; }

            public override string ToString()
            {
                return securitiesAccount.accountNumber
                        + " " + securitiesAccount.accountPreferences.nickName
                       + "   $" + securitiesAccount.currentBalances.liquidationValue.ToString("N2");
            }

            public class SecuritiesAccount
            {
                public override string ToString()
                {
                    return accountNumber + " " + type + " $" + currentBalances.liquidationValue.ToString("N2");
                }

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

                public record CurrentBalances(decimal accruedInterest, decimal cashBalance, decimal cashReceipts, decimal longOptionMarketValue,
                          decimal liquidationValue, decimal longMarketValue, decimal moneyMarketFund, decimal savings,
                          decimal shortMarketValue, decimal pendingDeposits, decimal mutualFundValue, decimal bondValue,
                          decimal shortOptionMarketValue, decimal availableFunds, decimal availableFundsNonMarginableTrade,
                          decimal buyingPower, decimal buyingPowerNonMarginableTrade, decimal dayTradingBuyingPower, decimal equity,
                          decimal equityPercentage, decimal longMarginValue, decimal maintenanceCall, decimal maintenanceRequirement,
                          decimal marginBalance, decimal regTCall, decimal shortBalance, decimal shortMarginValue, decimal sma,
                          decimal? cashAvailableForTrading, decimal? cashAvailableForWithdrawal, decimal? cashCall,
                          decimal? longNonMarginableMarketValue, decimal? totalCash, decimal? cashDebitCallValue, decimal? unsettledCash);
                public record InitialBalances(decimal accruedInterest, decimal availableFundsNonMarginableTrade, decimal bondValue,
                              decimal buyingPower, decimal cashBalance, decimal cashAvailableForTrading, decimal cashReceipts,
                              decimal dayTradingBuyingPower, decimal dayTradingBuyingPowerCall, decimal dayTradingEquityCall, decimal equity,
                              decimal equityPercentage, decimal liquidationValue, decimal longMarginValue, decimal longOptionMarketValue,
                              decimal longStockValue, decimal maintenanceCall, decimal maintenanceRequirement, decimal margin,
                              decimal marginEquity, decimal moneyMarketFund, decimal mutualFundValue, decimal regTCall, decimal shortMarginValue,
                              decimal shortOptionMarketValue, decimal shortStockValue, decimal totalCash, bool isInCall, decimal pendingDeposits,
                              decimal marginBalance, decimal shortBalance, decimal accountValue, decimal? cashAvailableForWithdrawal,
                              decimal? unsettledCash, decimal? cashDebitCallValue);
                public record Instrument(string assetType, string cusip, string symbol, string description, decimal netChange, string type,
                              DateTime? maturityDate, decimal? variableRate, string? putCall, string? underlyingSymbol);
                public record Position(decimal shortQuantity, decimal averagePrice, decimal currentDayProfitLoss,
                              decimal currentDayProfitLossPercentage, decimal longQuantity, decimal settledLongQuantity,
                              decimal settledShortQuantity, decimal agedQuantity, Instrument instrument, decimal marketValue,
                              decimal maintenanceRequirement, decimal averageLongPrice, decimal taxLotAverageLongPrice,
                              decimal longOpenProfitLoss, decimal previousSessionLongQuantity, decimal currentDayCost,
                              decimal averageShortPrice, decimal taxLotAverageShortPrice, decimal shortOpenProfitLoss,
                              decimal previousSessionShortQuantity)
                {
                    public override string ToString()
                    {
                        return instrument.symbol + ", Qty=" + (longQuantity != 0 ? longQuantity : -shortQuantity).ToString();
                    }
                }

                public record ProjectedBalances(decimal availableFunds, decimal availableFundsNonMarginableTrade, decimal buyingPower,
                              decimal dayTradingBuyingPower, decimal dayTradingBuyingPowerCall, decimal maintenanceCall, decimal regTCall,
                              bool isInCall, decimal stockBuyingPower, decimal? cashAvailableForTrading, decimal? cashAvailableForWithdrawal);
            }
            public record AggregatedBalance(decimal currentLiquidationValue, decimal liquidationValue);

        }
    }
}
