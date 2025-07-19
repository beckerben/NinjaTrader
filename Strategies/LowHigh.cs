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
	public class LowHigh : Strategy
	{
		
		
		int waitBars = 20;
		double enterPrice;
		double exitPrice;
		double lastPrice = 0;
		int lastPriceBar = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Low to High Compare";
				Name										= "Low High";
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
				
				barsBack = 3;
				profitTaker = 10;
				stopLoss = 20;
				
			}
			else if (State == State.Configure)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] < (BarsRequiredToTrade ))
				return;
			
			double upSlope = (Low[0] - High[barsBack]) / barsBack;
			double downSlope = (High[0] - Low[barsBack]) / barsBack;
			
			
			//Print("Low0 " + Low[0] + " Low[barsBack] " + Low[barsBack] + " High0 " + High[0] + " High " + High[barsBack] + " upSlope " + upSlope + " downSlope " + downSlope);
			
			if (upSlope > 0.5 ) EnterLong();
			
			if (downSlope < -0.5) EnterShort();
				
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

//			if (price < lastPrice)
//			{
//				Print("Loser");
//			}
//			if (price > lastPrice)
//			{
//				Print ("Winner");	
//			}
//			//Print ("execution price " + price + " last execution price " + lastPrice); 
//			lastPrice = price;
//			lastPriceBar = CurrentBars[0];
			
		}		
		
		#region Properties
		
			[NinjaScriptProperty]
			[Range(1, 50)]
			[Display(Name="Bars Back", Description="Bars to go back to compare high", Order=3, GroupName="Parameters")]
			public int barsBack
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
