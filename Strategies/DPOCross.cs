
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
using static NinjaTrader.NinjaScript.Indicators.dpDPO;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class DPOCross : Strategy
    {
        #region Variables
  
		private dpDPO dp;
        private ATR atr;
		
        // Entry tracking
        private bool longEntryConditionMet = false;
        private bool shortEntryConditionMet = false;

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "DPO Cross Strategy";
                Name = "DPOCross";
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

            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
				
				TradingHours rth = TradingHours.Get("US Equities RTH");
				
				dp = dpDPO(14, 1, 14, 20.0, 0.01, true, true, true, true, Marker_Types.Triangle, 10, 10, 3.0, Marker_Types.Dot, 10, 10, true, MA_Type.EMA, 50, 22, 1, 26, 52, 9, 14);
				AddChartIndicator(dp);
                atr = ATR(14);
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure we have enough bars to calculate
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // Check if we're within RTH trading hours (if enabled)
            if (EnableRTHFilter && !IsWithinRTH()) return;

            // Calculate stop loss and target in ticks based on ATR
            int stopLossTicks = (int)(atr[0] * StopLossATRMultiplier / TickSize);
            int targetTicks = (int)(atr[0] * ProfitTargetATRMultiplier / TickSize);

            // Set stop loss and profit target for any new entries
            SetStopLoss(CalculationMode.Ticks, stopLossTicks);
            SetProfitTarget(CalculationMode.Ticks, targetTicks);

			if (dp.Crossings[0] == -1) 
			{
				if (Position.MarketPosition != MarketPosition.Short)
				{
					EnterShort();
				}
			}

			if (dp.Crossings[0] == 1 ) 
			{
				if (Position.MarketPosition != MarketPosition.Long)
				{
					EnterLong();
				}
			}
			
        }


        private bool IsWithinRTH()
        {
            // Check if current time is within RTH trading hours
            return Time[0].TimeOfDay >= RTHStartTime && 
                   Time[0].TimeOfDay <= RTHEndTime;
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
        {
            if (order.Name == "LongEntry" || order.Name == "ShortEntry")
            {
                if (orderState == OrderState.Filled)
                {
                    //Print(string.Format("Order Filled: {0} at {1} on {2}", order.Name, averageFillPrice, time));
                }
                else if (orderState == OrderState.Cancelled || orderState == OrderState.Rejected)
                {
                    //Print(string.Format("Order {0}: {1} - {2}", orderState, order.Name, comment));
                }
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.Name == "LongEntry" || execution.Order.Name == "ShortEntry")
            {
                //Print(string.Format("Execution: {0} - {1} contracts at {2}", execution.Order.Name, quantity, price));
            }

            if (marketPosition == MarketPosition.Flat)
            {
                //ExecuteEntries();
            }
        }

        #region Properties

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Stop Loss ATR Multiplier", Description = "Multiplier for ATR stop loss", Order = 1, GroupName = "Risk Management")]
        public double StopLossATRMultiplier { get; set; } = 1.0;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Profit Target ATR Multiplier", Description = "Multiplier for ATR profit target", Order = 2, GroupName = "Risk Management")]
        public double ProfitTargetATRMultiplier { get; set; } = 2.0;

        [NinjaScriptProperty]
        [Display(Name = "Enable RTH Filter", Description = "Enable/disable regular trading hours filter", Order = 1, GroupName = "Trading Hours")]
        public bool EnableRTHFilter { get; set; } = true;
		
        [NinjaScriptProperty]
        [Display(Name = "Start Time", Description = "RTH start time", Order = 9, GroupName = "Trading Hours")]
        public TimeSpan RTHStartTime { get; set; } = new TimeSpan(9, 30, 0);

        [NinjaScriptProperty]
        [Display(Name = "End Time", Description = "RTH end time", Order = 10, GroupName = "Trading Hours")]
        public TimeSpan RTHEndTime { get; set; } = new TimeSpan(16, 0, 0);
		
        
        #endregion
    }
}
