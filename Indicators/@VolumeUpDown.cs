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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VolumeUpDown : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Calculate					= Calculate.OnBarClose;
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVolumeUpDown;
				DrawOnPricePanel			= false;
				IsOverlay					= false;
				IsSuspendedWhileInactive	= true;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVolumeUpDown;

				AddPlot(new Stroke(Brushes.DarkCyan,	2),	PlotStyle.Bar,	Custom.Resource.VolumeUp);
				AddPlot(new Stroke(Brushes.Crimson,	2),	PlotStyle.Bar,	Custom.Resource.VolumeDown);
				AddLine(Brushes.DarkGray,				0,					Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.Historical && Calculate == Calculate.OnPriceChange)
			{
				Draw.TextFixed(this, "NinjaScriptInfo", string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), TextPosition.BottomRight);
				Log(string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), LogLevel.Error);
			}
		}

		protected override void OnBarUpdate()
		{
			if (Close[0] >= Open[0])
			{
				UpVolume[0] = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
				DownVolume.Reset();
			}
			else
			{
				UpVolume.Reset();
				DownVolume[0] = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
			}
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DownVolume => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpVolume => Values[0];
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VolumeUpDown[] cacheVolumeUpDown;
		public VolumeUpDown VolumeUpDown()
		{
			return VolumeUpDown(Input);
		}

		public VolumeUpDown VolumeUpDown(ISeries<double> input)
		{
			if (cacheVolumeUpDown != null)
				for (int idx = 0; idx < cacheVolumeUpDown.Length; idx++)
					if (cacheVolumeUpDown[idx] != null &&  cacheVolumeUpDown[idx].EqualsInput(input))
						return cacheVolumeUpDown[idx];
			return CacheIndicator<VolumeUpDown>(new VolumeUpDown(), input, ref cacheVolumeUpDown);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeUpDown VolumeUpDown()
		{
			return indicator.VolumeUpDown(Input);
		}

		public Indicators.VolumeUpDown VolumeUpDown(ISeries<double> input )
		{
			return indicator.VolumeUpDown(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeUpDown VolumeUpDown()
		{
			return indicator.VolumeUpDown(Input);
		}

		public Indicators.VolumeUpDown VolumeUpDown(ISeries<double> input )
		{
			return indicator.VolumeUpDown(input);
		}
	}
}

#endregion
