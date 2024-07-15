// <copyright file="AccountActivity.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using Newtonsoft.Json;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.AccountActivity;
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
                get
                {
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
                try
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
                            var data = ActivityObject.ConvertLoSignScaleToValue(aa.data).Replace("{}", "null");
                            try
                            {
                                aa.activityObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data, type, jsonSettings);
                            }
                            catch (Exception ex)
                            { // breakpoint here to catch if there are fields in the json string not in the c# class.
                                aa.activityObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data, type); // retry and ignore missing fields
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
                    }
                    else
                        Callback(Data); // callback to application with updated values
                }
                catch (Exception ex)
                {
                    throw;
                }
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

                /// <summary>
                /// convert lo & signScale to numeric value
                ///  "AveragePrice": { "lo": "5265000",  "signScale": 12 }  is converted to  "AveragePrice": 5.265
                ///  "AveragePrice": { "lo": "1"}  is converted to  "AveragePrice": 1
                ///  "AveragePrice": { "signScale": 12 }  is converted to  "AveragePrice": null
                ///  "AveragePrice": {}  is converted to  "AveragePrice": null
                ///  "AveragePrice": { "lo": "5",  "signScale": 4 }  is converted to  "AveragePrice": 0.05
                /// </summary>
                /// <param name="jsonString"></param>
                /// <returns>transformed jsonString</returns>
                public static string ConvertLoSignScaleToValue(string jsonString)
                {
                    for (int p = 0; (p = jsonString.IndexOf("\"signScale\":", p + 1)) != -1;)
                    {
                        var start = jsonString.LastIndexOf('{', p);
                        var len = jsonString.IndexOf('}', p) - start + 1;
                        var txt = jsonString.Substring(start, len);
                        var lo_start = txt.IndexOf("\"lo\":");
                        if (lo_start == -1)  // if "lo": not found, value is null
                        {
                            jsonString = jsonString.Substring(0, start) + "null" + jsonString.Substring(start + len);
                        }
                        else
                        {
                            lo_start += 5;
                            var lo_end = txt.IndexOf(",", lo_start);
                            var lo = "0000" + txt.Substring(lo_start, lo_end - lo_start).Trim(' ').Trim('"'); // leading zeros needed for values < 1.00
                            var signScale_start = txt.IndexOf("\"signScale\":") + 12;
                            var signScale = Convert.ToInt32(txt.Substring(signScale_start).TrimEnd('}').Trim(' '));

                            string value = lo.Insert(lo.Length - (int)(signScale / 2), ".").Trim('0').TrimEnd('.');
                            if (value[0] == '.')
                                value = "0" + value;

                            jsonString = jsonString.Substring(0, start) + value + jsonString.Substring(start + len);
                        }
                        p = start;
                    }
                    for (int p = 0; (p = jsonString.IndexOf("\"lo\":", p + 1)) != -1;) // handle cases with just "lo"
                    {
                        var start = jsonString.LastIndexOf('{', p);
                        var len = jsonString.IndexOf('}', p) + 1;
                        var lo = jsonString.Substring(p + 5, len - p - 6).Trim(' ').Trim('"');
                        if (lo[0] == '.')
                            lo = "0" + lo;
                        jsonString = jsonString.Substring(0, start) + lo + jsonString.Substring(len);
                        p = start;
                    }
                    return jsonString;
                }
            }


            // =================================================================================================
            #region Cancel Accepted
            public class CancelAccepted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, CancelAcceptedEvent CancelAcceptedEvent);
                public record CancelAcceptedEvent(string EventType, string LifecycleSchwabOrderID, PlanSubmitDate? PlanSubmitDate,
                              string ClientProductCode, bool AutoConfirm, CancelTimeStamp CancelTimeStamp,
                              List<LegCancelRequestInfoList> LegCancelRequestInfoList, string CancelRequestType);
                public record CancelAcceptedTime(string DateTimeString);
                public record CancelTimeStamp(string DateTimeString);

                public record LegCancelRequestInfoList(string LegID, int IntendedOrderQuantity, string? ChangedNewOrderID,
                              int RequestedAmount, string LegStatus, string LegSubStatus, string ChangedNewSchwabOrderId,
                              CancelAcceptedTime CancelAcceptedTime, string EventUserID);

                public record PlanSubmitDate(string DateTimeString);
            }

            #endregion Cancel Accepted


            // =================================================================================================
            #region Order Created
            public class OrderCreated : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record AccountInfo(string AccountNumber, string AccountBranch, string CustomerOrFirmCode,
                              string OrderPlacementCustomerID, string AccountState, string AccountTypeCode);
                public record AssetOrderEquityOrderLeg(OrderInstruction OrderInstruction, CommissionInfo CommissionInfo, string AssetType,
                              string TimeInForce, string OrderTypeCode, List<OrderLeg> OrderLegs, string OrderCapacityCode,
                              string SettlementType, int Rule80ACode, string SolicitedCode, string TradeTag, EquityOrder EquityOrder);
                public record Base_Event(string EventType, OrderCreatedEventEquityOrder OrderCreatedEventEquityOrder);
                public record ClientChannelInfo(string ClientProductCode, string EventUserID, string EventUserType);
                public record CommissionInfo(decimal EstimatedOrderQuantity, decimal EstimatedPrincipalAmount, decimal? EstimatedCommissionAmount);
                public record EntryTimestamp(string DateTimeString);
                public record EquityOptionsOrderLeg(string OpenClosePositionCode);
                public record EquityOrder(string TradingSessionCodeOnOrder);
                public record EquityOrderInstruction();
                public record EquityOrderLeg(EquityOptionsOrderLeg? EquityOptionsOrderLeg);
                public record ExecutionStrategy(string Type, LimitExecutionStrategy LimitExecutionStrategy);
                public record ExpiryTimeStamp(string DateTimeString);
                public record LegClientRequestInfo(string SecurityId, string SecurityIdTypeCd);
                public record LifecycleCreatedTimestamp(string DateTimeString);
                public record LimitExecutionStrategy(string Type, decimal LimitPrice, string LimitPriceUnitCode);
                public record OptionExpiryDate(string DateTimeString);
                public record OptionsQuote(string PutCallCode);
                public record OptionsSecurityInfo(string PutCallCode, string UnderlyingSchwabSecurityID, decimal StrikePrice,
                              OptionExpiryDate OptionExpiryDate);
                public record Order_(string SchwabOrderID, string AccountNumber, Order_ Order, AccountInfo AccountInfo,
                              ClientChannelInfo ClientChannelInfo, LifecycleCreatedTimestamp LifecycleCreatedTimestamp,
                              string LifecycleSchwabOrderID, EntryTimestamp EntryTimestamp, ExpiryTimeStamp ExpiryTimeStamp,
                              bool AutoConfirm, PlanSubmitDate PlanSubmitDate, string SourceOMS, string FirmID, string OrderAccount,
                              AssetOrderEquityOrderLeg AssetOrderEquityOrderLeg);
                public record OrderCreatedEventEquityOrder(string EventType, Order_ Order);
                public record OrderInstruction(string HandlingInstructionCode, ExecutionStrategy ExecutionStrategy,
                              PreferredRoute PreferredRoute, EquityOrderInstruction EquityOrderInstruction);
                public record OrderLeg(string LegID, string LegParentSchwabOrderID, decimal Quantity, string QuantityUnitCodeType,
                              decimal LeavesQuantity, string BuySellCode, Security Security, QuoteOnOrderAcceptance QuoteOnOrderAcceptance,
                              LegClientRequestInfo LegClientRequestInfo, string AccountingRuleCode, decimal EstimatedNetAmount,
                              decimal EstimatedPrincipalAmnt, EquityOrderLeg EquityOrderLeg);
                public record PlanSubmitDate(string DateTimeString);
                public record PreferredRoute();
                public record QuoteOnOrderAcceptance(decimal Ask, string? AskSize, decimal Bid, string? BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, decimal Mid, string SchwabOrderID, OptionsQuote? OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
                public record Security(string SchwabSecurityID, string Symbol, string UnderlyingSymbol, string? PrimaryExchangeCode,
                              string MajorAssetType, string PrimaryMarketSymbol, string ShortDescriptionText, string ShortName, string CUSIP,
                              string? SEDOL, string? ISIN, OptionsSecurityInfo? OptionsSecurityInfo);

                public decimal? LimitPriceValue
                {
                    get
                    {
                        try
                        {
                            return BaseEvent.OrderCreatedEventEquityOrder.Order.Order.AssetOrderEquityOrderLeg.OrderInstruction.ExecutionStrategy.LimitExecutionStrategy.LimitPrice;
                        }
                        catch { return null; }
                    }
                }

                /// <summary>
                /// Quantity per order leg
                /// </summary>
                /// <param name="index">Leg index</param>
                /// <returns>Quantity for leg or null if index is invalid</returns>
                public decimal? LegQuantity(int index)
                {
                    try
                    {
                        if (index >= BaseEvent.OrderCreatedEventEquityOrder.Order.Order.AssetOrderEquityOrderLeg.OrderLegs.Count)
                            return null;
                        return BaseEvent.OrderCreatedEventEquityOrder.Order.Order.AssetOrderEquityOrderLeg.OrderLegs[index].Quantity;
                    }
                    catch { return null; }
                }

                /// <summary>
                /// LegLeavesQuantity per order leg
                /// </summary>
                /// <param name="index">Leg index</param>
                /// <returns>LeavesQuantity for leg or null if index is invalid</returns>
                public decimal? LegLeavesQuantity(int index)
                {
                    try
                    {
                        if (index >= BaseEvent.OrderCreatedEventEquityOrder.Order.Order.AssetOrderEquityOrderLeg.OrderLegs.Count)
                            return null;
                        return BaseEvent.OrderCreatedEventEquityOrder.Order.Order.AssetOrderEquityOrderLeg.OrderLegs[index].LeavesQuantity;
                    }
                    catch { return null; }

                }
            }

            #endregion Order Created


            // =================================================================================================
            #region Order Accepted
            public class OrderAccepted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, OrderAcceptedEvent OrderAcceptedEvent);
                public record CreatedTimeStamp(string DateTimeString);
                public record ExpiryTimeStamp(string DateTimeString);
                public record OrderAcceptedEvent(string EventType, CreatedTimeStamp CreatedTimeStamp, ExpiryTimeStamp ExpiryTimeStamp,
                              string Status, string TradingSessionCodeOnOrderEntry, List<QuoteOnOrderEntry> QuoteOnOrderEntry);
                public record OptionsQuote(string PutCallCode);
                public record QuoteOnOrderEntry(decimal Ask, string? AskSize, decimal Bid, string? BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, decimal Mid, string SchwabOrderID, OptionsQuote OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
            }
            #endregion Order Accepted


            // =================================================================================================
            #region Execution Requested
            public class ExecutionRequested : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, ExecutionRequestedEventRoutedInfo ExecutionRequestedEventRoutedInfo);
                public record ExecutionRequestedEventRoutedInfo(string EventType, int RouteSequenceNumber, RouteInfo RouteInfo,
                              string RouteRequestedBy, string LegId);
                public record OptionsQuote(string PutCallCode);
                public record Quote(decimal Ask, string? AskSize, decimal Bid, string? BidSize, QuoteTimestamp QuoteTimestamp, string Symbol,
                              string QuoteTypeCode, decimal Mid, string SchwabOrderID, OptionsQuote OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
                public record RoutedExecutionTimestamp(string DateTimeString);
                public record RoutedTime(string DateTimeString);
                public record RouteInfo(string RouteName, int RouteSequenceNumber, RoutedExecutionTimestamp RoutedExecutionTimestamp,
                              Quote Quote, string RouteRequestedType, int RoutedQuantity, decimal RoutedPrice, string RouteStatus,
                              string ClientOrderID, RoutedTime RoutedTime, string RouteTimeInForce, object RouteAcknowledgmentTimeStamp);
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
                public record ExecutionInfo(int ExecutionSequenceNumber, string ExecutionId, int ExecutionQuantity,
                              ExecutionTimeStamp ExecutionTimeStamp, string ExecutionTransType, string ExecutionCapacityCode,
                              string RouteName, int RouteSequenceNumber, VenuExecutionTimeStamp VenuExecutionTimeStamp, string CancelType,
                              string ReportingCapacityCode, object AsOfTimeStamp, string ClientOrderID);
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
                public record ExecutionRequestCompletedEvent(string EventType, string LegId, string ResponseType, string ExchangeOrderID,
                              ExecutionTime ExecutionTime, int RouteSequenceNumber, string RouteStatus,
                              RouteAcknowledgmentTimeStamp RouteAcknowledgmentTimeStamp, string ClientOrderID);
                public record ExecutionTime(string DateTimeString);
                public record RouteAcknowledgmentTimeStamp(string DateTimeString);
            }
            #endregion Execution Request Completed


            // =================================================================================================
            #region Order UROut Completed
            public class OrderUROutCompleted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, OrderUROutCompletedEvent_ OrderUROutCompletedEvent);
                public record ExecutionTimeStamp(string DateTimeString);
                //public record LeavesQuantity();
                public record OrderUROutCompletedEvent_(string EventType, string LegId, string ExecutionId, decimal? LeavesQuantity,
                              int CancelQuantity, string LegStatus, string LegSubStatus, string OutCancelType,
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
                public record AssetOrderEquityOrderLeg(OrderInstruction OrderInstruction, CommissionInfo CommissionInfo, string AssetType,
                              string TimeInForce, string OrderTypeCode, List<OrderLeg> OrderLegs, string OrderCapacityCode,
                              string SettlementType, int Rule80ACode, string SolicitedCode, string TradeTag, EquityOrder EquityOrder);
                public record Base_Event(string EventType, ChangeCreatedEventEquityOrder ChangeCreatedEventEquityOrder);
                public record ChangeCreatedEventEquityOrder(string EventType, Order_ Order, string ParentSchwabOrderID,
                              string LifecycleSchwabOrderID);
                public record ClientChannelInfo(string ClientProductCode, string EventUserID, string EventUserType);
                public record CommissionInfo(int EstimatedOrderQuantity, decimal EstimatedPrincipalAmount, decimal? EstimatedCommissionAmount);
                public record EntryTimestamp(string DateTimeString);
                public record EquityOrder(string TradingSessionCodeOnOrder);
                public record ExecutionStrategy(string Type, LimitExecutionStrategy LimitExecutionStrategy);
                public record ExpiryTimeStamp(string DateTimeString);
                public record LegClientRequestInfo(string SecurityId, string SecurityIdTypeCd);
                public record LifecycleCreatedTimestamp(string DateTimeString);
                public record LimitExecutionStrategy(string Type, decimal LimitPrice, string LimitPriceUnitCode);
                public record OptionsQuote(string PutCallCode);
                public record OptionsSecurityInfo(string PutCallCode, string UnderlyingSchwabSecurityID, decimal StrikePrice,
                              OptionExpiryDate OptionExpiryDate);
                public record Order_(string SchwabOrderID, string AccountNumber, Order_ Order, AccountInfo AccountInfo,
                              ClientChannelInfo ClientChannelInfo, LifecycleCreatedTimestamp LifecycleCreatedTimestamp,
                              string LifecycleSchwabOrderID, EntryTimestamp EntryTimestamp, ExpiryTimeStamp ExpiryTimeStamp,
                              bool AutoConfirm, PlanSubmitDate PlanSubmitDate, string SourceOMS, string FirmID, string OrderAccount,
                              AssetOrderEquityOrderLeg AssetOrderEquityOrderLeg);
                public record OrderInstruction(string HandlingInstructionCode, ExecutionStrategy ExecutionStrategy, object PreferredRoute,
                              object EquityOrderInstruction);
                public record OrderLeg(string LegID, string LegParentSchwabOrderID, int Quantity, string QuantityUnitCodeType,
                              int LeavesQuantity, string BuySellCode, Security Security, QuoteOnOrderAcceptance QuoteOnOrderAcceptance,
                              LegClientRequestInfo LegClientRequestInfo, string AccountingRuleCode, decimal EstimatedNetAmount,
                              decimal EstimatedPrincipalAmnt, object EquityOrderLeg);
                public record PlanSubmitDate(string DateTimeString);
                public record QuoteOnOrderAcceptance(decimal Ask, string? AskSize, decimal Bid, string? BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, decimal Mid, string SchwabOrderID, OptionsQuote? OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
                public record Security(string SchwabSecurityID, string Symbol, string UnderlyingSymbol, string PrimaryExchangeCode,
                              string MajorAssetType, string PrimaryMarketSymbol, string ShortDescriptionText, string ShortName, string CUSIP,
                              string SEDOL, string ISIN, OptionsSecurityInfo? OptionsSecurityInfo);
            }
            #endregion Change Created

            // =================================================================================================
            #region Change Accepted
            public class ChangeAccepted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record Base_Event(string EventType, ChangeAcceptedEvent ChangeAcceptedEvent);
                public record ChangeAcceptedEvent(string EventType, CreatedTimeStamp CreatedTimeStamp, ExpiryTimeStamp ExpiryTimeStamp,
                              string TradingSessionCodeOnOrderEntry, List<QuoteOnOrderEntry> QuoteOnOrderEntry, string Status,
                              string LegStatus, List<LegInfoUpdate> LegInfoUpdate);
                public record CreatedTimeStamp(string DateTimeString);
                public record ExpiryTimeStamp(string DateTimeString);
                public record LegInfoUpdate(string LegId, string AccountingRuleCode, int IntendedOrderQuantity, string PreviousLegId);
                public record OptionsQuote(string PutCallCode);
                public record QuoteOnOrderEntry(decimal Ask, string? AskSize, decimal Bid, string? BidSize, QuoteTimestamp QuoteTimestamp,
                              string Symbol, string QuoteTypeCode, decimal Mid, string SchwabOrderID, OptionsQuote? OptionsQuote);
                public record QuoteTimestamp(string DateTimeString);
            }
            #endregion Change Accepted

            // =================================================================================================
            #region Order Fill Completed
            public class OrderFillCompleted : ActivityObject
            {
                public Base_Event BaseEvent { get; set; }

                public record ActualChargedFeesCommissionAndTax(object StateTaxWithholding, object FederalTaxWithholding, object SECFees,
                              object ORF, object FTT, object TaxWithholding1446, object GoodsAndServicesTax, object IOF, object TAF,
                              object CommissionAmount);
                public record Base_Event(string EventType,
                             OrderFillCompletedEventOrderLegQuantityInfo OrderFillCompletedEventOrderLegQuantityInfo);
                public record ExecutionInfo(int ExecutionSequenceNumber, string ExecutionId, string VenueExecutionID, int ExecutionQuantity,
                              decimal ExecutionPrice, ExecutionTimeStamp ExecutionTimeStamp, string ExecutionTransType,
                              string ExecutionBroker, string ExecutionCapacityCode, string RouteName, int RouteSequenceNumber,
                              VenuExecutionTimeStamp VenuExecutionTimeStamp, string ReportingCapacityCode,
                              object ActualChargedCommissionAmount, object AsOfTimeStamp,
                              ActualChargedFeesCommissionAndTax ActualChargedFeesCommissionAndTax, string ClientOrderID);
                public record ExecutionTimeStamp(string DateTimeString);
                public record OrderFillCompletedEventOrderLegQuantityInfo(string EventType, string LegId, string LegStatus,
                              QuantityInfo QuantityInfo, string LegSubStatus, ExecutionInfo ExecutionInfo,
                              OrderInfoForTransactionPosting OrderInfoForTransactionPosting);
                public record OrderInfoForTransactionPosting(decimal LimitPrice, string OrderTypeCode, string BuySellCode, int Quantity,
                              object StopPrice, string Symbol, string SchwabSecurityID, string SolicitedCode, string AccountingRuleCode,
                              string SettlementType, string OrderCreatedUserID, string OrderCreatedUserType, string ClientProductCode);
                public record QuantityInfo(string ExecutionID, int CumulativeQuantity, object LeavesQuantity, decimal AveragePrice);
                public record VenuExecutionTimeStamp(string DateTimeString);
            }
            #endregion Order Fill Completed
        }
    }

}


