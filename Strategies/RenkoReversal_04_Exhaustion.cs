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
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	public class RenkoReversal_04_Exhaustion : Strategy
	{
		#region Private Fields

		private RenkoReversalProbability _probIndicator;

		#endregion

		#region State Management

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "SPEC-04: Exhaustion-based Renko reversal strategy. Only enters after a sustained directional run exceeds an exhaustion threshold. Uses rolling frequency lookup from RenkoReversalProbability indicator.";
				Name = "RenkoReversal_04_Exhaustion";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 1;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				IsInstantiatedOnEachOptimizationIteration = true;

				// Strategy parameters
				ReversalThreshold = 0.50;
				ExhaustionThreshold = 3;
				LookbackBricks = 10;
				StopLossBricks = 2.0;
				ProfitTargetBricks = 3.0;

				// Indicator parameters (passed through to RenkoReversalProbability)
				EMAPeriod = 20;
				RSIPeriod = 14;
				VolumeLookback = 10;
				VolumeHighRatio = 1.5;
				VolumeLowRatio = 0.7;
				RSIOverbought = 65;
				RSIOversold = 35;
				MinHistory = 5;
			}
			else if (State == State.Configure)
			{
				// The strategy must load the 10-second secondary series here —
				// hosted indicators cannot add their own data series in NT8.
				// The indicator will detect this series already exists and use it.
				AddDataSeries(BarsPeriodType.Second, 10);
			}
			else if (State == State.DataLoaded)
			{
				// Instantiate the probability indicator with exhaustion filtering enabled
				_probIndicator = RenkoReversalProbability(ReversalThreshold, LookbackBricks,
					ExhaustionThreshold, EMAPeriod, RSIPeriod, VolumeLookback,
					VolumeHighRatio, VolumeLowRatio, RSIOverbought, RSIOversold, MinHistory);

				// Show the indicator on the chart
				AddChartIndicator(_probIndicator);
			}
		}

		#endregion

		#region OnBarUpdate

		protected override void OnBarUpdate()
		{
			// Only process primary Renko series
			if (BarsInProgress != 0)
				return;

			// Need enough history
			if (CurrentBar < BarsRequiredToTrade)
				return;

			// Read probability from the indicator
			double prob = _probIndicator.Probability[0];

			// Brick direction of the just-closed brick
			int dir = BrickDirection(0);
			if (dir == 0)
				return;

			// --- Exhaustion gate ---
			int consec = ConsecutiveCount(0);
			if (consec < ExhaustionThreshold)
				return;

			// --- Probability threshold ---
			if (prob < ReversalThreshold)
				return;

			// --- Position check: only enter when flat ---
			if (Position.MarketPosition != MarketPosition.Flat)
				return;

			// --- Entry ---
			double brickSize = Math.Abs(Close[0] - Open[0]);
			if (brickSize <= 0)
				return;

			double slDistance = StopLossBricks * brickSize;
			double ptDistance = ProfitTargetBricks * brickSize;

			if (dir > 0)
			{
				// Last brick was UP → expect reversal DOWN → enter Short
				SetStopLoss("ShortEntry", CalculationMode.Ticks, slDistance / TickSize, false);
				SetProfitTarget("ShortEntry", CalculationMode.Ticks, ptDistance / TickSize);
				EnterShort(DefaultQuantity, "ShortEntry");
			}
			else
			{
				// Last brick was DOWN → expect reversal UP → enter Long
				SetStopLoss("LongEntry", CalculationMode.Ticks, slDistance / TickSize, false);
				SetProfitTarget("LongEntry", CalculationMode.Ticks, ptDistance / TickSize);
				EnterLong(DefaultQuantity, "LongEntry");
			}
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
			int maxLookback = Math.Min(startIndex + LookbackBricks, CurrentBar);

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

		#region Properties — Strategy

		[NinjaScriptProperty]
		[Range(0.01, 0.99)]
		[Display(Name = "Reversal Threshold", Description = "Minimum P(reversal) to enter", Order = 1, GroupName = "1. Strategy")]
		public double ReversalThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Exhaustion Threshold", Description = "Minimum consecutive bricks before reversal is considered", Order = 2, GroupName = "1. Strategy")]
		public int ExhaustionThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "Lookback Bricks", Order = 3, GroupName = "1. Strategy")]
		public int LookbackBricks { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 10.0)]
		[Display(Name = "Stop Loss (Bricks)", Order = 4, GroupName = "1. Strategy")]
		public double StopLossBricks { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 20.0)]
		[Display(Name = "Profit Target (Bricks)", Order = 5, GroupName = "1. Strategy")]
		public double ProfitTargetBricks { get; set; }

		#endregion

		#region Properties — Indicator Pass-Through

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "EMA Period (10s)", Order = 1, GroupName = "2. Indicator")]
		public int EMAPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "RSI Period (10s)", Order = 2, GroupName = "2. Indicator")]
		public int RSIPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(2, 50)]
		[Display(Name = "Volume Lookback", Order = 3, GroupName = "2. Indicator")]
		public int VolumeLookback { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 5.0)]
		[Display(Name = "Volume High Ratio", Order = 4, GroupName = "2. Indicator")]
		public double VolumeHighRatio { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume Low Ratio", Order = 5, GroupName = "2. Indicator")]
		public double VolumeLowRatio { get; set; }

		[NinjaScriptProperty]
		[Range(50, 90)]
		[Display(Name = "RSI Overbought", Order = 6, GroupName = "2. Indicator")]
		public double RSIOverbought { get; set; }

		[NinjaScriptProperty]
		[Range(10, 50)]
		[Display(Name = "RSI Oversold", Order = 7, GroupName = "2. Indicator")]
		public double RSIOversold { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Min History", Description = "Minimum samples before using bucket probability", Order = 8, GroupName = "2. Indicator")]
		public int MinHistory { get; set; }

		#endregion
	}
}
