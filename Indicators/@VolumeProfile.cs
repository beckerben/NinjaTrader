//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VolumeProfile : Indicator
	{
		#region Properties
		internal class VolumeInfoItem
		{
			public double up;
			public double down;
			public double neutral;
		}

		private				double			alpha				= 50;
		private				double			askPrice;
		private readonly	int				barSpacing			= 1;
		private				double			bidPrice;
		private				DateTime		cacheSessionEnd		= Globals.MinDate;
		private				DateTime		currentDate			= Globals.MinDate;
		private				bool			drawLines;
		private readonly	List<int>		newSessionBarIdx	= new();
		private				DateTime		sessionDateTmp		= Globals.MinDate;
		private				SessionIterator sessionIterator;
		private				int				startIndexOf;
		private				SessionIterator storedSession;

		private readonly	List<Dictionary<double, VolumeInfoItem>> 	sortedDicList	= new();
		private				Dictionary<double, VolumeInfoItem> 			cacheDictionary = new();
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVolumeProfile;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVolumeProfile;
				Calculate					= Calculate.OnEachTick;
				DrawLines					= false;
				IsChartOnly					= true;
				IsOverlay					= true;
				DrawOnPricePanel			= false;
				LineBrush					= Brushes.DarkGray;
				VolumeDownBrush				= Brushes.Crimson;
				VolumeNeutralBrush			= Brushes.DarkGray;
				VolumeUpBrush				= Brushes.DarkCyan;
			}
			else if (State == State.Configure)
				ZOrder = -1;
			else if (State == State.DataLoaded)
				storedSession = new SessionIterator(Bars);
			else if (State == State.Historical && Calculate != Calculate.OnEachTick)
				Draw.TextFixed(this, "NinjaScriptInfo", string.Format(Custom.Resource.NinjaScriptOnBarCloseError, Name), TextPosition.BottomRight);
		}

		protected override void OnBarUpdate() {}

		private DateTime GetLastBarSessionDate(DateTime time)
		{
			if (time <= cacheSessionEnd)
				return sessionDateTmp;
			
			if (!Bars.BarsType.IsIntraday)
				return sessionDateTmp;

			storedSession.GetNextSession(time, true);
			
			cacheSessionEnd = storedSession.ActualSessionEnd;
			sessionDateTmp 	= TimeZoneInfo.ConvertTime(cacheSessionEnd.AddSeconds(-1), Globals.GeneralOptions.TimeZoneInfo, Bars.TradingHours.TimeZoneInfo);

			if(newSessionBarIdx.Count == 0 || newSessionBarIdx.Count > 0 && CurrentBar > newSessionBarIdx[newSessionBarIdx.Count - 1])
				newSessionBarIdx.Add(CurrentBar);

			return sessionDateTmp;
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (Bars.Count <= 0)
				return;

			double			price;
			long			volume;
			VolumeInfoItem	volumeInfoItem;
			DateTime		lastBarTimeStamp = GetLastBarSessionDate(Time[0]);

			if (lastBarTimeStamp != currentDate)
			{
				cacheDictionary = new Dictionary<double, VolumeInfoItem>();
				sortedDicList.Add(cacheDictionary);
			}

			currentDate = lastBarTimeStamp;
			if (Bars.IsTickReplay)
			{
				if (e.MarketDataType == MarketDataType.Last)
				{
					price	= e.Price;
					volume	= e.Volume;

					if (!cacheDictionary.ContainsKey(price))
						cacheDictionary.Add(price, new VolumeInfoItem());

					volumeInfoItem = cacheDictionary[price];

					if (price >= e.Ask)
						volumeInfoItem.up		+= volume;
					else if (price <= e.Bid)
						volumeInfoItem.down		+= volume;
					else
						volumeInfoItem.neutral	+= volume;
				}
			}
			else
			{
				if (e.MarketDataType == MarketDataType.Ask)
				{
					askPrice = e.Price;
					return;
				}

				if (e.MarketDataType == MarketDataType.Bid)
				{
					bidPrice = e.Price;
					return;
				}

				if (e.MarketDataType != MarketDataType.Last || ChartControl == null || askPrice == 0 || bidPrice == 0)
					return;

				if (Bars != null && !SessionIterator.IsInSession(Globals.Now, true, true))
					return;

				price	= e.Price;
				volume	= e.Volume;

				if (!cacheDictionary.ContainsKey(price))
					cacheDictionary.Add(price, new VolumeInfoItem());

				volumeInfoItem = cacheDictionary[price];

				if (price >= askPrice)
					volumeInfoItem.up		+= volume;
				else if (price <= bidPrice)
					volumeInfoItem.down		+= volume;
				else
					volumeInfoItem.neutral	+= volume;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if(Bars == null || Bars.Instrument == null || IsInHitTest)
				return;

			int		firstBarIdxToPaint	= -1;
			double	tickSize			= Bars.Instrument.MasterInstrument.TickSize;
			double	volumeMax			= 0;

			SharpDX.Direct2D1.Brush upBrush			= VolumeUpBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush downBrush		= VolumeDownBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush neutralBrush	= VolumeNeutralBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush lineBrushDx		= LineBrush.ToDxBrush(RenderTarget);

			upBrush.Opacity			= (float)(alpha / 100.0);
			downBrush.Opacity		= (float)(alpha / 100.0);
			neutralBrush.Opacity	= (float)(alpha / 100.0);

			for (int i = newSessionBarIdx.Count - 1; i > 0; i--)
			{
				if (newSessionBarIdx[i] <= ChartBars.ToIndex)
				{
					startIndexOf		= i;
					firstBarIdxToPaint	= newSessionBarIdx[i];
					break;
				}
			}
			
			if (sortedDicList.Count < 1 && cacheDictionary.Keys.Count > 0)
				sortedDicList.Add(cacheDictionary);

			foreach (Dictionary<double, VolumeInfoItem> tmpDict in sortedDicList)
			{
				foreach (KeyValuePair<double, VolumeInfoItem> keyValue in tmpDict)
				{
					double price = keyValue.Key;

					if (Bars.BarsType.IsIntraday && (price > chartScale.MaxValue || price < chartScale.MinValue))
						continue;

					VolumeInfoItem	vii		= keyValue.Value;
					double			total	= vii.up + vii.down + vii.neutral;
					volumeMax				= Math.Max(volumeMax, total);
				}
			}

			if (volumeMax.ApproxCompare(0) == 0)
				return;

			int viiPositions = 0;

			foreach (KeyValuePair<double, VolumeInfoItem> keyValue in sortedDicList[startIndexOf])
			{
				viiPositions++;

				VolumeInfoItem vii = keyValue.Value;

				double	priceLower			= keyValue.Key - tickSize / 2;
				float	yLower				= chartScale.GetYByValue(priceLower);
				float	yUpper				= chartScale.GetYByValue(priceLower + tickSize);
				float	height				= Math.Max(1, Math.Abs(yUpper - yLower) - barSpacing);
				int		barWidthUp			= (int)(ChartPanel.W / 2.0 * (vii.up / volumeMax));
				int		barWidthNeutral		= (int)(ChartPanel.W / 2.0 * (vii.neutral / volumeMax));
				int		barWidthDown		= (int)(ChartPanel.W / 2.0 * (vii.down / volumeMax));
				float	stationaryXpos		= chartControl.GetXByBarIndex(ChartBars, !Bars.IsTickReplay ? ChartBars.FromIndex : Math.Max(ChartBars.FromIndex, firstBarIdxToPaint));
				float	xpos				= chartControl.GetXByBarIndex(ChartBars, !Bars.IsTickReplay ? ChartBars.FromIndex : Math.Max(1, Math.Max(ChartBars.FromIndex, firstBarIdxToPaint)) - 1);

				RenderTarget.FillRectangle(new SharpDX.RectangleF(xpos, yUpper, barWidthUp, height), upBrush);
				xpos += barWidthUp;
				RenderTarget.FillRectangle(new SharpDX.RectangleF(xpos, yUpper, barWidthNeutral, height), neutralBrush);
				xpos += barWidthNeutral;
				RenderTarget.FillRectangle(new SharpDX.RectangleF(xpos, yUpper, barWidthDown, height), downBrush);

				if (!drawLines)
					continue;
				
				// Lower line
				RenderTarget.DrawLine(new SharpDX.Vector2(stationaryXpos, yLower), new SharpDX.Vector2(ChartPanel.X + ChartPanel.W, yLower), lineBrushDx);

				// Upper line (only at very top)
				if (viiPositions == sortedDicList[startIndexOf].Count)
					RenderTarget.DrawLine(new SharpDX.Vector2(stationaryXpos, yUpper), new SharpDX.Vector2(ChartPanel.X + ChartPanel.W, yUpper), lineBrushDx);
			}

			lineBrushDx.Dispose();
			upBrush.Dispose();
			downBrush.Dispose();
			neutralBrush.Dispose();
		}

		#region Properties
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Opacity", Order = 0, GroupName = "NinjaScriptParameters")]
		public double Opacity
		{
			get => alpha;
			set => alpha = Math.Max(1, value);
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "DrawLines", Order = 1, GroupName = "NinjaScriptParameters")]
		public bool DrawLines
		{
			get => drawLines;
			set => drawLines = value;
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "LineColor", Order = 2, GroupName = "NinjaScriptParameters")]
		public Brush LineBrush { get; set; }

		[Browsable(false)]
		public string LineBrushSerialize
		{
			get => Serialize.BrushToString(LineBrush);
			set => LineBrush = Serialize.StringToBrush(value);
		}

		private SessionIterator SessionIterator => sessionIterator ??= new SessionIterator(Bars);

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "VolumeDownColor", Order = 3, GroupName = "NinjaScriptParameters")]
		public Brush VolumeDownBrush { get; set; }

		[Browsable(false)]
		public string VolumeDownBrushSerialize
		{
			get => Serialize.BrushToString(VolumeDownBrush);
			set => VolumeDownBrush = Serialize.StringToBrush(value);
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "VolumeNeutralColor", Order = 4, GroupName = "NinjaScriptParameters")]
		public Brush VolumeNeutralBrush { get; set; }

		[Browsable(false)]
		public string VolumeNeutralBrushSerialize
		{
			get => Serialize.BrushToString(VolumeNeutralBrush);
			set => VolumeNeutralBrush = Serialize.StringToBrush(value);
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "VolumeUpColor", Order = 5, GroupName = "NinjaScriptParameters")]
		public Brush VolumeUpBrush { get; set; }

		[Browsable(false)]
		public string VolumeUpBrushSerialize
		{
			get => Serialize.BrushToString(VolumeUpBrush);
			set => VolumeUpBrush = Serialize.StringToBrush(value);
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VolumeProfile[] cacheVolumeProfile;
		public VolumeProfile VolumeProfile()
		{
			return VolumeProfile(Input);
		}

		public VolumeProfile VolumeProfile(ISeries<double> input)
		{
			if (cacheVolumeProfile != null)
				for (int idx = 0; idx < cacheVolumeProfile.Length; idx++)
					if (cacheVolumeProfile[idx] != null &&  cacheVolumeProfile[idx].EqualsInput(input))
						return cacheVolumeProfile[idx];
			return CacheIndicator<VolumeProfile>(new VolumeProfile(), input, ref cacheVolumeProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeProfile VolumeProfile()
		{
			return indicator.VolumeProfile(Input);
		}

		public Indicators.VolumeProfile VolumeProfile(ISeries<double> input )
		{
			return indicator.VolumeProfile(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeProfile VolumeProfile()
		{
			return indicator.VolumeProfile(Input);
		}

		public Indicators.VolumeProfile VolumeProfile(ISeries<double> input )
		{
			return indicator.VolumeProfile(input);
		}
	}
}

#endregion
