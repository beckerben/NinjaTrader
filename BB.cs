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
	public class BB : Strategy
	{
		
		int lastExitBar = 0;
		int waitBars = 20;
		double enterPrice;
		double exitPrice;

		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Makes use of Bolinger Bands";
				Name										= "BB";
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
				
				stdDev = 2;
				period = 20;
				exitOn = 0;
				profitTaker = 1024;
				stopLoss = 1024;
				stochPeriod = 8;
				
				
			}
			else if (State == State.Configure)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				
				AddChartIndicator(Bollinger(stdDev,period));	
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] < (BarsRequiredToTrade + period))
				return;
		
			bool enterLong = false;
			bool exitLong = false;

			//Print("Upper " + Bollinger(stdDev,period).Upper[0] + " Lower " + Bollinger(stdDev,period).Lower[0] + " Avg " + SMA(period)[0] + " Close " + Close[0]);
			
			//Print(Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0] + "," + Bollinger(stdDev,period).Lower[0] + "," + Bollinger(stdDev,period).Upper[0] + "," + (Bollinger(stdDev,period).Upper[0]-Bollinger(stdDev,period).Lower[0]) + "," + SMA(period)[0]);
			
			
			if (Position.MarketPosition == MarketPosition.Flat && CrossBelow(Close, Bollinger(stdDev,period).Lower,1) )
			{
				enterLong = true;
			}
			
			
			if ((Position.MarketPosition == MarketPosition.Long && (exitOn == 0 && CrossAbove(Close, SMA(period),1)) || (exitOn == 1 && CrossAbove(Close, Bollinger(stdDev,period).Upper,1) )))
			{
				exitLong = true;	
			}
			
			
			if (enterLong) 
			{
					EnterLong();
			}
			if (exitLong)
			{
					ExitLong();
			}
			
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
			[NinjaScriptProperty]
			[Range(0.5, 4.0)]
			[Display(Name="StdDev", Description="Standard deviation e.g. 2.0", Order=1, GroupName="Parameters")]
			public double stdDev
			{ get; set; }

			[NinjaScriptProperty]
			[Range(5,1024)]
			[Display(Name="Period", Description="Period", Order=2, GroupName="Parameters")]
			public int period
			{ get; set; }

			
			[NinjaScriptProperty]
			[Range(0, 1)]
			[Display(Name="Exit On", Description="0 = Moving Average, 1 = Upper Band", Order=3, GroupName="Parameters")]
			public int exitOn
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
			
			[NinjaScriptProperty]
			[Range(1, 1024)]
			[Display(Name="Stoch RSI Period", Description="Stoch RSI Period", Order=6, GroupName="Parameters")]
			public int stochPeriod
			{ get; set; }				
			
		#endregion

	}
}


//todo: playing short side? does trading hours matter? hows it perofrm on nasdaq? 
//anyway to prevent sideways entries maybe slope angle?  
//add confirmation of nasdaq signal too?