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
	public class VWAPAnticipatedMove : Strategy
	{

		private VWAP8 vwap;
		double priorAveragePrice = 0;
		double buffer = 0;
		double calDays = 30;
		
		protected override void OnStateChange()
		
		
		{
			if (State == State.SetDefaults)
			{
				
				Description = @"This strategy leverages the VIX to calculated an expected move range up to 4 standard deviations and trades accordingly. ";
				Name = "VWAPAnticipatedMove";
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
				BarsRequiredToTrade							= 32;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
			
				
				profitTaker = 1024;
				stopLoss = 1024;
				
				AddPlot(Brushes.Red, "SD0"); //0
				AddPlot(Brushes.Red, "SD1"); //1
				AddPlot(Brushes.Red, "SD2"); //2
				AddPlot(Brushes.Red, "SD3"); //3
				AddPlot(Brushes.Red, "SD4"); //4
				
				AddPlot(Brushes.LimeGreen, "SD-0"); //5
				AddPlot(Brushes.LimeGreen, "SD-1"); //6
				AddPlot(Brushes.LimeGreen, "SD-2"); //7
				AddPlot(Brushes.LimeGreen, "SD-3"); //8
				AddPlot(Brushes.LimeGreen, "SD-5"); //9
				
			}
			else if (State == State.Configure)
			{
				AddDataSeries("VX 12-20", Data.BarsPeriodType.Minute, 1, Data.MarketDataType.Last); //1
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}

			else if (State == State.DataLoaded)
			{
				vwap = VWAP8();
				vwap.Plots[0].Brush = Brushes.White;
				VOL().Panel = 2;
				
				//add the indicators
				AddChartIndicator(vwap);

			}
		}

		protected override void OnBarUpdate()
		{
			
			if (BarsInProgress != 0)
				return;
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
		
			
			// Resets the stop loss to the original value when all positions are closed
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss*4);
				SetProfitTarget(CalculationMode.Ticks, profitTaker*4);
			}
			
			// If a long position is open, allow for stop loss modification to breakeven
			else if (Position.MarketPosition == MarketPosition.Long)
			{
				// Once the price is greater than entry price+50 ticks, set stop loss to breakeven
				if (priorAveragePrice != Position.AveragePrice)
				{
					SetProfitTarget(CalculationMode.Price, Position.AveragePrice + profitTaker);
					SetStopLoss(CalculationMode.Price, Position.AveragePrice - stopLoss);
					priorAveragePrice = Position.AveragePrice;
				}
			}			
			
			
			//calculate the standard deviation 
			double sd = (vwap[0] * (Closes[1][0] / 100) * Math.Sqrt(calDays / 365)) / calDays;
			
		
			//calculate the offsets from the vwap
			Values[0][0] = vwap[0] + (sd * 0.5);
			Values[1][0] = vwap[0] + sd;
			Values[2][0] = vwap[0] + (sd * 2);
			Values[3][0] = vwap[0] + (sd * 3);
			Values[4][0] = vwap[0] + (sd * 4);
			Values[5][0] = vwap[0] - (sd * 0.5);
			Values[6][0] = vwap[0] - sd;
			Values[7][0] = vwap[0] - (sd * 2);
			Values[8][0] = vwap[0] - (sd * 3);
			Values[9][0] = vwap[0] - (sd * 4);	
			
			//determine the buy signals
			bool buy0 = false;
			bool buy1 = false;
			bool buy2 = false;
			bool buy3 = false;
			bool buy4 = false;
			
			if (Position.Quantity == 0 && Close[0] < Values[5][0] )
			{
				buy0 = true;	
			}
			
			if (Position.Quantity == 1 && Close[0] < Values[6][0] )
			{
				buy1 = true;	
			}
			
			if (Position.Quantity == 2 && Close[0] < Values[7][0] )
			{
				buy2 = true;	
			}
			
			if (Position.Quantity == 3 && Close[0] < Values[8][0] )
			{
				buy3 = true;	
			}
			
			if (Position.Quantity >= 4 && Close[0] < Values[9][0] && CurrentBar >  buffer )
			{
				buy4 = true;	
				buffer = CurrentBar + 15;
			}
			
			
			bool sell = false;
			if (Position.Quantity > 0 && Close[0] > Values[0][0]) sell = true;
			
			if (buy0 || buy1 || buy2 || buy3 || buy4) 
					EnterLong();
			if (sell)
					ExitLong();
		}

		#region Properties

		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Profit Taker", Description="Points to profit take at", Order=2, GroupName="Parameters")]
		public int profitTaker
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Loss", Description="Points to set stop loss at", Order=3, GroupName="Parameters")]
		public int stopLoss
		{ get; set; }
		
		#endregion

	}
}
