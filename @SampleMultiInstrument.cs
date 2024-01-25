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
	public class SampleMultiInstrument : Strategy
	{
		private RSI rsi;
		private ADX adx;
		private ADX adx1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= NinjaTrader.Custom.Resource.NinjaScriptStrategyDescriptionSampleMultiInstrument;
				Name		= NinjaTrader.Custom.Resource.NinjaScriptStrategyNameSampleMultiInstrument;
				// This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = false;
			}
			else if (State == State.Configure)
			{
				// Add an MSFT 1 minute Bars object to the strategy
				AddDataSeries("MSFT", Data.BarsPeriodType.Minute, 1);

				// Sets a 20 tick trailing stop for an open position
				SetTrailStop(CalculationMode.Ticks, 20);
			}
			else if (State == State.DataLoaded)
			{
				// Instantiate indicators in State.DataLoaded is the best practice.
				rsi = RSI(14, 1);
				adx = ADX(14);
				
				// Note: Bars are added to the BarsArray and can be accessed via an index value
				// E.G. BarsArray[1] ---> Accesses the 1 minute Bars added above
				adx1 = ADX(BarsArray[1], 14);

				// Add RSI and ADX indicators to the chart for display
				// This only displays the indicators for the primary Bars object (main instrument) on the chart
				// Note:  Only indicators using the chart data primary series can be displayed using AddChartIndicator()
				AddChartIndicator(rsi);
				AddChartIndicator(adx);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 0 || CurrentBars[1] < 0)
				return;

			// OnBarUpdate() will be called on incoming tick events on all Bars objects added to the strategy
			// We only want to process events on our primary Bars object (main instrument) (index = 0) which
			// is set when adding the strategy to a chart
			if (BarsInProgress != 0)
				return;

			// Checks if the 14 period ADX on both instruments are trending (above a value of 30)
			if (adx[0] > 30 && adx1[0] > 30)
			{
				// If RSI crosses above a value of 30 then enter a long position via a limit order
				if (CrossAbove(rsi, 30, 1))
				{
					// Draws a square 1 tick above the high of the bar identifying when a limit order is issued
					Draw.Square(this, "My Square" + CurrentBar, false, 0, High[0] + TickSize, Brushes.DodgerBlue);

					// Enter a long position via a limit order at the current ask price
					EnterLongLimit(GetCurrentAsk(), "RSI");
				}
			}

			// Any open long position will exit if RSI crosses below a value of 75
			// This is in addition to the trail stop set in the OnStateChange() method under State.Configure
			if (CrossBelow(rsi, 75, 1))
				ExitLong();
		}
	}
}
