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
	public class ZLEMA3 : Strategy
	{
		
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Makes use of ZLEMA";
				Name										= "ZLEMA3";
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
				
				zlemaPeriod1 = 13;
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
				
				AddChartIndicator(ZLEMA(zlemaPeriod1));	

				AddChartIndicator(StochRSI(9));

			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			if (BarsInProgress != 0)
				return;
		
		

			if (ZLEMA(zlemaPeriod1)[0] > ZLEMA(zlemaPeriod1)[1] && StochRSI(8)[0] >= 0.8)
			{
				EnterLong();
				
			}
			
			//if (ZLEMA(zlemaPeriod1)[0] < ZLEMA(zlemaPeriod1)[1] )
			if (Position.MarketPosition == MarketPosition.Long && Close[0] < ZLEMA(zlemaPeriod1)[0] )
			{
				ExitLong();
			}
			
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="ZLEMA Period1", Description="", Order=1, GroupName="Parameters")]
			public int zlemaPeriod1
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
