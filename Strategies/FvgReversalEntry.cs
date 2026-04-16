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
	/// Scenario B: FVG Reversal Bar + Favorable Close Entry.
	/// Trades Fair Value Gap closures on 30-second NQ futures bars.
	/// Qualifies FVGs by size (ATR-normalized), volume ratio, and reversal bar,
	/// then waits up to 3 bars for a first favorable close before entering.
	/// Target = TargetAtrMultiplier * ATR from entry close. Stop = 0.5 ATR beyond opposite FVG edge.
	/// </summary>
	public class FvgReversalEntry : Strategy
	{
		#region FVG Record
		private class FvgRecord
		{
			public int FormationBar;
			public double FvgTop;
			public double FvgBottom;
			public double FvgSizeAtr;
			public bool IsBullish;
			public bool IsQualified;
			public int EntryWaitBarsLeft;
			public bool EntryTriggered;
			public int EntryBar;
			public bool IsActive;
			public double StopPrice;
			public double TargetPrice;
		}
		#endregion

		#region Variables
		private List<FvgRecord> activeFvgs;
		private ATR atrIndicator;
		private SMA volmaIndicator;
		private FvgRecord currentTradeFvg;
		private int positionEntryBar;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Scenario B: FVG Reversal Bar + Favorable Close Entry Strategy";
				Name						= "FvgReversalEntry";
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

				// Strategy parameters
				MaxFvgSizeAtr				= 0.3;
				MaxVolumeRatio				= 1.0;
				RequireReversalBar			= true;
				EntryWaitBars				= 3;
				StopAtrMultiplier			= 0.5;
				TargetAtrMultiplier			= 1.0;
				MaxBarsInTrade				= 20;
				AtrPeriod					= 14;
				VolmaPeriod					= 14;
				Quantity					= 1;
				MaxOpenFvgsTracked			= 50;
				EnableLogging				= true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				atrIndicator = ATR(AtrPeriod);
				volmaIndicator = SMA(Volume, VolmaPeriod);
				activeFvgs = new List<FvgRecord>();
				currentTradeFvg = null;
			}
			else if (State == State.Transition)
			{
				// Clear stale FVGs when transitioning from historical to realtime
				if (activeFvgs != null)
					activeFvgs.Clear();
				currentTradeFvg = null;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;

			double atrValue = atrIndicator[0];
			if (atrValue < 0.01)
				return;

			double volmaValue = volmaIndicator[0];

			// Step 1: Detect and qualify new FVGs
			DetectFvgs(atrValue, volmaValue);

			// Step 2: Check closure of open FVGs
			CheckFvgClosures();

			// Step 3: Check entry triggers for qualified FVGs (only if flat)
			if (Position.MarketPosition == MarketPosition.Flat)
				CheckEntryTriggers(atrValue);

			// Step 4: Time-based exit for open positions
			CheckTimeExit();

			// Step 5: Cleanup old FVGs
			CleanupFvgs();
		}

		#region Step 1 — Detect FVGs
		private void DetectFvgs(double atrValue, double volmaValue)
		{
			if (CurrentBar < 2)
				return;

			// Bullish FVG: High[2] < Low[0]
			if (High[2] < Low[0])
			{
				double top = Low[0];
				double bottom = High[2];
				double sizeAtr = (top - bottom) / atrValue;
				double volumeRatio = volmaValue > 0 ? Volume[0] / volmaValue : 999;
				bool isReversal = Close[0] < Open[0];
				bool qualified = (sizeAtr <= MaxFvgSizeAtr)
					&& (volumeRatio < MaxVolumeRatio)
					&& (!RequireReversalBar || isReversal);

				var fvg = new FvgRecord
				{
					FormationBar		= CurrentBar,
					FvgTop				= top,
					FvgBottom			= bottom,
					FvgSizeAtr			= sizeAtr,
					IsBullish			= true,
					IsQualified			= qualified,
					EntryWaitBarsLeft	= EntryWaitBars,
					EntryTriggered		= false,
					EntryBar			= 0,
					IsActive			= true,
					StopPrice			= 0,
					TargetPrice			= 0
				};
				activeFvgs.Add(fvg);

				if (EnableLogging)
					Print(string.Format("Bar {0} | Bullish FVG: top={1:F2} bot={2:F2} sizeATR={3:F3} volRatio={4:F2} reversal={5} qualified={6}",
						CurrentBar, top, bottom, sizeAtr, volumeRatio, isReversal, qualified));
			}

			// Bearish FVG: Low[2] > High[0]
			if (Low[2] > High[0])
			{
				double top = Low[2];
				double bottom = High[0];
				double sizeAtr = (top - bottom) / atrValue;
				double volumeRatio = volmaValue > 0 ? Volume[0] / volmaValue : 999;
				bool isReversal = Close[0] > Open[0];
				bool qualified = (sizeAtr <= MaxFvgSizeAtr)
					&& (volumeRatio < MaxVolumeRatio)
					&& (!RequireReversalBar || isReversal);

				var fvg = new FvgRecord
				{
					FormationBar		= CurrentBar,
					FvgTop				= top,
					FvgBottom			= bottom,
					FvgSizeAtr			= sizeAtr,
					IsBullish			= false,
					IsQualified			= qualified,
					EntryWaitBarsLeft	= EntryWaitBars,
					EntryTriggered		= false,
					EntryBar			= 0,
					IsActive			= true,
					StopPrice			= 0,
					TargetPrice			= 0
				};
				activeFvgs.Add(fvg);

				if (EnableLogging)
					Print(string.Format("Bar {0} | Bearish FVG: top={1:F2} bot={2:F2} sizeATR={3:F3} volRatio={4:F2} reversal={5} qualified={6}",
						CurrentBar, top, bottom, sizeAtr, volumeRatio, isReversal, qualified));
			}

			// Cap list size
			while (activeFvgs.Count > MaxOpenFvgsTracked)
				activeFvgs.RemoveAt(0);
		}
		#endregion

		#region Step 2 — Check FVG Closures
		private void CheckFvgClosures()
		{
			foreach (var fvg in activeFvgs)
			{
				if (!fvg.IsActive)
					continue;

				if (fvg.IsBullish && Low[0] <= fvg.FvgBottom)
				{
					fvg.IsActive = false;
				}
				else if (!fvg.IsBullish && High[0] >= fvg.FvgTop)
				{
					fvg.IsActive = false;
				}
			}
		}
		#endregion

		#region Step 3 — Check Entry Triggers
		private void CheckEntryTriggers(double atrValue)
		{
			foreach (var fvg in activeFvgs)
			{
				if (!fvg.IsActive || !fvg.IsQualified || fvg.EntryTriggered)
					continue;

				// Skip the formation bar itself
				if (CurrentBar == fvg.FormationBar)
					continue;

				// Check if FVG closed before we could enter
				if (!fvg.IsActive)
					continue;

				// Check for favorable close
				bool favorableClose = fvg.IsBullish
					? (Close[0] < Open[0])   // bearish close for bullish FVG
					: (Close[0] > Open[0]);   // bullish close for bearish FVG

				if (favorableClose)
				{
					fvg.EntryTriggered = true;
					fvg.EntryBar = CurrentBar;

					if (fvg.IsBullish)
					{
						// Bullish FVG → enter SHORT (price drops to close gap)
						fvg.TargetPrice = Close[0] - (TargetAtrMultiplier * atrValue);
						fvg.StopPrice   = fvg.FvgTop + (StopAtrMultiplier * atrValue);
						SubmitShortEntry(fvg);
					}
					else
					{
						// Bearish FVG → enter LONG (price rises to close gap)
						fvg.TargetPrice = Close[0] + (TargetAtrMultiplier * atrValue);
						fvg.StopPrice   = fvg.FvgBottom - (StopAtrMultiplier * atrValue);
						SubmitLongEntry(fvg);
					}
					return; // Only one entry per bar
				}
				else
				{
					fvg.EntryWaitBarsLeft--;
					if (fvg.EntryWaitBarsLeft <= 0)
					{
						fvg.IsQualified = false; // Expired — no favorable close in time
						if (EnableLogging)
							Print(string.Format("Bar {0} | FVG from bar {1} expired (no favorable close in {2} bars)",
								CurrentBar, fvg.FormationBar, EntryWaitBars));
					}
				}
			}
		}
		#endregion

		#region Order Submission
		private void SubmitShortEntry(FvgRecord fvg)
		{
			currentTradeFvg = fvg;
			positionEntryBar = CurrentBar;

			SetStopLoss("FVG_B", CalculationMode.Price, fvg.StopPrice, false);
			SetProfitTarget("FVG_B", CalculationMode.Price, fvg.TargetPrice);
			EnterShort(Quantity, "FVG_B");

			if (EnableLogging)
				Print(string.Format("Bar {0} | SHORT entry: target={1:F2} stop={2:F2} (Bullish FVG from bar {3})",
					CurrentBar, fvg.TargetPrice, fvg.StopPrice, fvg.FormationBar));
		}

		private void SubmitLongEntry(FvgRecord fvg)
		{
			currentTradeFvg = fvg;
			positionEntryBar = CurrentBar;

			SetStopLoss("FVG_B", CalculationMode.Price, fvg.StopPrice, false);
			SetProfitTarget("FVG_B", CalculationMode.Price, fvg.TargetPrice);
			EnterLong(Quantity, "FVG_B");

			if (EnableLogging)
				Print(string.Format("Bar {0} | LONG entry: target={1:F2} stop={2:F2} (Bearish FVG from bar {3})",
					CurrentBar, fvg.TargetPrice, fvg.StopPrice, fvg.FormationBar));
		}
		#endregion

		#region Step 4 — Time-Based Exit
		private void CheckTimeExit()
		{
			if (Position.MarketPosition == MarketPosition.Flat)
				return;

			int barsInTrade = CurrentBar - positionEntryBar;
			if (barsInTrade >= MaxBarsInTrade)
			{
				if (EnableLogging)
					Print(string.Format("Bar {0} | Time exit after {1} bars in trade", CurrentBar, barsInTrade));

				if (Position.MarketPosition == MarketPosition.Long)
					ExitLong("TimeExit", "FVG_B");
				else if (Position.MarketPosition == MarketPosition.Short)
					ExitShort("TimeExit", "FVG_B");

				currentTradeFvg = null;
			}
		}
		#endregion

		#region Step 5 — Cleanup
		private void CleanupFvgs()
		{
			activeFvgs.RemoveAll(f => !f.IsActive || (CurrentBar - f.FormationBar > 500));
		}
		#endregion

		#region OnExecutionUpdate
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity,
			MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (execution.Order != null && execution.Order.OrderState == OrderState.Filled)
			{
				// If we just went flat, clear trade state
				if (Position.MarketPosition == MarketPosition.Flat && currentTradeFvg != null)
				{
					string reason = execution.Order.Name == "FVG_B" ? "entry" :
						execution.Order.Name.Contains("Profit") ? "target" :
						execution.Order.Name.Contains("Stop") ? "stop" :
						execution.Order.Name;

					if (EnableLogging)
						Print(string.Format("Bar {0} | Exit ({1}): price={2:F2} PnL={3:F2}",
							CurrentBar, reason, price, execution.Order.AverageFillPrice));

					currentTradeFvg = null;
				}
			}
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Max FVG Size (ATR)", Description = "Maximum FVG size in ATR units to qualify", Order = 1, GroupName = "Filters")]
		public double MaxFvgSizeAtr { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Max Volume Ratio", Description = "Maximum volume/VOLMA ratio to qualify", Order = 2, GroupName = "Filters")]
		public double MaxVolumeRatio { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Reversal Bar", Description = "Require the FVG bar to be a reversal bar", Order = 3, GroupName = "Filters")]
		public bool RequireReversalBar { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Entry Wait Bars", Description = "Max bars to wait for favorable close after FVG formation", Order = 4, GroupName = "Entry")]
		public int EntryWaitBars { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Stop ATR Multiplier", Description = "Stop distance in ATR units beyond opposite FVG edge", Order = 5, GroupName = "Entry")]
		public double StopAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name = "Target ATR Multiplier", Description = "Profit target distance in ATR units from entry close", Order = 6, GroupName = "Entry")]
		public double TargetAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Bars In Trade", Description = "Time-based exit after N bars", Order = 7, GroupName = "Exit")]
		public int MaxBarsInTrade { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR Period", Order = 7, GroupName = "Indicators")]
		public int AtrPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "VOLMA Period", Description = "Volume moving average period", Order = 8, GroupName = "Indicators")]
		public int VolmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Quantity", Description = "Contracts per trade", Order = 9, GroupName = "Trade")]
		public int Quantity { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Open FVGs Tracked", Description = "Limit FVG list size for performance", Order = 10, GroupName = "Trade")]
		public int MaxOpenFvgsTracked { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Logging", Description = "Print FVG and trade events to output window", Order = 11, GroupName = "Debug")]
		public bool EnableLogging { get; set; }
		#endregion
	}
}
