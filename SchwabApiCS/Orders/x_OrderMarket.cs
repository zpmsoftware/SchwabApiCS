// <copyright file="OrderMarket.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. http://mozilla.org/MPL/2.0/.
// </copyright>

using System;
using static SchwabApiCS.Order;

// https://json2csharp.com/

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        /// <summary>
        /// simple market order
        /// </summary> 
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="quantity">positive to buy, negative to sell</param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        [Obsolete("OrderMarket is deprecated, Use SchwabApi.OrderSingle() instead")]
        public long? OrderMarket(string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity)
        {
            return WaitForCompletion(OrderMarketAsync(accountNumber, symbol, assetType, duration, session, quantity));
        }

        /// <summary>
        /// simple market order async
        /// </summary> 
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="quantity">positive to buy, negative to sell</param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="limitPrice"></param>
        /// <returns></returns>
        [Obsolete("OrderMarketAsync is deprecated, Use SchwabApi.OrderSingleAsync() instead")]
        public async Task<ApiResponseWrapper<long?>> OrderMarketAsync(
                                     string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity)
        {
            var marketOrder = new Order(OrderType.MARKET, OrderStrategyTypes.SINGLE, session, duration);
            marketOrder.Add(new OrderLeg(symbol, assetType, quantity));
            return await OrderExecuteNewAsync(accountNumber, marketOrder);
        }
    }
}
