// <copyright file="OrderLimit.cs" company="ZPM Software Inc">
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
        /// <summary>
        /// OrderLimit is deprecated, Use SchwabApi.OrderSingle() instead
        /// </summary> 
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="quantity">positive to buy, negative to sell</param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="limitPrice"></param>
        /// <returns></returns>
        [Obsolete("OrderLimit is deprecated, Use SchwabApi.OrderSingle() instead")]
        public long? OrderLimit(string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity, decimal limitPrice)
        {
            return WaitForCompletion(OrderLimitAsync(accountNumber, symbol, assetType, duration, session, quantity, limitPrice));
        }

        /// <summary>
        /// OrderLimitAsync is deprecated, Use SchwabApi.OrderSingle() instead
        /// </summary> 
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="quantity">positive to buy, negative to sell</param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="limitPrice"></param>
        /// <returns></returns>
        [Obsolete("OrderLimitAsync is deprecated, Use SchwabApi.OrderSingleAsync() instead")]
        public async Task<ApiResponseWrapper<long?>> OrderLimitAsync(
                                     string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity, decimal limitPrice)
        {
            var limitOrder = new Order(OrderType.LIMIT, OrderStrategyTypes.SINGLE, session, duration, limitPrice);
            limitOrder.Add(new OrderLeg(symbol, assetType, quantity));
            return await OrderExecuteNewAsync(accountNumber, limitOrder);
        }
    }
}
