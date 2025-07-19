//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Dynamic Momentum Index is a variable term RSI. The RSI term varies
	///  from 3 to 30. The variable time period makes the RSI more responsive to
	/// short-term moves. The more volatile the price is, the shorter the time period is.
	///  It is interpreted in the same way as the RSI, but provides signals earlier.
	/// </summary>
	public class DMIndex : Indicator
	{
		private SMA		sma;
		private StdDev	stdDev;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionDMIndex;
				Name						= Custom.Resource.NinjaScriptIndicatorNameDMIndex;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= false;
				Smooth						= 3;

				AddPlot(Brushes.DarkCyan, Custom.Resource.NinjaScriptIndicatorNameDMIndex);
			}
			else if (State == State.DataLoaded)
			{
				stdDev		= StdDev(5);
				sma			= SMA(stdDev, 10);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				Value[0] = Input[0];
				return;
			}
			int rsiPeriod	= (int)(14 / (stdDev[0] / sma[0])) < 1 ? 1 : (int) (14 / (stdDev[0] / sma[0]));
			Value[0]		= RSI(rsiPeriod, Smooth)[0];
		}

		#region Properties
		// This property will be removed in future version because it does not affect calculation of DM Index
		[Browsable(false)]
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Smooth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DMIndex[] cacheDMIndex;
		public DMIndex DMIndex(int smooth)
		{
			return DMIndex(Input, smooth);
		}

		public DMIndex DMIndex(ISeries<double> input, int smooth)
		{
			if (cacheDMIndex != null)
				for (int idx = 0; idx < cacheDMIndex.Length; idx++)
					if (cacheDMIndex[idx] != null && cacheDMIndex[idx].Smooth == smooth && cacheDMIndex[idx].EqualsInput(input))
						return cacheDMIndex[idx];
			return CacheIndicator<DMIndex>(new DMIndex(){ Smooth = smooth }, input, ref cacheDMIndex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DMIndex DMIndex(int smooth)
		{
			return indicator.DMIndex(Input, smooth);
		}

		public Indicators.DMIndex DMIndex(ISeries<double> input , int smooth)
		{
			return indicator.DMIndex(input, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DMIndex DMIndex(int smooth)
		{
			return indicator.DMIndex(Input, smooth);
		}

		public Indicators.DMIndex DMIndex(ISeries<double> input , int smooth)
		{
			return indicator.DMIndex(input, smooth);
		}
	}
}

#endregion
