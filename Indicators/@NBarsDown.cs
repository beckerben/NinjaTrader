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
	/// This indicator returns 1 when we have n of consecutive bars down, otherwise returns 0.
	/// A down bar is defined as a bar where the close is below the open and the bars makes a
	/// lower high and a lower low. You can adjust the specific requirements with the indicator options.
	/// </summary>
	public class NBarsDown : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionNBarsDown;
				Name						= Custom.Resource.NinjaScriptIndicatorNameNBarsDown;
				BarCount					= 3;
				BarDown						= true;
				LowerHigh					= true;
				LowerLow					= true;
				IsSuspendedWhileInactive	= true;

				AddPlot(new Stroke(Brushes.Crimson, 2), PlotStyle.Bar, Custom.Resource.NBarsDownTrigger);
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

					if (!(Close[i] < Close[i + 1]))
						break;

					if (BarDown && !(Close[i] < Open[i]))
						break;

					if (LowerHigh && !(High[i] < High[i + 1]))
						break;

					if (LowerLow && !(Low[i] < Low[i + 1]))
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
		[Display(ResourceType = typeof(Custom.Resource), Name = "BarDown", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool BarDown { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "LowerHigh", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool LowerHigh { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "LowerLow", GroupName = "NinjaScriptParameters", Order = 3)]
		public bool LowerLow { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private NBarsDown[] cacheNBarsDown;
		public NBarsDown NBarsDown(int barCount, bool barDown, bool lowerHigh, bool lowerLow)
		{
			return NBarsDown(Input, barCount, barDown, lowerHigh, lowerLow);
		}

		public NBarsDown NBarsDown(ISeries<double> input, int barCount, bool barDown, bool lowerHigh, bool lowerLow)
		{
			if (cacheNBarsDown != null)
				for (int idx = 0; idx < cacheNBarsDown.Length; idx++)
					if (cacheNBarsDown[idx] != null && cacheNBarsDown[idx].BarCount == barCount && cacheNBarsDown[idx].BarDown == barDown && cacheNBarsDown[idx].LowerHigh == lowerHigh && cacheNBarsDown[idx].LowerLow == lowerLow && cacheNBarsDown[idx].EqualsInput(input))
						return cacheNBarsDown[idx];
			return CacheIndicator<NBarsDown>(new NBarsDown(){ BarCount = barCount, BarDown = barDown, LowerHigh = lowerHigh, LowerLow = lowerLow }, input, ref cacheNBarsDown);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.NBarsDown NBarsDown(int barCount, bool barDown, bool lowerHigh, bool lowerLow)
		{
			return indicator.NBarsDown(Input, barCount, barDown, lowerHigh, lowerLow);
		}

		public Indicators.NBarsDown NBarsDown(ISeries<double> input , int barCount, bool barDown, bool lowerHigh, bool lowerLow)
		{
			return indicator.NBarsDown(input, barCount, barDown, lowerHigh, lowerLow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.NBarsDown NBarsDown(int barCount, bool barDown, bool lowerHigh, bool lowerLow)
		{
			return indicator.NBarsDown(Input, barCount, barDown, lowerHigh, lowerLow);
		}

		public Indicators.NBarsDown NBarsDown(ISeries<double> input , int barCount, bool barDown, bool lowerHigh, bool lowerLow)
		{
			return indicator.NBarsDown(input, barCount, barDown, lowerHigh, lowerLow);
		}
	}
}

#endregion
