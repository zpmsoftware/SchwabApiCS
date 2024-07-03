// <copyright file="OrderTriggers.cs" company="ZPM Software Inc">
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
        // ========== First order triggers second order =========================
        // When first order fills, second order is activated


        /// <summary>
        /// First order triggers second order
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="firstOrder"></param>
        /// <param name="secondOrder"></param>
        /// <returns></returns>
        public long? OrderTriggersSecond(string accountNumber, Order firstOrder, ChildOrderStrategy secondOrder)
        {
            return WaitForCompletion(OrderTriggersSecondAsync(accountNumber, firstOrder, secondOrder));

        }

        /// <summary>
        /// First order triggers second order Async
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="firstOrder"></param>
        /// <param name="secondOrder"></param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<long?>> OrderTriggersSecondAsync(string accountNumber, Order firstOrder, ChildOrderStrategy secondOrder)
        {
            firstOrder.orderStrategyType = OrderStrategyTypes.TRIGGER.ToString();
            firstOrder.Add(secondOrder); // add order to submit if first order fills.
            return await OrderExecuteNewAsync(accountNumber, firstOrder);
        }



        // ========== First order triggers OCO bracker orders =========================
        // When orderToOpen order fills, an OCO bracket limit order and stop loss order are submitted.

        /// <summary>
        /// When first order fills, an OCO bracket limit order and stop loss order are submitted.
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="orderToOpen">order to open position</param>
        /// <param name="closePrice">limit price for profit</param>
        /// <param name="stopPrice">stop price for a loss</param>
        /// <returns></returns>
        public long? OrderTriggersOCOBracket(string accountNumber, Order orderToOpen, decimal closePrice, decimal stopPrice)
        {
            return WaitForCompletion(OrderTriggersOCOBracketAsync(accountNumber, orderToOpen, closePrice, stopPrice));
        }

        /// <summary>
        /// When first order fills, an OCO bracket limit order and stop loss order are submitted.
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <param name="orderToOpen">order to open position</param>
        /// <param name="closePrice">limit price for profit</param>
        /// <param name="stopPrice">stop price for a loss</param>
        /// <returns></returns>
        public async Task<ApiResponseWrapper<long?>> OrderTriggersOCOBracketAsync(
                     string accountNumber, Order orderToOpen, decimal closePrice, decimal stopPrice)
        {
            orderToOpen.orderStrategyType = OrderStrategyTypes.TRIGGER.ToString();
            var symbol = orderToOpen.orderLegCollection[0].instrument.symbol;
            var assetType = Order.GetAssetType(orderToOpen.orderLegCollection[0].instrument.assetType);
            var quantity = orderToOpen.orderLegCollection[0].quantity;

            var limitOrder = new Order.ChildOrderStrategy()
            {
                orderType = OrderType.LIMIT.ToString(),
                orderStrategyType = OrderStrategyTypes.SINGLE.ToString(),
                session = orderToOpen.session,
                duration = Order.Duration.GOOD_TILL_CANCEL.ToString(),
                price = closePrice
            };
            limitOrder.Add(new Order.OrderLeg(symbol, assetType, Position.TO_CLOSE, -quantity));

            var stopOrder = new Order.ChildOrderStrategy()
            {
                orderType = OrderType.STOP.ToString(),
                orderStrategyType = OrderStrategyTypes.SINGLE.ToString(),
                session = orderToOpen.session,
                duration = Order.Duration.GOOD_TILL_CANCEL.ToString(),
                stopPrice = stopPrice
            };
            stopOrder.Add(new OrderLeg(symbol, assetType, Position.TO_CLOSE, -quantity));

            var ocoOrder = new Order.ChildOrderStrategy()
            {
                orderStrategyType = OrderStrategyTypes.OCO.ToString()
            };

            ocoOrder.Add(limitOrder);
            ocoOrder.Add(stopOrder);

            orderToOpen.Add(ocoOrder); // add order to execute if first order fills.
            return await OrderExecuteNewAsync(accountNumber, orderToOpen);
        }


        // ======================================================================================

        [Obsolete("this version has been depreciated.")] // to specific.  use OrderTriggersSecond().
        public long? OrderFirstTriggersSecond(
                    string accountNumber, string symbol, Order.AssetType assetType, decimal quantity,
                    Order.Instruction instruction1, Order.OrderType orderType1, Order.Duration duration1, Order.Session session1, decimal? price1,
                    Order.Instruction instruction2, Order.OrderType orderType2, Order.Duration duration2, Order.Session session2, decimal? price2)
        {
            return WaitForCompletion(OrderFirstTriggersSecondAsync(accountNumber, symbol, assetType, quantity,
                   instruction1, orderType1, duration1, session1, price1, instruction2, orderType2, duration2, session2, price2));
        }

        [Obsolete("this version has been depreciated.")] // to specific.  use OrderTriggersSecondAsync().
        public async Task<ApiResponseWrapper<long?>> OrderFirstTriggersSecondAsync(
                    string accountNumber, string symbol, Order.AssetType assetType, decimal quantity,
                    Order.Instruction instruction1, Order.OrderType orderType1,  Order.Duration duration1, Order.Session session1, decimal? price1,
                    Order.Instruction instruction2, Order.OrderType orderType2, Order.Duration duration2, Order.Session session2, decimal? price2)
        {
            var order = new Order()
            {
                orderType = orderType1.ToString(),
                session = session1.ToString(),
                duration = duration1.ToString(),
                orderStrategyType = Order.OrderStrategyTypes.TRIGGER.ToString(),
                price = price1,
                orderLegCollection = new List<OrderLeg>()
            };

            order.orderLegCollection.Add(new OrderLeg()
            {
                instruction = instruction1.ToString(),
                quantity = quantity,
                instrument = new Order.Instrument() { symbol = symbol, assetType = assetType.ToString() }
            });

            order.AddChildOrderStrategy(new ChildOrderStrategy()
            {
                orderType = orderType2.ToString(),
                session = session2.ToString(),
                duration = duration2.ToString(),
                orderStrategyType = Order.OrderStrategyTypes.SINGLE.ToString(),
                price = price2
            });

            order.childOrderStrategies[0].AddOrderLeg(new OrderLeg()
            {
                instruction = instruction2.ToString(),
                quantity = quantity,
                instrument = new Order.Instrument() { symbol = symbol, assetType = assetType.ToString() }
            });

            return await OrderExecuteNewAsync(accountNumber, order);
        }
    }
}
