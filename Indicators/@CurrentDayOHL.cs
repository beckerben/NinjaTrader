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
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Plots the open, high, and low values from the session starting on the current day.
	/// </summary>
	public class CurrentDayOHL : Indicator
	{
		private DateTime			currentDate			=	Core.Globals.MinDate;
		private double				currentOpen			=	double.MinValue;
		private double				currentHigh			=	double.MinValue;
		private double				currentLow			=	double.MaxValue;
		private DateTime			lastDate			= 	Core.Globals.MinDate;
		private SessionIterator		sessionIterator;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionCurrentDayOHL;
				Name						= Custom.Resource.NinjaScriptIndicatorNameCurrentDayOHL;
				IsAutoScale					= false;
				DrawOnPricePanel			= false;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				ShowLow						= true;
				ShowHigh					= true;
				ShowOpen					= true;
				BarsRequiredToPlot			= 0;

				AddPlot(new Stroke(Brushes.Goldenrod,	DashStyleHelper.Dash, 2), PlotStyle.Square, Custom.Resource.CurrentDayOHLOpen);
				AddPlot(new Stroke(Brushes.SeaGreen,	DashStyleHelper.Dash, 2), PlotStyle.Square, Custom.Resource.CurrentDayOHLHigh);
				AddPlot(new Stroke(Brushes.Red,			DashStyleHelper.Dash, 2), PlotStyle.Square, Custom.Resource.CurrentDayOHLLow);
			}
			else if (State == State.Configure)
			{
				currentDate			= Core.Globals.MinDate;
				currentOpen			= double.MinValue;
				currentHigh			= double.MinValue;
				currentLow			= double.MaxValue;
				lastDate			= Core.Globals.MinDate;
			}
			else if (State == State.DataLoaded)
			{
				sessionIterator = new SessionIterator(Bars);
			}
			else if (State == State.Historical)
			{
				if (!Bars.BarsType.IsIntraday)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", Custom.Resource.CurrentDayOHLError, TextPosition.BottomRight);
					Log(Custom.Resource.CurrentDayOHLError, LogLevel.Error);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (!Bars.BarsType.IsIntraday) return;

			lastDate 		= currentDate;
			currentDate 	= sessionIterator.GetTradingDay(Time[0]);
			
			if (lastDate != currentDate || currentOpen <= double.MinValue)
			{
				currentOpen		= Open[0];
				currentHigh		= High[0];
				currentLow		= Low[0];
			}

			currentHigh			= Math.Max(currentHigh, High[0]);
			currentLow			= Math.Min(currentLow, Low[0]);

			if (ShowOpen)
				CurrentOpen[0] = currentOpen;

			if (ShowHigh)
				CurrentHigh[0] = currentHigh;

			if (ShowLow)
				CurrentLow[0] = currentLow;
		}

		#region Properties
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> CurrentOpen => Values[0];

		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> CurrentHigh => Values[1];

		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> CurrentLow => Values[2];

		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowHigh", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool ShowHigh { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowLow", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool ShowLow { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowOpen", GroupName = "NinjaScriptParameters", Order = 3)]
		public bool ShowOpen { get; set; }
		#endregion
		
		public override string FormatPriceMarker(double price) => Instrument.MasterInstrument.FormatPrice(price);
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CurrentDayOHL[] cacheCurrentDayOHL;
		public CurrentDayOHL CurrentDayOHL()
		{
			return CurrentDayOHL(Input);
		}

		public CurrentDayOHL CurrentDayOHL(ISeries<double> input)
		{
			if (cacheCurrentDayOHL != null)
				for (int idx = 0; idx < cacheCurrentDayOHL.Length; idx++)
					if (cacheCurrentDayOHL[idx] != null &&  cacheCurrentDayOHL[idx].EqualsInput(input))
						return cacheCurrentDayOHL[idx];
			return CacheIndicator<CurrentDayOHL>(new CurrentDayOHL(), input, ref cacheCurrentDayOHL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CurrentDayOHL CurrentDayOHL()
		{
			return indicator.CurrentDayOHL(Input);
		}

		public Indicators.CurrentDayOHL CurrentDayOHL(ISeries<double> input )
		{
			return indicator.CurrentDayOHL(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CurrentDayOHL CurrentDayOHL()
		{
			return indicator.CurrentDayOHL(Input);
		}

		public Indicators.CurrentDayOHL CurrentDayOHL(ISeries<double> input )
		{
			return indicator.CurrentDayOHL(input);
		}
	}
}

#endregion
