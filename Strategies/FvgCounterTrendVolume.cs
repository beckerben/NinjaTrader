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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	/// <summary>
	/// Scenario G-Volume: Range Chart Counter-Trend Volume Exhaustion Strategy.
	/// Trades counter-trend reversals on 16-tick range NQ bars.
	/// Uses below-average volume on FVG-forming bars as the primary exhaustion signal.
	/// Optional CCI+RSI overlay for combined filtering.
	/// </summary>
	public class FvgCounterTrendVolume : Strategy
	{
		#region Inner Classes
		private class FvgRecord
		{
			public int FormationBar;
			public double FvgTop;
			public double FvgBottom;
			public bool IsBullish;
			public int RunPosition;
			public double VolumeRatio;
			public bool IsClosed;
		}

		private class RunTracker
		{
			public bool IsBullish;
			public List<FvgRecord> FvgsInRun = new List<FvgRecord>();
			public int Length { get { return FvgsInRun.Count; } }
		}
		#endregion

		#region Variables
		private ATR atrIndicator;
		private SMA volmaIndicator;
		private CCI cciIndicator;
		private RSI rsiIndicator;
		private RunTracker currentRun;
		private List<FvgRecord> openFvgs;
		private int positionEntryBar;
		private bool runInitialized;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Scenario G-Volume: Counter-trend volume exhaustion on FVG runs";
				Name						= "FvgCounterTrendVolume";
				Calculate					= Calculate.OnBarClose;
				EntriesPerDirection			= 1;
				EntryHandling				= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds	= 30;
				IsFillLimitOnTouch			= false;
				MaximumBarsLookBack			= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution			= OrderFillResolution.Standard;
				Slippage					= 1;
				StartBehavior				= StartBehavior.WaitUntilFlat;
				TimeInForce					= TimeInForce.Gtc;
				TraceOrders					= false;
				RealtimeErrorHandling		= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling			= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade			= 20;
				IsInstantiatedOnEachOptimizationIteration = true;

				// Volume Filter
				MaxVolumeRatio				= 0.8;
				VolmaPeriod					= 14;

				// CCI/RSI Overlay (off by default)
				UseCciRsiOverlay			= false;
				CciThreshold				= 100;
				RsiThreshold				= 70;

				// Entry
				MinRunLength				= 3;
				MaxEntryPosition			= 4;

				// Risk
				StopAtrMultiplier			= 1.5;
				MinRiskReward				= 2.0;
				MaxBarsInTrade				= 50;

				// Target
				TargetFvgsBack				= 2;

				// Indicators
				AtrPeriod					= 14;
				CciPeriod					= 14;
				RsiPeriod					= 14;
				RsiSmoothing				= 3;

				// General
				Quantity					= 1;
				MaxOpenFvgsTracked			= 100;
				EnableLogging				= true;
			}
			else if (State == State.DataLoaded)
			{
				atrIndicator = ATR(AtrPeriod);
				volmaIndicator = SMA(Volume, VolmaPeriod);

				if (UseCciRsiOverlay)
				{
					cciIndicator = CCI(CciPeriod);
					rsiIndicator = RSI(RsiPeriod, RsiSmoothing);
				}

				openFvgs = new List<FvgRecord>();
				currentRun = null;
				runInitialized = false;
			}
			else if (State == State.Transition)
			{
				openFvgs?.Clear();
				currentRun = null;
				runInitialized = false;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;

			double atrValue = atrIndicator[0];
			if (atrValue < 0.25)
				return;

			double volmaValue = volmaIndicator[0];
			if (volmaValue < 1)
				return;

			// Step 1 & 2: Detect FVGs and update run tracker
			bool newFvgDetected = DetectAndTrackFvgs(volmaValue);

			// Step 3: Check FVG closures
			CheckFvgClosures();

			// Step 4: Check entry conditions
			if (newFvgDetected && Position.MarketPosition == MarketPosition.Flat)
				CheckEntryConditions(atrValue, volmaValue);

			// Step 5: Time-based exit
			CheckTimeExit();

			// Step 6: Cleanup
			CleanupFvgs();
		}

		#region FVG Detection and Run Tracking
		private bool DetectAndTrackFvgs(double volmaValue)
		{
			if (CurrentBar < 2)
				return false;

			bool bullishFvg = High[2] < Low[0];
			bool bearishFvg = Low[2] > High[0];

			if (!bullishFvg && !bearishFvg)
				return false;

			FvgRecord firstFvg = null;
			FvgRecord secondFvg = null;

			if (bullishFvg && bearishFvg)
			{
				var bullRecord = CreateFvgRecord(true, volmaValue);
				var bearRecord = CreateFvgRecord(false, volmaValue);

				if (currentRun != null && currentRun.IsBullish)
				{
					firstFvg = bullRecord;
					secondFvg = bearRecord;
				}
				else
				{
					firstFvg = bearRecord;
					secondFvg = bullRecord;
				}
			}
			else if (bullishFvg)
			{
				firstFvg = CreateFvgRecord(true, volmaValue);
			}
			else
			{
				firstFvg = CreateFvgRecord(false, volmaValue);
			}

			AddFvgToRun(firstFvg);
			openFvgs.Add(firstFvg);

			if (secondFvg != null)
			{
				AddFvgToRun(secondFvg);
				openFvgs.Add(secondFvg);
			}

			return true;
		}

		private FvgRecord CreateFvgRecord(bool isBullish, double volmaValue)
		{
			return new FvgRecord
			{
				FormationBar = CurrentBar,
				FvgTop       = isBullish ? Low[0] : Low[2],
				FvgBottom    = isBullish ? High[2] : High[0],
				IsBullish    = isBullish,
				RunPosition  = 0,
				VolumeRatio  = Volume[0] / volmaValue,
				IsClosed     = false
			};
		}

		private void AddFvgToRun(FvgRecord fvg)
		{
			if (!runInitialized || currentRun == null)
			{
				currentRun = new RunTracker { IsBullish = fvg.IsBullish };
				currentRun.FvgsInRun.Add(fvg);
				fvg.RunPosition = 1;
				runInitialized = true;
			}
			else if (currentRun.IsBullish == fvg.IsBullish)
			{
				currentRun.FvgsInRun.Add(fvg);
				fvg.RunPosition = currentRun.Length;
			}
			else
			{
				currentRun = new RunTracker { IsBullish = fvg.IsBullish };
				currentRun.FvgsInRun.Add(fvg);
				fvg.RunPosition = 1;
			}

			if (EnableLogging)
				Print(string.Format("Bar {0} | {1} FVG: top={2:F2} bot={3:F2} runPos={4} runLen={5} volRatio={6:F2}",
					CurrentBar, fvg.IsBullish ? "Bull" : "Bear", fvg.FvgTop, fvg.FvgBottom,
					fvg.RunPosition, currentRun.Length, fvg.VolumeRatio));
		}
		#endregion

		#region FVG Closure Check
		private void CheckFvgClosures()
		{
			foreach (var fvg in openFvgs)
			{
				if (fvg.IsClosed)
					continue;

				if (fvg.IsBullish && Low[0] <= fvg.FvgBottom)
					fvg.IsClosed = true;
				else if (!fvg.IsBullish && High[0] >= fvg.FvgTop)
					fvg.IsClosed = true;
			}
		}
		#endregion

		#region Entry Logic
		private void CheckEntryConditions(double atrValue, double volmaValue)
		{
			if (currentRun == null)
				return;

			int runLength = currentRun.Length;

			if (runLength < MinRunLength || runLength > MaxEntryPosition)
				return;

			// Volume exhaustion check
			double currentVolumeRatio = Volume[0] / volmaValue;
			if (currentVolumeRatio >= MaxVolumeRatio)
			{
				if (EnableLogging)
					Print(string.Format("Bar {0} | Volume filter FAIL: ratio={1:F2} (need<{2:F1})",
						CurrentBar, currentVolumeRatio, MaxVolumeRatio));
				return;
			}

			// Optional CCI/RSI overlay
			double dirCci = 0, dirRsi = 0;
			if (UseCciRsiOverlay)
			{
				if (currentRun.IsBullish)
				{
					dirCci = cciIndicator[0];
					dirRsi = rsiIndicator[0];
				}
				else
				{
					dirCci = -cciIndicator[0];
					dirRsi = 100.0 - rsiIndicator[0];
				}

				if (dirCci <= CciThreshold || dirRsi <= RsiThreshold)
				{
					if (EnableLogging)
						Print(string.Format("Bar {0} | CCI/RSI overlay FAIL: dirCCI={1:F1} dirRSI={2:F1}",
							CurrentBar, dirCci, dirRsi));
					return;
				}
			}

			// Target FVG
			int targetPosition = runLength - TargetFvgsBack;
			if (targetPosition < 1)
				return;

			FvgRecord targetFvg = currentRun.FvgsInRun[targetPosition - 1];
			if (targetFvg.IsClosed)
			{
				if (EnableLogging)
					Print(string.Format("Bar {0} | Target FVG at position {1} already closed, skip", CurrentBar, targetPosition));
				return;
			}

			// Calculate prices
			double entryPrice = Close[0];
			double targetPrice, stopPrice;

			if (currentRun.IsBullish)
			{
				targetPrice = targetFvg.FvgBottom;
				stopPrice   = entryPrice + (StopAtrMultiplier * atrValue);
			}
			else
			{
				targetPrice = targetFvg.FvgTop;
				stopPrice   = entryPrice - (StopAtrMultiplier * atrValue);
			}

			double reward = Math.Abs(entryPrice - targetPrice);
			double risk   = Math.Abs(entryPrice - stopPrice);

			if (risk < 0.01)
				return;

			double rr = reward / risk;

			if (rr < MinRiskReward)
			{
				if (EnableLogging)
					Print(string.Format("Bar {0} | R:R too low: {1:F2} (need>={2:F1})", CurrentBar, rr, MinRiskReward));
				return;
			}

			// Submit order
			positionEntryBar = CurrentBar;

			if (currentRun.IsBullish)
			{
				SetStopLoss("G_Vol", CalculationMode.Price, stopPrice, false);
				SetProfitTarget("G_Vol", CalculationMode.Price, targetPrice);
				EnterShort(Quantity, "G_Vol");

				if (EnableLogging)
					Print(string.Format("Bar {0} | SHORT entry: price={1:F2} target={2:F2} stop={3:F2} R:R={4:F2} runLen={5} volRatio={6:F2}",
						CurrentBar, entryPrice, targetPrice, stopPrice, rr, runLength, currentVolumeRatio));
			}
			else
			{
				SetStopLoss("G_Vol", CalculationMode.Price, stopPrice, false);
				SetProfitTarget("G_Vol", CalculationMode.Price, targetPrice);
				EnterLong(Quantity, "G_Vol");

				if (EnableLogging)
					Print(string.Format("Bar {0} | LONG entry: price={1:F2} target={2:F2} stop={3:F2} R:R={4:F2} runLen={5} volRatio={6:F2}",
						CurrentBar, entryPrice, targetPrice, stopPrice, rr, runLength, currentVolumeRatio));
			}
		}
		#endregion

		#region Time-Based Exit
		private void CheckTimeExit()
		{
			if (Position.MarketPosition == MarketPosition.Flat)
				return;

			int barsInTrade = CurrentBar - positionEntryBar;
			if (barsInTrade >= MaxBarsInTrade)
			{
				if (EnableLogging)
					Print(string.Format("Bar {0} | Time exit after {1} bars", CurrentBar, barsInTrade));

				if (Position.MarketPosition == MarketPosition.Long)
					ExitLong("TimeExit", "G_Vol");
				else if (Position.MarketPosition == MarketPosition.Short)
					ExitShort("TimeExit", "G_Vol");
			}
		}
		#endregion

		#region Cleanup
		private void CleanupFvgs()
		{
			while (openFvgs.Count > MaxOpenFvgsTracked)
				openFvgs.RemoveAt(0);

			openFvgs.RemoveAll(f => f.IsClosed && (CurrentBar - f.FormationBar > 200));
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Max Volume Ratio", Description = "Volume/VOLMA must be below this (lower = stricter)", Order = 1, GroupName = "Volume Filter")]
		public double MaxVolumeRatio { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "VOLMA Period", Description = "Volume moving average lookback", Order = 2, GroupName = "Volume Filter")]
		public int VolmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use CCI/RSI Overlay", Description = "Add CCI+RSI exhaustion on top of volume filter", Order = 1, GroupName = "CCI/RSI Overlay")]
		public bool UseCciRsiOverlay { get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "CCI Threshold", Description = "Directional CCI threshold (only if overlay enabled)", Order = 2, GroupName = "CCI/RSI Overlay")]
		public double CciThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "RSI Threshold", Description = "Directional RSI threshold (only if overlay enabled)", Order = 3, GroupName = "CCI/RSI Overlay")]
		public double RsiThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Min Run Length", Description = "Minimum FVGs in run before entry allowed", Order = 1, GroupName = "Entry")]
		public int MinRunLength { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Entry Position", Description = "Maximum run position to consider entry", Order = 2, GroupName = "Entry")]
		public int MaxEntryPosition { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Stop ATR Multiplier", Description = "Stop distance in ATR units beyond entry", Order = 1, GroupName = "Risk")]
		public double StopAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Min Risk:Reward", Description = "Minimum R:R ratio to accept trade", Order = 2, GroupName = "Risk")]
		public double MinRiskReward { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Bars in Trade", Description = "Time-based exit after N bars", Order = 3, GroupName = "Risk")]
		public int MaxBarsInTrade { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Target FVGs Back", Description = "How many FVGs back from entry to set target", Order = 1, GroupName = "Target")]
		public int TargetFvgsBack { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR Period", Order = 1, GroupName = "Indicators")]
		public int AtrPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "CCI Period", Order = 2, GroupName = "Indicators")]
		public int CciPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "RSI Period", Order = 3, GroupName = "Indicators")]
		public int RsiPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "RSI Smoothing", Order = 4, GroupName = "Indicators")]
		public int RsiSmoothing { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Quantity", Description = "Contracts per trade", Order = 1, GroupName = "General")]
		public int Quantity { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Open FVGs Tracked", Description = "Limit FVG list size for performance", Order = 2, GroupName = "General")]
		public int MaxOpenFvgsTracked { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Logging", Description = "Print FVG and trade events to output window", Order = 3, GroupName = "General")]
		public bool EnableLogging { get; set; }
		#endregion
	}
}
