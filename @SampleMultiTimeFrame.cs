//
// Copyright (C) 2023, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SampleMultiTimeFrame : Strategy
	{
		private SMA sma50B0;
		private SMA sma50B1;
		private SMA sma50B2;
		private SMA sma5B0;
		private SMA sma5B1;
		private SMA sma5B2;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= NinjaTrader.Custom.Resource.NinjaScriptStrategyDescriptionSampleMultiTimeFrame;
				Name		= NinjaTrader.Custom.Resource.NinjaScriptStrategyNameSampleMultiTimeFrame;
				// This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = false;
			}
			else if (State == State.Configure)
			{
				// Add a 5 minute Bars object to the strategy
				AddDataSeries(Data.BarsPeriodType.Minute, 5);

				// Add a 15 minute Bars object to the strategy
				AddDataSeries(Data.BarsPeriodType.Minute, 15);
			}
			else if (State == State.DataLoaded)
			{
				// Best practice is to instantiate indicators in State.DataLoaded.
				sma50B0 = SMA(50);
				sma5B0 = SMA(5);
				
				// Note: Bars are added to the BarsArray and can be accessed via an index value
				// E.G. BarsArray[1] ---> Accesses the 5 minute Bars object added above
				sma50B1 = SMA(BarsArray[1], 50);
				sma50B2 = SMA(BarsArray[2], 50);
				sma5B1  = SMA(BarsArray[1], 5);
				sma5B2  = SMA(BarsArray[2], 5);

				// Add simple moving averages to the chart for display
				// This only displays the SMA's for the primary Bars object on the chart
				// Note only indicators based on the charts primary data series can be added.
				AddChartIndicator(sma5B0);
				AddChartIndicator(sma50B0);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
				return;
		
			// OnBarUpdate() will be called on incoming tick events on all Bars objects added to the strategy
			// We only want to process events on our primary Bars object (index = 0) which is set when adding
			// the strategy to a chart
			if (BarsInProgress != 0)
				return;		

			// Checks  if the 5 period SMA is above the 50 period SMA on both the 5 and 15 minute time frames
			if (sma5B1[0] > sma50B1[0] && sma5B2[0] > sma50B2[0])
			{
				// Checks for a cross above condition of the 5 and 50 period SMA on the primary Bars object and enters long
				if (CrossAbove(sma5B0, sma50B0, 1))
				{
					EnterLong(1000, "SMA");
				}
			}

			// Checks for a cross below condition of the 5 and 15 period SMA on the 15 minute time frame and exits long
			if (CrossBelow(sma5B2, sma50B2, 1))
				ExitLong(1000);
		}
	}
}
