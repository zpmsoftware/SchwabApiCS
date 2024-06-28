// <copyright file="OrderOCOBracket.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using static SchwabApiCS.Order;

// https://json2csharp.com/

namespace SchwabApiCS
{
    public partial class SchwabApi
    {
        // ========== OCO (one cancels the other) Bracket =========================
        // a limit order and a stop order.
        // The first order to fill cancels the other.
        

        public long? OrderOCOBracket(string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity, decimal? limitPrice, decimal? stopPrice)
        {
            return WaitForCompletion(OrderOCOBracketAsync(accountNumber, symbol, assetType, duration, session,
                                                          quantity, limitPrice, stopPrice));
        }

        /// <summary>
        /// OCO Braket order: limit order + stop(market) order
        /// Use to close a long order (use negative quantity to sell) or close a short position (use positive quantity to buy)
        /// </summary> 
        /// <param name="accountNumber"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="quantity">positive to buy, negative to sell</param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="limitPrice"></param>
        /// <param name="stopPrice"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<long?>> OrderOCOBracketAsync(
                                     string accountNumber, string symbol, Order.AssetType assetType, Order.Duration duration,
                                     Order.Session session, decimal quantity, decimal? limitPrice, decimal? stopPrice)
        {
            var limitOrder = new Order.ChildOrderStrategy(OrderType.LIMIT, OrderStrategyTypes.SINGLE, session, duration, limitPrice);
            limitOrder.Add(new OrderLeg(symbol, assetType, quantity));

            var stopOrder = new Order.ChildOrderStrategy(OrderType.STOP, OrderStrategyTypes.SINGLE, session, duration, stopPrice);
            stopOrder.Add(new OrderLeg(symbol, assetType, quantity));

            var ocoOrder = new Order() {
                orderStrategyType = OrderStrategyTypes.OCO.ToString(),
                accountNumber = accountNumber,
            };
            ocoOrder.Add(limitOrder);
            ocoOrder.Add(stopOrder);

            return await OrderExecuteNewAsync(accountNumber, ocoOrder);
        }
    }
}
