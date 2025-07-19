//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Data;
using NinjaTrader.Core.FloatingPoint;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Swing indicator plots lines that represents the swing high and low points.
	/// </summary>
	public class Swing : Indicator
	{
		private int				constant;
		private double			currentSwingHigh;
		private double			currentSwingLow;
		private ArrayList		lastHighCache;
		private double			lastSwingHighValue;
		private ArrayList		lastLowCache;
		private double			lastSwingLowValue;
		private int				saveCurrentBar;

		private Series<double> swingHighSeries;
		private Series<double> swingHighSwings;
		private Series<double> swingLowSeries;
		private Series<double> swingLowSwings;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionSwing;
				Name						= Custom.Resource.NinjaScriptIndicatorNameSwing;
				DisplayInDataBox			= false;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				Strength					= 5;

				AddPlot(new Stroke(Brushes.DarkCyan,	2), PlotStyle.Dot, Custom.Resource.SwingHigh);
				AddPlot(new Stroke(Brushes.Goldenrod,	2), PlotStyle.Dot, Custom.Resource.SwingLow);
			}

			else if (State == State.Configure)
			{
				currentSwingHigh	= 0;
				currentSwingLow		= 0;
				lastSwingHighValue	= 0;
				lastSwingLowValue	= 0;
				saveCurrentBar		= -1;
				constant			= 2 * Strength + 1;
				Calculate			= Calculate.OnBarClose;
			}
			else if (State == State.DataLoaded)
			{
				lastHighCache	= new ArrayList();
				lastLowCache	= new ArrayList();

				swingHighSeries = new Series<double>(this);
				swingHighSwings = new Series<double>(this);
				swingLowSeries	= new Series<double>(this);
				swingLowSwings	= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			double high0	= !(Input is PriceSeries || Input is Bars) ? Input[0] : High[0];
			double low0		= !(Input is PriceSeries || Input is Bars) ? Input[0] : Low[0];
			double close0	= !(Input is PriceSeries || Input is Bars) ? Input[0] : Close[0];

			if (BarsArray[0].BarsType.IsRemoveLastBarSupported && CurrentBar < saveCurrentBar)
			{
				currentSwingHigh			= SwingHighPlot.IsValidDataPoint(0) ? SwingHighPlot[0] : 0;
				currentSwingLow				= SwingLowPlot.IsValidDataPoint(0) ? SwingLowPlot[0] : 0;
				lastSwingHighValue			= swingHighSeries[0];
				lastSwingLowValue			= swingLowSeries[0];
				swingHighSeries[Strength]	= 0;
				swingLowSeries[Strength]	= 0;

				lastHighCache.Clear();
				lastLowCache.Clear();
				for (int barsBack = Math.Min(CurrentBar, constant) - 1; barsBack >= 0; barsBack--)
				{
					lastHighCache.Add(!(Input is PriceSeries or Data.Bars) ? Input[barsBack] : High[barsBack]);
					lastLowCache.Add(!(Input is PriceSeries or Data.Bars) ? Input[barsBack] : Low[barsBack]);
				}
				saveCurrentBar = CurrentBar;
				return;
			}

			if (saveCurrentBar != CurrentBar)
			{
				swingHighSwings[0]	= 0;	// initializing important internal
				swingLowSwings[0]	= 0;	// initializing important internal

				swingHighSeries[0]	= 0;	// initializing important internal
				swingLowSeries[0]	= 0;	// initializing important internal

				lastHighCache.Add(high0);
				if (lastHighCache.Count > constant)
					lastHighCache.RemoveAt(0);
				lastLowCache.Add(low0);
				if (lastLowCache.Count > constant)
					lastLowCache.RemoveAt(0);

				if (lastHighCache.Count == constant)
				{
					bool	isSwingHigh					= true;
					double	swingHighCandidateValue		= (double) lastHighCache[Strength];
					for (int i = 0; i < Strength; i++)
						if (((double) lastHighCache[i]).ApproxCompare(swingHighCandidateValue) >= 0)
							isSwingHigh = false;

					for (int i = Strength+1; i < lastHighCache.Count; i++)
						if (((double) lastHighCache[i]).ApproxCompare(swingHighCandidateValue) > 0)
							isSwingHigh = false;

					swingHighSwings[Strength] = isSwingHigh ? swingHighCandidateValue : 0.0;
					if (isSwingHigh)
						lastSwingHighValue = swingHighCandidateValue;

					if (isSwingHigh)
					{
						currentSwingHigh = swingHighCandidateValue;
						for (int i = 0; i <= Strength; i++)
							SwingHighPlot[i] = currentSwingHigh;
					}
					else if (high0 > currentSwingHigh || currentSwingHigh.ApproxCompare(0.0) == 0)
					{
						currentSwingHigh = 0.0;
						SwingHighPlot[0] = close0;
						SwingHighPlot.Reset();
					}
					else
						SwingHighPlot[0] = currentSwingHigh;

					if (isSwingHigh)
						for (int i = 0; i <= Strength; i++)
							swingHighSeries[i] = lastSwingHighValue;
					else
						swingHighSeries[0] = lastSwingHighValue;
				}

				if (lastLowCache.Count == constant)
				{
					bool	isSwingLow				= true;
					double	swingLowCandidateValue	= (double) lastLowCache[Strength];
					for (int i = 0; i < Strength; i++)
						if (((double) lastLowCache[i]).ApproxCompare(swingLowCandidateValue) <= 0)
							isSwingLow = false;

					for (int i=Strength+1; i < lastLowCache.Count; i++)
						if (((double) lastLowCache[i]).ApproxCompare(swingLowCandidateValue) < 0)
							isSwingLow = false;

					swingLowSwings[Strength] = isSwingLow ? swingLowCandidateValue : 0.0;
					if (isSwingLow)
						lastSwingLowValue = swingLowCandidateValue;

					if (isSwingLow)
					{
						currentSwingLow = swingLowCandidateValue;
						for (int i = 0; i <= Strength; i++)
							SwingLowPlot[i] = currentSwingLow;
					}
					else if (low0 < currentSwingLow || currentSwingLow.ApproxCompare(0.0) == 0)
					{
						currentSwingLow = double.MaxValue;
						SwingLowPlot[0] = close0;
						SwingLowPlot.Reset();
					}
					else
						SwingLowPlot[0] = currentSwingLow;

					if (isSwingLow)
						for (int i = 0; i <= Strength; i++)
							swingLowSeries[i] = lastSwingLowValue;
					else
						swingLowSeries[0] = lastSwingLowValue;
				}

				saveCurrentBar = CurrentBar;
			}
			else if (CurrentBar >= constant - 1)
			{
				if (lastHighCache.Count == constant && high0.ApproxCompare((double) lastHighCache[lastHighCache.Count - 1]) > 0)
					lastHighCache[lastHighCache.Count - 1] = high0;
				if (lastLowCache.Count == constant && low0.ApproxCompare((double) lastLowCache[lastLowCache.Count - 1]) < 0)
					lastLowCache[lastLowCache.Count - 1] = low0;

				if (high0 > currentSwingHigh && swingHighSwings[Strength] > 0.0)
				{
					swingHighSwings[Strength] = 0.0;
					for (int i = 0; i <= Strength; i++)
					{
						SwingHighPlot[i] = close0;
						SwingHighPlot.Reset(i);
						currentSwingHigh = 0.0;
					}
				}
				else if (high0 > currentSwingHigh && currentSwingHigh.ApproxCompare(0.0) != 0)
				{
					SwingHighPlot[0] = close0;
					SwingHighPlot.Reset();
					currentSwingHigh = 0.0;
				}
				else if (high0 <= currentSwingHigh)
					SwingHighPlot[0] = currentSwingHigh;

				if (low0 < currentSwingLow && swingLowSwings[Strength] > 0.0)
				{
					swingLowSwings[Strength] = 0.0;
					for (int i = 0; i <= Strength; i++)
					{
						SwingLowPlot[i] = close0;
						SwingLowPlot.Reset(i);
						currentSwingLow = double.MaxValue;
					}
				}
				else if (low0 < currentSwingLow && currentSwingLow.ApproxCompare(double.MaxValue) != 0)
				{
					SwingLowPlot.Reset();
					currentSwingLow = double.MaxValue;
				}
				else if (low0 >= currentSwingLow)
					SwingLowPlot[0] = currentSwingLow;
			}
		}

		#region Functions
		/// <summary>
		/// Returns the number of bars ago a swing low occurred. Returns a value of -1 if a swing low is not found within the look back period.
		/// </summary>
		/// <param name="barsAgo"></param>
		/// <param name="instance"></param>
		/// <param name="lookBackPeriod"></param>
		/// <returns></returns>
		public int SwingLowBar(int barsAgo, int instance, int lookBackPeriod)
		{
			if (instance < 1)
				throw new Exception(string.Format(Custom.Resource.SwingSwingLowBarInstanceGreaterEqual, GetType().Name, instance));
			if (barsAgo < 0)
				throw new Exception(string.Format(Custom.Resource.SwingSwingLowBarBarsAgoGreaterEqual, GetType().Name, barsAgo));
			if (barsAgo >= Count)
				throw new Exception(string.Format(Custom.Resource.SwingSwingLowBarBarsAgoOutOfRange, GetType().Name, Count - 1, barsAgo));

			Update();

			for (int idx=CurrentBar - barsAgo - Strength; idx >= CurrentBar - barsAgo - Strength - lookBackPeriod; idx--)
			{
				if (idx < 0)
					return -1;
				if (idx >= swingLowSwings.Count)
					continue;

				if (swingLowSwings.GetValueAt(idx).Equals(0.0))
					continue;

				if (instance == 1) // 1-based, < to be save
					return CurrentBar - idx;

				instance--;
			}

			return -1;
		}

		/// <summary>
		/// Returns the number of bars ago a swing high occurred. Returns a value of -1 if a swing high is not found within the look back period.
		/// </summary>
		/// <param name="barsAgo"></param>
		/// <param name="instance"></param>
		/// <param name="lookBackPeriod"></param>
		/// <returns></returns>
		public int SwingHighBar(int barsAgo, int instance, int lookBackPeriod)
		{
			if (instance < 1)
				throw new Exception(string.Format(Custom.Resource.SwingSwingHighBarInstanceGreaterEqual, GetType().Name, instance));
			if (barsAgo < 0)
				throw new Exception(string.Format(Custom.Resource.SwingSwingHighBarBarsAgoGreaterEqual, GetType().Name, barsAgo));
			if (barsAgo >= Count)
				throw new Exception(string.Format(Custom.Resource.SwingSwingHighBarBarsAgoOutOfRange, GetType().Name, Count - 1, barsAgo));

			Update();

			for (int idx=CurrentBar - barsAgo - Strength; idx >= CurrentBar - barsAgo - Strength - lookBackPeriod; idx--)
			{
				if (idx < 0)
					return -1;
				if (idx >= swingHighSwings.Count)
					continue;

				if (swingHighSwings.GetValueAt(idx).Equals(0.0))
					continue;

				if (instance <= 1) // 1-based, < to be save
					return CurrentBar - idx;

				instance--;
			}

			return -1;
		}
		#endregion

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Strength", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Strength { get; set; }

		/// <summary>
		/// Gets the high swings.
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SwingHigh
		{
			get
			{
				Update();
				return swingHighSeries;
			}
		}

		private Series<double> SwingHighPlot
		{
			get
			{
				Update();
				return Values[0];
			}
		}

		/// <summary>
		/// Gets the low swings.
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SwingLow
		{
			get
			{
				Update();
				return swingLowSeries;
			}
		}

		private Series<double> SwingLowPlot
		{
			get
			{
				Update();
				return Values[1];
			}
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Swing[] cacheSwing;
		public Swing Swing(int strength)
		{
			return Swing(Input, strength);
		}

		public Swing Swing(ISeries<double> input, int strength)
		{
			if (cacheSwing != null)
				for (int idx = 0; idx < cacheSwing.Length; idx++)
					if (cacheSwing[idx] != null && cacheSwing[idx].Strength == strength && cacheSwing[idx].EqualsInput(input))
						return cacheSwing[idx];
			return CacheIndicator<Swing>(new Swing(){ Strength = strength }, input, ref cacheSwing);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Swing Swing(int strength)
		{
			return indicator.Swing(Input, strength);
		}

		public Indicators.Swing Swing(ISeries<double> input , int strength)
		{
			return indicator.Swing(input, strength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Swing Swing(int strength)
		{
			return indicator.Swing(Input, strength);
		}

		public Indicators.Swing Swing(ISeries<double> input , int strength)
		{
			return indicator.Swing(input, strength);
		}
	}
}

#endregion
