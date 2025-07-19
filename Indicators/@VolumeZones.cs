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
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VolumeZones : Indicator
	{
		internal struct VolumeInfo
		{
			public double up;
			public double down;
			public double total;
		}

		private VolumeInfo[] volumeInfo = new VolumeInfo[20];

		private int barCount;
		private int barSpacing;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVolumeZones;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVolumesZones;
				Calculate					= Calculate.OnBarClose;
				IsChartOnly					= true;
				IsOverlay					= true;
				DisplayInDataBox			= false;
				PaintPriceMarkers			= false;
				DrawLines					= false;
				Opacity						= 50;
				BarCount					= 10;
				BarSpacing					= 1;
				BarDownBrush				= Brushes.Crimson;
				BarUpBrush					= Brushes.DarkCyan;
				LineBrush					= Brushes.DarkGray;
			}
			else if (State == State.Configure)
				ZOrder = -1;
		}

		protected override void OnBarUpdate() {}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (IsInHitTest)
				return;

			int		lastBar		= ChartBars.ToIndex;
			int		firstBar	= ChartBars.FromIndex;
			double	highPrice	= 0;
			double	lowPrice	= double.MaxValue;

			SharpDX.Direct2D1.Brush brushDown	= BarDownBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush lineBrush	= LineBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush brushUp		= BarUpBrush.ToDxBrush(RenderTarget);
			brushDown.Opacity					= (float)(Opacity / 100.0);
			brushUp.Opacity						= (float)(Opacity / 100.0);

			for (int idx = firstBar; idx <= lastBar && idx >= 0; idx++)
			{
				highPrice	= Math.Max(highPrice, Bars.GetHigh(idx));
				lowPrice	= Math.Min(lowPrice, Bars.GetLow(idx));
			}

			int		volumeBarCount	= BarCount;
			double	priceRange		= highPrice - lowPrice;
			double	priceBoxSize	= priceRange / volumeBarCount;
			double	volumeMax		= 0;

			// Pass 1: Fill all VolumeInfo structures with appropriate data
			for (int i = 0; i < volumeBarCount; i++)
			{

				double priceUpper = lowPrice + priceBoxSize * (i + 1);
				double priceLower = lowPrice + priceBoxSize * i;

				double priceVolumeUp   = 0;
				double priceVolumeDown = 0;

				for (int idx = firstBar; idx <= lastBar; idx++)
				{
					PriceSeries series = Inputs[0] as PriceSeries;

					double checkPrice = series?.PriceType switch
					{
						PriceType.Open		=> Bars.GetOpen(idx),
						PriceType.Close		=> Bars.GetClose(idx),
						PriceType.High		=> Bars.GetHigh(idx),
						PriceType.Low		=> Bars.GetLow(idx),
						PriceType.Median	=> (Bars.GetHigh(idx) + Bars.GetLow(idx)) / 2,
						PriceType.Typical	=> (Bars.GetHigh(idx) + Bars.GetLow(idx) + Bars.GetClose(idx)) / 3,
						PriceType.Weighted	=> (Bars.GetHigh(idx) + Bars.GetLow(idx) + 2 * Bars.GetClose(idx)) / 4,
						_					=> Bars.GetClose(idx)
					};

					if (checkPrice >= priceLower && checkPrice < priceUpper)
						if (Bars.GetOpen(idx) < Bars.GetClose(idx))
							priceVolumeUp += Bars.GetVolume(idx);
						else
							priceVolumeDown += Bars.GetVolume(idx);
				}

				volumeInfo[i].up	= priceVolumeUp;
				volumeInfo[i].down	= priceVolumeDown;
				volumeInfo[i].total = priceVolumeUp + priceVolumeDown;

				volumeMax = Math.Max(volumeMax, volumeInfo[i].total);
			}

			// Pass 2: Paint the volume bars
			for (int i = 0; i < Math.Min(volumeBarCount, lastBar - firstBar + 1); i++)
			{
				double	priceUpper		= lowPrice + priceBoxSize * (i + 1);
				double	priceLower		= lowPrice + priceBoxSize * i;
				int		yUpper			= Convert.ToInt32(chartScale.GetYByValue(priceUpper)) + BarSpacing;
				int		yLower			= Convert.ToInt32(chartScale.GetYByValue(priceLower));
				int		barWidthUp		= (int)(chartScale.Height / 2 * (volumeInfo[i].up / volumeMax));
				int		barWidthDown	= (int)(chartScale.Height / 2 * (volumeInfo[i].down / volumeMax));

				SharpDX.RectangleF rect = new(ChartPanel.X, yUpper, barWidthUp, Math.Abs(yUpper - yLower));
				RenderTarget.FillRectangle(rect, brushUp);
				RenderTarget.DrawRectangle(rect, brushUp);

				SharpDX.RectangleF rect2 = new(ChartPanel.X + barWidthUp, yUpper, barWidthDown, Math.Abs(yUpper - yLower));
				RenderTarget.DrawRectangle(rect2, brushDown);
				RenderTarget.FillRectangle(rect2, brushDown);

				if (DrawLines)
				{
					RenderTarget.DrawLine(new(ChartPanel.X, yLower), new(ChartPanel.X + ChartPanel.W, yLower), lineBrush);
					if( i == volumeBarCount - 1)
						RenderTarget.DrawLine(new(ChartPanel.X, yUpper), new(ChartPanel.X + ChartPanel.W, yUpper), lineBrush);
				}
			}

			lineBrush.Dispose();
			brushDown.Dispose();
			brushUp.Dispose();
		}

		#region Properties
		[Range(2, 20)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BarCount", Order = 1, GroupName = "NinjaScriptParameters")]
		public int BarCount
		{
			get => barCount;
			set
			{
				barCount = value;
				if (value > volumeInfo.Length)
					volumeInfo = new VolumeInfo[value];
			}
		}

		[Range(0, 5)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BarSpacing", Order = 2, GroupName = "NinjaScriptParameters")]
		public int BarSpacing
		{
			get => barSpacing;
			set => barSpacing = Math.Max(0, value);
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "DownBarColor", Order = 3, GroupName = "NinjaScriptParameters")]
		public Brush BarDownBrush { get; set; }

		[Browsable(false)]
		public string BarColorDownSerialize
		{
			get => Serialize.BrushToString(BarDownBrush);
			set => BarDownBrush = Serialize.StringToBrush(value);
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "DrawLines", Order = 4, GroupName = "NinjaScriptParameters")]
		public bool DrawLines { get; set; }

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "LineColor", Order = 5, GroupName = "NinjaScriptParameters")]
		public Brush LineBrush { get; set; }

		[Browsable(false)]
		public string LineBrushSerialize
		{
			get => Serialize.BrushToString(LineBrush);
			set => LineBrush = Serialize.StringToBrush(value);
		}
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "UpBarColor", Order = 6, GroupName = "NinjaScriptParameters")]
		public Brush BarUpBrush { get; set; }

		[Browsable(false)]
		public string BarColorUpSerialize
		{
			get => Serialize.BrushToString(BarUpBrush);
			set => BarUpBrush = Serialize.StringToBrush(value);
		}

		[Range(10, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Opacity", Order = 7, GroupName = "NinjaScriptParameters")]
		public double Opacity { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VolumeZones[] cacheVolumeZones;
		public VolumeZones VolumeZones()
		{
			return VolumeZones(Input);
		}

		public VolumeZones VolumeZones(ISeries<double> input)
		{
			if (cacheVolumeZones != null)
				for (int idx = 0; idx < cacheVolumeZones.Length; idx++)
					if (cacheVolumeZones[idx] != null &&  cacheVolumeZones[idx].EqualsInput(input))
						return cacheVolumeZones[idx];
			return CacheIndicator<VolumeZones>(new VolumeZones(), input, ref cacheVolumeZones);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeZones VolumeZones()
		{
			return indicator.VolumeZones(Input);
		}

		public Indicators.VolumeZones VolumeZones(ISeries<double> input )
		{
			return indicator.VolumeZones(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeZones VolumeZones()
		{
			return indicator.VolumeZones(Input);
		}

		public Indicators.VolumeZones VolumeZones(ISeries<double> input )
		{
			return indicator.VolumeZones(input);
		}
	}
}

#endregion
