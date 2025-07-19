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
	/// Returns a value of 1 when the current close is greater than the prior close after penetrating the lowest low of the last n bars.
	/// </summary>
	public class KeyReversalUp : Indicator
	{
		private MIN min;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionKeyReversalUp;
				Name						= Custom.Resource.NinjaScriptIndicatorNameKeyReversalUp;
				IsSuspendedWhileInactive	= true;
				Period						= 1;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.KeyReversalPlot0);
			}
			else if (State == State.DataLoaded)
				min = MIN(Low, Period);
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period + 1)
				return;

			Value[0] = Low[0] < min[1] && Close[0] > Close[1] ? 1: 0;
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
		private KeyReversalUp[] cacheKeyReversalUp;
		public KeyReversalUp KeyReversalUp(int period)
		{
			return KeyReversalUp(Input, period);
		}

		public KeyReversalUp KeyReversalUp(ISeries<double> input, int period)
		{
			if (cacheKeyReversalUp != null)
				for (int idx = 0; idx < cacheKeyReversalUp.Length; idx++)
					if (cacheKeyReversalUp[idx] != null && cacheKeyReversalUp[idx].Period == period && cacheKeyReversalUp[idx].EqualsInput(input))
						return cacheKeyReversalUp[idx];
			return CacheIndicator<KeyReversalUp>(new KeyReversalUp(){ Period = period }, input, ref cacheKeyReversalUp);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KeyReversalUp KeyReversalUp(int period)
		{
			return indicator.KeyReversalUp(Input, period);
		}

		public Indicators.KeyReversalUp KeyReversalUp(ISeries<double> input , int period)
		{
			return indicator.KeyReversalUp(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KeyReversalUp KeyReversalUp(int period)
		{
			return indicator.KeyReversalUp(Input, period);
		}

		public Indicators.KeyReversalUp KeyReversalUp(ISeries<double> input , int period)
		{
			return indicator.KeyReversalUp(input, period);
		}
	}
}

#endregion
