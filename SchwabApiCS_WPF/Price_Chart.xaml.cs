// <copyright file="Price_Chart.xaml.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using SchwabApiCS;
using System.Windows.Controls;
using ZpmPriceCharts;

namespace SchwabApiCS_WPF
{
    /// <summary>
    /// Interaction logic for PriceChart.xaml
    /// </summary>
    public partial class Price_Chart : UserControl
    {
        public SchwabApi schwabApi;
        CandleSet cs = new CandleSet();

        public Price_Chart()
        {
            InitializeComponent();

            PriceChart1.AddStudy(new Studies.SMA(50, System.Windows.Media.Brushes.Orange), true, false);
            PriceChart1.AddStudy(new Studies.RSI(14, 70, 30, System.Windows.Media.Brushes.Yellow, System.Windows.Media.Brushes.WhiteSmoke), false, false);

            PriceChart1.AddStudy(new Studies.ADX(14, System.Windows.Media.Brushes.Pink), false, false);
            PriceChart1.AddStudy(new Studies.ATR(14, System.Windows.Media.Brushes.LightBlue), false, false);
            PriceChart1.AddStudy(new Studies.EMA(50, System.Windows.Media.Brushes.OrangeRed), false, false);
            PriceChart1.AddStudy(new Studies.OBV(System.Windows.Media.Brushes.MediumPurple), false, false);
            PriceChart1.AddStudy(new Studies.RecentHighLow(System.Windows.Media.Brushes.White), false, false);


            PriceChart1.StudiesChanged();
        }

        private void Symbol_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
                Reload();
        }

        public string Symbol { 
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
            if (Symbol == "")
                return;

            if (Symbol != cs.Symbol) // get symbol description
            {
                try
                {
                    var quote = schwabApi.GetQuote(Symbol);
                    SymbolDescription = cs.Description = quote.reference.description;
                    cs.Symbol = Symbol;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                        SymbolDescription = $"Symbol {Symbol} not found";
                    else
                        SymbolDescription = $"Error: {ex.Message}";
                    return;
                }
            }
            cs.PrependCandles = PriceChart1.PrependCandlesNeeded;
            cs.StartTime = DateTime.Today.AddYears(-1);
            cs.EndTime = DateTime.Today.AddDays(1);
            cs.Decimals = 2; // hard coded for now - will be needed for forex

            SchwabApi.PriceHistory ph = null;
            try
            {
                switch ((string)((ComboBoxItem)cbxPeriod.SelectedItem).Content)
                {
                    case "Week":
                        cs.TimeFrame = CandleSet.TimeFrameClass.GetTimeFrameClass(CandleSet.TimeFrames.Week);
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.weekly,
                                            1, cs.TimeFrame.PrependStartTime(cs), cs.EndTime, false);
                        break;

                    case "Day":
                        cs.TimeFrame = CandleSet.TimeFrameClass.GetTimeFrameClass(CandleSet.TimeFrames.Day);
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.year, 1, SchwabApi.FrequencyType.daily,
                                            1, cs.TimeFrame.PrependStartTime(cs), cs.EndTime, false);
                        break;

                    case "5 Minutes":
                        cs.TimeFrame = CandleSet.TimeFrameClass.GetTimeFrameClass(CandleSet.TimeFrames.Minute5);
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.day, 5, SchwabApi.FrequencyType.minute,
                                            5, cs.TimeFrame.PrependStartTime(cs), cs.EndTime, true);
                        break;

                    case "15 Minutes":
                        cs.TimeFrame = CandleSet.TimeFrameClass.GetTimeFrameClass(CandleSet.TimeFrames.Minute15);
                        ph = schwabApi.GetPriceHistory(Symbol, SchwabApi.PeriodType.day, 5, SchwabApi.FrequencyType.minute,
                                            15, cs.TimeFrame.PrependStartTime(cs), cs.EndTime, true);
                        break;

                    default:
                        throw new Exception("Unsupported period.");
                }
                cs.Candles = ConvertSchwabToZpmCandles(ph.candles);
                cs.StartTimeIndex = cs.Candles.FindIndex(r => r.DateTime >= cs.StartTime);
                PriceChart1.Draw(ZpmPriceCharts.PriceChart.ChartType.CandleStick, cs);
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
            foreach(var s in schwabCandles)
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
    }
}
