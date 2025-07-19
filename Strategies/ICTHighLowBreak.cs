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

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.PriceAction
{
	public class ICTHighLowBreak : Strategy
	{
		bool _breakHigh = false;
		double _breakHighBar;
		double _breakHighStructurePrice;
		bool _breakHighStructure = false;
		bool _tradedHighBreak;
		
		bool _breakLow = false;
		double _breakLowBar;
		double _breakLowStructurePrice;
		bool _breakLowStructure = false;
		bool _tradedLowBreak;
		
		CurrentDayOHL today;
		PriorDayOHLC daily;
		ZigZag zigzag;
		FairValueGap gaps;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "ICTHighLowBreak";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				RFactor = 1;
				Lookback = 10;
				Gap = 1;
			}
			else if (State == State.Configure)
			{
				daily = PriorDayOHLC();
				today = CurrentDayOHL();
				zigzag = ZigZag(DeviationType.Points, 0.5, true);
				gaps = FairValueGap(Gap);
				
				AddChartIndicator(daily);
				AddChartIndicator(zigzag);
			}
		}

		protected override void OnBarUpdate()
		{
			if (Bars.BarsSinceNewTradingDay == 0)
			{
				_breakHigh = false;
				_breakHighStructure = false;
				_breakLow = false;
				_breakLowStructure = false;
				_tradedLowBreak = false;
				_tradedHighBreak = false;
			}
			
			if (today.Open[0] < daily.PriorHigh[0] && !_tradedHighBreak)
			{
				if (!_breakHigh && CrossAbove(Close, daily.PriorHigh, 1))
				{
					int lastpivot = zigzag.LowBar(1, 1, Lookback);
					if(lastpivot > 0)
					{
						_breakHigh = true;				
						_breakHighStructurePrice = Low[lastpivot];
						_breakHighBar = CurrentBar;
					}
				}
				
				if (_breakHigh && Close[0] < _breakHighStructurePrice && CurrentBar > 3)
				{
					double foo = gaps[0];
					if(gaps.LastDownGap() > _breakHighBar)
					{
						_breakHighStructure = true;
						double entryPrice = gaps.LastDownGapPrice();
						int lastHigh = zigzag.HighBar(1, 1, Lookback);
						if(lastHigh > 0)
						{
							double stopprice = High[lastHigh];
							double stop = (stopprice - entryPrice) / TickSize;
							
							//double target = entryPrice - ((stop - entryPrice) * 2);
						
							EnterShortLimit(entryPrice);
							SetStopLoss(CalculationMode.Ticks, stop);
							SetProfitTarget(CalculationMode.Ticks, stop * RFactor);
							//SetStopLoss(CalculationMode.Ticks, stop);
							//SetProfitTarget(CalculationMode.Price, target);
						}
					}
				}
			}
			
			
			if (today.Open[0] > daily.PriorLow[0] && !_tradedLowBreak)
			{
				if (!_breakLow && CrossBelow(Close, daily.PriorLow, 1))
				{
					int lastpivot = zigzag.HighBar(1, 1, Lookback);
					if(lastpivot > 0)
					{
						_breakLow = true;				
						_breakLowStructurePrice = High[lastpivot];
						_breakLowBar = CurrentBar;
					}
				}
				
				if (_breakLow && Close[0] > _breakLowStructurePrice && CurrentBar > 3)
				{
					double foo = gaps[0];
					if(gaps.LastUpGap() > _breakLowBar)
					{
						_breakLowStructure = true;
						double entryPrice = gaps.LastUpGapPrice();
						int lastLow = zigzag.LowBar(1, 1, Lookback);
						if(lastLow > 0)
						{
							double stopprice = Low[lastLow];
							double stop = (entryPrice - stopprice) / TickSize;
							//double target = entryPrice + ((entryPrice - stop) * 2);
						
							EnterLongLimit(entryPrice);
							SetStopLoss(CalculationMode.Ticks, stop);
							SetProfitTarget(CalculationMode.Ticks, stop * RFactor);
							//SetStopLoss(CalculationMode.Price, stop);
							//SetProfitTarget(CalculationMode.Price, target);
						}
					}
				}
			}
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (marketPosition == MarketPosition.Long)
				_tradedLowBreak = true;
			
			if (marketPosition == MarketPosition.Short)
				_tradedHighBreak = true;
		}
		
		[NinjaScriptProperty]
		public int Lookback
		{ get; set; }
		
		[NinjaScriptProperty]
		public int Gap
		{ get; set; }
		
		[NinjaScriptProperty]
		public double RFactor
		{get; set; }
	}
}
