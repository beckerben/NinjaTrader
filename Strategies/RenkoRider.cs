
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

namespace NinjaTrader.NinjaScript.Strategies
{
    public class RenkoRider : Strategy
    {
        #region Variables
  
        private int renkoBarSize; // Add this to store the Renko bar size

        // Entry tracking
        private bool longEntryConditionMet = false;
        private bool shortEntryConditionMet = false;

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Aggressive Renko trend following strategy";
                Name = "RenkoRider";
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
				
                // Query and store the Renko bar size
                if (BarsPeriod.BarsPeriodType == BarsPeriodType.Renko)
                {
                    renkoBarSize = BarsPeriod.Value;
                    Print("Renko bar size detected: " + renkoBarSize);
                }
                else
                {
                    Print("Warning: This strategy is designed for Renko charts but is running on a different chart type.");
                }
				
				
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure we have enough bars to calculate
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // Check if we're within RTH trading hours
            if (!IsWithinRTH()) return;

            if (Close[0] > Close[1])
            {
                CheckLongEntryConditions();
                longEntryConditionMet = true;
                shortEntryConditionMet = false;
            }
            else
            {
                CheckShortEntryConditions();   
                longEntryConditionMet = false;
                shortEntryConditionMet = true;
            }

            // Execute entries
            ExecuteEntries();
        }


        private bool IsWithinRTH()
        {
            // Check if current time is within RTH trading hours
            return Time[0].TimeOfDay >= RTHStartTime && 
                   Time[0].TimeOfDay <= RTHEndTime;
        }

        private void CheckLongEntryConditions()
        {
            //todo: add conditions
        }

        private void CheckShortEntryConditions()
        {
            //todo: add conditions
        }

		private void ExecuteEntries()
		{
		    // Long entries
		    if (longEntryConditionMet && Position.MarketPosition == MarketPosition.Flat)
		    {
		        // Calculate stop loss and target offsets in ticks
		        int stopLossTicks = renkoBarSize;
		        int targetTicks = (int)(2.0 * renkoBarSize);
		
		
		        EnterLongLimit(Low[0], "LongEntry");
		        SetStopLoss("LongEntry", CalculationMode.Ticks, stopLossTicks, false);
		        SetProfitTarget("LongEntry", CalculationMode.Ticks, targetTicks);
		    }
		
		    // Short entries
		    if (shortEntryConditionMet && Position.MarketPosition == MarketPosition.Flat)
		    {
		        // Calculate stop loss and target offsets in ticks
		        int stopLossTicks = renkoBarSize;
		        int targetTicks = (int)(2.0 * renkoBarSize);
		
				EnterShortLimit(High[0], "ShortEntry");
		        SetStopLoss("ShortEntry", CalculationMode.Ticks, stopLossTicks, false);
		        SetProfitTarget("ShortEntry", CalculationMode.Ticks, targetTicks);
		    }
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
        [Display(Name = "Start Time", Description = "RTH start time", Order = 9, GroupName = "Trading Hours")]
        public TimeSpan RTHStartTime { get; set; } = new TimeSpan(9, 30, 0);

        [NinjaScriptProperty]
        [Display(Name = "End Time", Description = "RTH end time", Order = 10, GroupName = "Trading Hours")]
        public TimeSpan RTHEndTime { get; set; } = new TimeSpan(16, 0, 0);
		
        // Property to access the detected Renko bar size
        [Display(Name = "Renko Bar Size", Description = "The detected Renko bar size from the chart", Order = 11, GroupName = "Chart Info")]
        public int RenkoBarSize { get { return renkoBarSize; } }
		
        #endregion
    }
}
