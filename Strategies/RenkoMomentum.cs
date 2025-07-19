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
		bool longOk = false;
		bool shortOk = false;
		
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
				RenkoBarSize = 40;
				SlippageTick = 3;
				
			}
			else if (State == State.Configure)
			{
				//SetProfitTarget(CalculationMode.Ticks,(RenkoBarSize*2.5));
				//SetStopLoss(CalculationMode.Ticks,(RenkoBarSize*2)+SlippageTick);
				SetTrailStop(CalculationMode.Ticks,(RenkoBarSize*2.0)+SlippageTick);
				RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
			}
			else if (State == State.DataLoaded)
			{
				AddChartIndicator(WoodiesCCI(2, 5, 14, 34, 25, 6, 60, 100, 2));	
			}			
		}

		protected override void OnBarUpdate()
		{
			//sanity check we are using a renko series
//			if(BarsPeriod.BarsPeriodType != BarsPeriodType.Renko)
//			{
//				Draw.TextFixed(this, "NinjaScriptInfo", "The RenkoStrategy must be ran on a Renko chart.", TextPosition.BottomRight);
//				return;
//			}
			
			//check if we have enough bars to trade
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			int zoneColor = Convert.ToInt32(WoodiesCCI(2, 5, 14, 34, 25, 6, 60, 100, 2).ZoneBars[0]);
			switch (zoneColor)
			{
			    case 0: //negative red
			        longOk = true;
			        shortOk = true;
			        break;
			    case 1: //positive blue
			        longOk = true;
			        shortOk = true;
			        break;
			    case 2: //neutral gray
			        longOk = true;
			        shortOk = true;
			        break;
			    case 3: //last neutral yellow
			        longOk = true;
			        shortOk = true;
			        break;
			}

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

			if (Position.MarketPosition == MarketPosition.Flat)
			{
			    if(trendUp && trendSwitched && longOk)
			        EnterLong();
			    if(!trendUp && trendSwitched && shortOk)
			        EnterShort();
			}
			
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="RenkoBarSize", Description="The configured renko bar height", Order=0, GroupName="Config")]
		public int RenkoBarSize
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="SlippageTick", Description="The added buffer ticks to add to target entry / exit", Order=1, GroupName="Config")]
		public int SlippageTick
		{ get; set; }
		
		#endregion		
		
		
	}
}
