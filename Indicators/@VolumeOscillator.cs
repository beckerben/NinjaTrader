//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Volume Oscillator measures volume by calculating the difference of a fast and
	/// a slow moving average of volume. The Volume Oscillator can provide insight into the
	/// strength or weakness of a price trend. A positive value suggests there is enough
	/// market support to continue driving price activity in the direction of the current
	/// trend. A negative value suggests there is a lack of support, that prices may begin
	/// to become stagnant or reverse.
	/// </summary>
	public class VolumeOscillator : Indicator
	{
		private SMA smaFast;
		private SMA smaSlow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVolumeOscillator;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVolumeOscillator;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				Fast						= 12;
				Slow						= 26;

				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Bar, Custom.Resource.NinjaScriptIndicatorNameVolumeOscillator);
			}
			else if (State == State.DataLoaded)
			{
				smaFast	= SMA(Volume, Fast);
				smaSlow	= SMA(Volume, Slow);
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
			double value = smaFast[0] - smaSlow[0];
			if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
				value = Core.Globals.ToCryptocurrencyVolume((long)value);
			Value[0] = value;
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast { get; set; }


		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Slow { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VolumeOscillator[] cacheVolumeOscillator;
		public VolumeOscillator VolumeOscillator(int fast, int slow)
		{
			return VolumeOscillator(Input, fast, slow);
		}

		public VolumeOscillator VolumeOscillator(ISeries<double> input, int fast, int slow)
		{
			if (cacheVolumeOscillator != null)
				for (int idx = 0; idx < cacheVolumeOscillator.Length; idx++)
					if (cacheVolumeOscillator[idx] != null && cacheVolumeOscillator[idx].Fast == fast && cacheVolumeOscillator[idx].Slow == slow && cacheVolumeOscillator[idx].EqualsInput(input))
						return cacheVolumeOscillator[idx];
			return CacheIndicator<VolumeOscillator>(new VolumeOscillator(){ Fast = fast, Slow = slow }, input, ref cacheVolumeOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeOscillator VolumeOscillator(int fast, int slow)
		{
			return indicator.VolumeOscillator(Input, fast, slow);
		}

		public Indicators.VolumeOscillator VolumeOscillator(ISeries<double> input , int fast, int slow)
		{
			return indicator.VolumeOscillator(input, fast, slow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeOscillator VolumeOscillator(int fast, int slow)
		{
			return indicator.VolumeOscillator(Input, fast, slow);
		}

		public Indicators.VolumeOscillator VolumeOscillator(ISeries<double> input , int fast, int slow)
		{
			return indicator.VolumeOscillator(input, fast, slow);
		}
	}
}

#endregion
