
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
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class EMACross : Strategy
    {
        private EMA fastEMA;
        private EMA slowEMA;
        private MACD macd;
        private SMA volSMA;
        private int fastEMAPeriod = 13;
        private int slowEMAPeriod = 26;
        private int macdFast = 12, macdSlow = 26, macdSmooth = 9;
        private int volSMAPeriod = 20;
        private int stopLossTicks = 10;
        private int takeProfitTicks = 20;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "NQ Futures Scalping Strategy with 1:2 Risk-Reward";
                Name = "EMACross";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 50;
                IsInstantiatedOnEachOptimizationIteration = true;

                // Trading session restrictions (9:30 AM - 4:00 PM ET)
                bool IsSessionStartTimeEnabled = true;
                TimeSpan SessionStartTime = new TimeSpan(9, 30, 0);
                bool IsSessionEndTimeEnabled = true;
                TimeSpan SessionEndTime = new TimeSpan(16, 0, 0);
            }

            else if (State == State.Configure)
            {
                SetProfitTarget(CalculationMode.Ticks, takeProfitTicks);
                SetStopLoss(CalculationMode.Ticks, stopLossTicks);
            }

            else if (State == State.DataLoaded)
            {
                // Initialize indicators
                fastEMA = EMA(fastEMAPeriod);
                slowEMA = EMA(slowEMAPeriod);
                macd = MACD(macdFast, macdSlow, macdSmooth);
                volSMA = SMA(Volume, volSMAPeriod);

                // Add indicators to chart
                AddChartIndicator(fastEMA);
                AddChartIndicator(slowEMA);
                AddChartIndicator(macd);
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure we have enough bars for calculation
            if (CurrentBars[0] < BarsRequiredToTrade)
                return;

            // Check session time (9:30 AM - 4:00 PM ET only)
            if (!IsValidTradingTime())
                return;

            // Long entry conditions
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                // Bullish momentum scalping setup
                if (CrossAbove(fastEMA, slowEMA, 1) &&
                    macd.Default[0] > 0 &&
                    Volume[0] > volSMA[0])
                {
                    EnterLong(1, "Long Entry");
                }

                // Short entry conditions
                else if (CrossBelow(fastEMA, slowEMA, 1) &&
                         macd.Default[0] < 0 &&
                         Volume[0] > volSMA[0])
                {
                    EnterShort(1, "Short Entry");
                }
            }
        }

        private bool IsValidTradingTime()
        {
            TimeSpan currentTime = Time[0].TimeOfDay;
            TimeSpan sessionStart = new TimeSpan(9, 30, 0);
            TimeSpan sessionEnd = new TimeSpan(16, 0, 0);

            return currentTime >= sessionStart && currentTime <= sessionEnd;
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, 
                                            int quantity, int filled, double averageFillPrice, 
                                            OrderState orderState, DateTime time, ErrorCode error, string comment)
        {

		}

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Stop Loss Ticks", Order=1, GroupName="Parameters")]
        public int StopLossTicks
        {
            get { return stopLossTicks; }
            set { stopLossTicks = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Take Profit Ticks", Order=2, GroupName="Parameters")]
        public int TakeProfitTicks
        {
            get { return takeProfitTicks; }
            set { takeProfitTicks = Math.Max(1, value); }
        }


        #endregion
    }
}
