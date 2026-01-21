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
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class FVGOrbBreakoutStrategy : Strategy
    {
        private double orbHigh;
        private double orbLow;
        private bool orbCalculated;
        private DateTime orbStartTime;
        private DateTime orbEndTime;
        private bool inTradingSession;
        private DateTime currentTradingDate;
        private Rectangle orbBox;
        private NinjaTrader.NinjaScript.DrawingTools.Line orbHighLine;
        private NinjaTrader.NinjaScript.DrawingTools.Line orbLowLine;
        
        private double entryPrice;
        private double stopLoss;
        private double target;
        private bool longEntry;
        private bool shortEntry;
        private bool fvgDetected;
        private int fvgDetectedBar;
        private int tradeCounter;
        private bool tradeTakenToday;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"FVG ORB Breakout Strategy - Scalping strategy based on Opening Range Breakout with Fair Value Gap confirmation";
                Name = "FVGOrbBreakoutStrategy";
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
                BarsRequiredToTrade = 3;
                IsInstantiatedOnEachOptimizationIteration = true;
                
                // Default parameters
                OrbDurationMinutes = 5;
                SessionStartHour = 9;
                SessionStartMinute = 30;
                SessionEndHour = 11;
                SessionEndMinute = 30;
                RiskRewardRatio = 2.0;
                ShowOrbBox = true;
                ShowOrbLines = true;
                OrbBoxColor = Brushes.Blue;
                OrbLineColor = Brushes.Red;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Minute, 1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 1) return; // Work on 1-minute data series
            
            DateTime currentTime = Time[0];
            
            // Check if we're in trading session
            CheckTradingSession(currentTime);
            
            if (!inTradingSession) return;
            
            // Calculate ORB if not done yet
            if (!orbCalculated)
            {
                CalculateORB(currentTime);
            }
            
            if (!orbCalculated) return;
            
            // Draw ORB visual elements
            DrawOrbElements();
            
            // Check for FVG and entry conditions
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                CheckForEntry();
            }
        }
        
        private void CheckTradingSession(DateTime currentTime)
        {
            DateTime sessionStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 
                                               SessionStartHour, SessionStartMinute, 0);
            DateTime sessionEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 
                                             SessionEndHour, SessionEndMinute, 0);
            
            // Check if we have a new trading day
            if (currentTradingDate.Date != currentTime.Date)
            {
                // Reset for new day
                ResetForNewTradingDay();
                currentTradingDate = currentTime.Date;
            }
            
            inTradingSession = currentTime >= sessionStart && currentTime <= sessionEnd;
        }
        
        private void ResetForNewTradingDay()
        {
            orbCalculated = false;
            orbHigh = 0;
            orbLow = 0;
            longEntry = false;
            shortEntry = false;
            tradeTakenToday = false;
            orbBox = null;
            orbHighLine = null;
            orbLowLine = null;
        }
        
        private void CalculateORB(DateTime currentTime)
        {
            DateTime sessionStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 
                                               SessionStartHour, SessionStartMinute, 0);
            orbStartTime = sessionStart;
            orbEndTime = sessionStart.AddMinutes(OrbDurationMinutes);
            
            if (currentTime >= orbEndTime)
            {
                // Find bars within ORB period
                double high = double.MinValue;
                double low = double.MaxValue;
                
                for (int i = 0; i < CurrentBars[1]; i++)
                {
                    if (Times[1][i] >= orbStartTime && Times[1][i] < orbEndTime)
                    {
                        high = Math.Max(high, Highs[1][i]);
                        low = Math.Min(low, Lows[1][i]);
                    }
                }
                
                if (high != double.MinValue && low != double.MaxValue)
                {
                    orbHigh = high;
                    orbLow = low;
                    orbCalculated = true;
                }
            }
        }
        
        private void DrawOrbElements()
        {
            if (!orbCalculated) return;
            
            string orbTag = "ORB_" + orbStartTime.ToString("yyyyMMdd_HHmmss");
            
            if (ShowOrbBox && orbBox == null)
            {
                orbBox = Draw.Rectangle(this, orbTag + "_Box", false, 
                    orbStartTime, orbLow, orbEndTime, orbHigh, 
                    OrbBoxColor, OrbBoxColor, 20);
            }
            
            if (ShowOrbLines)
            {
                // Calculate session end time for this day
                DateTime sessionEnd = new DateTime(orbStartTime.Year, orbStartTime.Month, orbStartTime.Day, 
                                                 SessionEndHour, SessionEndMinute, 0);
                
                if (orbHighLine == null)
                {
                    orbHighLine = Draw.Line(this, orbTag + "_High", false,
                        orbEndTime, orbHigh, 
                        sessionEnd, orbHigh,
                        OrbLineColor, DashStyleHelper.Solid, 2);
                }
                
                if (orbLowLine == null)
                {
                    orbLowLine = Draw.Line(this, orbTag + "_Low", false,
                        orbEndTime, orbLow,
                        sessionEnd, orbLow,
                        OrbLineColor, DashStyleHelper.Solid, 2);
                }
            }
        }
        
        private void CheckForEntry()
        {
            if (CurrentBars[1] < 3) return;
            
            // Skip if we already took a trade today
            if (tradeTakenToday) return;
            
            // Skip if we already have a position
            if (Position.MarketPosition != MarketPosition.Flat) return;
            
            // Check for Fair Value Gap (3-candle pattern) - all bars are now closed
            bool hasFVG = CheckFairValueGap();
            
            if (!hasFVG) return;
            
            // Now check if any of the FVG pattern bars broke through ORB
            // Check all three bars of the FVG pattern for breakout
            bool breakoutHigh = Closes[1][0] > orbHigh || Closes[1][1] > orbHigh || Closes[1][2] > orbHigh;
            bool breakoutLow = Closes[1][0] < orbLow || Closes[1][1] < orbLow || Closes[1][2] < orbLow;
            
            if (!breakoutHigh && !breakoutLow) return;
            
            // Enter trade - FVG is confirmed and breakout occurred
            if (breakoutHigh)
            {
                EnterLongPosition();
            }
            else if (breakoutLow)
            {
                EnterShortPosition();
            }
        }
        
        private bool CheckFairValueGap()
        {
            if (CurrentBars[1] < 3) return false;
            
            // Check for gap between candle 2 bars ago and current candle
            // Middle candle (1 bar ago) should create the gap
            double candle1High = Highs[1][2];  // 2 bars ago
            double candle1Low = Lows[1][2];
            
            double candle2High = Highs[1][1];  // 1 bar ago (middle expansive candle)
            double candle2Low = Lows[1][1];
            
            double candle3High = Highs[1][0];  // Current candle
            double candle3Low = Lows[1][0];
            
            // Check for bullish FVG (gap between candle1 high and candle3 low)
            bool bullishFVG = candle1High < candle3Low;
            
            // Check for bearish FVG (gap between candle1 low and candle3 high)
            bool bearishFVG = candle1Low > candle3High;
            
            return bullishFVG || bearishFVG;
        }
        
        private void EnterLongPosition()
        {
            // Check if we already have a position or pending orders
            if (Position.MarketPosition != MarketPosition.Flat) return;
            
            tradeCounter++;
            string entryName = "FVGLong" + tradeCounter;
            
            entryPrice = Closes[1][0]; // Use close of the FVG candle (current bar that just closed)
            
            // Stop loss is the low of the middle bar of the FVG pattern 
            stopLoss = Lows[1][1]; // Low of the middle FVG candle
            
            double riskAmount = entryPrice - stopLoss;
            double targetDistance = riskAmount * RiskRewardRatio;
            target = entryPrice + (Math.Round(targetDistance / TickSize) * TickSize);
            
            // Debug output
            Print(string.Format("LONG ENTRY DEBUG - Time: {0}", Times[1][0].ToString("HH:mm:ss")));
            Print(string.Format("  Entry Price (FVG Close): {0:F2}", entryPrice));
            Print(string.Format("  Stop Loss (Middle FVG Bar Low): {0:F2}", stopLoss));
            Print(string.Format("  Risk Amount: {0:F2}", riskAmount));
            Print(string.Format("  Target Price: {0:F2}", target));
            Print(string.Format("  Risk/Reward Ratio: {0:F1}", RiskRewardRatio));
            
            EnterLong(1, entryName);
            SetStopLoss(entryName, CalculationMode.Price, stopLoss, false);
            SetProfitTarget(entryName, CalculationMode.Price, target);
            
            longEntry = true;
            tradeTakenToday = true; // Mark trade taken after order submission
        }
        
        private void EnterShortPosition()
        {
            // Check if we already have a position or pending orders
            if (Position.MarketPosition != MarketPosition.Flat) return;
            
            tradeCounter++;
            string entryName = "FVGShort" + tradeCounter;
            
            entryPrice = Closes[1][0]; // Use close of the FVG candle (current bar that just closed)
            
            // Stop loss is the high of the middle bar of the FVG pattern
            stopLoss = Highs[1][1]; // High of the middle FVG candle
            
            double riskAmount = stopLoss - entryPrice;
            double targetDistance = riskAmount * RiskRewardRatio;
            target = entryPrice - (Math.Round(targetDistance / TickSize) * TickSize);
            
            // Debug output
            Print(string.Format("SHORT ENTRY DEBUG - Time: {0}", Times[1][0].ToString("HH:mm:ss")));
            Print(string.Format("  Entry Price (FVG Close): {0:F2}", entryPrice));
            Print(string.Format("  Stop Loss (Middle FVG Bar High): {0:F2}", stopLoss));
            Print(string.Format("  Risk Amount: {0:F2}", riskAmount));
            Print(string.Format("  Target Price: {0:F2}", target));
            Print(string.Format("  Risk/Reward Ratio: {0:F1}", RiskRewardRatio));
            
            EnterShort(1, entryName);
            SetStopLoss(entryName, CalculationMode.Price, stopLoss, false);
            SetProfitTarget(entryName, CalculationMode.Price, target);
            
            shortEntry = true;
            tradeTakenToday = true; // Mark trade taken after order submission
        }
        
        private double FindStopLossLevel(bool isLong)
        {
            // Find the first candle that closed through the ORB range
            // Start from further back and work forward to find the FIRST breakout candle
            double stopLevel = isLong ? orbLow : orbHigh; // Default fallback
            
            for (int i = Math.Min(CurrentBars[1] - 1, 20); i >= 0; i--)
            {
                if (isLong)
                {
                    if (Closes[1][i] > orbHigh)
                    {
                        stopLevel = Lows[1][i]; // Keep updating to find the FIRST one
                    }
                }
                else
                {
                    if (Closes[1][i] < orbLow)
                    {
                        stopLevel = Highs[1][i]; // Keep updating to find the FIRST one
                    }
                }
            }
            
            return stopLevel;
        }
        
        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
        {
            // Reset entry flags when orders are filled or cancelled
            if (orderState == OrderState.Filled || orderState == OrderState.Cancelled || orderState == OrderState.Rejected)
            {
                if (order.Name.Contains("FVGLong"))
                {
                    if (orderState == OrderState.Filled && order.OrderAction == OrderAction.Buy)
                    {
                        // Long entry filled - do nothing, stop/target will handle exit
                    }
                    else if (orderState != OrderState.Filled)
                    {
                        longEntry = false; // Reset if order was cancelled/rejected
                    }
                }
                else if (order.Name.Contains("FVGShort"))
                {
                    if (orderState == OrderState.Filled && order.OrderAction == OrderAction.SellShort)
                    {
                        // Short entry filled - do nothing, stop/target will handle exit
                    }
                    else if (orderState != OrderState.Filled)
                    {
                        shortEntry = false; // Reset if order was cancelled/rejected
                    }
                }
            }
        }
        
        protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
        {
            // Reset entry flags when position goes flat
            if (marketPosition == MarketPosition.Flat)
            {
                longEntry = false;
                shortEntry = false;
            }
        }
        
        #region Properties
        
        [NinjaScriptProperty]
        [Range(1, 60)]
        [Display(Name = "ORB Duration (Minutes)", Description = "Duration in minutes for Opening Range calculation", Order = 1, GroupName = "ORB Settings")]
        public int OrbDurationMinutes { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name = "Session Start Hour", Description = "Trading session start hour (24-hour format)", Order = 2, GroupName = "Session Settings")]
        public int SessionStartHour { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 59)]
        [Display(Name = "Session Start Minute", Description = "Trading session start minute", Order = 3, GroupName = "Session Settings")]
        public int SessionStartMinute { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name = "Session End Hour", Description = "Trading session end hour (24-hour format)", Order = 4, GroupName = "Session Settings")]
        public int SessionEndHour { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 59)]
        [Display(Name = "Session End Minute", Description = "Trading session end minute", Order = 5, GroupName = "Session Settings")]
        public int SessionEndMinute { get; set; }
        
        [NinjaScriptProperty]
        [Range(1.0, 10.0)]
        [Display(Name = "Risk/Reward Ratio", Description = "Target profit as multiple of risk", Order = 6, GroupName = "Risk Management")]
        public double RiskRewardRatio { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Show ORB Box", Description = "Display ORB box on chart", Order = 7, GroupName = "Visual Settings")]
        public bool ShowOrbBox { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Show ORB Lines", Description = "Display ORB high/low lines on chart", Order = 8, GroupName = "Visual Settings")]
        public bool ShowOrbLines { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "ORB Box Color", Description = "Color for ORB box", Order = 9, GroupName = "Visual Settings")]
        public Brush OrbBoxColor { get; set; }
        
        [Browsable(false)]
        public string OrbBoxColorSerializable
        {
            get { return Serialize.BrushToString(OrbBoxColor); }
            set { OrbBoxColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "ORB Line Color", Description = "Color for ORB lines", Order = 10, GroupName = "Visual Settings")]
        public Brush OrbLineColor { get; set; }
        
        [Browsable(false)]
        public string OrbLineColorSerializable
        {
            get { return Serialize.BrushToString(OrbLineColor); }
            set { OrbLineColor = Serialize.StringToBrush(value); }
        }
        
        #endregion
    }
}