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

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ORBFVGScalpingStrategy : Strategy
	{
		#region Variables
		private double openingRangeHigh = 0;
		private double openingRangeLow = 0;
		private bool openingRangeSet = false;
		private DateTime openingRangeEndTime;
		private DateTime tradingSessionStartTime;
		private DateTime tradingSessionEndTime;
		private DateTime signalPeriodEndTime;
		private bool inTradingSession = false;
		private bool inORBPeriod = false;
		private bool inSignalPeriod = false;
		private int positionCounter = 0;
		
		// Drawing objects
		private object highLine;
		private object lowLine;
		private object openingRangeBox;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Opening Range Breakout with Fair Value Gap Integration Strategy - Trades FVG intersections with ORB boundaries";
				Name										= "ORB FVG Scalping Strategy";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 10; // Allow multiple positions
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				// Default parameter values
				OpeningRangeMinutes			= 5;
				SessionStartHour			= 9;
				SessionStartMinute			= 30;
				SessionEndHour				= 16;
				SessionEndMinute			= 0;
				SignalPeriodDuration		= 90; // 1.5 hours after ORB period
				RiskPerTrade				= 1.0;
				RiskRewardRatio				= 2.0;
				UseStopLoss					= true;
				UseTakeProfit				= true;
				ShowORBBox					= true;
				ShowFVGSignals				= true;
				ORBHighColor				= Brushes.Green;
				ORBLowColor					= Brushes.Red;
				BullishSignalColor			= Brushes.Green;
				BearishSignalColor			= Brushes.Red;
				BoxOpacity					= 50;
			}
			else if (State == State.Configure)
			{
				// Add a secondary data series if needed (optional)
			}
			else if (State == State.DataLoaded)
			{
				// Initialize any indicators here if needed
			}
		}

		protected override void OnBarUpdate()
		{
			// Ensure we have enough bars and are in a valid state
			if (BarsInProgress != 0 || CurrentBars[0] < BarsRequiredToTrade)
				return;

			// Get current time in EST (assuming your chart timezone is configured properly)
			DateTime currentTime = Time[0];
			
			// Set trading session times for today
			DateTime today = currentTime.Date;
			tradingSessionStartTime = today.AddHours(SessionStartHour).AddMinutes(SessionStartMinute);
			tradingSessionEndTime = today.AddHours(SessionEndHour).AddMinutes(SessionEndMinute);
			openingRangeEndTime = tradingSessionStartTime.AddMinutes(OpeningRangeMinutes);
			signalPeriodEndTime = tradingSessionStartTime.AddMinutes(SignalPeriodDuration);
			
			// Determine current session state
			inTradingSession = currentTime >= tradingSessionStartTime && currentTime <= tradingSessionEndTime;
			inORBPeriod = currentTime >= tradingSessionStartTime && currentTime < openingRangeEndTime;
			inSignalPeriod = currentTime >= openingRangeEndTime && currentTime < signalPeriodEndTime && openingRangeSet;
			
			// Reset variables at start of new trading day
			if (Bars.IsFirstBarOfSession || (currentTime >= tradingSessionStartTime && !openingRangeSet && currentTime.Date == tradingSessionStartTime.Date))
			{
				ResetDailyVariables();
			}
			
			// Exit if not in trading session
			if (!inTradingSession)
				return;
			
			// Capture opening range during ORB period
			if (inORBPeriod)
			{
				CaptureOpeningRange();
			}
			
			// Mark ORB as set after the period ends
			if (!inORBPeriod && openingRangeHigh > 0 && openingRangeLow > 0 && !openingRangeSet)
			{
				openingRangeSet = true;
				Print($"Opening Range Set - High: {openingRangeHigh}, Low: {openingRangeLow}");
			}
			
			// Look for FVG intersections during signal period
			if (inSignalPeriod && openingRangeSet)
			{
				CheckForFVGIntersections();
			}
			
			// Draw opening range visualization
			if (ShowORBBox && openingRangeSet)
			{
				DrawOpeningRange();
			}
		}

		private void CaptureOpeningRange()
		{
			if (openingRangeHigh == 0 || High[0] > openingRangeHigh)
				openingRangeHigh = High[0];
			
			if (openingRangeLow == 0 || Low[0] < openingRangeLow)
				openingRangeLow = Low[0];
		}

		private void CheckForFVGIntersections()
		{
			// Need at least 3 bars to check for FVG
			if (CurrentBar < 2)
				return;
			
			// Fair Value Gap detection
			bool bullishFVG = High[2] < Low[0]; // Current low is above the high from 2 bars ago
			bool bearishFVG = Low[2] > High[0]; // Current high is below the low from 2 bars ago
			
			// Check for FVG intersection with ORB boundaries
			bool bullishIntersection = false;
			bool bearishIntersection = false;
			
			if (bullishFVG)
			{
				// Bullish FVG intersecting upper ORB boundary
				// Previous candle opened below ORB high and closed above it
				if (Open[1] <= openingRangeHigh && Close[1] >= openingRangeHigh)
				{
					bullishIntersection = true;
				}
			}
			
			if (bearishFVG)
			{
				// Bearish FVG intersecting lower ORB boundary  
				// Previous candle opened above ORB low and closed below it
				if (Open[1] >= openingRangeLow && Close[1] <= openingRangeLow)
				{
					bearishIntersection = true;
				}
			}
			
			// Execute trades based on intersections
			if (bullishIntersection)
			{
				ExecuteBullishTrade();
			}
			
			if (bearishIntersection)
			{
				ExecuteBearishTrade();
			}
		}

		private void ExecuteBullishTrade()
		{
			positionCounter++;
			
			double entryPrice = Close[0];
			double stopLossPrice = Low[1]; // Previous candle's low
			double risk = entryPrice - stopLossPrice;
			double takeProfitPrice = entryPrice + (risk * RiskRewardRatio);
			
			// Calculate position size based on risk per trade
			double riskAmount = Account.Get(AccountItem.NetLiquidation, Currency.UsDollar) * (RiskPerTrade / 100.0);
			int quantity = (int)(riskAmount / (risk * Instrument.MasterInstrument.PointValue));
			
			if (quantity <= 0)
				quantity = 1;
			
			string entryId = "Long_" + positionCounter.ToString();
			string exitId = "LongExit_" + positionCounter.ToString();
			
			// Enter long position
			EnterLong(quantity, entryId);
			
			// Set stop loss and take profit
			if (UseStopLoss && UseTakeProfit)
			{
				ExitLongLimit(quantity, takeProfitPrice, exitId + "_TP", entryId);
				ExitLongStopMarket(quantity, stopLossPrice, exitId + "_SL", entryId);
			}
			else if (UseStopLoss)
			{
				ExitLongStopMarket(quantity, stopLossPrice, exitId + "_SL", entryId);
			}
			else if (UseTakeProfit)
			{
				ExitLongLimit(quantity, takeProfitPrice, exitId + "_TP", entryId);
			}
			
			// Draw signal marker
			if (ShowFVGSignals)
			{
				Draw.ArrowUp(this, "BullSignal_" + positionCounter.ToString(), false, 0, Low[0] - (2 * TickSize), BullishSignalColor);
			}
			
			Print($"Bullish FVG Intersection - Entry: {entryPrice}, Stop: {stopLossPrice}, Target: {takeProfitPrice}, Qty: {quantity}");
		}

		private void ExecuteBearishTrade()
		{
			positionCounter++;
			
			double entryPrice = Close[0];
			double stopLossPrice = High[1]; // Previous candle's high
			double risk = stopLossPrice - entryPrice;
			double takeProfitPrice = entryPrice - (risk * RiskRewardRatio);
			
			// Calculate position size based on risk per trade
			double riskAmount = Account.Get(AccountItem.NetLiquidation, Currency.UsDollar) * (RiskPerTrade / 100.0);
			int quantity = (int)(riskAmount / (risk * Instrument.MasterInstrument.PointValue));
			
			if (quantity <= 0)
				quantity = 1;
			
			string entryId = "Short_" + positionCounter.ToString();
			string exitId = "ShortExit_" + positionCounter.ToString();
			
			// Enter short position
			EnterShort(quantity, entryId);
			
			// Set stop loss and take profit
			if (UseStopLoss && UseTakeProfit)
			{
				ExitShortLimit(quantity, takeProfitPrice, exitId + "_TP", entryId);
				ExitShortStopMarket(quantity, stopLossPrice, exitId + "_SL", entryId);
			}
			else if (UseStopLoss)
			{
				ExitShortStopMarket(quantity, stopLossPrice, exitId + "_SL", entryId);
			}
			else if (UseTakeProfit)
			{
				ExitShortLimit(quantity, takeProfitPrice, exitId + "_TP", entryId);
			}
			
			// Draw signal marker
			if (ShowFVGSignals)
			{
				Draw.ArrowDown(this, "BearSignal_" + positionCounter.ToString(), false, 0, High[0] + (2 * TickSize), BearishSignalColor);
			}
			
			Print($"Bearish FVG Intersection - Entry: {entryPrice}, Stop: {stopLossPrice}, Target: {takeProfitPrice}, Qty: {quantity}");
		}

		private void DrawOpeningRange()
		{
			if (openingRangeHigh > 0 && openingRangeLow > 0)
			{
				// Remove existing drawing objects
				RemoveDrawObject("ORB_Box");
				RemoveDrawObject("ORB_HighLine");
				RemoveDrawObject("ORB_LowLine");
				
				// Draw opening range box
				DateTime startTime = tradingSessionStartTime;
				DateTime endTime = tradingSessionEndTime;
				
				openingRangeBox = Draw.Rectangle(this, "ORB_Box", false, startTime, openingRangeLow, 
					endTime, openingRangeHigh, Brushes.Transparent, ORBHighColor, BoxOpacity);
				
				// Draw horizontal lines for high and low
				highLine = Draw.Line(this, "ORB_HighLine", false, startTime, openingRangeHigh, 
					endTime, openingRangeHigh, ORBHighColor, DashStyleHelper.Solid, 2);
				
				lowLine = Draw.Line(this, "ORB_LowLine", false, startTime, openingRangeLow, 
					endTime, openingRangeLow, ORBLowColor, DashStyleHelper.Solid, 2);
			}
		}

		private void ResetDailyVariables()
		{
			openingRangeHigh = 0;
			openingRangeLow = 0;
			openingRangeSet = false;
			positionCounter = 0;
			
			// Remove previous day's drawing objects
			RemoveDrawObject("ORB_Box");
			RemoveDrawObject("ORB_HighLine");
			RemoveDrawObject("ORB_LowLine");
		}

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, 
			int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			// Handle order updates if needed
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, 
			MarketPosition marketPosition, string orderId, DateTime time)
		{
			// Handle execution updates if needed
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, 60)]
		[Display(Name = "Opening Range Minutes", Description = "Number of minutes to calculate the opening range", Order = 1, GroupName = "Strategy Parameters")]
		public int OpeningRangeMinutes { get; set; }

		[NinjaScriptProperty]
		[Range(0, 23)]
		[Display(Name = "Session Start Hour", Description = "Hour when trading session starts (24-hour format)", Order = 2, GroupName = "Strategy Parameters")]
		public int SessionStartHour { get; set; }

		[NinjaScriptProperty]
		[Range(0, 59)]
		[Display(Name = "Session Start Minute", Description = "Minute when trading session starts", Order = 3, GroupName = "Strategy Parameters")]
		public int SessionStartMinute { get; set; }

		[NinjaScriptProperty]
		[Range(0, 23)]
		[Display(Name = "Session End Hour", Description = "Hour when trading session ends (24-hour format)", Order = 4, GroupName = "Strategy Parameters")]
		public int SessionEndHour { get; set; }

		[NinjaScriptProperty]
		[Range(0, 59)]
		[Display(Name = "Session End Minute", Description = "Minute when trading session ends", Order = 5, GroupName = "Strategy Parameters")]
		public int SessionEndMinute { get; set; }

		[NinjaScriptProperty]
		[Range(30, 300)]
		[Display(Name = "Signal Period Duration", Description = "How long after ORB period to look for signals (minutes)", Order = 6, GroupName = "Strategy Parameters")]
		public int SignalPeriodDuration { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Risk Per Trade (%)", Description = "Percentage of account to risk per trade", Order = 7, GroupName = "Risk Management")]
		public double RiskPerTrade { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 10.0)]
		[Display(Name = "Risk:Reward Ratio", Description = "Risk to reward ratio for profit targets", Order = 8, GroupName = "Risk Management")]
		public double RiskRewardRatio { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Stop Loss", Description = "Enable stop loss orders", Order = 9, GroupName = "Risk Management")]
		public bool UseStopLoss { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Take Profit", Description = "Enable take profit orders", Order = 10, GroupName = "Risk Management")]
		public bool UseTakeProfit { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show ORB Box", Description = "Display opening range box on chart", Order = 11, GroupName = "Visual Settings")]
		public bool ShowORBBox { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show FVG Signals", Description = "Display FVG intersection signals on chart", Order = 12, GroupName = "Visual Settings")]
		public bool ShowFVGSignals { get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "ORB High Color", Description = "Color for ORB high line", Order = 13, GroupName = "Visual Settings")]
		public Brush ORBHighColor { get; set; }

		[Browsable(false)]
		public string ORBHighColorSerializable
		{
			get { return Serialize.BrushToString(ORBHighColor); }
			set { ORBHighColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "ORB Low Color", Description = "Color for ORB low line", Order = 14, GroupName = "Visual Settings")]
		public Brush ORBLowColor { get; set; }

		[Browsable(false)]
		public string ORBLowColorSerializable
		{
			get { return Serialize.BrushToString(ORBLowColor); }
			set { ORBLowColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Bullish Signal Color", Description = "Color for bullish FVG signals", Order = 15, GroupName = "Visual Settings")]
		public Brush BullishSignalColor { get; set; }

		[Browsable(false)]
		public string BullishSignalColorSerializable
		{
			get { return Serialize.BrushToString(BullishSignalColor); }
			set { BullishSignalColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Bearish Signal Color", Description = "Color for bearish FVG signals", Order = 16, GroupName = "Visual Settings")]
		public Brush BearishSignalColor { get; set; }

		[Browsable(false)]
		public string BearishSignalColorSerializable
		{
			get { return Serialize.BrushToString(BearishSignalColor); }
			set { BearishSignalColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Box Opacity", Description = "Opacity of the opening range box (1-100)", Order = 17, GroupName = "Visual Settings")]
		public int BoxOpacity { get; set; }
		#endregion
	}
}