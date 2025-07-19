//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The VOLMA (Volume Moving Average) plots an exponential moving average (EMA) of volume.
	/// </summary>
	public class VOLMA : Indicator
	{
		private EMA ema;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVOLMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVOLMA;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameVOLMA);
			}
			else if (State == State.DataLoaded)
				ema = EMA(Volume, Period);
			else if (State == State.Historical && Calculate == Calculate.OnPriceChange)
			{
				Draw.TextFixed(this, "NinjaScriptInfo", string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), TextPosition.BottomRight);
				Log(string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), LogLevel.Error);
			}
		}

		protected override void OnBarUpdate()
			=> Value[0] = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)ema[0]) : ema[0];

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VOLMA[] cacheVOLMA;
		public VOLMA VOLMA(int period)
		{
			return VOLMA(Input, period);
		}

		public VOLMA VOLMA(ISeries<double> input, int period)
		{
			if (cacheVOLMA != null)
				for (int idx = 0; idx < cacheVOLMA.Length; idx++)
					if (cacheVOLMA[idx] != null && cacheVOLMA[idx].Period == period && cacheVOLMA[idx].EqualsInput(input))
						return cacheVOLMA[idx];
			return CacheIndicator<VOLMA>(new VOLMA(){ Period = period }, input, ref cacheVOLMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VOLMA VOLMA(int period)
		{
			return indicator.VOLMA(Input, period);
		}

		public Indicators.VOLMA VOLMA(ISeries<double> input , int period)
		{
			return indicator.VOLMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VOLMA VOLMA(int period)
		{
			return indicator.VOLMA(Input, period);
		}

		public Indicators.VOLMA VOLMA(ISeries<double> input , int period)
		{
			return indicator.VOLMA(input, period);
		}
	}
}

#endregion
