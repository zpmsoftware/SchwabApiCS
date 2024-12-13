// <copyright file="OrderStopLoss.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using static SchwabApiCS.Order;

// https://json2csharp.com/

namespace SchwabApiCS
{
    public partial class SchwabApi
    {

        // ================ OrderStopLoss - Stop Loss Order =================================================

        /// <summary>
        /// Stop Loss Order
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity">negative qty to close a long position, positive qty to close a short position</param>
        /// <param name="stopPrice"></param>
        /// <returns></returns>
        public long? OrderStopLoss(string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity, decimal stopPrice)
        {
            return WaitForCompletion(OrderStopLossAsync(accountNumber, symbol, assetType, duration, session, quantity, stopPrice));
        }

        /// <summary>
        /// Stop Loss Order async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity">negative qty to close a long position, positive qty to close a short position</param>
        /// <param name="stopPrice"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<long?>> OrderStopLossAsync(string accountNumber, string symbol,
                    Order.AssetType assetType, Order.Duration duration, Order.Session session, decimal quantity, decimal stopPrice)
        {
            if (stopPrice == 0)
                return new ApiResponseWrapper<long?> (default, true, 900, "stopPrice is required on STOP orders");

            var order = new Order(OrderType.STOP, Order.OrderStrategyTypes.SINGLE, session, duration, stopPrice);
            order.Add(new Order.OrderLeg(symbol, assetType, Position.TO_CLOSE, -quantity));

            return await OrderExecuteNewAsync(accountNumber, order);
        }


    }
}
