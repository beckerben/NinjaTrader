//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// This indicator returns 1 when we have n of consecutive bars up, otherwise returns 0.
	/// An up bar is defined as a bar where the close is above the open and the bars makes a higher
	/// high and a higher low. You can adjust the specific requirements with the indicator options.
	/// </summary>
	public class NBarsUp : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionNBarsUp;
				Name						= Custom.Resource.NinjaScriptIndicatorNameNBarsUp;
				BarCount					= 3;
				BarUp						= true;
				HigherHigh					= true;
				HigherLow					= true;
				IsSuspendedWhileInactive	= true;

				AddPlot(new Stroke(Brushes.DarkCyan, 2), PlotStyle.Bar, Custom.Resource.NinjaScriptIndicatorDiff);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarCount)
				Value[0] = 0;
			else
			{
				bool gotBars = false;

				for (int i = 0; i < BarCount + 1; i++)
				{
					if (i == BarCount)
					{
						gotBars = true;
						break;
					}

					if (!(Close[i] > Close[i + 1]))
						break;

					if (BarUp && !(Close[i] > Open[i]))
						break;

					if (HigherHigh && !(High[i] > High[i + 1]))
						break;

					if (HigherLow && !(Low[i] > Low[i + 1]))
						break;
				}

				Value[0] = gotBars ? 1 : 0;
			}
		}

		#region Properties
		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BarCount", GroupName = "NinjaScriptParameters", Order = 0)]
		public int BarCount { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BarUp", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool BarUp { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "HigherHigh", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool HigherHigh { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "HigherLow", GroupName = "NinjaScriptParameters", Order = 3)]
		public bool HigherLow { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private NBarsUp[] cacheNBarsUp;
		public NBarsUp NBarsUp(int barCount, bool barUp, bool higherHigh, bool higherLow)
		{
			return NBarsUp(Input, barCount, barUp, higherHigh, higherLow);
		}

		public NBarsUp NBarsUp(ISeries<double> input, int barCount, bool barUp, bool higherHigh, bool higherLow)
		{
			if (cacheNBarsUp != null)
				for (int idx = 0; idx < cacheNBarsUp.Length; idx++)
					if (cacheNBarsUp[idx] != null && cacheNBarsUp[idx].BarCount == barCount && cacheNBarsUp[idx].BarUp == barUp && cacheNBarsUp[idx].HigherHigh == higherHigh && cacheNBarsUp[idx].HigherLow == higherLow && cacheNBarsUp[idx].EqualsInput(input))
						return cacheNBarsUp[idx];
			return CacheIndicator<NBarsUp>(new NBarsUp(){ BarCount = barCount, BarUp = barUp, HigherHigh = higherHigh, HigherLow = higherLow }, input, ref cacheNBarsUp);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.NBarsUp NBarsUp(int barCount, bool barUp, bool higherHigh, bool higherLow)
		{
			return indicator.NBarsUp(Input, barCount, barUp, higherHigh, higherLow);
		}

		public Indicators.NBarsUp NBarsUp(ISeries<double> input , int barCount, bool barUp, bool higherHigh, bool higherLow)
		{
			return indicator.NBarsUp(input, barCount, barUp, higherHigh, higherLow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.NBarsUp NBarsUp(int barCount, bool barUp, bool higherHigh, bool higherLow)
		{
			return indicator.NBarsUp(Input, barCount, barUp, higherHigh, higherLow);
		}

		public Indicators.NBarsUp NBarsUp(ISeries<double> input , int barCount, bool barUp, bool higherHigh, bool higherLow)
		{
			return indicator.NBarsUp(input, barCount, barUp, higherHigh, higherLow);
		}
	}
}

#endregion
