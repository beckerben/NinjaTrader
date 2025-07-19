using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class BreakawayFairValueGaps : Indicator
    {
        private int length = 20;
        private bool showMitigation = true;
        private int maxDuration = 60;
        private bool showDash = true;
        private bool use_med = false;
        private string dashLoc = "Bottom Right";
        private int textSize = 14;

        private List<NinjaTrader.NinjaScript.DrawingTools.Line> bull_bfvg_mt = new List<NinjaTrader.NinjaScript.DrawingTools.Line>();
        private List<NinjaTrader.NinjaScript.DrawingTools.Line> bear_bfvg_mt = new List<NinjaTrader.NinjaScript.DrawingTools.Line>();
        private List<NinjaTrader.NinjaScript.DrawingTools.Text> bull_bfvg_la = new List<NinjaTrader.NinjaScript.DrawingTools.Text>();
        private List<NinjaTrader.NinjaScript.DrawingTools.Text> bear_bfvg_la = new List<NinjaTrader.NinjaScript.DrawingTools.Text>();

        private int bull_fvg_count = 0, bull_bfvg_count = 0;
        private int bear_fvg_count = 0, bear_bfvg_count = 0;

        private double upper, lower, mid;
        private bool bull_break = false, bear_break = false;
        private double bull_lvl = 0, bear_lvl = 0;

        private List<int> bull_bars = new List<int>();
        private List<int> bear_bars = new List<int>();

        private int bear_mit_count = 0, bull_mit_count = 0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "LuxAlgo - Breakaway FVG";
                Name = "BreakawayFairValueGaps";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < length)
                return;

            upper = MAX(High, length)[2];
            lower = MIN(Low, length)[2];
            mid = (upper + lower) / 2;

            bool bull_fvg = Low[0] > High[2] && Close[1] > High[2];
            bool bear_fvg = High[0] < Low[2] && Close[1] < Low[2];

            if (Low[0] > upper && bull_fvg)
            {
                Draw.Rectangle(this, "bullBFVG" + CurrentBar, false, 2, High[2], 0, Low[0], Brushes.Green, Brushes.Green, 40);
                bull_bfvg_count++;
                bull_break = false;
                bull_lvl = High[2];

                if (showMitigation)
                {
                    bull_bfvg_mt.Add(Draw.Line(this, "bullMT" + CurrentBar, false, 2, High[2], 0, High[2], Brushes.Green, DashStyleHelper.Solid, 2));
                    bull_bfvg_la.Add(Draw.Text(this, "bullLA" + CurrentBar, "2", 0, High[2], Brushes.Black));
                }
            }

            if (High[0] < lower && bear_fvg)
            {
                Draw.Rectangle(this, "bearBFVG" + CurrentBar, false, 2, Low[2], 0, High[0], Brushes.Red, Brushes.Red, 40);
                bear_bfvg_count++;
                bear_break = false;
                bear_lvl = Low[2];

                if (showMitigation)
                {
                    bear_bfvg_mt.Add(Draw.Line(this, "bearMT" + CurrentBar, false, 2, Low[2], 0, Low[2], Brushes.Red, DashStyleHelper.Solid, 2));
                    bear_bfvg_la.Add(Draw.Text(this, "bearLA" + CurrentBar, "2", 0, Low[2], Brushes.Black));
                }
            }

            bull_lvl = Math.Max(bull_lvl, mid);
            bear_lvl = Math.Min(bear_lvl, mid);

            if (High[0] > bear_lvl)
                bear_break = true;
            if (Low[0] < bull_lvl)
                bull_break = true;

            bull_fvg_count += bull_fvg ? 1 : 0;
            bear_fvg_count += bear_fvg ? 1 : 0;

            if (bull_bfvg_mt.Count > 0)
            {
                for (int i = bull_bfvg_mt.Count - 1; i >= 0; i--)
                {
                    bull_bfvg_mt[i].EndAnchor = new ChartAnchor { Time = Time[0], Price = bull_bfvg_mt[i].StartAnchor.Price };
                    //bull_bfvg_la[i].Text = (CurrentBar - Bars.GetBar(bull_bfvg_mt[i].StartAnchor.Time)).ToString();

                    if (Close[0] < bull_bfvg_mt[i].StartAnchor.Price)
                    {
                        bull_bfvg_mt.RemoveAt(i);
                        bull_bfvg_la.RemoveAt(i);
                        bull_mit_count++;
                        bull_bars.Add(CurrentBar - Bars.GetBar(bull_bfvg_mt[i].StartAnchor.Time));
                    }
                    else if (CurrentBar - Bars.GetBar(bull_bfvg_mt[i].StartAnchor.Time) == maxDuration)
                    {
                        bull_bfvg_mt.RemoveAt(i);
                        bull_bfvg_la.RemoveAt(i);
                    }
                }
            }

            if (bear_bfvg_mt.Count > 0)
            {
                for (int i = bear_bfvg_mt.Count - 1; i >= 0; i--)
                {
                    bear_bfvg_mt[i].EndAnchor = new ChartAnchor { Time = Time[0], Price = bear_bfvg_mt[i].StartAnchor.Price };
                    //bear_bfvg_la[i].Text = (CurrentBar - Bars.GetBar(bear_bfvg_mt[i].StartAnchor.Time)).ToString();

                    if (Close[0] > bear_bfvg_mt[i].StartAnchor.Price)
                    {
                        bear_bfvg_mt.RemoveAt(i);
                        bear_bfvg_la.RemoveAt(i);
                        bear_mit_count++;
                        bear_bars.Add(CurrentBar - Bars.GetBar(bear_bfvg_mt[i].StartAnchor.Time));
                    }
                    else if (CurrentBar - Bars.GetBar(bear_bfvg_mt[i].StartAnchor.Time) == maxDuration)
                    {
                        bear_bfvg_mt.RemoveAt(i);
                        bear_bfvg_la.RemoveAt(i);
                    }
                }
            }

            if (showDash && IsFirstTickOfBar)
            {
                //Draw.TextFixed(this, "Dashboard", "Max " + maxDuration + " Bars\nMitigation %\n" + (use_med ? "Med." : "Avg.") + " Bars\nTotal", TextPosition.BottomRight, Brushes.White, Brushes.Black, 14);
            }

            if (showDash)
            {
                //Draw.TextFixed(this, "DashboardBull", "Bullish\n" + (bull_mit_count / bull_bfvg_count * 100).ToString("P") + "\n" + (use_med ? Median(bull_bars) : bull_bars.Average()).ToString() + "\n" + bull_bfvg_count, TextPosition.BottomRight, Brushes.White, Brushes.Black, 14);
                //Draw.TextFixed(this, "DashboardBear", "Bearish\n" + (bear_mit_count / bear_bfvg_count * 100).ToString("P") + "\n" + (use_med ? Median(bear_bars) : bear_bars.Average()).ToString() + "\n" + bear_bfvg_count, TextPosition.BottomRight, Brushes.White, Brushes.Black, 14);
            }
        }

        private double Median(List<int> list)
        {
            if (list.Count == 0)
                return 0;

            list.Sort();
            int mid = list.Count / 2;
            if (list.Count % 2 == 0)
                return (list[mid - 1] + list[mid]) / 2.0;
            else
                return list[mid];
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BreakawayFairValueGaps[] cacheBreakawayFairValueGaps;
		public BreakawayFairValueGaps BreakawayFairValueGaps()
		{
			return BreakawayFairValueGaps(Input);
		}

		public BreakawayFairValueGaps BreakawayFairValueGaps(ISeries<double> input)
		{
			if (cacheBreakawayFairValueGaps != null)
				for (int idx = 0; idx < cacheBreakawayFairValueGaps.Length; idx++)
					if (cacheBreakawayFairValueGaps[idx] != null &&  cacheBreakawayFairValueGaps[idx].EqualsInput(input))
						return cacheBreakawayFairValueGaps[idx];
			return CacheIndicator<BreakawayFairValueGaps>(new BreakawayFairValueGaps(), input, ref cacheBreakawayFairValueGaps);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BreakawayFairValueGaps BreakawayFairValueGaps()
		{
			return indicator.BreakawayFairValueGaps(Input);
		}

		public Indicators.BreakawayFairValueGaps BreakawayFairValueGaps(ISeries<double> input )
		{
			return indicator.BreakawayFairValueGaps(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BreakawayFairValueGaps BreakawayFairValueGaps()
		{
			return indicator.BreakawayFairValueGaps(Input);
		}

		public Indicators.BreakawayFairValueGaps BreakawayFairValueGaps(ISeries<double> input )
		{
			return indicator.BreakawayFairValueGaps(input);
		}
	}
}

#endregion
