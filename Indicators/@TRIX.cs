//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The TRIX (Triple Exponential Average) displays the percentage Rate of Change (ROC)
	/// of a triple EMA. Trix oscillates above and below the zero value. The indicator
	/// applies triple smoothing in an attempt to eliminate insignificant price movements
	/// within the trend that you're trying to isolate.
	/// </summary>
	public class TRIX : Indicator
	{
		private EMA	emaDefault;
		private EMA	emaTriple;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionTRIX;
				Name						= Custom.Resource.NinjaScriptIndicatorNameTRIX;
				IsSuspendedWhileInactive	= true;
				Period						= 14;
				SignalPeriod				= 3;

				AddPlot(Brushes.DimGray,		Custom.Resource.NinjaScriptIndicatorDefault);
				AddPlot(Brushes.Crimson,		Custom.Resource.TRIXSignal);
				AddLine(Brushes.DarkGray, 0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.DataLoaded)
			{
				emaTriple	= EMA(EMA(EMA(Inputs[0], Period), Period), Period);
				emaDefault	= EMA(Default, SignalPeriod);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				Value[0] = Input[0];
				return;
			}

			double emaTriple0	= emaTriple[0];
			Default[0]			= 100 * ((emaTriple0  - emaTriple[1]) / emaTriple0);
			Signal[0]			= emaDefault[0];
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Default => Values[0];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal => Values[1];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "SignalPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
		public int SignalPeriod { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TRIX[] cacheTRIX;
		public TRIX TRIX(int period, int signalPeriod)
		{
			return TRIX(Input, period, signalPeriod);
		}

		public TRIX TRIX(ISeries<double> input, int period, int signalPeriod)
		{
			if (cacheTRIX != null)
				for (int idx = 0; idx < cacheTRIX.Length; idx++)
					if (cacheTRIX[idx] != null && cacheTRIX[idx].Period == period && cacheTRIX[idx].SignalPeriod == signalPeriod && cacheTRIX[idx].EqualsInput(input))
						return cacheTRIX[idx];
			return CacheIndicator<TRIX>(new TRIX(){ Period = period, SignalPeriod = signalPeriod }, input, ref cacheTRIX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TRIX TRIX(int period, int signalPeriod)
		{
			return indicator.TRIX(Input, period, signalPeriod);
		}

		public Indicators.TRIX TRIX(ISeries<double> input , int period, int signalPeriod)
		{
			return indicator.TRIX(input, period, signalPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TRIX TRIX(int period, int signalPeriod)
		{
			return indicator.TRIX(Input, period, signalPeriod);
		}

		public Indicators.TRIX TRIX(ISeries<double> input , int period, int signalPeriod)
		{
			return indicator.TRIX(input, period, signalPeriod);
		}
	}
}

#endregion
