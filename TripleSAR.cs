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
	public class TripleSAR : Strategy
	{
		ParabolicSAR slow;
		ParabolicSAR med;
		ParabolicSAR fast;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"strategy by rollas from TV";
				Name										= "TripleSAR";
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
				
				pointDiff = 1;
				profitTaker = 100;
				stopLoss = 100;
				
				
			}
			else if (State == State.Configure)
			{
				//SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				
				
				slow = ParabolicSAR(0.01,0.2,0.01);
				med = ParabolicSAR(0.01,0.2,0.02);
				fast = ParabolicSAR(0.01,0.2,0.03);
				
				slow.Plots[0].PlotStyle = PlotStyle.Cross;
				slow.Plots[0].Brush = Brushes.Red;
				AddChartIndicator(slow);
				
				med.Plots[0].PlotStyle = PlotStyle.Cross;
				med.Plots[0].Brush = Brushes.LimeGreen;
				AddChartIndicator(med);
				
				fast.Plots[0].PlotStyle = PlotStyle.Cross;
				fast.Plots[0].Brush = Brushes.Blue;
				AddChartIndicator(fast);

			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			if (Math.Abs(slow[0]-med[0]) <= pointDiff && 
				Math.Abs(slow[0]-fast[0]) <= pointDiff &&
				Math.Abs(med[0]-fast[0]) <= pointDiff &&
				slow[0] < Close[0] &&
				med[0] < Close[0] &&
				fast[0] < Close[0])
			{
				EnterLong();
			}
			
			if (Math.Abs(slow[0]-med[0]) <= pointDiff && 
				Math.Abs(slow[0]-fast[0]) <= pointDiff &&
				Math.Abs(med[0]-fast[0]) <= pointDiff &&
				slow[0] > Close[0] &&
				med[0] > Close[0] &&
				fast[0] > Close[0])
			{
				EnterShort();
			}
						
			
			if (Position.MarketPosition == MarketPosition.Long && Close[0] < fast[0])
					ExitLong();
		
			if (Position.MarketPosition == MarketPosition.Short && Close[0] > fast[0])
					ExitShort();
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
				
			[Range(0.01, double.MaxValue), NinjaScriptProperty]
			[Display(Name="Point Differential", Description="Point separation", Order=1, GroupName="Parameters")]
			public double pointDiff
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
