//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Chaikin Volatility
	/// </summary>
	public class ChaikinVolatility : Indicator
	{
		private EMA	ema;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionChaikinVolatility;
				Name						= Custom.Resource.NinjaScriptIndicatorNameChaikinVolatility;
				IsSuspendedWhileInactive	= true;
				MAPeriod					= 10;
				ROCPeriod					= 10;

				AddPlot(Brushes.Crimson, Custom.Resource.NinjaScriptIndicatorNameChaikinVolatility);
			}
			else if (State == State.DataLoaded)
				ema	= EMA(Range(), MAPeriod);
		}

		protected override void OnBarUpdate()
		{
			double emaRocPeriod	= ema[Math.Min(CurrentBar, ROCPeriod)];
			Value[0]			= CurrentBar == 0 ? ema[0] : (ema[0] - emaRocPeriod) / emaRocPeriod * 100;
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "MAPeriod", GroupName = "NinjaScriptParameters", Order = 0)]
		public int MAPeriod { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ROCPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
		public int ROCPeriod { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ChaikinVolatility[] cacheChaikinVolatility;
		public ChaikinVolatility ChaikinVolatility(int mAPeriod, int rOCPeriod)
		{
			return ChaikinVolatility(Input, mAPeriod, rOCPeriod);
		}

		public ChaikinVolatility ChaikinVolatility(ISeries<double> input, int mAPeriod, int rOCPeriod)
		{
			if (cacheChaikinVolatility != null)
				for (int idx = 0; idx < cacheChaikinVolatility.Length; idx++)
					if (cacheChaikinVolatility[idx] != null && cacheChaikinVolatility[idx].MAPeriod == mAPeriod && cacheChaikinVolatility[idx].ROCPeriod == rOCPeriod && cacheChaikinVolatility[idx].EqualsInput(input))
						return cacheChaikinVolatility[idx];
			return CacheIndicator<ChaikinVolatility>(new ChaikinVolatility(){ MAPeriod = mAPeriod, ROCPeriod = rOCPeriod }, input, ref cacheChaikinVolatility);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ChaikinVolatility ChaikinVolatility(int mAPeriod, int rOCPeriod)
		{
			return indicator.ChaikinVolatility(Input, mAPeriod, rOCPeriod);
		}

		public Indicators.ChaikinVolatility ChaikinVolatility(ISeries<double> input , int mAPeriod, int rOCPeriod)
		{
			return indicator.ChaikinVolatility(input, mAPeriod, rOCPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ChaikinVolatility ChaikinVolatility(int mAPeriod, int rOCPeriod)
		{
			return indicator.ChaikinVolatility(Input, mAPeriod, rOCPeriod);
		}

		public Indicators.ChaikinVolatility ChaikinVolatility(ISeries<double> input , int mAPeriod, int rOCPeriod)
		{
			return indicator.ChaikinVolatility(input, mAPeriod, rOCPeriod);
		}
	}
}

#endregion
