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
	/// Returns a value of 1 when the current close is less than the prior close after penetrating the highest high of the last n bars.
	/// </summary>
	public class KeyReversalDown : Indicator
	{
		private MAX max;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionKeyReversalDown;
				Name						= Custom.Resource.NinjaScriptIndicatorNameKeyReversalDown;
				IsSuspendedWhileInactive	= true;
				Period						= 1;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.KeyReversalPlot0);
			}
			else if (State == State.DataLoaded)
				max = MAX(High, Period);
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period + 1)
				return;

			Value[0] = High[0] > max[1] && Close[0] < Close[1] ? 1: 0;
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
		private KeyReversalDown[] cacheKeyReversalDown;
		public KeyReversalDown KeyReversalDown(int period)
		{
			return KeyReversalDown(Input, period);
		}

		public KeyReversalDown KeyReversalDown(ISeries<double> input, int period)
		{
			if (cacheKeyReversalDown != null)
				for (int idx = 0; idx < cacheKeyReversalDown.Length; idx++)
					if (cacheKeyReversalDown[idx] != null && cacheKeyReversalDown[idx].Period == period && cacheKeyReversalDown[idx].EqualsInput(input))
						return cacheKeyReversalDown[idx];
			return CacheIndicator<KeyReversalDown>(new KeyReversalDown(){ Period = period }, input, ref cacheKeyReversalDown);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KeyReversalDown KeyReversalDown(int period)
		{
			return indicator.KeyReversalDown(Input, period);
		}

		public Indicators.KeyReversalDown KeyReversalDown(ISeries<double> input , int period)
		{
			return indicator.KeyReversalDown(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KeyReversalDown KeyReversalDown(int period)
		{
			return indicator.KeyReversalDown(Input, period);
		}

		public Indicators.KeyReversalDown KeyReversalDown(ISeries<double> input , int period)
		{
			return indicator.KeyReversalDown(input, period);
		}
	}
}

#endregion
