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
	public class Doji : Strategy
	{

//		private Series<double> upper;

		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Looks for doji reversals, muts be run on range chart";
				Name										= "Doji";
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
				
				height = 6;
				pullback = 12;
				profitTaker = 12;
				stopLoss = 8;
				
				
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
				SetStopLoss(CalculationMode.Ticks, stopLoss);
			}
			else if (State == State.DataLoaded)
			{
//				upper = new Series<double>(this,MaximumBarsLookBack.Infinite);
				if (State != State.Historical && BarsPeriod.BarsPeriodType != BarsPeriodType.Range)
				{
					MessageBox.Show("You must use the range chart type");
				}
				
			}
		}

		protected override void OnBarUpdate()
		{
						
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
//			upper[0] = (std[0] * dev + (SMA(macd,length))[0]);
//			Print(Time[0] + " Close: " + Close[0] + " MACD: " + macd[0] + " Upper: " + upper[0] + " Lower: " + lower[0] );
			
			//determine doji up or bottom
			bool dojiUp = false;
			bool dojiDown = false;
			double oc = (Close[0] - Open[0]) * 4;
			if (oc < 0 && Math.Abs(oc) <= height) dojiDown = true;
			if (oc > 0 && oc <= height) dojiUp = true;
				
			if (Position.MarketPosition == MarketPosition.Flat && dojiUp)
			{
				if(pullback == 0)
				{
					EnterLong();
				}
				else
				{
					EnterLongLimit(High[0] - (pullback / 4));
				}
				//SetStopLoss(CalculationMode.Price,Low[0] - 0.25);
			}
		
			if (Position.MarketPosition == MarketPosition.Flat && dojiDown)
			{
				if(pullback == 0)
				{
					EnterShort();
				}
				else
				{
					EnterShortLimit(Low[0] + (pullback / 4));
				}
				//SetStopLoss(CalculationMode.Price,High[0] + 0.25);
			}
				
			
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			
		}		
		
		#region Properties
				
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Doji Ticks", Description="", Order=1, GroupName="Parameters")]
			public int height
			{ get; set; }
			
			[Range(0, double.MaxValue), NinjaScriptProperty]
			[Display(Name="Pullback Ticks", Description="The amount of ticks for the limit order", Order=2, GroupName="Parameters")]
			public double pullback
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

