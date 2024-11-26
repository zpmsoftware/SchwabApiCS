// <copyright file="OrderBase.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;

// https://json2csharp.com/

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        private const string OrdersBaseUrl = "https://api.schwabapi.com/trader/v1";

        // =========== ORDER EXECUTE ========================================================================
        // all buy & sell orders go through OrderExecuteNew, OrderExecuteReplace or OrderExecuteDelete.

        /// <summary>
        /// Json string for last executed order.
        /// intended for debugging and verification
        /// </summary>
        public static string LastOrderJson;


        // =========== OrderExecuteNew for new orders ========================================================================
        // use this method to create your own complex orders.
        // See other order methods that use OrderExecuteNew() for examples on how to built more complex or special orders.

        /// <summary>
        /// Place an order. roll your own order and send it here.
        /// All other order methods use this to send the order.
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="order">Create the order object and populate it.</param>
        public long? OrderExecuteNew(string accountNumber, Order order)
        {
            return WaitForCompletion(OrderExecuteNewAsync(accountNumber, order));
        }

        /// <summary>
        /// Place an order - async. roll your own order and send it here.
        /// All other order methods use this to send the order.
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="order">Create the order object and populate it.</param>
        /// <returns>SchwabOrderID or null</returns>
        public async Task<ApiResponseWrapper<long?>> OrderExecuteNewAsync(string accountNumber, Order order)
        {
            if (order.orderLegCollection != null)
            {
                foreach (var o in order.orderLegCollection)
                {
                    if ((o.instruction == "BUY" || o.instruction == "SELL") && o.instrument.assetType == "OPTION")
                        throw new SchwabApiException("OrderExecuteNewAsync error: Instruction " + o.instruction + " invalid with Options.");
                }
            }

            order.price = PriceAdjust(order.price); // Schwab rule: orders above $1 can only have 2 decimals.
            order.stopPrice = PriceAdjust(order.stopPrice);

            var jsonOrder = order.JsonSerialize();
            LastOrderJson = jsonOrder;

            var result = await Post<long?>(OrdersBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + "/orders", jsonOrder);
            if (result.ResponseMessage != null && result.ResponseMessage.IsSuccessStatusCode)
            {
                var pathParts = result.ResponseMessage.Headers.Location.LocalPath.Split('/');
                result.Data = Convert.ToInt64(pathParts[pathParts.Length-1]); // get last part, should be SchwabOrderID
            } 
            else
            {
                result.HasError = true;
            }
            return result;  
        }

        /// <summary>
        /// Schwab rule: orders above $1 can only have 2 decimals.
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        internal static decimal? PriceAdjust(decimal? price)
        {
            if (price != null && (decimal)price > 1.0M)
                price = Math.Round((decimal)price, 2);
            return price;
        }


        // ================ OrderExecuteReplace - Replace Order =================================================

        /// <summary>
        /// Replace an Order
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public long? OrderExecuteReplace(string accountNumber, Order order)
        {
            return WaitForCompletion(OrderExecuteReplaceAsync(accountNumber, order));
        }

        /// <summary>
        /// Replace Order async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<long?>> OrderExecuteReplaceAsync(string accountNumber, Order order)
        {
            var jsonOrder = Newtonsoft.Json.JsonConvert.SerializeObject(order);
            LastOrderJson = jsonOrder;
            var result = await Put<long?>(OrdersBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + "/orders/" + order.orderId, jsonOrder);
            if (!result.HasError && result.ResponseMessage != null)
            {
                var pathParts = result.ResponseMessage.Headers.Location.LocalPath.Split('/');
                result.Data = Convert.ToInt64(pathParts[pathParts.Length - 1]); // get last part, should be orderId
            }
            return result;
        }


        // ================ OrderExecuteDelete - Delete an order =================================================

        /// <summary>
        /// Delete Order
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public bool OrderExecuteDelete(string accountNumber, Order order)
        {
            return WaitForCompletion(OrderExecuteDeleteAsync(accountNumber, order));
        }

        /// <summary>
        /// Delete Order async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<bool>> OrderExecuteDeleteAsync(string accountNumber, Order order)
        {
            return await Delete(OrdersBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + "/orders/" + order.orderId);
        }

        public bool OrderExecuteDelete(string accountNumber, long orderId)
        {
            return WaitForCompletion(OrderExecuteDeleteAsync(accountNumber, orderId));
        }

        /// <summary>
        /// Delete Order async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<bool>> OrderExecuteDeleteAsync(string accountNumber, long orderId)
        {
            return await Delete(OrdersBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + "/orders/" + orderId.ToString());
        }

    }
}
