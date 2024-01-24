using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MacdCrossStrategy : Strategy
    {
        private MACD macd;
        private int profitTargetTicks;
        private int stopLossTicks;
        private int fast;
        private int slow;
        private int smooth;
		private bool reverseOnCrossover;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "A strategy that uses the MACD indicator for entry and exit.";
                Name = "MacdCrossStrategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
				
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.ByStrategyPosition;
                BarsRequiredToTrade = 20;
				
                // Default setting for MACD
                fast = 12;
                slow = 26;
                smooth = 9;
                profitTargetTicks = 10;
                stopLossTicks = 10;
				ReverseOnCrossover = false;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Minute, 1);
            }
            else if (State == State.DataLoaded)
            {
                macd = MACD(Fast, Slow, Smooth);
                AddChartIndicator(macd);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade)
                return;

            var macd = MACD(Fast, Slow, Smooth);

            if (macd.Diff[1] < 0 && macd.Diff[0] > 0)
            {
                if (ReverseOnCrossover && Position.MarketPosition == MarketPosition.Short)
                {
                    ExitShort();
                }

                EnterLong();
            }
            else if (macd.Diff[1] > 0 && macd.Diff[0] < 0)
            {
                if (ReverseOnCrossover && Position.MarketPosition == MarketPosition.Long)
                {
                    ExitLong();
                }

                EnterShort();
            }

            if (!ReverseOnCrossover)
            {
                if (Position.MarketPosition == MarketPosition.Long)
                {
                    SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks);
                    SetStopLoss(CalculationMode.Ticks, StopLossTicks);
                }
                else if (Position.MarketPosition == MarketPosition.Short)
                {
                    SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks);
                    SetStopLoss(CalculationMode.Ticks, StopLossTicks);
                }
            }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
        public int Fast
        {
            get { return fast; }
                        set { fast = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
        public int Slow
        {
            get { return slow; }
            set { slow = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptStrategyParameters", Order = 2)]
        public int Smooth
        {
            get { return smooth; }
            set { smooth = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Profit Target Ticks", GroupName = "NinjaScriptStrategyParameters", Order = 3)]
        public int ProfitTargetTicks
        {
            get { return profitTargetTicks; }
            set { profitTargetTicks = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Stop Loss Ticks", GroupName = "NinjaScriptStrategyParameters", Order = 4)]
        public int StopLossTicks
        {
            get { return stopLossTicks; }
            set { stopLossTicks = Math.Max(1, value); }
        }
		
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Reverse On Crossover", GroupName = "NinjaScriptStrategyParameters", Order = 5)]
        public bool ReverseOnCrossover
        {
            get { return reverseOnCrossover; }
            set { reverseOnCrossover = value; }
        }		
    }
}

