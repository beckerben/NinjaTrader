//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Linear Regression. The Linear Regression is an indicator that 'predicts' the value of a security's price.
	/// </summary>
	public class dpLRL : Indicator
	{
		private double avg;
		private double divisor;
		private double intercept;
		private double myPeriod;
		private double priorSumXY;
		private double priorSumY;
		private double slope;
		private double sumX2;
		private double sumX;
		private double sumXY;
		private double sumY;
		private SUM sum;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionLinReg;
				Name = "dpLRL";
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 50;

				AddPlot(new Stroke(Brushes.Black, 22), PlotStyle.Line, "LRShadow");
				AddPlot(new Stroke(Brushes.DimGray, 12), PlotStyle.Line, "LRTrend");
				AddPlot(new Stroke(Brushes.Silver, 6), PlotStyle.Line, "LR");
				ColorByBarClose = true;
				UpColor = Brushes.LawnGreen;
				DownColor = Brushes.HotPink;

			}
			else if (State == State.Configure)
			{
				avg = divisor = intercept = myPeriod = priorSumXY
					= priorSumY = slope = sumX = sumX2 = sumY = sumXY = 0;
			}
			else if (State == State.DataLoaded)
			{
				sum = SUM(Inputs[0], Period);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				double sumX = (double)Period * (Period - 1) * 0.5;
				double divisor = sumX * sumX - (double)Period * Period * (Period - 1) * (2 * Period - 1) / 6;
				double sumXY = 0;

				for (int count = 0; count < Period && CurrentBar - count >= 0; count++)
					sumXY += count * Input[count];

				double slope = ((double)Period * sumXY - sumX * SUM(Inputs[0], Period)[0]) / divisor;
				double intercept = (SUM(Inputs[0], Period)[0] - slope * sumX) / Period;

				Value[0] = intercept + slope * (Period - 1);
			}
			else
			{
				if (IsFirstTickOfBar)
				{
					priorSumY = sumY;
					priorSumXY = sumXY;
					myPeriod = Math.Min(CurrentBar + 1, Period);
					sumX = myPeriod * (myPeriod - 1) * 0.5;
					sumX2 = myPeriod * (myPeriod + 1) * 0.5;
					divisor = myPeriod * (myPeriod + 1) * (2 * myPeriod + 1) / 6 - sumX2 * sumX2 / myPeriod;
				}

				double input0 = Input[0];
				sumXY = priorSumXY - (CurrentBar >= Period ? priorSumY : 0) + myPeriod * input0;
				sumY = priorSumY + input0 - (CurrentBar >= Period ? Input[Period] : 0);
				avg = sumY / myPeriod;
				slope = (sumXY - sumX2 * avg) / divisor;
				intercept = (sum[0] - slope * sumX) / myPeriod;
				Value[0] = CurrentBar == 0 ? input0 : (intercept + slope * (myPeriod - 1));
			}

			Values[1][0] = Value[0];
			Values[2][0] = Value[0];
			
			if (CurrentBar < 3)
				return;
			
			PlotBrushes[1][0] = Value[0] >= Value[1] ? UpColor : DownColor; // coloured by slope
			
			if (ColorByBarClose)
			{
				PlotBrushes[2][0] = Input[0] >= Values[2][0] ? UpColor : DownColor; // coloured by bar close price
			}
		}

		#region Properties
		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Color by bar close", GroupName = "Plot Colors", Order = 1)]
		public bool ColorByBarClose { get; set; }

		[XmlIgnore]
		[Display(Name = "Up Color", GroupName = "Plot Colors", Order = 2)]
		public Brush UpColor { get; set; }

		[Browsable(false)]
		public string UpColorSerializable
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Down Color", GroupName = "Plot Colors", Order = 3)]
		public Brush DownColor { get; set; }

		[Browsable(false)]
		public string DownColorSerializable
		{
			get { return Serialize.BrushToString(DownColor); }
			set { DownColor = Serialize.StringToBrush(value); }
		}
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private dpLRL[] cachedpLRL;
		public dpLRL dpLRL(int period, bool colorByBarClose)
		{
			return dpLRL(Input, period, colorByBarClose);
		}

		public dpLRL dpLRL(ISeries<double> input, int period, bool colorByBarClose)
		{
			if (cachedpLRL != null)
				for (int idx = 0; idx < cachedpLRL.Length; idx++)
					if (cachedpLRL[idx] != null && cachedpLRL[idx].Period == period && cachedpLRL[idx].ColorByBarClose == colorByBarClose && cachedpLRL[idx].EqualsInput(input))
						return cachedpLRL[idx];
			return CacheIndicator<dpLRL>(new dpLRL(){ Period = period, ColorByBarClose = colorByBarClose }, input, ref cachedpLRL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.dpLRL dpLRL(int period, bool colorByBarClose)
		{
			return indicator.dpLRL(Input, period, colorByBarClose);
		}

		public Indicators.dpLRL dpLRL(ISeries<double> input , int period, bool colorByBarClose)
		{
			return indicator.dpLRL(input, period, colorByBarClose);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.dpLRL dpLRL(int period, bool colorByBarClose)
		{
			return indicator.dpLRL(Input, period, colorByBarClose);
		}

		public Indicators.dpLRL dpLRL(ISeries<double> input , int period, bool colorByBarClose)
		{
			return indicator.dpLRL(input, period, colorByBarClose);
		}
	}
}

#endregion
