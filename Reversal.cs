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
	/// Recommended backtesting strategy is to leverage the playback connection rather than the strategy analyzer
	/// 
	/// Setup: works on range or time based charts
	/// 
	/// Strategy hypothesis, the use of key reversal and the MACD to look for entries for a small continuation of the reversal
	/// 
	/// </summary>
	public class Reversal : Strategy
	{
		bool enterLong = false;
		bool enterShort = false;

		protected override void OnStateChange()
		{
			
			if (State == State.SetDefaults)
			{
				Description									= @"This is a test of Ben Becker's hypothesis to follow Renko consecutive trend, ideal for volatile market vs sideways (in theory)";
				Name										= "Reversal";
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
				BarsRequiredToTrade							= 14;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;

				reversalPeriod = 14;
				profitTaker = 35;
				stopLoss = 20;
				rangeSize = 30;
				rangeDivisor = 4;
				
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks,(profitTaker));
				SetStopLoss(CalculationMode.Ticks,stopLoss);
				//SetTrailStop(CalculationMode.Ticks,stopLoss*0.5);
				RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
			}
			else if (State == State.DataLoaded)
			{
				AddChartIndicator(KeyReversalDown(reversalPeriod));
				AddChartIndicator(KeyReversalUp(reversalPeriod));
			}			
		}

		protected override void OnBarUpdate()
		{
			
			//check if we have enough bars to trade
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			

			
			//lets determine if we enter
			//long
			if (
				KeyReversalUp(reversalPeriod)[0] == 1
				&& (DoubleStochastics(10)[0] >= 90.0 || DoubleStochastics(10)[0] <= 10.0)
				//&& (RSquared(8)[0] >= 0.75 || RSquared(8)[0] <= 0.20)
				&& UltimateOscillator(7, 14, 28)[0] <= 35.0
				//&& Close[0] <= Low[1]+(rangeSize/rangeDivisor/4)
				) 
			{
				enterLong = true;
			}
			else
			{
				enterLong = false;
			}
			
			//short
			if (
				KeyReversalDown(reversalPeriod)[0] == 1
				&& (DoubleStochastics(10)[0] >= 90.0 || DoubleStochastics(10)[0] <= 10.0)
				//&& (RSquared(8)[0] >= 0.75 || RSquared(8)[0] <= 0.20)
				&& UltimateOscillator(7, 14, 28)[0] >= 65.0
				//&& Close[0] >= High[1]-(rangeSize/rangeDivisor/4)
				)				
			{
				enterShort = true;
			}
			else
			{
				enterShort = false;
			}
						
			
			//take the position
			if (Position.MarketPosition == MarketPosition.Flat)
			{
			    if(enterLong)
			        EnterLong();
			    if(enterShort)
			        EnterShort();
			}
			
		}

		#region Properties
		
			[NinjaScriptProperty]
			[Range(6, 1024)]
			[Display(Name="Range Size", Description="RangeSize", Order=1, GroupName="Parameters")]
			public int rangeSize
			{ get; set; }			

			[NinjaScriptProperty]
			[Range(1.0, 32.0)]
			[Display(Name="Range Divisor", Description="RangeDivisor", Order=2, GroupName="Parameters")]
			public float rangeDivisor
			{ get; set; }			
		
			
		    [NinjaScriptProperty]
			[Range(6, 1024)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=4, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
			[NinjaScriptProperty]
			[Range(6, 1024)]
			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=5, GroupName="Parameters")]
			public int stopLoss
			{ get; set; }						

			[NinjaScriptProperty]
			[Range(2, 256)]
			[Display(Name="Reversal Period", Description="Reversal period", Order=6, GroupName="Parameters")]
			public int reversalPeriod
			{ get; set; }						

			
		#endregion
		
		
	}
}
