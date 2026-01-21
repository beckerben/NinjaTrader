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
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Stochastic Oscillator is made up of two lines that oscillate between
	/// a vertical scale of 0 to 100. The %K is the main line and it is drawn as
	/// a solid line. The second is the %D line and is a moving average of %K.
	/// The %D line is drawn as a dotted line. Use as a buy/sell signal generator,
	/// buying when fast moves above slow and selling when fast moves below slow.
	/// </summary>
	public class StochasticsFastShaded : Indicator
	{
		private Series<double>			den;
		private MAX						max;
		private MIN						min;
		private Series<double>			nom;
		private SharpDX.Direct2D1.Brush	upperShadeBrushDX;
		private SharpDX.Direct2D1.Brush	lowerShadeBrushDX;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "Stochastic Fast with Shaded Areas";
				Name						= "StochasticsFastShaded";
				IsSuspendedWhileInactive	= true;
				PeriodD						= 3;
				PeriodK						= 14;
				UpperShadeColor				= Brushes.Red;
				LowerShadeColor				= Brushes.Green;
				ShadeOpacity				= 30;

				AddPlot(Brushes.Blue,			"D");
				AddPlot(Brushes.Orange,			"K");
				AddLine(Brushes.Red,			20,	"Lower");
				AddLine(Brushes.Red,			80,	"Upper");
				
				// Make lines more visible
				Plots[0].Width = 4;			// D line - very thick
				Plots[0].PlotStyle = PlotStyle.Line;
				Plots[1].Width = 3;			// K line - thick
				Plots[1].PlotStyle = PlotStyle.Line;
				Lines[0].Width = 3;			// Lower line - thick
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[1].Width = 3;			// Upper line - thick
				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
			}
			else if (State == State.DataLoaded)
			{
				den			= new Series<double>(this);
				nom			= new Series<double>(this);
				min			= MIN(Low, PeriodK);
				max			= MAX(High, PeriodK);
			}
		}

		protected override void OnBarUpdate()
		{
			double min0	= min[0];
			nom[0]		= Close[0] - min0;
			den[0]		= max[0] - min0;

			K[0] = den[0].ApproxCompare(0) == 0 ? CurrentBar == 0 ? 50 : K[1] : Math.Min(100, Math.Max(0, 100 * nom[0] / den[0]));

			// Calculate D as SMA of K
			if (CurrentBar >= PeriodD - 1)
			{
				double sum = 0;
				for (int i = 0; i < PeriodD; i++)
				{
					sum += K[i];
				}
				D[0] = sum / PeriodD;
			}
			else
			{
				D[0] = K[0]; // Use K value until we have enough bars for SMA
			}
		}

		public override void OnRenderTargetChanged()
		{
			if (upperShadeBrushDX != null)
				upperShadeBrushDX.Dispose();
			if (lowerShadeBrushDX != null)
				lowerShadeBrushDX.Dispose();
			upperShadeBrushDX = null;
			lowerShadeBrushDX = null;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (ChartBars == null || ChartBars.Count == 0)
				return;

			if (upperShadeBrushDX == null)
			{
				System.Windows.Media.Color upperColor = ((System.Windows.Media.SolidColorBrush)UpperShadeColor).Color;
				byte alpha = (byte)(255 * ShadeOpacity / 100);
				SharpDX.Color4 upperColorDX = new SharpDX.Color4(upperColor.R / 255f, upperColor.G / 255f, upperColor.B / 255f, alpha / 255f);
				upperShadeBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, upperColorDX);
			}

			if (lowerShadeBrushDX == null)
			{
				System.Windows.Media.Color lowerColor = ((System.Windows.Media.SolidColorBrush)LowerShadeColor).Color;
				byte alpha = (byte)(255 * ShadeOpacity / 100);
				SharpDX.Color4 lowerColorDX = new SharpDX.Color4(lowerColor.R / 255f, lowerColor.G / 255f, lowerColor.B / 255f, alpha / 255f);
				lowerShadeBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, lowerColorDX);
			}

			int startIdx = Math.Max(0, ChartBars.GetBarIdxByX(chartControl, 0));
			int endIdx = Math.Min(ChartBars.Count - 1, ChartBars.GetBarIdxByX(chartControl, chartControl.CanvasRight));

			for (int idx = startIdx; idx <= endIdx; idx++)
			{
				if (idx >= K.Count || idx >= D.Count)
					continue;

				double dValue = D.GetValueAt(idx);

				int x = chartControl.GetXByBarIndex(ChartBars, idx);
				int nextX = (idx < endIdx) ? chartControl.GetXByBarIndex(ChartBars, idx + 1) : x + 1;
				int barWidth = Math.Max(1, nextX - x);

				if (dValue > 80)
				{
					float yTop = chartScale.GetYByValue(100);
					float yDLine = chartScale.GetYByValue(dValue);
					RenderTarget.FillRectangle(new SharpDX.RectangleF(x, yTop, barWidth, yDLine - yTop), upperShadeBrushDX);
				}

				if (dValue < 20)
				{
					float yDLine = chartScale.GetYByValue(dValue);
					float yBottom = chartScale.GetYByValue(0);
					RenderTarget.FillRectangle(new SharpDX.RectangleF(x, yDLine, barWidth, yBottom - yDLine), lowerShadeBrushDX);
				}
			}
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> D => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> K => Values[1];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Period D", GroupName = "Parameters", Order = 0)]
		public int PeriodD { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Period K", GroupName = "Parameters", Order = 1)]
		public int PeriodK { get; set; }

		[XmlIgnore]
		[Display(Name = "Upper Shade Color", GroupName = "Shading", Order = 2)]
		public System.Windows.Media.Brush UpperShadeColor { get; set; }

		[Browsable(false)]
		public string UpperShadeColorSerializable
		{
			get { return Serialize.BrushToString(UpperShadeColor); }
			set { UpperShadeColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Lower Shade Color", GroupName = "Shading", Order = 3)]
		public System.Windows.Media.Brush LowerShadeColor { get; set; }

		[Browsable(false)]
		public string LowerShadeColorSerializable
		{
			get { return Serialize.BrushToString(LowerShadeColor); }
			set { LowerShadeColor = Serialize.StringToBrush(value); }
		}

		[Range(1, 100), NinjaScriptProperty]
		[Display(Name = "Shade Opacity (%)", GroupName = "Shading", Order = 4)]
		public int ShadeOpacity { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private StochasticsFastShaded[] cacheStochasticsFastShaded;
		public StochasticsFastShaded StochasticsFastShaded(int periodD, int periodK, int shadeOpacity)
		{
			return StochasticsFastShaded(Input, periodD, periodK, shadeOpacity);
		}

		public StochasticsFastShaded StochasticsFastShaded(ISeries<double> input, int periodD, int periodK, int shadeOpacity)
		{
			if (cacheStochasticsFastShaded != null)
				for (int idx = 0; idx < cacheStochasticsFastShaded.Length; idx++)
					if (cacheStochasticsFastShaded[idx] != null && cacheStochasticsFastShaded[idx].PeriodD == periodD && cacheStochasticsFastShaded[idx].PeriodK == periodK && cacheStochasticsFastShaded[idx].ShadeOpacity == shadeOpacity && cacheStochasticsFastShaded[idx].EqualsInput(input))
						return cacheStochasticsFastShaded[idx];
			return CacheIndicator<StochasticsFastShaded>(new StochasticsFastShaded(){ PeriodD = periodD, PeriodK = periodK, ShadeOpacity = shadeOpacity }, input, ref cacheStochasticsFastShaded);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.StochasticsFastShaded StochasticsFastShaded(int periodD, int periodK, int shadeOpacity)
		{
			return indicator.StochasticsFastShaded(Input, periodD, periodK, shadeOpacity);
		}

		public Indicators.StochasticsFastShaded StochasticsFastShaded(ISeries<double> input , int periodD, int periodK, int shadeOpacity)
		{
			return indicator.StochasticsFastShaded(input, periodD, periodK, shadeOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.StochasticsFastShaded StochasticsFastShaded(int periodD, int periodK, int shadeOpacity)
		{
			return indicator.StochasticsFastShaded(Input, periodD, periodK, shadeOpacity);
		}

		public Indicators.StochasticsFastShaded StochasticsFastShaded(ISeries<double> input , int periodD, int periodK, int shadeOpacity)
		{
			return indicator.StochasticsFastShaded(input, periodD, periodK, shadeOpacity);
		}
	}
}

#endregion
