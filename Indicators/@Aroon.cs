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
	/// The Aroon Indicator was developed by Tushar Chande. Its comprised of two plots one
	/// measuring the number of periods since the most recent x-period high (Aroon Up) and the
	/// other measuring the number of periods since the most recent x-period low (Aroon Down).
	/// </summary>
	public class Aroon : Indicator
	{
		private double		runningMax;
		private int			runningMaxBar;
		private double		runningMin;
		private int			runningMinBar;
		private int			saveCurrentBar;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionAroon;
				Name						= Custom.Resource.NinjaScriptIndicatorNameAroon;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.DarkCyan,		Custom.Resource.NinjaScriptIndicatorUp);
				AddPlot(Brushes.SlateBlue,	Custom.Resource.NinjaScriptIndicatorDown);
				AddLine(Brushes.DarkGray,	30,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.DarkGray,	70,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.Configure)
			{
				runningMax		= 0;
				runningMaxBar	= 0;
				runningMin		= 0;
				runningMinBar	= 0;
			}
		}

		protected override void OnBarUpdate()
		{
			double high0	= High[0];
			double low0		= Low[0];

			if (CurrentBar == 0)
			{
				Down[0]			= 0;
				Up[0]			= 0;
				runningMax		= high0;
				runningMin		= low0;
				runningMaxBar	= 0;
				runningMinBar	= 0;
				return;
			}

			int back = Math.Min(Period, CurrentBar);
			if (CurrentBar - runningMaxBar >= Period || CurrentBar < saveCurrentBar)
			{
				runningMax = double.MinValue;
				for (int barsBack = back; barsBack > 0; barsBack--)
					if (High[barsBack] >= runningMax)
					{
						runningMax		= High[barsBack];
						runningMaxBar	= CurrentBar - barsBack;
					}
			}

			if (CurrentBar - runningMinBar >= Period || CurrentBar < saveCurrentBar)
			{
				runningMin = double.MaxValue;
				for (int barsBack = back; barsBack > 0; barsBack--)
					if (Low[barsBack] <= runningMin)
					{
						runningMin		= Low[barsBack];
						runningMinBar	= CurrentBar - barsBack;
					}
			}

			if (high0 >= runningMax)
			{
				runningMax		= high0;
				runningMaxBar	= CurrentBar;
			}

			if (low0 <= runningMin)
			{
				runningMin		= low0;
				runningMinBar	= CurrentBar;
			}

			saveCurrentBar = CurrentBar;

			Up[0] = 100 * ((double)(back - (CurrentBar - runningMaxBar)) / back);
			Down[0] = 100 * ((double)(back - (CurrentBar - runningMinBar)) / back);
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Down => Values[1];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Up => Values[0];
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Aroon[] cacheAroon;
		public Aroon Aroon(int period)
		{
			return Aroon(Input, period);
		}

		public Aroon Aroon(ISeries<double> input, int period)
		{
			if (cacheAroon != null)
				for (int idx = 0; idx < cacheAroon.Length; idx++)
					if (cacheAroon[idx] != null && cacheAroon[idx].Period == period && cacheAroon[idx].EqualsInput(input))
						return cacheAroon[idx];
			return CacheIndicator<Aroon>(new Aroon(){ Period = period }, input, ref cacheAroon);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Aroon Aroon(int period)
		{
			return indicator.Aroon(Input, period);
		}

		public Indicators.Aroon Aroon(ISeries<double> input , int period)
		{
			return indicator.Aroon(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Aroon Aroon(int period)
		{
			return indicator.Aroon(Input, period);
		}

		public Indicators.Aroon Aroon(ISeries<double> input , int period)
		{
			return indicator.Aroon(input, period);
		}
	}
}

#endregion
