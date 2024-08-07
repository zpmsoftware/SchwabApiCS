﻿// <copyright file="GetOrder.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;

// https://json2csharp.com/

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        // ========================= Get Orders ================================

        /// <summary>
        /// Get orders for all accounts
        /// </summary>
        /// <param name="apiClient"></param>
        /// <returns></returns>
        public IList<Order> GetOrders(DateTime fromDate, DateTime toDate)
        {
            return WaitForCompletion(GetOrdersAsync(fromDate, toDate));
        }

        /// <summary>
        /// Get orders for all accounts async
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<IList<Order>>> GetOrdersAsync(DateTime fromDate, DateTime toDate)
        {
            const string utcDateFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
            string fDate = fromDate.ToUniversalTime().ToString(utcDateFormat);
            string tDate = toDate.ToUniversalTime().ToString(utcDateFormat);

            return await Get<IList<Order>>(OrdersBaseUrl + "/orders?fromEnteredTime=" + fDate + "&toEnteredTime=" + tDate);
        }


        /// <summary>
        /// Get Orders for a specific account
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="status">null or single status</param>
        /// <returns></returns>
        public IList<Order> GetOrders(string accountNumber, DateTime fromDate, DateTime toDate, Order.Status? status = null)
        {
            return WaitForCompletion(GetOrdersAsync(accountNumber, fromDate, toDate, status));
        }

        /// <summary>
        /// Get Orders for a specific account async
        /// If orders outside of a date range are still open within the specified date range, they are included
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="fromDate">required</param>
        /// <param name="toDate">required</param>
        /// <param name="status">optional or single status</param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<IList<Order>>> GetOrdersAsync(string accountNumber, DateTime fromDate, DateTime toDate, Order.Status? status = null)
        {
            string fDate = fromDate.ToUniversalTime().ToString(utcDateFormat);
            string tDate = toDate.ToUniversalTime().ToString(utcDateFormat);
            string parms = $"fromEnteredTime={fDate}&toEnteredTime={tDate}";

            if (status != null)
                parms += "&status=" + status.ToString();
            var t = await Get<IList<Order>>(OrdersBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + "/orders?" + parms);
            return t;
        }


        /// <summary>
        /// Get Order a specific order
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public Order GetOrder(string accountNumber, long orderId)
        {
            return WaitForCompletion(GetOrderAsync(accountNumber, orderId));
        }

        /// <summary>
        /// Get Order a specific order async
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<Order>> GetOrderAsync(string accountNumber, long orderId)
        {
            return await Get<Order>(OrdersBaseUrl + "/accounts/" + GetAccountNumberHash(accountNumber) + "/orders/" + orderId.ToString());
        }
    }
}
