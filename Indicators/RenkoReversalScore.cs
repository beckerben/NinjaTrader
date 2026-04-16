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
	public class RenkoReversalScore : Indicator
	{
		#region Private Fields

		private EMA _emaFast;
		private EMA _emaSlow;
		private RSI _rsi10s;

		#endregion

		#region State Management

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Displays a weighted reversal score (SPEC-02) for each Renko brick. No lookup table — each feature votes with points.";
				Name = "RenkoReversalScore";
				Calculate = Calculate.OnBarClose;
				IsOverlay = false;
				DisplayInDataBox = true;
				DrawOnPricePanel = false;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;

				// Parameters
				ScoreThreshold = 0.60;
				LookbackBricks = 10;
				EMAPeriodFast = 8;
				EMAPeriodSlow = 20;
				RSIPeriod = 14;
				VolumeLookback = 10;
				RSIOverbought = 65;
				RSIOversold = 35;

				// Plots
				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "Score");
				AddLine(new Stroke(Brushes.OrangeRed, DashStyleHelper.Dash, 1), 0.60, "Threshold");
				AddLine(new Stroke(Brushes.DimGray, DashStyleHelper.Dot, 1), 0.50, "Neutral");
			}
			else if (State == State.Configure)
			{
				// Add 10-second secondary data series
				AddDataSeries(BarsPeriodType.Second, 10);
			}
			else if (State == State.DataLoaded)
			{
				// Initialize indicators on secondary series (BarsArray[1])
				_emaFast = EMA(BarsArray[1], EMAPeriodFast);
				_emaSlow = EMA(BarsArray[1], EMAPeriodSlow);
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
			if (CurrentBars[0] < 2 || CurrentBars[1] < EMAPeriodSlow + 2)
			{
				Values[0][0] = 0.5;
				return;
			}

			// With Calculate.OnBarClose:
			//   Bar[0] = just-closed brick (trigger)
			//   Bar[1] = prior brick

			int dir = BrickDirection(0);
			if (dir == 0)
			{
				Values[0][0] = 0.5;
				return;
			}

			// Compute weighted score
			double rawScore;
			string breakdown;
			ComputeReversalScore(dir, out rawScore, out breakdown);

			// Normalize: [-1, 1] → [0, 1]
			double normalizedScore = (rawScore + 1.0) / 2.0;
			normalizedScore = Math.Max(0.0, Math.Min(1.0, normalizedScore));

			// Plot
			Values[0][0] = normalizedScore;

			// Update threshold line to match parameter
			Lines[0].Value = ScoreThreshold;

			// Debug text
			string debugText = string.Format("dir={0} | raw={1:F2} norm={2:F2}\n{3}",
				dir > 0 ? "UP" : "DN", rawScore, normalizedScore, breakdown);
			Draw.TextFixed(this, "DebugScore", debugText, TextPosition.BottomRight,
				Brushes.White, new SimpleFont("Consolas", 11), Brushes.Transparent, Brushes.Black, 80);
		}

		#endregion

		#region Scoring Engine

		private void ComputeReversalScore(int brickDir, out double score, out string breakdown)
		{
			score = 0;
			var parts = new List<string>();

			// --- Feature 1: Consecutive exhaustion ---
			int consec = ConsecutiveCount(0);
			if (consec >= 6)
			{
				score += 0.75;
				parts.Add("consec=" + consec + "(+0.75)");
			}
			else if (consec >= 4)
			{
				score += 0.5;
				parts.Add("consec=" + consec + "(+0.50)");
			}

			// --- Feature 2: Consecutive continuation penalty ---
			// 3+ bricks same direction without reaching exhaustion threshold
			if (consec >= 3 && consec < 4)
			{
				score -= 0.5;
				parts.Add("cont=" + consec + "(-0.50)");
			}

			// --- Feature 3: EMA divergence (slow EMA slope opposite to brick) ---
			int emaSlope = GetEMASlopeSign(_emaSlow);
			if (emaSlope != 0 && emaSlope != brickDir)
			{
				// EMA moving opposite to brick direction → reversal signal
				score += 0.5;
				parts.Add("emaDiv(+0.50)");
			}
			else if (emaSlope != 0 && emaSlope == brickDir)
			{
				// EMA aligned with brick direction → continuation signal
				score -= 0.5;
				parts.Add("emaAlign(-0.50)");
			}

			// --- Feature 4: RSI exhaustion ---
			double rsiVal = GetRSI();
			if (brickDir > 0 && rsiVal > RSIOverbought)
			{
				// Up brick + overbought → expect reversal down
				score += 0.5;
				parts.Add("rsiOB(+0.50)");
			}
			else if (brickDir < 0 && rsiVal < RSIOversold)
			{
				// Down brick + oversold → expect reversal up
				score += 0.5;
				parts.Add("rsiOS(+0.50)");
			}

			// --- Feature 5: Volume drop ---
			double volRatio = GetVolumeRatio();
			if (volRatio < 0.7)
			{
				score += 0.25;
				parts.Add("volDrop(+0.25)");
			}

			// --- Feature 6: EMA cross (fast over slow) ---
			bool emaCross = DetectEMACross(brickDir);
			if (emaCross)
			{
				score += 0.5;
				parts.Add("emaCross(+0.50)");
			}

			// Clamp to [-1, 1]
			score = Math.Max(-1.0, Math.Min(1.0, score));

			breakdown = parts.Count > 0 ? string.Join(" | ", parts) : "no signals";
		}

		#endregion

		#region Brick Utilities

		private int BrickDirection(int index)
		{
			if (Close[index] > Open[index]) return 1;
			if (Close[index] < Open[index]) return -1;
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

		private int GetEMASlopeSign(EMA ema)
		{
			if (CurrentBars[1] < EMAPeriodSlow + 3)
				return 0;

			double emaNow = ema[1];
			double emaPrev = ema[3];
			double delta = emaNow - emaPrev;

			if (delta > 0) return 1;
			if (delta < 0) return -1;
			return 0;
		}

		private double GetRSI()
		{
			if (CurrentBars[1] < RSIPeriod + 2)
				return 50.0;

			return _rsi10s[1];
		}

		private double GetVolumeRatio()
		{
			if (CurrentBars[0] < VolumeLookback + 1)
				return 1.0;

			double avgVol = 0;
			for (int i = 1; i <= VolumeLookback; i++)
				avgVol += Volume[i];
			avgVol /= VolumeLookback;

			return avgVol > 0 ? Volume[0] / avgVol : 1.0;
		}

		/// <summary>
		/// Detects if the fast EMA crossed over/under the slow EMA in a direction
		/// that supports a reversal of the current brick direction.
		/// Up brick → bearish cross (fast crossed below slow) = reversal signal
		/// Down brick → bullish cross (fast crossed above slow) = reversal signal
		/// </summary>
		private bool DetectEMACross(int brickDir)
		{
			if (CurrentBars[1] < EMAPeriodSlow + 3)
				return false;

			double fastNow = _emaFast[1];
			double slowNow = _emaSlow[1];
			double fastPrev = _emaFast[2];
			double slowPrev = _emaSlow[2];

			if (brickDir > 0)
			{
				// Up brick: bearish cross = fast crossed below slow
				return fastPrev >= slowPrev && fastNow < slowNow;
			}
			else if (brickDir < 0)
			{
				// Down brick: bullish cross = fast crossed above slow
				return fastPrev <= slowPrev && fastNow > slowNow;
			}

			return false;
		}

		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(0.01, 0.99)]
		[Display(Name = "Score Threshold", Order = 1, GroupName = "Parameters")]
		public double ScoreThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "Lookback Bricks", Order = 2, GroupName = "Parameters")]
		public int LookbackBricks { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "EMA Period Fast (10s)", Order = 3, GroupName = "Parameters")]
		public int EMAPeriodFast { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "EMA Period Slow (10s)", Order = 4, GroupName = "Parameters")]
		public int EMAPeriodSlow { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "RSI Period (10s)", Order = 5, GroupName = "Parameters")]
		public int RSIPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "Volume Lookback", Order = 6, GroupName = "Parameters")]
		public int VolumeLookback { get; set; }

		[NinjaScriptProperty]
		[Range(50, 90)]
		[Display(Name = "RSI Overbought", Order = 7, GroupName = "Parameters")]
		public double RSIOverbought { get; set; }

		[NinjaScriptProperty]
		[Range(10, 50)]
		[Display(Name = "RSI Oversold", Order = 8, GroupName = "Parameters")]
		public double RSIOversold { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> NormalizedScore
		{
			get { return Values[0]; }
		}

		#endregion
	}
}
