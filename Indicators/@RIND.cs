//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// RIND (Range Indicator) compares the intraday range (high - low) to the
	/// inter-day (close - previous close) range. When the intraday range is greater
	/// than the inter-day range, the Range Indicator will be a high value. This
	/// signals an end to the current trend. When the Range Indicator is at a low
	/// level, a new trend is about to start.
	/// </summary>
	public class RIND : Indicator
	{
		private EMA				ema;
		private MIN				min;
		private MAX				max;
		private Series<double>	stochRange;
		private Series<double>	val1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRIND;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRIND;
				IsOverlay					= false;
				IsSuspendedWhileInactive	= true;
				PeriodQ						= 3;
				Smooth						= 10;

				AddPlot(Brushes.DarkCyan, Custom.Resource.NinjaScriptIndicatorNameRIND);
			}

			else if (State == State.DataLoaded)
			{
				stochRange 	= new Series<double>(this);
				val1		= new Series<double>(this);
				ema			= EMA(stochRange, Smooth);
				min			= MIN(val1, PeriodQ);
				max			= MAX(val1, PeriodQ);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				stochRange[0]	= 50;
				return;
			}

			double high0		= High[0];
			double low0			= Low[0];
			double close0		= Close[0];
			double close1		= Close[1];
			double trueRange	= Math.Max(high0, close1) - Math.Min(low0, close1);

			if (close0 > close1)
				val1[0] = trueRange / (close0 - close1);
			else
				val1[0] = trueRange;

			double min0		= min[0];
			double max0		= max[0];
			double val10	= val1[0];

			if (max0 - min0 > 0)
				stochRange[0] = 100 * ((val10 - min0) / (max0 - min0));
			else
				stochRange[0] = 100 * (val10 - min0);

			Value[0] = ema[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PeriodQ", GroupName = "NinjaScriptParameters", Order = 0)]
		public int PeriodQ { get; set; }

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
		private RIND[] cacheRIND;
		public RIND RIND(int periodQ, int smooth)
		{
			return RIND(Input, periodQ, smooth);
		}

		public RIND RIND(ISeries<double> input, int periodQ, int smooth)
		{
			if (cacheRIND != null)
				for (int idx = 0; idx < cacheRIND.Length; idx++)
					if (cacheRIND[idx] != null && cacheRIND[idx].PeriodQ == periodQ && cacheRIND[idx].Smooth == smooth && cacheRIND[idx].EqualsInput(input))
						return cacheRIND[idx];
			return CacheIndicator<RIND>(new RIND(){ PeriodQ = periodQ, Smooth = smooth }, input, ref cacheRIND);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RIND RIND(int periodQ, int smooth)
		{
			return indicator.RIND(Input, periodQ, smooth);
		}

		public Indicators.RIND RIND(ISeries<double> input , int periodQ, int smooth)
		{
			return indicator.RIND(input, periodQ, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RIND RIND(int periodQ, int smooth)
		{
			return indicator.RIND(Input, periodQ, smooth);
		}

		public Indicators.RIND RIND(ISeries<double> input , int periodQ, int smooth)
		{
			return indicator.RIND(input, periodQ, smooth);
		}
	}
}

#endregion
