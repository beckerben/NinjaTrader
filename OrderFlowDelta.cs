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
	[Gui.CategoryOrder("NetDelta", 1000100)]
	[Gui.CategoryOrder("Entry&Exit", 1000200)]
	[Gui.CategoryOrder("Profit&Loss", 1000300)]
	[Gui.CategoryOrder("Trading", 1000400)]
	[Gui.CategoryOrder("Display", 1000500)]
	[Gui.CategoryOrder("ATM", 1000600)]
	
	public class OrderFlowDelta : Strategy
	{
		#region Classes
		
		private class PositionInfo : ICloneable
		{
			public MarketPosition PositionType { get; set; }
			public double Price { get; set; }
			public int Quantity { get; set; }
			public double ExitPrice { get; set; }
			public int ExitQuantity { get; set; }
			
			public PositionInfo()
			{
				Reset();
			}
			
			public void Reset()
			{
				PositionType = MarketPosition.Flat;
				Price = 0d;
				Quantity = 0;
				ExitPrice = 0d;
				ExitQuantity = 0;
			}
			
			public object Clone()
			{
				return new PositionInfo
				{
					PositionType = PositionType,
					Price = Price,
					Quantity = Quantity,
					ExitPrice = ExitPrice,
					ExitQuantity = ExitQuantity
				};
			}
			
			public override string ToString()
			{
				return string.Format("dir: {0}, p: {1}, q: {2}, ep: {3}, eq: {4}", PositionType, Price, Quantity, ExitPrice, ExitQuantity);
			}
		}
		
		#endregion
		
		#region Variables
		
		private OrderFlowCumulativeDelta _OFCD;
		private PositionInfo _positionInfo;
		private PositionInfo _lastPositionInfo;
		private double _totalLossAmount;
		private double _totalProfitAmount;
		private string _atmStrategyId;
		private string _atmStrategyOrderId;
		private bool _isAtmStrategyCreated;
		private SimpleFont _markerFont;
		private bool _isLongDisabled;
		private bool _isShortDisabled;
		
		#endregion
		
		#region Initialization
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"OrderFlowDelta";
				Name										= "OrderFlowDelta";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
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
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				NetDelta = 50;

				Enable_Long = true;
				Bars_Above_Trend_For_Long = 3;
				Enable_Short = true;
				Bars_Below_Trend_For_Short = 3;
				Exit_By_First_Opposite_Bar = true;
				Enable_Profit_Target = false;
				Profit_Target_Ticks = 10;
				Enable_Stop_Loss = false;
				Stop_Loss_Ticks = 15;
				Enable_Trading_Time = false;
				Disable_Trading_By_Loss_Amount = false;
				No_Trading_Loss_Amount = 0d;
				Atm_Strategy_Template = string.Empty;
			}
			else if (State == State.Configure)
			{
				_OFCD = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);
				AddChartIndicator(_OFCD);
				
				_positionInfo = new PositionInfo();
				_lastPositionInfo = new PositionInfo();
				_totalLossAmount = 0d;
				_isAtmStrategyCreated = false;
				
				if (string.IsNullOrEmpty(Atm_Strategy_Template))
				{
					if (Enable_Profit_Target)
					{
						SetProfitTarget(CalculationMode.Ticks, Profit_Target_Ticks);
					}
					if (Enable_Stop_Loss)
					{
						SetStopLoss(CalculationMode.Ticks, Stop_Loss_Ticks);
					}
				}
				
				_markerFont = new SimpleFont("Arial", 10);
			}
			else if (State == State.Realtime)
			{
				
			}
		}
		
		#endregion
		
		#region Private methods
		
		private ISeries<double> GetSMASeries(PriceType priceType)
		{
			ISeries<double> result = new Series<double>(this, MaximumBarsLookBack.Infinite);
			switch (priceType)
			{
				case PriceType.Open:
					result = Open;
					break;
				case PriceType.Close:
					result = Close;
					break;
				case PriceType.Median:
					result = Median;
					break;
				case PriceType.Typical:
					result = Typical;
					break;
				case PriceType.Weighted:
					result = Weighted;
					break;
				case PriceType.High:
					result = High;
					break;
				case PriceType.Low:
					result = Low;
					break;
			}
			return result;
		}
		
		private bool IsTradingTime()
		{
			var result = true;
			var barTimestamp  = Time[0].Hour * 60 + Time[0].Minute;
			var startTimestamp = Start_Trading_Time.Hour * 60 + Start_Trading_Time.Minute;
			var endTimestamp = End_Trading_Time.Hour * 60 + End_Trading_Time.Minute;
			var isNoTradingTime = (barTimestamp < startTimestamp || barTimestamp >= endTimestamp) && Enable_Trading_Time;
			if (isNoTradingTime)
			{
				if (_positionInfo.PositionType != MarketPosition.Flat)
				{
					if (string.IsNullOrEmpty(Atm_Strategy_Template))
					{
						if (_positionInfo.PositionType == MarketPosition.Long)
						{
							ExitLong();
						}
						else if (_positionInfo.PositionType == MarketPosition.Short)
						{
							ExitShort();
						}
					}
					else
					{
						if (State == State.Realtime)
						{
							AtmStrategyClose(_atmStrategyId);
						}
					}
					_positionInfo.Reset();
				}
				if (_positionInfo.PositionType == MarketPosition.Flat)
				{
					result = false;
				}
			}
			
			return result;
		}
		
		private bool IsLongExit()
		{
			if (_positionInfo.PositionType == MarketPosition.Flat || CurrentBar < 1)
				return false;
			
			return _positionInfo.PositionType == MarketPosition.Long && Close[0] < Close[1] && Exit_By_First_Opposite_Bar;
		}
		
		private bool IsShortExit()
		{
			if (_positionInfo.PositionType == MarketPosition.Flat || CurrentBar < 1)
				return false;
			
			return _positionInfo.PositionType == MarketPosition.Short && Close[0] > Close[1] && Exit_By_First_Opposite_Bar;
		}
		
		private bool IsLongEntry()
		{
			if (_positionInfo.PositionType != MarketPosition.Flat || !Enable_Long)
				return false;
			
			if (CurrentBar < Bars_Above_Trend_For_Long)
				return false;
			
			var result = false;
			
			if ((_OFCD.DeltaClose[0] > 0) && (Math.Abs(_OFCD.DeltaClose[0]) >= NetDelta) && (Close[0] < Open[0]))
			{
				result = true;	
			}

			return result;
		}
		
		private bool IsShortEntry()
		{
			if (_positionInfo.PositionType != MarketPosition.Flat || !Enable_Short)
				return false;
			
			if (CurrentBar < Bars_Below_Trend_For_Short)
				return false;
			
			var result = false;
			if ((_OFCD.DeltaClose[0] < 0) && (Math.Abs(_OFCD.DeltaClose[0]) >= NetDelta) && (Close[0] > Open[0]))
			{
				result = true;	
			}
			
			return result;
		}
		
		private void PrintPositionChanged()
		{
			bool isFlat = _positionInfo.PositionType == MarketPosition.Flat;
			Print(string.Format("{0}. Position changed: dir: {1}, q: {2}, p: {3}", Time[0], _positionInfo.PositionType, isFlat ? 0 : _positionInfo.Quantity, isFlat ? 0 : _positionInfo.Price));
		}
		
		private void DrawText(string tag, string text, double y, int offset = 0)
		{
			Draw.Text(this, tag, true, text, 0, y, offset, ChartControl.Properties.ChartText, _markerFont, System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
		
		private void CheckEntryDisabled(bool isExit)
		{
			if (isExit || _isLongDisabled)
			{
				_isLongDisabled = Close[0] > Close[1];
			}
			if (isExit || _isShortDisabled)
			{
				_isShortDisabled = Close[0] < Close[1];
			}
		}
		
		#region ATM Strategy Processing
		
		private void CreateATM(OrderAction orderAction)
		{
			if (!string.IsNullOrEmpty(Atm_Strategy_Template))
			{
				_atmStrategyId = GetAtmStrategyUniqueId();
				_atmStrategyOrderId = GetAtmStrategyUniqueId();
				_isAtmStrategyCreated = false;
				AtmStrategyCreate(orderAction, OrderType.Market, 0, 0, TimeInForce.Day, _atmStrategyOrderId, Atm_Strategy_Template, _atmStrategyId,
					(atmCallbackErrorCode, atmCallbackId) => {
					if (atmCallbackId == _atmStrategyId)
					{
						if (atmCallbackErrorCode == Cbi.ErrorCode.NoError)
						{
							_isAtmStrategyCreated = true;
						}
					}
				});
			}
		}
		
		private void ProcessATM()
		{
			if (_totalLossAmount >= No_Trading_Loss_Amount && Disable_Trading_By_Loss_Amount)
				return;
			var exitDone = false;
			if (IsLongExit() && !exitDone)
			{
				AtmStrategyClose(_atmStrategyId);
				exitDone = true;
			}
			if (IsShortExit() && !exitDone)
			{
				AtmStrategyClose(_atmStrategyId);
				exitDone = true;
			}
			UpdatePositionInfo();
			CheckEntryDisabled(exitDone);
			if (IsLongEntry() && !_isLongDisabled)
			{
				CreateATM(OrderAction.Buy);
			}
			if (IsShortEntry() && !_isShortDisabled)
			{
				CreateATM(OrderAction.SellShort);
			}
			UpdatePositionInfo();
			if (_positionInfo.PositionType != MarketPosition.Flat)
			{
				var k = _positionInfo.PositionType == MarketPosition.Long ? 1 : -1;
				if (!Enable_Stop_Loss)
				{
					var stopPrice = (k > 0 ? Low[0] : High[0]) - k * TickSize * Stop_Loss_Ticks;
					AtmStrategyChangeStopTarget(0, stopPrice, "STOP1", _atmStrategyId);
				}
				if (!Enable_Profit_Target)
				{
					var targetPrice = (k < 0 ? Low[0] : High[0]) + k * TickSize * Profit_Target_Ticks;
					AtmStrategyChangeStopTarget(targetPrice, 0, "TARGET1", _atmStrategyId);
				}
			}
		}
		
		private void UpdatePositionInfo()
		{
			if (string.IsNullOrEmpty(_atmStrategyId) || !_isAtmStrategyCreated)
				return;
			
			var positionInfo = new PositionInfo()
			{
				PositionType = GetAtmStrategyMarketPosition(_atmStrategyId),
				Quantity = GetAtmStrategyPositionQuantity(_atmStrategyId),
				Price = GetAtmStrategyPositionAveragePrice(_atmStrategyId)
			};
			if (_positionInfo.PositionType != positionInfo.PositionType ||
				_positionInfo.Quantity != positionInfo.Quantity ||
				_positionInfo.Price != positionInfo.Price)
			{
				if (_positionInfo.PositionType != positionInfo.PositionType)
				{
					DrawMarkers(positionInfo);
					if (positionInfo.PositionType != MarketPosition.Flat)
					{
						var k = positionInfo.PositionType == MarketPosition.Long ? 1 : -1;
						if (Enable_Stop_Loss)
						{
							var stopPrice = positionInfo.Price - k * TickSize * Stop_Loss_Ticks;
							AtmStrategyChangeStopTarget(0, stopPrice, "STOP1", _atmStrategyId);
						}
						if (Enable_Profit_Target)
						{
							var targetPrice = Enable_Profit_Target ? positionInfo.Price + k * TickSize * Profit_Target_Ticks : double.MaxValue;
							AtmStrategyChangeStopTarget(targetPrice, 0, "TARGET1", _atmStrategyId);
						}
					}
				}
				_positionInfo = positionInfo;
				PrintPositionChanged();
			}
		}
		
		private void DrawMarkers(PositionInfo newPositionInfo)
		{
			if (newPositionInfo.PositionType == MarketPosition.Long)
			{
				Draw.ArrowUp(this, "EntryLong" + CurrentBar.ToString(), false, 0, Low[0] - TickSize, Brushes.Blue, true);
				var markerText = string.Format("Buy\nQ: {0}\nP: {1}", newPositionInfo.Quantity, newPositionInfo.Price);
				DrawText("EntryLongLabel" + CurrentBar.ToString(), markerText, Low[0] - 2);
			}
			else if (newPositionInfo.PositionType == MarketPosition.Short)
			{
				Draw.ArrowDown(this, "EntryShort" + CurrentBar.ToString(), false, 0, High[0] + TickSize, Brushes.Red, true);
				var markerText = string.Format("Sell\nQ: {0}\nP: {1}", newPositionInfo.Quantity, newPositionInfo.Price);
				DrawText("EntryShortLabel" + CurrentBar.ToString(), markerText, High[0] + 2);
			}
			else if (newPositionInfo.PositionType == MarketPosition.Flat)
			{
				var pnl = GetAtmStrategyRealizedProfitLoss(_atmStrategyId);
				if (_positionInfo.PositionType == MarketPosition.Long)
				{
					Draw.ArrowDown(this, "ExitLong" + CurrentBar.ToString(), false, 0, High[0] + TickSize, Brushes.Red, true);
					var markerText = string.Format("Exit\nP&L {0}", pnl);
					DrawText("ExitLongLabel" + CurrentBar.ToString(), markerText, High[0] + 2);
				}
				else
				{
					Draw.ArrowUp(this, "ExitShort" + CurrentBar.ToString(), false, 0, Low[0] - TickSize, Brushes.Blue, true);
					var markerText = string.Format("Exit\nP&L {0}", pnl);
					DrawText("ExitShortLabel" + CurrentBar.ToString(), markerText, Low[0] - 2);
				}
				if (pnl < 0)
				{
					_totalLossAmount += Math.Abs(pnl);
				}
			}
		}
		
		#endregion
		
		#region NT8 Strategy Processing
		
		private void ProcessNT8()
		{
			if (_positionInfo.PositionType == MarketPosition.Flat && _lastPositionInfo.PositionType != MarketPosition.Flat)
			{
				DrawPnL();
			}
			_lastPositionInfo = (PositionInfo)_positionInfo.Clone();
			
			var exitDone = false;
			if (IsLongExit() && !exitDone)
			{
				ExitLong();
				exitDone = true;
			}
			if (IsShortExit() && !exitDone)
			{
				ExitShort();
				exitDone = true;
			}
			CheckEntryDisabled(exitDone);
			if (IsLongEntry() && !_isLongDisabled)
			{
				EnterLong();
			}
			if (IsShortEntry() && !_isShortDisabled)
			{
				EnterShort();
			}
		}
		
		private void DrawPnL()
		{
			var tagPrefix = _lastPositionInfo.PositionType == MarketPosition.Long ? "ExitLongLabel" : "ExitShortLabel";
			var pnl = _positionInfo.ExitQuantity * _positionInfo.ExitPrice - _lastPositionInfo.Quantity * _lastPositionInfo.Price;
			var markerText = string.Format("P&L {0}", pnl);
			var y = 0d;
			if (_lastPositionInfo.PositionType == MarketPosition.Long)
			{
				DrawText(tagPrefix + CurrentBar.ToString(), markerText, High[0], 70);
			}
			else
			{
				DrawText(tagPrefix + CurrentBar.ToString(), markerText, Low[0], -70);
			}
			if (pnl < 0)
			{
				_totalLossAmount += Math.Abs(pnl);
			}
		}
		
		#endregion
		
		#endregion
		
		#region Main
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			Print(string.Format("{0}. Order '{1}' has been filled (q: {2}, p: {3})", execution.Order.Time, execution.Order.Name, quantity, price));
			_lastPositionInfo = (PositionInfo)_positionInfo.Clone();
			if (_positionInfo.PositionType == MarketPosition.Flat)
			{
				_positionInfo.PositionType = marketPosition;
				_positionInfo.Price = price;
				_positionInfo.Quantity = quantity;
			}
			else
			{
				if (_positionInfo.PositionType != marketPosition)
				{
					_positionInfo.Reset();
					_positionInfo.ExitPrice = price;
					_positionInfo.ExitQuantity = quantity;
				}
			}
			PrintPositionChanged();
		}

		protected override void OnBarUpdate()
		{
			if (!(IsFirstTickOfBar || Calculate == Calculate.OnBarClose))
				return;
			
			if (!IsTradingTime())
				return;
			
			if (string.IsNullOrEmpty(Atm_Strategy_Template))
			{
				ProcessNT8();
			}
			else
			{
				if (State == State.Realtime)
				{
					ProcessATM();
				}
			}
		}
		
		#endregion
		
		#region Properties
		
		#region Delta
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="NetDelta", Description="Volume Net Delta",  Order = 1, GroupName = "Delta")]
        public int NetDelta
        {
            get; set;
        }
		
		
		#endregion
		
		#region Entry&Exit
		
		[NinjaScriptProperty]
		[Display(Name="Enable Long Trades", Description="Enable Long Trades", Order = 1, GroupName="Entry&Exit")]
		public bool Enable_Long
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Bars Above Trend For Long Entry", Description="Bars Above Trend For Long Entry",  Order = 2, GroupName = "Entry&Exit")]
        public int Bars_Above_Trend_For_Long
        {
            get; set;
        }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Short Trades", Description="Enable Short Trades", Order = 3, GroupName="Entry&Exit")]
		public bool Enable_Short
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Bars Below Trend For Long Short", Description="Bars Below Trend For Long Short",  Order = 4, GroupName = "Entry&Exit")]
        public int Bars_Below_Trend_For_Short
        {
            get; set;
        }
		
		[NinjaScriptProperty]
		[Display(Name="Exit by the First Opposite Bar", Description="Exit by the First Opposite Bar", Order = 5, GroupName="Entry&Exit")]
		public bool Exit_By_First_Opposite_Bar
		{ get; set; }
		
		#endregion
		
		#region Profit&Loss
		
		[NinjaScriptProperty]
		[Display(Name="Enable Profit Target", Description="Enable profit target", Order = 1, GroupName="Profit&Loss")]
		public bool Enable_Profit_Target
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Profit Target Ticks", Description="Profit target ticks", Order = 2, GroupName="Profit&Loss")]
		public int Profit_Target_Ticks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Stop Loss", Description="Enable stop loss", Order = 3, GroupName="Profit&Loss")]
		public bool Enable_Stop_Loss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Loss Ticks", Description="Stop loss ticks", Order = 4, GroupName="Profit&Loss")]
		public int Stop_Loss_Ticks
		{ get; set; }
		
		#endregion
		
		#region Trading
		
		[NinjaScriptProperty]
		[Display(Name="Enable Trading Time", Description="Enable trading time", Order = 1, GroupName="Trading")]
		public bool Enable_Trading_Time
		{ get; set; }
		
		[Gui.PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time", Description="Start trading time", Order = 2, GroupName="Trading")]
		public DateTime Start_Trading_Time { get; set; }
		
		[Gui.PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time", Description="End trading time", Order = 3, GroupName="Trading")]
		public DateTime End_Trading_Time { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Disable Trading By Loss Amount", Description="Disable Trading By Loss Amount", Order = 4, GroupName="Trading")]
		public bool Disable_Trading_By_Loss_Amount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Loss Amount", Description="Loss Amount", Order = 5, GroupName="Trading")]
		public double No_Trading_Loss_Amount
		{ get; set; }
		
		#endregion
		
		#region Display
		
		#endregion
		
		#region ATM
		
		[NinjaScriptProperty]
		[Display(Name="ATM Strategy Template", Description="ATM Strategy Template", Order = 1, GroupName="ATM")]
		public string Atm_Strategy_Template
		{ get; set; }
		
		#endregion
		
		#endregion
	}
}
