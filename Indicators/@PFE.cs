//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The PFE (Polarized Fractal Efficiency) is an indicator that uses fractal
	///  geometry to determine how efficiently the price is moving.
	/// </summary>
	public class PFE : Indicator
	{
		private Series<double>	div;
		private EMA				ema;
		private Series<double>	pfeSeries;
		private Series<double>	singlePfeSeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionPFE;
				Name						= Custom.Resource.NinjaScriptIndicatorNamePFE;
				IsSuspendedWhileInactive	= true;
				Period						= 14;
				Smooth						= 10;

				AddPlot(Brushes.DodgerBlue,	Custom.Resource.NinjaScriptIndicatorNamePFE);
				AddLine(Brushes.DarkGray,	0,	Custom.Resource.PFEZero);
			}

			else if (State == State.DataLoaded)
			{
				div				= new Series<double>(this);
				pfeSeries		= new Series<double>(this);
				singlePfeSeries	= new Series<double>(this);
				ema				= EMA(pfeSeries, Smooth);
			}
		}

		protected override void OnBarUpdate()
		{
			double input0 = Input[0];

			if (CurrentBar < Period)
			{
				singlePfeSeries[0]	= CurrentBar == 0 ? 1 : Math.Sqrt(Math.Pow(Input[1] - input0, 2) + 1);
				div[0]				= singlePfeSeries[0] + (CurrentBar > 0 ? div[1] : 0);
				return;
			}

			double input1		= Input[1];
			double inputPeriod	= Input[Period];

			singlePfeSeries[0]	= Math.Sqrt(Math.Pow(input1 - input0, 2) + 1);
			div[0]				= singlePfeSeries[0] + div[1] - singlePfeSeries[Period];
			pfeSeries[0] 		= (input0 < inputPeriod ? -1 : 1) * (Math.Sqrt(Math.Pow(input0 - inputPeriod, 2) + Math.Pow(Period, 2)) / div[0]);
			Value[0]			= ema[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Smooth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PFE[] cachePFE;
		public PFE PFE(int period, int smooth)
		{
			return PFE(Input, period, smooth);
		}

		public PFE PFE(ISeries<double> input, int period, int smooth)
		{
			if (cachePFE != null)
				for (int idx = 0; idx < cachePFE.Length; idx++)
					if (cachePFE[idx] != null && cachePFE[idx].Period == period && cachePFE[idx].Smooth == smooth && cachePFE[idx].EqualsInput(input))
						return cachePFE[idx];
			return CacheIndicator<PFE>(new PFE(){ Period = period, Smooth = smooth }, input, ref cachePFE);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PFE PFE(int period, int smooth)
		{
			return indicator.PFE(Input, period, smooth);
		}

		public Indicators.PFE PFE(ISeries<double> input , int period, int smooth)
		{
			return indicator.PFE(input, period, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PFE PFE(int period, int smooth)
		{
			return indicator.PFE(Input, period, smooth);
		}

		public Indicators.PFE PFE(ISeries<double> input , int period, int smooth)
		{
			return indicator.PFE(input, period, smooth);
		}
	}
}

#endregion
