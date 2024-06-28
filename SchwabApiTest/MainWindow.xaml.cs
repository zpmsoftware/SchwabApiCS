// <copyright file="Mainwindow.xaml.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using System.Windows;
using Newtonsoft.Json;
using System.Data;
using SchwabApiCS;
using static SchwabApiCS.SchwabApi;
using System.Windows.Controls;
using static SchwabApiCS.Streamer;
using SchwabApiCS_Test;
using System.IO;

namespace SchwabApiTest
{
    /// <summary>
    /// Test for SchwabApiTest
    /// </summary>
    public partial class MainWindow : Window
    {
        private static SchwabApi schwabApi;
        private string tokenDataFileName = "";
        private SchwabTokens schwabTokens;
        private const string title = "SchwabApiCS - Schwab API Library Test";
        private Streamer streamer;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                Title = title;
                // modify tokenDataFileName to where your tokens are located
                var resourcesPath = System.IO.Directory.GetCurrentDirectory();
                var p = resourcesPath.IndexOf(@"\SchwabApiTest\");
                if (p != -1)
                    resourcesPath = resourcesPath.Substring(0, p + 15);

                tokenDataFileName = resourcesPath + "SchwabTokens.json"; // located in the project folder.
                schwabTokens = new SchwabTokens(tokenDataFileName); // gotta get the tokens First.
                if (schwabTokens.NeedsReAuthorization)
                {
                    SchwabApiCS_WPF.ApiAuthorize.Open(tokenDataFileName);
                    schwabTokens = new SchwabTokens(tokenDataFileName); // reload changes
                }
                AppStart();
            }
            catch (Exception ex)
            {
                var msg = SchwabApi.ExceptionMessage(ex);
                MessageBox.Show(msg.Message, msg.Title);
            }
        }

        const int FixedColumns = 5;
        IList<AccountInfo> accounts;
        List<Acct> accts;

        public class SymbolItem
        {
            public string Symbol;
            public int count;
            public decimal Quantity;
        }

        public class Acct
        {
            public string AccountNumber { get; set; }
            public string AccountName { get; set; }
            public decimal LiquidationValue { get; set; }
            public decimal CashBalance { get; set; }
            public decimal DayPL { get; set; }

            public decimal[] PositionPL { get; set; }

            public string CashBalanceDisplay
            {
                get
                {
                    if (LiquidationValue == 0)
                        return CashBalance.ToString("N2") + "     ";
                    return CashBalance.ToString("N2") + ((CashBalance / LiquidationValue) * 100).ToString("N0").PadLeft(4) + "%";
                }
            }
        }

        private void AppStart()
        {
            schwabApi = new SchwabApi(schwabTokens);

            // application code starts here =============================
            var t = Test();
            t.Wait();
            Title = title + ", version " + SchwabApi.Version.ToString();

            var symbols = new List<SymbolItem>(); // symbols list for accounts positions.

            foreach (var a in accounts)
            {
                foreach (var p in a.securitiesAccount.positions)
                {
                    var s = symbols.Where(r => r.Symbol == p.instrument.symbol).SingleOrDefault();
                    if (s == null)
                        symbols.Add(new SymbolItem() { Symbol = p.instrument.symbol, count = 1, Quantity = p.longQuantity > 0 ? p.longQuantity : -p.shortQuantity });
                    else
                    {
                        s.Quantity += p.longQuantity > 0 ? p.longQuantity : -p.shortQuantity;
                        s.count++;
                    }
                }
            }
            symbols = symbols.OrderByDescending(r => r.count).ThenBy(r => r.Symbol).ToList();
            accts = new List<Acct>();

            foreach (var a in accounts)
            {
                var acct = new Acct() {
                    AccountNumber = a.securitiesAccount.accountNumber,
                    AccountName = a.securitiesAccount.accountPreferences.nickName,
                    LiquidationValue = a.securitiesAccount.currentBalances.liquidationValue,
                    CashBalance = a.securitiesAccount.currentBalances.cashBalance,
                    PositionPL = new decimal[symbols.Count]
                };
                accts.Add(acct);
                foreach (var p in a.securitiesAccount.positions)
                {
                    var idx = symbols.FindIndex(r => r.Symbol == p.instrument.symbol);
                    if (idx >= 0)
                    {
                        acct.PositionPL[idx] = p.currentDayProfitLoss;
                        acct.DayPL += p.currentDayProfitLoss;
                    }
                }
            }

            while (AccountList.Columns.Count > FixedColumns) // drop and reload symbol column
                AccountList.Columns.RemoveAt(FixedColumns);

            for (var x = 0; x < symbols.Count; x++)
            {
                DataGridTextColumn textColumn = new DataGridTextColumn();
                textColumn.Binding = new System.Windows.Data.Binding("PositionPL[" + x.ToString() + "]") { StringFormat = "#.00;-.00; " };
                var s = new Style();
                s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                s.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 3, 30, 3)));
                textColumn.CellStyle = s;

                var tb = new TextBlock { Text = symbols[x].Symbol };
                textColumn.Header = tb;

                AccountList.Columns.Add(textColumn);
            }

            AccountList.ItemsSource = accts;
        }

        public void StreamerCallback(List<LevelOneEquity> levelOneEquities)
        {
            EquityList.Dispatcher.Invoke(() =>
            {
                EquityList.ItemsSource = null; // to force refresh
                EquityList.ItemsSource = levelOneEquities;
            });
        }

        public void OptionsStreamerCallback(List<LevelOneOption> levelOneOptions)
        {
            OptionsList.Dispatcher.Invoke(() =>
            {
                OptionsList.ItemsSource = null; // to force refresh
                OptionsList.ItemsSource = levelOneOptions.OrderBy(r=> r.Symbol).ToList();
            });
        }

        public void FuturesStreamerCallback(List<LevelOneFuture> levelOneOptions)
        {
        }

        public void AccountActivityStreamerCallback(List<AccountActivity> accountActivity)
        {
        }

        public async Task<string>Test()
        {
            try  // see SchwabApi.cs for list of methods
            {
                var accountHashes = schwabApi.GetAccountNumbers();  // Note: all methods will translate accountNumbers to accountHash as needed
                accounts = schwabApi.GetAccounts(true);
                var userPref = schwabApi.GetUserPreferences();

                var accountNumber = accounts[0].securitiesAccount.accountNumber; // account to use for testing.
                //var accountNumber = accounts.Where(r=> r.securitiesAccount.accountPreferences.nickName.Contains("401")).First().securitiesAccount.accountNumber; // account index to use for testing.

                streamer = new Streamer(schwabApi);
                streamer.LevelOneEquities.Request("SPY,IWM,GLD,NVDA", Streamer.LevelOneEquity.CommonFields, StreamerCallback);
                streamer.LevelOneEquities.Add("AAPL" );
                //streamer.LevelOneEquities.Remove("AAPL"); - works
                // streamer.LevelOneEquities.View(Streamer.LevelOneEquities.AllFields); -- not working yet

                var ocp = new OptionChainParameters
                {
                    contractType = SchwabApi.ContractType.ALL,
                    strike = 210
                };

                var aaplOptions = schwabApi.GetOptionChain("AAPL", ocp);
                var optionSymbols = string.Join(',', aaplOptions.calls.Select(a => a.symbol).ToArray());
                streamer.LevelOneOptions.Request(optionSymbols, LevelOneOption.CommonFields, OptionsStreamerCallback);

                streamer.AccountActivities.Request(AccountActivityStreamerCallback);

                // streamer.FuturesRequest("/ESN24", LevelOneOptions.CommonFields, FuturesStreamerCallback); - not available yet?  accepts call, no results



                var taskAppl = schwabApi.GetQuoteAsync("AAPL");
                taskAppl.Wait();
                SchwabApiCS.SchwabApi.Quote.QuotePrice applQuote = taskAppl.Result.Data.quote;
                QuoteTitle.Content = "Quote: AAPL";
                Quote.Text = JsonConvert.SerializeObject(applQuote, Formatting.Indented);
                TaskJson.Text = JsonConvert.SerializeObject(taskAppl, Formatting.Indented); // display in MainWindow


                // uncomment lines below for more testing
                /*
                var orderOCO = new SchwabApiCS.Order(Order.OrderType.LIMIT, Order.OrderStrategyTypes.TRIGGER, Order.Session.NORMAL,
                                                     Order.Duration.GOOD_TILL_CANCEL, 180M);
                orderOCO.Add(new Order.OrderLeg("AAPL", Order.AssetType.EQUITY, 1));
                var orderTriggersOCO = schwabApi.OrderTriggersOCOBracketAsync(accountNumber, orderOCO, 250M, 150M);
                orderTriggersOCO.Wait();

                var quotes = schwabApi.GetQuotes("IWM,SPY,USO", true, "quote");
                
                var account = schwabApi.GetAccount(accountNumber, true);
                var accountOrders = schwabApi.GetOrders(accountNumber, DateTime.Today, DateTime.Today.AddDays(1));

                var marketHours = schwabApi.GetMarketHours(DateTime.Today);
                var marketHoursTomorrow = schwabApi.GetMarketHours(DateTime.Today.AddDays(1));

                var instrument1 = schwabApi.GetInstrumentByCusipId("464287655");
                var instrument2 = schwabApi.GetInstrumentsBySymbol("IWM,SPY", SchwabApi.SearchBy.fundamental);

                // 2024-06-04 Api Support acknowledged there is odd stuff going on with results - not ready for prime time.
                var movers = schwabApi.GetMovers(SchwabApi.Indexes.SPX, SchwabApi.MoversSort.PERCENT_CHANGE_UP); 
               
                var aaplOptions = schwabApi.GetOptionChain("AAPL", SchwabApi.ContractType.ALL, 2);
                var aaplExpirations = schwabApi.GetOptionExpirationChain("AAPL");

                var p = new OptionChainParameters() { contractType = ContractType.CALL, strikeCount = 2, expMonth = OptionExpMonth.JUL };
                var aaplOptions2 = schwabApi.GetOptionChain("AAPL", p);


                // asynchronous requests:  blast 3 requests simultaneously ===============================
                // all the methods are available in async versions as well
                var taskAppl2 = schwabApi.GetQuoteAsync("AAPL");
                var taskQ2 = schwabApi.GetQuoteAsync("IWM", "quote");
                var taskT1 = schwabApi.GetAccountTransactionsAsync(accountNumber, DateTime.Today.AddMonths(-3),
                                                              DateTime.Now, SchwabApi.TransactionTypes.TRADE);
                Task.WaitAll(taskAppl2, taskQ2, taskT1); // wait for all 3 to complete
                var quote2 = taskQ2.Result.Data;
                var tranactions = taskT1.Result.Data;
                if (tranactions.Count > 0) // test getting transaction by id
                {
                    var trans = schwabApi.GetAccountTransaction(tranactions[0].accountNumber, tranactions[0].activityId);
                }

                // === ORDERS: uncomment ones you want to test.  Best to execute after hours, or use prices that won't fill. ============================
                var pQuote = schwabApi.GetQuote("GLD"); // change this symbol to one you have a postion in

                // place a OCO bracket order to close a GLD position
                //var ocoTask = schwabApi.OrderOCOBracketAsync(accountNumber, ocoQuote.symbol, Order.GetAssetType(ocoQuote.assetMainType), Order.Duration.DAY, Order.Session.NORMAL,
                //                                             -1, ocoQuote.quote.mark + 20, ocoQuote.quote.mark - 20);  // qty is negative to sell

                //var limitOrder = schwabApi.OrderLimit(accountNumber, ocoQuote.symbol, Order.GetAssetType(ocoQuote.assetMainType), Order.Duration.DAY,
                //                                       Order.Session.NORMAL, 1, ocoQuote.quote.mark - 20); // -20 shouldn't fill.

                //var marketOrder = schwabApi.OrderMarket(accountNumber, ocoQuote.symbol, Order.GetAssetType(ocoQuote.assetMainType), Order.Duration.DAY,
                //                                        Order.Session.NORMAL, 1);


                var stopLoss = schwabApi.OrderStopLoss(accountNumber, pQuote.symbol, Order.GetAssetType(pQuote.assetMainType), Order.Duration.GOOD_TILL_CANCEL,
                                                       Order.Session.NORMAL, -1, pQuote.quote.mark - 10);
                if (stopLoss != null)
                {
                    var result = schwabApi.OrderExecuteDelete(accountNumber, (long)stopLoss); // delete order just created
                }


                // OrderFirstTriggersSecond ==================================
                //  build first order
                var order1 = new Order(Order.OrderType.LIMIT, Order.OrderStrategyTypes.SINGLE, Order.Session.NORMAL, Order.Duration.DAY, pQuote.quote.mark-10);
                order1.Add(new Order.OrderLeg(pQuote.symbol, Order.GetAssetType(pQuote.assetMainType), 1));

                // build second order
                var order2 = new Order.ChildOrderStrategy(Order.OrderType.STOP, Order.OrderStrategyTypes.SINGLE, Order.Session.NORMAL, Order.Duration.DAY, pQuote.quote.mark - 20);
                order2.Add(new Order.OrderLeg(pQuote.symbol, Order.GetAssetType(pQuote.assetMainType), -1));

                // send the orders
                var orderTrigger = schwabApi.OrderTriggersSecond(accountNumber, order1, order2);
                if (orderTrigger != null)
                {
                    var order = schwabApi.GetOrder(accountNumber, (long)orderTrigger);

                    if (order.status != Order.Status.REJECTED.ToString())
                    {
                        var result = schwabApi.OrderExecuteDelete(accountNumber, (long)orderTrigger); // delete order just created
                    }
                }


                var aaplDayPrices = schwabApi.GetPriceHistory("AAPL", SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.daily,
                                                            1, null, null, false);
                var aaplDayPrices1 = schwabApi.GetPriceHistory("AAPL", SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.daily,
                                                            1, DateTime.Today.AddDays(-8), DateTime.Today.AddDays(1), false); // this picks up todays price
                var aapl15minPrices = schwabApi.GetPriceHistory("AAPL", SchwabApi.PeriodType.day, 1, SchwabApi.FrequencyType.minute,
                                                                15, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(1), true);


                var price2 = applQuote.mark - 50; // use price that won't fill
                var orderId = schwabApi.OrderSingle(accountNumber, "AAPL", Order.AssetType.EQUITY, Order.OrderType.LIMIT,
                                                  Order.Session.NORMAL, Order.Duration.GOOD_TILL_CANCEL, 1, price2); // this shouldn't fill
                // what does the json order just sent look like? - add a watch for "SchwabApi.LastOrderJson"

                if (orderId != null)
                {
                    var order = schwabApi.GetOrder(accountNumber, (long)orderId);
                    if (order.status != Order.Status.REJECTED.ToString())
                    {
                        var task = schwabApi.OrderExecuteDeleteAsync(accountNumber, (long)orderId); // delete order just created
                        task.Wait();
                        var schwabClientCorrelId = task.Result.SchwabClientCorrelId;  // this is Schwab's service reqest tracking GIUD
                    }
                }

                // exception handling
                // calling a non-async method will throw an error right away if request has errors.
                // An async method will throw an error when taskErr.Result.Data is accessed if taskErr.Result.HasError is true.
                //var throwsAnError = schwabApi.GetAccountTransactions("12345678", DateTime.Today.AddMonths(-3),
                //                                              DateTime.Now, SchwabApi.TransactionTypes.TRADE);
                /*
                var taskErr = schwabApi.GetAccountTransactionsAsync("12345678", DateTime.Today.AddMonths(-3),
                                                              DateTime.Now, SchwabApi.TransactionTypes.TRADE);
                taskErr.Wait();
                //var d2 = taskErr.Result.Data; // this will throw an error right away if taskErr.Result.HasError is true

                if (!taskErr.Result.HasError) // do this to stop a throw if not desired.
                {
                    var d = taskErr.Result.Data;  // safe to access data.
                } else
                { // will get there because acocunt# is bad.
                    var msg = taskErr.Result.Message;
                    var url = taskErr.Result.Url;
                }
                */

            }
            catch (Exception ex)
            {
                throw; // pass up to caller.  This catch is unnessary, but helpful.  Set a breakpoint here when debugging.
            }
            return "";
        }

        private void AccountList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}

