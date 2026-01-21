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

// This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    public class FairValueGapScalper : Strategy
    {
        #region Variables
        private double sessionHigh = double.MinValue;
        private double sessionLow = double.MaxValue;
        private bool sessionRangeSet = false;
        private DateTime sessionStartTime;
        private bool fairValueGapDetected = false;
        private bool breakoutConfirmed = false;
        private double entryPrice = 0;
        private double stopLoss = 0;
        private double profitTarget = 0;
        private bool longTrade = false;
        private bool shortTrade = false;
        private double avgVolume = 0;
        private int volumePeriod = 20;
        private List<double> volumeList = new List<double>();
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                     = @"Fair Value Gap Scalping Strategy with Volume and ATR Confirmations";
                Name                           = "FairValueGapScalper";
                Calculate                      = Calculate.OnEachTick;
                EntriesPerDirection            = 1;
                EntryHandling                  = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy   = true;
                ExitOnSessionCloseSeconds      = 30;
                IsFillLimitOnTouch             = false;
                MaximumBarsLookBack            = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution            = OrderFillResolution.Standard;
                Slippage                       = 0;
                StartBehavior                  = StartBehavior.WaitUntilFlat;
                TimeInForce                    = TimeInForce.Gtc;
                TraceOrders                    = false;
                RealtimeErrorHandling          = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling             = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade            = 20;
                IsInstantiatedOnEachOptimizationIteration = true;
                
                // User configurable parameters
                SessionPeriodMinutes           = 5;
                EnableVolumeConfirmation       = false; // Disabled by default for testing
                EnableCandleSizeConfirmation   = false; // Disabled by default for testing
                ATRMultiplier                  = 1.25;
                ATRPeriod                      = 14;
                VolumeMultiplier               = 1.5;
                StartHour                      = 9;
                StartMinute                    = 30;
                RiskRewardRatio                = 2.0;
                TestMode                       = true; // Enable test mode by default
            }
            else if (State == State.Configure)
            {
                // Add 1-minute data series for fair value gap detection
                AddDataSeries(Data.BarsPeriodType.Minute, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Initialize session time for today
                sessionStartTime = DateTime.Today.AddHours(StartHour).AddMinutes(StartMinute);
                Print("Strategy initialized. Session start time: " + sessionStartTime);
            }
        }

        protected override void OnBarUpdate()
        {
            // Only trade during market hours and after minimum bars requirement
            if (CurrentBars[0] < BarsRequiredToTrade || CurrentBars[1] < BarsRequiredToTrade)
                return;

            // Primary timeframe (default chart timeframe)
            if (BarsInProgress == 0)
            {
                UpdateSessionRange();
            }
            
            // 1-minute timeframe for fair value gap detection
            if (BarsInProgress == 1)
            {
                if (sessionRangeSet)
                {
                    CheckForBreakout();
                    if (breakoutConfirmed)
                    {
                        DetectFairValueGap();
                    }
                }
            }
        }

        private void UpdateSessionRange()
        {
            DateTime currentTime = Time[0];
            
            // In test mode, set session range immediately
            if (TestMode && !sessionRangeSet)
            {
                Print("Test mode: Setting session range immediately");
                SetSessionRange();
                sessionRangeSet = true;
                fairValueGapDetected = false;
                breakoutConfirmed = false;
                
                // Draw session high and low lines
                Draw.Line(this, "SessionHigh" + CurrentBar, false, 0, sessionHigh, -10, sessionHigh, 
                    Brushes.Red, DashStyleHelper.Solid, 2);
                Draw.Line(this, "SessionLow" + CurrentBar, false, 0, sessionLow, -10, sessionLow, 
                    Brushes.Blue, DashStyleHelper.Solid, 2);
                
                Print(String.Format("Session Range Set - High: {0}, Low: {1}", sessionHigh, sessionLow));
                return;
            }
            
            // Check if we're at the start of the session
            if (IsTimeToSetSessionRange(currentTime))
            {
                SetSessionRange();
                sessionRangeSet = true;
                fairValueGapDetected = false;
                breakoutConfirmed = false;
                
                // Draw session high and low lines
                Draw.Line(this, "SessionHigh" + CurrentBar, false, 0, sessionHigh, -10, sessionHigh, 
                    Brushes.Red, DashStyleHelper.Solid, 2);
                Draw.Line(this, "SessionLow" + CurrentBar, false, 0, sessionLow, -10, sessionLow, 
                    Brushes.Blue, DashStyleHelper.Solid, 2);
                
                Print(String.Format("Session Range Set - High: {0}, Low: {1}", sessionHigh, sessionLow));
            }
        }

        private bool IsTimeToSetSessionRange(DateTime currentTime)
        {
            // Check if current time is within the session window
            DateTime sessionEndTime = sessionStartTime.AddMinutes(SessionPeriodMinutes);
            bool inSessionWindow = currentTime >= sessionStartTime && currentTime <= sessionEndTime;
            bool notAlreadySet = !sessionRangeSet;
            
            // If we're past the session window and haven't set the range yet, set it anyway
            bool pastSessionWindow = currentTime > sessionEndTime;
            bool shouldSetAnyway = pastSessionWindow && notAlreadySet;
            
            Print(String.Format("Time check: Current={0}, SessionStart={1}, SessionEnd={2}, InWindow={3}, NotSet={4}, PastWindow={5}, ShouldSetAnyway={6}", 
                currentTime, sessionStartTime, sessionEndTime, inSessionWindow, notAlreadySet, pastSessionWindow, shouldSetAnyway));
            
            return (inSessionWindow && notAlreadySet) || shouldSetAnyway;
        }

        private void SetSessionRange()
        {
            // Calculate session range from the beginning of the session period
            sessionHigh = double.MinValue;
            sessionLow = double.MaxValue;
            
            // Look back from current bar to find the session period
            int barsToCheck = Math.Min(SessionPeriodMinutes, CurrentBar);
            
            for (int i = 0; i < barsToCheck; i++)
            {
                sessionHigh = Math.Max(sessionHigh, High[i]);
                sessionLow = Math.Min(sessionLow, Low[i]);
            }
            
            Print(String.Format("Session range calculated from {0} bars: High={1}, Low={2}", 
                barsToCheck, sessionHigh, sessionLow));
        }

        private void CheckForBreakout()
        {
            if (!sessionRangeSet || Position.MarketPosition != MarketPosition.Flat)
            {
                Print(String.Format("Breakout check skipped: sessionRangeSet={0}, Position={1}", 
                    sessionRangeSet, Position.MarketPosition));
                return;
            }

            double currentHigh = Highs[1][0];
            double currentLow = Lows[1][0];
            double currentClose = Closes[1][0];
            
            Print(String.Format("Checking breakout: Close={0}, SessionHigh={1}, SessionLow={2}", 
                currentClose, sessionHigh, sessionLow));
            
            // In test mode, trigger breakout on any price movement
            if (TestMode)
            {
                Print("Test mode: Triggering breakout for testing");
                breakoutConfirmed = true;
                longTrade = true;
                shortTrade = false;
                return;
            }
            
            // Check for energetic push through session high
            if (currentClose > sessionHigh)
            {
                Print("Price above session high, checking energetic move...");
                if (IsEnergeticMove(true))
                {
                    breakoutConfirmed = true;
                    longTrade = true;
                    shortTrade = false;
                    Print("Bullish breakout confirmed above session high: " + sessionHigh);
                }
                else
                {
                    Print("Bullish breakout failed energetic move check");
                }
            }
            // Check for energetic push through session low
            else if (currentClose < sessionLow)
            {
                Print("Price below session low, checking energetic move...");
                if (IsEnergeticMove(false))
                {
                    breakoutConfirmed = true;
                    longTrade = false;
                    shortTrade = true;
                    Print("Bearish breakout confirmed below session low: " + sessionLow);
                }
                else
                {
                    Print("Bearish breakout failed energetic move check");
                }
            }
        }

        private bool IsEnergeticMove(bool isUpMove)
        {
            bool volumeConfirmed = true;
            bool candleSizeConfirmed = true;
            
            // Volume confirmation
            if (EnableVolumeConfirmation)
            {
                volumeConfirmed = CheckVolumeConfirmation();
                Print("Volume confirmation: " + volumeConfirmed);
            }
            
            // Candle size confirmation using ATR
            if (EnableCandleSizeConfirmation)
            {
                candleSizeConfirmed = CheckCandleSizeConfirmation();
                Print("Candle size confirmation: " + candleSizeConfirmed);
            }
            
            bool result = volumeConfirmed && candleSizeConfirmed;
            Print("Energetic move result: " + result);
            return result;
        }

        private bool CheckVolumeConfirmation()
        {
            double currentVolume = Volumes[1][0];
            
            // Calculate average volume
            UpdateAverageVolume(currentVolume);
            
            if (avgVolume > 0)
            {
                double volumeThreshold = avgVolume * VolumeMultiplier;
                bool volumeConfirmed = currentVolume > volumeThreshold;
                
                if (volumeConfirmed)
                {
                    Print(String.Format("Volume confirmation: Current={0}, Avg={1}, Threshold={2}", 
                        currentVolume, avgVolume, volumeThreshold));
                }
                
                return volumeConfirmed;
            }
            
            return true; // If no volume data available, don't block the trade
        }

        private void UpdateAverageVolume(double currentVolume)
        {
            volumeList.Add(currentVolume);
            
            if (volumeList.Count > volumePeriod)
            {
                volumeList.RemoveAt(0);
            }
            
            if (volumeList.Count > 0)
            {
                avgVolume = volumeList.Average();
            }
        }

        private bool CheckCandleSizeConfirmation()
        {
            double candleRange = Highs[1][0] - Lows[1][0];
            double atrValue = ATR(ATRPeriod)[0] * ATRMultiplier;
            
            bool candleSizeConfirmed = candleRange >= atrValue;
            
            if (candleSizeConfirmed)
            {
                Print(String.Format("Candle size confirmation: Range={0}, ATR Threshold={1}", 
                    candleRange, atrValue));
            }
            
            return candleSizeConfirmed;
        }

        private void DetectFairValueGap()
        {
            if (CurrentBars[1] < 3 || fairValueGapDetected)
            {
                Print(String.Format("FVG detection skipped: CurrentBars[1]={0}, fairValueGapDetected={1}", 
                    CurrentBars[1], fairValueGapDetected));
                return;
            }

            // In test mode, trigger FVG detection immediately
            if (TestMode)
            {
                Print("Test mode: Triggering FVG detection for testing");
                fairValueGapDetected = true;
                ExecuteTrade();
                return;
            }

            // Check for 3-candle Fair Value Gap pattern
            double candle1High = Highs[1][2];
            double candle1Low = Lows[1][2];
            double candle2High = Highs[1][1];
            double candle2Low = Lows[1][1];
            double candle3High = Highs[1][0];
            double candle3Low = Lows[1][0];
            
            Print(String.Format("FVG Check - Candle1: H={0}, L={1}; Candle2: H={2}, L={3}; Candle3: H={4}, L={5}", 
                candle1High, candle1Low, candle2High, candle2Low, candle3High, candle3Low));
            
            bool bullishFVG = false;
            bool bearishFVG = false;
            
            if (longTrade)
            {
                // Bullish Fair Value Gap: Gap between candle1 high and candle3 low
                bullishFVG = candle1High < candle3Low;
                Print(String.Format("Bullish FVG check: candle1High({0}) < candle3Low({1}) = {2}", 
                    candle1High, candle3Low, bullishFVG));
            }
            else if (shortTrade)
            {
                // Bearish Fair Value Gap: Gap between candle1 low and candle3 high
                bearishFVG = candle1Low > candle3High;
                Print(String.Format("Bearish FVG check: candle1Low({0}) > candle3High({1}) = {2}", 
                    candle1Low, candle3High, bearishFVG));
            }
            
            if (bullishFVG || bearishFVG)
            {
                fairValueGapDetected = true;
                Print(String.Format("Fair Value Gap detected - Bullish: {0}, Bearish: {1}", 
                    bullishFVG, bearishFVG));
                ExecuteTrade();
            }
            else
            {
                Print("No Fair Value Gap detected");
            }
        }

        private void ExecuteTrade()
        {
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                Print("ExecuteTrade skipped: Position is not flat");
                return;
            }

            double currentPrice = Closes[1][0];
            Print(String.Format("Executing trade: longTrade={0}, shortTrade={1}, currentPrice={2}", 
                longTrade, shortTrade, currentPrice));
            
            if (longTrade)
            {
                // Calculate stop loss at the first candle that closed through the range
                stopLoss = GetFirstCandleCloseThoughRange(true);
                double risk = currentPrice - stopLoss;
                profitTarget = currentPrice + (risk * RiskRewardRatio);
                
                Print(String.Format("Long Entry: Price={0}, Stop={1}, Target={2}, Risk={3}", 
                    currentPrice, stopLoss, profitTarget, risk));
                
                EnterLong(1, "FVG_Long");
                entryPrice = currentPrice;
            }
            else if (shortTrade)
            {
                // Calculate stop loss at the first candle that closed through the range
                stopLoss = GetFirstCandleCloseThoughRange(false);
                double risk = stopLoss - currentPrice;
                profitTarget = currentPrice - (risk * RiskRewardRatio);
                
                Print(String.Format("Short Entry: Price={0}, Stop={1}, Target={2}, Risk={3}", 
                    currentPrice, stopLoss, profitTarget, risk));
                
                EnterShort(1, "FVG_Short");
                entryPrice = currentPrice;
            }
            else
            {
                Print("ExecuteTrade: No trade direction set");
            }
        }

        private double GetFirstCandleCloseThoughRange(bool isLongTrade)
        {
            // Look back to find the first candle that closed through the session range
            for (int i = 0; i < Math.Min(CurrentBars[1], 20); i++)
            {
                double candleClose = Closes[1][i];
                
                if (isLongTrade && candleClose > sessionHigh)
                {
                    return Lows[1][i];
                }
                else if (!isLongTrade && candleClose < sessionLow)
                {
                    return Highs[1][i];
                }
            }
            
            // Fallback to session levels
            return isLongTrade ? sessionLow : sessionHigh;
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
        {
            if (order.Name == "FVG_Long" || order.Name == "FVG_Short")
            {
                if (orderState == OrderState.Filled)
                {
                    bool isLong = order.Name == "FVG_Long";
                    
                    // Set stop loss
                    if (isLong)
                    {
                        ExitLongStopMarket(0, true, order.Quantity, stopLoss, "Stop_Long", "FVG_Long");
                        ExitLongLimit(0, true, order.Quantity, profitTarget, "Target_Long", "FVG_Long");
                    }
                    else
                    {
                        ExitShortStopMarket(0, true, order.Quantity, stopLoss, "Stop_Short", "FVG_Short");
                        ExitShortLimit(0, true, order.Quantity, profitTarget, "Target_Short", "FVG_Short");
                    }
                    
                    Print(String.Format("Trade filled: {0} at {1}", order.Name, averageFillPrice));
                }
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.Name.Contains("Stop_") || execution.Order.Name.Contains("Target_"))
            {
                // Reset for next trade
                ResetTradeState();
                Print(String.Format("Trade closed: {0} at {1}", execution.Order.Name, price));
            }
        }

        private void ResetTradeState()
        {
            fairValueGapDetected = false;
            breakoutConfirmed = false;
            longTrade = false;
            shortTrade = false;
            entryPrice = 0;
            stopLoss = 0;
            profitTarget = 0;
            
            // Reset session range at the end of trading day
            if (Time[0].TimeOfDay > TimeSpan.FromHours(16)) // 4 PM
            {
                sessionRangeSet = false;
                sessionStartTime = sessionStartTime.AddDays(1);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Session Period Minutes", Description="Number of minutes for initial session range calculation", Order=1, GroupName="Parameters")]
        public int SessionPeriodMinutes
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Volume Confirmation", Description="Use volume confirmation for trade entries", Order=2, GroupName="Parameters")]
        public bool EnableVolumeConfirmation
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Candle Size Confirmation", Description="Use ATR-based candle size confirmation", Order=3, GroupName="Parameters")]
        public bool EnableCandleSizeConfirmation
        { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name="ATR Multiplier", Description="ATR multiplier for candle size confirmation", Order=4, GroupName="Parameters")]
        public double ATRMultiplier
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="ATR Period", Description="Period for ATR calculation", Order=5, GroupName="Parameters")]
        public int ATRPeriod
        { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, double.MaxValue)]
        [Display(Name="Volume Multiplier", Description="Volume multiplier for confirmation", Order=6, GroupName="Parameters")]
        public double VolumeMultiplier
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name="Start Hour", Description="Session start hour (24-hour format)", Order=7, GroupName="Parameters")]
        public int StartHour
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, 59)]
        [Display(Name="Start Minute", Description="Session start minute", Order=8, GroupName="Parameters")]
        public int StartMinute
        { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, double.MaxValue)]
        [Display(Name="Risk Reward Ratio", Description="Risk to reward ratio for profit targets", Order=9, GroupName="Parameters")]
        public double RiskRewardRatio
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Test Mode", Description="Enable test mode for debugging", Order=10, GroupName="Parameters")]
        public bool TestMode
        { get; set; }
        #endregion
    }
}