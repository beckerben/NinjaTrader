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
	public class ZLEMA2 : Strategy
	{
		
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Makes use of ZLEMA & EMA";
				Name										= "ZLEMA2";
				Calculate									= Calculate.OnEachTick;
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
				
				zlemaPeriod = 9;
				emaPeriod = 9;
				profitTaker = 16;
				stopLoss = 1000;
				trailTicks = 12;
				
			}
			else if (State == State.Configure)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				AddChartIndicator(ZLEMA(zlemaPeriod));	
				AddChartIndicator(EMA(emaPeriod));	
				ChartIndicators[0].Plots[0].Brush = Brushes.Red;
				ChartIndicators[1].Plots[0].Brush = Brushes.LimeGreen;
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			
			if (CrossAbove(ZLEMA(zlemaPeriod),EMA(emaPeriod),1))
			{
				//SetTrailStop(CalculationMode.Ticks, trailTicks);
				EnterLong();
			}
			if (CrossBelow(ZLEMA(zlemaPeriod), EMA(emaPeriod),1) )
			{
				//SetTrailStop(CalculationMode.Ticks, trailTicks);
				EnterShort();
			}
			
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="ZLEMA Period", Description="", Order=1, GroupName="Parameters")]
			public int zlemaPeriod
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="EMA Period", Description="", Order=2, GroupName="Parameters")]
			public int emaPeriod
			{ get; set; }
						
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Trail Stop Ticks", Description="", Order=3, GroupName="Parameters")]
			public int trailTicks
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(2, 1024)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=9, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
			[NinjaScriptProperty]
			[Range(1, 1024)]
			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=10, GroupName="Parameters")]
			public int stopLoss
			{ get; set; }			
		#endregion

	}
}


//todo: playing short side? does trading hours matter? hows it perofrm on nasdaq? 
//anyway to prevent sideways entries maybe slope angle?  
//add confirmation of nasdaq signal too?
