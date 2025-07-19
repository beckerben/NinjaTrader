//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Moving Average Envelopes
	/// </summary>
	public class MAEnvelopes : Indicator
	{
		private EMA		ema;
		private HMA		hma;
		private SMA		sma;
		private TEMA	tema;
		private TMA		tma;
		private WMA		wma;
		private int period;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMAEnvelopes;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMAEnvelopes;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				MAType						= 3;
				Period						= 14;
				EnvelopePercentage			= 1.5;

				AddPlot(Brushes.DodgerBlue,																Custom.Resource.NinjaScriptIndicatorUpper);
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dash, 1), PlotStyle.Line,	Custom.Resource.NinjaScriptIndicatorMiddle);
				AddPlot(Brushes.DodgerBlue,																Custom.Resource.NinjaScriptIndicatorLower);
			}
			else if (State == State.DataLoaded)
			{
				ema		= EMA(Inputs[0], Period);
				hma		= HMA(Inputs[0], Math.Max(2, Period));
				sma		= SMA(Inputs[0], Period);
				tma		= TMA(Inputs[0], Period);
				tema	= TEMA(Inputs[0], Period);
				wma		= WMA(Inputs[0], Period);
			}
		}

		protected override void OnBarUpdate()
		{
			double maValue = 0;

			switch (MAType)
			{
				case 1:
				{
					Middle[0] = maValue = ema[0];
					break;
				}
				case 2:
				{
					Middle[0] = maValue = hma[0];
					break;
				}
				case 3:
				{
					Middle[0] = maValue = sma[0];
					break;
				}
				case 4:
				{
					Middle[0] = maValue = tma[0];
					break;
				}
				case 5:
				{
					Middle[0] = maValue = tema[0];
					break;
				}
				case 6:
				{
					Middle[0] = maValue = wma[0];
					break;
				}
			}

			Upper[0] = maValue + maValue * EnvelopePercentage / 100;
			Lower[0] = maValue - maValue * EnvelopePercentage / 100;
		}

		#region Properties

		[Range(0.01, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "EnvelopePercentage", GroupName = "NinjaScriptParameters", Order = 0)]
		public double EnvelopePercentage { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lower => Values[2];

		[Range(1, 6), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "MAType", GroupName = "NinjaScriptParameters", Order = 1)]
		[RefreshProperties(RefreshProperties.All)]
		[TypeConverter(typeof(MovingAverageEnumConverter))] // Converts the int to string values
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
		public int MAType { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Middle => Values[1];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Period
		{
			get => MAType != 2 ? period : Math.Max(2, period);
			set => period = MAType != 2 ? value : Math.Max(2, value);
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Upper => Values[0];

		#endregion
	}

	#region MovingAverageEnumConverter
	public class MovingAverageEnumConverter : TypeConverter
	{
		// Set the values to appear in the combo box
		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			List<string> values = new() { "EMA", "HMA", "SMA", "TMA", "TEMA", "WMA" };

			return new StandardValuesCollection(values);
		}

		// map the value from string to int type
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			int mATypeValue = 3;

			switch (value?.ToString())
			{
				case "EMA":		mATypeValue = 1; break;
				case "HMA":		mATypeValue = 2; break;
				case "SMA":		mATypeValue = 3; break;
				case "TMA":		mATypeValue = 4; break;
				case "TEMA":	mATypeValue = 5; break;
				case "WMA":		mATypeValue = 6; break;
			}
			return mATypeValue;
		}

		// map the int type to string
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			string mATypeString = "SMA";

			switch (value.ToString())
			{
				case "1": mATypeString = "EMA"; break;
				case "2": mATypeString = "HMA"; break;
				case "3": mATypeString = "SMA"; break;
				case "4": mATypeString = "TMA"; break;
				case "5": mATypeString = "TEMA"; break;
				case "6": mATypeString = "WMA"; break;
			}
			return mATypeString;
		}

		// required interface members needed to compile
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{ return true; }

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{ return true; }

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{ return true; }

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{ return true; }
	}
	#endregion
}
#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MAEnvelopes[] cacheMAEnvelopes;
		public MAEnvelopes MAEnvelopes(double envelopePercentage, int mAType, int period)
		{
			return MAEnvelopes(Input, envelopePercentage, mAType, period);
		}

		public MAEnvelopes MAEnvelopes(ISeries<double> input, double envelopePercentage, int mAType, int period)
		{
			if (cacheMAEnvelopes != null)
				for (int idx = 0; idx < cacheMAEnvelopes.Length; idx++)
					if (cacheMAEnvelopes[idx] != null && cacheMAEnvelopes[idx].EnvelopePercentage == envelopePercentage && cacheMAEnvelopes[idx].MAType == mAType && cacheMAEnvelopes[idx].Period == period && cacheMAEnvelopes[idx].EqualsInput(input))
						return cacheMAEnvelopes[idx];
			return CacheIndicator<MAEnvelopes>(new MAEnvelopes(){ EnvelopePercentage = envelopePercentage, MAType = mAType, Period = period }, input, ref cacheMAEnvelopes);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MAEnvelopes MAEnvelopes(double envelopePercentage, int mAType, int period)
		{
			return indicator.MAEnvelopes(Input, envelopePercentage, mAType, period);
		}

		public Indicators.MAEnvelopes MAEnvelopes(ISeries<double> input , double envelopePercentage, int mAType, int period)
		{
			return indicator.MAEnvelopes(input, envelopePercentage, mAType, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MAEnvelopes MAEnvelopes(double envelopePercentage, int mAType, int period)
		{
			return indicator.MAEnvelopes(Input, envelopePercentage, mAType, period);
		}

		public Indicators.MAEnvelopes MAEnvelopes(ISeries<double> input , double envelopePercentage, int mAType, int period)
		{
			return indicator.MAEnvelopes(input, envelopePercentage, mAType, period);
		}
	}
}

#endregion
