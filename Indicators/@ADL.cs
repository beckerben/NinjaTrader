//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Accumulation/Distribution (AD) study attempts to quantify the amount of volume flowing into or
	/// out of an instrument by identifying the position of the close of the period in relation to that period's high/low range.
	/// </summary>
	public class ADL : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionADL;
				Name						= Custom.Resource.NinjaScriptIndicatorNameADL;
				IsSuspendedWhileInactive	= true;
				DrawOnPricePanel			= false;
				AddPlot(Brushes.DarkCyan, Custom.Resource.ADLAD);
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
		{
			double high0	= High[0];
			double low0		= Low[0];
			double close0	= Close[0];
			double volume0  = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];

			AD[0] = (CurrentBar == 0 ? 0 : AD[1]) + (high0.ApproxCompare(low0) != 0 ? (close0 - low0 - (high0 - close0)) / (high0 - low0) * volume0 : 0);
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> AD => Values[0];

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ADL[] cacheADL;
		public ADL ADL()
		{
			return ADL(Input);
		}

		public ADL ADL(ISeries<double> input)
		{
			if (cacheADL != null)
				for (int idx = 0; idx < cacheADL.Length; idx++)
					if (cacheADL[idx] != null &&  cacheADL[idx].EqualsInput(input))
						return cacheADL[idx];
			return CacheIndicator<ADL>(new ADL(), input, ref cacheADL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ADL ADL()
		{
			return indicator.ADL(Input);
		}

		public Indicators.ADL ADL(ISeries<double> input )
		{
			return indicator.ADL(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ADL ADL()
		{
			return indicator.ADL(Input);
		}

		public Indicators.ADL ADL(ISeries<double> input )
		{
			return indicator.ADL(input);
		}
	}
}

#endregion
