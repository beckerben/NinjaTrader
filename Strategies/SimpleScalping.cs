#region Using declarations
using System;
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

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    public class SimpleScalping : Strategy
    {
        private SMA sma10;
        private SMA sma20;
        private RSI rsi14;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Simple Scalping Strategy";
                Name = "SimpleScalping";
                Calculate = Calculate.OnBarClose;
            }
            else if (State == State.Configure)
            {
                // Instantiate indicators
                sma10 = SMA(10);
                sma20 = SMA(20);
                rsi14 = RSI(14, 3);
                
                // Add indicators to chart (uncomment below if you want to see the indicators in the chart)
                 //AddChartIndicator(sma10);
                 //AddChartIndicator(sma20);
                 //AddChartIndicator(rsi14);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20)
                return;

            // Buy condition
            if (CrossAbove(sma10, sma20, 1) && rsi14[1] < 30 && rsi14[0] > 30)
            {
                EnterLong();
            }

            // Sell condition
            if (CrossBelow(sma10, sma20, 1) && rsi14[1] > 70 && rsi14[0] < 70)
            {
                EnterShort();
            }

            // Exit condition for long position
            if (Position.MarketPosition == MarketPosition.Long && (CrossBelow(sma10, sma20, 1) || rsi14[0] > 70))
            {
                ExitLong();
            }

            // Exit condition for short position
            if (Position.MarketPosition == MarketPosition.Short && (CrossAbove(sma10, sma20, 1) || rsi14[0] < 30))
            {
                ExitShort();
            }
        }
    }
}
