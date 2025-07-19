//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
public class SampleCustomRender : Indicator
{
		// These are WPF Brushes which are pushed and exposed to the UI by default
		// And allow users to configure a custom value of their choice
		// We will later convert the user defined brush from the UI to SharpDX Brushes for rendering purposes
		private System.Windows.Media.Brush	areaBrush;
		private int							areaOpacity;
		private SMA							mySma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionSampleCustomRender;
				Name						= Custom.Resource.NinjaScriptIndicatorNameSampleCustomRender;
				Calculate					= Calculate.OnBarClose;
				DisplayInDataBox			= false;
				IsOverlay					= true;
				IsChartOnly					= true;
				IsSuspendedWhileInactive	= true;
				ScaleJustification			= ScaleJustification.Right;
				AreaBrush					= System.Windows.Media.Brushes.DodgerBlue;
				TextBrush					= System.Windows.Media.Brushes.DodgerBlue;
				SmallAreaBrush				= System.Windows.Media.Brushes.Crimson;
				AreaOpacity					= 20;

				AddPlot(System.Windows.Media.Brushes.Crimson, Custom.Resource.NinjaScriptIndicatorNameSampleCustomRender);
			}
			else if (State == State.DataLoaded)
				mySma = SMA(20);
			else if (State == State.Historical)
				SetZOrder(-1); // default here is go below the bars and called in State.Historical
		}

		protected override void OnBarUpdate() => Value[0] = mySma[0];

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{

			// This sample should be used along side the help guide educational resource on this topic:
			// http://www.ninjatrader.com/support/helpGuides/nt8/en-us/?using_sharpdx_for_custom_chart_rendering.htm

			// Default plotting in base class. Uncomment if indicators holds at least one plot
			// in this case we would expect NOT to see the SMA plot we have as well in this sample script
			//base.OnRender(chartControl, chartScale);

			// 1.1 - SharpDX Vectors and Charting RenderTarget Coordinates

			// The SharpDX SDK uses "Vector2" objects to describe a two-dimensional point of a device (X and Y coordinates)

			// For our custom script, we need a way to determine the Chart's RenderTarget coordinates to draw our custom shapes
			// This info can be found within the NinjaTrader.Gui.ChartPanel class.
			// You can also use various chartScale and chartControl members to calculate values relative to time and price
			// However, those concepts will not be discussed or used in this sample
			// Notes:  RenderTarget is always the full ChartPanel, so we need to be mindful which sub-ChartPanel we're dealing with
			// Always use ChartPanel X, Y, W, H - as chartScale and chartControl properties WPF units, so they can be drastically different depending on DPI set
			SharpDX.Vector2 startPoint	= new(ChartPanel.X, ChartPanel.Y);
			SharpDX.Vector2 endPoint	= new(ChartPanel.X + ChartPanel.W, ChartPanel.Y + ChartPanel.H);

			// These Vector2 objects are equivalent with WPF System.Windows.Point and can be used interchangeably depending on your requirements
			// For convenience, NinjaTrader provides a "ToVector2()" extension method to convert from WPF Points to SharpDX.Vector2
			SharpDX.Vector2 startPoint1	= new System.Windows.Point(ChartPanel.X, ChartPanel.Y + ChartPanel.H).ToVector2();
			SharpDX.Vector2 endPoint1	= new System.Windows.Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y).ToVector2();

			// SharpDX.Vector2 objects contain X/Y properties which are helpful to recalculate new properties based on the initial vector
			float width		= endPoint.X - startPoint.X;
			float height	= endPoint.Y - startPoint.Y;

			// Or you can recalculate a new vector from existing vector objects
			SharpDX.Vector2 center = (startPoint + endPoint) / 2;

			// Tip: This check is simply added to prevent the Indicator dialog menu from opening as a user clicks on the chart
			// The default behavior is to open the Indicator dialog menu if a user double clicks on the indicator
			// (i.e, the indicator falls within the RenderTarget "hit testing")
			// You can remove this check if you want the default behavior implemented
			if (!IsInHitTest)
			{
				// 1.2 - SharpDX Brush Resources

				// RenderTarget commands must use a special brush resource defined in the SharpDX.Direct2D1 namespace
				// These resources exist just like you will find in the WPF/Windows.System.Media namespace
				// such as SolidColorBrushes, LienarGraidentBrushes, RadialGradientBrushes, etc.
				// To begin, we will start with the most basic "Brush" type
				// Warning:  Brush objects must be disposed of after they have been used
				// for convenience, you can simply convert a WPF Brush to a DXBrush using the ToDxBrush() extension method provided by NinjaTrader
				// This is a common approach if you have a Brush property created e.g., on the UI you wish to use in custom rendering routines
				SharpDX.Direct2D1.Brush areaBrushDx			= areaBrush.ToDxBrush(RenderTarget);
				SharpDX.Direct2D1.Brush smallAreaBrushDx	= SmallAreaBrush.ToDxBrush(RenderTarget);
				SharpDX.Direct2D1.Brush textBrushDx			= TextBrush.ToDxBrush(RenderTarget);

				// However - it should be noted that this conversion process can be rather expensive
				// If you have many brushes being created, and are not tied to WPF resources
				// You should rather favor creating the SharpDX Brush directly:
				// Warning:  SolidColorBrush objects must be disposed of after they have been used
				SharpDX.Direct2D1.SolidColorBrush customDXBrush = new(RenderTarget, SharpDX.Color.DodgerBlue);

				// 1.3 - Using The RenderTarget
				// before executing chart commands, you have the ability to describe how the RenderTarget should render
				// for example, we can store the existing RenderTarget AntialiasMode mode
				// then update the AntialiasMode to be the quality of non-text primitives are rendered
				SharpDX.Direct2D1.AntialiasMode oldAntialiasMode = RenderTarget.AntialiasMode;
				RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;

				// Note: The code above stores the oldAntialiasMode as a best practices
				// i.e., if you plan on changing a property of the RenderTarget, you should plan to set it back
				// This is to make sure your requirements to no interfere with the function of another script
				// Additionally smoothing has some performance impacts

				// Once you have defined all the necessary requirements for you object
				//  You can execute a command on the RenderTarget to draw specific shapes
				// e.g., we can now use the RenderTarget's DrawLine() command to render a line
				// using the start/end points and areaBrushDx objects defined before
				RenderTarget.DrawLine(startPoint, endPoint, areaBrushDx, 4);

				// Since rendering occurs in a sequential fashion, after you have executed a command
				// you can switch a property of the RenderTarget to meet other requirements
				// For example, we can draw a second line now which uses a different AntialiasMode
				// and the changes render on the chart for both lines from the time they received commands
				RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
				RenderTarget.DrawLine(startPoint1, endPoint1, areaBrushDx, 4);

				// 1.4 - Rendering Custom Shapes

				// SharpDX namespace consists of several shapes you can use to draw objects more complicated than lines
				// For example, we can use the RectangleF object to draw a rectangle that covers the entire chart area
				SharpDX.RectangleF rect = new(startPoint.X, startPoint.Y, width, height);

				// The RenderTarget consists of two commands related to Rectangles.
				// The FillRectangle() method is used to "Paint" the area of a Rectangle
				RenderTarget.FillRectangle(rect, areaBrushDx);

				// and DrawRectangle() is used to "Paint" the outline of a Rectangle
				RenderTarget.DrawRectangle(rect, customDXBrush, 2);

				// Another example is an ellipse which can be used to draw circles
				// The ellipse center point can be used from the Vectors calculated earlier
				// The width and height an absolute 100 device pixels
				// To ensure that pixel coordinates work across all DPI devices, we use the NinjaTrader ChartingExteions methods
				// Which will convert the "100" value from WPF pixels to Device Pixels both vertically and horizontally
				int ellipseRadiusY = ChartingExtensions.ConvertToVerticalPixels(100, ChartControl.PresentationSource);
				int ellipseRadiusX = ChartingExtensions.ConvertToHorizontalPixels(100, ChartControl.PresentationSource);

				SharpDX.Direct2D1.Ellipse ellipse = new(center, ellipseRadiusX, ellipseRadiusY);

				// 1.5 - Complex Brush Types and Shapes
				// For this ellipse, we can use one of the more complex brush types "RadialGradientBrush"
				// Warning:  RadialGradientBrush objects must be disposed of after they have been used

				// However creating a RadialGradientBrush requires a few more properties than SolidColorBrush
				// First, you need to define the array gradient stops the brush will eventually use
				SharpDX.Direct2D1.GradientStop[] gradientStops = new SharpDX.Direct2D1.GradientStop[2];

				// With the gradientStops array, we can describe the color and position of the individual gradients
				gradientStops[0].Color		= SharpDX.Color.Goldenrod;
				gradientStops[0].Position	= 0.0f;
				gradientStops[1].Color		= SharpDX.Color.SeaGreen;
				gradientStops[1].Position	= 1.0f;

				// then declare a GradientStopCollection from our render target that uses the gradientStops array defined just before
				// Warning:  GradientStopCollection objects must be disposed of after they have been used
				SharpDX.Direct2D1.GradientStopCollection gradientStopCollection = new(RenderTarget, gradientStops);

				// we also need to tell our RadialGradientBrush to match the size and shape of the ellipse that we will be drawing
				// for convenience, SharpDX provides a RadialGradientBrushProperties structure to help define these properties
				SharpDX.Direct2D1.RadialGradientBrushProperties radialGradientBrushProperties = new()
				{
					GradientOriginOffset = new SharpDX.Vector2(0, 0),
					Center = ellipse.Point,
					RadiusX = ellipse.RadiusY,
					RadiusY = ellipse.RadiusY
				};

				// we now have everything we need to create a radial gradient brush
				SharpDX.Direct2D1.RadialGradientBrush radialGradientBrush = new(RenderTarget, radialGradientBrushProperties,
					gradientStopCollection);

				// Finally, we can use this radialGradientBrush to "Paint" the area of the ellipse
				RenderTarget.FillEllipse(ellipse, radialGradientBrush);

				// 1.6 - Simple Text Rendering

				// For rendering custom text to the Chart, there are a few ways you can approach depending on your requirements
				// The most straight forward way is to "borrow" the existing chartControl font provided as a "SimpleFont" class
				// Using the chartControl LabelFont, your custom object will also change to the user defined properties allowing
				// your object to match different fonts if defined by user.

				// The code below will use the chartControl Properties Label Font if it exists,
				// or fall back to a default property if it cannot obtain that value
				Gui.Tools.SimpleFont simpleFont = chartControl.Properties.LabelFont ??  new Gui.Tools.SimpleFont("Arial", 12);

				// the advantage of using a SimpleFont is they are not only very easy to describe
				// but there is also a convenience method which can be used to convert the SimpleFont to a SharpDX.DirectWrite.TextFormat used to render to the chart
				// Warning:  TextFormat objects must be disposed of after they have been used
				TextFormat textFormat1 = simpleFont.ToDirectWriteTextFormat();

				// Once you have the format of the font, you need to describe how the font needs to be laid out
				// Here we will create a new Vector2() which draws the font according to the to top left corner of the chart (offset by a few pixels)
				SharpDX.Vector2 upperTextPoint = new(ChartPanel.X + 10, ChartPanel.Y + 20);
				// Warning:  TextLayout objects must be disposed of after they have been used
				TextLayout textLayout1 = new(Core.Globals.DirectWriteFactory, Custom.Resource.SampleCustomPlotUpperLeftCorner, textFormat1, ChartPanel.X + ChartPanel.W, textFormat1.FontSize);

				// With the format and layout of the text completed, we can now render the font to the chart
				RenderTarget.DrawTextLayout(upperTextPoint, textLayout1, textBrushDx, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

				// 1.7 - Advanced Text Rendering

				// Font formatting and text layouts can get as complex as you need them to be
				// This example shows how to use a complete custom font unrelated to the existing user-defined chart control settings
				// Warning:  TextLayout and TextFormat objects must be disposed of after they have been used
				TextFormat textFormat2 = new(Core.Globals.DirectWriteFactory, "Century Gothic", FontWeight.Bold, FontStyle.Italic, 32f);
				TextLayout textLayout2 = new(Core.Globals.DirectWriteFactory, Custom.Resource.SampleCustomPlotLowerRightCorner, textFormat2, 400, textFormat1.FontSize);

				// the textLayout object provides a way to measure the described font through a "Metrics" object
				// This allows you to create new vectors on the chart which are entirely dependent on the "text" that is being rendered
				// For example, we can create a rectangle that surrounds our font based off the textLayout which would dynamically change if the text used in the layout changed dynamically
				SharpDX.Vector2 lowerTextPoint = new(ChartPanel.W - textLayout2.Metrics.Width - 5, ChartPanel.Y + (ChartPanel.H - textLayout2.Metrics.Height));
				SharpDX.RectangleF rect1 = new(lowerTextPoint.X, lowerTextPoint.Y, textLayout2.Metrics.Width, textLayout2.Metrics.Height);

				// We can draw the Rectangle based on the TextLayout used above
				RenderTarget.FillRectangle(rect1, smallAreaBrushDx);
				RenderTarget.DrawRectangle(rect1, smallAreaBrushDx, 2);

				// And render the advanced text layout using the DrawTextLayout() method
				// Note:  When drawing the same text repeatedly, using the DrawTextLayout() method is more efficient than using the DrawText()
				// because the text doesn't need to be formatted and the layout processed with each call
				RenderTarget.DrawTextLayout(lowerTextPoint, textLayout2, textBrushDx, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

				// 1.8 - Cleanup
				// This concludes all of the rendering concepts used in the sample
				// However - there are some final clean up processes we should always provided before we are done

				// If changed, do not forget to set the AntialiasMode back to the default value as described above as a best practice
				RenderTarget.AntialiasMode = oldAntialiasMode;

				// We also need to make sure to dispose of every device dependent resource on each render pass
				// Failure to dispose of these resources will eventually result in unnecessary amounts of memory being used on the chart
				// Although the effects might not be obvious as first, if you see issues related to memory increasing over time
				// Objects such as these should be inspected first
				areaBrushDx.Dispose();
				customDXBrush.Dispose();
				gradientStopCollection.Dispose();
				radialGradientBrush.Dispose();
				smallAreaBrushDx.Dispose();
				textBrushDx.Dispose();
				textFormat1.Dispose();
				textFormat2.Dispose();
				textLayout1.Dispose();
				textLayout2.Dispose();
			}
		}

		#region Properties
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush AreaBrush
		{
			get => areaBrush;
			set
			{
				areaBrush = value;
				if (areaBrush != null)
				{
					if (areaBrush.IsFrozen)
						areaBrush = areaBrush.Clone();
					areaBrush.Opacity = areaOpacity / 100d;
					areaBrush.Freeze();
				}
			}
		}

		[Browsable(false)]
		public string AreaBrushSerialize
		{
			get => Serialize.BrushToString(AreaBrush);
			set => AreaBrush = Serialize.StringToBrush(value);
		}

		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral")]
		public int AreaOpacity
		{
			get => areaOpacity;
			set
			{
				areaOpacity = Math.Max(0, Math.Min(100, value));
				if (areaBrush != null)
				{
					System.Windows.Media.Brush newBrush		= areaBrush.Clone();
					newBrush.Opacity	= areaOpacity / 100d;
					newBrush.Freeze();
					areaBrush			= newBrush;
				}
			}
		}

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "SmallAreaColor", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush SmallAreaBrush { get; set; }

		[Browsable(false)]
		public string SmallAreaBrushSerialize
		{
			get => Serialize.BrushToString(SmallAreaBrush);
			set => SmallAreaBrush = Serialize.StringToBrush(value);
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> TestPlot => Values[0];

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "TextColor", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush TextBrush { get; set; }

		[Browsable(false)]
		public string TextBrushSerialize
		{
			get => Serialize.BrushToString(TextBrush);
			set => TextBrush = Serialize.StringToBrush(value);
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SampleCustomRender[] cacheSampleCustomRender;
		public SampleCustomRender SampleCustomRender()
		{
			return SampleCustomRender(Input);
		}

		public SampleCustomRender SampleCustomRender(ISeries<double> input)
		{
			if (cacheSampleCustomRender != null)
				for (int idx = 0; idx < cacheSampleCustomRender.Length; idx++)
					if (cacheSampleCustomRender[idx] != null &&  cacheSampleCustomRender[idx].EqualsInput(input))
						return cacheSampleCustomRender[idx];
			return CacheIndicator<SampleCustomRender>(new SampleCustomRender(), input, ref cacheSampleCustomRender);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SampleCustomRender SampleCustomRender()
		{
			return indicator.SampleCustomRender(Input);
		}

		public Indicators.SampleCustomRender SampleCustomRender(ISeries<double> input )
		{
			return indicator.SampleCustomRender(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SampleCustomRender SampleCustomRender()
		{
			return indicator.SampleCustomRender(Input);
		}

		public Indicators.SampleCustomRender SampleCustomRender(ISeries<double> input )
		{
			return indicator.SampleCustomRender(input);
		}
	}
}

#endregion
