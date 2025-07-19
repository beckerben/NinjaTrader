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
	public class AK : Strategy
	{

		private Series<double> upper;
		private Series<double> lower;
		private Series<double> macd;
		private Series<double> std;
		private bool cocked;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"strategy by algokid from TV";
				Name										= "AK";
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
				BarsRequiredToTrade							= 30;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				length = 10;
				dev = 1;
				fastLength = 12;
				slowLength = 26;
				cocked = false;
				
				profitTaker = 12;
				stopLoss = 16;
				
				
			}
			else if (State == State.Configure)
			{

				SetProfitTarget(CalculationMode.Ticks, profitTaker);
				SetStopLoss(CalculationMode.Ticks, stopLoss);

			}
			else if (State == State.DataLoaded)
			{
				
				upper = new Series<double>(this,MaximumBarsLookBack.Infinite);
				lower = new Series<double>(this,MaximumBarsLookBack.Infinite);
				macd = new Series<double>(this,MaximumBarsLookBack.Infinite);
				std = new Series<double>(this,MaximumBarsLookBack.Infinite);
				
			}
		}

		protected override void OnBarUpdate()
		{
			
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;

			
			macd[0] = EMA(Close,fastLength)[0] - EMA(Close,slowLength)[0];
			std[0] = StdDev(macd,length)[0];
			upper[0] = (std[0] * dev + (SMA(macd,length))[0]);
			lower[0] = ((SMA(macd,length)[0]) - (std[0] * dev));
			
			//Print(Time[0] + " Close: " + Close[0] + " MACD: " + macd[0] + " Upper: " + upper[0] + " Lower: " + lower[0] );
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);	
			}
			
			if (macd[0] > upper[0] & !cocked)
			{
				cocked = true;
				SetStopLoss(CalculationMode.Price,Low[0]-0.5);		
				EnterLong();
			}
			
			if (macd[0] < lower[0] & !cocked) 
			{
				cocked = true;
				SetStopLoss(CalculationMode.Price,High[0]+0.5);			
				EnterShort();
			}
			
			if (macd[0] < upper[0] && macd[0] > lower[0])
				cocked = false;
			
			if (!cocked && Position.MarketPosition == MarketPosition.Long)
				ExitLong();
			if (!cocked && Position.MarketPosition == MarketPosition.Short)
				ExitShort();
			
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
				
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="BB Periods", Description="", Order=1, GroupName="Parameters")]
			public int length
			{ get; set; }

			[Range(0.1, double.MaxValue), NinjaScriptProperty]
			[Display(Name="Deviations", Description="", Order=2, GroupName="Parameters")]
			public double dev
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Fast Length", Description="", Order=3, GroupName="Parameters")]
			public int fastLength
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Slow Length", Description="", Order=4, GroupName="Parameters")]
			public int slowLength
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
