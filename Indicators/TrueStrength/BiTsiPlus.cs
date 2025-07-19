#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.BrettIller
{
	#region Category Order
	[Gui.CategoryOrder("Indicator Settings", 1)]
	[Gui.CategoryOrder("Display", 2)]
	[Gui.CategoryOrder("Indicator Version", 20)]
	#endregion

	public class BiTsiPlus : Indicator, ICustomTypeDescriptor
	{
        #region Indicators, Series and Floating Variables
        //Indicators
        private TSI _indTsi;

        //Series

        //Floating Variables

        //int
        //double
        //bool
        //string
        private string _sThisName = "BI TSI Plus";
		//other

		#endregion

		#region On State Change
		protected override void OnStateChange()
        {
            #region Set Defaults
				#region Default Crap
            if (State == State.SetDefaults)
            {
                Description = @"";
                Name = _sThisName;
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = true;
                #endregion

                #region Indicator Settings

                Fast = 3;
                Slow = 14;
                ObLevel = 25;
                OsLevel = -25;

                #endregion

                #region Display

                ObColor = Brushes.Red;
                OsColor = Brushes.Green;

                #endregion
            }
            #endregion

            #region Other States

            #region State Configure
            else if (State == State.Configure)
            {
                ClearOutputWindow();
            }
			#endregion

			#region State Data Loaded
			else if (State == State.DataLoaded)
			{
                _indTsi = TSI(BarsArray[0], Fast, Slow);
            }
            #endregion

            #endregion
        }
        #endregion

        #region On Bar Update
        protected override void OnBarUpdate()
        {
			if (CurrentBar < 20) return;

            if (_indTsi[0] > ObLevel)
                BarBrush = ObColor;
            else if (_indTsi[0] < OsLevel)
                BarBrush = OsColor;
            else
                BarBrush = null;
            
        }
        #endregion

        #region Objects

        #region Functions

        #region Override Display Name
        public override string DisplayName { get { return _sThisName; } }
        #endregion

        #endregion

        #endregion

        #region Properties

        #region Indicator Settings

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Fast", Description = "Choose the fast period for your indicator.", Order = 10, GroupName = "Indicator Settings")]
        public int Fast { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Slow", Description = "Choose the slow period for your indicator.", Order = 20, GroupName = "Indicator Settings")]
        public int Slow { get; set; }

        [Range(0.0001, double.MaxValue), NinjaScriptProperty]
        [Display(Name = "OB Level", Description = "Use this to set a value for the OB level", Order = 30, GroupName = "Indicator Settings")]
        public double ObLevel { get; set; }

        [Range(double.MinValue, -0.0001), NinjaScriptProperty]
        [Display(Name = "OB Level", Description = "Use this to set a value for the OS level", Order = 40, GroupName = "Indicator Settings")]
        public double OsLevel { get; set; }

        #endregion

        #region Display

        [XmlIgnore]
        [Display(Name = "OB Color", Description = "Choose the color with which to paint the OB candles.", Order = 10, GroupName = "Display")]
        public Brush ObColor { get; set; }
        [Browsable(false)]
        public string ObColor_ { get { return Serialize.BrushToString(ObColor); } set { ObColor = Serialize.StringToBrush(value); } }

        [XmlIgnore]
        [Display(Name = "OS Color", Description = "Choose the color with which to paint the OS candles.", Order = 20, GroupName = "Display")]
        public Brush OsColor { get; set; }
        [Browsable(false)]
        public string OsColor_ { get { return Serialize.BrushToString(OsColor); } set { OsColor = Serialize.StringToBrush(value); } }

        #endregion

        #region Indicator Version
        private const string VERSION = "v1.00 02.19.2024";
		[Display(Name = "Indicator Version", GroupName = "Indicator Version", Description = "Indicator Version", Order = 0)]
		public string indicatorVersion { get { return VERSION; } }
		#endregion

		#endregion

		#region Custom Property Manipulation
		private void ModifyProperties(PropertyDescriptorCollection col)
		{
		}
		#endregion

		#region ICustomTypeDescriptor Members
		public PropertyDescriptorCollection GetProperties() { return TypeDescriptor.GetProperties(GetType()); }
		public object GetPropertyOwner(PropertyDescriptor pd) { return this; }
		public AttributeCollection GetAttributes() { return TypeDescriptor.GetAttributes(GetType()); }
		public string GetClassName() { return TypeDescriptor.GetClassName(GetType()); }
		public string GetComponentName() { return TypeDescriptor.GetComponentName(GetType()); }
		public TypeConverter GetConverter() { return TypeDescriptor.GetConverter(GetType()); }
		public EventDescriptor GetDefaultEvent() { return TypeDescriptor.GetDefaultEvent(GetType()); }
		public PropertyDescriptor GetDefaultProperty() { return TypeDescriptor.GetDefaultProperty(GetType()); }
		public object GetEditor(Type editorBaseType) { return TypeDescriptor.GetEditor(GetType(), editorBaseType); }
		public EventDescriptorCollection GetEvents(Attribute[] attributes) { return TypeDescriptor.GetEvents(GetType(), attributes); }
		public EventDescriptorCollection GetEvents() { return TypeDescriptor.GetEvents(GetType()); }
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection orig = GetFilteredIndicatorProperties(TypeDescriptor.GetProperties(GetType(), attributes), this.IsOwnedByChart, this.IsCreatedByStrategy);
			PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
			orig.CopyTo(arr, 0);
			PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);
			ModifyProperties(col);
			return col;
		}
		public static PropertyDescriptorCollection GetFilteredIndicatorProperties(PropertyDescriptorCollection origProperties, bool isOwnedByChart, bool isCreatedByStrategy)
		{
			List<PropertyDescriptor> allProps = new List<PropertyDescriptor>();
			foreach (PropertyDescriptor pd in origProperties) { allProps.Add(pd); }
			Type[] excludedTypes = new Type[] { typeof(System.Windows.Media.Brush), typeof(NinjaTrader.Gui.Stroke), typeof(System.Windows.Media.Color), typeof(System.Windows.Media.Pen) };
			Func<Type, bool> IsNotAVisualType = (Type propType) => {
				foreach (Type testType in excludedTypes) { if (testType.IsAssignableFrom(propType)) return false; }
				return true;
			};
			IEnumerable<string> baseIndProperties = from bp in typeof(IndicatorBase).GetProperties(BindingFlags.Instance | BindingFlags.Public) select bp.Name;
			IEnumerable<PropertyDescriptor> filteredProps = from p in allProps where p.IsBrowsable && (!isOwnedByChart && !isCreatedByStrategy ? (!baseIndProperties.Contains(p.Name) && p.Name != "Calculate" && p.Name != "Displacement" && IsNotAVisualType(p.PropertyType)) : true) select p;
			return new PropertyDescriptorCollection(filteredProps.ToArray());
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BrettIller.BiTsiPlus[] cacheBiTsiPlus;
		public BrettIller.BiTsiPlus BiTsiPlus(int fast, int slow, double obLevel, double osLevel)
		{
			return BiTsiPlus(Input, fast, slow, obLevel, osLevel);
		}

		public BrettIller.BiTsiPlus BiTsiPlus(ISeries<double> input, int fast, int slow, double obLevel, double osLevel)
		{
			if (cacheBiTsiPlus != null)
				for (int idx = 0; idx < cacheBiTsiPlus.Length; idx++)
					if (cacheBiTsiPlus[idx] != null && cacheBiTsiPlus[idx].Fast == fast && cacheBiTsiPlus[idx].Slow == slow && cacheBiTsiPlus[idx].ObLevel == obLevel && cacheBiTsiPlus[idx].OsLevel == osLevel && cacheBiTsiPlus[idx].EqualsInput(input))
						return cacheBiTsiPlus[idx];
			return CacheIndicator<BrettIller.BiTsiPlus>(new BrettIller.BiTsiPlus(){ Fast = fast, Slow = slow, ObLevel = obLevel, OsLevel = osLevel }, input, ref cacheBiTsiPlus);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BrettIller.BiTsiPlus BiTsiPlus(int fast, int slow, double obLevel, double osLevel)
		{
			return indicator.BiTsiPlus(Input, fast, slow, obLevel, osLevel);
		}

		public Indicators.BrettIller.BiTsiPlus BiTsiPlus(ISeries<double> input , int fast, int slow, double obLevel, double osLevel)
		{
			return indicator.BiTsiPlus(input, fast, slow, obLevel, osLevel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BrettIller.BiTsiPlus BiTsiPlus(int fast, int slow, double obLevel, double osLevel)
		{
			return indicator.BiTsiPlus(Input, fast, slow, obLevel, osLevel);
		}

		public Indicators.BrettIller.BiTsiPlus BiTsiPlus(ISeries<double> input , int fast, int slow, double obLevel, double osLevel)
		{
			return indicator.BiTsiPlus(input, fast, slow, obLevel, osLevel);
		}
	}
}

#endregion
