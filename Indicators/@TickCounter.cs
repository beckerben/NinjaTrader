//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//

#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TickCounter : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= Custom.Resource.NinjaScriptIndicatorDescriptionTickCounter;
				Name				= Custom.Resource.NinjaScriptIndicatorNameTickCounter;
				Calculate			= Calculate.OnEachTick;
				CountDown			= true;
				DisplayInDataBox	= false;
				DrawOnPricePanel	= false;
				IsChartOnly			= true;
				IsOverlay			= true;
				ShowPercent			= false;
				TextPositionFine	= TextPositionFine.BottomRight;
			}
		}

		protected override void OnBarUpdate()
		{
			double periodValue 	= BarsPeriod.BarsPeriodType == BarsPeriodType.Tick ? BarsPeriod.Value : BarsPeriod.BaseBarsPeriodValue;
			double tickCount 	= ShowPercent ? CountDown ? 1 - Bars.PercentComplete : Bars.PercentComplete : CountDown ? periodValue - Bars.TickCount : Bars.TickCount;
			string tickMsg		= ShowPercent ? tickCount.ToString("P0") : tickCount.ToString(CultureInfo.InvariantCulture);

			string tick1 = BarsPeriod.BarsPeriodType == BarsPeriodType.Tick 
							|| (BarsPeriod.BarsPeriodType is BarsPeriodType.HeikenAshi or BarsPeriodType.PriceOnVolume or BarsPeriodType.Volumetric && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Tick) ? CountDown 
					? Custom.Resource.TickCounterTicksRemaining + tickMsg : Custom.Resource.TickCounterTickCount + tickMsg
					: Custom.Resource.TickCounterBarError;

			Draw.TextFixedFine(this, "NinjaScriptInfo", tick1, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
		public bool CountDown { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "ShowPercent", Order = 2, GroupName = "NinjaScriptParameters")]
		public bool ShowPercent { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
		public TextPositionFine TextPositionFine { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TickCounter[] cacheTickCounter;
		public TickCounter TickCounter(bool countDown, bool showPercent)
		{
			return TickCounter(Input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public TickCounter TickCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return TickCounter(Input, countDown, showPercent, textPositionFine);
		}

		public TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent)
		{
			return TickCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			if (cacheTickCounter != null)
				for (int idx = 0; idx < cacheTickCounter.Length; idx++)
					if (cacheTickCounter[idx] != null && cacheTickCounter[idx].CountDown == countDown && cacheTickCounter[idx].ShowPercent == showPercent && cacheTickCounter[idx].TextPositionFine == textPositionFine && cacheTickCounter[idx].EqualsInput(input))
						return cacheTickCounter[idx];
			return CacheIndicator<TickCounter>(new TickCounter() { CountDown = countDown, ShowPercent = showPercent }, input, ref cacheTickCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TickCounter TickCounter(bool countDown, bool showPercent)
		{
			return indicator.TickCounter(Input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.TickCounter TickCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.TickCounter(Input, countDown, showPercent, textPositionFine);
		}

		public Indicators.TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent)
		{
			return indicator.TickCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.TickCounter(input, countDown, showPercent, textPositionFine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TickCounter TickCounter(bool countDown, bool showPercent)
		{
			return indicator.TickCounter(Input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.TickCounter TickCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.TickCounter(Input, countDown, showPercent, textPositionFine);
		}

		public Indicators.TickCounter TickCounter(ISeries<double> input , bool countDown, bool showPercent)
		{
			return indicator.TickCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
		}

		public Indicators.TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
		{
			return indicator.TickCounter(input, countDown, showPercent, textPositionFine);
		}
	}
}

#endregion
