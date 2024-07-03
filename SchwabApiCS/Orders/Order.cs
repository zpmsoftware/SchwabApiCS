// <copyright file="Order.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;

namespace SchwabApiCS
{

    /// <summary>
    /// Schwab order class - all orders use this class
    /// </summary>
    public class Order
    {
        // ====== ORDER CONSTRUCTORS ================================================
        #region Order_Constructors
        public Order()  { }  // empty order

        /// <summary>
        /// Generic order
        /// </summary>
        /// <param name="orderType">LIMIT, MARKET, STOP etc</param>
        /// <param name="orderStrategyTypes"></param>
        /// <param name="session">NORMAL,AM,PM.SEAMLESS</param>
        /// <param name="duration">DAY, GOOD_TILL_CANCEL, etc</param>
        /// <param name="price"></param>
        public Order(OrderType orderType, OrderStrategyTypes orderStrategyTypes, Session session, Duration duration, decimal? price = null)
        {
            this.orderType = orderType.ToString();
            this.orderStrategyType = orderStrategyTypes.ToString();
            this.session = session.ToString();
            this.duration = duration.ToString();
            if (orderType == OrderType.STOP)
                this.stopPrice = SchwabApi.PriceAdjust(price);
            else
                this.price = SchwabApi.PriceAdjust(price);
        }
        
        #endregion Order_Constructors


        // ==== ORDER HELPER METHODS ========================================================
        #region Order_Helper_Methods

        public void Add(OrderLeg orderLeg)
        {
            if (orderLegCollection == null)
                orderLegCollection = new List<OrderLeg>();
            orderLegCollection.Add(orderLeg);
        }

        public void Add(ChildOrderStrategy childOrderStrategy)
        {
            if (childOrderStrategies == null)
                childOrderStrategies = new List<ChildOrderStrategy>();
            childOrderStrategies.Add(childOrderStrategy);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} Qty: {5}, {6} {7}",
                accountNumber, orderStrategyType, orderLegCollection[0].instrument.symbol, orderLegCollection[0].instruction,
                orderType, orderLegCollection[0].quantity.ToString("#.##"),
                enteredTime == null ? "" : ((DateTime)enteredTime).ToString("yyyy-MM-dd hh:mm:ss tt,"),
                status
                );
        }

        public string JsonSerialize()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static SchwabApiCS.Order.OrderType GetOrderType(string orderType)
        {
            return SchwabApi.GetEnum<SchwabApiCS.Order.OrderType>(orderType);
        }

        public static SchwabApiCS.Order.AssetType GetAssetType(string assetType)
        {
            if (assetType == "COLLECTIVE_INVESTMENT")
                return SchwabApiCS.Order.AssetType.EQUITY;

            return SchwabApi.GetEnum<SchwabApiCS.Order.AssetType>(assetType);
        }

        public static SchwabApiCS.Order.Duration GetDuration(string duration)
        {
            if (duration == "GTC")
                return SchwabApiCS.Order.Duration.GOOD_TILL_CANCEL;

            return SchwabApi.GetEnum<SchwabApiCS.Order.Duration>(duration);
        }

        public static SchwabApiCS.Order.Session GetSession(string session)
        {
            return SchwabApi.GetEnum<SchwabApiCS.Order.Session>(session);
        }


        public static SchwabApiCS.Order.OrderStrategyTypes GetOrderStrategyType(string orderStrategyType)
        {
            return SchwabApi.GetEnum<SchwabApiCS.Order.OrderStrategyTypes>(orderStrategyType);
        }

        #endregion Order_Helper_Methods


        // ========= ORDER_ENUMS DEFINITIONS ==================================================
        #region Order_Enums

        public enum Position { TO_OPEN, TO_CLOSE };

        public enum Instruction { BUY, SELL, BUY_TO_COVER, SELL_SHORT, BUY_TO_OPEN, BUY_TO_CLOSE, SELL_TO_OPEN, SELL_TO_CLOSE, EXCHANGE, SELL_SHORT_EXEMPT };

        public enum OrderType
        {
            MARKET, LIMIT, STOP, STOP_LIMIT, TRAILING_STOP, CABINET, NON_MARKETABLE, MARKET_ON_CLOSE,
            EXERCISE, TRAILING_STOP_LIMIT, NET_DEBIT, NET_CREDIT, NET_ZERO, LIMIT_ON_CLOSE, UNKNOWN
        };
        public enum OrderTypeRequest // Same as orderType, but does not have UNKNOWN since this type is not allowed as an input
        {
            MARKET, LIMIT, STOP, STOP_LIMIT, TRAILING_STOP, CABINET, NON_MARKETABLE, MARKET_ON_CLOSE,
            EXERCISE, TRAILING_STOP_LIMIT, NET_DEBIT, NET_CREDIT, NET_ZERO, LIMIT_ON_CLOSE, UNKNOWN
        };
        public enum Duration
        {
            DAY, GOOD_TILL_CANCEL, FILL_OR_KILL, IMMEDIATE_OR_CANCEL, END_OF_WEEK,
            END_OF_MONTH, NEXT_END_OF_MONTH, UNKNOWN
        }
        public enum ComplexOrderStrategyType
        {
            NONE, COVERED, VERTICAL, BACK_RATIO, CALENDAR, DIAGONAL, STRADDLE, STRANGLE, COLLAR_SYNTHETIC, BUTTERFLY,
            CONDOR, IRON_CONDOR, VERTICAL_ROLL, COLLAR_WITH_STOCK, DOUBLE_DIAGONAL, UNBALANCED_BUTTERFLY,
            UNBALANCED_CONDOR, UNBALANCED_IRON_CONDOR, UNBALANCED_VERTICAL_ROLL, MUTUAL_FUND_SWAP, CUSTOM
        }

        public enum AssetType { BOND, EQUITY, ETF, EXTENDED, FOREX, FUTURE, FUTURE_OPTION, FUNDAMENTAL, INDEX, INDICATOR, MUTUAL_FUND, OPTION, UNKNOWN }
        public enum Session { NORMAL, AM, PM, SEAMLESS };
        public enum RequestedDestination { INET, ECN_ARCA, CBOE, AMEX, PHLX, ISE, BOX, NYSE, NASDAQ, BATS, C2, AUTO };
        public enum StopPriceLinkBasis { MANUAL, BASE, TRIGGER, LAST, BID, ASK, ASK_BID, MARK, AVERAGE };
        public enum StopPriceLinkType { VALUE, PERCENT, TICK };
        public enum StopType { STANDARD, BID, ASK, LAST, MARK };
        public enum PriceLinkBasis { MANUAL, BASE, TRIGGER, LAST, BID, ASK, ASK_BID, MARK, AVERAGE };
        public enum PriceLinkType { VALUE, PERCENT, TICK };
        public enum TaxLotMethod { FIFO, LIFO, HIGH_COST, LOW_COST, AVERAGE_COST, SPECIFIC_LOT, LOSS_HARVESTER };
        public enum SpecialInstruction { ALL_OR_NONE, DO_NOT_REDUCE, ALL_OR_NONE_DO_NOT_REDUCE };
        public enum OrderStrategyTypes { SINGLE, CANCEL, RECALL, PAIR, FLATTEN, TWO_DAY_SWAP, BLAST_ALL, OCO, TRIGGER };
        public enum Status
        {
            AWAITING_PARENT_ORDER, AWAITING_CONDITION, AWAITING_STOP_CONDITION, AWAITING_MANUAL_REVIEW,
            ACCEPTED, AWAITING_UR_OUT, PENDING_ACTIVATION, QUEUED, WORKING, REJECTED, PENDING_CANCEL,
            CANCELED, PENDING_REPLACE, REPLACED, FILLED, EXPIRED, NEW, AWAITING_RELEASE_TIME,
            PENDING_ACKNOWLEDGEMENT, PENDING_RECALL, UNKNOWN
        };
        public enum AmountIndicator { DOLLARS, SHARES, ALL_SHARES, PERCENTAGE, UNKNOWN };
        public enum SettlementInstruction { REGULAR, CASH, NEXT_DAY, UNKNOWN };

        #endregion Order_Enums


        // ============ ORDER_PROPERTIES =====================================================
        #region Orrder_Properties


        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? orderType { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? session { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? duration { get; set; }

        [DefaultValue(0D)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public decimal? price { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? orderStrategyType { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? cancelTime { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string complexOrderStrategyType { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? quantity { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? filledQuantity { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? remainingQuantity { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string requestedDestination { get; set; } = "";// not in buy order

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string destinationLinkName { get; set; } = "";

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? stopPrice { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string stopType { get; set; } = "";

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<OrderLeg> orderLegCollection { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? orderId { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? cancelable { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? editable { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string status { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? enteredTime { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? filledTime { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string tag { get; set; } = "";

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? accountNumber { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? closeTime { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<OrderActivity> orderActivityCollection { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ChildOrderStrategy> childOrderStrategies { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? statusDescription { get; set; }

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? specialInstruction { get; set; }

        #endregion Orrder_Properties


        // ========= ExecutionLeg Class ======================================================
        #region ExecutionLeg_Class

        public class ExecutionLeg
        {
            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public long? legId { get; set; }

            public decimal quantity { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? mismarkedQuantity { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? price { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public DateTime? time { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public long? instrumentId { get; set; }
        }
        #endregion ExecutionLeg_Class


        // ========= Instrument Class =========================================================
        #region Instrument_Class
        public class Instrument
        {
            public string assetType { get; set; } = "";

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? cusip { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? symbol { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public long? instrumentId { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? description { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? positionEffect { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? type { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? putCall { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? underlyingSymbol { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<OptionDeliverable>? optionDeliverables { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public DateTime? maturityDate { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? variableRate { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? netChange { get; set; }
        }
        #endregion Instrument_Class


        // ========= OptionDeliverable Class ===================================================
        #region OptionDeliverable_Class
        public class OptionDeliverable
        {
            public string? symbol { get; set; }
            public decimal? deliverableUnits { get; set; }
        }
        #endregion OptionDeliverable_Class


        // ========= Instrument Class ==========================================================
        #region Instrument_Class

        public class OrderActivity
        {
            public string? activityType { get; set; }
            public string? executionType { get; set; }
            public decimal quantity { get; set; }
            public decimal? orderRemainingQuantity { get; set; }
            public List<ExecutionLeg>? executionLegs { get; set; }
        }
        #endregion Instrument_Class


        // ========= OrderLeg Class ============================================================
        #region OrderLeg_Class

        public class OrderLeg
        {
            public OrderLeg() { }  // empty order leg

            /// <summary>
            /// New Order Leg.  Assumes opening a position if quantity >0 or closing if < 0.  Does not support shorting equities oe selling options.
            /// </summary>
            /// <param name="symbol"></param>
            /// <param name="assetType"></param>
            /// <param name="quantity">Negative to sell, positive to buy</param>
            /// <returns></returns>
            [Obsolete("This OrderLeg constructor is deprecated. It assumes buy/sell based on quantity, which doesn't support shorting or selling optons.")]
            public OrderLeg(string symbol, AssetType assetType, decimal quantity)
            {
                CalculateInstruction(assetType, quantity > 0 ? Position.TO_OPEN : Position.TO_CLOSE , quantity);
                this.quantity = Math.Abs(quantity);
                this.instrument = new Order.Instrument() { symbol = symbol, assetType = assetType.ToString() };
            }


            /// <summary>
            /// New Order Leg
            /// </summary>
            /// <param name="symbol"></param>
            /// <param name="assetType"></param>
            /// <param name="position">TO_OPEN, TO_CLOSE</param>
            /// <param name="quantity">Negative to sell, positive to buy</param>
            /// <returns></returns>
            public OrderLeg(string symbol, AssetType assetType, Position position, decimal quantity)
            {
                CalculateInstruction(assetType, position, quantity);
                this.quantity = Math.Abs(quantity);
                this.instrument = new Order.Instrument() { symbol = symbol, assetType = assetType.ToString() };
            }

            /// <summary>
            /// New Order Leg
            /// </summary>
            /// <param name="symbol"></param>
            /// <param name="assetType"></param>
            /// <param name="instruction">BUY_TO_OPEN, SELL_TO_CLOSE, etc.</param>
            /// <param name="quantity">Negative to sell, positive to buy</param>
            /// <returns></returns>
            public OrderLeg(string symbol, AssetType assetType, Instruction instruction, decimal quantity)
            {
                this.instruction = instruction.ToString();
                this.quantity = Math.Abs(quantity);
                this.instrument = new Order.Instrument() { symbol = symbol, assetType = assetType.ToString() };
            }

            private void CalculateInstruction(AssetType assetType, Position position, decimal quantity)
            {
                if (assetType == AssetType.OPTION)
                {
                    if (position == Position.TO_OPEN)
                        this.instruction = (quantity > 0 ? Instruction.BUY_TO_OPEN : Instruction.SELL_TO_OPEN).ToString();
                    else // TO_CLOSE
                        this.instruction = (quantity > 0 ? Instruction.BUY_TO_CLOSE : Instruction.SELL_TO_CLOSE).ToString();
                }
                else
                {
                    if (position == Position.TO_OPEN)
                        this.instruction = (quantity > 0 ? Instruction.BUY : Instruction.SELL_SHORT).ToString();
                    else // TO_CLOSE
                        this.instruction = (quantity > 0 ? Instruction.BUY_TO_COVER : Instruction.SELL).ToString();
                }
            }

            public string instruction { get; set; }
            public decimal quantity { get; set; }
            public Instrument instrument { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? orderLegType { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public long? legId { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? positionEffect { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? quantityType { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? divCapGains { get; set; }
            // public string toSymbol { get; set; }
        }
        #endregion OrderLeg_Class


        // ========= ChildOrderStrategy Class ===================================================
        #region ChildOrderStrategy_Class

        public class ChildOrderStrategy
        {
            public ChildOrderStrategy() { }

            public ChildOrderStrategy(OrderType orderType, OrderStrategyTypes orderStrategyTypes, Session session, Duration duration, decimal? price = null)
            {
                this.orderType = orderType.ToString();
                this.orderStrategyType = orderStrategyTypes.ToString();
                this.session = session.ToString();
                this.duration = duration.ToString();

                if (orderType == OrderType.STOP)
                    this.stopPrice = SchwabApi.PriceAdjust(price);
                else
                    this.price = SchwabApi.PriceAdjust(price);
            }

            public void Add(OrderLeg orderLeg)
            {
                if (orderLegCollection == null)
                    orderLegCollection = new List<OrderLeg>();
                orderLegCollection.Add(orderLeg);
            }

            public void Add(ChildOrderStrategy childOrderStrategy)
            {
                if (childOrderStrategies == null)
                    childOrderStrategies = new List<ChildOrderStrategy>();
                childOrderStrategies.Add(childOrderStrategy);
            }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string orderStrategyType { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string orderType { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string session { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string duration { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? stopPrice { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? quantity { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? price { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<OrderLeg> orderLegCollection { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? complexOrderStrategyType { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? filledQuantity { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public decimal? remainingQuantity { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? requestedDestination { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? destinationLinkName { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public long? orderId { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? cancelable { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? editable { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? status { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public DateTime? enteredTime { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public DateTime? closeTime { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? tag { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? accountNumber { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? statusDescription { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public DateTime? cancelTime { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? stopType { get; set; }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<ChildOrderStrategy> childOrderStrategies { get; set; }


            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<OrderActivity>? orderActivityCollection { get; set; }


            [Obsolete("this version has been depreciated.")]
            public void AddOrderLeg(OrderLeg orderLeg)
            {
                if (orderLegCollection == null)
                    orderLegCollection = new List<OrderLeg>();
                orderLegCollection.Add(orderLeg);
            }

        }

        #endregion ChildOrderStrategy_Class



        // ======================================================================================
        // ========= Depreciated Methods ========================================================
        #region Depreciated_Methods
        /// <summary>
        /// obsolete - use Add(childOrderStrategy)
        /// </summary>
        /// <param name="childOrderStrategy"></param>
        [Obsolete("AddChildOrderStrategy is deprecated, Use Order.Add(ChildOrderStrategy).")]
        public void AddChildOrderStrategy(ChildOrderStrategy childOrderStrategy)
        {
            Add(childOrderStrategy);
        }

        // ============= deprecated ==========================

        /// <summary>
        ///  OrderLimit is deprecated, Use SchwabApi.OrderLimit(). create simple LIMIT BUY or SELL order. 
        /// </summary>
        /// <param name="instruction">BUY or SELL</param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        [Obsolete("OrderLimit is deprecated, Use SchwabApi.OrderSingle().")]
        public static Order OrderLimit(string instruction, string symbol, Order.AssetType assetType, Order.Duration duration,
                                  Order.Session session, decimal quantity, decimal price)
        {
            return CreateOrder(instruction, OrderType.LIMIT, symbol, assetType, duration, session, quantity, price);
        }

        /// <summary>
        ///  This OrderMarket is deprecated, Use SchwabApi.OrderMarket()
        /// </summary>
        /// <param name="instruction">BUY or SELL</param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        [Obsolete("This OrderMarket is deprecated, Use SchwabApi.OrderSingle().")]
        public static Order OrderMarket(string instruction, string symbol, Order.AssetType assetType, Order.Duration duration,
                                  Order.Session session, decimal quantity)
        {
            return CreateOrder(instruction, OrderType.MARKET, symbol, assetType, duration, session, quantity);
        }


        /// <summary>
        /// CreateOrder is deprecated, Use one of the other Order constructors
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="orderType"></param>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [Obsolete("CreateOrder is deprecated, Use one of the other Order constructors.")]
        private static Order CreateOrder(string instruction, Order.OrderType orderType, string symbol, Order.AssetType assetType, Order.Duration duration,
                                          Order.Session session, decimal quantity, decimal? price = null)
        {
            if (orderType != OrderType.LIMIT && orderType != OrderType.MARKET)
                throw new SchwabApiException("Create OrderLimit, orderType '" + orderType.ToString() + "' not valid");

            var order = new Order()
            {
                orderType = orderType.ToString(),
                session = session.ToString(),
                duration = duration.ToString(),
                orderStrategyType = Order.OrderStrategyTypes.SINGLE.ToString(),
                price = price,  // ignored for MARKET order
                orderLegCollection = new List<OrderLeg>()
            };
            order.orderLegCollection.Add(new OrderLeg()
            {
                instruction = instruction,
                quantity = quantity,
                instrument = new Instrument() { symbol = symbol, assetType = assetType.ToString() }
            });
            return order;
        }

        /// <summary>
        /// OrderStopLoss is deprecated, Use SchwabApi.OrderStopLoss()
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="assetType"></param>
        /// <param name="duration"></param>
        /// <param name="session"></param>
        /// <param name="quantity"></param>
        /// <param name="stopPrice"></param>
        /// <returns></returns>
        [Obsolete("OrderStopLoss is deprecated, Use SchwabApi.OrderStopLoss()")]
        public static Order OrderStopLoss(string symbol, Order.AssetType assetType, Order.Duration duration,
                                          Order.Session session, decimal quantity, decimal stopPrice)
        {
            var order = new Order()
            {
                orderType = OrderType.STOP.ToString(),
                session = session.ToString(),
                duration = duration.ToString(),
                orderStrategyType = Order.OrderStrategyTypes.SINGLE.ToString(),
                stopPrice = stopPrice,  // ignored for MARKET order
                orderLegCollection = new List<OrderLeg>()
            };
            order.orderLegCollection.Add(new OrderLeg()
            {
                instruction = "SELL",
                quantity = quantity,
                instrument = new Instrument() { symbol = symbol, assetType = assetType.ToString() }
            });
            return order;
        }

        #endregion Depreciated_Methods
    }
}
