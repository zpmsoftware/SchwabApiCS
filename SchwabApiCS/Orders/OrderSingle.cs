// <copyright file="OrderSingle.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using static SchwabApiCS.SchwabApi;

// https://json2csharp.com/

namespace SchwabApiCS
{
    public partial class SchwabApi
    {

        // ================ OrderSingle - Single Order to Buy or Sell, Limit or Market =================================================

        /// <summary>
        /// Order Single - BUY/Sell LIMIT/MARKET
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="orderType"></param>
        /// <param name="session"></param>
        /// <param name="duration"></param>
        /// <param name="position"></param>
        /// <param name="quantity">positive qty to open a long position, negative qty to open a short position</param>
        /// <param name="price"></param>
        /// <returns>orderId or null</returns>
        public long? OrderSingle(string accountNumber, string symbol, Order.AssetType assetType, Order.OrderType orderType,
                               Order.Session session, Order.Duration duration, Order.Position position, decimal quantity, decimal? price = null)
        {
            return WaitForCompletion(OrderSingleAsync(accountNumber, symbol, assetType, orderType, session, duration, position, quantity, price));
        }

        /// <summary>
        /// Order Buy - LIMIT or MARKET async
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="orderType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity">positive qty to open a long position, negative qty to open a short position</param>
        /// <param name="price"></param>
        /// <returns>orderId or null</returns>
        public async Task<ApiResponseWrapper<long?>> OrderSingleAsync(string accountNumber, string symbol, Order.AssetType assetType,
                            Order.OrderType orderType, Order.Session session, Order.Duration duration, Order.Position position, decimal quantity, decimal? price = null)
        {
            Order order;

            switch (orderType)
            {
                case Order.OrderType.LIMIT:
                    if (price == null)
                        return new ApiResponseWrapper<long?>(default, true, 900, "Price is required on LIMIT orders");

                    order = new Order(Order.OrderType.LIMIT, Order.OrderStrategyTypes.SINGLE, session, duration, price);
                    order.Add(new Order.OrderLeg(symbol, assetType, position, quantity));
                    break;

                case Order.OrderType.MARKET:
                    order = new Order(Order.OrderType.MARKET, Order.OrderStrategyTypes.SINGLE, session, duration, price);
                    order.Add(new Order.OrderLeg(symbol, assetType, position, quantity));
                    break;

                default:
                    throw new SchwabApiException("Order Type " + orderType.ToString() + " not implemented");
            }
            return await OrderExecuteNewAsync(accountNumber, order);
        }



        /// <summary>
        /// Order Single - buy/Sell LIMIT/MARKET
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="orderType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity">positive qty to BUY_TO_OPEN, negative qty to SELL_TO_CLOSE.</param>
        /// <param name="price"></param>
        /// <returns>orderId or null</returns>
        [Obsolete("This version of OrderSingle is deprecated. It assumes BUY or SELL instruction based on quantity, which doesn't support shorting or selling optons.")]

        public long? OrderSingle(string accountNumber, string symbol, Order.AssetType assetType, Order.OrderType orderType,
                               Order.Session session, Order.Duration duration, decimal quantity, decimal? price = null)
        {
            return WaitForCompletion(OrderSingleAsync(accountNumber, symbol, assetType, orderType, session, duration, quantity, price));
        }

        /// <summary>
        /// Order Buy - LIMIT or MARKET async
        /// </summary>
        /// <param name="accountNumber">account number or accountNumberHash</param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="orderType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity">positive qty to BUY_TO_OPEN, negative qty to SELL_TO_CLOSE.</param>
        /// <param name="price"></param>
        /// <returns>orderId or null</returns>
        [Obsolete("This version of OrderSingle is deprecated. It assumes BUY or SELL instruction based on quantity, which doesn't support shorting or selling optons.")]
        public async Task<ApiResponseWrapper<long?>> OrderSingleAsync(string accountNumber, string symbol, Order.AssetType assetType,
                            Order.OrderType orderType, Order.Session session, Order.Duration duration, decimal quantity, decimal? price = null)
        {
            Order order;

            switch (orderType)
            {
                case Order.OrderType.LIMIT:
                    if (price == null)
                        return new ApiResponseWrapper<long?>(default, true, 900, "Price is required on LIMIT orders");

                    order = new Order(Order.OrderType.LIMIT, Order.OrderStrategyTypes.SINGLE, session, duration, price);
                    order.Add(new Order.OrderLeg(symbol, assetType, quantity));
                    break;

                case Order.OrderType.MARKET:
                    order = new Order(Order.OrderType.MARKET, Order.OrderStrategyTypes.SINGLE, session, duration, price);
                    order.Add(new Order.OrderLeg(symbol, assetType, quantity));
                    break;

                default:
                    throw new SchwabApiException("Order Type " + orderType.ToString() + " not implemented");
            }
            return await OrderExecuteNewAsync(accountNumber, order);
        }
    }
}
