// <copyright file="AccountActivity.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.StreamerRequests;
using System.Security.Authentication;
using System.Windows.Controls;
using static SchwabApiCS.Streamer.LevelOneEquity;
using static SchwabApiCS.Streamer.ResponseMessage;
using static SchwabApiCS.Streamer;
using static SchwabApiCS.Streamer.AccountActivity;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class AccountActivityClass : ServiceClass
        {
            public delegate void AccountActivityCallback(List<AccountActivity> data);

            private List<AccountActivity> Data = new List<AccountActivity>();
            private AccountActivityCallback? Callback = null;

            public AccountActivityClass(Streamer streamer)
                : base(streamer, Streamer.Services.ACCT_ACTIVITY)
            {
            }

            /// <summary>
            /// Account Activity Request
            /// </summary>
            /// <param name="fields">comma separated list of field indexes like "1,2,3.." - see LevelOneEquities.Fields</param>
            /// <param name="callback">method to call whenever values change</param>
            public void Request(AccountActivityCallback callback)
            {
                streamer.ServiceRequest(Services.ACCT_ACTIVITY, "Account Activity", "0,1,2,3");
                Callback = callback;
            }

            /// <summary>
            /// Account Activity response
            /// </summary>
            /// <param name="response"></param>
            /// <exception cref="Exception"></exception>
            internal override void ProcessResponse(ResponseMessage.Response response)
            {
                if (response.content.code != 0)
                {
                    throw new Exception(string.Format(
                        "streamer ACCT_ACTIVITY {0} Error: {1} {2} ", response.command, response.content.code, response.content.msg));
                }

                switch (response.command)
                {
                    case "SUBS":
                        break;
                    case "ADD":
                        break;
                    case "UNSUBS":
                        break;

                    default:
                        break;
                }
            }

            internal override void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                // { "seq": 0, "key": "Account Activity", "1": "", "2": "SUBSCRIBED", "3": "" }
                Data = new List<AccountActivity>();
                foreach (var c in content)
                {
                    var jsonContent = c.ToString().Replace("\"1\":", "accountNumber:").Replace("\"2\":", "activity:").Replace("\"3\":", "data:");
                    var aa = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountActivity>(jsonContent);
                    switch (aa.activity)
                    {
                        case "OrderCreated":
                            break;
                        case "OrderAccepted":
                            break;
                        case "CancelAccepted":
                            aa.cancelAccepted = Newtonsoft.Json.JsonConvert.DeserializeObject<CancelAccepted>(aa.data);
                            aa.orderId = ((CancelAccepted)aa.cancelAccepted).SchwabOrderID;
                            break;
                        case "ExecutionCreated":
                        case "OrderUROutCompleted":
                            break;
                        case "SUBSCRIBED":
                            break;
                        default:
                            break;
                    }
                    Data.Add(aa);
                }
                Callback(Data); // callback to application with updated values
            }
        }

        public class AccountActivity
        {
            public override string ToString()
            {
                return string.Format("{0} {1} {2}", seq, accountNumber, activity);

            }
            public int seq { get; set; } = 0;
            public string key { get; set; }  // "Account Activity"

            public string accountNumber { get; set; }
            public string activity { get; set; }
            public string data { get; set; }

            public long? orderId { get; set; }  // orderId pulled from the data if there is one




            public CancelAccepted cancelAccepted { get; set; }

            public class CancelAccepted
            {
                public long SchwabOrderID { get; set; }
                public string AccountNumber { get; set; }
                public Base_Event BaseEvent { get; set; }

                public class Base_Event
                {
                    public string EventType { get; set; }
                    public CancelAccepted_Event CancelAcceptedEvent { get; set; }
                }

                public class CancelAccepted_Event
                {
                    public string EventType { get; set; }
                    public string LifecycleSchwabOrderID { get; set; }
                    public PlanSubmitDate PlanSubmitDate { get; set; }
                    public string ClientProductCode { get; set; }
                    public bool AutoConfirm { get; set; }
                    public CancelTimeStamp CancelTimeStamp { get; set; }
                    public List<LegCancelRequestInfoList> LegCancelRequestInfoList { get; set; }
                    public string CancelRequestType { get; set; }
                }
                public class CancelAcceptedTime
                {
                    public string DateTimeString { get; set; }
                }

                public class CancelTimeStamp
                {
                    public string DateTimeString { get; set; }
                }

                public class IntendedOrderQuantity
                {
                    public string lo { get; set; }
                    public int signScale { get; set; }
                }

                public class LegCancelRequestInfoList
                {
                    public string LegID { get; set; }
                    public IntendedOrderQuantity IntendedOrderQuantity { get; set; }
                    public RequestedAmount RequestedAmount { get; set; }
                    public string LegStatus { get; set; }
                    public string LegSubStatus { get; set; }
                    public CancelAcceptedTime CancelAcceptedTime { get; set; }
                    public string EventUserID { get; set; }
                }

                public class PlanSubmitDate
                {
                    public string DateTimeString { get; set; }
                }

                public class RequestedAmount
                {
                    public string lo { get; set; }
                    public int signScale { get; set; }
                }

            }
        }

        // OrderCreated
        /* 
         // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class AccountInfo
            {
                public string AccountNumber { get; set; }
                public string AccountBranch { get; set; }
                public string CustomerOrFirmCode { get; set; }
                public string OrderPlacementCustomerID { get; set; }
                public string AccountState { get; set; }
                public string AccountTypeCode { get; set; }
            }

            public class Ask
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class AskSize
            {
                public string lo { get; set; }
            }

            public class AssetOrderEquityOrderLeg
            {
                public OrderInstruction OrderInstruction { get; set; }
                public CommissionInfo CommissionInfo { get; set; }
                public string AssetType { get; set; }
                public string TimeInForce { get; set; }
                public string OrderTypeCode { get; set; }
                public List<OrderLeg> OrderLegs { get; set; }
                public string OrderCapacityCode { get; set; }
                public string SettlementType { get; set; }
                public int Rule80ACode { get; set; }
                public string SolicitedCode { get; set; }
                public string TradeTag { get; set; }
                public EquityOrder EquityOrder { get; set; }
            }

            public class BaseEvent
            {
                public string EventType { get; set; }
                public OrderCreatedEventEquityOrder OrderCreatedEventEquityOrder { get; set; }
            }

            public class Bid
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class BidSize
            {
                public string lo { get; set; }
            }

            public class ClientChannelInfo
            {
                public string ClientProductCode { get; set; }
                public string EventUserID { get; set; }
                public string EventUserType { get; set; }
            }

            public class CommissionInfo
            {
                public EstimatedOrderQuantity EstimatedOrderQuantity { get; set; }
                public EstimatedPrincipalAmount EstimatedPrincipalAmount { get; set; }
                public EstimatedCommissionAmount EstimatedCommissionAmount { get; set; }
            }

            public class EntryTimestamp
            {
                public string DateTimeString { get; set; }
            }

            public class EquityOrder
            {
                public string TradingSessionCodeOnOrder { get; set; }
            }

            public class EquityOrderInstruction
            {
            }

            public class EquityOrderLeg
            {
            }

            public class EstimatedCommissionAmount
            {
                public int signScale { get; set; }
            }

            public class EstimatedNetAmount
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class EstimatedOrderQuantity
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class EstimatedPrincipalAmnt
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class EstimatedPrincipalAmount
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class ExecutionStrategy
            {
                public string Type { get; set; }
                public LimitExecutionStrategy LimitExecutionStrategy { get; set; }
            }

            public class ExpiryTimeStamp
            {
                public string DateTimeString { get; set; }
            }

            public class LeavesQuantity
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class LegClientRequestInfo
            {
                public string SecurityId { get; set; }
                public string SecurityIdTypeCd { get; set; }
            }

            public class LifecycleCreatedTimestamp
            {
                public string DateTimeString { get; set; }
            }

            public class LimitExecutionStrategy
            {
                public string Type { get; set; }
                public LimitPrice LimitPrice { get; set; }
                public string LimitPriceUnitCode { get; set; }
            }

            public class LimitPrice
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class Mid
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class Order
            {
                public string SchwabOrderID { get; set; }
                public string AccountNumber { get; set; }
                public Order Order { get; set; }
                public AccountInfo AccountInfo { get; set; }
                public ClientChannelInfo ClientChannelInfo { get; set; }
                public LifecycleCreatedTimestamp LifecycleCreatedTimestamp { get; set; }
                public string LifecycleSchwabOrderID { get; set; }
                public EntryTimestamp EntryTimestamp { get; set; }
                public ExpiryTimeStamp ExpiryTimeStamp { get; set; }
                public bool AutoConfirm { get; set; }
                public PlanSubmitDate PlanSubmitDate { get; set; }
                public string SourceOMS { get; set; }
                public string FirmID { get; set; }
                public string OrderAccount { get; set; }
                public AssetOrderEquityOrderLeg AssetOrderEquityOrderLeg { get; set; }
            }

            public class OrderCreatedEventEquityOrder
            {
                public string EventType { get; set; }
                public Order Order { get; set; }
            }

            public class OrderInstruction
            {
                public string HandlingInstructionCode { get; set; }
                public ExecutionStrategy ExecutionStrategy { get; set; }
                public PreferredRoute PreferredRoute { get; set; }
                public EquityOrderInstruction EquityOrderInstruction { get; set; }
            }

            public class OrderLeg
            {
                public string LegID { get; set; }
                public string LegParentSchwabOrderID { get; set; }
                public Quantity Quantity { get; set; }
                public string QuantityUnitCodeType { get; set; }
                public LeavesQuantity LeavesQuantity { get; set; }
                public string BuySellCode { get; set; }
                public Security Security { get; set; }
                public QuoteOnOrderAcceptance QuoteOnOrderAcceptance { get; set; }
                public LegClientRequestInfo LegClientRequestInfo { get; set; }
                public string AccountingRuleCode { get; set; }
                public EstimatedNetAmount EstimatedNetAmount { get; set; }
                public EstimatedPrincipalAmnt EstimatedPrincipalAmnt { get; set; }
                public EquityOrderLeg EquityOrderLeg { get; set; }
            }

            public class PlanSubmitDate
            {
                public string DateTimeString { get; set; }
            }

            public class PreferredRoute
            {
            }

            public class Quantity
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class QuoteOnOrderAcceptance
            {
                public Ask Ask { get; set; }
                public AskSize AskSize { get; set; }
                public Bid Bid { get; set; }
                public BidSize BidSize { get; set; }
                public QuoteTimestamp QuoteTimestamp { get; set; }
                public string Symbol { get; set; }
                public string QuoteTypeCode { get; set; }
                public Mid Mid { get; set; }
                public string SchwabOrderID { get; set; }
            }

            public class QuoteTimestamp
            {
                public string DateTimeString { get; set; }
            }

            public class Root
            {
                public string SchwabOrderID { get; set; }
                public string AccountNumber { get; set; }
                public BaseEvent BaseEvent { get; set; }
            }

            public class Security
            {
                public string SchwabSecurityID { get; set; }
                public string Symbol { get; set; }
                public string UnderlyingSymbol { get; set; }
                public string PrimaryExchangeCode { get; set; }
                public string MajorAssetType { get; set; }
                public string PrimaryMarketSymbol { get; set; }
                public string ShortDescriptionText { get; set; }
                public string ShortName { get; set; }
                public string CUSIP { get; set; }
                public string SEDOL { get; set; }
                public string ISIN { get; set; }
            }


         * */

        // OrderAccepted
        /*
         // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            public class Ask
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class AskSize
            {
                public string lo { get; set; }
            }

            public class BaseEvent
            {
                public string EventType { get; set; }
                public OrderAcceptedEvent OrderAcceptedEvent { get; set; }
            }

            public class Bid
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class BidSize
            {
                public string lo { get; set; }
            }

            public class CreatedTimeStamp
            {
                public string DateTimeString { get; set; }
            }

            public class ExpiryTimeStamp
            {
                public string DateTimeString { get; set; }
            }

            public class Mid
            {
                public string lo { get; set; }
                public int signScale { get; set; }
            }

            public class OrderAcceptedEvent
            {
                public string EventType { get; set; }
                public CreatedTimeStamp CreatedTimeStamp { get; set; }
                public ExpiryTimeStamp ExpiryTimeStamp { get; set; }
                public string Status { get; set; }
                public string TradingSessionCodeOnOrderEntry { get; set; }
                public List<QuoteOnOrderEntry> QuoteOnOrderEntry { get; set; }
            }

            public class QuoteOnOrderEntry
            {
                public Ask Ask { get; set; }
                public AskSize AskSize { get; set; }
                public Bid Bid { get; set; }
                public BidSize BidSize { get; set; }
                public QuoteTimestamp QuoteTimestamp { get; set; }
                public string Symbol { get; set; }
                public string QuoteTypeCode { get; set; }
                public Mid Mid { get; set; }
                public string SchwabOrderID { get; set; }
            }

            public class QuoteTimestamp
            {
                public string DateTimeString { get; set; }
            }

            public class Root
            {
                public string SchwabOrderID { get; set; }
                public string AccountNumber { get; set; }
                public BaseEvent BaseEvent { get; set; }
            }


         * */
    }
}

