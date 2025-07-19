//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Indicates the current buying or selling pressure as a perecentage.
	/// This is a tick by tick indicator. If 'Calculate on bar close' is true, the indicator values will always be 100.
	/// </summary>
	public class BuySellPressure : Indicator
	{
		private double		buys;
		private double 		sells;
		private int 		activeBar = -1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= Custom.Resource.NinjaScriptIndicatorDescriptionBuySellPressure;
				Name				= Custom.Resource.NinjaScriptIndicatorNameBuySellPressure;
				BarsRequiredToPlot	= 1;
				Calculate			= Calculate.OnEachTick;
				DrawOnPricePanel	= false;
				IsOverlay			= false;

				AddPlot(Brushes.DarkCyan,			Custom.Resource.BuySellPressureBuyPressure);
				AddPlot(Brushes.Crimson,			Custom.Resource.BuySellPressureSellPressure);

				AddLine(Brushes.DimGray,	75,		Custom.Resource.NinjaScriptIndicatorUpper);
				AddLine(Brushes.DimGray,	25,		Custom.Resource.NinjaScriptIndicatorLower);
			}
			else if (State == State.Historical)
			{
				if (Calculate != Calculate.OnEachTick)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(Custom.Resource.NinjaScriptOnBarCloseError, Name), TextPosition.BottomRight);
					Log(string.Format(Custom.Resource.NinjaScriptOnBarCloseError, Name), LogLevel.Error);
				}
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if(e.MarketDataType == MarketDataType.Last)
			{
				if (e.Price >= e.Ask)
					buys += Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume(e.Volume) : e.Volume;
				else if (e.Price <= e.Bid)
					sells += Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume(e.Volume) : e.Volume;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < activeBar || CurrentBar <= BarsRequiredToPlot)
				return;

			// New Bar has been formed
			// - Assign last volume counted to the prior bar
			// - Reset volume count for new bar
			if (CurrentBar != activeBar)
			{
				BuyPressure[1] = buys / (buys + sells) * 100;
				SellPressure[1] = sells / (buys + sells) * 100;
				buys = 1;
				sells = 1;
				activeBar = CurrentBar;
			}

			BuyPressure[0] = buys / (buys + sells) * 100;
			SellPressure[0] = sells / (buys + sells) * 100;
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BuyPressure => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SellPressure => Values[1];

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BuySellPressure[] cacheBuySellPressure;
		public BuySellPressure BuySellPressure()
		{
			return BuySellPressure(Input);
		}

		public BuySellPressure BuySellPressure(ISeries<double> input)
		{
			if (cacheBuySellPressure != null)
				for (int idx = 0; idx < cacheBuySellPressure.Length; idx++)
					if (cacheBuySellPressure[idx] != null &&  cacheBuySellPressure[idx].EqualsInput(input))
						return cacheBuySellPressure[idx];
			return CacheIndicator<BuySellPressure>(new BuySellPressure(), input, ref cacheBuySellPressure);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BuySellPressure BuySellPressure()
		{
			return indicator.BuySellPressure(Input);
		}

		public Indicators.BuySellPressure BuySellPressure(ISeries<double> input )
		{
			return indicator.BuySellPressure(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BuySellPressure BuySellPressure()
		{
			return indicator.BuySellPressure(Input);
		}

		public Indicators.BuySellPressure BuySellPressure(ISeries<double> input )
		{
			return indicator.BuySellPressure(input);
		}
	}
}

#endregion
