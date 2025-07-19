//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Core.FloatingPoint;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Linear regression is used to calculate a best fit line for the price data. In addition an upper and lower band is added by calculating the standard deviation of prices from the regression line.
	/// </summary>
	public class RegressionChannel : Indicator
	{
		private Series<double> interceptSeries;
		private Series<double> slopeSeries;
		private Series<double> stdDeviationSeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRegressionChannel;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRegressionChannel;
				IsAutoScale					= false;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 35;
				Width						= 2;

				AddPlot(Brushes.DarkGray,		Custom.Resource.NinjaScriptIndicatorMiddle);
				AddPlot(Brushes.DodgerBlue,	Custom.Resource.NinjaScriptIndicatorUpper);
				AddPlot(Brushes.DodgerBlue,	Custom.Resource.NinjaScriptIndicatorLower);
			}
			else if (State == State.DataLoaded)
			{
				interceptSeries		= new Series<double>(this);
				slopeSeries			= new Series<double>(this);
				stdDeviationSeries	= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			// First we calculate the linear regression parameters

			double sumX		= (double) Period*(Period - 1)*.5;
			double divisor	= sumX * sumX - (double) Period*Period*(Period - 1)*(2*Period - 1)/6;
			double sumXy	= 0;
			double sumY		= 0;
			int barCount	= Math.Min(Period, CurrentBar);

			for (int count = 0; count < barCount; count++)
			{
				sumXy += count*Input[count];
				sumY += Input[count];
			}

			if (divisor.ApproxCompare(0) == 0 && Period == 0) return;

			double slope		= (Period*sumXy - sumX*sumY)/divisor;
			double intercept	= (sumY - slope*sumX)/Period;

			slopeSeries[0]		= slope;
			interceptSeries[0]	= intercept;

			// Next we calculate the standard deviation of the
			// residuals (vertical distances to the regression line).

			double sumResiduals = 0;

			for (int count = 0; count < barCount; count++)
			{
				double regressionValue = intercept + slope * (Period - 1 - count);
				double residual = Math.Abs(Input[count] - regressionValue);
				sumResiduals += residual;
			}

			double avgResiduals = sumResiduals / Math.Min(CurrentBar - 1, Period);

			sumResiduals = 0;
			for (int count = 0; count < barCount; count++)
			{
				double regressionValue = intercept + slope * (Period - 1 - count);
				double residual = Math.Abs(Input[count] - regressionValue);
				sumResiduals += (residual - avgResiduals) * (residual - avgResiduals);
			}

			double stdDeviation = Math.Sqrt(sumResiduals / Math.Min(CurrentBar + 1, Period));
			stdDeviationSeries[0] = stdDeviation;

			double middle	= intercept + slope * (Period - 1);
			Middle[0]		= CurrentBar == 0 ? Input[0] : middle;
			Upper[0]		= stdDeviation.ApproxCompare(0) == 0 || double.IsInfinity(stdDeviation) ? Input[0] : middle + stdDeviation * Width;
			Lower[0]		= stdDeviation.ApproxCompare(0) == 0 || double.IsInfinity(stdDeviation) ? Input[0] : middle - stdDeviation * Width;
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lower => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Middle => Values[0];

		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptGeneral", Order = 0)]
		public int Period
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Upper => Values[1];

		[Range(1, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Width", GroupName = "NinjaScriptGeneral", Order = 1)]
		public double Width { get; set; }
		#endregion

		#region Misc
		private int GetXPos(int barsBack) =>
			ChartControl.GetXByBarIndex(ChartBars, Math.Max(0, Bars.Count - 1 - barsBack - (Calculate == Calculate.OnBarClose ? 1 : 0)));

		private static int GetYPos(double price, ChartScale chartScale) => chartScale.GetYByValue(price);

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (Bars == null || ChartControl == null) return;

			RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

			ChartPanel panel = chartControl.ChartPanels[ChartPanel.PanelIndex];

			int		idx				= BarsArray[0].Count - 1 - (Calculate == Calculate.OnBarClose ? 1 : 0);
			double	intercept		= interceptSeries.GetValueAt(idx);
			double	slope			= slopeSeries.GetValueAt(idx);
			double	stdDev			= stdDeviationSeries.GetValueAt(idx);
			int		stdDevPixels	= (int) Math.Round(stdDev*Width/(chartScale.MaxValue - chartScale.MinValue)*panel.H, 0);
			int		xPos			= GetXPos(Period - 1 - Displacement);
			int		yPos			= GetYPos(intercept, chartScale);
			int		xPos2			= GetXPos(0 - Displacement);
			int		yPos2			= GetYPos(intercept + slope*(Period - 1), chartScale);
			Vector2	startVector		= new(xPos, yPos);
			Vector2	endVector		= new(xPos2, yPos2);

			// Middle
			RenderTarget.DrawLine(startVector, endVector, Plots[0].BrushDX, Plots[0].Width, Plots[0].StrokeStyle);

			// Upper
			RenderTarget.DrawLine(new Vector2(startVector.X, startVector.Y - stdDevPixels), new Vector2(endVector.X, endVector.Y - stdDevPixels), Plots[1].BrushDX, Plots[1].Width, Plots[1].StrokeStyle);

			// Lower
			RenderTarget.DrawLine(new Vector2(startVector.X, startVector.Y + stdDevPixels), new Vector2(endVector.X, endVector.Y + stdDevPixels), Plots[2].BrushDX, Plots[2].Width, Plots[2].StrokeStyle);

			RenderTarget.AntialiasMode = AntialiasMode.Aliased;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RegressionChannel[] cacheRegressionChannel;
		public RegressionChannel RegressionChannel(int period, double width)
		{
			return RegressionChannel(Input, period, width);
		}

		public RegressionChannel RegressionChannel(ISeries<double> input, int period, double width)
		{
			if (cacheRegressionChannel != null)
				for (int idx = 0; idx < cacheRegressionChannel.Length; idx++)
					if (cacheRegressionChannel[idx] != null && cacheRegressionChannel[idx].Period == period && cacheRegressionChannel[idx].Width == width && cacheRegressionChannel[idx].EqualsInput(input))
						return cacheRegressionChannel[idx];
			return CacheIndicator<RegressionChannel>(new RegressionChannel(){ Period = period, Width = width }, input, ref cacheRegressionChannel);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RegressionChannel RegressionChannel(int period, double width)
		{
			return indicator.RegressionChannel(Input, period, width);
		}

		public Indicators.RegressionChannel RegressionChannel(ISeries<double> input , int period, double width)
		{
			return indicator.RegressionChannel(input, period, width);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RegressionChannel RegressionChannel(int period, double width)
		{
			return indicator.RegressionChannel(Input, period, width);
		}

		public Indicators.RegressionChannel RegressionChannel(ISeries<double> input , int period, double width)
		{
			return indicator.RegressionChannel(input, period, width);
		}
	}
}

#endregion
