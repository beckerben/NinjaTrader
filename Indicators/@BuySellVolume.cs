//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BuySellVolume : Indicator
	{
		private int		activeBar;
		private double	buys;
		private double	sells;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description				= Custom.Resource.NinjaScriptIndicatorDescriptionBuySellVolume;
				Name					= Custom.Resource.NinjaScriptIndicatorNameBuySellVolume;
				BarsRequiredToPlot		= 1;
				Calculate				= Calculate.OnEachTick;
				DrawOnPricePanel		= false;
				IsOverlay				= false;
				DisplayInDataBox		= true;

				// Plots will overlap each other no matter which one of these comes first
				// in NT8, we would add the Sells first in code and then Buys, and the "Sells" was always in front of the buys.
				AddPlot(new Stroke(Brushes.DarkCyan,	2), PlotStyle.Bar, Custom.Resource.BuySellVolumeBuys);
				AddPlot(new Stroke(Brushes.Crimson,		2), PlotStyle.Bar, Custom.Resource.BuySellVolumeSells);
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
				if(e.Price >= e.Ask)
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
				Sells[1] = sells;
				Buys[1] = buys + sells;
				buys = 0;
				sells = 0;
				activeBar = CurrentBar;
			}

			Sells[0] = sells;
			Buys[0] = buys + sells;
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Sells => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Buys => Values[0];
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BuySellVolume[] cacheBuySellVolume;
		public BuySellVolume BuySellVolume()
		{
			return BuySellVolume(Input);
		}

		public BuySellVolume BuySellVolume(ISeries<double> input)
		{
			if (cacheBuySellVolume != null)
				for (int idx = 0; idx < cacheBuySellVolume.Length; idx++)
					if (cacheBuySellVolume[idx] != null &&  cacheBuySellVolume[idx].EqualsInput(input))
						return cacheBuySellVolume[idx];
			return CacheIndicator<BuySellVolume>(new BuySellVolume(), input, ref cacheBuySellVolume);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BuySellVolume BuySellVolume()
		{
			return indicator.BuySellVolume(Input);
		}

		public Indicators.BuySellVolume BuySellVolume(ISeries<double> input )
		{
			return indicator.BuySellVolume(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BuySellVolume BuySellVolume()
		{
			return indicator.BuySellVolume(Input);
		}

		public Indicators.BuySellVolume BuySellVolume(ISeries<double> input )
		{
			return indicator.BuySellVolume(input);
		}
	}
}

#endregion
