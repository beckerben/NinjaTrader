//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Relative Spread Strength of the spread between two moving averages. TASC, October 2006, p. 16.
	/// </summary>
	public class RSS : Indicator
	{
		private EMA				ema1;
		private EMA				ema2;
		private RSI				rsi;
		private SMA				sma;
		private Series<double>	spread;
		private Series<double>	rs;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRSS;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRSS;
				IsSuspendedWhileInactive	= true;
				EMA1						= 10;
				EMA2						= 40;
				Length						= 5;

				AddPlot(Brushes.DarkCyan,		Custom.Resource.NinjaScriptIndicatorNameRSS);
				AddLine(Brushes.DarkGray,	20,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.DarkGray,	80,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				spread 	= new Series<double>(this);
				rs		= new Series<double>(this);
				ema1	= EMA(EMA1);
				ema2	= EMA(EMA2);
				rsi		= RSI(spread, Length, 1);
				sma		= SMA(rs, 5);
			}
		}

		protected override void OnBarUpdate()
		{
			spread[0]	= ema1[0] - ema2[0];
			rs[0] 		= rsi[0];
			Value[0] 	= sma[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "EMA1", GroupName = "NinjaScriptParameters", Order = 0)]
		public int EMA1 { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "EMA2", GroupName = "NinjaScriptParameters", Order = 1)]
		public int EMA2 { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Length", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Length { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RSS[] cacheRSS;
		public RSS RSS(int eMA1, int eMA2, int length)
		{
			return RSS(Input, eMA1, eMA2, length);
		}

		public RSS RSS(ISeries<double> input, int eMA1, int eMA2, int length)
		{
			if (cacheRSS != null)
				for (int idx = 0; idx < cacheRSS.Length; idx++)
					if (cacheRSS[idx] != null && cacheRSS[idx].EMA1 == eMA1 && cacheRSS[idx].EMA2 == eMA2 && cacheRSS[idx].Length == length && cacheRSS[idx].EqualsInput(input))
						return cacheRSS[idx];
			return CacheIndicator<RSS>(new RSS(){ EMA1 = eMA1, EMA2 = eMA2, Length = length }, input, ref cacheRSS);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RSS RSS(int eMA1, int eMA2, int length)
		{
			return indicator.RSS(Input, eMA1, eMA2, length);
		}

		public Indicators.RSS RSS(ISeries<double> input , int eMA1, int eMA2, int length)
		{
			return indicator.RSS(input, eMA1, eMA2, length);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RSS RSS(int eMA1, int eMA2, int length)
		{
			return indicator.RSS(Input, eMA1, eMA2, length);
		}

		public Indicators.RSS RSS(ISeries<double> input , int eMA1, int eMA2, int length)
		{
			return indicator.RSS(input, eMA1, eMA2, length);
		}
	}
}

#endregion
