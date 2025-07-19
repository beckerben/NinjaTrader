//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.DirectWrite;

#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Commitment of traders indicator
	/// </summary>
	[TypeConverter("NinjaTrader.NinjaScript.Indicators.COTTypeConverter")]
	public class COT : Indicator
	{
		private		bool					backCalculated;
		private		CotReport[]				reports;
		private		TextFormat				textFormat;
		private		Gui.Tools.SimpleFont	font;

		private Vector2 GetPosition(TextLayout textLayout, int pos) =>
			LegendLocation switch
			{
				LegendLocation.TopLeft		=> new Vector2(8, ChartPanel.Y + 12 + (pos + 1) * textLayout.Metrics.Height),
				LegendLocation.TopRight		=> new Vector2(ChartPanel.W - 8 - textLayout.Metrics.Width, ChartPanel.Y + 12 + (pos + 1) * textLayout.Metrics.Height),
				LegendLocation.BottomLeft	=> new Vector2(8, ChartPanel.Y + (float)ChartPanel.H - (Number + 1 - pos) * textLayout.Metrics.Height),
				LegendLocation.BottomRight	=> new Vector2(ChartPanel.W - 8 - textLayout.Metrics.Width, ChartPanel.Y + (float)ChartPanel.H - (Number + 1 - pos) * textLayout.Metrics.Height),
				_							=> new Vector2(-1, -1)
			};

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionCOT;
				Name						= Custom.Resource.NinjaScriptIndicatorNameCOT;
				IsSuspendedWhileInactive	= true;
				Number						= 3;
				LegendLocation				= LegendLocation.TopLeft;

				CotReport1					= new CotReport { ReportType = CotReportType.Futures, Field = CotReportField.NoncommercialNet };
				CotReport2					= new CotReport { ReportType = CotReportType.Futures, Field = CotReportField.CommercialNet };
				CotReport3					= new CotReport { ReportType = CotReportType.Futures, Field = CotReportField.NonreportablePositionsNet };
				CotReport4					= new CotReport { ReportType = CotReportType.Futures, Field = CotReportField.OpenInterest };
				CotReport5					= new CotReport { ReportType = CotReportType.Futures, Field = CotReportField.TotalNet };

				AddPlot(Brushes.CornflowerBlue,	Custom.Resource.COT1);
				AddPlot(Brushes.Red,				Custom.Resource.COT2);
				AddPlot(Brushes.LimeGreen,		Custom.Resource.COT3);
				AddPlot(Brushes.Goldenrod,		Custom.Resource.COT4);
				AddPlot(Brushes.BlueViolet,		Custom.Resource.COT5);
			}
			else if (State == State.Configure)
			{
				reports				= new[] { CotReport1, CotReport2, CotReport3, CotReport4, CotReport5 };
				BarsRequiredToPlot	= 0;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CotData.GetCotReportNames(Instrument.MasterInstrument.Name).Count == 0)
			{
				Draw.TextFixed(this, "Error", Custom.Resource.CotDataError, TextPosition.BottomRight);
				return;
			}

			if (!Core.Globals.MarketDataOptions.DownloadCotData)
				Draw.TextFixed(this, "Warning", Custom.Resource.CotDataWarning, TextPosition.BottomRight);

			if (CotData.IsDownloadingData)
			{
				Draw.TextFixed(this, "Warning", Custom.Resource.CotDataStillDownloading, TextPosition.BottomRight);
				return;
			}

			for (int i = 0; i < Number; i++)
			{
				if (!backCalculated && CurrentBar > 0)
				{
					for (int j = CurrentBar - 1; j >= 0; j--)
					{
						double value1 = reports[i].Calculate(Instrument.MasterInstrument.Name, Time[j]);
						if (!double.IsNaN(value1)) // returns NaN if Instrument/Report combination is not valid.
							Values[i][j] = value1;
					}
				}
				double value = reports[i].Calculate(Instrument.MasterInstrument.Name, Time[0]);
				if (!double.IsNaN(value)) // returns NaN if Instrument/Report combination is not valid.
					Values[i][0] = value;
			}
			backCalculated = true;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (!backCalculated || LegendLocation == LegendLocation.Disabled)
				return;

			if (font == null || !font.Equals(chartControl.Properties.LabelFont) || textFormat == null || textFormat.IsDisposed)
			{
				if (textFormat is { IsDisposed: false })
					textFormat.Dispose();
				font		= chartControl.Properties.LabelFont;
				textFormat	= font.ToDirectWriteTextFormat();
			}

			for (int i = 0; i < Number; i++)
			{
				TextLayout d = new(Core.Globals.DirectWriteFactory, Core.Globals.ToLocalizedObject(reports[i].Field, Core.Globals.GeneralOptions.CurrentUICulture), textFormat, ChartPanel.W, textFormat.FontSize);
				RenderTarget.DrawTextLayout(GetPosition(d, i), d, Plots[i].BrushDX);
				d.Dispose();
			}
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Cot1 => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Cot2 => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Cot3 => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Cot4 => Values[3];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Cot5 => Values[4];

		[Range(1, 5)]
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumberOfCotPlots", GroupName = "NinjaScriptParameters", Order = 0)]
		[TypeConverter(typeof(RangeEnumConverter))]
		[RefreshProperties(RefreshProperties.All)]
		public int Number { get; set; }

		//[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "COT1", GroupName = "NinjaScriptParameters", Order = 1)]
		[XmlIgnore]
		public CotReport CotReport1 {  get; set; }

		//[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "COT2", GroupName = "NinjaScriptParameters", Order = 2)]
		[XmlIgnore]
		public CotReport CotReport2 {  get; set; }

		//[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "COT3", GroupName = "NinjaScriptParameters", Order = 3)]
		[XmlIgnore]
		public CotReport CotReport3 {  get; set; }

		//[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "COT4", GroupName = "NinjaScriptParameters", Order = 4)]
		[XmlIgnore]
		public CotReport CotReport4 {  get; set; }

		//[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "COT5", GroupName = "NinjaScriptParameters", Order = 5)]
		[XmlIgnore]
		public CotReport CotReport5 {  get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "LegendLocation", GroupName = "NinjaScriptParameters", Order = 6)]
		public LegendLocation LegendLocation { get; set; }

		[Browsable(false)]
		public int Cot1Serialize
		{
			get => (int) CotReport1.ReportType * 100 + (int) CotReport1.Field;
			set => CotReport1 = new CotReport { ReportType = (CotReportType) (value / 100), Field = (CotReportField) (value % 100) };
		}

		[Browsable(false)]
		public int Cot2Serialize
		{
			get => (int) CotReport2.ReportType * 100 + (int) CotReport2.Field;
			set => CotReport2 = new CotReport { ReportType = (CotReportType) (value / 100), Field = (CotReportField) (value % 100) };
		}

		[Browsable(false)]
		public int Cot3Serialize
		{
			get => (int) CotReport3.ReportType * 100 + (int) CotReport3.Field;
			set => CotReport3 = new CotReport { ReportType = (CotReportType) (value / 100), Field = (CotReportField) (value % 100) };
		}

		[Browsable(false)]
		public int Cot4Serialize
		{
			get => (int) CotReport4.ReportType * 100 + (int) CotReport4.Field;
			set => CotReport4 = new CotReport { ReportType = (CotReportType) (value / 100), Field = (CotReportField) (value % 100) };
		}

		[Browsable(false)]
		public int Cot5Serialize
		{
			get => (int) CotReport5.ReportType * 100 + (int) CotReport5.Field;
			set => CotReport5 = new CotReport { ReportType = (CotReportType) (value / 100), Field = (CotReportField) (value % 100) };
		}

		#endregion
	}

	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum LegendLocation
	{
		Disabled,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public class COTTypeConverter : IndicatorBaseConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			PropertyDescriptorCollection propertyDescriptorCollection = base.GetPropertiesSupported(context) ? base.GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes);

			COT	thisCotInstance			= (COT) value;
			int	number					= thisCotInstance.Number;
			if (number == 5)
				return propertyDescriptorCollection;

			PropertyDescriptorCollection	adjusted	= new(null);
			List<string>					propsToSkip	= new();

			for (int i = number + 1; i <= 5; i++)
			{
				propsToSkip.Add("CotReport" + i);
				propsToSkip.Add("Plot" + (i - 1));
			}

			if (propertyDescriptorCollection != null)
				foreach (PropertyDescriptor thisDescriptor in propertyDescriptorCollection)
				{
					adjusted.Add(propsToSkip.Contains(thisDescriptor.Name)
						? new PropertyDescriptorExtended(thisDescriptor, _ => value, null, new Attribute[] { new BrowsableAttribute(false) })
						: thisDescriptor);
				}

			return adjusted;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private COT[] cacheCOT;
		public COT COT(int number)
		{
			return COT(Input, number);
		}

		public COT COT(ISeries<double> input, int number)
		{
			if (cacheCOT != null)
				for (int idx = 0; idx < cacheCOT.Length; idx++)
					if (cacheCOT[idx] != null && cacheCOT[idx].Number == number && cacheCOT[idx].EqualsInput(input))
						return cacheCOT[idx];
			return CacheIndicator<COT>(new COT() { Number = number }, input, ref cacheCOT);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.COT COT(int number)
		{
			return indicator.COT(Input, number);
		}

		public Indicators.COT COT(ISeries<double> input, int number)
		{
			return indicator.COT(input, number);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.COT COT(int number)
		{
			return indicator.COT(Input, number);
		}

		public Indicators.COT COT(ISeries<double> input, int number)
		{
			return indicator.COT(input, number);
		}
	}
}

#endregion
