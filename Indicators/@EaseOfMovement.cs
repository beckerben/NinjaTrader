//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Ease of Movement (EMV) indicator emphasizes days in which the stock is moving
	///  easily and minimizes the days in which the stock is finding it difficult to move.
	/// A buy signal is generated when the EMV crosses above zero, a sell signal when it
	///  crosses below zero. When the EMV hovers around zero, then there are small price
	/// movements and/or high volume, which is to say, the price is not moving easily.
	/// </summary>
	public class EaseOfMovement : Indicator
	{
		private EMA				ema;
		private Series<double>	emv;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionEaseOfMovement;
				Name						= Custom.Resource.NinjaScriptIndicatorNameEaseOfMovement;
				IsSuspendedWhileInactive	= true;
				DrawOnPricePanel			= false;
				Smoothing					= 14;
				VolumeDivisor				= 10000;

				AddPlot(Brushes.DodgerBlue,			Custom.Resource.NinjaScriptIndicatorNameEaseOfMovement);
				AddLine(Brushes.DarkGray,	0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.DataLoaded)
			{
				emv = new Series<double>(this);
				ema = EMA(emv, Smoothing);
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
			if (CurrentBar == 0)
				return;
			double volume0	= Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
			double midPoint	= Median[0] - Median[1];
			double boxRatio = volume0 / VolumeDivisor / (High[0] - Low[0]);

			emv[0] 			= boxRatio.ApproxCompare(0) == 0 ? 0 : midPoint / boxRatio;
			Value[0]		= ema[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smoothing", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Smoothing { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "VolumeDivisor", GroupName = "NinjaScriptParameters", Order = 1)]
		public int VolumeDivisor { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EaseOfMovement[] cacheEaseOfMovement;
		public EaseOfMovement EaseOfMovement(int smoothing, int volumeDivisor)
		{
			return EaseOfMovement(Input, smoothing, volumeDivisor);
		}

		public EaseOfMovement EaseOfMovement(ISeries<double> input, int smoothing, int volumeDivisor)
		{
			if (cacheEaseOfMovement != null)
				for (int idx = 0; idx < cacheEaseOfMovement.Length; idx++)
					if (cacheEaseOfMovement[idx] != null && cacheEaseOfMovement[idx].Smoothing == smoothing && cacheEaseOfMovement[idx].VolumeDivisor == volumeDivisor && cacheEaseOfMovement[idx].EqualsInput(input))
						return cacheEaseOfMovement[idx];
			return CacheIndicator<EaseOfMovement>(new EaseOfMovement(){ Smoothing = smoothing, VolumeDivisor = volumeDivisor }, input, ref cacheEaseOfMovement);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EaseOfMovement EaseOfMovement(int smoothing, int volumeDivisor)
		{
			return indicator.EaseOfMovement(Input, smoothing, volumeDivisor);
		}

		public Indicators.EaseOfMovement EaseOfMovement(ISeries<double> input , int smoothing, int volumeDivisor)
		{
			return indicator.EaseOfMovement(input, smoothing, volumeDivisor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EaseOfMovement EaseOfMovement(int smoothing, int volumeDivisor)
		{
			return indicator.EaseOfMovement(Input, smoothing, volumeDivisor);
		}

		public Indicators.EaseOfMovement EaseOfMovement(ISeries<double> input , int smoothing, int volumeDivisor)
		{
			return indicator.EaseOfMovement(input, smoothing, volumeDivisor);
		}
	}
}

#endregion
