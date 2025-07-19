//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class IchimokuCloud : Indicator
	{
		private			Series<double>				baseLine;
		private			SharpDX.Direct2D1.Brush		bearishBrushDx;
		private			SharpDX.Direct2D1.Brush		bullishBrushDx;
		private const	float						CloudOpacity			= 0.30f;
		private			Series<double>				conversionLine;
		private			Series<double>				laggingLine;
		private			Series<double>				leadingSpanA;
		private			Series<double>				leadingSpanB;
		private			int							normalDisplacement;
		private			List<Point>					selectionPoints			= new ();			
		private			int							totalLag;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionIchimokuCloud;
				Name						= Custom.Resource.NinjaScriptIndicatorNameIchimokuCloud;
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;

				DisplayInDataBox			= true;
				DrawOnPricePanel			= true;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				MaximumBarsLookBack			= MaximumBarsLookBack.Infinite;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive	= true;

				BasePeriod					= 26;
				ConversionPeriod			= 9;
				LaggingDisplacement			= 26;
				LeadingSpanBPeriod			= 52;
				SpanDisplacement			= -26;

				Displacement				= Math.Abs(SpanDisplacement);

				AddPlot(Brushes.Blue,		"Conversion (Tenkan)");
				AddPlot(Brushes.Red,		"Base (Kijun)");
				AddPlot(Brushes.LimeGreen,	"Leading (Senkou) span A");
				AddPlot(Brushes.Red,		"Leading (Senkou) span B");
				AddPlot(Brushes.Green,		"Lagging (Chikou)");
			}
			else if (State == State.DataLoaded)
			{
				baseLine			= new Series<double>(this);
				conversionLine		= new Series<double>(this);
				laggingLine			= new Series<double>(this);
				leadingSpanA		= new Series<double>(this);
				leadingSpanB		= new Series<double>(this);

				Displacement		= Math.Abs(SpanDisplacement);
				normalDisplacement	= Math.Abs(SpanDisplacement);
				totalLag			= Displacement + LaggingDisplacement;
			}
			else if (State == State.Terminated)
			{
				bullishBrushDx?.Dispose();
				bearishBrushDx?.Dispose();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar <= Math.Max(Math.Max(ConversionPeriod, BasePeriod), LeadingSpanBPeriod) + totalLag)
				return;

			baseLine[0]			= (MAX(High, BasePeriod)[0] + MIN(Low, BasePeriod)[0]) / 2;
			conversionLine[0]	= (MAX(High, ConversionPeriod)[0] + MIN(Low, ConversionPeriod)[0]) / 2;
			laggingLine[0]		= Close[0];
			leadingSpanA[0]		= (conversionLine[0] + baseLine[0]) / 2;
			leadingSpanB[0]		= (MAX(High, LeadingSpanBPeriod)[0] + MIN(Low, LeadingSpanBPeriod)[0]) / 2;

			Values[0][normalDisplacement]	= conversionLine[0];
			Values[1][normalDisplacement]	= baseLine[0];
			Values[2][0]					= leadingSpanA[0];
			Values[3][0]					= leadingSpanB[0];
			Values[4][totalLag]				= laggingLine[0];
		}

		protected override Point[] OnGetSelectionPoints(ChartControl chartControl, ChartScale chartScale) => !IsSelected ? Array.Empty<Point>() : selectionPoints.ToArray();

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (ChartBars == null || ChartBars.Count < 2 || Values[2].Count == 0 || 
				(CurrentBar <= Math.Max(Math.Max(ConversionPeriod, BasePeriod), LeadingSpanBPeriod) + totalLag))
				return;

			int displacement	= Math.Abs(SpanDisplacement);
			int firstPainted	= ChartBars.FromIndex;
			int lastPainted		= ChartBars.ToIndex + displacement;
			if (firstPainted < 0 || lastPainted < 0)
				return;

			List<float>	spanA		= new ();
			List<float>	spanB		= new ();
			List<int>	barList		= new ();

			// We only need to render the cloud for bars that are currently visible on the chart
			for (int barIdx = firstPainted; barIdx <= lastPainted; barIdx++)
			{
				int sourceIdx = barIdx - displacement;
				if (sourceIdx < 0 || sourceIdx >= CurrentBar)
					continue;

				double a = Values[2].GetValueAt(sourceIdx);
				double b = Values[3].GetValueAt(sourceIdx);
				if (double.IsNaN(a) || double.IsNaN(b))
					continue;

				spanA.Add((float)a);
				spanB.Add((float)b);
				barList.Add(barIdx);
			}

			if (spanA.Count < 2)
				return;

			void DrawSection(int startIdx, int endIdx, bool bullish)
			{
				if (endIdx <= startIdx) return;

				using SharpDX.Direct2D1.PathGeometry geo = new(Core.Globals.D2DFactory);
				using GeometrySink sink = geo.Open();

				int x0 = chartControl.GetXByBarIndex(ChartBars, barList[startIdx]);
				int y0 = chartScale.GetYByValue(spanA[startIdx]);
				sink.BeginFigure(new Point(x0, y0).ToVector2(), FigureBegin.Filled);
				selectionPoints.Add(new Point(x0, y0));

				for (int i = startIdx + 1; i <= endIdx; i++)
				{
					int x = chartControl.GetXByBarIndex(ChartBars, barList[i]);
					int y = chartScale.GetYByValue(spanA[i]);
					sink.AddLine(new Point(x, y).ToVector2());
					selectionPoints.Add(new Point(x, y));
				}

				for (int i = endIdx; i >= startIdx; i--)
				{
					int x = chartControl.GetXByBarIndex(ChartBars, barList[i]);
					int y = chartScale.GetYByValue(spanB[i]);
					sink.AddLine(new Point(x, y).ToVector2());
					selectionPoints.Add(new Point(x, y));
				}

				sink.EndFigure(FigureEnd.Closed);
				sink.Close();

				RenderTarget.FillGeometry(geo, bullish ? bullishBrushDx : bearishBrushDx);
			}

			// Walk through our points and start a new section when bull/bear span flips
			bool	currentBullish	= spanA[0] >= spanB[0];
			int		segStart		= 0;
			selectionPoints			= new ();

			for (int i = 1; i < spanA.Count; i++)
			{
				bool bull = spanA[i] >= spanB[i];
				if (bull != currentBullish)
				{
					DrawSection(segStart, i - 1, currentBullish);
					segStart		= i - 1;
					currentBullish	= bull;
				}
			}

			// Draw the final section
			DrawSection(segStart, spanA.Count - 1, currentBullish);
		}

		public override void OnRenderTargetChanged()
		{
			bullishBrushDx?.Dispose();
			bearishBrushDx?.Dispose();

			if (RenderTarget != null)
			{
				bullishBrushDx = Plots[2].Brush.ToDxBrush(RenderTarget, CloudOpacity);
				bearishBrushDx = Plots[3].Brush.ToDxBrush(RenderTarget, CloudOpacity);
			}
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Conversion (Tenkan) period", Order = 1, GroupName = "Parameters")]
		public int ConversionPeriod { get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Base (Kijun) period", Order = 2, GroupName = "Parameters")]
		public int BasePeriod { get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Leading (Senkou) span B period", Order = 3, GroupName = "Parameters")]
		public int LeadingSpanBPeriod { get; set; }
		
		[Range(int.MinValue, -1), NinjaScriptProperty]
		[Display(Name = "Span displacement", Order = 4, GroupName = "Parameters")]
		public int SpanDisplacement { get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Lagging (Chikou) displacement", Order = 5, GroupName = "Parameters")]
		public int LaggingDisplacement { get; set; }		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private IchimokuCloud[] cacheIchimokuCloud;
		public IchimokuCloud IchimokuCloud(int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
		{
			return IchimokuCloud(Input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
		}

		public IchimokuCloud IchimokuCloud(ISeries<double> input, int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
		{
			if (cacheIchimokuCloud != null)
				for (int idx = 0; idx < cacheIchimokuCloud.Length; idx++)
					if (cacheIchimokuCloud[idx] != null && cacheIchimokuCloud[idx].ConversionPeriod == conversionPeriod && cacheIchimokuCloud[idx].BasePeriod == basePeriod && cacheIchimokuCloud[idx].LeadingSpanBPeriod == leadingSpanBPeriod && cacheIchimokuCloud[idx].SpanDisplacement == spanDisplacement && cacheIchimokuCloud[idx].LaggingDisplacement == laggingDisplacement && cacheIchimokuCloud[idx].EqualsInput(input))
						return cacheIchimokuCloud[idx];
			return CacheIndicator<IchimokuCloud>(new IchimokuCloud() { ConversionPeriod = conversionPeriod, BasePeriod = basePeriod, LeadingSpanBPeriod = leadingSpanBPeriod, SpanDisplacement = spanDisplacement, LaggingDisplacement = laggingDisplacement }, input, ref cacheIchimokuCloud);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.IchimokuCloud IchimokuCloud(int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
		{
			return indicator.IchimokuCloud(Input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
		}

		public Indicators.IchimokuCloud IchimokuCloud(ISeries<double> input, int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
		{
			return indicator.IchimokuCloud(input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.IchimokuCloud IchimokuCloud(int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
		{
			return indicator.IchimokuCloud(Input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
		}

		public Indicators.IchimokuCloud IchimokuCloud(ISeries<double> input, int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
		{
			return indicator.IchimokuCloud(input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
		}
	}
}

#endregion
