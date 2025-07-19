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
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The MAMA (MESA Adaptive Moving Average) was developed by John Ehlers.
	/// It adapts to price movement in a new and unique way. The adaptation is
	/// based on the Hilbert Transform Discriminator. The adavantage of this method
	/// features fast attack average and a slow decay average. The MAMA + the FAMA
	/// (Following Adaptive Moving Average) lines only cross at major market reversals.
	/// </summary>
	public class MAMA : Indicator
	{
		private Series<double>		detrender;
		private Series<double>		i1;
		private Series<double>		i2;
		private Series<double>		im;
		private Series<double>		period;
		private Series<double>		phase;
		private Series<double>		q1;
		private Series<double>		q2;
		private Series<double>		re;
		private Series<double>		smooth;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMAMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMAMA;
				FastLimit					= 0.5;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				SlowLimit					= 0.05;

				AddPlot(Brushes.Goldenrod,	Custom.Resource.NinjaScriptIndicatorDefault);
				AddPlot(Brushes.DodgerBlue,	Custom.Resource.MAMAFAMA);
			}
			else if (State == State.DataLoaded)
			{
				detrender	= new Series<double>(this);
				period		= new Series<double>(this);
				smooth		= new Series<double>(this);
				i1			= new Series<double>(this);
				i2			= new Series<double>(this);
				im			= new Series<double>(this);
				q1			= new Series<double>(this);
				q2			= new Series<double>(this);
				re			= new Series<double>(this);
				phase		= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 6)
				return;

			if (CurrentBar == 6)
			{
				Default[0]	= (High[0] + Low[0]) / 2;
				Fama[0]		= Input[0];
				return;
			}

			double period1	= period[1];
			smooth[0]		= (4 * Median[0] + 3 * Median[1] + 2 * Median[2] + Median[3]) / 10;
			detrender[0]	= (0.0962 * smooth[0] + 0.5769 * smooth[2]
								- 0.5769 * smooth[4] - 0.0962 * smooth[6]) * (0.075 * period1 + 0.54);

			// Compute InPhase and Quadrature components
			q1[0]			= (0.0962 * detrender[0] + 0.5769 * detrender[2]
								- 0.5769 * detrender[4] - 0.0962 * detrender[6]) * (0.075 * period1 + 0.54);
			i1[0]			= detrender[3];
			double i10		= i1[0];

			// Advance the phase of i1 and q1 by 90
			double jI		= (0.0962 * i10 + 0.5769 * i1[2] - 0.5769 * i1[4] - 0.0962 * i1[6]) * (0.075 * period1 + 0.54);
			double jQ		= (0.0962 * q1[0] + 0.5769 * q1[2] - 0.5769 * q1[4] - 0.0962 * q1[6]) * (0.075 * period1 + 0.54);

			// Phasor addition for 3 bar averaging
			i2[0]			= i10 - jQ;
			q2[0]			= q1[0] + jI;

			// Smooth the I and Q components before applying the discriminator
			i2[0]			= 0.2 * i2[0] + 0.8 * i2[1];
			q2[0]			= 0.2 * q2[0] + 0.8 * q2[1];

			double i20		= i2[0];
			double q21		= q2[1];
			double i21		= i2[1];
			double q20		= q2[0];
			double period0	= period[0];

			// Homodyne Discriminator
			re[0]			= i20 * i21 + q20 * q21;
			im[0]			= i20 * q21 - q20 * i21;
			re[0]			= 0.2 * re[0] + 0.8 * re[1];
			im[0]			= 0.2 * im[0] + 0.8 * im[1];

			if (im[0] != 0.0 && re[0] != 0.0)	period0 = 360 / (180 / Math.PI * Math.Atan(im[0] / re[0]));
			if (period0 > 1.5  * period1)		period0 = 1.5  * period1;
			if (period0 < 0.67 * period1)		period0 = 0.67 * period1;
			if (period0 < 6)					period0 = 6;
			if (period0 > 50)					period0 = 50;

			period[0] = 0.2 * period0 + 0.8 * period1;

			if (i1[0] != 0.0) phase[0] = 180 / Math.PI * Math.Atan(q1[0] / i1[0]);

			double deltaPhase = phase[1] - phase[0];
			if (deltaPhase < 1)	deltaPhase = 1;

			double alpha = FastLimit / deltaPhase;
			if (alpha < SlowLimit) alpha = SlowLimit;

			// MAMA
			Default[0]	= alpha * Median[0] + (1 - alpha) * Default[1];
			// FAMA
			Fama[0]		= 0.5 * alpha * Default[0] + (1 - 0.5 * alpha) * Fama[1];
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Default => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Fama => Values[1];

		[Range(0.05, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "FastLimit", GroupName = "NinjaScriptParameters", Order = 0)]
		public double FastLimit { get; set; }

		[Range(0.005, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "SlowLimit", GroupName = "NinjaScriptParameters", Order = 1)]
		public double SlowLimit { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MAMA[] cacheMAMA;
		public MAMA MAMA(double fastLimit, double slowLimit)
		{
			return MAMA(Input, fastLimit, slowLimit);
		}

		public MAMA MAMA(ISeries<double> input, double fastLimit, double slowLimit)
		{
			if (cacheMAMA != null)
				for (int idx = 0; idx < cacheMAMA.Length; idx++)
					if (cacheMAMA[idx] != null && cacheMAMA[idx].FastLimit == fastLimit && cacheMAMA[idx].SlowLimit == slowLimit && cacheMAMA[idx].EqualsInput(input))
						return cacheMAMA[idx];
			return CacheIndicator<MAMA>(new MAMA(){ FastLimit = fastLimit, SlowLimit = slowLimit }, input, ref cacheMAMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MAMA MAMA(double fastLimit, double slowLimit)
		{
			return indicator.MAMA(Input, fastLimit, slowLimit);
		}

		public Indicators.MAMA MAMA(ISeries<double> input , double fastLimit, double slowLimit)
		{
			return indicator.MAMA(input, fastLimit, slowLimit);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MAMA MAMA(double fastLimit, double slowLimit)
		{
			return indicator.MAMA(Input, fastLimit, slowLimit);
		}

		public Indicators.MAMA MAMA(ISeries<double> input , double fastLimit, double slowLimit)
		{
			return indicator.MAMA(input, fastLimit, slowLimit);
		}
	}
}

#endregion
