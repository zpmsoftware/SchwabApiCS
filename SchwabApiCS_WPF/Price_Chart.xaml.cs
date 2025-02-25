// <copyright file="Price_Chart.xaml.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using SchwabApiCS;
using System.Windows;
using System.Windows.Controls;
using ZpmPriceCharts;
using static SchwabApiCS.SchwabApi;
using static ZpmPriceCharts.CandleSet;

namespace SchwabApiCS_WPF
{
    /// <summary>
    /// Interaction logic for PriceChart.xaml
    /// </summary>
    public partial class Price_Chart : UserControl
    {
        public static string DateFormat = "MM/dd/yyyy";  // default date format for use with price charts

        public SchwabApi schwabApi;
        CandleSet candelSet = new CandleSet();

        FrequencyTypes FrequencyType; // current value from popup, may ot may not be applied yes
        TimeFrames TimeFrame; // current value from popup, may ot may not be applied yet
        DateTime LastMarketDay = DateTime.Today;
        DateTime LastDayChecked = DateTime.Today.AddDays(-1); // Use yesterday to force check
        DateTime ToDate;
        DateTime FromDate;

        public Price_Chart()
        {
            InitializeComponent();

            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.SMA(20, System.Windows.Media.Brushes.Orange), true, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.RSI(14, 70, 30, System.Windows.Media.Brushes.Yellow, System.Windows.Media.Brushes.WhiteSmoke), false, false);

            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.ADX(14, System.Windows.Media.Brushes.Pink), false, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.ATR(14, System.Windows.Media.Brushes.LightBlue), false, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.EMA(20, System.Windows.Media.Brushes.OrangeRed), false, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.OBV(System.Windows.Media.Brushes.MediumPurple), false, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.RecentHighLow(System.Windows.Media.Brushes.White), false, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.PriceChannel(20, System.Windows.Media.Brushes.Orange, false), false, false);
            PriceChart1.AddStudy(new ZpmPriceCharts.Studies.PriceChannel(55,System.Windows.Media.Brushes.White, false), false, false);

            PriceChart1.StudiesChanged();
            TimeFrameFromDate.Text = DateTime.Today.AddYears(-1).AddDays(1).ToString(DateFormat);
            TimeFrameToDate.Text = DateTime.Today.ToString(DateFormat);

            ChartSettings(CandleSet.FrequencyTypes.Day, TimeFrames.Years1);
        }

        private void Symbol_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
                Reload();
        }

        public string Symbol
        {
            get { return tbSymbol.Text.Trim(); }
            set { tbSymbol.Text = value; }
        }

        public string Period
        {
            get { return (string)((ComboBoxItem)cbxPeriod.SelectedItem).Content; }
            set { ((ComboBoxItem)cbxPeriod.SelectedItem).Content = value; }
        }
        public string SymbolDescription
        {
            get { return (string)lblSymbolDescription.Content; }
            set { lblSymbolDescription.Content = value; }
        }

        public void Reload()
        {
            if (candelSet.FrequencyType == null)
                return; // not set yet.

            // check dates
            if (schwabApi != null && LastDayChecked != DateTime.Today)  // first time or today changed
            {
                for (LastMarketDay = DateTime.Today;
                    schwabApi.GetMarketHours(LastMarketDay).equity.sessionHours == null;
                    LastMarketDay = LastMarketDay.AddDays(-1)) ;
                LastDayChecked = DateTime.Today;
            }

            if (TimeFrame == TimeFrames.Custom)
            {
                candelSet.EndTime = ToDate == DateTime.Today ? LastMarketDay.AddDays(1) : (DateTime)ToDate; // need to specify tomorrow 12am to get today's values
                candelSet.StartTime = (DateTime)FromDate;
            }
            else if ((int)TimeFrame < (int)TimeFrames.Years1) // intraday
            {
                candelSet.EndTime = LastMarketDay.AddDays(1);  // need to specify tomorrow 12am to get today's values
                candelSet.StartTime = candelSet.EndTime.AddDays(-1 - (int)TimeFrame);
            }
            else if ((int)TimeFrame != (int)TimeFrames.Custom) // day, week, month
            {
                candelSet.EndTime = LastMarketDay.AddDays(1); // need to specify tomorrow 12am to get today's values
                candelSet.StartTime = candelSet.EndTime.AddYears(1000 - (int)TimeFrame);
            }

            var cbi = (ComboBoxItem)cbxPeriod.FindName(candelSet.FrequencyType.FrequencyTypeId.ToString());
            cbi.IsSelected = true;

            var text = (string)cbi.Content + " - ";
            if (TimeFrame == TimeFrames.Custom)
            {
                text += candelSet.StartTime.ToString(DateFormat) + " to " + candelSet.EndTime.ToString(DateFormat); ;
            }
            else if ((int)TimeFrame < (int)TimeFrames.Years1)
                text += (((int)TimeFrame).ToString() + " days").Replace("1 days", "1 day");
            else
                text += (((int)TimeFrame - 1000).ToString() + " years").Replace("1 years", "1 year");

            ChartSettingsButton.Text = text;

            if (Symbol == "")
                return;

            if (Symbol != candelSet.Symbol) // get symbol description
            {
                try
                {
                    var quote = schwabApi.GetQuotes(Symbol);
                    if (quote[0].invalidSymbols != null)
                    {
                        SymbolDescription = $"Symbol {Symbol} not found";
                        return;
                    }
                    SymbolDescription = candelSet.Description = quote[0].reference.description;
                    candelSet.Symbol = Symbol;
                    switch (quote[0].assetMainType)
                    {
                        case "FOREX":
                            SymbolDescription = $"Error: FOREX is not supported (yet)";
                            return;
                        case "FUTURE": candelSet.ExtendedHours = true; break;
                        default: candelSet.ExtendedHours = false; break;
                    }

                }
                catch (Exception ex)
                {
                    SymbolDescription = $"Error: {ex.Message}";
                    return;
                }
            }
            candelSet.PrependCandles = PriceChart1.PrependCandlesNeeded;
            candelSet.Decimals = 2; // hard coded for now - will be needed for forex

            SchwabApi.PriceHistory ph = null;
            try
            {
                switch (candelSet.FrequencyType.FrequencyTypeId)
                {
                    case FrequencyTypes.Month:
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.monthly,
                                            1, candelSet.FrequencyType.PrependStartTime(candelSet), candelSet.EndTime, candelSet.ExtendedHours);
                        break;

                    case FrequencyTypes.Week:
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.weekly,
                                            1, candelSet.FrequencyType.PrependStartTime(candelSet), candelSet.EndTime, candelSet.ExtendedHours);
                        break;

                    case FrequencyTypes.Day:
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.daily,
                                            1, candelSet.FrequencyType.PrependStartTime(candelSet), candelSet.EndTime, candelSet.ExtendedHours);
                        break;

                    case FrequencyTypes.Minute1:
                    case FrequencyTypes.Minute5:
                    case FrequencyTypes.Minute15:
                    case FrequencyTypes.Minute30:
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.day, 5, SchwabApi.FrequencyType.minute,
                                            (int)candelSet.FrequencyType.FrequencyTypeId, candelSet.FrequencyType.PrependStartTime(candelSet), candelSet.EndTime, true);
                        break;
                    case FrequencyTypes.Hour:
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.day, 5, SchwabApi.FrequencyType.minute,
                                            30, candelSet.FrequencyType.PrependStartTime(candelSet), candelSet.EndTime, true);
                        ph = new PriceHistory() // build hour candles from 30 min candles
                        {
                            symbol = ph.symbol,
                            previousCloseDate = ph.previousCloseDate,
                            previousClose = ph.previousClose,
                            candles = ph.HourCandles()
                        };
                        break;

                    default:
                        throw new Exception("Unsupported period.");
                }
                candelSet.LoadTime = DateTime.Now; // used to test when studies need to be recalulated.
                candelSet.Candles = ConvertSchwabToZpmCandles(ph.candles);
                candelSet.StartTimeIndex = candelSet.Candles.FindIndex(r => r.DateTime >= candelSet.StartTime);
                PriceChart1.Draw(ZpmPriceCharts.PriceChart.ChartType.CandleStick, candelSet, null);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("404"))
                    SymbolDescription = $"Symbol {Symbol} not found";
                else
                    SymbolDescription = $"Error: {ex.Message}";
            }
        }

        public List<ZpmPriceCharts.Candle> ConvertSchwabToZpmCandles(List<SchwabApi.Candle> schwabCandles)
        {
            var zCandles = new List<ZpmPriceCharts.Candle>();
            foreach (var s in schwabCandles)
                zCandles.Add(new ZpmPriceCharts.Candle(s.dateTime, s.open, s.high, s.low, s.close, s.volume));
            return zCandles;
        }

        private void Refresh_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Reload();
        }

        private void Period_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Reload();
        }


        /*
        public enum TimePeriods
        {
            Minute1 = 1, // 1 minute
            Minute5 = 5, // 5 minutes
            Minute15 = 15, // 15 minutes
            Minute30 = 30, // 30 minutes
            Minute60 = 60, // 1 hour
            Day = 101,
            Week = 107,
            Mmonth = 130
        }
        */

        public enum TimeFrames
        {
            Custom = 0,

            Days1 = 1,  // Day
            Days5 = 5,
            Days15 = 15,
            Days30 = 30,
            Days50 = 50,
            Days100 = 100,
            Days200 = 200,

            Years1 = 1001, // Year
            Years2 = 1002,
            Years3 = 1003,
            Years4 = 1004,
            Years5 = 1005,
            Years10 = 1010,
            Years20 = 1020
        };

        public void ChartSettings(CandleSet.FrequencyTypes frequencyType, TimeFrames timeFrame, DateTime? fromDate = null, DateTime? toDate = null)
        {
            RadioButton? rb = ((RadioButton?)TimeFramePopup.FindName(timeFrame.ToString()));
            candelSet.FrequencyType = CandleSet.FrequencyTypeClass.GetFrequencyTypeClass(frequencyType);
            TimeFrame = timeFrame;
            rb.IsChecked = true;
            if (TimeFrame == TimeFrames.Custom)
            {
                FromDate = (DateTime)fromDate;
                ToDate = (DateTime)toDate;
                TimeFrameFromDate.SelectedDate = FromDate;
                TimeFrameToDate.SelectedDate = ToDate;
            }
            Reload();
        }

        private void TimeFrameSettings_Apply(object sender, System.Windows.RoutedEventArgs e)
        {
            var frequencyType = (string)(((ComboBoxItem)cbxFrequencyType.SelectedItem).Name);
            DateTime? fromDate = null;
            DateTime? toDate = null;
            string timeFrame = "";

            var rb = GetSelectedTimeFrameRadioButton();
            timeFrame = (string)((RadioButton)rb).Name;
            TimeFramePopup.IsOpen = false;
            if (timeFrame != "")
            {
                var tf = GetTimeFrame(timeFrame);
                if (tf == TimeFrames.Custom)
                {
                    fromDate = TimeFrameFromDate.SelectedDate;
                    toDate = TimeFrameToDate.SelectedDate;
                    if (fromDate == null)
                    {
                        TimeFrameFromDate.Focus();
                        return;
                    }
                    if (toDate == null)
                    {
                        TimeFrameToDate.Focus();
                        return;
                    }
                    if (fromDate > toDate)
                    {
                        TimeFrameFromDate.Focus();
                        return;
                    }
                }
                ChartSettings(GetFrequencyType(frequencyType), tf, fromDate, toDate);
            }
        }


        /// <summary>
        /// Convert frequencyType string to frequencyType enum
        /// </summary>
        /// <param name="frequencyType"></param>
        /// <returns></returns>
        public static FrequencyTypes GetFrequencyType(string frequencyType)
        {
            return GetEnum<FrequencyTypes>(frequencyType);
        }

        /// <summary>
        /// Convert timeFrame string to timeFrame enum
        /// </summary>
        /// <param name="timeFrame"></param>
        /// <returns></returns>
        public static TimeFrames GetTimeFrame(string timeFrame)
        {
            return GetEnum<TimeFrames>(timeFrame);
        }

        /// <summary>
        /// Enum Convert
        /// </summary>
        /// <typeparam name="T">Enum type to convert to</typeparam>
        /// <param name="enumStringValue">string value to covert from</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static T GetEnum<T>(string enumStringValue)
        {
            foreach (var t in (T[])Enum.GetValues(typeof(T)))
            {
                if (t.ToString() == enumStringValue)
                    return t;
            }
            throw new Exception("Invalid " + typeof(T).Name + " type '" + enumStringValue + "'");
        }

        /// <summary>
        /// FrequencyType: minute, day, week, month
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxFrequencyType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FrequencyType = GetFrequencyType((string)(((ComboBoxItem)cbxFrequencyType.SelectedItem).Name));
            RadioButton? rb = GetSelectedTimeFrameRadioButton();
            if (rb != null)
            {
                TimeFrame = GetTimeFrame(rb.Name);

                if (FrequencyType < CandleSet.FrequencyTypes.Day) // intraday
                {
                    IntraDayTimeFrames.Visibility = Visibility.Visible;
                    InterDayTimeFrames.Visibility = Visibility.Collapsed;
                    rb = (RadioButton?)IntraDayTimeFrames.FindName(TimeFrame.ToString());
                    if (rb == null || rb.Name.StartsWith("Y"))
                    {
                        switch (FrequencyType) // set default fot frequency type
                        {
                            case CandleSet.FrequencyTypes.Minute1: rb = Days1; TimeFrame = TimeFrames.Days1; break;
                            case CandleSet.FrequencyTypes.Minute5: rb = Days5; TimeFrame = TimeFrames.Days5; break;
                            case CandleSet.FrequencyTypes.Minute15: rb = Days5; TimeFrame = TimeFrames.Days5; break;
                            case CandleSet.FrequencyTypes.Minute30: rb = Days5; TimeFrame = TimeFrames.Days5; break;
                            case CandleSet.FrequencyTypes.Hour: rb = Days5; TimeFrame = TimeFrames.Days5; break;
                            default:
                                throw new Exception("unrecognized Frequency type");
                        }
                    }
                }
                else
                {
                    IntraDayTimeFrames.Visibility = Visibility.Collapsed;
                    InterDayTimeFrames.Visibility = Visibility.Visible;
                    rb = (RadioButton?)InterDayTimeFrames.FindName(TimeFrame.ToString());
                    if (rb == null || rb.Name.StartsWith("D"))
                    {
                        switch (FrequencyType) // set default fot frequency type
                        {
                            case CandleSet.FrequencyTypes.Day: rb = Years1; TimeFrame = TimeFrames.Years1; break;
                            case CandleSet.FrequencyTypes.Week: rb = Years3; TimeFrame = TimeFrames.Years3; break;
                            case CandleSet.FrequencyTypes.Month: rb = Years10; TimeFrame = TimeFrames.Years10; break;
                            default:
                                throw new Exception("unrecognized Frequency type");
                        }
                    }
                }
                rb.IsChecked = true;
            }
        }

        public RadioButton? GetSelectedTimeFrameRadioButton()
        {
            RadioButton? rb = null;

            if (InterDayTimeFrames.IsVisible)
            {
                foreach (var r in InterDayTimeFrames.Children)
                {
                    if ((bool)((RadioButton)r).IsChecked)
                        return (RadioButton)r;
                }
            }
            else
            {
                foreach (var r in IntraDayTimeFrames.Children)
                {
                    if ((bool)((RadioButton)r).IsChecked)
                        return (RadioButton)r;
                }
            }
            if ((bool)Custom.IsChecked)
                return Custom;
            return null;
        }

        /// <summary>
        /// Keep TimeFramePopup aligned when parent window is moved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window w = Window.GetWindow(TimeFramePopup);
            if (null != w)
            {
                w.LocationChanged += delegate (object? sender2, EventArgs args)
                {
                    var offset = TimeFramePopup.HorizontalOffset;
                    // "bump" the offset to cause the popup to reposition itself
                    //   on its own
                    TimeFramePopup.HorizontalOffset = offset + 1;
                    TimeFramePopup.HorizontalOffset = offset;
                };
                // Also handle the window being resized (so the popup's position stays
                //  relative to its target element if the target element moves upon 
                //  window resize)
                w.SizeChanged += delegate (object sender3, SizeChangedEventArgs e2)
                {
                    var offset = TimeFramePopup.HorizontalOffset;
                    TimeFramePopup.HorizontalOffset = offset + 1;
                    TimeFramePopup.HorizontalOffset = offset;
                };
            }
        }
    }
}
