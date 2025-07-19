//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Trend lines automatically plots recent trends by connect high points together for high trends and connecting low points together for low trends.
	/// </summary>
	public class TrendLines : Indicator
	{
		private int			lastHighBar			= -1;
		private int			lastLowBar			= -1;
		private double		lastHighPrice		= double.MinValue;
		private double		lastLowPrice		= double.MaxValue;
		private bool?		highTrendIsActive;
		private bool		alertIsArmed;
		private TrendRay	highTrend;
		private TrendRay	lowTrend;
		private TrendQueue	trendLines;
		private Swing		swing;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= Custom.Resource.NinjaScriptIndicatorDescriptionTrendLines;
				Name				= Custom.Resource.NinjaScriptIndicatorNameTrendLines;
				Calculate			= Calculate.OnBarClose;
				IsOverlay			= true;
				DisplayInDataBox	= false;
				DrawOnPricePanel	= false;
				PaintPriceMarkers	= false;
				Strength			= 5;
				NumberOfTrendLines	= 1;
				OldTrendsOpacity	= 25;
				AlertOnBreak		= false;
				AlertOnBreakSound	= System.IO.Path.Combine(Core.Globals.InstallDir, "sounds", "Alert2.wav");
				TrendLineHighStroke = new Stroke(Brushes.DarkCyan,	1f);
				TrendLineLowStroke	= new Stroke(Brushes.Goldenrod,	1f);
			}
			else if (State == State.Configure)
				AddPlot(Brushes.White, Custom.Resource.TrendLinesCurrentTrendLine);
			else if (State == State.DataLoaded)
			{
				swing		= Swing(Input, Strength);
				trendLines	= new TrendQueue(this, NumberOfTrendLines);
				if (ChartPanel == null)
					Draw.TextFixed(this, "TrendLinesStrategyAnalyzer", Custom.Resource.TrendLinesNotVisible, TextPosition.BottomRight);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 0)
				return;

			// High Trend Line
			int swingHighBar = swing.SwingHighBar(0, 1, Strength + 1);

			if (swingHighBar != -1)
			{
				double swingHighPrice = !(Input is PriceSeries || Input is Bars) ? Input[swingHighBar] : High[swingHighBar];

				if (swingHighPrice < lastHighPrice && lastHighBar > -1)
				{
					highTrend = new TrendRay(lastHighBar, lastHighPrice, CurrentBar - swingHighBar, swingHighPrice) { IsHigh = true };
					trendLines.Enqueue(highTrend);
					highTrendIsActive	= true;
					alertIsArmed		= true;
				}

				lastHighBar		= CurrentBar - swingHighBar;
				lastHighPrice	= swingHighPrice;
			}

			// Low Trend Line
			int swingLowBar = swing.SwingLowBar(0, 1, Strength + 1);

			if (swingLowBar != -1)
			{
				double swingLowPrice = !(Input is PriceSeries || Input is Bars) ? Input[swingLowBar] : Low[swingLowBar];

				if (swingLowPrice > lastLowPrice && lastLowBar > -1)
				{
					lowTrend = new TrendRay(lastLowBar, lastLowPrice, CurrentBar - swingLowBar, swingLowPrice);
					trendLines.Enqueue(lowTrend);
					highTrendIsActive	= false;
					alertIsArmed		= true;
				}

				lastLowBar		= CurrentBar - swingLowBar;
				lastLowPrice	= swingLowPrice;
			}

			if (highTrendIsActive.HasValue)
			{
				if (ChartControl == null || ChartControl.BarSpacingType == BarSpacingType.TimeBased)
				{
					if (highTrendIsActive.Value)
					{
						double slope = (highTrend.EndPrice - highTrend.StartPrice) / (highTrend.EndBar - highTrend.StartBar);
						Values[0][0] = slope * CurrentBar - (slope * highTrend.StartBar - highTrend.StartPrice);
					}
					else
					{
						double slope = (lowTrend.EndPrice - lowTrend.StartPrice) / (lowTrend.EndBar - lowTrend.StartBar);
						Values[0][0] = slope * CurrentBar - (slope * lowTrend.StartBar - lowTrend.StartPrice);
					}
				}
				else
				{
					if (highTrendIsActive.Value)
					{
						double startSlotIndex	= ChartControl.GetSlotIndexByTime(ChartBars.GetTimeByBarIdx(ChartControl, highTrend.StartBar));
						double endSlotIndex		= ChartControl.GetSlotIndexByTime(ChartBars.GetTimeByBarIdx(ChartControl, highTrend.EndBar));
						double curSlotIndex		= ChartControl.GetSlotIndexByTime(Time[0]);
						double slope			= (highTrend.EndPrice - highTrend.StartPrice) / (endSlotIndex - startSlotIndex);
						Values[0][0]			= slope * curSlotIndex - (slope * startSlotIndex - highTrend.StartPrice);
					}
					else
					{
						double startSlotIndex	= ChartControl.GetSlotIndexByTime(ChartBars.GetTimeByBarIdx(ChartControl, lowTrend.StartBar));
						double endSlotIndex		= ChartControl.GetSlotIndexByTime(ChartBars.GetTimeByBarIdx(ChartControl, lowTrend.EndBar));
						double curSlotIndex		= ChartControl.GetSlotIndexByTime(Time[0]);
						double slope			= (lowTrend.EndPrice - lowTrend.StartPrice) / (endSlotIndex - startSlotIndex);
						Values[0][0]			= slope * curSlotIndex - (slope * startSlotIndex - lowTrend.StartPrice);
					}
				}

				if (State == State.Realtime && AlertOnBreak && alertIsArmed && (CrossAbove(Input, Values[0][0], 1) || CrossBelow(Input, Values[0][0], 1)))
				{
					Alert(string.Empty, Priority.High, string.Format(Custom.Resource.TrendLinesTrendLineBroken, 
							highTrendIsActive.Value ? Custom.Resource.TrendLinesTrendLineHigh: Custom.Resource.TrendLinesTrendLineLow), 
						AlertOnBreakSound, 0, Brushes.Transparent, highTrendIsActive.Value ? TrendLineHighStroke.Brush : TrendLineLowStroke.Brush);

					alertIsArmed = false;
				}
			}
		}

		public override void OnCalculateMinMax()
		{
			double minValue = double.MaxValue;
			double maxValue = double.MinValue;

			foreach (TrendRay trend in trendLines)
				AutoScalePerRay(trend.Ray, ref minValue, ref maxValue);

			MinValue = minValue;
			MaxValue = maxValue;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale) { /* Don't Render Plots */ }

		#region Helpers
		private void AutoScalePerRay(Ray ray, ref double minValue, ref double maxValue)
		{
			// Do not do anything if there is no Ray (Strategy Analyzer chart)
			if (ray == null)
				return;

			int startIdx = ChartBars.GetBarIdxByTime(ChartControl, ray.StartAnchor.Time);

			if (startIdx >= ChartBars.FromIndex - Displacement && startIdx <= ChartBars.ToIndex - Displacement)
			{
				if (ray.StartAnchor.Price < minValue)
					minValue = ray.StartAnchor.Price;
				if (ray.StartAnchor.Price > maxValue)
					maxValue = ray.StartAnchor.Price;
			}

			int endIdx = ChartBars.GetBarIdxByTime(ChartControl, ray.EndAnchor.Time);

			if (endIdx >= ChartBars.FromIndex - Displacement && endIdx <= ChartBars.ToIndex - Displacement)
			{
				if (ray.EndAnchor.Price < minValue)
					minValue = ray.EndAnchor.Price;
				if (ray.EndAnchor.Price > maxValue)
					maxValue = ray.EndAnchor.Price;
			}
		}

		private class TrendRay
		{
			public readonly	int		StartBar;
			public readonly	double	StartPrice;
			public readonly	int		EndBar;
			public readonly	double	EndPrice;
			public			Ray		Ray;
			public			bool		IsHigh;

			public TrendRay(int startBar, double startPrice, int endBar, double endPrice)
			{
				StartBar	= startBar;
				StartPrice	= startPrice;
				EndBar		= endBar;
				EndPrice	= endPrice;
			}
		}

		private class TrendQueue : Queue<TrendRay>
		{
			private readonly	TrendLines	instance;
			private				TrendRay	lastTrend;

			public new void Enqueue(TrendRay trend)
			{
				if (instance.ChartControl != null)
				{
					string rayName	= $"{(trend.IsHigh ? Custom.Resource.TrendLinesTrendLineHigh : Custom.Resource.TrendLinesTrendLineLow)}_{trend.StartBar}";
					trend.Ray		= Draw.Ray(instance,
												rayName,
												false,
												instance.CurrentBar - trend.StartBar - instance.Displacement,
												trend.StartPrice,
												instance.CurrentBar - trend.EndBar - instance.Displacement,
												trend.EndPrice,
												trend.IsHigh ? instance.TrendLineHighStroke.Brush : instance.TrendLineLowStroke.Brush,
												trend.IsHigh ? instance.TrendLineHighStroke.DashStyleHelper : instance.TrendLineLowStroke.DashStyleHelper,
												(int)(trend.IsHigh ? instance.TrendLineHighStroke.Width : instance.TrendLineLowStroke.Width));

					trend.Ray.Stroke.Opacity = trend.IsHigh ? instance.TrendLineHighStroke.Opacity : instance.TrendLineLowStroke.Opacity;

					if (lastTrend != null)
						lastTrend.Ray.Stroke.Opacity = instance.OldTrendsOpacity;
				}

				lastTrend = trend;
				base.Enqueue(trend);

				// Make it into a circular buffer
				if (Count > instance.NumberOfTrendLines)
				{
					TrendRay toRemove = Dequeue();

					// Ray will be null if no ChartControl
					if (toRemove.Ray != null)
						instance.RemoveDrawObject(toRemove.Ray.Tag);
				}
			}

			public TrendQueue(TrendLines instance, int capacity) : base(capacity) => this.instance = instance;
		}
		#endregion

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Strength", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Strength { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumberOfTrendLines", GroupName = "NinjaScriptParameters", Order = 1)]
		public int NumberOfTrendLines { get; set; }

		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "OldTrendsOpacity", GroupName = "NinjaScriptParameters", Order = 2)]
		public int OldTrendsOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AlertOnBreak", GroupName = "NinjaScriptParameters", Order = 3)]
		public bool AlertOnBreak { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AlertOnBreakSound", GroupName = "NinjaScriptParameters", Order = 4)]
		public string AlertOnBreakSound { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "TrendLinesTrendLineHigh", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
		public Stroke TrendLineHighStroke { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "TrendLinesTrendLineLow", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
		public Stroke TrendLineLowStroke { get; set; }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TrendLines[] cacheTrendLines;
		public TrendLines TrendLines(int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
		{
			return TrendLines(Input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
		}

		public TrendLines TrendLines(ISeries<double> input, int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
		{
			if (cacheTrendLines != null)
				for (int idx = 0; idx < cacheTrendLines.Length; idx++)
					if (cacheTrendLines[idx] != null && cacheTrendLines[idx].Strength == strength && cacheTrendLines[idx].NumberOfTrendLines == numberOfTrendLines && cacheTrendLines[idx].OldTrendsOpacity == oldTrendsOpacity && cacheTrendLines[idx].AlertOnBreak == alertOnBreak && cacheTrendLines[idx].EqualsInput(input))
						return cacheTrendLines[idx];
			return CacheIndicator<TrendLines>(new TrendLines() { Strength = strength, NumberOfTrendLines = numberOfTrendLines, OldTrendsOpacity = oldTrendsOpacity, AlertOnBreak = alertOnBreak }, input, ref cacheTrendLines);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TrendLines TrendLines(int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
		{
			return indicator.TrendLines(Input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
		}

		public Indicators.TrendLines TrendLines(ISeries<double> input, int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
		{
			return indicator.TrendLines(input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TrendLines TrendLines(int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
		{
			return indicator.TrendLines(Input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
		}

		public Indicators.TrendLines TrendLines(ISeries<double> input, int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
		{
			return indicator.TrendLines(input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
		}
	}
}

#endregion