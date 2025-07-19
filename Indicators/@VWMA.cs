//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The VWMA (Volume-Weighted Moving Average) returns the volume-weighted moving average
	/// for the specified price series and period. VWMA is similar to a Simple Moving Average
	/// (SMA), but each bar of data is weighted by the bar's Volume. VWMA places more significance
	/// on the days with the largest volume and the least for the days with lowest volume for the period specified.
	/// </summary>
	public class VWMA : Indicator
	{
		private double			priorVolPriceSum;
		private double			volPriceSum;
		private Series<double>	volSum;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVWMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVWMA;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				DrawOnPricePanel			= false;
				Period						= 14;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.NinjaScriptIndicatorNameVWMA);
			}
			else if (State == State.DataLoaded)
				volSum	= new Series<double>(this);
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
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				int numBars = Math.Min(CurrentBar, Period);

				double sum	= 0;
				double vsum	= 0;
				for (int i = 0; i < numBars; i++)
				{
					double vol	= Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[i]) : Volume[i];
					sum			+= Input[i] * vol;
					vsum		+= vol;
				}

				// Protect agains div by zero evilness
				Value[0] = vsum <= double.Epsilon ? sum : sum / vsum;
			}
			else
			{
				if (IsFirstTickOfBar)
					priorVolPriceSum = volPriceSum;

				double volume0		= Volume[0];
				double volumePeriod = Volume[Math.Min(Period, CurrentBar)];
				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
				{
					volume0			= Core.Globals.ToCryptocurrencyVolume((long)volume0);
					volumePeriod	= Core.Globals.ToCryptocurrencyVolume((long)volumePeriod);
				}
				volPriceSum = priorVolPriceSum + Input[0] * volume0 - (CurrentBar >= Period ? Input[Period] * volumePeriod : 0);
				volSum[0]	= volume0 + (CurrentBar > 0 ? volSum[1] : 0) - (CurrentBar >= Period ? volumePeriod : 0);
				Value[0]	= volSum[0].ApproxCompare(0) == 0 ? volPriceSum : volPriceSum / volSum[0];
			}
		}

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
		private VWMA[] cacheVWMA;
		public VWMA VWMA(int period)
		{
			return VWMA(Input, period);
		}

		public VWMA VWMA(ISeries<double> input, int period)
		{
			if (cacheVWMA != null)
				for (int idx = 0; idx < cacheVWMA.Length; idx++)
					if (cacheVWMA[idx] != null && cacheVWMA[idx].Period == period && cacheVWMA[idx].EqualsInput(input))
						return cacheVWMA[idx];
			return CacheIndicator<VWMA>(new VWMA(){ Period = period }, input, ref cacheVWMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VWMA VWMA(int period)
		{
			return indicator.VWMA(Input, period);
		}

		public Indicators.VWMA VWMA(ISeries<double> input , int period)
		{
			return indicator.VWMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VWMA VWMA(int period)
		{
			return indicator.VWMA(Input, period);
		}

		public Indicators.VWMA VWMA(ISeries<double> input , int period)
		{
			return indicator.VWMA(input, period);
		}
	}
}

#endregion
