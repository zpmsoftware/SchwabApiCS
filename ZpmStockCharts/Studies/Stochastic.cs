using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class Stochastic : Study
    {
        // https://www.investopedia.com/terms/s/stochasticoscillator.asp

        StochasticValues stochasticValues;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kPeriods"></param>
        /// <param name="dPeriods"></param>
        /// <param name="kSmoothing"></param>
        /// <param name="method">0 = simple, 1= expodential</param>
        /// <param name="color"></param>
        public Stochastic(int kPeriods, int dPeriods, int kSmoothing, int method, Brush color)
        : base(color)
        {
            stochasticValues = new StochasticValues(kPeriods, dPeriods, kSmoothing, method);
        }

        public override string StudyDescription()
        {
            return stochasticValues.ToString();
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - Stochastic.";
        }

        public override int PrependCandlesNeeded { get { return Math.Max(stochasticValues.KPeriods, stochasticValues.DPeriods) - 1; } } // required to get a good value for the firsst chart candle


        public override void Calculate(CandleSet cs)
        {
            stochasticValues.Calculate(cs.Candles);
        }

        public static StochasticValues Calculate(List<Candle> prices, int kPeriods, int dPeriods, int kSmoothing, int method)
        {
            var s = new StochasticValues(kPeriods, dPeriods, kSmoothing, method);
            s.Calculate(prices);
            return s;
        }

        public class StochasticValues
        {
            public StochasticValues(int kPeriods, int dPeriods, int kSmoothing, int method)
            {
                Method = method;
                DPeriods = dPeriods;
                KPeriods = kPeriods;
                KSmoothing = kSmoothing;
            }
            public override string ToString()
            {
                return $"Stoch({KPeriods},{DPeriods},{KSmoothing},{Method})";
            }

            public double[] K { get; set; }
            public double[] D { get; set; }

            public int KPeriods { get; set; }
            public int DPeriods { get; set; }
            public int KSmoothing { get; set; }
            public int Method { get; set; }

            public void Calculate(List<Candle> p)
            {
                double[] kd = null; // kData[sdIdx];
                //var p = priceHistorySet.Prices;

                if (K == null || K.Length != p.Count)
                    K = new double[p.Count];

                double low = p[0].Low;
                double high = p[0].High;
                int lowIdx = 0;
                int highIdx = 0;

                for (int x = 0; x < p.Count; x++)
                {
                    if (p[x].Low <= low)
                    {
                        low = p[x].Low;
                        lowIdx = x;
                    }
                    else if (lowIdx == x - KPeriods) // low value is one being dropped
                    {
                        low = 9999999;
                        for (int i = lowIdx + 1; i <= x; i++)
                        {
                            if (p[i].Low <= low)
                            {
                                low = p[i].Low;
                                lowIdx = i;
                            }
                        }
                    }

                    if (p[x].High >= high)
                    {
                        high = p[x].High;
                        highIdx = x;
                    }
                    else if (highIdx == x - KPeriods) // high value is one being dropped
                    {
                        high = 0;
                        for (int i = highIdx + 1; i <= x; i++)
                        {
                            if (p[i].High >= high)
                            {
                                high = p[i].High;
                                highIdx = i;
                            }
                        }
                    }

                    if (high == low)
                        K[x] = (x > 0) ? K[x - 1] : 100;
                    else
                        K[x] = 100 * (p[x].Close - low) / (high - low);
                }

                D = (KSmoothing > 1) ? SMA.Calculate(K, KSmoothing) : K;
            }

            /// <summary>
            /// %K cross %D
            /// </summary>
            /// <param name="idx"></param>
            /// <returns>0 no cross, -1= %K crossed below %D, 1= %K crossed above %D</returns>
            public int Cross(int idx)
            { // =0 no cross, -1= %K crossed below %D, 1= %K crossed above %D
                if (idx > 0)
                {
                    if (K[idx - 1] >= D[idx - 1])
                    { // was above or same
                        if (K[idx] < D[idx])
                            return -1; // now crossed below
                    }
                    if (K[idx - 1] <= D[idx - 1])
                    { // was below or same
                        if (K[idx] > D[idx])
                            return 1; // now crossed above
                    }
                }
                return 0; // no cross today
            }
        }

        public override void Draw(PriceChart chart)
        {
            UiElements.Clear();
            if (stochasticValues.K == null)
                return;

            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index. Values[] has earlier dates, StartDateIndex lines up values with chart.
            var k = stochasticValues.K;
            var d = stochasticValues.D;

            if (startIdx < k.Length)
            {
                var y = chart.LAxisPosition(70, 100);
                var ln = chart.ChartLine(Color, 0, chart.ChartArea.ActualWidth, y, y, .5);
                UiElements.Add(ln);

                y = chart.LAxisPosition(30, 100);
                ln = chart.ChartLine(Color, 0, chart.ChartArea.ActualWidth, y, y, .5);
                UiElements.Add(ln);


                var perUnit = (chart.ChartArea.ActualHeight + 200) / 100;
                double x1 = chart.ChartCandleCenter(0);
                double ky1 = ((100 - k[startIdx]) * perUnit) - 100;
                double dy1 = ((100 - d[startIdx]) * perUnit) - 100;

                for (int x = 0; x + startIdx < k.Length && x < chart.NbrCandles; x++)
                {
                    ln = chart.ChartLine(Color);
                    ln.X1 = x1;
                    ln.Y1 = ky1;
                    ln.X2 = chart.ChartCandleCenter(x);
                    ln.Y2 = chart.LAxisPosition(k[x + startIdx], 100);
                    UiElements.Add(ln);
                    ky1 = ln.Y2;

                    ln = chart.ChartLine(System.Windows.Media.Brushes.Brown);
                    ln.X1 = x1;
                    ln.Y1 = dy1;
                    ln.X2 = chart.ChartCandleCenter(x);
                    ln.Y2 = chart.LAxisPosition(d[x + startIdx], 100);
                    UiElements.Add(ln);
                    dy1 = ln.Y2;

                    x1 = ln.X2;
                }
            }
        }

    }
}
