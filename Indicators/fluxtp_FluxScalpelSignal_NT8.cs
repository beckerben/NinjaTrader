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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class FluxScalpelSignal : Indicator
	{
		#region Variables
		private Series<double> 	R;
		private Series<double>	SP;
		private Series<double>	SZE, OB, OS;
		private double 		HLP;
		private	double	 	LLP;
		private	double 		myRange;
		private	double		Prange;
		#endregion
		private int AlertsThisBar = 0;
		private Series<double> FilterMA;
		private string tag=string.Empty;
		private string ArrowTagUp = "_BTTFT+PLT_FSS_LONG";
		private string ArrowTagDown = "_BTTFT+PLT_FSS_SHORT";

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Signal
		{
			get { return Values[0]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Momentum
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> OBLevel
		{
			get { return Values[2]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> OSLevel
		{
			get { return Values[3]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> TrendDirection
		{
			get { return Values[4]; }
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				bool Debug = System.IO.File.Exists("c:\\111111111111.txt") && NinjaTrader.Cbi.License.MachineId=="710DB663BDDC5FEAC4E6833DFA2BA11E";
				if(!Debug) VendorLicense("BackToTheFutureTrading", "FluxTriggerPack", "www.BackToTheFutureTrading.com", "Ron@backtothefuturetrading.com");
				
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Signal");
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Momentum");
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "OB Level");
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "OS Level");
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Trend Direction");

				string Version = "v2.1";
				Name = "Flux Scalpel Signal "+Version;
				Description									= @"";
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				Calculate									= Calculate.OnBarClose;
			}
			else if (State == State.DataLoaded)
			{
				R 	= new Series<double>(this);
				SP	= new Series<double>(this);
				SZE	= new Series<double>(this);
				OB	= new Series<double>(this);
				OS	= new Series<double>(this);
				FilterMA = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{	
			if(Plots[0].Brush != Brushes.Transparent) {
				Plots[0].Brush = Brushes.Transparent;
				Print("Flux Scalpel Signal - Signal Plot Color changed to 'Transparent':  Signal plot not intended to appear on a chart, it is only designed for Strategy and MarketAnalyzer integration only.");
			}
			if(IsFirstTickOfBar) tag = "FSS"+CurrentBar.ToString();
			if (CurrentBar < 2) return;
			if(IsFirstTickOfBar) AlertsThisBar=0;

			bool UpFilter = true;
			bool DownFilter = true;
			if(CurrentBar>5 && pEngageFilter){
				if(pFilterMAtype == FluxScalpelSignal_FilterMAtype.SMA) FilterMA[0] = SMA(this.pFilterMAperiod)[0];
				if(pFilterMAtype == FluxScalpelSignal_FilterMAtype.EMA) FilterMA[0] = EMA(this.pFilterMAperiod)[0];
				if(pFilterMAtype == FluxScalpelSignal_FilterMAtype.HMA) FilterMA[0] = HMA(this.pFilterMAperiod)[0];
				if(FilterMA[0] > FilterMA[1]) DownFilter = false;
				if(FilterMA[0] < FilterMA[1]) UpFilter = false;
			}
			int direction = 0;
			if(pEngageFilter) {
				if(UpFilter) direction = 1;
				if(DownFilter) direction = -1;
			}

			R[0] = Close[0] > Close[1] ? 1: -1;
			SP[0] = TEMA(R, period)[0];
			SZE[0] = 100 * (SP[0]/period);
			
			// Set momentum value for DataBox
			Momentum[0] = SZE[0];
			
			// Set trend direction for DataBox (1 = up, -1 = down, 0 = neutral)
			TrendDirection[0] = direction;
			
			// Set default levels when not enough bars
			if (CurrentBar < longPeriod)
			{
				OBLevel[0] = pOBLevel;
				OSLevel[0] = pOSLevel;
			}

			if (CurrentBar >= longPeriod)
			{
				HLP = (MAX(SZE, longPeriod)[0]);
				LLP = (MIN(SZE, longPeriod)[0]);
				myRange = HLP - LLP;
				Prange = myRange * (percent / 100);
				OB[0] = LLP + Prange;
				OS[0] = HLP + Prange;
				
				// Set OB/OS levels for DataBox
				OBLevel[0] = pOBLevel;
				OSLevel[0] = pOSLevel;

				double MaxLine = pOBLevel;
				double MinLine = pOSLevel;
				RemoveDrawObject(tag);
				Signal[0] = 0;
				if(SZE[1] >= MaxLine && SZE[0] <= MaxLine && direction <= 0) {
					Draw.ArrowDown(this, tag+ArrowTagDown, true, 0, High[0]+TickSize*pSeparation, new SolidColorBrush(pDownArrowColor));
					Signal[0] = -1;
				}
				if(SZE[1] <= MinLine && SZE[0] >= MinLine && direction >= 0) {
					Draw.ArrowUp(this, tag+ArrowTagUp, true, 0, Low[0]-TickSize*pSeparation, new SolidColorBrush(pUpArrowColor));
					Signal[0] = 1;
				}
				MaxLine = pOBWarning;
				MinLine = pOSWarning;
				if(SZE[0] >= MaxLine) {
					if(pOBAlertBackgroundColor != Colors.Transparent && pOBAlertBackgroundColorOpacity > 0) BackBrush = new SolidColorBrush(Color.FromArgb((byte)(25*pOBAlertBackgroundColorOpacity), pOBAlertBackgroundColor.R, pOBAlertBackgroundColor.G, pOBAlertBackgroundColor.B));
					if(pOBAlertBarColor != Colors.Transparent) {
						BarBrush = new SolidColorBrush(pOBAlertBarColor);
						CandleOutlineBrush = new SolidColorBrush(pOBAlertBarColor);
					}
					if(AlertsThisBar<pMaxAlertsPerBar) {
						AlertsThisBar++;
						Alert(CurrentBar.ToString()+AlertsThisBar.ToString(),Priority.High, "ScalpelSignal in OB area", pOBSoundFile, 1, new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.White));
					}
				}
				if(SZE[0] <= MinLine) {
					if(pOSAlertBackgroundColor != Colors.Transparent && pOSAlertBackgroundColorOpacity > 0) BackBrush = new SolidColorBrush(Color.FromArgb((byte)(25*pOSAlertBackgroundColorOpacity), pOSAlertBackgroundColor.R, pOSAlertBackgroundColor.G, pOSAlertBackgroundColor.B));
					if(pOSAlertBarColor != Colors.Transparent) {
						BarBrush = new SolidColorBrush(pOSAlertBarColor);
						CandleOutlineBrush = new SolidColorBrush(pOSAlertBarColor);
					}
					if(AlertsThisBar<pMaxAlertsPerBar) {
						AlertsThisBar++;
						Alert(CurrentBar.ToString()+AlertsThisBar.ToString(),Priority.High, "ScalpelSignal in OS area", pOSSoundFile, 1, new SolidColorBrush(Colors.Lime), new SolidColorBrush(Colors.White));
					}
				}
			}
		}

		internal class LoadSoundFileList : StringConverter
		{
			#region LoadSoundFileList
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
			{
				return true;
			}

			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
			{
				return false;
			}

			public override System.ComponentModel.TypeConverter.StandardValuesCollection
				GetStandardValues(ITypeDescriptorContext context)
			{
				string folder = System.IO.Path.Combine(NinjaTrader.Core.Globals.InstallDir,"sounds");
				string search = "*.wav";
				System.IO.DirectoryInfo dirCustom = new System.IO.DirectoryInfo(folder);
				System.IO.FileInfo[] filCustom = dirCustom.GetFiles(search);

				string[] list = new string[filCustom.Length];
				int i = 0;
				foreach (System.IO.FileInfo fi in filCustom)
				{
					list[i] = fi.Name;
					i++;
				}
				string[] filteredlist = new string[i];
				for(i = 0; i<filteredlist.Length; i++) filteredlist[i] = list[i];
				return new StandardValuesCollection(filteredlist);
			}
			#endregion
		}

		#region Properties
		#region Sounds
		private int pMaxAlertsPerBar = 1;
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Alerts Per Bar", Description="Number of audible alerts per bar, set to '0' to turn-off audible alerts", Order=1, GroupName="Parameters")]
		public int MaxAlertsPerBar
		{
			get { return pMaxAlertsPerBar; }
			set { pMaxAlertsPerBar = Math.Max(0, value); }
		}

		private string pOBSoundFile = "FluxScalpel.wav";
		[NinjaScriptProperty]
		[TypeConverter(typeof(LoadSoundFileList))]
		[Display(Name="OB Sound File", Description="Sound file when above the OB_Warning - it must exist in your Sounds folder in order to be played", Order=2, GroupName="Audible")]
		public string OBSoundFile
		{
			get { return pOBSoundFile; }
			set { pOBSoundFile = value; pOBSoundFile = pOBSoundFile.Replace(" ",string.Empty);}
		}

		private string pOSSoundFile = "FluxScalpel.wav";
		[NinjaScriptProperty]
		[TypeConverter(typeof(LoadSoundFileList))]
		[Display(Name="OS Sound File", Description="Sound file when below the OS_Warning - it must exist in your Sounds folder in order to be played", Order=3, GroupName="Audible")]
		public string OSSoundFile
		{
			get { return pOSSoundFile; }
			set { pOSSoundFile = value; pOSSoundFile = pOSSoundFile.Replace(" ",string.Empty);}
		}
		#endregion

		private int pFilterMAperiod = 120;
		private FluxScalpelSignal_FilterMAtype pFilterMAtype = FluxScalpelSignal_FilterMAtype.SMA;
		
		#region Filter Parameters
		[NinjaScriptProperty]
		[Display(Name="Filter MA Type", Description="Type of MA filter", Order=1, GroupName="Filter")]
		public FluxScalpelSignal_FilterMAtype FilterMAtype
		{
			get { return pFilterMAtype; }
			set { pFilterMAtype = value; }
		}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filter MA Period", Description="Period of MA filter", Order=2, GroupName="Filter")]
		public int FilterMAperiod
		{
			get { return pFilterMAperiod; }
			set { pFilterMAperiod = value; }
		}

		private bool pEngageFilter = true;
		[NinjaScriptProperty]
		[Display(Name="Engage Filter", Description="Engage MA filter?  Only signals in the direction of the trend will be given", Order=3, GroupName="Filter")]
		public bool EngageFilter
		{
			get { return pEngageFilter; }
			set { pEngageFilter = value; }
		}
		#endregion

		private 	int period 		= 14;
		private 	int longPeriod 	= 30;
		private		int percent 	= 95;

		private int pSeparation = 1;
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Separation", Description="Ticks between bar and signal arrows", Order=1, GroupName="Visual")]
		public int Separation
		{
			get { return pSeparation; }
			set { pSeparation = value; }
		}

		private Color pDownArrowColor = Colors.Red;
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Down Arrow Color", Description="", Order=2, GroupName="Visual")]
		public Color DownArrowColor
		{
			get { return pDownArrowColor; }
			set { pDownArrowColor = value; }
		}

		[Browsable(false)]
		public string DownArrowColorSerializable
		{
			get { return pDownArrowColor.ToString(); }
			set { pDownArrowColor = (Color)ColorConverter.ConvertFromString(value); }
		}

		private Color pUpArrowColor = Colors.Lime;
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Up Arrow Color", Description="", Order=3, GroupName="Visual")]
		public Color UpArrowColor
		{
			get { return pUpArrowColor; }
			set { pUpArrowColor = value; }
		}

		[Browsable(false)]
		public string UpArrowColorSerializable
		{
			get { return pUpArrowColor.ToString(); }
			set { pUpArrowColor = (Color)ColorConverter.ConvertFromString(value); }
		}

		private double pOBLevel = 6.75;
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="OB Level", Description="Overbought level, must be above zero", Order=1, GroupName="Parameters")]
		public double OB_Level
		{
			get { return pOBLevel; }
			set { pOBLevel = Math.Max(0, value); }
		}

		private double pOBWarning = 6.25;
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="OB Warning", Description="Overbought alert level, must be above zero", Order=2, GroupName="Parameters")]
		public double OB_Warning
		{
			get { return pOBWarning; }
			set { pOBWarning = Math.Max(0, value); }
		}

		private double pOSLevel = -6.75;
		[NinjaScriptProperty]
		[Range(double.MinValue, 0)]
		[Display(Name="OS Level", Description="Oversold level, must be below zero", Order=3, GroupName="Parameters")]
		public double OS_Level
		{
			get { return pOSLevel; }
			set { pOSLevel = Math.Min(0, value); }
		}

		private double pOSWarning = -6.25;
		[NinjaScriptProperty]
		[Range(double.MinValue, 0)]
		[Display(Name="OS Warning", Description="Oversold alert level, must be below zero", Order=4, GroupName="Parameters")]
		public double OS_Warning
		{
			get { return pOSWarning; }
			set { pOSWarning = Math.Min(0, value); }
		}

		private Color pOBAlertBackgroundColor = Colors.Transparent;
		private int pOBAlertBackgroundColorOpacity = 3;

		private Color pOSAlertBackgroundColor = Colors.Transparent;
		private int pOSAlertBackgroundColorOpacity = 3;

		private Color pOBAlertBarColor = Colors.Pink;
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="OB Alert Bar Color", Description="Color of bar when SZE is above the FixedAlertOB level.  To turn-off bar coloring, set this to 'Transparent'", Order=4, GroupName="Visual")]
		public Color OBAlertBarColor
		{
			get { return pOBAlertBarColor; }
			set { pOBAlertBarColor = value; }
		}

		[Browsable(false)]
		public string OBAlertBarColorSerializable
		{
			get { return pOBAlertBarColor.ToString(); }
			set { pOBAlertBarColor = (Color)ColorConverter.ConvertFromString(value); }
		}

		private Color pOSAlertBarColor = Colors.Aqua;
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="OS Alert Bar Color", Description="Color of bar when SZE is below the FixedAlertOS level.  To turn-off bar coloring, set this to 'Transparent'", Order=5, GroupName="Visual")]
		public Color OSAlertBarColor
		{
			get { return pOSAlertBarColor; }
			set { pOSAlertBarColor = value; }
		}

		[Browsable(false)]
		public string OSAlertBarColorSerializable
		{
			get { return pOSAlertBarColor.ToString(); }
			set { pOSAlertBarColor = (Color)ColorConverter.ConvertFromString(value); }
		}
		#endregion
	}
}

public enum FluxScalpelSignal_FilterMAtype {SMA,EMA,HMA};

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FluxScalpelSignal[] cacheFluxScalpelSignal;
		public FluxScalpelSignal FluxScalpelSignal(int maxAlertsPerBar, string oBSoundFile, string oSSoundFile, FluxScalpelSignal_FilterMAtype filterMAtype, int filterMAperiod, bool engageFilter, int separation, Color downArrowColor, Color upArrowColor, double oB_Level, double oB_Warning, double oS_Level, double oS_Warning, Color oBAlertBarColor, Color oSAlertBarColor)
		{
			return FluxScalpelSignal(Input, maxAlertsPerBar, oBSoundFile, oSSoundFile, filterMAtype, filterMAperiod, engageFilter, separation, downArrowColor, upArrowColor, oB_Level, oB_Warning, oS_Level, oS_Warning, oBAlertBarColor, oSAlertBarColor);
		}

		public FluxScalpelSignal FluxScalpelSignal(ISeries<double> input, int maxAlertsPerBar, string oBSoundFile, string oSSoundFile, FluxScalpelSignal_FilterMAtype filterMAtype, int filterMAperiod, bool engageFilter, int separation, Color downArrowColor, Color upArrowColor, double oB_Level, double oB_Warning, double oS_Level, double oS_Warning, Color oBAlertBarColor, Color oSAlertBarColor)
		{
			if (cacheFluxScalpelSignal != null)
				for (int idx = 0; idx < cacheFluxScalpelSignal.Length; idx++)
					if (cacheFluxScalpelSignal[idx] != null && cacheFluxScalpelSignal[idx].MaxAlertsPerBar == maxAlertsPerBar && cacheFluxScalpelSignal[idx].OBSoundFile == oBSoundFile && cacheFluxScalpelSignal[idx].OSSoundFile == oSSoundFile && cacheFluxScalpelSignal[idx].FilterMAtype == filterMAtype && cacheFluxScalpelSignal[idx].FilterMAperiod == filterMAperiod && cacheFluxScalpelSignal[idx].EngageFilter == engageFilter && cacheFluxScalpelSignal[idx].Separation == separation && cacheFluxScalpelSignal[idx].DownArrowColor == downArrowColor && cacheFluxScalpelSignal[idx].UpArrowColor == upArrowColor && cacheFluxScalpelSignal[idx].OB_Level == oB_Level && cacheFluxScalpelSignal[idx].OB_Warning == oB_Warning && cacheFluxScalpelSignal[idx].OS_Level == oS_Level && cacheFluxScalpelSignal[idx].OS_Warning == oS_Warning && cacheFluxScalpelSignal[idx].OBAlertBarColor == oBAlertBarColor && cacheFluxScalpelSignal[idx].OSAlertBarColor == oSAlertBarColor && cacheFluxScalpelSignal[idx].EqualsInput(input))
						return cacheFluxScalpelSignal[idx];
			return CacheIndicator<FluxScalpelSignal>(new FluxScalpelSignal(){ MaxAlertsPerBar = maxAlertsPerBar, OBSoundFile = oBSoundFile, OSSoundFile = oSSoundFile, FilterMAtype = filterMAtype, FilterMAperiod = filterMAperiod, EngageFilter = engageFilter, Separation = separation, DownArrowColor = downArrowColor, UpArrowColor = upArrowColor, OB_Level = oB_Level, OB_Warning = oB_Warning, OS_Level = oS_Level, OS_Warning = oS_Warning, OBAlertBarColor = oBAlertBarColor, OSAlertBarColor = oSAlertBarColor }, input, ref cacheFluxScalpelSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FluxScalpelSignal FluxScalpelSignal(int maxAlertsPerBar, string oBSoundFile, string oSSoundFile, FluxScalpelSignal_FilterMAtype filterMAtype, int filterMAperiod, bool engageFilter, int separation, Color downArrowColor, Color upArrowColor, double oB_Level, double oB_Warning, double oS_Level, double oS_Warning, Color oBAlertBarColor, Color oSAlertBarColor)
		{
			return indicator.FluxScalpelSignal(Input, maxAlertsPerBar, oBSoundFile, oSSoundFile, filterMAtype, filterMAperiod, engageFilter, separation, downArrowColor, upArrowColor, oB_Level, oB_Warning, oS_Level, oS_Warning, oBAlertBarColor, oSAlertBarColor);
		}

		public Indicators.FluxScalpelSignal FluxScalpelSignal(ISeries<double> input , int maxAlertsPerBar, string oBSoundFile, string oSSoundFile, FluxScalpelSignal_FilterMAtype filterMAtype, int filterMAperiod, bool engageFilter, int separation, Color downArrowColor, Color upArrowColor, double oB_Level, double oB_Warning, double oS_Level, double oS_Warning, Color oBAlertBarColor, Color oSAlertBarColor)
		{
			return indicator.FluxScalpelSignal(input, maxAlertsPerBar, oBSoundFile, oSSoundFile, filterMAtype, filterMAperiod, engageFilter, separation, downArrowColor, upArrowColor, oB_Level, oB_Warning, oS_Level, oS_Warning, oBAlertBarColor, oSAlertBarColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FluxScalpelSignal FluxScalpelSignal(int maxAlertsPerBar, string oBSoundFile, string oSSoundFile, FluxScalpelSignal_FilterMAtype filterMAtype, int filterMAperiod, bool engageFilter, int separation, Color downArrowColor, Color upArrowColor, double oB_Level, double oB_Warning, double oS_Level, double oS_Warning, Color oBAlertBarColor, Color oSAlertBarColor)
		{
			return indicator.FluxScalpelSignal(Input, maxAlertsPerBar, oBSoundFile, oSSoundFile, filterMAtype, filterMAperiod, engageFilter, separation, downArrowColor, upArrowColor, oB_Level, oB_Warning, oS_Level, oS_Warning, oBAlertBarColor, oSAlertBarColor);
		}

		public Indicators.FluxScalpelSignal FluxScalpelSignal(ISeries<double> input , int maxAlertsPerBar, string oBSoundFile, string oSSoundFile, FluxScalpelSignal_FilterMAtype filterMAtype, int filterMAperiod, bool engageFilter, int separation, Color downArrowColor, Color upArrowColor, double oB_Level, double oB_Warning, double oS_Level, double oS_Warning, Color oBAlertBarColor, Color oSAlertBarColor)
		{
			return indicator.FluxScalpelSignal(input, maxAlertsPerBar, oBSoundFile, oSSoundFile, filterMAtype, filterMAperiod, engageFilter, separation, downArrowColor, upArrowColor, oB_Level, oB_Warning, oS_Level, oS_Warning, oBAlertBarColor, oSAlertBarColor);
		}
	}
}

#endregion
