//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Globalization;
using System.Windows;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	[TypeConverter("NinjaTrader.NinjaScript.Indicators.FVGTypeConverter")]
	public class FVG : Indicator
	{
		private class Gap
		{
			public int						BarIndex	{ get; }
			public bool						IsUp		{ get; }
			public Dictionary<double, int>	Ticks		{ get; } = new();

			public Gap(Cbi.Instrument instrument, int barIndex, double barsAgoPrice, double currentBarPrice, int barsToKeep = 0)
			{
				BarIndex	= barIndex;
				IsUp		= barsAgoPrice < currentBarPrice;

				if (IsUp)
					for (double p = barsAgoPrice; p <= currentBarPrice; p += instrument.MasterInstrument.TickSize)
					{
						p = instrument.MasterInstrument.RoundToTickSize(p);
						Ticks.Add(p, barsToKeep == 0 ? -1 : BarIndex + barsToKeep);
					}
				else
					for (double p = barsAgoPrice; p >= currentBarPrice; p -= instrument.MasterInstrument.TickSize)
					{
						p = instrument.MasterInstrument.RoundToTickSize(p);
						Ticks.Add(p, barsToKeep == 0 ? -1 : BarIndex + barsToKeep);
					}
			}

			public void Check(Cbi.Instrument instrument, double minPrice, double maxPrice, int barIndex, bool terminateAll)
			{
				if (barIndex < BarIndex + 2)
					return;

				for (double p = minPrice; p <= maxPrice; p += instrument.MasterInstrument.TickSize)
				{
					p = instrument.MasterInstrument.RoundToTickSize(p);
					if (Ticks.ContainsKey(p) && Ticks[p] == -1)
					{
						if (terminateAll)
						{
							foreach (double key in Ticks.Keys.ToList())
								Ticks[key] = barIndex;
							break;
						}

						Ticks[p] = Math.Max(barIndex, Ticks[p]);
					}
				}
			}

		}

		private	readonly	Dictionary<int, Gap>	gaps	= new();

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.FVGDescription;
				Name						= Custom.Resource.FVGName;
				ExtendUntil					= 1;
				MaxFvg						= 10;
				MinimumTicks				= 1;
				BarsToExtend				= 10;
				FvgColorDown				= Brushes.Red;
				FvgColorUp					= Brushes.LimeGreen;
				IsOverlay					= true;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= true;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
			}
			else if (State == State.Configure)
				ZOrder = -1;
		}

		protected override Point[] OnGetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			if (!IsSelected || gaps.Count == 0)
				return Array.Empty<Point>();

			List<Point> points = new();

			foreach (Gap g in gaps.Values)
			{
				int x1 		= chartControl.GetXByTime(ChartBars.GetTimeByBarIdx(chartControl, g.BarIndex + Displacement));
				int endIdx 	= g.Ticks[g.Ticks.Keys.Min()] > 0 ? g.Ticks[g.Ticks.Keys.Min()] + Displacement : ChartBars.ToIndex;
				int x2 		= chartControl.GetXByTime(ChartBars.GetTimeByBarIdx(chartControl, endIdx));
				int y 		= chartScale.GetYByValue(g.Ticks.Keys.Min());
				points.Add(new Point(x1, y));points.Add(new Point(x2, y));

				if (Math.Abs(g.Ticks.Keys.Min() - g.Ticks.Keys.Max()) > TickSize * 0.1)
				{
					y 		= chartScale.GetYByValue(g.Ticks.Keys.Max());
					points.Add(new Point(x1, y));points.Add(new Point(x2, y));
				}
			}

			return points.ToArray();
		}

		protected override void OnBarUpdate()
		{
			foreach (Gap gap in gaps.Values)
				gap.Check(Instrument, Low[0], High[0], CurrentBar, ExtendUntil == 2);

			if (CurrentBar < 3)
				return;

			int upGap = (int)Math.Round((Low[0] - High[2]) / TickSize);
			int dnGap = (int)Math.Round((Low[2] - High[0]) / TickSize);

			Gap g = null;
			if (upGap >= MinimumTicks)
				g = new Gap(Instrument, CurrentBar - 1, High[2], Low[0], ExtendUntil == 3 ? BarsToExtend : 0);
			else if (dnGap >= MinimumTicks) 
				g = new Gap(Instrument, CurrentBar - 1, Low[2], High[0], ExtendUntil == 3 ? BarsToExtend : 0);
			
			if (g != null)
			{
				if (gaps.ContainsKey(CurrentBar - 1))
					gaps[CurrentBar - 1] = g;
				else
					gaps.Add(CurrentBar - 1, g);
			}
			else
				gaps.Remove(CurrentBar - 1);

			while (gaps.Count > MaxFvg)
				gaps.Remove(gaps.Keys.Min());
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			foreach (Gap gap in gaps.Values)
			{
				foreach (KeyValuePair<double, int> kvp in gap.Ticks)
				{
					double y = chartScale.GetYByValue(kvp.Key + TickSize * 0.5);
					double x = chartControl.GetXByBarIndex(ChartBars, gap.BarIndex + Displacement);
					double w = chartControl.GetXByBarIndex(ChartBars, kvp.Value > 0 ? kvp.Value  + Displacement : ChartBars.ToIndex) - x;
					double h = chartScale.GetYByValue(kvp.Key - TickSize * 0.5) - y;

					RenderTarget.FillRectangle(new SharpDX.RectangleF((float)x, (float)y, (float)w, (float)h),
						gap.IsUp
							? FvgColorUp.ToDxBrush(RenderTarget, Opacity / 100f)
							: FvgColorDown.ToDxBrush(RenderTarget, Opacity / 100f));
				}
			}
		}

		#region Properties
		[Range(1, 3)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "FVGExtendUntil", GroupName = "NinjaScriptParameters", Order = 40)]
		[RefreshProperties(RefreshProperties.All)]
		[TypeConverter(typeof(FVGEnumConverter))] // Converts the int to string values
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
		public int ExtendUntil { get; set; }

		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "FVGMaxFVG", GroupName = "NinjaScriptParameters", Order = 70)]
		public int MaxFvg { get; set; }

		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "FVGMinimumTicks", GroupName = "NinjaScriptParameters", Order = 60)]
		public int MinimumTicks { get; set; } = 10;

		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "FVGBarsToExtend", GroupName = "NinjaScriptParameters", Order = 50)]
		public int BarsToExtend { get; set; } = 10;

		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Opacity", GroupName = "NinjaScriptParameters", Order = 30)]
		public int Opacity { get; set; } = 40;

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptChartStylePointAndFigureUpColor", GroupName = "NinjaScriptParameters", Order = 10)]
		public Brush FvgColorUp { get; set; }

		[Browsable(false)]
		public string FvgColorUpSerializable
		{
			get => Serialize.BrushToString(FvgColorUp);
			set => FvgColorUp = Serialize.StringToBrush(value);
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptChartStylePointAndFigureDownColor", GroupName = "NinjaScriptParameters", Order = 20)]
		public Brush FvgColorDown { get; set; }

		[Browsable(false)]
		public string FvgColorDownSerializable
		{
			get => Serialize.BrushToString(FvgColorDown);
			set => FvgColorDown = Serialize.StringToBrush(value);
		}
		#endregion
	}

	#region TypeConverters
	public class FVGEnumConverter : TypeConverter
	{
		// Set the values to appear in the combo box
		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			List<string> values = new() { Custom.Resource.FVGFilled, Custom.Resource.FVGPartiallyFilled, Custom.Resource.FVGBarsSpecified };

			return new StandardValuesCollection(values);
		}

		// map the value from string to int type
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			return value?.ToString() switch
			{
				{ } stringValue when stringValue == Custom.Resource.FVGFilled			=> 1,
				{ } stringValue when stringValue == Custom.Resource.FVGPartiallyFilled	=> 2,
				_																		=> 3
			};
		}

		// map the int type to string
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			return value.ToString() switch
			{
				"1"		=> Custom.Resource.FVGFilled,
				"2"		=> Custom.Resource.FVGPartiallyFilled,
				_		=> Custom.Resource.FVGBarsSpecified
			};
		}

		// required interface members needed to compile
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
	}

	public class FVGTypeConverter : IndicatorBaseConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			PropertyDescriptorCollection propertyDescriptorCollection = base.GetPropertiesSupported(context) ? base.GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes);

			FVG	fvgInstance	= (FVG) value;
			int	extendUntil	= fvgInstance.ExtendUntil;
			if (extendUntil == 3)
				return propertyDescriptorCollection;

			PropertyDescriptorCollection adjusted = new(null);
			if (propertyDescriptorCollection != null)
				foreach (PropertyDescriptor thisDescriptor in propertyDescriptorCollection)
				{
					adjusted.Add(thisDescriptor.Name is nameof(fvgInstance.BarsToExtend)
						? new PropertyDescriptorExtended(thisDescriptor, _ => value, null,
							new Attribute[] { new BrowsableAttribute(false) })
						: thisDescriptor);
				}

			return adjusted;
		}
	}

	#endregion
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FVG[] cacheFVG;
		public FVG FVG()
		{
			return FVG(Input);
		}

		public FVG FVG(ISeries<double> input)
		{
			if (cacheFVG != null)
				for (int idx = 0; idx < cacheFVG.Length; idx++)
					if (cacheFVG[idx] != null &&  cacheFVG[idx].EqualsInput(input))
						return cacheFVG[idx];
			return CacheIndicator<FVG>(new FVG(), input, ref cacheFVG);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FVG FVG()
		{
			return indicator.FVG(Input);
		}

		public Indicators.FVG FVG(ISeries<double> input )
		{
			return indicator.FVG(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FVG FVG()
		{
			return indicator.FVG(Input);
		}

		public Indicators.FVG FVG(ISeries<double> input )
		{
			return indicator.FVG(input);
		}
	}
}

#endregion
