//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Data;
using NinjaTrader.Core.FloatingPoint;

#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class Correlation : Indicator
	{
		private		double				avg0;
		private		double				avg1;
		private		SessionIterator		sessionIterator;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= Custom.Resource.NinjaScriptIndicatorDescriptionCorrelation;
				Name				= Custom.Resource.NinjaScriptIndicatorNameCorrelation;
				Calculate			= Calculate.OnBarClose;
				IsOverlay			= false;
				Period				= 10;
				CorrelationSeries	= string.Empty;

				AddPlot(new Stroke(Brushes.Goldenrod, 1), PlotStyle.Line, Custom.Resource.NinjaScriptIndicatorNameCorrelation);
			}
			else if (State == State.Configure && !string.IsNullOrWhiteSpace(CorrelationSeries))
				AddDataSeries(CorrelationSeries);
		}

		private SessionIterator SessionIterator
		{
			get
			{
				if (sessionIterator == null && BarsArray.Length == 2 && BarsArray[1] != null)
					sessionIterator = new SessionIterator(BarsArray[1]);
				return sessionIterator;
			}
		}
		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				avg1 = SMA(BarsArray[1], Period)[0];
				return;
			}

			if (SessionIterator == null || CurrentBars[0] < Period || CurrentBars[1] < Period)
				return;

			if (Bars.BarsType.IsIntraday && !SessionIterator.IsInSession(Times[0][0], true, true))
				return;

			avg0 = SMA(BarsArray[0], Period)[0];

			double nominator	= 0;
			double denominator1 = 0;
			double denominator2 = 0;

			for (int i = 0; i < Period; i++)
			{
				nominator		+= (avg0 - Inputs[0][i]) * (avg1 - Inputs[1][i]);
				denominator1	+= (avg0 - Inputs[0][i]) * (avg0 - Inputs[0][i]);
				denominator2	+= (avg1 - Inputs[1][i]) * (avg1 - Inputs[1][i]);
			}

			double denominator	= Math.Sqrt(denominator1) * Math.Sqrt(denominator2);

			Value[0]			= denominator.ApproxCompare(0) == 0 ? 0 : nominator / denominator;
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptIndicatorCount", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptMarketAnalyzerColumnNameInstrument", GroupName = "NinjaScriptParameters", Order = 0)]
		[PropertyEditor("NinjaTrader.Gui.Tools.UppercaseTextEditor")]
		public string CorrelationSeries { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Correlation[] cacheCorrelation;
		public Correlation Correlation(int period, string correlationSeries)
		{
			return Correlation(Input, period, correlationSeries);
		}

		public Correlation Correlation(ISeries<double> input, int period, string correlationSeries)
		{
			if (cacheCorrelation != null)
				for (int idx = 0; idx < cacheCorrelation.Length; idx++)
					if (cacheCorrelation[idx] != null && cacheCorrelation[idx].Period == period && cacheCorrelation[idx].CorrelationSeries == correlationSeries && cacheCorrelation[idx].EqualsInput(input))
						return cacheCorrelation[idx];
			return CacheIndicator<Correlation>(new Correlation(){ Period = period, CorrelationSeries = correlationSeries }, input, ref cacheCorrelation);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Correlation Correlation(int period, string correlationSeries)
		{
			return indicator.Correlation(Input, period, correlationSeries);
		}

		public Indicators.Correlation Correlation(ISeries<double> input , int period, string correlationSeries)
		{
			return indicator.Correlation(input, period, correlationSeries);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Correlation Correlation(int period, string correlationSeries)
		{
			return indicator.Correlation(Input, period, correlationSeries);
		}

		public Indicators.Correlation Correlation(ISeries<double> input , int period, string correlationSeries)
		{
			return indicator.Correlation(input, period, correlationSeries);
		}
	}
}

#endregion
