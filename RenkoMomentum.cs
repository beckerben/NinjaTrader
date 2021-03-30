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
	/// Strategy hypothesis, in times of volatility and consistent / quick ups and downs, this strategy will enter and follow
	/// when the renko trend shifts, an entry will be made or reversed to follow the trend, the goal is to follow continual
	/// climbs up / down.  Entries and stops set based on renko bar height + small buffer.  Position reverses when trend flips.
	/// </summary>
	public class RenkoMomentum : Strategy
	{
		bool trendUp = false; 
		bool trendSwitched = false;
		private Order entryOrder = null;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"This is a test of Ben Becker's hypothesis to follow Renko consecutive trend, ideal for volatile market vs sideways (in theory)";
				Name										= "RenkoMomentum";
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
				BarsRequiredToTrade							= 10;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				RenkoBarSize = 12;
				
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks,(RenkoBarSize*1.5)+2);
				SetTrailStop(CalculationMode.Ticks,(RenkoBarSize*2)+2);
				RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
				//SetStopLoss(CalculationMode.Ticks,(RenkoBarSize*1.5)+2);
			}
		}

		protected override void OnBarUpdate()
		{
			//sanity check we are using a renko series
			if(BarsPeriod.BarsPeriodType != BarsPeriodType.Renko)
			{
				Draw.TextFixed(this, "NinjaScriptInfo", "The RenkoStrategy must be ran on a Renko chart.", TextPosition.BottomRight);
				return;
			}
			
			//check if we have enough bars to trade
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			//lets determine if this bar is higher than the prior bar
			if (Close[0] > Close[1])
			{
				if (!trendUp) 	
					trendSwitched = true;	
				else
					trendSwitched = false;
				trendUp = true;
			}
			else
			{
				if (trendUp)
					trendSwitched = true;
				else
					trendSwitched = false;
				trendUp = false;
			}
			
			//if no position, enter long only
			if(Position.MarketPosition == MarketPosition.Flat)
			{
				//if we are trending down, set a stop limit to enter a long based on the high one bar back
				if (trendUp && trendSwitched)
					EnterLong();
				
				if (!trendUp && trendSwitched)
					EnterShort();

			}

			
		}
		

		#region Properties

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="RenkoBarSize", Description="The configured renko bar height", Order=0, GroupName="Config")]
		public int RenkoBarSize
		{ get; set; }

		#endregion		
		
		
	}
}
