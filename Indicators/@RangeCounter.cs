//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class RangeCounter : Indicator
	{
		private bool	isAdvancedType;
		private string	rangeString;
		private bool	supportsRange;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRangeCounter;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRangeCounter;
				Calculate					= Calculate.OnPriceChange;
				CountDown					= true;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= false;
				IsOverlay					= true;
				IsChartOnly					= true;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				TextPositionFine			= TextPositionFine.BottomRight;
			}
			else if (State == State.Historical)
			{
				isAdvancedType		= BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.PriceOnVolume || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric;
				bool isOtherType	= BarsPeriod.ToString().IndexOf("Range", StringComparison.Ordinal) >= 0 || BarsPeriod.ToString().IndexOf(Custom.Resource.BarsPeriodTypeNameRange, StringComparison.Ordinal) >= 0;

				if (BarsPeriod.BarsPeriodType == BarsPeriodType.Range || BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Range && isAdvancedType ||
					BarsArray[0].BarsType.BuiltFrom == BarsPeriodType.Tick && isOtherType)
					supportsRange = true;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray == null || BarsArray.Length == 0)
				return;

			if (supportsRange)
			{
				double	high		= High.GetValueAt(Bars.Count - 1 - (Calculate == Calculate.OnBarClose ? 1 : 0));
				double	low			= Low.GetValueAt(Bars.Count - 1 - (Calculate == Calculate.OnBarClose ? 1 : 0));
				double	close		= Close.GetValueAt(Bars.Count - 1 - (Calculate == Calculate.OnBarClose ? 1 : 0));
				int		actualRange	= (int)Math.Round(Math.Max(close - low, high - close) / Bars.Instrument.MasterInstrument.TickSize);
				double	rangeCount	= CountDown ? (isAdvancedType ? BarsPeriod.BaseBarsPeriodValue : BarsPeriod.Value) - actualRange : actualRange;

				rangeString	= CountDown ? string.Format(Custom.Resource.RangeCounterRemaing, rangeCount) : string.Format(Custom.Resource.RangerCounterCount, rangeCount);
			}
			else
				rangeString = Custom.Resource.RangeCounterBarError;

			Draw.TextFixedFine(this, "NinjaScriptInfo", rangeString, TextPositionFine);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
		public bool CountDown { get; set; }

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
		private RangeCounter[] cacheRangeCounter;
		public RangeCounter RangeCounter(bool countDown)
		{
			return RangeCounter(Input, countDown);
		}

		public RangeCounter RangeCounter(bool countDown, TextPositionFine textPositionFine)
		{
			return RangeCounter(Input, countDown, textPositionFine);
		}

		public RangeCounter RangeCounter(ISeries<double> input, bool countDown)
		{
			return RangeCounter(input, countDown, TextPositionFine.BottomRight);
		}

		public RangeCounter RangeCounter(ISeries<double> input, bool countDown, TextPositionFine textPositionFine)
		{
			if (cacheRangeCounter != null)
				for (int idx = 0; idx < cacheRangeCounter.Length; idx++)
					if (cacheRangeCounter[idx] != null && cacheRangeCounter[idx].CountDown == countDown && cacheRangeCounter[idx].TextPositionFine == textPositionFine && cacheRangeCounter[idx].EqualsInput(input))
						return cacheRangeCounter[idx];
			return CacheIndicator<RangeCounter>(new RangeCounter() { CountDown = countDown }, input, ref cacheRangeCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RangeCounter RangeCounter(bool countDown)
		{
			return indicator.RangeCounter(Input, countDown);
		}

		public Indicators.RangeCounter RangeCounter(bool countDown, TextPositionFine textPositionFine)
		{
			return indicator.RangeCounter(Input, countDown, textPositionFine);
		}

		public Indicators.RangeCounter RangeCounter(ISeries<double> input , bool countDown)
		{
			return indicator.RangeCounter(input, countDown);
		}

		public Indicators.RangeCounter RangeCounter(ISeries<double> input, bool countDown, TextPositionFine textPositionFine)
		{
			return indicator.RangeCounter(input, countDown, textPositionFine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RangeCounter RangeCounter(bool countDown)
		{
			return indicator.RangeCounter(Input, countDown, TextPositionFine.BottomRight);
		}

		public Indicators.RangeCounter RangeCounter(bool countDown, TextPositionFine textPositionFine)
		{
			return indicator.RangeCounter(Input, countDown, textPositionFine);
		}

		public Indicators.RangeCounter RangeCounter(ISeries<double> input, bool countDown)
		{
			return indicator.RangeCounter(input, countDown, TextPositionFine.BottomRight);
		}

		public Indicators.RangeCounter RangeCounter(ISeries<double> input, bool countDown, TextPositionFine textPositionFine)
		{
			return indicator.RangeCounter(input, countDown, textPositionFine);
		}
	}
}

#endregion
