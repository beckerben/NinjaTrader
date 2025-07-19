//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The VROC (Volume Rate-of-Change) shows whether or not a volume trend is
	/// developing in either an up or down direction. It is similar to the ROC
	/// indicator, but is applied to volume instead.
	/// </summary>
	public class VROC : Indicator
	{
		private Series<double>	smaVolume;
		private SMA				sma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVROC;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVROC;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				Period						= 14;
				Smooth						= 3;

				AddPlot(Brushes.Goldenrod,	Custom.Resource.NinjaScriptIndicatorNameVROC);
				AddLine(Brushes.DarkGray,	0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
			}

			else if (State == State.DataLoaded)
			{
				smaVolume	= new Series<double>(this);
				sma			= SMA(smaVolume, Smooth);
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
			double back 	= Volume[Math.Min(CurrentBar, Period - 1)];
			double volume0	= Volume[0];

			if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
			{
				back		= Core.Globals.ToCryptocurrencyVolume((long)back);
				volume0		= Core.Globals.ToCryptocurrencyVolume((long)volume0);
			}
			smaVolume[0] 	= 100 * volume0 / (back == 0 ? 1 : back) - 100;
			Value[0]		= sma[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Smooth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VROC[] cacheVROC;
		public VROC VROC(int period, int smooth)
		{
			return VROC(Input, period, smooth);
		}

		public VROC VROC(ISeries<double> input, int period, int smooth)
		{
			if (cacheVROC != null)
				for (int idx = 0; idx < cacheVROC.Length; idx++)
					if (cacheVROC[idx] != null && cacheVROC[idx].Period == period && cacheVROC[idx].Smooth == smooth && cacheVROC[idx].EqualsInput(input))
						return cacheVROC[idx];
			return CacheIndicator<VROC>(new VROC(){ Period = period, Smooth = smooth }, input, ref cacheVROC);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VROC VROC(int period, int smooth)
		{
			return indicator.VROC(Input, period, smooth);
		}

		public Indicators.VROC VROC(ISeries<double> input , int period, int smooth)
		{
			return indicator.VROC(input, period, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VROC VROC(int period, int smooth)
		{
			return indicator.VROC(Input, period, smooth);
		}

		public Indicators.VROC VROC(ISeries<double> input , int period, int smooth)
		{
			return indicator.VROC(input, period, smooth);
		}
	}
}

#endregion
