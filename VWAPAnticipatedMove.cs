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
	/// <summary>
	/// Depends on the VWAP8 indicator which is available via community indicators
	/// </summary>
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
			
				exitLongSD05 = true;
				exitShortSD05 = true;
				profitTaker = 25;
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
				AddChartIndicator(CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20));
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (BarsInProgress != 0)
				return;
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
		
			double cR1 = 0;
			double cR3 = 0;
			double cR4 = 0;
			double cS1 = 0;
			double cS3 = 0;
			double cS4 = 0;
			
			// Evaluates that this is a valid pivot point value
			if (CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).S4.IsValidDataPoint(0))
			{
				// Prints the current pivot point value
				cR1 = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).R1[0];
				cR3 = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).R3[0];
				cR4 = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).R4[0];
				cS1 = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).S1[0];
				cS3 = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).S3[0];
				cS4 = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20).S4[0];
			}
			
			// Resets the stop loss to the original value when all positions are closed
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss*4);
				SetProfitTarget(CalculationMode.Ticks, profitTaker*4);
			}
			
			// If a position is open, allow for stop loss modification to breakeven
			else if (Position.Quantity > 0)
			{
				// Once the price is greater than entry price+profit taker, set stop loss
				if (priorAveragePrice != Position.AveragePrice)
				{
					if (Position.MarketPosition == MarketPosition.Long) 
					{
						SetProfitTarget(CalculationMode.Price, Position.AveragePrice + profitTaker);
						SetStopLoss(CalculationMode.Price, cS4);
					}
					if (Position.MarketPosition == MarketPosition.Short)
					{
						SetProfitTarget(CalculationMode.Price, Position.AveragePrice - profitTaker);
						SetStopLoss(CalculationMode.Price, cR4);
					}
					priorAveragePrice = Position.AveragePrice;
				}
			}			
			
			
			//calculate the standard deviation 
			double sd = (vwap[0] * (Closes[1][0] / 100) * Math.Sqrt(calDays / 365)) / calDays;
			Print("VX: " + Closes[1][0].ToString() + " SD: " + sd.ToString());
		
			//calculate the offsets from the vwap
			//above
			Values[0][0] = vwap[0] + (sd * 0.5);
			Values[1][0] = vwap[0] + sd;
			Values[2][0] = vwap[0] + (sd * 2);
			Values[3][0] = vwap[0] + (sd * 3);
			Values[4][0] = vwap[0] + (sd * 4);
			//below
			Values[5][0] = vwap[0] - (sd * 0.5);
			Values[6][0] = vwap[0] - sd;
			Values[7][0] = vwap[0] - (sd * 2);
			Values[8][0] = vwap[0] - (sd * 3);
			Values[9][0] = vwap[0] - (sd * 4);	
			
			///********************************************************************************************************
			//determine the long signals
			///********************************************************************************************************
			#region LongSignals
			bool long0 = false;
			bool long1 = false;
			bool long2 = false;
			bool long3 = false;
			bool long4 = false;
			
			
			if (Position.Quantity == 0 
				&& Close[0] < Values[5][0]  
				&& Close[0] > Values[6][0] && Close[0] < cR1
				&& cS4 < Values[7][0])
			{
				long0 = true;	
			}
			
			if (Position.Quantity == 1 && Close[0] < Values[6][0] )
			{
				long1 = true;	
			}
			
			if (Position.Quantity == 2 && Close[0] < Values[7][0] )
			{
				long2 = true;	
			}
			
			if (Position.Quantity == 3 && Close[0] < Values[8][0] )
			{
				long3 = true;	
			}
			
			if (Position.Quantity >= 4 && Close[0] < Values[9][0] && CurrentBar >  buffer )
			{
				long4 = true;	
				buffer = CurrentBar + 15;
			}
			
			//long exit logic
			bool exitLong = false;
			if (exitLongSD05 && Position.MarketPosition == MarketPosition.Long && Close[0] > Values[0][0]) exitLong = true;
			if (!exitLongSD05 && Position.MarketPosition == MarketPosition.Long && Close[0] > vwap[0]) exitLong = true;
			
			#endregion //LongSignals
			
			///********************************************************************************************************
			//determine the short signals
			///********************************************************************************************************
			#region ShortSignals
			bool short0 = false;
			bool short1 = false;
			bool short2 = false;
			bool short3 = false;
			bool short4 = false;
			
			
			if ((Position.Quantity == 0 
				&& Close[0] > Values[0][0] 
				&& Close[0] < Values[1][0] 
				&& Close[0] > cS1
				&& cR4 > Values[2][0]
				) || 
				(exitLong 
				&& Close[0] > Values[0][0] 
				&& Close[0] < Values[1][0] 
				&& Close[0] > cS1
				&& cR4 > Values[2][0]))
			{
				short0 = true;	
			}
			
			if (Position.MarketPosition == MarketPosition.Short && Position.Quantity == 1 && Close[0] > Values[1][0] )
			{
				short1 = true;	
			}
			
			if (Position.MarketPosition == MarketPosition.Short && Position.Quantity == 2 && Close[0] > Values[2][0] )
			{
				short2 = true;	
			}
			
			if (Position.MarketPosition == MarketPosition.Short && Position.Quantity == 3 && Close[0] > Values[3][0] )
			{
				short3 = true;	
			}
			
			if (Position.MarketPosition == MarketPosition.Short && Position.Quantity >= 4 && Close[0] > Values[4][0] && CurrentBar >  buffer )
			{
				short4 = true;	
				buffer = CurrentBar + 15;
			}
			
			//short exit logic
			bool exitShort = false;
			if (exitShortSD05 && Position.MarketPosition == MarketPosition.Short && Close[0] < Values[5][0]) exitShort = true;
			if (!exitShortSD05 && Position.MarketPosition == MarketPosition.Short && Close[0] < vwap[0]) exitShort = true;
			
			#endregion //ShortSignals
			
			
			//execute the entries
			if ((Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long) && (long0 || long1 || long2 || long3 || long4))
					EnterLong();
			if (exitLong)
					ExitLong();
			
			if ((Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short) && (short0 || short1 || short2 || short3 || short4))
					EnterShort();
			if (exitShort)
					ExitShort();
		}

		#region Properties

		[NinjaScriptProperty]
		[Display(Name="Exit Long SD+0.5", Description="Exit VWAP + 0.5 SD, else VWAP", Order=1, GroupName="Parameters")]
		public bool exitLongSD05
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Exit Short SD-0.5", Description="Exit VWAP - 0.5 SD, else VWAP", Order=2, GroupName="Parameters")]
		public bool exitShortSD05
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Profit Taker Points", Description="Points to profit take at", Order=9, GroupName="Parameters")]
		public int profitTaker
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Loss Points", Description="Points to set stop loss at", Order=10, GroupName="Parameters")]
		public int stopLoss
		{ get; set; }
		
		#endregion

	}
}
