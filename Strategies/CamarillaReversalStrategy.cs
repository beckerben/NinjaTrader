
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
    public class CamarillaReversalStrategy : Strategy
    {
        #region Variables

        // Indicators
        private CamarillaPivots camarillaPivots;
        private OrderFlowVWAP vwapIndicator;
        private RSI rsiIndicator;
        private SMA volumeAverage;
		private ATR atrIndicator;

        // Strategy State Variables
        private double R3, R4, S3, S4;
        private double vwapValue, vwapUpper2StdDev, vwapLower2StdDev;
        private double currentRSI;
        private double avgVolume;

        // Divergence Detection Variables
        private List<double> priceHighs;
        private List<double> priceLows;
        private List<double> rsiHighs;
        private List<double> rsiLows;
        private List<int> highBars;
        private List<int> lowBars;

        // Entry tracking
        private bool longEntryConditionMet = false;
        private bool shortEntryConditionMet = false;

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Camarilla Pivot Reversal Strategy with VWAP, RSI, and Volume Confluence";
                Name = "CamarillaReversalStrategy";
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

                // Parameters
                RSIPeriod = 14;
                VolumeSpikePeriod = 20;
                VolumeMultiplier = 1.5;
                DivergenceLookback = 10;
				ATRMultiplier = 2;
				ATRPeriod = 14;
				ProfitTargetMultiplier = 2;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Day, 1);
            }
            else if (State == State.DataLoaded)
            {
				
				TradingHours rth = TradingHours.Get("US Equities RTH");
				
                // Initialize indicators
                camarillaPivots = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.DailyBars, 0, 0, 0, 20);
                vwapIndicator = OrderFlowVWAP(VWAPResolution.Standard, 
                    rth, 
                    VWAPStandardDeviations.Three, 
                    1.0, 2.0, 3.0);
                rsiIndicator = RSI(RSIPeriod,3);
                volumeAverage = SMA(Volume, VolumeSpikePeriod);
				atrIndicator = ATR(ATRPeriod);

                // Initialize collections for divergence detection
                priceHighs = new List<double>();
                priceLows = new List<double>();
                rsiHighs = new List<double>();
                rsiLows = new List<double>();
                highBars = new List<int>();
                lowBars = new List<int>();

                // Add chart indicators for visualization
                AddChartIndicator(camarillaPivots);
                AddChartIndicator(vwapIndicator);
                AddChartIndicator(rsiIndicator);
				
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure we have enough bars to calculate
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // Update indicator values
            UpdateIndicatorValues();

            // Update pivot levels
            UpdatePivotLevels();

            // Check for divergences
            CheckForDivergences();

            // Check if we're within RTH trading hours
            if (!IsWithinRTH()) return;

            // Check entry conditions
            CheckLongEntryConditions();
            CheckShortEntryConditions();

            // Execute entries
            ExecuteEntries();
        }

        private void UpdateIndicatorValues()
        {
            if (BarsInProgress == 0)
            {
                vwapValue = vwapIndicator.VWAP[0];
                vwapUpper2StdDev = vwapIndicator.StdDev2Upper[0];
                vwapLower2StdDev = vwapIndicator.StdDev2Lower[0];
                currentRSI = rsiIndicator[0];
                avgVolume = volumeAverage[0];
            }
        }

        private void UpdatePivotLevels()
        {
            if (camarillaPivots.R3.IsValidDataPoint(0))
            {
                R3 = camarillaPivots.R3[0];
                R4 = camarillaPivots.R4[0];
                S3 = camarillaPivots.S3[0];
                S4 = camarillaPivots.S4[0];
            }
        }

        private void CheckForDivergences()
        {
            // Simple pivot detection for divergence analysis
            if (CurrentBar >= DivergenceLookback + 1)
            {
                // Check for price high with RSI divergence
                if (High[1] > High[2] && High[1] > High[0])
                {
                    priceHighs.Add(High[1]);
                    rsiHighs.Add(rsiIndicator[1]);
                    highBars.Add(CurrentBar - 1);

                    // Keep only recent data
                    if (priceHighs.Count > DivergenceLookback)
                    {
                        priceHighs.RemoveAt(0);
                        rsiHighs.RemoveAt(0);
                        highBars.RemoveAt(0);
                    }
                }

                // Check for price low with RSI divergence
                if (Low[1] < Low[2] && Low[1] < Low[0])
                {
                    priceLows.Add(Low[1]);
                    rsiLows.Add(rsiIndicator[1]);
                    lowBars.Add(CurrentBar - 1);

                    // Keep only recent data
                    if (priceLows.Count > DivergenceLookback)
                    {
                        priceLows.RemoveAt(0);
                        rsiLows.RemoveAt(0);
                        lowBars.RemoveAt(0);
                    }
                }
            }
        }

        private bool CheckBullishDivergence()
        {
            if (priceLows.Count < 2 || rsiLows.Count < 2)
                return false;

            // Check if price made lower low but RSI made higher low
            int lastIndex = priceLows.Count - 1;
            return priceLows[lastIndex] < priceLows[lastIndex - 1] && 
                   rsiLows[lastIndex] > rsiLows[lastIndex - 1];
        }

        private bool CheckBearishDivergence()
        {
            if (priceHighs.Count < 2 || rsiHighs.Count < 2)
                return false;

            // Check if price made higher high but RSI made lower high
            int lastIndex = priceHighs.Count - 1;
            return priceHighs[lastIndex] > priceHighs[lastIndex - 1] && 
                   rsiHighs[lastIndex] < rsiHighs[lastIndex - 1];
        }

        private bool CheckVolumeSpike()
        {
            return Volume[0] > (avgVolume * VolumeMultiplier);
        }

        private bool IsWithinRTH()
        {
            // Check if current time is within RTH trading hours
            return Time[0].TimeOfDay >= RTHStartTime && 
                   Time[0].TimeOfDay <= RTHEndTime;
        }

        private void CheckLongEntryConditions()
        {
            longEntryConditionMet = false;

            if (R3 == 0 || S3 == 0) return; // Ensure pivot values are valid

            // Check if price has reached or fallen below S3
            bool atS3Level = Low[0] <= S3 && Close[0] > S3;
			//Check if price has fallen below S4 
		    bool atS4Level = Low[0] < S4 && Close[0] > S4;

            // Check if price is below VWAP -2σ band
            bool vwapConfluence = Close[0] < vwapLower2StdDev;

            // Check RSI conditions (oversold or bullish divergence)
            bool rsiOversold = currentRSI < 30;
            bool rsiDivergence = CheckBullishDivergence();
            bool rsiCondition = rsiOversold || rsiDivergence;

            // Check volume spike
            bool volumeSpike = CheckVolumeSpike();

		
		    if ((atS3Level || atS4Level) &&
				(
				1==1
				//&& vwapConfluence 
				&& rsiCondition 
				//&& volumeSpike
				))
		    {
		        longEntryConditionMet = true;
				Print(string.Format("Long Entry Condition Met at {0} - Price: {1}, S3: {2}, VWAP-2σ: {3}, RSI: {4}, Volume: {5}", 
                    Time[0], Close[0], S3, vwapLower2StdDev, currentRSI, Volume[0]));
		    }
        }

        private void CheckShortEntryConditions()
        {
            shortEntryConditionMet = false;

            if (R3 == 0 || S3 == 0) return; // Ensure pivot values are valid

            // Check if price has reached or exceeded R3 or R4
            bool atR3Level = High[0] >= R3 && Close[0] < R3;
			bool atR4Level = High[0] >= R4 && Close[0] < R4;

            // Check if price is above VWAP +2σ band
            bool vwapConfluence = Close[0] > vwapUpper2StdDev;

            // Check RSI conditions (overbought or bearish divergence)
            bool rsiOverbought = currentRSI > 70;
            bool rsiDivergence = CheckBearishDivergence();
            bool rsiCondition = rsiOverbought || rsiDivergence;

            // Check volume spike
            bool volumeSpike = CheckVolumeSpike();

            // All conditions must be met
            if ((atR3Level || atR4Level) &&
				(
				1==1
				//&& vwapConfluence 
				&& rsiCondition 
				//&& volumeSpike
				))
            {
                shortEntryConditionMet = true;
                Print(string.Format("Short Entry Condition Met at {0} - Price: {1}, R3: {2}, VWAP+2σ: {3}, RSI: {4}, Volume: {5}", 
                    Time[0], Close[0], R3, vwapUpper2StdDev, currentRSI, Volume[0]));
            }
        }

		private void ExecuteEntries()
		{
		    // Long entries
		    if (longEntryConditionMet && Position.MarketPosition == MarketPosition.Flat)
		    {
		        // Calculate stop loss and target offsets in ticks
		        int stopLossTicks = (int)((ATRMultiplier * atrIndicator[0]) / TickSize);
		        int targetTicks = (int)((ProfitTargetMultiplier * ATRMultiplier * atrIndicator[0]) / TickSize);
		
		        Print(string.Format("LONG ENTRY - ATR: {0:F2}, ATRMultiplier: {1}, ProfitTargetMultiplier: {2}, StopLossTicks: {3}, TargetTicks: {4}, CurrentPrice: {5:F2}", 
		            atrIndicator[0], ATRMultiplier, ProfitTargetMultiplier, stopLossTicks, targetTicks, Close[0]));
		
		        EnterLong("LongEntry");
		        SetStopLoss("LongEntry", CalculationMode.Ticks, stopLossTicks, false);
		        SetProfitTarget("LongEntry", CalculationMode.Ticks, targetTicks);
		    }
		
		    // Short entries
		    if (shortEntryConditionMet && Position.MarketPosition == MarketPosition.Flat)
		    {
		        // Calculate stop loss and target offsets in ticks
		        int stopLossTicks = (int)((ATRMultiplier * atrIndicator[0]) / TickSize);
		        int targetTicks = (int)((ProfitTargetMultiplier * ATRMultiplier * atrIndicator[0]) / TickSize);
		
		        Print(string.Format("SHORT ENTRY - ATR: {0:F2}, ATRMultiplier: {1}, ProfitTargetMultiplier: {2}, StopLossTicks: {3}, TargetTicks: {4}, CurrentPrice: {5:F2}", 
		            atrIndicator[0], ATRMultiplier, ProfitTargetMultiplier, stopLossTicks, targetTicks, Close[0]));
		
		        EnterShort("ShortEntry");
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
                    Print(string.Format("Order Filled: {0} at {1} on {2}", order.Name, averageFillPrice, time));
                }
                else if (orderState == OrderState.Cancelled || orderState == OrderState.Rejected)
                {
                    Print(string.Format("Order {0}: {1} - {2}", orderState, order.Name, comment));
                }
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.Name == "LongEntry" || execution.Order.Name == "ShortEntry")
            {
                Print(string.Format("Execution: {0} - {1} contracts at {2}", execution.Order.Name, quantity, price));
            }
        }

        #region Properties

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "RSI Period", Description = "Period for RSI calculation", Order = 1, GroupName = "Indicators")]
        public int RSIPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Volume Spike Period", Description = "Period for volume average calculation", Order = 2, GroupName = "Indicators")]
        public int VolumeSpikePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 5.0)]
        [Display(Name = "Volume Multiplier", Description = "Multiplier for volume spike detection", Order = 3, GroupName = "Indicators")]
        public double VolumeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(2, 50)]
        [Display(Name = "Divergence Lookback", Description = "Number of bars to look back for divergence", Order = 4, GroupName = "Indicators")]
        public int DivergenceLookback { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "ATR Period", Description = "Period for ATR calculation", Order = 6, GroupName = "Indicators")]
		public int ATRPeriod { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "ATR Multiplier", Description = "Multiplier for ATR stop loss", Order = 7, GroupName = "Entry")]
		public double ATRMultiplier { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Profit Target Multiplier", Description = "Multiplier for ATR profit target (relative to stop loss)", Order = 8, GroupName = "Entry")]
		public double ProfitTargetMultiplier { get; set; }
		
        [NinjaScriptProperty]
        [Display(Name = "Start Time", Description = "RTH start time", Order = 9, GroupName = "Trading Hours")]
        public TimeSpan RTHStartTime { get; set; } = new TimeSpan(9, 30, 0);

        [NinjaScriptProperty]
        [Display(Name = "End Time", Description = "RTH end time", Order = 10, GroupName = "Trading Hours")]
        public TimeSpan RTHEndTime { get; set; } = new TimeSpan(16, 0, 0);
		
        #endregion
    }
}
