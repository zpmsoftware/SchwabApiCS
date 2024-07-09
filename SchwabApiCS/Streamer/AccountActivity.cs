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
using static SchwabApiCS.Streamer.AccountActivity.CancelAccepted;
using static SchwabApiCS.Streamer.AccountActivity.OrderCreated;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class AccountActivityService : Service
        {
            public delegate void AccountActivityCallback(List<AccountActivity> data);

            private List<AccountActivity> Data = new List<AccountActivity>();
            private AccountActivityCallback? Callback = null;

            public AccountActivityService(Streamer streamer, string reference)
                : base(streamer, Service.Services.ACCT_ACTIVITY, reference)
            {
            }

            private static List<Type>? accountActivityClasses = null;
            /// <summary>
            /// list of class types for all known AccountActivity classes
            /// </summary>
            public static List<Type>? AccountActivityClasses
            {
                get { 
                    if (accountActivityClasses == null)
                    {
                        accountActivityClasses = AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(t => t.GetTypes()).Where(
                                t => t.IsClass &&
                                t.FullName.StartsWith("SchwabApiCS.Streamer+AccountActivity") &&
                                t.DeclaringType.Name == "AccountActivity")
                           .ToList();
                    }
                    return accountActivityClasses;
                }
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

            internal override void ProcessResponseSUBS(ResponseMessage.Response response)
            {
                // do nothing
            }

            internal override void ProcessData(DataMessage.DataItem d, dynamic content)
            {
                Data = new List<AccountActivity>();
                foreach (var c in content)
                {
                    var jsonContent = c.ToString(); //.Replace("\"1\":", "accountNumber:").Replace("\"2\":", "activity:").Replace("\"3\":", "data:"); - handled by json property decorations now

                    var aa = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountActivity>(jsonContent);
                    Data.Add(aa);

                    var type = AccountActivityClasses.Where(r => r.Name == aa.activity).SingleOrDefault();
                    if (type != null)  // convert to known class types
                    {
                        try
                        {
                            aa.activityObject = Newtonsoft.Json.JsonConvert.DeserializeObject(aa.data, type, jsonSettings);
                        }
                        catch (Exception ex) { // breakpoint here to catch if there are fields in the json string not in the c# class.
                            aa.activityObject = Newtonsoft.Json.JsonConvert.DeserializeObject(aa.data, type); // retry and ignore missing fields
                        }
                    }
                    else if (aa.activity != "SUBSCRIBED") // skip subscribed
                    {
                        var xx = 1;  // use to break on new type
                    }
                }

                if (Data.Count > 1)
                { // check for duplicates
                    var Data2 = Data.DistinctBy(r => new { r.accountNumber, r.activity, r.data }).ToList();
                    if (Data2.Count != Data.Count)
                    {
                        var xx = 1;  // use as a breakpoint when duplicates encountered.  Doesn't always happen
                    }
                    Callback(Data2);
                } else
                    Callback(Data); // callback to application with updated values
            }

            internal override void RemoveFromData(string symbol) // DO NOTHING FOR THIS CLASS
            {
            }
        }

        public class AccountActivity
        {
            public override string ToString()
            {
                return string.Format("{0} {1} {2} {3}", seq, accountNumber, activity, activityObject == null ? "" : activityObject.SchwabOrderID);

            }
            public int seq { get; set; } = 0;
            public string key { get; set; }  // "Account Activity"

            [JsonProperty("1")]
            public string accountNumber { get; set; }

            [JsonProperty("2")]
            public string activity { get; set; }

            [JsonProperty("3")]
            public string data { get; set; }
            public ActivityObject? activityObject { get; set; }


            /// <summary>
            /// Parent class for all activity typesd
            /// </summary>
            public class ActivityObject
            {
                public long SchwabOrderID { get; set; }
                public string AccountNumber { get; set; }
            }


            // =================================================================================================
            #region Cancel Accepted
            public class CancelAccepted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, CancelAcceptedEvent CancelAcceptedEvent);
                public record CancelAcceptedEvent(string EventType, string LifecycleSchwabOrderID, PlanSubmitDate PlanSubmitDate,
                              string ClientProductCode, bool AutoConfirm, CancelTimeStamp CancelTimeStamp,
                              List<LegCancelRequestInfoList> LegCancelRequestInfoList, string CancelRequestType);
                public record CancelAcceptedTime(string DateTimeString);
                public record CancelTimeStamp(string DateTimeString);
                public record IntendedOrderQuantity(string lo, int signScale);
                public record LegCancelRequestInfoList(string LegID, IntendedOrderQuantity IntendedOrderQuantity, string ChangedNewOrderID,
                              RequestedAmount RequestedAmount, string LegStatus, string LegSubStatus, string ChangedNewSchwabOrderId,
                              CancelAcceptedTime CancelAcceptedTime, string EventUserID);
                public record PlanSubmitDate(string DateTimeString);
                public record RequestedAmount(string lo, int signScale);
            }
            #endregion Cancel Accepted


            // =================================================================================================
            #region Order Created
            public class OrderCreated : ActivityObject  // using "record" for compactness, speed and immutable
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, OrderCreatedEventEquityOrder OrderCreatedEventEquityOrder);
                public record AccountInfo(string AccountNumber, string AccountBranch, string CustomerOrFirmCode,
                              string OrderPlacementCustomerID, string AccountState, string AccountTypeCode);
                public record Ask(string lo, int signScale);
                public record AskSize(string lo);
                public record AssetOrderEquityOrderLeg(OrderInstruction OrderInstruction, CommissionInfo CommissionInfo, string AssetType,
                              string TimeInForce, string OrderTypeCode, List<OrderLeg> OrderLegs, string OrderCapacityCode,
                              string SettlementType, int Rule80ACode, string SolicitedCode, string TradeTag, EquityOrder EquityOrder);
                public record Bid(string lo, int signScale);
                public record BidSize(string lo);
                public record ClientChannelInfo(string ClientProductCode, string EventUserID, string EventUserType);
                public record CommissionInfo(EstimatedOrderQuantity EstimatedOrderQuantity,
                              EstimatedPrincipalAmount EstimatedPrincipalAmount, EstimatedCommissionAmount EstimatedCommissionAmount);
                public record EntryTimestamp(string DateTimeString);
                public record EquityOptionsOrderLeg(string OpenClosePositionCode);
                public record EquityOrder(string TradingSessionCodeOnOrder);
                public record EquityOrderInstruction();
                public record EquityOrderLeg(EquityOptionsOrderLeg EquityOptionsOrderLeg);
                public record EstimatedCommissionAmount(string lo, int signScale);
                public record EstimatedNetAmount(string lo, int signScale);
                public record EstimatedOrderQuantity(string lo, int signScale);
                public record EstimatedPrincipalAmnt(string lo, int signScale);
                public record EstimatedPrincipalAmount(string lo, int signScale);
                public record ExecutionStrategy(string Type, LimitExecutionStrategy LimitExecutionStrategy);
                public record ExpiryTimeStamp(string DateTimeString);
                public record LeavesQuantity(string lo, int signScale);
                public record LegClientRequestInfo(string SecurityId, string SecurityIdTypeCd);
                public record LifecycleCreatedTimestamp(string DateTimeString);
                public record LimitExecutionStrategy(string Type, LimitPrice LimitPrice, string LimitPriceUnitCode);
                public record LimitPrice(string lo, int signScale);
                public record Mid(string lo, int signScale);
                public record OptionExpiryDate(string DateTimeString);
                public record OptionsQuote(string PutCallCode);
                public record OptionsSecurityInfo(string PutCallCode, string UnderlyingSchwabSecurityID, StrikePrice StrikePrice,
                              OptionExpiryDate OptionExpiryDate);
                public record Order_(string SchwabOrderID, string AccountNumber, Order_ Order, AccountInfo AccountInfo,
                              ClientChannelInfo ClientChannelInfo, LifecycleCreatedTimestamp LifecycleCreatedTimestamp,
                              string LifecycleSchwabOrderID, EntryTimestamp EntryTimestamp, ExpiryTimeStamp ExpiryTimeStamp,
                              bool AutoConfirm, PlanSubmitDate PlanSubmitDate, string SourceOMS, string FirmID, string OrderAccount,
                              AssetOrderEquityOrderLeg AssetOrderEquityOrderLeg);
                public record OrderCreatedEventEquityOrder(string EventType, Order_ Order);
                public record OrderInstruction(string HandlingInstructionCode, ExecutionStrategy ExecutionStrategy,
                              PreferredRoute PreferredRoute, EquityOrderInstruction EquityOrderInstruction);

                public record OrderLeg(string LegID, string LegParentSchwabOrderID, Quantity Quantity, string QuantityUnitCodeType,
                              LeavesQuantity LeavesQuantity, string BuySellCode, Security Security,
                              QuoteOnOrderAcceptance QuoteOnOrderAcceptance, LegClientRequestInfo LegClientRequestInfo,
                              string AccountingRuleCode, EstimatedNetAmount EstimatedNetAmount,
                              EstimatedPrincipalAmnt EstimatedPrincipalAmnt, EquityOrderLeg EquityOrderLeg);
                public record PlanSubmitDate(string DateTimeString);
                public record PreferredRoute();
                public record Quantity(string lo, int signScale);
                public record QuoteOnOrderAcceptance(Ask Ask, AskSize AskSize, Bid Bid, BidSize BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, Mid Mid, string SchwabOrderID, OptionsQuote OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
                public record Security(string SchwabSecurityID, string Symbol, string UnderlyingSymbol, string PrimaryExchangeCode, string MajorAssetType,
                              string PrimaryMarketSymbol, string ShortDescriptionText, string ShortName, string CUSIP,
                              string SEDOL, string ISIN, OptionsSecurityInfo OptionsSecurityInfo);
                public record StrikePrice(string lo, int signScale);
            }
            #endregion Order Created


            // =================================================================================================
            #region Order Accepted
            public class OrderAccepted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Ask(string lo, int signScale);
                public record AskSize(string lo);
                public record Base_Event(string EventType, OrderAcceptedEvent OrderAcceptedEvent);
                public record Bid(string lo, int signScale);
                public record BidSize(string lo);
                public record CreatedTimeStamp(string DateTimeString);
                public record ExpiryTimeStamp(string DateTimeString);
                public record Mid(string lo, int signScale);
                public record OptionsQuote(string PutCallCode);
                public record OrderAcceptedEvent(string EventType, CreatedTimeStamp CreatedTimeStamp, ExpiryTimeStamp ExpiryTimeStamp,
                                    string Status, string TradingSessionCodeOnOrderEntry, List<QuoteOnOrderEntry> QuoteOnOrderEntry);
                public record QuoteOnOrderEntry(Ask Ask, AskSize AskSize, Bid Bid, BidSize BidSize, QuoteTimestamp QuoteTimestamp,
                                    string Symbol, string QuoteTypeCode, Mid Mid, string SchwabOrderID, OptionsQuote OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
            }
            #endregion Order Accepted


            // =================================================================================================
            #region Execution Requested
            public class ExecutionRequested : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Ask(string lo, int signScale);
                public record AskSize(string lo);
                public record Base_Event(string EventType, ExecutionRequestedEventRoutedInfo ExecutionRequestedEventRoutedInfo);
                public record Bid(string lo, int signScale);
                public record BidSize(string lo);
                public record ExecutionRequestedEventRoutedInfo(string EventType, int RouteSequenceNumber, RouteInfo RouteInfo,
                                    string RouteRequestedBy, string LegId);
                public record Mid(string lo, int signScale);
                public record Quote(Ask Ask, AskSize AskSize, Bid Bid, BidSize BidSize, QuoteTimestamp QuoteTimestamp, string Symbol,
                                    string QuoteTypeCode, Mid Mid, string SchwabOrderID);
                public record QuoteTimestamp(string DateTimeString);
                public record RouteAcknowledgmentTimeStamp();
                public record RoutedExecutionTimestamp(string DateTimeString);
                public record RoutedPrice(string lo, int signScale);
                public record RoutedQuantity(string lo, int signScale);
                public record RoutedTime(string DateTimeString);
                public record RouteInfo(string RouteName, int RouteSequenceNumber, RoutedExecutionTimestamp RoutedExecutionTimestamp,
                                        Quote Quote, string RouteRequestedType, RoutedQuantity RoutedQuantity, RoutedPrice RoutedPrice,
                                        string RouteStatus, string ClientOrderID, RoutedTime RoutedTime, string RouteTimeInForce,
                                        RouteAcknowledgmentTimeStamp RouteAcknowledgmentTimeStamp);
            }
            #endregion Execution Requested


            // =================================================================================================
            #region Execution Created
            public class ExecutionCreated : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record AsOfTimeStamp(string DateTimeString);
                public record Base_Event(string EventType, ExecutionCreatedEventExecutionInfo ExecutionCreatedEventExecutionInfo);
                public record ExecutionCreatedEventExecutionInfo(string EventType, string LegId, ExecutionInfo ExecutionInfo,
                              AsOfTimeStamp AsOfTimeStamp, int RouteSequenceNumber);
                public record ExecutionInfo(int ExecutionSequenceNumber, string ExecutionId, ExecutionQuantity ExecutionQuantity,
                              ExecutionTimeStamp ExecutionTimeStamp, string ExecutionTransType, string ExecutionCapacityCode,
                              string RouteName, int RouteSequenceNumber, VenuExecutionTimeStamp VenuExecutionTimeStamp, string CancelType,
                              string ReportingCapacityCode, AsOfTimeStamp AsOfTimeStamp, string ClientOrderID);
                public record ExecutionQuantity(string lo, int signScale);
                public record ExecutionTimeStamp(string DateTimeString);
                public record VenuExecutionTimeStamp(string DateTimeString);
            }
            #endregion Execution Created


            // =================================================================================================
            #region Execution Request Created
            public class ExecutionRequestCreated : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, ExecutionRequestCreatedEvent ExecutionRequestCreatedEvent);
                public record ExecutionRequestCreatedEvent(string EventType, string LegId, string RouteName, string RouteRequestType,
                              int RouteSequenceNumber, string RouteRequestedBy, string RouteStatus, string SenderCompID,
                              RoutedTime RoutedTime, string ClientOrderID);
                public record RoutedTime(string DateTimeString);
            }
            #endregion Execution Request Created


            // =================================================================================================
            #region Execution Request Completed
            public class ExecutionRequestCompleted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }
                public record Base_Event(string EventType, ExecutionRequestCompletedEvent ExecutionRequestCompletedEvent);
                public record ExecutionRequestCompletedEvent(string EventType, string LegId, string ResponseType, ExecutionTime ExecutionTime, int RouteSequenceNumber, string RouteStatus, RouteAcknowledgmentTimeStamp RouteAcknowledgmentTimeStamp, string ClientOrderID);
                public record ExecutionTime(string DateTimeString);
                public record RouteAcknowledgmentTimeStamp(string DateTimeString);
            }
            #endregion Execution Request Completed


            // =================================================================================================
            #region Order UROut Completed
            public class OrderUROutCompleted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }
                public record Base_Event(string EventType, OrderUROutCompletedEvent OrderUROutCompletedEvent);
                public record CancelQuantity(string lo, int signScale);
                public record ExecutionTimeStamp(string DateTimeString);
                public record LeavesQuantity();
                public record OrderUROutCompletedEvent(string EventType, string LegId, string ExecutionId, LeavesQuantity LeavesQuantity,
                              CancelQuantity CancelQuantity, string LegStatus, string LegSubStatus, string OutCancelType,
                              VenueExecutionTimeStamp VenueExecutionTimeStamp, ExecutionTimeStamp ExecutionTimeStamp, string RouteName);
                public record VenueExecutionTimeStamp(string DateTimeString);
            }
            #endregion Order UROut Completed


            // =================================================================================================
            #region Change Created
            public class ChangeCreated : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record AccountInfo(string AccountNumber, string AccountBranch, string CustomerOrFirmCode,
                              string OrderPlacementCustomerID, string AccountState, string AccountTypeCode);
                public record Ask(string lo, int signScale);
                public record AskSize(string lo);
                public record AssetOrderEquityOrderLeg(OrderInstruction OrderInstruction, CommissionInfo CommissionInfo, string AssetType,
                              string TimeInForce, string OrderTypeCode, List<OrderLeg> OrderLegs, string OrderCapacityCode,
                              string SettlementType, int Rule80ACode, string SolicitedCode, string TradeTag, EquityOrder EquityOrder);
                public record Base_Event(string EventType, ChangeCreatedEventEquityOrder ChangeCreatedEventEquityOrder);
                public record Bid(string lo, int signScale);
                public record BidSize(string lo);
                public record ChangeCreatedEventEquityOrder(string EventType, Order_ Order, string ParentSchwabOrderID,
                              string LifecycleSchwabOrderID);
                public record ClientChannelInfo(string ClientProductCode, string EventUserID, string EventUserType);
                public record CommissionInfo(EstimatedOrderQuantity EstimatedOrderQuantity,
                              EstimatedPrincipalAmount EstimatedPrincipalAmount, EstimatedCommissionAmount EstimatedCommissionAmount);
                public record EntryTimestamp(string DateTimeString);
                public record EquityOrder(string TradingSessionCodeOnOrder);
                public record EquityOrderInstruction();
                public record EquityOrderLeg();
                public record EstimatedCommissionAmount(int signScale);
                public record EstimatedNetAmount(string lo, int signScale);
                public record EstimatedOrderQuantity(string lo, int signScale);
                public record EstimatedPrincipalAmnt(string lo, int signScale);
                public record EstimatedPrincipalAmount(string lo, int signScale);
                public record ExecutionStrategy(string Type, LimitExecutionStrategy LimitExecutionStrategy);
                public record ExpiryTimeStamp(string DateTimeString);
                public record LeavesQuantity(string lo, int signScale);
                public record LegClientRequestInfo(string SecurityId, string SecurityIdTypeCd);
                public record LifecycleCreatedTimestamp(string DateTimeString);
                public record LimitExecutionStrategy(string Type, LimitPrice LimitPrice, string LimitPriceUnitCode);
                public record LimitPrice(string lo, int signScale);
                public record Mid(string lo, int signScale);
                public record Order_(string SchwabOrderID, string AccountNumber, Order_ Order, AccountInfo AccountInfo,
                              ClientChannelInfo ClientChannelInfo, LifecycleCreatedTimestamp LifecycleCreatedTimestamp,
                              string LifecycleSchwabOrderID, EntryTimestamp EntryTimestamp, ExpiryTimeStamp ExpiryTimeStamp,
                              bool AutoConfirm, PlanSubmitDate PlanSubmitDate, string SourceOMS, string FirmID, string OrderAccount,
                              AssetOrderEquityOrderLeg AssetOrderEquityOrderLeg);
                public record OrderInstruction(string HandlingInstructionCode, ExecutionStrategy ExecutionStrategy,
                              PreferredRoute PreferredRoute, EquityOrderInstruction EquityOrderInstruction);
                public record OrderLeg(string LegID, string LegParentSchwabOrderID, Quantity Quantity, string QuantityUnitCodeType,
                              LeavesQuantity LeavesQuantity, string BuySellCode, Security Security,
                              QuoteOnOrderAcceptance QuoteOnOrderAcceptance, LegClientRequestInfo LegClientRequestInfo,
                              string AccountingRuleCode, EstimatedNetAmount EstimatedNetAmount,
                              EstimatedPrincipalAmnt EstimatedPrincipalAmnt, EquityOrderLeg EquityOrderLeg);
                public record PlanSubmitDate(string DateTimeString);
                public record PreferredRoute();
                public record Quantity(string lo, int signScale);
                public record QuoteOnOrderAcceptance(Ask Ask, AskSize AskSize, Bid Bid, BidSize BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, Mid Mid, string SchwabOrderID);
                public record QuoteTimestamp(string DateTimeString);
                public record Security(string SchwabSecurityID, string Symbol, string UnderlyingSymbol, string PrimaryExchangeCode,
                              string MajorAssetType, string PrimaryMarketSymbol, string ShortDescriptionText, string ShortName, string CUSIP,
                              string SEDOL);
            }
            #endregion Change Created

            // =================================================================================================
            #region Change Accepted
            public class ChangeAccepted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Ask(string lo, int signScale);
                public record AskSize(string lo);
                public record Base_Event(string EventType, ChangeAcceptedEvent ChangeAcceptedEvent);
                public record Bid(string lo, int signScale);
                public record BidSize(string lo);
                public record ChangeAcceptedEvent(string EventType, CreatedTimeStamp CreatedTimeStamp, ExpiryTimeStamp ExpiryTimeStamp,
                              string TradingSessionCodeOnOrderEntry, List<QuoteOnOrderEntry> QuoteOnOrderEntry, string Status,
                              string LegStatus, List<LegInfoUpdate> LegInfoUpdate);
                public record CreatedTimeStamp(string DateTimeString);
                public record ExpiryTimeStamp(string DateTimeString);
                public record IntendedOrderQuantity(string lo, int signScale);
                public record LegInfoUpdate(string LegId, string AccountingRuleCode, IntendedOrderQuantity IntendedOrderQuantity,
                              string PreviousLegId);
                public record Mid(string lo, int signScale);
                public record QuoteOnOrderEntry(Ask Ask, AskSize AskSize, Bid Bid, BidSize BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, Mid Mid, string SchwabOrderID);
                public record QuoteTimestamp(string DateTimeString);
            }
            #endregion Change Accepted

        }
    }

}


