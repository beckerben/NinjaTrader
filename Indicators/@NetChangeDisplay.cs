//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Displays net change on the chart.
	/// </summary>
	public class NetChangeDisplay : Indicator
	{
		private Cbi.Account		account;
		private double			currentValue;
		private double			lastValue;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionNetChangeDisplay;
				Name						= Custom.Resource.NinjaScriptIndicatorNameNetChangeDisplay;
				Calculate					= Calculate.OnPriceChange;
				IsOverlay					= true;
				DrawOnPricePanel			= true;
				IsSuspendedWhileInactive	= true;
				Unit 						= Cbi.PerformanceUnit.Percent;
				PositiveBrush 				= Brushes.LimeGreen;
				NegativeBrush 				= Brushes.Red;
				Location 					= NetChangePosition.TopRight;
				Font 						= new SimpleFont("Arial", 18);
			}
		}

		private TextPosition GetTextPosition(NetChangePosition ncp) =>
			ncp switch
			{
				NetChangePosition.BottomLeft	=> TextPosition.BottomLeft,
				NetChangePosition.BottomRight	=> TextPosition.BottomRight,
				NetChangePosition.TopLeft		=> TextPosition.TopLeft,
				NetChangePosition.TopRight		=> TextPosition.TopRight,
				_								=> TextPosition.TopRight
			};

		protected override void OnBarUpdate() { }
		
		protected override void OnConnectionStatusUpdate(Cbi.ConnectionStatusEventArgs connectionStatusUpdate)
		{
			if (connectionStatusUpdate.PriceStatus == Cbi.ConnectionStatus.Connected && connectionStatusUpdate.PreviousStatus == Cbi.ConnectionStatus.Connecting
					&& connectionStatusUpdate.Connection.Accounts.Count > 0
					&& account == null)
				account = connectionStatusUpdate.Connection.Accounts[0];
			else if (connectionStatusUpdate.Status == Cbi.ConnectionStatus.Disconnected && connectionStatusUpdate.PreviousStatus == Cbi.ConnectionStatus.Disconnecting
					&& account != null && account.Connection == connectionStatusUpdate.Connection)
				account = null;
		}

		protected override void OnMarketData(Data.MarketDataEventArgs marketDataUpdate)
		{
			if (marketDataUpdate.IsReset)
			{
				currentValue = double.MinValue;
				
				if (Math.Abs(lastValue - currentValue) > 0.000000000000000001)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", FormatValue(currentValue), GetTextPosition(Location), currentValue >= 0 ? PositiveBrush : NegativeBrush, Font, Brushes.Transparent, Brushes.Transparent, 0);
					lastValue = currentValue;
				}
				return;
			}

			if (marketDataUpdate.MarketDataType != Data.MarketDataType.Last || marketDataUpdate.Instrument.MarketData.LastClose == null)
				return;

			double	rate = 0;
			
			if (account != null)
				rate = marketDataUpdate.Instrument.GetConversionRate(Data.MarketDataType.Bid, account.Denomination, out _);

			currentValue = Unit switch
			{
				Cbi.PerformanceUnit.Percent		=> (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / 
													marketDataUpdate.Instrument.MarketData.LastClose.Price,
				Cbi.PerformanceUnit.Pips		=> (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) /
													Instrument.MasterInstrument.TickSize * (Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.Forex ? 0.1 : 1),
				Cbi.PerformanceUnit.Ticks		=> (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / Instrument.MasterInstrument.TickSize,
				Cbi.PerformanceUnit.Currency	=> (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) * Instrument.MasterInstrument.PointValue * rate *
													(Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.Forex ? account?.ForexLotSize ?? Cbi.Account.DefaultLotSize : 1),
				Cbi.PerformanceUnit.Points		=> marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price,
				_								=> currentValue
			};

			if (Math.Abs(lastValue - currentValue) > 0.000000000000000001)
			{
				Draw.TextFixed(this, "NinjaScriptInfo", FormatValue(currentValue), GetTextPosition(Location), currentValue >= 0 ? PositiveBrush : NegativeBrush, Font, Brushes.Transparent, Brushes.Transparent, 0);
				lastValue = currentValue;
			}
		}
		
		public string FormatValue(double value)
		{
			if (value <= double.MinValue)
				return string.Empty;

			switch (Unit)
			{
				case Cbi.PerformanceUnit.Currency:
					{
						Cbi.Currency formatCurrency = account?.Denomination ?? Instrument.MasterInstrument.Currency;
						return Core.Globals.FormatCurrency(value, formatCurrency);
					}
				case Cbi.PerformanceUnit.Points: return value.ToString(Core.Globals.GetTickFormatString(Instrument.MasterInstrument.TickSize), Core.Globals.GeneralOptions.CurrentCulture);
				case Cbi.PerformanceUnit.Percent: return value.ToString("P", Core.Globals.GeneralOptions.CurrentCulture);
				case Cbi.PerformanceUnit.Pips:
					{
						System.Globalization.CultureInfo forexCulture = Core.Globals.GeneralOptions.CurrentCulture.Clone() as System.Globalization.CultureInfo;
						if (forexCulture != null)
							forexCulture.NumberFormat.NumberDecimalSeparator = "'";
						return (Math.Round(value * 10) / 10.0).ToString("0.0", forexCulture);
					}
				case Cbi.PerformanceUnit.Ticks: return Math.Round(value).ToString(Core.Globals.GeneralOptions.CurrentCulture);
				default: return "0";
			}
		}
		
		#region Properties
		[XmlIgnore]
		[Browsable(false)]
		public double NetChange => currentValue;

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Unit", GroupName = "NinjaScriptParameters", Order = 0)]
		public Cbi.PerformanceUnit Unit { get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PositiveColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
		public Brush PositiveBrush { get; set; }
		
		[Browsable(false)]
		public string PositiveBrushSerialize
		{
			get => Serialize.BrushToString(PositiveBrush);
			set => PositiveBrush = Serialize.StringToBrush(value);
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NegativeColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1820)]
		public Brush NegativeBrush { get; set; }
		
		[Browsable(false)]
		public string NegativeBrushSerialize
		{
			get => Serialize.BrushToString(NegativeBrush);
			set => NegativeBrush = Serialize.StringToBrush(value);
		}
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Location", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1830)]
		public NetChangePosition Location { get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Font", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
		public SimpleFont Font { get; set; }
		#endregion
	}
}

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum NetChangePosition
{
	BottomLeft,
	BottomRight,
	TopLeft,
	TopRight,
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private NetChangeDisplay[] cacheNetChangeDisplay;
		public NetChangeDisplay NetChangeDisplay(Cbi.PerformanceUnit unit, NetChangePosition location)
		{
			return NetChangeDisplay(Input, unit, location);
		}

		public NetChangeDisplay NetChangeDisplay(ISeries<double> input, Cbi.PerformanceUnit unit, NetChangePosition location)
		{
			if (cacheNetChangeDisplay != null)
				for (int idx = 0; idx < cacheNetChangeDisplay.Length; idx++)
					if (cacheNetChangeDisplay[idx] != null && cacheNetChangeDisplay[idx].Unit == unit && cacheNetChangeDisplay[idx].Location == location && cacheNetChangeDisplay[idx].EqualsInput(input))
						return cacheNetChangeDisplay[idx];
			return CacheIndicator<NetChangeDisplay>(new NetChangeDisplay(){ Unit = unit, Location = location }, input, ref cacheNetChangeDisplay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.NetChangeDisplay NetChangeDisplay(Cbi.PerformanceUnit unit, NetChangePosition location)
		{
			return indicator.NetChangeDisplay(Input, unit, location);
		}

		public Indicators.NetChangeDisplay NetChangeDisplay(ISeries<double> input , Cbi.PerformanceUnit unit, NetChangePosition location)
		{
			return indicator.NetChangeDisplay(input, unit, location);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.NetChangeDisplay NetChangeDisplay(Cbi.PerformanceUnit unit, NetChangePosition location)
		{
			return indicator.NetChangeDisplay(Input, unit, location);
		}

		public Indicators.NetChangeDisplay NetChangeDisplay(ISeries<double> input , Cbi.PerformanceUnit unit, NetChangePosition location)
		{
			return indicator.NetChangeDisplay(input, unit, location);
		}
	}
}

#endregion
