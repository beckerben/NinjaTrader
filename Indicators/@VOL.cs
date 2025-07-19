//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Volume is simply the number of shares (or contracts) traded during a specified time frame (e.g. hour, day, week, month, etc).
	/// </summary>
	public class VOL : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVOL;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVOL;
				BarsRequiredToPlot			= 0;
				Calculate					= Calculate.OnEachTick;
				DrawOnPricePanel			= false;
				IsSuspendedWhileInactive	= true;

				AddPlot(new Stroke(Brushes.DodgerBlue, 2),	PlotStyle.Bar,	Custom.Resource.VOLVolume);
				AddLine(Brushes.DarkGray, 0,			Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.Historical)
			{
				if (Calculate == Calculate.OnPriceChange)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), TextPosition.BottomRight);
					Log(string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), LogLevel.Error);
				}
			}
		}

		protected override void OnBarUpdate()
			=> Value[0] = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VOL[] cacheVOL;
		public VOL VOL()
		{
			return VOL(Input);
		}

		public VOL VOL(ISeries<double> input)
		{
			if (cacheVOL != null)
				for (int idx = 0; idx < cacheVOL.Length; idx++)
					if (cacheVOL[idx] != null &&  cacheVOL[idx].EqualsInput(input))
						return cacheVOL[idx];
			return CacheIndicator<VOL>(new VOL(), input, ref cacheVOL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VOL VOL()
		{
			return indicator.VOL(Input);
		}

		public Indicators.VOL VOL(ISeries<double> input )
		{
			return indicator.VOL(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VOL VOL()
		{
			return indicator.VOL(Input);
		}

		public Indicators.VOL VOL(ISeries<double> input )
		{
			return indicator.VOL(input);
		}
	}
}

#endregion
