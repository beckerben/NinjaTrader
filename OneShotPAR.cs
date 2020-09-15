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

namespace NinjaTrader.NinjaScript.Strategies
{
	public class OneShotPAR : Strategy
	{

//		private Series<double> upper;
		bool cocked;
		string parState;

		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"PAR trader";
				Name										= "OneShotPAR";
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
				
				period = 15;
				profitTaker = 10;
				allowMulti = false;
				cocked = false;
				parState = "none";
				
//				stopLoss = 50;
				
				
			}
			else if (State == State.Configure)
			{
				RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
//				SetStopLoss(CalculationMode.Ticks, stopLoss);
			}
			else if (State == State.DataLoaded)
			{
//				upper = new Series<double>(this,MaximumBarsLookBack.Infinite);
//				if (State != State.Historical && BarsPeriod.BarsPeriodType != BarsPeriodType.Range)
//				{
//					MessageBox.Show("You must use the range chart type");
//				}
				
				AddChartIndicator(ParabolicSAR(0.02, 0.2, 0.02));
				AddChartIndicator(ZLEMA(period));
				
			}
		}

		protected override void OnBarUpdate()
		{
						
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
//			upper[0] = (std[0] * dev + (SMA(macd,length))[0]);
//			Print(Time[0] + " Close: " + Close[0] + " MACD: " + macd[0] + " Upper: " + upper[0] + " Lower: " + lower[0] );
			
				
			if (Position.MarketPosition != MarketPosition.Flat)
			{
				SetStopLoss(CalculationMode.Price,((Close[0]+ParabolicSAR(Close, 0.02, 0.2, 0.02)[0])/2));
				Print(Time[0] + " Not Flat, Setting Stop Loss " + ((Close[0]+ParabolicSAR(Close, 0.02, 0.2, 0.02)[0])/2).ToString());
			}
			else
			{
				//SetStopLoss(CalculationMode.Price,ParabolicSAR(Close, 0.02, 0.2, 0.02)[0]);		
				//Print(Time[0] + " Flat, Setting Stop Loss " + (ParabolicSAR(Close, 0.02, 0.2, 0.02)[0]).ToString());
			}
			
			if (ParabolicSAR(Close, 0.02, 0.2, 0.02)[0] > High[0]) 
			{
				if (parState != "high") cocked = true;
				parState = "high";
			}
			else 
			{
				if (parState != "low") cocked = true;
				parState = "low";
			}
			
			if ((cocked || allowMulti) && parState == "high" && Close[0] > ZLEMA(period)[0])
			{
				if (GetCurrentBid() > Close[0]-0.5) EnterShortStopLimit(Close[0]-0.5,Close[0]-0.5);
				else EnterShortLimit(Close[0]-0.5);
				cocked = false;
			}
			
			if ((cocked || allowMulti) && parState == "low" && Close[0] < ZLEMA(period)[0])
			{
				if (GetCurrentAsk() < Close[0]+0.5) EnterLongStopLimit(Close[0]+0.5,Close[0]+0.5);
				else EnterLongLimit(Close[0]+0.5);
				cocked = false;
			}
			

			
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			
		}		
		
		
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice,
                                    OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{

		    // Rejection handling
		    if (order.OrderState == OrderState.Rejected)
		    {
				Print(time.ToString() + " Error: Order rejected, order type " + order.OrderType + " " + nativeError); 
			}
		}
		
		#region Properties
				
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Period", Description="", Order=1, GroupName="Parameters")]
			public int period
			{ get; set; }
			
			[Display(Name="Allow Multi-Entry", Description="", Order=2, GroupName="Parameters")]
			public bool allowMulti
			{ get; set; }
			

			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=9, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
//			[NinjaScriptProperty]
//			[Range(1, int.MaxValue)]
//			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=10, GroupName="Parameters")]
//			public int stopLoss
//			{ get; set; }			
			
			
		#endregion

	}
}

