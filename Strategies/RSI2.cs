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
namespace NinjaTrader.NinjaScript.Strategies
{
	public class RSI2 : Strategy
	{
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"from https://www.tradingview.com/script/xOm7jSPf-CM-RSI-2-Strategy-Lower-Indicator/ ";
				Name										= "RSI-2";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= false;
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
				
				rsiLow = 10;
				rsiHigh = 90;
				shortSMA = 5;
				longSMA = 200;
	
				profitTaker = 1000;
				stopLoss = 1000;
				
				
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
				SetStopLoss(CalculationMode.Ticks, stopLoss);
			}
			else if (State == State.DataLoaded)
			{
				AddChartIndicator(SMA(shortSMA));	
				AddChartIndicator(SMA(longSMA));	
				AddChartIndicator(RSI(2,1));
				SMA(shortSMA).Plots[0].Brush = Brushes.Red;
				SMA(longSMA).Plots[0].Brush = Brushes.LimeGreen;		

				
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			

			//Entry Rules:
			//Buy Only When Stock is Above 200-SMA, AND Below 5-Day SMA, With RSI Below 10
			//Short Only when Stock is Below 200-SMA, AND Above 5-Day SMA, With RSI Above 90

			//Exit Criteria:
			//If Buying EXIT when Price goes Above 5 SMA.
			//If Selling Exit when Price Goes Below 5 SMA.
			//***The thought process is that the security has “Pulled Back” from it’s Major Trend - And will Continue The Major Trend By Crossing the 5 SMA in the direction of the Major Tend.
						
			if (Close[0] > SMA(longSMA)[0] && Close[0] < SMA(shortSMA)[0] && RSI(2,1)[0] < rsiLow)
					EnterLong();
			
			if (Close[0] < SMA(longSMA)[0] && Close[0] > SMA(shortSMA)[0] && RSI(2,1)[0] > rsiHigh)
					EnterShort();
			
			if (Position.MarketPosition == MarketPosition.Long && Close[0] > SMA(shortSMA)[0])
					ExitLong();
			
			if (Position.MarketPosition == MarketPosition.Short && Close[0] < SMA(shortSMA)[0])
					ExitShort();
			
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
				
		
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="RSI Low", Description="", Order=1, GroupName="Parameters")]
			public int rsiLow
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="RSI High", Description="", Order=1, GroupName="Parameters")]
			public int rsiHigh
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Short SMA", Description="", Order=1, GroupName="Parameters")]
			public int shortSMA
			{ get; set; }

			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Long SMA", Description="", Order=1, GroupName="Parameters")]
			public int longSMA
			{ get; set; }			
			
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=9, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=10, GroupName="Parameters")]
			public int stopLoss
			{ get; set; }			
			
			
		#endregion

	}
}


//todo: playing short side? does trading hours matter? hows it perofrm on nasdaq? 
//anyway to prevent sideways entries maybe slope angle?  
//add confirmation of nasdaq signal too?
