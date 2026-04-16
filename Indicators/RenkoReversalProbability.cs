#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class RenkoReversalProbability : Indicator
	{
		#region Private Fields

		private SortedDictionary<string, ReversalStats> _lookup;
		private EMA _ema10s;
		private RSI _rsi10s;
		private bool _lastBarWasNewBrick;

		#endregion

		#region ReversalStats Inner Class

		private class ReversalStats
		{
			public int Reversals { get; set; }
			public int Total { get; set; }

			public double P()
			{
				return Total > 0 ? (double)Reversals / Total : 0.5;
			}
		}

		#endregion

		#region State Management

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Displays the rolling probability that the next Renko brick will be a reversal bar.";
				Name = "RenkoReversalProbability";
				Calculate = Calculate.OnBarClose;
				IsOverlay = false;
				DisplayInDataBox = true;
				DrawOnPricePanel = false;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;

				// Parameters
				ReversalThreshold = 0.55;
				LookbackBricks = 10;
				ExhaustionThreshold = 0;  // 0 = off (SPEC-01 mode), >= 1 = only update lookup when consec meets threshold (SPEC-04 mode)
				EMAPeriod = 20;
				RSIPeriod = 14;
				VolumeLookback = 10;
				VolumeHighRatio = 1.5;
				VolumeLowRatio = 0.7;
				RSIOverbought = 65;
				RSIOversold = 35;
				MinHistory = 5;

				// Plot defaults
				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "Probability");
				AddLine(new Stroke(Brushes.OrangeRed, DashStyleHelper.Dash, 1), 0.55, "Threshold");
				AddLine(new Stroke(Brushes.DimGray, DashStyleHelper.Dot, 1), 0.50, "Neutral");
			}
			else if (State == State.Configure)
			{
				// Add 10-second secondary data series.
				// When hosted by a strategy via AddChartIndicator(), the strategy
				// must add all data series in its own Configure state — the indicator
				// is not allowed to. We catch the exception to handle both cases:
				// standalone (works) and hosted (already loaded by the strategy).
				try
				{
					AddDataSeries(BarsPeriodType.Second, 10);
				}
				catch (Exception)
				{
					// Hosted by a strategy that already loaded the 10s series — expected
				}
			}
			else if (State == State.DataLoaded)
			{
				_lookup = new SortedDictionary<string, ReversalStats>();

				// Initialize indicators on secondary series (BarsArray[1])
				_ema10s = EMA(BarsArray[1], EMAPeriod);
				_rsi10s = RSI(BarsArray[1], RSIPeriod, 1);
			}
		}

		#endregion

		#region OnBarUpdate

		protected override void OnBarUpdate()
		{
			// Only process primary Renko series
			if (BarsInProgress != 0)
				return;

			// Need enough bars on both series
			if (CurrentBars[0] < 2 || CurrentBars[1] < EMAPeriod + 2)
			{
				Values[0][0] = 0.5;
				return;
			}

			// With Calculate.OnBarClose, every OnBarUpdate call is a completed brick:
			//   Bar[0] = just-closed brick (our trigger)
			//   Bar[1] = prior brick
			//   Bar[2] = two bricks ago

			// Step 1: Update lookup (one-bar-late rule)
			// Now that bar[0] closed, we know the outcome for bar[1]'s bucket:
			// Did bar[0] reverse relative to bar[1]?
			if (CurrentBars[0] >= 2)
			{
				int dir0 = BrickDirection(0);
				int dir1 = BrickDirection(1);

				if (dir0 != 0 && dir1 != 0)
				{
					// ExhaustionThreshold gate: when > 0, only update lookup for bars
					// where consecutive count met the threshold (SPEC-04 filtered lookup)
					bool meetsExhaustion = ExhaustionThreshold <= 0 || ConsecutiveCount(1) >= ExhaustionThreshold;

					if (meetsExhaustion)
					{
						string prevBucket = BuildBucket(1);
						bool wasReversal = dir0 != dir1;
						UpdateLookup(prevBucket, wasReversal);
					}
				}
			}

			// Step 2: Build bucket for current state (bar[0] = just-closed brick)
			// We're predicting: will the NEXT brick reverse relative to bar[0]?
			string currentBucket = BuildBucket(0);
			double prob = P_Reversal(currentBucket);

			// Step 3: Plot probability
			Values[0][0] = prob;

			// Step 4: Update threshold line to match parameter
			Lines[0].Value = ReversalThreshold;

			// Step 5: Draw debug text
			int consec = ConsecutiveCount(0);
			int sampleCount = GetSampleCount(currentBucket);
			string debugText = currentBucket + "  |  N=" + sampleCount;
			if (ExhaustionThreshold > 0)
				debugText += "  |  exh=" + (consec >= ExhaustionThreshold ? "YES" : "no") + " (" + consec + "/" + ExhaustionThreshold + ")";
			Draw.TextFixed(this, "DebugBucket", debugText, TextPosition.BottomRight,
				Brushes.White, new SimpleFont("Consolas", 11), Brushes.Transparent, Brushes.Black, 80);
		}

		#endregion

		#region Brick Utilities

		private int BrickDirection(int index)
		{
			if (Close[index] > Open[index]) return 1;   // up brick
			if (Close[index] < Open[index]) return -1;  // down brick
			return 0;
		}

		private int ConsecutiveCount(int startIndex)
		{
			int dir = BrickDirection(startIndex);
			if (dir == 0) return 0;

			int count = 1;
			int maxLookback = Math.Min(startIndex + LookbackBricks, CurrentBars[0]);

			for (int i = startIndex + 1; i <= maxLookback; i++)
			{
				if (BrickDirection(i) == dir)
					count++;
				else
					break;
			}

			return count;
		}

		#endregion

		#region Secondary Series Helpers

		private int GetEMASlope()
		{
			// Use last completed 10s bars (index 1 and 3 on secondary to avoid noise)
			if (CurrentBars[1] < EMAPeriod + 3)
				return 0;

			double emaNow = _ema10s[1];
			double emaPrev = _ema10s[3];
			double delta = emaNow - emaPrev;

			if (delta > 0) return 1;   // rising
			if (delta < 0) return -1;  // falling
			return 0;                   // flat
		}

		private int GetRSIZone()
		{
			if (CurrentBars[1] < RSIPeriod + 2)
				return 0;

			double rsi = _rsi10s[1];

			if (rsi > RSIOverbought) return 1;   // overbought
			if (rsi < RSIOversold) return -1;     // oversold
			return 0;                              // neutral
		}

		private string GetVolumeLabel()
		{
			if (CurrentBars[0] < VolumeLookback)
				return "normal";

			double avgVol = 0;
			for (int i = 1; i <= VolumeLookback; i++)
				avgVol += Volume[i];
			avgVol /= VolumeLookback;

			double ratio = avgVol > 0 ? Volume[1] / avgVol : 1.0;

			if (ratio > VolumeHighRatio) return "high";
			if (ratio < VolumeLowRatio) return "low";
			return "normal";
		}

		#endregion

		#region Bucket Building

		private string BuildBucket(int barIndex)
		{
			// Consecutive count for the bar at barIndex
			int consec = ConsecutiveCount(barIndex);
			string consecLabel = consec >= 4 ? "4+" : consec.ToString();

			// EMA slope from 10s series
			int emaSlope = GetEMASlope();
			string emaLabel = emaSlope > 0 ? "up" : emaSlope < 0 ? "down" : "flat";

			// RSI zone from 10s series
			int rsiZone = GetRSIZone();
			string rsiLabel = rsiZone > 0 ? "ob" : rsiZone < 0 ? "os" : "neutral";

			// Volume ratio
			string volLabel = GetVolumeLabel();

			return string.Format("consec={0},ema={1},rsi={2},vol={3}",
				consecLabel, emaLabel, rsiLabel, volLabel);
		}

		#endregion

		#region Lookup Table

		private void UpdateLookup(string bucket, bool wasReversal)
		{
			if (!_lookup.ContainsKey(bucket))
				_lookup[bucket] = new ReversalStats();

			var s = _lookup[bucket];
			s.Total++;
			if (wasReversal) s.Reversals++;
		}

		private double P_Reversal(string bucket)
		{
			if (_lookup.TryGetValue(bucket, out var s) && s.Total >= MinHistory)
				return s.P();
			return 0.5;  // neutral default
		}

		private int GetSampleCount(string bucket)
		{
			if (_lookup.TryGetValue(bucket, out var s))
				return s.Total;
			return 0;
		}

		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(0.01, 0.99)]
		[Display(Name = "Reversal Threshold", Order = 1, GroupName = "Parameters")]
		public double ReversalThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "Lookback Bricks", Order = 2, GroupName = "Parameters")]
		public int LookbackBricks { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Exhaustion Threshold", Description = "0 = off (SPEC-01). When > 0, lookup only trains on bars where consecutive count >= this value (SPEC-04).", Order = 3, GroupName = "Parameters")]
		public int ExhaustionThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "EMA Period (10s)", Order = 4, GroupName = "Parameters")]
		public int EMAPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "RSI Period (10s)", Order = 5, GroupName = "Parameters")]
		public int RSIPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "Volume Lookback", Order = 6, GroupName = "Parameters")]
		public int VolumeLookback { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 5.0)]
		[Display(Name = "Volume High Ratio", Order = 7, GroupName = "Parameters")]
		public double VolumeHighRatio { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume Low Ratio", Order = 8, GroupName = "Parameters")]
		public double VolumeLowRatio { get; set; }

		[NinjaScriptProperty]
		[Range(50, 90)]
		[Display(Name = "RSI Overbought", Order = 9, GroupName = "Parameters")]
		public double RSIOverbought { get; set; }

		[NinjaScriptProperty]
		[Range(10, 50)]
		[Display(Name = "RSI Oversold", Order = 10, GroupName = "Parameters")]
		public double RSIOversold { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Min History", Description = "Minimum samples before using bucket probability", Order = 11, GroupName = "Parameters")]
		public int MinHistory { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Probability
		{
			get { return Values[0]; }
		}

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RenkoReversalProbability[] cacheRenkoReversalProbability;
		public RenkoReversalProbability RenkoReversalProbability(double reversalThreshold, int lookbackBricks, int exhaustionThreshold, int eMAPeriod, int rSIPeriod, int volumeLookback, double volumeHighRatio, double volumeLowRatio, double rSIOverbought, double rSIOversold, int minHistory)
		{
			return RenkoReversalProbability(Input, reversalThreshold, lookbackBricks, exhaustionThreshold, eMAPeriod, rSIPeriod, volumeLookback, volumeHighRatio, volumeLowRatio, rSIOverbought, rSIOversold, minHistory);
		}

		public RenkoReversalProbability RenkoReversalProbability(ISeries<double> input, double reversalThreshold, int lookbackBricks, int exhaustionThreshold, int eMAPeriod, int rSIPeriod, int volumeLookback, double volumeHighRatio, double volumeLowRatio, double rSIOverbought, double rSIOversold, int minHistory)
		{
			if (cacheRenkoReversalProbability != null)
				for (int idx = 0; idx < cacheRenkoReversalProbability.Length; idx++)
					if (cacheRenkoReversalProbability[idx] != null && cacheRenkoReversalProbability[idx].ReversalThreshold == reversalThreshold && cacheRenkoReversalProbability[idx].LookbackBricks == lookbackBricks && cacheRenkoReversalProbability[idx].ExhaustionThreshold == exhaustionThreshold && cacheRenkoReversalProbability[idx].EMAPeriod == eMAPeriod && cacheRenkoReversalProbability[idx].RSIPeriod == rSIPeriod && cacheRenkoReversalProbability[idx].VolumeLookback == volumeLookback && cacheRenkoReversalProbability[idx].VolumeHighRatio == volumeHighRatio && cacheRenkoReversalProbability[idx].VolumeLowRatio == volumeLowRatio && cacheRenkoReversalProbability[idx].RSIOverbought == rSIOverbought && cacheRenkoReversalProbability[idx].RSIOversold == rSIOversold && cacheRenkoReversalProbability[idx].MinHistory == minHistory && cacheRenkoReversalProbability[idx].EqualsInput(input))
						return cacheRenkoReversalProbability[idx];
			return CacheIndicator<RenkoReversalProbability>(new RenkoReversalProbability(){ ReversalThreshold = reversalThreshold, LookbackBricks = lookbackBricks, ExhaustionThreshold = exhaustionThreshold, EMAPeriod = eMAPeriod, RSIPeriod = rSIPeriod, VolumeLookback = volumeLookback, VolumeHighRatio = volumeHighRatio, VolumeLowRatio = volumeLowRatio, RSIOverbought = rSIOverbought, RSIOversold = rSIOversold, MinHistory = minHistory }, input, ref cacheRenkoReversalProbability);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RenkoReversalProbability RenkoReversalProbability(double reversalThreshold, int lookbackBricks, int exhaustionThreshold, int eMAPeriod, int rSIPeriod, int volumeLookback, double volumeHighRatio, double volumeLowRatio, double rSIOverbought, double rSIOversold, int minHistory)
		{
			return indicator.RenkoReversalProbability(Input, reversalThreshold, lookbackBricks, exhaustionThreshold, eMAPeriod, rSIPeriod, volumeLookback, volumeHighRatio, volumeLowRatio, rSIOverbought, rSIOversold, minHistory);
		}

		public Indicators.RenkoReversalProbability RenkoReversalProbability(ISeries<double> input , double reversalThreshold, int lookbackBricks, int exhaustionThreshold, int eMAPeriod, int rSIPeriod, int volumeLookback, double volumeHighRatio, double volumeLowRatio, double rSIOverbought, double rSIOversold, int minHistory)
		{
			return indicator.RenkoReversalProbability(input, reversalThreshold, lookbackBricks, exhaustionThreshold, eMAPeriod, rSIPeriod, volumeLookback, volumeHighRatio, volumeLowRatio, rSIOverbought, rSIOversold, minHistory);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RenkoReversalProbability RenkoReversalProbability(double reversalThreshold, int lookbackBricks, int exhaustionThreshold, int eMAPeriod, int rSIPeriod, int volumeLookback, double volumeHighRatio, double volumeLowRatio, double rSIOverbought, double rSIOversold, int minHistory)
		{
			return indicator.RenkoReversalProbability(Input, reversalThreshold, lookbackBricks, exhaustionThreshold, eMAPeriod, rSIPeriod, volumeLookback, volumeHighRatio, volumeLowRatio, rSIOverbought, rSIOversold, minHistory);
		}

		public Indicators.RenkoReversalProbability RenkoReversalProbability(ISeries<double> input , double reversalThreshold, int lookbackBricks, int exhaustionThreshold, int eMAPeriod, int rSIPeriod, int volumeLookback, double volumeHighRatio, double volumeLowRatio, double rSIOverbought, double rSIOversold, int minHistory)
		{
			return indicator.RenkoReversalProbability(input, reversalThreshold, lookbackBricks, exhaustionThreshold, eMAPeriod, rSIPeriod, volumeLookback, volumeHighRatio, volumeLowRatio, rSIOverbought, rSIOversold, minHistory);
		}
	}
}

#endregion
