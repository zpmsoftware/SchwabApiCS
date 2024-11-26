// <copyright file="PriceChart.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ZpmPriceCharts
{
    /// <summary>
    /// Interaction logic for PriceChartControl.xaml
    /// </summary>
    public partial class PriceChart : UserControl
    {
        public CandleSet Cset { get; set; }
        public string BackgroundColor { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }

        private int _startCandle;
        public int StartCandle { 
            get { return _startCandle; } 
            set { _startCandle = Math.Max(0, value); }
        }
        public int NbrCandles { get; set; }  // candles that can be displayed
        public double CandleWidth = 9;  // width of each candle.  Adjustable from 5 to 19
        public double PriceFactor { get; set; } // number of pixels per $

        const double borderchartBorder = 1; // allow for chart border thickness
        const int MaxPriceLabels = 15;

        //public TimeSpan Period { get; set; }
        private double TopPrice { get; set; }
        private double BottomPrice { get; set; }
        private string PriceFormat;
        public List<ChartStudy> ChartStudies { get; protected set; } = new List<ChartStudy>();
        private Brush txtColor = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160));
        private Brush txtColor2 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
        private Brush GreenColor = new SolidColorBrush(Color.FromArgb(255, 0, 197, 49));
        private Brush RedColor = new SolidColorBrush(Color.FromArgb(255, 255, 95, 95));
        private Brush GridColor = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        private Brush GreenCandleBody = new SolidColorBrush(Color.FromArgb(255, 26, 26, 26));


        private DoubleCollection dashes = new DoubleCollection() { 1, 5 };
        private Line MouseX;  // cross hairs for mouse position
        private Line MouseY;  // cross hairs for mouse position
        private TextBlock MousePrice;
        Border MousePriceBox;
        private TextBlock MouseDate;
        Border MouseDateBox;
        private int Decimals = 2;
        private ChartType Chart_Type;
        private Line[] HeadingStudyLine;
        private TextBlock[] HeadingStudyTb;
        private string[] StudyDescription = new string[4];
        private double LastChartAreaHeight = 0;
        private double HScrollRatio = 0;
        public static BitmapImage CircleX;

        public string DecimalFormat = "N2";

        public List<Candle> Candles { get; protected set; }  // candles, with leading prepend candles removed.

        public enum ChartType
        {
            CandleStick  // only one so far
        }

        public PriceChart()
        {
            InitializeComponent();

            CircleX = new BitmapImage(new Uri("Creek.jpg", UriKind.Relative));

            Cset = null;
            BackgroundColor = "Black";
            StartCandle = 0;
            PriceFormat = "G";

            HeadingStudyLine = new Line[4] { lnStudy1, lnStudy2, lnStudy3, lnStudy4 };
            HeadingStudyTb = new TextBlock[4] { tbStudy1, tbStudy2, tbStudy3, tbStudy4 };
            //StudiesChanged();

            MousePriceBox = new Border() { CornerRadius = new CornerRadius(5), Background = System.Windows.Media.Brushes.LightGray, Visibility = Visibility.Hidden, Margin = new Thickness(3, 0, 0, 0) };
            MousePrice = new TextBlock() { Text = "99.99", Foreground = System.Windows.Media.Brushes.Black, Padding = new Thickness(0, 0, 5, 0) };
            MousePriceBox.Child = MousePrice;

            MouseDateBox = new Border() { CornerRadius = new CornerRadius(4), Background = System.Windows.Media.Brushes.LightGray, Visibility = Visibility.Hidden, Margin = new Thickness(0, 3, 0, 0) };
            MouseDate = new TextBlock() { Text = "12/30/2017", FontSize=12, Foreground = System.Windows.Media.Brushes.Black, Margin = new Thickness(3, 0, 3, 1) };  //Padding = new Thickness(5, 0, 5, 0)
            MouseDateBox.Child = MouseDate;
            System.Windows.Controls.Canvas.SetTop(MouseDate, 10);

            // do this last so it's on top
            MouseX = ChartLine(GridColor);
            MouseY = ChartLine(GridColor);
        }

        public int PrependCandlesNeeded 
        {  
            get
            {
                return ChartStudies.Max(r => r.Study.PrependCandlesNeeded);
            }
        }

        public class ChartStudy
        {
            public ChartStudy(Studies.Study s, bool show, bool controlsLaxis)
            {
                Study = s;
                Show = show;
                ControlsLaxis = controlsLaxis;
                Name = s.GetType().Name;
                ButtonText = s.StudyDescription();
                StudyToolTip = s.StudyToolTip();
            }
            public ChartStudy(Studies.Study s, string name, string buttonText, bool show, bool controlsLaxis)
            {
                Study = s;
                Show = show;
                ControlsLaxis = controlsLaxis;
                Name = name;
                ButtonText = buttonText;
            }

            public Studies.Study Study {  get; set; }
            public string Name {  get; set; } // UIElement Name=
            public string ButtonText { get; set; }  // text to show in the button
            public string StudyToolTip { get; set; }
            public bool Show { get; set; }
            public bool ControlsLaxis { get; set; }
        }

        public void StudiesChanged()
        {
            StudyButtons.Children.Clear();
            SelectedStudiesChanged();
            foreach (var s in ChartStudies)
                StudyButtons.Children.Add(StudyToggleButton(s));
        }

        public void SelectedStudiesChanged()
        {
            int sx = 0;
            for (int x = 0; x < ChartStudies.Count && sx < HeadingStudyTb.Length; x++)
            {
                if (x < ChartStudies.Count && ChartStudies[x].Show)
                {
                    StudyDescription[sx] = ChartStudies[x].Study.StudyDescription() + ": ";
                    HeadingStudyTb[sx].Foreground = ChartStudies[x].Study.Color;
                    HeadingStudyLine[sx].Visibility = Visibility.Visible;
                    HeadingStudyTb[sx].Visibility = Visibility.Visible;
                    sx++;
                }
            }
            while (sx < HeadingStudyTb.Length)
            {
                StudyDescription[sx] = "";
                HeadingStudyLine[sx].Visibility = Visibility.Hidden;
                HeadingStudyTb[sx].Visibility = Visibility.Hidden;
                sx++;
            }
        }

        private System.Windows.Controls.Primitives.ToggleButton StudyToggleButton(ChartStudy cs)
        {
            var tb = new TextBlock()
            {
                Text = cs.ButtonText,
                TextAlignment = TextAlignment.Center,
                ToolTip = cs.StudyToolTip
            };

            var formattedText = new FormattedText(
                cs.ButtonText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(tb.FontFamily, tb.FontStyle, FontWeights.Bold, tb.FontStretch),
                tb.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                VisualTreeHelper.GetDpi(tb).PixelsPerDip);

            tb.Width = formattedText.Width + 12;

            var tbutton = new System.Windows.Controls.Primitives.ToggleButton()
            {
                Name = cs.Name,
                Content = tb,
                IsChecked = cs.Show,
                FontSize = 12,
                Height = 24,
                Tag = cs,
                
            };
            if (cs.Show)
                tbutton.Background = cs.Study.Color;

            tbutton.Checked += StudyToggleButton_CheckedChanged;
            tbutton.Unchecked += StudyToggleButton_CheckedChanged;

            return tbutton;
        }

        /// <summary>
        /// show/hide study button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StudyToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var tbutton = (System.Windows.Controls.Primitives.ToggleButton)sender;
            var cs = (ChartStudy)tbutton.Tag;
            cs.Show = (bool)tbutton.IsChecked;

            if (cs.Show)
            {
                cs.Study.Draw(this);
                tbutton.Background = cs.Study.Color;
            }
            else
            {
                tbutton.Background = new SolidColorBrush(Colors.Transparent);
                if (cs.Study.UiElements.Count > 0)
                {
                    var start = ChartArea.Children.IndexOf(cs.Study.UiElements[0]);
                    if (start != -1)
                        ChartArea.Children.RemoveRange(start, cs.Study.UiElements.Count);
                    cs.Study.UiElements.Clear();
                }
            }
            SelectedStudiesChanged();
            UpdateChartArea();
        }

        public void ClearStudies()
        {
            ChartStudies.Clear();
        }

        public void AddStudy(Studies.Study s, bool show, bool controlsLaxis)
        {
            ChartStudies.Add( new ChartStudy(s, show, controlsLaxis));
        }

        public void SetStartCandle(int idx)
        {
            StartCandle = idx;
            if (idx > 0)
            {
                if (StartCandle + NbrCandles > Candles.Count)
                    StartCandle = Candles.Count - NbrCandles;
            }
        }

        /// <summary>
        /// Draw price chart
        /// </summary>
        public void Draw(ChartType chartType, CandleSet candleSet)
        {
            if (ChartAreaActualHeight() == 0 || candleSet == null)
                return;
            Chart_Type = chartType;
            Cset = candleSet;
            Symbol = candleSet.Symbol;
            tbSymbol.Text = Symbol;
            Description = candleSet.Description;
            Decimals = candleSet.Decimals;
            DecimalFormat = "N" + Decimals.ToString();

            NbrCandles = Math.Max(2, (int)(ChartArea.ActualWidth / CandleWidth));
            foreach (var s in ChartStudies)
                s.Study.Caclulate(candleSet);

            Candles = Cset.Candles.Where(r => r.DateTime >= Cset.StartTime).ToList();

            SetStartCandle(Candles.Count - NbrCandles);
            ReDraw();
        }

        private SolidColorBrush shadeColor = null;
        private SolidColorBrush shadeTextColor = null;

        // Drawing Groups, in priority order. Collection of UI elements in each group
        protected List<System.Windows.UIElement> AfterHoursShadingElements;
        protected List<System.Windows.UIElement> VolumeElements;
        protected List<System.Windows.UIElement> GridLinesElements;
        protected List<System.Windows.UIElement> CandleElements;
        protected List<System.Windows.UIElement> LaxisElements;

        private double MaxVolume = 0;

        public void ReDraw() 
        {
            DrawAfterHoursShading();
            DrawRaxisAndGridLines();  // draw Right axis (Prices)
            DrawLaxis(); // for studies
            DrawXaxis(); // Dates
            DrawVolume();
            DrawCandles();

            foreach (var cs in ChartStudies)
            {
                if (cs.Show)
                    cs.Study.Draw(this);
            }

            UpdateChartArea();
        }

        private void UpdateChartArea()
        {
            if (CandleElements == null) // candles not loaded yet
                return;

            // last item that is drawn is on top.
            ChartArea.Children.Clear();

            foreach (var e in AfterHoursShadingElements)
                ChartArea.Children.Add(e);
            foreach (var e in VolumeElements)
                ChartArea.Children.Add(e);
            foreach (var e in GridLinesElements)
                ChartArea.Children.Add(e);

            foreach (var cs in ChartStudies)
            {
                if (cs.Show)
                {
                    foreach (var e in cs.Study.UiElements)
                        ChartArea.Children.Add(e);
                }
            }

            ChartArea.Children.Add(MouseX);
            ChartArea.Children.Add(MouseY);

            foreach (var e in CandleElements)
                ChartArea.Children.Add(e);

            DrawHeading(StartCandle + NbrCandles - 1);

            hScrollBar.Maximum = Candles.Count - NbrCandles;
            hScrollBar.Minimum = 0;
            hScrollBar.Value = StartCandle;
            hScrollBar.LargeChange = NbrCandles - 1;
            hScrollBar.ViewportSize = NbrCandles;

        }

        private void DrawAfterHoursShading()
        {
            AfterHoursShadingElements = new List<System.Windows.UIElement>();
            if ((int)Cset.TimeFrame.TimeFrameId < 1000)
            { // shading for after hours times
                var sCandle = 0;
                var isAfterHours = Cset.TimeFrame.IsAfterHours(Candles[StartCandle].DateTime);
                int x;
                for (x = 1; x < NbrCandles && x + StartCandle < Candles.Count; x++)
                {
                    if (isAfterHours != Cset.TimeFrame.IsAfterHours(Candles[x + StartCandle].DateTime))
                    {
                        HoursShading(isAfterHours, sCandle, x, false);
                        sCandle = x;
                        isAfterHours = !isAfterHours;
                    }
                }
                HoursShading(isAfterHours, sCandle, x, true);
            }
        }

        public void HoursShading(bool isAfterHours, int sCandle, int x, bool last)
        {
            if (shadeColor == null)
            {
                shadeColor = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
                shadeTextColor = new SolidColorBrush(Color.FromArgb(100, 200, 200, 200));
            }

            if (isAfterHours)
            {
                Rectangle body = new Rectangle()
                {
                    Height = ChartAreaActualHeight(),
                    Width = last ? ChartArea.ActualWidth - CandleWidth * sCandle : CandleWidth * (x - sCandle),
                    Fill = shadeColor
                };
                System.Windows.Controls.Canvas.SetTop(body, 0);
                System.Windows.Controls.Canvas.SetLeft(body, sCandle * CandleWidth);
                AfterHoursShadingElements.Add(body);
            }
            else if (x - sCandle > 5)
            {
                var txt = new TextBlock()
                {
                    Text = Candles[x + StartCandle - 1].DateTime.ToString("ddd MM/dd/yyyy"),
                    Width = (x - sCandle) * CandleWidth,
                    TextAlignment = TextAlignment.Center,
                    Foreground = shadeTextColor,
                    FontSize = 18
                };
                System.Windows.Controls.Canvas.SetTop(txt, 5);
                System.Windows.Controls.Canvas.SetLeft(txt, sCandle * CandleWidth);
                AfterHoursShadingElements.Add(txt);
            }
        }

        private void DrawVolume()
        {
            VolumeElements = new List<System.Windows.UIElement>();

            double volumeHeight = Math.Floor(ChartAreaActualHeight() / 4);
            Brush volumeColor = new SolidColorBrush(Color.FromArgb(128, 90, 90, 255));
            for (int x = 0; x < NbrCandles && x + StartCandle < Candles.Count; x++)
            {
                Rectangle body = new Rectangle()
                {
                    Height = volumeHeight * (Candles[StartCandle + x].Volume / MaxVolume),
                    Width = CandleWidth - 4,
                    Fill = volumeColor
                };
                var top = ChartAreaActualHeight() - body.Height;
                System.Windows.Controls.Canvas.SetTop(body, top);
                System.Windows.Controls.Canvas.SetLeft(body, x * CandleWidth + 2);
                VolumeElements.Add(body);
            }
        }

        private void DrawLaxis()
        {
            // draw Left axis (0-100 for studies)
            Laxis.Children.Clear();
            for (var x = 0; x <= 100; x += 10)
            {
                var y = LAxisPosition(x, 100) + 18;

                var txt = new TextBlock() { Text = x.ToString(), Width = Laxis.ActualWidth - 12, TextAlignment = TextAlignment.Right };
                txt.Foreground = txtColor;
                Laxis.Children.Add(txt);
                System.Windows.Controls.Canvas.SetTop(txt, y - 9);

                var tic = ChartLine(GridColor, Laxis.ActualWidth - 6, Laxis.ActualWidth - 3, y, y);
                Laxis.Children.Add(tic);
            }
        }

        /// <summary>
        /// ChartArea.ActualHeight calculation
        /// fix for - not accurate until rendered
        /// </summary>
        /// <returns></returns>
        private double ChartAreaActualHeight()
        {
            if (StudyButtons.ActualHeight > 0 || StudyButtons.Children.Count == 0 || ChartArea.ActualHeight == 0)
                return ChartArea.ActualHeight;
            return ChartArea.ActualHeight - ((System.Windows.Controls.Primitives.ToggleButton)StudyButtons.Children[0]).Height;
        }

        private void DrawRaxisAndGridLines() // draw Right axis (Prices)
        {
            double topPrice = 0;
            double bottomPrice = 99999999;
            MaxVolume = 0;

            for (int x = 0; x < NbrCandles && x + StartCandle < Candles.Count; x++)
            {
                var d = Candles[StartCandle + x];
                if (d.High > topPrice)
                    topPrice = d.High;
                if (d.Low < bottomPrice)
                    bottomPrice = d.Low;
                if (d.Volume > MaxVolume)
                    MaxVolume = d.Volume;
            }

            double priceLabelIncrement;
            /*if (Phs.Sector == "FOREX")
            {
                topPrice = Math.Ceiling(topPrice*100)/100 + .01d;
                bottomPrice = Math.Floor(bottomPrice*100)/100 - .01d;
                priceLabelIncrement = (topPrice - bottomPrice) / MaxPriceLabels;
            }
            else */
            {
                topPrice = Math.Ceiling(topPrice);
                bottomPrice = Math.Floor(bottomPrice);

                double tp = topPrice; // save
                int x;

                priceLabelIncrement = (topPrice - bottomPrice) / MaxPriceLabels;
                double[] incrementLevels = { .5, 1, 2.5, 5, 10, 20, 25, 50, 100, 200, 500, 1000 };
                for (x = 0; x < incrementLevels.Length; x++)
                { // adjust price increment to one of the standard ones
                    if (priceLabelIncrement <= incrementLevels[x])
                    {
                        priceLabelIncrement = incrementLevels[x];
                        break;
                    }
                }

                bottomPrice = Math.Round(((topPrice + bottomPrice) / 2) / priceLabelIncrement, 0) * priceLabelIncrement;
                bottomPrice -= priceLabelIncrement * Math.Ceiling((double)MaxPriceLabels / 2);
                topPrice = bottomPrice + priceLabelIncrement * MaxPriceLabels;

                if (topPrice < tp)
                {
                    priceLabelIncrement = incrementLevels[x + 1];
                    bottomPrice = Math.Round(((topPrice + bottomPrice) / 2) / priceLabelIncrement, 0) * priceLabelIncrement;
                    bottomPrice -= priceLabelIncrement * Math.Ceiling((double)MaxPriceLabels / 2);
                    topPrice = bottomPrice + priceLabelIncrement * MaxPriceLabels;
                }
            }

            PriceFactor = CalcFactor(topPrice, bottomPrice);

            var redrawPrices = false;
            GridLinesElements = new List<UIElement>();

            if (topPrice != TopPrice || bottomPrice != BottomPrice || LastChartAreaHeight != ChartAreaActualHeight())
            {
                Raxis.Children.Clear();
                TopPrice = topPrice;
                BottomPrice = bottomPrice;
                LastChartAreaHeight = ChartAreaActualHeight();
                redrawPrices = true;
            }

            for (var x = TopPrice; x >= BottomPrice; x -= (TopPrice - BottomPrice) / MaxPriceLabels)
            {
                double top = CalcTop(x);
                if (x < TopPrice && x > BottomPrice)
                {
                    var ln = ChartLine(GridColor);
                    ln.StrokeDashArray = dashes;
                    ln.Y1 = ln.Y2 = top;
                    ln.X1 = 0;
                    ln.X2 = ChartArea.ActualWidth;
                    GridLinesElements.Add(ln);
                }

                top += ChartHeading.ActualHeight;

                if (redrawPrices)
                {
                    var tic = ChartLine(GridColor);
                    tic.Y1 = tic.Y2 = top;
                    tic.X1 = 0;
                    tic.X2 = 3;
                    Raxis.Children.Add(tic);

                    var txt = new TextBlock() { Text = x.ToString(PriceFormat) };
                    txt.Foreground = txtColor;
                    Raxis.Children.Add(txt);
                    System.Windows.Controls.Canvas.SetTop(txt, top - 9);
                }
            }
            if (redrawPrices)
                Raxis.Children.Add(MousePriceBox);
        }

        /// <summary>
        /// draw X axis (Dates)
        /// </summary>
        private void DrawXaxis()
        {
            Xaxis.Children.Clear();

            int lastDateCandle = 0;
            int year = Candles[StartCandle].DateTime.Year;

            int fontSize = 10;
            if (CandleWidth > 9)
                fontSize = 12;
            else if (CandleWidth > 7)
                fontSize = 11;

            for (int x = 0; x < NbrCandles && x + StartCandle < Candles.Count; x++)
            {
                var d = Candles[StartCandle + x];
                var dateText = Cset.TimeFrame.ChartXaxisDateText(x, lastDateCandle, d.DateTime);

                if (dateText != "")
                {
                    DrawVerticaChartlLine(x);
                    var tb = new TextBlock() { Text = dateText, FontSize= fontSize };
                    tb.Foreground = txtColor;
                    Xaxis.Children.Add(tb);
                    System.Windows.Controls.Canvas.SetLeft(tb, ChartCandleCenter(x) - (tb.Text.Length * 6 / 2));
                    lastDateCandle = x;
                }

                if (year != d.DateTime.Year)
                {
                    year = d.DateTime.Year;
                    YearChangeLine(year, x);
                }
            }
            Xaxis.Children.Add(MouseDateBox);
        }

        /// <summary>
        /// Draw Candlesticks
        /// </summary>
        private void DrawCandles()
        {
            CandleElements = new List<UIElement>();
            for (int x = 0; x < NbrCandles && x + StartCandle < Candles.Count; x++)
            {
                switch (Chart_Type)
                {
                    case ChartType.CandleStick:
                        DrawCandleStick(x, Candles[StartCandle + x]);
                        break;
                }
            }

        }

        /// <summary>
        /// Draw a candlestick
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="d"></param>
        public void DrawCandleStick(int idx, Candle d)
        {
            Brush lineBrush;
            Brush bodyBrush;

            if (d.Open == d.Close)
            {
                lineBrush = System.Windows.Media.Brushes.White;
                bodyBrush = System.Windows.Media.Brushes.White;
            }
            else if (d.Close > d.Open)
            {
                lineBrush = GreenColor;
                bodyBrush = GreenCandleBody;
            }
            else
            {
                lineBrush = RedColor;
                bodyBrush = RedColor;
            }

            var ln = ChartLine(lineBrush);
            ln.X1 = ln.X2 = ChartCandleCenter(idx);
            ln.Y1 = (TopPrice - d.High) * PriceFactor;
            ln.Y2 = (TopPrice - d.Low) * PriceFactor;
            CandleElements.Add(ln);

            double top = 0;
            double candlePadding = CandleWidth <= 7 ? 1 : 2; // space closer if CandleWith is 5 or 7
            Rectangle body = new Rectangle()
            {
                Height = Math.Max(2, (Math.Abs(d.Open - d.Close) * PriceFactor)),
                Width = CandleWidth - candlePadding * 2,
                StrokeThickness = 1,
                Stroke = ln.Stroke,
                Fill = bodyBrush
            };
            top = (TopPrice - Math.Max(d.Open, d.Close)) * PriceFactor;
            CandleElements.Add(body);
            System.Windows.Controls.Canvas.SetTop(body, top);
            System.Windows.Controls.Canvas.SetLeft(body, idx * CandleWidth + candlePadding);
        }

        /// <summary>
        /// Add Rectangle to chart as first object to be drawn.
        /// Use to highlight a group of candles
        /// </summary>
        /// <param name="startCandle"></param>
        /// <param name="endCandle"></param>
        /// <param name="startPrice"></param>
        /// <param name="endPrice"></param>
        /// <param name="background"></param>
        /// <param name="borderThichness"></param>
        /// <param name="borderColor"></param>
        public Rectangle? BackgroundRectangle(int startCandle, int endCandle, double startPrice, double endPrice,
                        SolidColorBrush backgroundColor, int borderThickness=0, SolidColorBrush? borderColor=null,
                        double offsetX = 0, double offsetY=0 )
        {
            if (endCandle < StartCandle)
                return null; // candle not in view

            Rectangle rect = new Rectangle()
            {
                Height = Math.Max(2, (Math.Abs(endPrice - startPrice) * PriceFactor)),
                Width = CandleWidth * (endCandle - startCandle + 1),
                Fill = backgroundColor
            };
            if (borderThickness > 0 && borderColor != null)
            {
                rect.Stroke = borderColor;
                rect.StrokeThickness = borderThickness;
                rect.RadiusX = rect.RadiusY = 2;
            }
            System.Windows.Controls.Canvas.SetTop(rect, ChartAreaY(Math.Max(endPrice, startPrice)) + offsetY);
            System.Windows.Controls.Canvas.SetLeft(rect, ChartAreaX(startCandle) + offsetX);
            return rect;
        }

        /// <summary>
        /// Convert price to Y chart position
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        public double ChartAreaY(double price)
        {
            return (TopPrice - price) * PriceFactor;
        }

        /// <summary>
        /// Convert candle idx to X chart position
        /// </summary>
        /// <param name="candleIdx"></param>
        /// <returns></returns>
        public double ChartAreaX(int candleIdx)
        {
            return CandleWidth * (candleIdx - StartCandle);
        }

        public double LAxisPosition(double value, double maxValue)
        {
            return ((ChartAreaActualHeight() + 2) * (maxValue - value) / maxValue);
        }

        /// <summary>
        /// Draw a vertical dashed line for candle(idx)
        /// </summary>
        /// <param name="idx"></param>
        public void DrawVerticaChartlLine(int idx)
        {
            var ln = ChartLine(GridColor);
            ln.StrokeDashArray = dashes;
            ln.X1 = ln.X2 = ChartCandleCenter(idx);
            ln.Y1 = 0;
            ln.Y2 = ChartAreaActualHeight();
            ChartArea.Children.Add(ln);
        }

        /// <summary>
        /// Get left position of the center of the candle
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public double ChartCandleCenter(int idx)
        {
            return idx* CandleWidth +Math.Floor(CandleWidth / 2);
        }

        public double ChartCandleLeft(int idx)
        {
            return idx * CandleWidth;
        }

        /// <summary>
        /// Calculate candle index from X coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int ChartXtoCandle(double x)
        {
            var candleIdx = Math.Min(NbrCandles - 1, (int)Math.Floor(x / CandleWidth));
            return candleIdx;
        }



        /// <summary>
        /// Use with Chart.Prices variable
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int ChartXtoCandleIdx(double x)
        {
            return ChartXtoCandle(x) + StartCandle;
        }


        public double CalcFactor(double topValue, double bottomValue)
        {
            return (ChartAreaActualHeight() + 2) / (topValue - bottomValue); // number of pixels per unit
        }

        /// <summary>
        /// caclulate TOP for a price
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        public double CalcTop(double price)
        {
            return ((TopPrice - price) * PriceFactor);
        }

        /// <summary>
        /// create a line object
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        public Line ChartLine(Brush brush, double thickness=1)
        {
            var pln = new Line()
            {
                Visibility = System.Windows.Visibility.Visible,
                StrokeThickness = thickness,
                SnapsToDevicePixels = true,
                Stroke = brush
            };
            pln.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            return pln;
        }

        public Line ChartLine(Brush brush, double x1, double x2, double y1, double y2, double opacity=1)
        {
            var pln = new Line()
            {
                Visibility = System.Windows.Visibility.Visible,
                StrokeThickness = 1,
                SnapsToDevicePixels = true,
                Stroke = brush,
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2= y2,
                Opacity = opacity
            };
            pln.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            return pln;
        }

        public Line HorizontalPriceLine(Brush brush, int startCandle, int endCandle, double price, double thickness=1, double opacity = 1)
        {
            return PriceLine(brush, startCandle, endCandle, price, price, thickness, opacity);
        }

        public Line PriceLine(Brush brush, int startCandle, int endCandle, double fromPrice, double toPrice, double thickness=1, double opacity = 1)
        {
            var pln = new Line()
            {
                Visibility = System.Windows.Visibility.Visible,
                StrokeThickness = thickness,
                SnapsToDevicePixels = true,
                Stroke = brush,
                X1 = ChartAreaX(startCandle),
                X2 = ChartAreaX(endCandle+1)-1,
                Y1 = ChartAreaY(fromPrice),
                Y2 = ChartAreaY(toPrice),
                Opacity = opacity,
            };
            pln.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            return pln;
        }

        public void YearChangeLine(int newYear, int candleIdx)
        {
            var line = ChartLine(GridColor);
            line.Y1 = 0;
            line.Y2 = ChartAreaActualHeight();
            line.X1 = line.X2 = ChartCandleLeft(candleIdx) -1;

            var txt = new TextBlock()
            {
                Text = (newYear - 1).ToString() + " " + newYear.ToString(),
                Width = 42,
                Height = 46,
                FontSize = 18,
                Foreground = txtColor2,
                TextWrapping = TextWrapping.WrapWithOverflow,
                TextAlignment = TextAlignment.Center,
                RenderTransform = new RotateTransform(-90)
            };
            Canvas.SetTop(txt, line.Y2 - 5);
            Canvas.SetLeft(txt, line.X1 - 24);

            GridLinesElements.Add(txt);
            GridLinesElements.Add(line);
        }

        /// <summary>
        /// Add a price line (horzontal) and price to the chart
        /// </summary>
        /// <param name="priceText"></param>
        /// <param name="price"></param>
        /// <param name="brush"></param>
        /// <param name="dashes"></param>
        public void AddPriceGridLine(string priceText, double price, Brush brush, DoubleCollection dashes)
        {
            double top = CalcTop(price);
            if (price < TopPrice && price > BottomPrice)
            {
                var ln = ChartLine(brush);
                ln.StrokeDashArray = dashes;
                ln.Y1 = ln.Y2 = top;
                ln.X1 = 0;
                ln.X2 = ChartArea.ActualWidth;
                ChartArea.Children.Add(ln);
            }

            top += ChartHeading.ActualHeight;

            var tic = ChartLine(brush);
            tic.Y1 = tic.Y2 = top;
            tic.X1 = 0;
            tic.X2 = 3;
            Raxis.Children.Add(tic);

            var txt = new TextBlock() { Text = priceText };
            txt.Foreground = txtColor;
            Raxis.Children.Add(txt);
            System.Windows.Controls.Canvas.SetTop(txt, top - 9);
        }

        private void DrawHeading(int candleIdx)
        {
            var idx = Math.Min(candleIdx + StartCandle, Candles.Count - 1);
            var d = Candles[idx];

            
            tbDate.Text = /*Phs.Interval().Substring(0,1) + ": " + */ d.DateTime.ToString("MM/dd/yyyy");
            tbOpen.Text = "O: " + d.Open.ToString(DecimalFormat);
            tbHigh.Text = "H: " + d.High.ToString(DecimalFormat);
            tbLow.Text = "L: " + d.Low.ToString(DecimalFormat);
            tbClose.Text = "C: " + d.Close.ToString(DecimalFormat);
            tbVolume.Text = "V: " + d.Volume.ToString("N0");

            int sx = 0;
            for (int x = 0; x < ChartStudies.Count && sx < HeadingStudyTb.Length; x++)
            {
                if (ChartStudies[x].Show)
                {
                    if (ChartStudies[x].Study.IsLoaded && idx + Cset.StartTimeIndex < ChartStudies[x].Study.Length)
                    {
                        HeadingStudyTb[sx].Text = StudyDescription[sx] + ChartStudies[x].Study.DisplayValue(idx + Cset.StartTimeIndex);
                        sx++;
                    }

                }
            }

        }

        public class PriceDateCandle
        {
            public double Price;
            public DateTime Date;
            public int ChartCandleIdx;
            public int PricesIdx;
        }

        /// <summary>
        /// Get Date & Price for a point on the ChartArea
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public PriceDateCandle? ClickedPriceDateCandle(MouseEventArgs e)
        {
            var position = e.GetPosition(ChartArea);
            var idx = ChartXtoCandleIdx(position.X);

            if (idx >= Candles.Count)
                return null;

            return new PriceDateCandle()
            {
                Price = TopPrice - Math.Round(position.Y / PriceFactor, Decimals),
                Date = Candles[idx].DateTime,
                PricesIdx = idx,
                ChartCandleIdx = idx-StartCandle
            };
        }

        private void ChartArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (Candles == null)
                return;


            var position = e.GetPosition(ChartArea);
            var candleIdx = ChartXtoCandleIdx(position.X);
            if (candleIdx < Candles.Count)
            {
                MouseX.X1 = MouseX.X2 = position.X;
                MouseX.Y2 = ChartAreaActualHeight();
                MouseY.Y1 = MouseY.Y2 = position.Y;
                MouseY.X2 = ChartArea.ActualWidth;

                var mb = ((Border)MousePrice.Parent);
                System.Windows.Controls.Canvas.SetTop(mb, position.Y + ChartHeading.ActualHeight - mb.ActualHeight / 2);
                var price = TopPrice - Math.Round(position.Y / PriceFactor, Decimals);
                MousePrice.Text = price.ToString("N2");
                mb.Visibility = Visibility.Visible;

                mb = ((Border)MouseDate.Parent);
                System.Windows.Controls.Canvas.SetLeft(mb, Math.Max(0, position.X - mb.ActualWidth / 2));


                MouseDate.Text = Cset.TimeFrame.DateText(Candles[candleIdx].DateTime);
                mb.Visibility = Visibility.Visible;
                DrawHeading(ChartXtoCandle(position.X));
            }
        }

        private void ChartArea_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Candles == null)
                return;
            
            MouseX.X1 = MouseX.X2 = 0;
            MouseX.Y2 = 0;
            MouseY.Y1 = MouseY.Y2 = 0;
            MouseY.X2 = 0;
            ((Border)MousePrice.Parent).Visibility = Visibility.Hidden;
            ((Border)MouseDate.Parent).Visibility = Visibility.Hidden;
            DrawHeading(StartCandle + NbrCandles - 1);
        }

        private void HScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            HScrollRatio = (Candles.Count - NbrCandles) / hScrollBar.Maximum;

            SetStartCandle((int)(e.NewValue * HScrollRatio));
            DrawAfterHoursShading();
            DrawVolume();
            DrawRaxisAndGridLines();  // draw Right axis (Prices)
            DrawXaxis(); // Dates
            DrawCandles();
            foreach (var cs in ChartStudies)
            {
                if (cs.Show)
                    cs.Study.Draw(this);
            }
            UpdateChartArea();

        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            if (Candles != null)
            {
                if (((Button)sender).Name == "ZoomIn")
                    CandleWidth = Math.Min(19, CandleWidth + 1);
                else // ZoomOut
                    CandleWidth = Math.Max(5, CandleWidth - 1);

                NbrCandles = Math.Max(2, (int)(ChartArea.ActualWidth / CandleWidth));
                SetStartCandle(Candles.Count - NbrCandles);
                ReDraw();
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(Chart_Type, Cset);
        }
    }

}
