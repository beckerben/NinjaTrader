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
	public class NineEighteenCrossover : Strategy
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Leverages 9-18 crossover of MACD";
				Name										= "NineEighteenCrossover";
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
				
				fast = 40;
				slow = 80;
				smooth = 40;
				profitTaker = 1024;
				stopLoss = 1024;
				increasingBars = 3;
				decreasingBars = 3;
				
			}
			else if (State == State.Configure)
			{
				//SetStopLoss(CalculationMode.Ticks, stopLoss);
				//SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				
				AddChartIndicator(MACD(fast,slow,smooth));	
			}
		}

		protected override void OnBarUpdate()
		{
			
			Print(MACD(fast,slow,smooth).Values[0][0]);
			
			if (CurrentBars[0] < (BarsRequiredToTrade + slow))
				return;
				  
			bool enterLong = false;
			bool exitLong = false;
			
			bool decreasing = false;
			bool increasing = false;
			int increasingCnt = 0;
			int decreasingCnt = 0;

			for (int i = 0; i < increasingBars; i++)
			{
				if (MACD(fast,slow,smooth).Diff[i] > MACD(fast,slow,smooth).Diff[i+1])	
				{
					increasingCnt++;
				}
				
			}
			if (increasingCnt == increasingBars)
			{
				increasing = true;
			}
			
			for (int i = 0; i < decreasingBars; i++)
			{
				if (MACD(fast,slow,smooth).Diff[i] < MACD(fast,slow,smooth).Diff[i+1])	
				{
					decreasingCnt++;
				}
			}
			if (decreasingCnt == decreasingBars)
			{
				decreasing = true;
			}
			
			
			if ( CrossAbove(MACD(fast,slow,smooth).Values[0],MACD(fast,slow,smooth).Values[1],1) )
			{
				EnterLong();
			}
			
			if ( CrossBelow(MACD(fast,slow,smooth).Values[0],MACD(fast,slow,smooth).Values[1],1))
			{
				EnterShort();
			}
			
		
			
						
		}
		
		#region Properties
			[NinjaScriptProperty]
			[Range(6, 1024)]
			[Display(Name="Fast", Description="Fast moving average", Order=1, GroupName="Parameters")]
			public int fast
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(9, 1024)]
			[Display(Name="Slow", Description="Slow moving average", Order=2, GroupName="Parameters")]
			public int slow
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(2, 1024)]
			[Display(Name="Smooth", Description="Smoother", Order=3, GroupName="Parameters")]
			public int smooth
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(2, 50)]
			[Display(Name="Increasing Bars", Description="Increasing Bars", Order=4, GroupName="Parameters")]
			public int increasingBars
			{ get; set; }

			[NinjaScriptProperty]
			[Range(2, 50)]
			[Display(Name="Decreasing Bars", Description="Decreasing Bars", Order=5, GroupName="Parameters")]
			public int decreasingBars
			{ get; set; }			
		
			[NinjaScriptProperty]
			[Range(2, 1024)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=6, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
			[NinjaScriptProperty]
			[Range(1, 1024)]
			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=7, GroupName="Parameters")]
			public int stopLoss
			{ get; set; }						
			
		#endregion

	}
}


//todo: playing short side? does trading hours matter? hows it perofrm on nasdaq? 
//anyway to prevent sideways entries maybe slope angle?  
//add confirmation of nasdaq signal too?