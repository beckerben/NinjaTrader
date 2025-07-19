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
	public class RenkoSniper : Strategy
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Looks for fast ticks";
				Name										= "RenkoSniper";
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
				RenkoTick = 2.0;
				MSTrigger = 250;
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks,(RenkoTick*1.0));
				SetStopLoss(CalculationMode.Ticks,(RenkoTick*1.5));
				//SetTrailStop(CalculationMode.Ticks,(RenkoTick*2.1));
				
			}
		}

		protected override void OnBarUpdate()
		{
			//check if we have enough bars to trade
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			
			if(Position.MarketPosition == MarketPosition.Flat)
			{
				//evaluate 
				//if (Time[0].Subtract(Time[1]).TotalMilliseconds <= MSTrigger) 
				//{
					//bar created in < trigger
					//if(Close[0] > High[1] && (((Close[0]-Open[0])*4)/RenkoTick)>=0.90)
					if ((((Close[0]-Open[0])*4)/RenkoTick)>=0.9 && Close[0]>EMA(14)[0] && Open[0]<EMA(14)[0])						
					{
						EnterLong();
					}
					
					//if(Close[0] < Low[1] && (((Open[0]-Close[0])*4)/RenkoTick)>=0.90)
					if((((Open[0]-Close[0])*4)/RenkoTick)>=0.9 && Close[0]<EMA(14)[0] && Open[0]>EMA(14)[0])
					{
						EnterShort();
					}
				//}
			}
		}

		#region Properties
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Renko Chart Tick", Description="The renko chart tick size.", Order=0, GroupName="Config")]
		public double RenkoTick
		{ get; set; }
			
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MS Trigger", Description="The millisecond trigger size.", Order=0, GroupName="Config")]
		public double MSTrigger
		{ get; set; }		
		
		#endregion
		
		
	}
}
