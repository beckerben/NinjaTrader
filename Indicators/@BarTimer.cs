//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BarTimer : Indicator
	{
		private string			timeLeft	= string.Empty;
		private DateTime		now		 	= Core.Globals.Now;
		private bool			connected,
								hasRealtimeData;
		private SessionIterator sessionIterator;

		private System.Windows.Threading.DispatcherTimer timer;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description 		= Custom.Resource.NinjaScriptIndicatorDescriptionBarTimer;
				Name 				= Custom.Resource.NinjaScriptIndicatorNameBarTimer;
				Calculate			= Calculate.OnEachTick;
				DrawOnPricePanel	= false;
				IsChartOnly			= true;
				IsOverlay			= true;
				DisplayInDataBox	= false;
				TextPositionFine	= TextPositionFine.BottomRight;
			}
			else if (State == State.Realtime)
			{
				if (timer == null && IsVisible)
				{
					if (Bars.BarsType.IsTimeBased && Bars.BarsType.IsIntraday)
					{
						lock (Connection.Connections)
						{
							if (Connection.Connections.ToList().FirstOrDefault(c => c.Status == ConnectionStatus.Connected && c.InstrumentTypes.Contains(Instrument.MasterInstrument.InstrumentType)) == null)
								Draw.TextFixedFine(this, "NinjaScriptInfo", Custom.Resource.BarTimerDisconnectedError, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
							else
								Draw.TextFixedFine(this, "NinjaScriptInfo",
									!SessionIterator.IsInSession(Now, false, true) ? Custom.Resource.BarTimerSessionTimeError : Custom.Resource.BarTimerWaitingOnDataError, TextPositionFine,
									ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
						}
					}
					else
						Draw.TextFixedFine(this, "NinjaScriptInfo", Custom.Resource.BarTimerTimeBasedError, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
				}
			}
			else if (State == State.Terminated)
			{
				if (timer == null)
					return;

				timer.IsEnabled = false;
				timer = null;
			}
		}

		protected override void OnBarUpdate()
		{
			if (State == State.Realtime)
			{
				hasRealtimeData = true;
				connected = true;
			}
		}

		protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
		{
			if (connectionStatusUpdate.PriceStatus == ConnectionStatus.Connected
				&& connectionStatusUpdate.Connection.InstrumentTypes.Contains(Instrument.MasterInstrument.InstrumentType)
				&& Bars.BarsType.IsTimeBased
				&& Bars.BarsType.IsIntraday)
			{
				connected = true;

				if (DisplayTime() && timer == null)
				{
					ChartControl.Dispatcher.InvokeAsync(() =>
					{
						timer			= new System.Windows.Threading.DispatcherTimer { Interval = new TimeSpan(0, 0, 1), IsEnabled = true };
						timer.Tick		+= OnTimerTick;
					});
				}
			}
			else if (connectionStatusUpdate.PriceStatus == ConnectionStatus.Disconnected)
				connected = false;
		}

		private bool DisplayTime() => ChartControl != null && Bars?.Instrument.MarketData != null && IsVisible;

		private void OnTimerTick(object sender, EventArgs e)
		{
			ForceRefresh();

			if (DisplayTime())
			{
				if (timer is { IsEnabled: false })
					timer.IsEnabled = true;

				if (connected)
				{
					if (SessionIterator.IsInSession(Now, false, true))
					{
						if (hasRealtimeData)
						{
							TimeSpan barTimeLeft = Bars.GetTime(Bars.Count - 1).Subtract(Now);

							timeLeft = barTimeLeft.Ticks < 0
								? "00:00:00"
								: barTimeLeft.Hours.ToString("00") + ":" + barTimeLeft.Minutes.ToString("00") + ":" + barTimeLeft.Seconds.ToString("00");

							Draw.TextFixedFine(this, "NinjaScriptInfo", Custom.Resource.BarTimerTimeRemaining + timeLeft, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
						}
						else
							Draw.TextFixedFine(this, "NinjaScriptInfo", Custom.Resource.BarTimerWaitingOnDataError, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
					}
					else
						Draw.TextFixedFine(this, "NinjaScriptInfo", Custom.Resource.BarTimerSessionTimeError, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
				}
				else
				{
					Draw.TextFixedFine(this, "NinjaScriptInfo", Custom.Resource.BarTimerDisconnectedError, TextPositionFine, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);

					if (timer != null)
						timer.IsEnabled = false;
				}
			}
		}

		private SessionIterator SessionIterator => sessionIterator ??= new SessionIterator(Bars);

		private DateTime Now
		{
			get
			{
				now = Connection.PlaybackConnection != null ? Connection.PlaybackConnection.Now : Core.Globals.Now;

				if (now.Millisecond > 0)
					now = Core.Globals.MinDate.AddSeconds((long)Math.Floor(now.Subtract(Core.Globals.MinDate).TotalSeconds));

				return now;
			}
		}

		#region Properties
		[Display(ResourceType = typeof(Custom.Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
		public TextPositionFine TextPositionFine { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BarTimer[] cacheBarTimer;
		public BarTimer BarTimer()
		{
			return BarTimer(Input, TextPositionFine.BottomRight);
		}

		public BarTimer BarTimer(TextPositionFine textPositionFine)
		{
			return BarTimer(Input, textPositionFine);
		}

		public BarTimer BarTimer(ISeries<double> input)
		{
			return BarTimer(input, TextPositionFine.BottomRight);
		}

		public BarTimer BarTimer(ISeries<double> input, TextPositionFine textPositionFine)
		{
			if (cacheBarTimer != null)
				for (int idx = 0; idx < cacheBarTimer.Length; idx++)
					if (cacheBarTimer[idx] != null && cacheBarTimer[idx].TextPositionFine == textPositionFine && cacheBarTimer[idx].EqualsInput(input))
						return cacheBarTimer[idx];
			return CacheIndicator<BarTimer>(new BarTimer(), input, ref cacheBarTimer);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BarTimer BarTimer()
		{
			return indicator.BarTimer(Input);
		}

		public Indicators.BarTimer BarTimer(TextPositionFine textPositionFine)
		{
			return indicator.BarTimer(Input, textPositionFine);
		}

		public Indicators.BarTimer BarTimer(ISeries<double> input)
		{
			return indicator.BarTimer(input);
		}

		public Indicators.BarTimer BarTimer(ISeries<double> input, TextPositionFine textPositionFine)
		{
			return indicator.BarTimer(input, textPositionFine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BarTimer BarTimer()
		{
			return indicator.BarTimer(Input);
		}

		public Indicators.BarTimer BarTimer(TextPositionFine textPositionFine)
		{
			return indicator.BarTimer(Input, textPositionFine);
		}

		public Indicators.BarTimer BarTimer(ISeries<double> input)
		{
			return indicator.BarTimer(input);
		}

		public Indicators.BarTimer BarTimer(ISeries<double> input, TextPositionFine textPositionFine)
		{
			return indicator.BarTimer(input, textPositionFine);
		}
	}
}

#endregion
