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
	public class RSIScalper : Strategy
	{
		int barCount = 0;	
		int lastExitBar = 0;
		int waitBars = 20;
		
		protected override void OnStateChange()
		{
				
			
			if (State == State.SetDefaults)
			{
				Description									= @"Makes use of Bolinger Bands";
				Name										= "RSI Scalper";
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
				
				slope = 0;
				rsiPeriod = 8;
				rsiSmooth= 3;
				profitTaker = 10;
				stopLoss = 22;
				
			}
			else if (State == State.Configure)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				
				AddChartIndicator(RSI(rsiPeriod, rsiSmooth));
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] < (BarsRequiredToTrade + rsiPeriod))
				return;
					
			barCount++;
			
			bool enterLong = false;
			bool exitLong = false;

		
			double rsiDiff1 = RSI(rsiPeriod,rsiSmooth).Avg[0] - RSI(rsiPeriod,rsiSmooth).Avg[1];
			double rsiDiff2 = RSI(rsiPeriod,rsiSmooth).Avg[1] - RSI(rsiPeriod,rsiSmooth).Avg[2];
			double backbar0 = Open[0] - Close[0];
			double backbar1 = Open[1] - Close[1];
			
			//Print("RSI Diff1 "+ rsiDiff1 + " RSI Diff2 " + rsiDiff2 + " Open0 " + Open[0] + " Close0 " + Close[0] + " Back Bar0 " + backbar0 + " Back Bar1 " + backbar1 + " RSI " + RSI(rsiPeriod,rsiSmooth)[0]);
			

			//readjust the profit & stop loss
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
//			// If a long position is open, allow for stop loss modification to breakeven
//			else if (Position.MarketPosition == MarketPosition.Long)
//			{
//				// Once the price is greater than entry price+50 ticks, set stop loss to breakeven
//				if (priorAveragePrice != Position.AveragePrice)
//				{
//					SetProfitTarget(CalculationMode.Price, Position.AveragePrice + profitTaker);
//					SetStopLoss(CalculationMode.Price, Position.AveragePrice - stopLoss);
//					priorAveragePrice = Position.AveragePrice;
//				}
//			}			
			
			
			//if (Position.MarketPosition == MarketPosition.Long && (exitOn == 0 && CrossAbove(Close, SMA(period),1)) || (exitOn == 1 && CrossAbove(Close, Bollinger(stdDev,period).Upper,1) ))
			if (Position.MarketPosition == MarketPosition.Flat &&  RSI(rsiPeriod,rsiSmooth)[0] <= 20 )
			{
				enterLong = true;	
			}
			
//			if ( Position.MarketPosition == MarketPosition.Long && CrossBelow(RSI(rsiPeriod,rsiSmooth),RSI(rsiPeriod,rsiSmooth).Avg,0))
//			{
//				exitLong = true;
//			}
			
			
			
			if (enterLong) 
			{
					EnterLong();
			}
//			if (exitLong)
//			{
//					ExitLong();
//			}
			
						
		}
		
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

//			Print ("Executed order " + marketPosition + " " + price); 
			lastExitBar = barCount;
			
		}

 
		
		#region Properties
			[NinjaScriptProperty]
			[Range(0.0, 0.95)]
			[Display(Name="Slope", Description="Slope", Order=1, GroupName="Parameters")]
			public double slope
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(2, 240)]
			[Display(Name="RSI Period", Description="RSI Period", Order=2, GroupName="Parameters")]
			public int rsiPeriod
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(2, 240)]
			[Display(Name="RSI Smooth", Description="RSI Period", Order=3, GroupName="Parameters")]
			public int rsiSmooth
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(2, 1024)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=4, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
			[NinjaScriptProperty]
			[Range(1, 1024)]
			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=5, GroupName="Parameters")]
			public int stopLoss
			{ get; set; }			
			
		
			
		#endregion

	}
}


//todo: playing short side? does trading hours matter? hows it perofrm on nasdaq? 
//anyway to prevent sideways entries maybe slope angle?  
//add confirmation of nasdaq signal too?
