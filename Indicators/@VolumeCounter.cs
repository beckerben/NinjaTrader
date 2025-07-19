//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VolumeCounter : Indicator
	{
		private double volume;
		private bool isVolume, isVolumeBase;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVolumeCounter;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVolumeCounter;
				Calculate					= Calculate.OnEachTick;
				CountDown					= true;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= false;
				IsChartOnly					= true;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				ShowPercent					= true;
				TextPositionFine			= TextPositionFine.BottomRight;
			}
			else if(State == State.DataLoaded)
			{
				isVolume 		= BarsPeriod.BarsPeriodType == BarsPeriodType.Volume;
				isVolumeBase 	= (BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.PriceOnVolume || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Volume;
			}
		}

		protected override void OnBarUpdate()
		{
			volume = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];

			double volumeCount = ShowPercent
				? CountDown ? (1 - Bars.PercentComplete) * 100 : Bars.PercentComplete * 100
				: CountDown ? (isVolumeBase ? BarsPeriod.BaseBarsPeriodValue : BarsPeriod.Value) - volume : volume;

			string volume1 = isVolume || isVolumeBase
				? (CountDown ? Custom.Resource.VolumeCounterVolumeRemaining + volumeCount : Custom.Resource.VolumeCounterVolumeCount + volumeCount) + (ShowPercent ? "%" : "")
				: Custom.Resource.VolumeCounterBarError;

			Draw.TextFixedFine(this, "NinjaScriptInfo", volume1, TextPositionFine);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CountDown", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool CountDown { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowPercent", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool ShowPercent { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
		public TextPositionFine TextPositionFine { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VolumeCounter[] cacheVolumeCounter;
		public VolumeCounter VolumeCounter(bool countDown, bool showPercent)
		{
			return VolumeCounter(Input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public VolumeCounter VolumeCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return VolumeCounter(Input, countDown, showPercent, textPositionFine);
		}

		public VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent)
		{
			return VolumeCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			if (cacheVolumeCounter != null)
				for (int idx = 0; idx < cacheVolumeCounter.Length; idx++)
					if (cacheVolumeCounter[idx] != null && cacheVolumeCounter[idx].CountDown == countDown && cacheVolumeCounter[idx].ShowPercent == showPercent && cacheVolumeCounter[idx].TextPositionFine == textPositionFine && cacheVolumeCounter[idx].EqualsInput(input))
						return cacheVolumeCounter[idx];
			return CacheIndicator<VolumeCounter>(new VolumeCounter() { CountDown = countDown, ShowPercent = showPercent }, input, ref cacheVolumeCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeCounter VolumeCounter(bool countDown, bool showPercent)
		{
			return indicator.VolumeCounter(Input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.VolumeCounter VolumeCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.VolumeCounter(Input, countDown, showPercent, textPositionFine);
		}

		public Indicators.VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent)
		{
			return indicator.VolumeCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.VolumeCounter(input, countDown, showPercent, textPositionFine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeCounter VolumeCounter(bool countDown, bool showPercent)
		{
			return indicator.VolumeCounter(Input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.VolumeCounter VolumeCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.VolumeCounter(Input, countDown, showPercent, textPositionFine);
		}

		public Indicators.VolumeCounter VolumeCounter(ISeries<double> input , bool countDown, bool showPercent)
		{
			return indicator.VolumeCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.VolumeCounter(input, countDown, showPercent, textPositionFine);
		}
	}
}

#endregion
