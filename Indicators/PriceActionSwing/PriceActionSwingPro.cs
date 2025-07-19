// #############################################################
// #														   #
// #                    PriceActionSwingPro                    #
// #														   #
// #     05.08.2022 by dorschden, die.unendlichkeit@gmx.de     #
// #														   #
// #              Comments are highly appreciated.             #
// #														   #
// #                 License: CC BY-NC-SA 4.0                  #
// #    https://creativecommons.org/licenses/by-nc-sa/4.0/     #
// #														   #
// #                 Rendering by traderpards                  #
// #														   #
// #############################################################

// TODO add naked swing lines for current bar
// TODO improve divergence performance

#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.PriceActionSwing;
using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.PriceActionSwing
{
    #region Categories definitions
    [CategoryOrder("Parameters", 0)]
    [CategoryOrder("Features", 10)]
    [CategoryOrder("Swing Values", 20)]
    [CategoryOrder("Swing Visualization", 30)]
    [CategoryOrder("Dot Visualization", 40)]
    [CategoryOrder("ABC Visualization", 50)]
    [CategoryOrder("ABC Alerts", 60)]
    [CategoryOrder("Divergence Visualization", 70)]
    [CategoryOrder("Naked Swings", 80)]
    [CategoryOrder("Statistics", 90)]
    [CategoryOrder("Data Series", 100)]
    [CategoryOrder("Setup", 110)]
    [CategoryOrder("Visual", 120)]
    [CategoryOrder("Plots", 130)]
    #endregion

    [TypeConverter("NinjaTrader.NinjaScript.Indicators.PriceActionSwing.PriceActionSwingProConverter")]
    /// <summary>
    /// The PriceActionSwing indicator for NinjaTrader 8 calculates swings in different ways and visualizes them. 
    /// It shows swing information like the swing length, duration, volume and many more.
    /// It contains a lot of features. It is also usable in the Market Analyzer.
    /// </summary>
    public class PriceActionSwingPro : Indicator
    {
        #region Swing calculation member
        private PriceActionSwingBase swingCalculation;
        public PriceActionSwingPro()
        {
            swingCalculation = new PriceActionSwingBase(this);
        }
        #endregion

        #region Fields
        //=========================================================================================
        #region Naked swings
        private SortedList<double, int> nakedcurHighSwingsList = new SortedList<double, int>();
        private SortedList<double, int> nakedcurLowSwingsList = new SortedList<double, int>();
        private readonly List<OldNakeds> oldNakedLows = new List<OldNakeds>();
        private readonly List<OldNakeds> oldNakedHighs = new List<OldNakeds>();
        #endregion

        #region ABC
        private bool abcLongChanceInProgress = false;
        private bool abcShortChanceInProgress = false;
        private readonly double retracementEntryValue = 38.0;
        private double entryLevel = 0.0;
        private int entryLineStartBar = 0;
        private int tmpCounter = 0;
        private int lastABC = 0;

        private int alertTag = 0;
        private Series<double> EntryLong;
        private Series<double> EntryShort;
        private Series<double> EntryLevelLine;
        private Series<ABC> ABCPlots;

        private int abcEntryTag = 0;
        private double ABCEntryArrowYTickOffsetValue = 0;
        #endregion

        #region Divergence
        private Series<double> DivergenceDataHigh;
        private Series<double> DivergenceDataLow;
        private readonly List<Divergences> DivergenceLines = new List<Divergences>();

        private double divLastSwing = 0.0;
        private double divLastOscValue = 0.0;
        private double divCurSwing = 0.0;
        private double divCurOscValue = 0.0;
        private bool divHiddenShortActive = false;
        private bool divRegularShortActive = false;
        private bool divHiddenLongActive = false;
        private bool divRegularLongActive = false;

        private int drawTagDivUp = 0;
        private int drawTagDivDn = 0;

        private Stochastics stochastics;
        private MACD macd;
        #endregion

        #region Statistic
        //===================================================================
        private double overallAvgDnLength = 0;
        private double overallAvgUpLength = 0;
        private double overallUpLength = 0;
        private double overallDnLength = 0;
        private double overallAvgDnDuration = 0;
        private double overallAvgUpDuration = 0;
        private double overallUpDuration = 0;
        private double overallDnDuration = 0;

        private double avgUpLength = 0;
        private double avgDnLength = 0;
        private double upLength = 0;
        private double dnLength = 0;
        private double avgUpDuration = 0;
        private double avgDnDuration = 0;
        private double upDuration = 0;
        private double dnDuration = 0;

        // Variables for the swing to swing relation statistic
        private int hhCount = 0;
        private int hhCountHH = 0;
        private double hhCountHHPercent = 0;
        private int hhCountLH = 0;
        private double hhCountLHPercent = 0;
        private int hhCountDT = 0;
        private double hhCountDTPercent = 0;
        private int hhCountHL = 0;
        private double hhCountHLPercent = 0;
        private int hhCountLL = 0;
        private double hhCountLLPercent = 0;
        private int hhCountDB = 0;
        private double hhCountDBPercent = 0;

        private int lhCount = 0;
        private int lhCountHH = 0;
        private double lhCountHHPercent = 0;
        private int lhCountLH = 0;
        private double lhCountLHPercent = 0;
        private int lhCountDT = 0;
        private double lhCountDTPercent = 0;
        private int lhCountHL = 0;
        private double lhCountHLPercent = 0;
        private int lhCountLL = 0;
        private double lhCountLLPercent = 0;
        private int lhCountDB = 0;
        private double lhCountDBPercent = 0;

        private int dtCount = 0;
        private int dtCountHH = 0;
        private double dtCountHHPercent = 0;
        private int dtCountLH = 0;
        private double dtCountLHPercent = 0;
        private int dtCountDT = 0;
        private double dtCountDTPercent = 0;
        private int dtCountHL = 0;
        private double dtCountHLPercent = 0;
        private int dtCountLL = 0;
        private double dtCountLLPercent = 0;
        private int dtCountDB = 0;
        private double dtCountDBPercent = 0;

        private int llCount = 0;
        private int llCountHH = 0;
        private double llCountHHPercent = 0;
        private int llCountLH = 0;
        private double llCountLHPercent = 0;
        private int llCountDT = 0;
        private double llCountDTPercent = 0;
        private int llCountHL = 0;
        private double llCountHLPercent = 0;
        private int llCountLL = 0;
        private double llCountLLPercent = 0;
        private int llCountDB = 0;
        private double llCountDBPercent = 0;

        private int hlCount = 0;
        private int hlCountHH = 0;
        private double hlCountHHPercent = 0;
        private int hlCountLH = 0;
        private double hlCountLHPercent = 0;
        private int hlCountDT = 0;
        private double hlCountDTPercent = 0;
        private int hlCountHL = 0;
        private double hlCountHLPercent = 0;
        private int hlCountLL = 0;
        private double hlCountLLPercent = 0;
        private int hlCountDB = 0;
        private double hlCountDBPercent = 0;

        private int dbCount = 0;
        private int dbCountHH = 0;
        private double dbCountHHPercent = 0;
        private int dbCountLH = 0;
        private double dbCountLHPercent = 0;
        private int dbCountDT = 0;
        private double dbCountDTPercent = 0;
        private int dbCountHL = 0;
        private double dbCountHLPercent = 0;
        private int dbCountLL = 0;
        private double dbCountLLPercent = 0;
        private int dbCountDB = 0;
        private double dbCountDBPercent = 0;

        private int upCount = 0;
        private int dnCount = 0;
        //===================================================================
        #endregion

        #region Visualize
        private int lowTextOffset;
        private int lowTextOffsetLength;
        private int lowTextOffsetPercent;
        private int lowTextOffsetLabel;
        private int lowTextOffsetTime;
        private int lowTextOffsetVolume;
        private double SwingSwitchYTickOffsetValue = 0;
        #endregion
        //=========================================================================================
        #endregion

        #region Class objects and DataSeries
        #region public class ABC 
        public class ABC
        {
            // ABC triangle point variables
            public int aBar;
            public double aY;
            public int bBar;
            public double bY;
            public int cBar;
            public double cY;
            public Brush lineColor;
        }
        #endregion

        #region public class OldNakeds
        public class OldNakeds
        {
            public int FromIdx { get; set; }
            public int ToIdx { get; set; }
            public double Price { get; set; }

            public OldNakeds(int inFromIdx, int inToIdx, double inPrice)
            {
                FromIdx = inFromIdx;
                ToIdx = inToIdx;
                Price = inPrice;
            }
        }
        #endregion

        #region class Divergence
        public class Divergences
        {
            public int Id { get; set; }
            public int StartIdx { get; set; }
            public double StartY { get; set; }
            public int EndIdx { get; set; }
            public double EndY { get; set; }
            public DivergenceType DivType { get; set; }  // Regular or hidden
            public DivergenceBias DivBias { get; set; }
        }
        #endregion
        #endregion

        #region Statistic fields and wpf controls

        private bool setInitialValues = false;
        private bool showPanel = true;
        private const double statisticWidth = 330;

        #region Chart object variables
        private System.Windows.Controls.Grid chartGrid;
        private NinjaTrader.Gui.Chart.ChartTab chartTab;
        private NinjaTrader.Gui.Chart.Chart chartWindow;
        private int tabControlStartColumn;
        private System.Windows.Controls.TabItem tabItem;
        private System.Windows.Style mainMenuItemStyle, systemMenuStyle;

        // Statistic panel
        private Grid rightSidePanelGrid;
        private GridSplitter rightSidePanelGridSplitter;
        private DataGrid lengthGrid, relationGrid;
        private bool rightSidePanelActive;

        // Menu item
        private bool ntBarActive;
        private Menu ntBarMenu;
        private NinjaTrader.Gui.Tools.NTMenuItem ntBartopItem;
        #endregion

        #region Class objects
        /// <summary>
        /// Used for the headers of the statistic tables.
        /// </summary>
        public struct HeaderBinding
        {
            public String header;
            public String binding;
            public bool rightBound;

            public HeaderBinding(String header, String binding)
            {
                this.header = header;
                this.binding = binding;
                this.rightBound = false;
            }

            public HeaderBinding(String header, String binding, bool rightBound)
            {
                this.header = header;
                this.binding = binding;
                this.rightBound = rightBound;
            }
        }

        /// <summary>
        /// This is the class containing the data.
        /// </summary>
        public class SwingStats
        {
            public string type { set; get; } // like "High" or "Low"
            public int count { set; get; }
            public double avgLength { set; get; }
            public double curLength { set; get; }
            public double avgDuration { set; get; }
            public double curDuration { set; get; }

            public SwingStats(string type, int count, double avgLength, double curLength,
                double avgDuration, double curDuration)
            {
                this.type = type;
                this.count = count;
                this.avgLength = avgLength;
                this.curLength = curLength;
                this.avgDuration = avgDuration;
                this.curDuration = curDuration;
            }
        }

        // this is the array containing the header information for the length table
        public HeaderBinding[] swingStatsHeaders = {
            new HeaderBinding("Swing", "type"),
            new HeaderBinding("Count", "count", true),
            new HeaderBinding("Average\nLength", "avgLength", true),
            new HeaderBinding("Current\nLength", "curLength", true),
            new HeaderBinding("Average\nDuration", "avgDuration", true),
            new HeaderBinding("Current\nDuration", "curDuration", true)
        };

        /// <summary>
        /// This class contains the swing relation data.
        /// </summary>
        public class SwingRelationClassicfifaction
        {
            public string swingType { set; get; } // like "HH" or "LH"
            public int count { set; get; }
            public string followingHH { set; get; }
            public string followingLH { set; get; }
            public string followingDT { set; get; }
            public string followingHL { set; get; }
            public string followingLL { set; get; }
            public string followingDB { set; get; }

            public SwingRelationClassicfifaction(string swingType, int count, string followingHH,
                string followingLH, string followingDT, string followingHL, string followingLL,
                string followingDB)
            {
                this.swingType = swingType;
                this.count = count;
                this.followingHH = followingHH;
                this.followingLH = followingLH;
                this.followingDT = followingDT;
                this.followingHL = followingHL;
                this.followingLL = followingLL;
                this.followingDB = followingDB;
            }
        }

        // this is the array containing the header information for the swing relation table.
        public HeaderBinding[] swingRelationStatsHeaders = {
            new HeaderBinding("Swing", "swingType"),
            new HeaderBinding("Count", "count", true),
            new HeaderBinding("HH %", "followingHH", true),
            new HeaderBinding("LH %", "followingLH", true),
            new HeaderBinding("DT %", "followingDT", true),
            new HeaderBinding("HL %", "followingHL", true),
            new HeaderBinding("LL %", "followingLL", true),
            new HeaderBinding("DB %", "followingDB", true)
        };

        private List<SwingStats> swingStatsList = new List<SwingStats>();
        private List<SwingRelationClassicfifaction> swingRelationList = new List<SwingRelationClassicfifaction>();
        #endregion

        #region Statistic WPF controls
        #region CreateWPFControls
        protected void CreateWPFControls()
        {
            // the main chart window
            chartWindow = System.Windows.Window.GetWindow(ChartControl.Parent) as Chart;
            // if not added to a chart, do nothing
            if (chartWindow == null)
                return;
            // this is the grid in the chart window
            chartGrid = chartWindow.MainTabControl.Parent as Grid;

            #region Right side panel wpf objects
            rightSidePanelGrid = new Grid();

            // this gridsplitter will allow the column our grid is in to be resized
            rightSidePanelGridSplitter = new GridSplitter()
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left,
                ResizeBehavior = GridResizeBehavior.BasedOnAlignment,
                ResizeDirection = GridResizeDirection.Columns,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 6
            };

            // Right alignment
            Style s = new Style();
            s.Setters.Add(new Setter(DataGridCell.HorizontalAlignmentProperty, HorizontalAlignment.Right));
            s.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));

            rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20) });
            rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(72) });

            lengthGrid = new DataGrid()
            {
                MinHeight = 72,
                MinWidth = 309,
                MaxWidth = 309,
                CanUserSortColumns = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                CanUserResizeColumns = false,
                IsReadOnly = true,
            };

            // set the source
            lengthGrid.AutoGenerateColumns = false;
            lengthGrid.ItemsSource = swingStatsList;

            for (int i = 0; i < swingStatsHeaders.Length; i++)
            {
                DataGridTextColumn col = new DataGridTextColumn();
                col.Binding = new Binding(swingStatsHeaders[i].binding);
                col.Header = swingStatsHeaders[i].header;
                if (swingStatsHeaders[i].rightBound == true)
                {
                    // set data values to right alignment
                    col.ElementStyle = s;
                }
                lengthGrid.Columns.Add(col);
            }
            Grid.SetRow(lengthGrid, 1);
            rightSidePanelGrid.Children.Add(lengthGrid);
            //#####################################################################################
            rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20) });
            rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(126) });

            relationGrid = new DataGrid()
            {
                MinHeight = 126,
                MinWidth = statisticWidth,
                MaxWidth = statisticWidth,
                CanUserSortColumns = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                CanUserResizeColumns = false,
                IsReadOnly = true,
                ToolTip = "Read per row. E.g. for the last row: double bottoms are followed in 100 % of the time by a higher high."
            };

            // set the source
            relationGrid.AutoGenerateColumns = false;
            relationGrid.ItemsSource = swingRelationList;

            for (int i = 0; i < swingRelationStatsHeaders.Length; i++)
            {
                DataGridTextColumn col = new DataGridTextColumn();
                col.Binding = new Binding(swingRelationStatsHeaders[i].binding);
                col.Header = swingRelationStatsHeaders[i].header;
                if (swingRelationStatsHeaders[i].rightBound == true)
                {
                    // set data values to right alignment
                    col.ElementStyle = s;
                }
                relationGrid.Columns.Add(col);
            }
            Grid.SetRow(relationGrid, 3);
            rightSidePanelGrid.Children.Add(relationGrid);
            rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20) });
            #endregion

            #region Custom item added to titlebar (NTBar) wpf objects
            // this is the actual object that you add to the chart windows Main Menu
            // which will act as a container for all the menu items
            ntBarMenu = new Menu
            {
                // important to set the alignment, otherwise you will never see the menu populated
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Top,
                // make sure to style as a System Menu	
                Style = systemMenuStyle
            };

            System.Windows.Media.Geometry topItem1Icon = Geometry.Parse("F1 M 30.25,58L 18,58L 18,45.75L 22,41.75L 22,50.75L 30,42.75L 33.25,46L 25.25,54L 34.25,54L 30.25,58 Z M 58,45.75L 58,58L 45.75,58L 41.75,54L 50.75,54L 42.75,46L 46,42.75L 54,50.75L 54,41.75L 58,45.75 Z M 45.75,18L 58,18L 58,30.25L 54,34.25L 54,25.25L 46,33.25L 42.75,30L 50.75,22L 41.75,22L 45.75,18 Z M 18,30.25L 18,18L 30.25,18L 34.25,22L 25.25,22L 33.25,30L 30,33.25L 22,25.25L 22,34.25L 18,30.25 Z");

            // this is the menu item which will appear on the chart's Main Menu
            ntBartopItem = new NTMenuItem()
            {
                // comment out or delete the Header assignment below to only show the icon
                Header = "Hide Statistic",
                Icon = topItem1Icon,
                Margin = new Thickness(0),
                Padding = new Thickness(1),
                Style = mainMenuItemStyle,
                VerticalAlignment = VerticalAlignment.Center
            };

            ntBartopItem.Click += NTBarMenuItemClick;
            ntBarMenu.Items.Add(ntBartopItem);
            #endregion

            if (TabSelected())
                ShowWPFControls();

            chartWindow.MainTabControl.SelectionChanged += TabChangedHandler;
        }
        #endregion

        #region DisposeWPFControls
        /// <summary>
        /// remove handlers / dispose objects
        /// </summary>
        private void DisposeWPFControls()
        {
            if (chartWindow != null)
                chartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;

            HideWPFControls();

            if (ntBarMenu != null)
            {
                ntBartopItem.Click -= NTBarMenuItemClick;
                chartWindow.MainMenu.Remove(ntBarMenu);
                ntBarActive = false;
            }
        }
        #endregion

        #region LoadBrushesFromSkin
        private void LoadBrushesFromSkin()
        {
            // while pulling brushes from a skin to use later in the chart,
            // sometimes we need to be in the thread of the chart when the brush is initialized
            mainMenuItemStyle = Application.Current.TryFindResource("MainMenuItem") as Style;
            systemMenuStyle = Application.Current.TryFindResource("SystemMenuStyle") as Style;
        }
        #endregion

        #region ShowWPFControls
        /// <summary>
        /// insert controls
        /// </summary>
        private void ShowWPFControls()
        {
            if (showPanel == true)
                ShowStatistic();

            if (!ntBarActive)
            {
                chartWindow.MainMenu.Add(ntBarMenu);
                ntBarActive = true;
            }
        }
        #endregion

        #region HideWPFControls
        /// <summary>
        /// remove controls
        /// </summary>
        private void HideWPFControls()
        {
            HideStatistic();

            if (ntBarActive)
            {
                chartWindow.MainMenu.Remove(ntBarMenu);
                ntBarActive = false;
            }
        }
        #endregion

        #region ShowStatistic
        private void ShowStatistic()
        {
            if (!rightSidePanelActive)
            {
                tabControlStartColumn = Grid.GetColumn(chartWindow.MainTabControl);

                double chartWindowWidth = chartWindow.Width;
                double gridWidth;
                if (chartWindowWidth < 550)
                {
                    gridWidth = chartWindowWidth / 4;
                }
                else
                {
                    gridWidth = statisticWidth;
                }

                // a new column is added to the right of MainTabControl
                chartGrid.ColumnDefinitions.Insert((tabControlStartColumn + 1), new ColumnDefinition()
                {
                    // The width will need to be GridUnitType.Star to work with the gridsplitter from chartTrader (as well as our own)
                    // The width set here is a ratio to other star columns (such as when we make the chart column starred below)
                    Width = new GridLength(1, GridUnitType.Star),
                    // the minimum width should at least be big enough to grab our added gridspliiter with mouse
                    MinWidth = gridWidth,
                    MaxWidth = statisticWidth,
                });

                // all items to the right of the MainTabControl are shifted to the right
                for (int i = 0; i < chartGrid.Children.Count; i++)
                    if (Grid.GetColumn(chartGrid.Children[i]) > tabControlStartColumn)
                        Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) + 1);

                // and then we set our new grid to be within the new column of the chart grid (and on the same row as the MainTabControl)
                Grid.SetColumn(rightSidePanelGrid, Grid.GetColumn(chartWindow.MainTabControl) + 1);
                Grid.SetRow(rightSidePanelGrid, Grid.GetRow(chartWindow.MainTabControl));

                chartGrid.Children.Add(rightSidePanelGrid);

                // add a grid splitter to the same column as our side panel grid to allow us to resize the width of our panel
                Grid.SetColumn(rightSidePanelGridSplitter, Grid.GetColumn(rightSidePanelGrid));
                Grid.SetRow(rightSidePanelGridSplitter, Grid.GetRow(rightSidePanelGrid));

                chartGrid.Children.Add(rightSidePanelGridSplitter);

                // to work with the added gridsplitter, the chart column to the left needs to be width star and larger than our panel
                chartGrid.ColumnDefinitions[Grid.GetColumn(chartWindow.MainTabControl)].Width = new GridLength(5, GridUnitType.Star);

                // let the script know the panel is active
                rightSidePanelActive = true;
            }
        }
        #endregion

        #region HideStatistic
        private void HideStatistic()
        {
            if (rightSidePanelActive)
            {
                // remove the column of our added grid
                chartGrid.ColumnDefinitions.RemoveAt(Grid.GetColumn(rightSidePanelGrid));
                // then remove the grid and gridsplitter
                chartGrid.Children.Remove(rightSidePanelGrid);
                chartGrid.Children.Remove(rightSidePanelGridSplitter);

                // if the childs column is 1 (so we can move it to 0) and the column is to the right of the column we are removing, shift it left
                for (int i = 0; i < chartGrid.Children.Count; i++)
                    if (Grid.GetColumn(chartGrid.Children[i]) > 0 && Grid.GetColumn(chartGrid.Children[i]) > Grid.GetColumn(rightSidePanelGrid))
                        Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) - 1);

                rightSidePanelActive = false;
            }
        }
        #endregion

        #region NTBarMenuItemClick
        /// <summary>
        /// Custom item added to titlebar (NTBar) click handler methods
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void NTBarMenuItemClick(object sender, RoutedEventArgs eventArgs)
        {
            if (rightSidePanelActive)
            {
                HideStatistic();
                showPanel = false;
                ntBartopItem.Header = "Show statistic";
            }
            else if (!rightSidePanelActive)
            {
                ShowStatistic();
                showPanel = true;
                ntBartopItem.Header = "Hide statistic";
            }

            ForceRefresh();
        }
        #endregion

        #region TabSelected
        private bool TabSelected()
        {
            if (ChartControl == null || chartWindow == null || chartWindow.MainTabControl == null)
                return false;

            bool tabSelected = false;

            if (ChartControl.ChartTab == ((chartWindow.MainTabControl.Items.GetItemAt(chartWindow.MainTabControl.SelectedIndex) as TabItem).Content as ChartTab))
                tabSelected = true;

            return tabSelected;
        }
        #endregion

        #region TabChangedHandler
        /// <summary>
        /// Runs ShowWPFControls if this is the selected chart tab, other wise runs HideWPFControls()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabChangedHandler(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            tabItem = e.AddedItems[0] as TabItem;
            if (tabItem == null)
                return;

            chartTab = tabItem.Content as ChartTab;
            if (chartTab == null)
                return;

            if (TabSelected())
                ShowWPFControls();
            else
                HideWPFControls();
        }
        #endregion
        #endregion
        #endregion

        #region OnStateChange
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"The PriceActionSwing indicator for NinjaTrader 8 calculates swings in different ways and visualizes them. It shows swing information like the swing length, duration, volume and many more. It contains a lot of features. It is also usable in the Market Analyzer.";
                Name = "PriceActionSwingPro";
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

                AddPlot(Brushes.Transparent, "AbcSignals");
                AddPlot(Brushes.Transparent, "DivergenceSignals");
                AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Square, "GannSwing");
                AddPlot(new Stroke(Brushes.Green, 7), PlotStyle.TriangleUp, "SwingSwitchUp");
                AddPlot(new Stroke(Brushes.Red, 7), PlotStyle.TriangleDown, "SwingSwitchDown");
                AddPlot(new Stroke(Brushes.LimeGreen, 10), PlotStyle.TriangleUp, "ABCEntryArrowUp");
                AddPlot(new Stroke(Brushes.OrangeRed, 10), PlotStyle.TriangleDown, "ABCEntryArrowDown");

                SwingStyleType = SwingStyle.Standard;
                SwingSize = 7;
                DtbStrength = 20;
                UseCloseValues = false;

                IgnoreInsideBars = true;
                UseBreakouts = true;

                DoubleBottomDotColor = Brushes.Gold;
                DoubleBottomDotSize = 6;
                LowerLowDotColor = Brushes.Red;
                LowerLowDotSize = 6;
                HigherLowDotColor = Brushes.Green;
                HigherLowDotSize = 6;
                DoubleTopDotColor = Brushes.Gold;
                DoubleTopDotSize = 6;
                LowerHighDotColor = Brushes.Red;
                LowerHighDotSize = 6;
                HigherHighDotColor = Brushes.Green;
                HigherHighDotSize = 6;

                BarsRequiredToPlot = 1;

                TextFont = new SimpleFont("Consolas", 12);
                SwingLengthType = SwingLengthStyle.Points;
                SwingDurationType = SwingDurationStyle.Bars;
                ShowSwingLabel = true;
                ShowSwingPercent = false;
                SwingTimeType = SwingTimeStyle.Off;
                SwingVolumeType = SwingVolumeStyle.Off;
                ShowSwingSwitch = false;
                VisualizationType = VisualizationStyle.ZigZag;
                ZigZagColorUp = Brushes.Green;
                ZigZagColorDn = Brushes.Red;
                ZigZagStyle = DashStyleHelper.Solid;
                ZigZagWidth = 3;
                TextColorHigherHigh = Brushes.Green;
                TextColorLowerHigh = Brushes.Red;
                TextColorDoubleTop = Brushes.Gold;
                TextColorHigherLow = Brushes.Green;
                TextColorLowerLow = Brushes.Red;
                TextColorDoubleBottom = Brushes.Gold;
                TextOffsetLength = 36;
                TextOffsetLabel = 60;
                TextOffsetVolume = 84;
                TextOffsetTime = 108;
                TextOffsetPercent = 132;
                SwingSwitchYTickOffset = 5;

                AbcPattern = AbcPatternMode.Long_Short;

                AbcLineStyle = DashStyleHelper.Solid;
                AbcLineStyleRatio = DashStyleHelper.Dash;
                AbcLineWidth = 4;
                AbcLineWidthRatio = 2;
                AbcTextFont = new SimpleFont("Courier", 14) { Bold = true };
                AbcTextOffsetLabel = 40;
                AbcTextColorDn = Brushes.OrangeRed;
                AbcTextColorUp = Brushes.LimeGreen;
                AbcZigZagColorDn = Brushes.OrangeRed;
                AbcZigZagColorUp = Brushes.LimeGreen;
                AbcMaxRetracement = 92.0;
                AbcMinRetracement = 61.0;
                ShowEntryArrows = true;
                ABCEntryArrowYTickOffset = 5;
                EntryLineStyle = DashStyleHelper.Solid;
                EntryLineWidth = 4;
                EntryLineColorDn = Brushes.OrangeRed;
                EntryLineColorUp = Brushes.LimeGreen;
                ShowEntryLine = true;
                ShowHistoricalEntryLine = false;
                ShowABCLabel = true;

                AlertAbc = true;
                AlertAbcEntry = true;
                AlertAbcPriority = Priority.Medium;
                AlertAbcEntryPriority = Priority.High;
                AlertAbcLongSoundFileName = "AbcLong.wav";
                AlertAbcLongEntrySoundFileName = "AbcLongEntry.wav";
                AlertAbcShortSoundFileName = "AbcShort.wav";
                AlertAbcShortEntrySoundFileName = "AbcShortEntry.wav";

                ShowNakedSwings = true;
                ShowHistoricalNakedSwings = false;
                NakedSwingHighColor = Brushes.Red;
                NakedSwingLowColor = Brushes.Green;
                NakedSwingDashStyle = DashStyleHelper.Solid;
                NakedSwingLineWidth = 1;

                DivergenceIndicatorMode = DivergenceMode.Off;
                DivergenceDirectionMode = DivergenceDirection.Long_Short;
                DivParam1 = 10;
                DivParam2 = 26;
                DivParam3 = 9;

                ShowDivergenceRegular = true;
                ShowDivergenceHidden = true;
                ShowDivergenceText = true;
                DivDnColor = Brushes.Red;
                DivUpColor = Brushes.Green;
                DivLineStyle = DashStyleHelper.Dot;
                DivLineWidth = 2;

                Statistic = StatisticMode.Off;
                StatisticLength = 5;
                ClearNinjaScriptOutputWindow = true;

                AddSwingExtension = false;
                AddSwingRetracementFast = false;
                AddSwingRetracementSlow = false;
            }
            else if (State == State.Configure)
            {
                swingCalculation.SetUserParameters(UseCloseValues, SwingStyleType, SwingSize,
                    DtbStrength, IgnoreInsideBars, UseBreakouts, SwingLengthType, 
                    SwingDurationType, SwingTimeType,
                    TextColorHigherHigh, TextColorLowerHigh, TextColorDoubleTop,
                    TextColorHigherLow, TextColorLowerLow, TextColorDoubleBottom);

                DoubleBottom = new Series<double>(this, MaximumBarsLookBack.Infinite);
                LowerLow = new Series<double>(this, MaximumBarsLookBack.Infinite);
                HigherLow = new Series<double>(this, MaximumBarsLookBack.Infinite);
                DoubleTop = new Series<double>(this, MaximumBarsLookBack.Infinite);
                LowerHigh = new Series<double>(this, MaximumBarsLookBack.Infinite);
                HigherHigh = new Series<double>(this, MaximumBarsLookBack.Infinite);

                EntryLong = new Series<double>(this, MaximumBarsLookBack.Infinite);
                EntryShort = new Series<double>(this, MaximumBarsLookBack.Infinite);
                EntryLevelLine = new Series<double>(this, MaximumBarsLookBack.Infinite);
                ABCPlots = new Series<ABC>(this, MaximumBarsLookBack.Infinite);

                DivergenceDataHigh = new Series<double>(this, MaximumBarsLookBack.Infinite);
                DivergenceDataLow = new Series<double>(this, MaximumBarsLookBack.Infinite);

                switch (DivergenceIndicatorMode)
                {
                    // Add custom divergence indicator
                    // Concept: add your own indicator here
                    // For example, if you want RSI, add RSI in PriceActionSwingBase:
                    // public enum DivergenceMode { Off, MACD, Stochastics, RSI }
                    // And add here
                    // case DivergenceMode.RSI:
                    //    // Add RSI indicator
                    //    break;
                    // Add divergence indicator in DataLoaded
                    case DivergenceMode.MACD:
                        macd = MACD(DivParam1, DivParam2, DivParam3);
                        break;
                    case DivergenceMode.Stochastics:
                        stochastics = Stochastics(DivParam1, DivParam2, DivParam3);
                        break;
                }
            }
            else if (State == State.DataLoaded)
            {
                // now all input should be loaded and we can set some dependent data values
                swingCalculation.SetAdditionalValues();
                swingCalculation.visualize = true;

                // If we do not set the offset, the text above and below swings is not symmetrical.
                lowTextOffset = (int)TextFont.Size * 2;
                // Set the text offset for the swing lows only once.
                lowTextOffsetLength = TextOffsetLength - lowTextOffset;
                lowTextOffsetPercent = TextOffsetPercent - lowTextOffset;
                lowTextOffsetLabel = TextOffsetLabel - lowTextOffset;
                lowTextOffsetTime = TextOffsetTime - lowTextOffset;
                lowTextOffsetVolume = TextOffsetVolume - lowTextOffset;

                ABCEntryArrowYTickOffsetValue = ABCEntryArrowYTickOffset * TickSize;
                SwingSwitchYTickOffsetValue = SwingSwitchYTickOffset * TickSize;

                switch (DivergenceIndicatorMode)
                {
                    // Add custom divergence indicator here like MACD or Stochastics
                    case DivergenceMode.MACD:
                        DivergenceDataHigh = macd.Diff;
                        DivergenceDataLow = macd.Diff;
                        break;
                    case DivergenceMode.Stochastics:
                        DivergenceDataHigh = stochastics.K;
                        DivergenceDataLow = stochastics.K;
                        break;
                }

                if (AlertAbcLongSoundFileName == "AbcLong.wav")
                {
                    AlertAbcLongSoundFileName = string.Format(@"{0}sounds\{1}", Core.Globals.InstallDir, AlertAbcLongSoundFileName);
                }
                if (AlertAbcLongEntrySoundFileName == "AbcLongEntry.wav")
                {
                    AlertAbcLongEntrySoundFileName = string.Format(@"{0}sounds\{1}", Core.Globals.InstallDir, AlertAbcLongEntrySoundFileName);
                }
                if (AlertAbcShortSoundFileName == "AbcShort.wav")
                {
                    AlertAbcShortSoundFileName = string.Format(@"{0}sounds\{1}", Core.Globals.InstallDir, AlertAbcShortSoundFileName);
                }
                if (AlertAbcShortEntrySoundFileName == "AbcShortEntry.wav")
                {
                    AlertAbcShortEntrySoundFileName = string.Format(@"{0}sounds\{1}", Core.Globals.InstallDir, AlertAbcShortEntrySoundFileName);
                }

                if (Statistic == StatisticMode.Table)
                {
                    if (setInitialValues == false)
                    {
                        // add initial data
                        swingStatsList.Add(new SwingStats("Up", 0, 0, 0, 0, 0));
                        swingStatsList.Add(new SwingStats("Down", 0, 0, 0, 0, 0));
                        // add initial relation data
                        swingRelationList.Add(new SwingRelationClassicfifaction("HH", 0, "", "", "", "", "", ""));
                        swingRelationList.Add(new SwingRelationClassicfifaction("LH", 0, "", "", "", "", "", ""));
                        swingRelationList.Add(new SwingRelationClassicfifaction("DT", 0, "", "", "", "", "", ""));
                        swingRelationList.Add(new SwingRelationClassicfifaction("HL", 0, "", "", "", "", "", ""));
                        swingRelationList.Add(new SwingRelationClassicfifaction("LL", 0, "", "", "", "", "", ""));
                        swingRelationList.Add(new SwingRelationClassicfifaction("DB", 0, "", "", "", "", "", ""));

                        setInitialValues = true;
                    }
                }
            }
            else if (State == State.Historical)
            {
                if (Statistic == StatisticMode.Table)
                {
                    // right side panel initialize variables
                    rightSidePanelActive = false;

                    if (ChartControl != null)
                    {
                        ChartControl.Dispatcher.InvokeAsync((System.Action)(() =>
                        {
                            LoadBrushesFromSkin();
                            // WPF modifications wait until State.Historical to play nice with duplicating tabs
                            CreateWPFControls();
                        }));
                    }
                }
            }
            else if (State == State.Terminated)
            {
                if (Statistic == StatisticMode.Table)
                {
                    if (ChartControl != null)
                    {
                        ChartControl.Dispatcher.InvokeAsync((() =>
                        {
                            DisposeWPFControls();
                        }));
                    }
                }
            }
        }
        #endregion

        #region OnBarUpdate
        protected override void OnBarUpdate()
        {
            // checks to ensure there are enough bars before beginning
            if (CurrentBar <= swingCalculation.calculationStartBar)
                return;

            swingCalculation.InitAndResetSwingCalculation();
            swingCalculation.CalculateSwings();
            ComputeVisualization();

            DrawABC();

            DrawNakedSwings();

            DrawDivergence();

            DrawFibonacci();

            ComputeStatistic();
        }
        #endregion

        #region OnRender
        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            try
            {
                base.OnRender(chartControl, chartScale);

                #region Local fields
                // The width of the bar
                int barWidth = Math.Abs(chartControl.GetXByBarIndex(ChartBars, 1) - chartControl.GetXByBarIndex(ChartBars, 0));

                // Half the bar width (So we can align our text with the center of the bar)
                int halfBarWidth = barWidth / 2;

                int barIdx = 0;

                double startPrice = 0;  // Starting price of the segment
                double endPrice = 0;    // Ending price of the segment
                double startVol = 0;    // Volume at the beginning of the segment
                double endVol = 0;      // Volume at the end of the segment 

                SharpDX.Direct2D1.Brush bullLineBrush = ZigZagColorUp.ToDxBrush(RenderTarget);
                SharpDX.Direct2D1.Brush bearLineBrush = ZigZagColorDn.ToDxBrush(RenderTarget);
                SharpDX.Direct2D1.Brush entryLineColor = null;
                SharpDX.Direct2D1.Brush nakedSwingBearBrush = NakedSwingHighColor.ToDxBrush(RenderTarget);
                SharpDX.Direct2D1.Brush nakedSwingBullBrush = NakedSwingLowColor.ToDxBrush(RenderTarget);
                SharpDX.Direct2D1.Brush divergenceBullBrush = DivUpColor.ToDxBrush(RenderTarget);
                SharpDX.Direct2D1.Brush divergenceBearBrush = DivDnColor.ToDxBrush(RenderTarget);

                // For determing the style of the line segments; i.e. solid or dashed lines.  For some reason, dots don't work.
                // Use the same style for each segment
                SharpDX.Direct2D1.StrokeStyle strokeStyle = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, StrokeProps(ZigZagStyle));
                SharpDX.Direct2D1.StrokeStyle strokeStyleABC = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, StrokeProps(AbcLineStyle));
                SharpDX.Direct2D1.StrokeStyle strokeStyleABCc = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, StrokeProps(AbcLineStyleRatio));
                SharpDX.Direct2D1.StrokeStyle strokeStyleEntries = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, StrokeProps(EntryLineStyle));
                SharpDX.Direct2D1.StrokeStyle strokeStyleNakedSwings = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, StrokeProps(NakedSwingDashStyle));
                SharpDX.Direct2D1.StrokeStyle strokeStyleDivergence = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, StrokeProps(DivLineStyle));

                Dictionary<double, List<SharpDX.Vector2>> SwingHighPoints = new Dictionary<double, List<SharpDX.Vector2>>();
                List<double> yHighVals = new List<double>();
                Dictionary<double, List<SharpDX.Vector2>> SwingLowPoints = new Dictionary<double, List<SharpDX.Vector2>>();
                List<double> yLowVals = new List<double>();
                #endregion

                // The way rendering works, most of the time, is you work your way through the chart, one bar at a 
                // time and draw your objects according to each bar.  If you need to draw a line, draw it from the 
                // bar where the lines ends back to where it begins.
                // The exception here are the line segments, which will be drawn when idx is an ending swing
                for (int idx = ChartBars.FromIndex; idx <= ChartBars.ToIndex; ++idx)
                {
                    SwingStruct thisSwingHigh = swingCalculation.SwingHighs.Find(x => x.barNumber == idx);
                    SwingStruct thisSwingLow = swingCalculation.SwingLows.Find(x => x.barNumber == idx);

                    #region Draw swings (line segments)
                    // In order to draw a line, we need start and end points. That's prices for our y and bar number for our x.  
                    // Both are members of the Swingstruct element we are looking for.

                    // If both are zero, we are at the beginning of the chart and need to look backwards for the 
                    // beginning swing point, which most times will be off the chart.
                    if (startPrice == 0
                        && endPrice == 0)
                    {
                        barIdx = idx;
                        if (thisSwingHigh.barNumber > 0)
                        {
                            while (barIdx > 2)
                            {
                                barIdx -= 1;
                                SwingStruct previousSwingLow = swingCalculation.SwingLows.Find(x => x.barNumber == barIdx);
                                if (previousSwingLow.barNumber > 0)
                                {
                                    startPrice = previousSwingLow.price;
                                    startVol = previousSwingLow.volume;
                                    break;
                                }
                            }
                            endPrice = thisSwingHigh.price;
                            endVol = thisSwingHigh.volume;
                        }
                        else if (thisSwingLow.barNumber > 0)
                        {
                            while (barIdx > 2)
                            {
                                barIdx -= 1;
                                SwingStruct previousSwingHigh = swingCalculation.SwingHighs.Find(x => x.barNumber == barIdx);
                                if (previousSwingHigh.barNumber > 0)
                                {
                                    startPrice = previousSwingHigh.price;
                                    startVol = previousSwingHigh.volume;
                                    break;
                                }
                            }
                            endPrice = thisSwingLow.price;
                            endVol = thisSwingLow.volume;
                        }
                    }
                    // Otherwise, we simply roll through the bars and collect the information at each swing
                    else
                    {
                        if (thisSwingHigh.barNumber > 0)
                        {
                            endPrice = thisSwingHigh.price;
                            endVol = thisSwingHigh.volume;
                        }
                        else if (thisSwingLow.barNumber > 0)
                        {
                            endPrice = thisSwingLow.price;
                            endVol = thisSwingLow.volume;
                        }
                    }

                    if (startPrice > 0
                        && endPrice > 0)
                    {
                        // Draw the line segment
                        SharpDX.Vector2 point1 = new SharpDX.Vector2
                        {
                            X = chartControl.GetXByBarIndex(ChartBars, barIdx),
                            Y = chartScale.GetYByValue(startPrice)
                        };

                        SharpDX.Vector2 point2 = new SharpDX.Vector2
                        {
                            X = chartControl.GetXByBarIndex(ChartBars, idx),
                            Y = chartScale.GetYByValue(endPrice)
                        };

                        // Depending on the VisualizationType the user selected, there's different things to do...
                        // The reason we don't see GannStyle here is because that's plotted by the framework instead.
                        // (So we'll not plot the line segment if it's GannStyle)
                        if (VisualizationType == VisualizationStyle.Dots_ZigZag
                            || VisualizationType == VisualizationStyle.ZigZag)
                        {
                            if (startPrice < endPrice)
                            {
                                RenderTarget.DrawLine(point1, point2, bullLineBrush, ZigZagWidth, strokeStyle);
                            }
                            else
                            {
                                RenderTarget.DrawLine(point1, point2, bearLineBrush, ZigZagWidth, strokeStyle);
                            }
                        }
                        if (VisualizationType == VisualizationStyle.ZigZagVolume)
                        {
                            if (startPrice < endPrice) // up swing
                            {
                                if (endVol > startVol)
                                {
                                    RenderTarget.DrawLine(point1, point2, bullLineBrush, ZigZagWidth, strokeStyle);
                                }
                                else
                                {
                                    RenderTarget.DrawLine(point1, point2, bearLineBrush, ZigZagWidth, strokeStyle);
                                }
                            }
                            else // down swing
                            {
                                if (startVol < endVol)
                                {
                                    RenderTarget.DrawLine(point1, point2, bearLineBrush, ZigZagWidth, strokeStyle);
                                }
                                else
                                {
                                    RenderTarget.DrawLine(point1, point2, bullLineBrush, ZigZagWidth, strokeStyle);
                                }
                            }
                        }

                        // Reset for the next wave (The end point here will be our next start point)
                        startPrice = endPrice;
                        startVol = endVol;
                        endPrice = 0;
                        endVol = 0;

                        barIdx = idx;
                    }
                    #endregion

                    #region Draw dots (ellipses)
                    if (VisualizationType == VisualizationStyle.Dots
                        || VisualizationType == VisualizationStyle.Dots_ZigZag)
                    {
                        int x = chartControl.GetXByBarIndex(ChartBars, idx);
                        int dotSize = 0;
                        int y = 0;
                        SharpDX.Direct2D1.Brush dotColor = null; // Brushes.Transparent.ToDxBrush(RenderTarget);

                        if (thisSwingHigh.barNumber > 0)
                        {
                            // Draw a dot on top
                            dotColor = PlotDotColor(thisSwingHigh.label);
                            y = chartScale.GetYByValue(thisSwingHigh.price);
                            dotSize = GetDotSize(thisSwingHigh.label);
                        }
                        else if (thisSwingLow.barNumber > 0)
                        {
                            // Draw a dot on bottom
                            dotColor = PlotDotColor(thisSwingLow.label);
                            y = chartScale.GetYByValue(thisSwingLow.price);
                            dotSize = GetDotSize(thisSwingLow.label);
                        }

                        if (dotColor != null)
                        {
                            SharpDX.Vector2 dotVector = new SharpDX.Vector2(x, y);
                            SharpDX.Direct2D1.Ellipse dotObj = new SharpDX.Direct2D1.Ellipse(dotVector, dotSize, dotSize);
                            RenderTarget.FillEllipse(dotObj, dotColor);

                            dotColor.Dispose();
                        }
                    }
                    #endregion

                    #region Draw Length (text)
                    if (thisSwingHigh.barNumber > 0
                        && thisSwingHigh.textColor != null)
                    {
                        if (thisSwingHigh.output != null)
                        {
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingHigh.price);
                            DrawText(barX - halfBarWidth, barY - TextOffsetLength, thisSwingHigh.textColor.ToDxBrush(RenderTarget), thisSwingHigh.output, barWidth);
                        }
                    }
                    else if (thisSwingLow.barNumber > 0
                            && thisSwingLow.textColor != null)
                    {
                        if (thisSwingLow.output != null)
                        {
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingLow.price);
                            DrawText(barX - halfBarWidth, barY + lowTextOffsetLength, thisSwingLow.textColor.ToDxBrush(RenderTarget), thisSwingLow.output, barWidth);
                        }
                    }
                    #endregion

                    #region Draw Label (text)
                    if (ShowSwingLabel)
                    {
                        SharpDX.Direct2D1.Brush labelColor = null; // Brushes.Transparent.ToDxBrush(RenderTarget);

                        if (thisSwingHigh.barNumber > 0
                            && thisSwingHigh.textColor != null)
                        {
                            if (thisSwingHigh.label != null)
                            {
                                labelColor = thisSwingHigh.textColor.ToDxBrush(RenderTarget);
                                int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                                int barY = chartScale.GetYByValue(thisSwingHigh.price);
                                DrawText(barX - halfBarWidth, barY - TextOffsetLabel, labelColor, thisSwingHigh.label, barWidth);

                            }
                        }
                        else if (thisSwingLow.barNumber > 0
                                 && thisSwingLow.textColor != null)
                        {
                            if (thisSwingLow.label != null)
                            {
                                labelColor = thisSwingLow.textColor.ToDxBrush(RenderTarget);
                                int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                                int barY = chartScale.GetYByValue(thisSwingLow.price);
                                DrawText(barX - halfBarWidth, barY + lowTextOffsetLabel, labelColor, thisSwingLow.label, barWidth);
                            }
                        }
                        if (labelColor != null)
                        {
                            labelColor.Dispose();
                        }
                    }
                    #endregion

                    #region Draw Percent (text)
                    if (ShowSwingPercent)
                    {
                        if (thisSwingHigh.barNumber > 0
                            && thisSwingHigh.percent != 0
                            && thisSwingHigh.textColor != null)
                        {
                            string percent = String.Format("{0:F1}%", thisSwingHigh.percent);
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingHigh.price);
                            DrawText(barX - halfBarWidth, barY - TextOffsetPercent, thisSwingHigh.textColor.ToDxBrush(RenderTarget), percent, barWidth);
                        }
                        else if (thisSwingLow.barNumber > 0
                                 && thisSwingLow.percent != 0
                                && thisSwingLow.textColor != null)
                        {
                            string percent = String.Format("{0:F1}%", thisSwingLow.percent);
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingLow.price);
                            DrawText(barX - halfBarWidth, barY + lowTextOffsetPercent, thisSwingLow.textColor.ToDxBrush(RenderTarget), percent, barWidth);
                        }
                    }
                    #endregion

                    #region Draw Time (text)
                    if (SwingTimeType != SwingTimeStyle.Off)
                    {
                        if (thisSwingHigh.barNumber > 0
                            && thisSwingHigh.textColor != null)
                        {
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingHigh.price);
                            DrawText(barX - halfBarWidth, barY - TextOffsetTime, thisSwingHigh.textColor.ToDxBrush(RenderTarget), thisSwingHigh.timeOutput, barWidth);
                        }
                        else if (thisSwingLow.barNumber > 0
                                 && thisSwingLow.textColor != null)
                        {
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingLow.price);
                            DrawText(barX - halfBarWidth, barY + lowTextOffsetTime, thisSwingLow.textColor.ToDxBrush(RenderTarget), thisSwingLow.timeOutput, barWidth);
                        }
                    }
                    #endregion

                    #region Draw Volume (text)
                    if (SwingVolumeType != SwingVolumeStyle.Off)
                    {
                        if (thisSwingHigh.barNumber > 0
                            && thisSwingHigh.textColor != null)
                        {
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingHigh.price);
                            DrawText(barX - halfBarWidth, barY - TextOffsetVolume, thisSwingHigh.textColor.ToDxBrush(RenderTarget), TruncIntToStr(thisSwingHigh.volume), barWidth);
                        }
                        else if (thisSwingLow.barNumber > 0
                                 && thisSwingLow.textColor != null)
                        {
                            int barX = ChartControl.GetXByBarIndex(ChartBars, idx);
                            int barY = chartScale.GetYByValue(thisSwingLow.price);
                            DrawText(barX - halfBarWidth, barY + lowTextOffsetVolume, thisSwingLow.textColor.ToDxBrush(RenderTarget), TruncIntToStr(thisSwingLow.volume), barWidth);
                        }
                    }
                    #endregion

                    #region Draw ABC and maybe the labels (line segments & text)
                    if (ABCPlots.IsValidDataPointAt(idx) == true)
                    {
                        ABC thisABC = ABCPlots.GetValueAt(idx);

                        int xA = chartControl.GetXByBarIndex(ChartBars, thisABC.aBar);
                        int yA = chartScale.GetYByValue(thisABC.aY);
                        int xB = chartControl.GetXByBarIndex(ChartBars, thisABC.bBar);
                        int yB = chartScale.GetYByValue(thisABC.bY);
                        int xC = chartControl.GetXByBarIndex(ChartBars, thisABC.cBar);
                        int yC = chartScale.GetYByValue(thisABC.cY);
                        int width = AbcLineWidth;
                        int widthRatio = AbcLineWidthRatio;

                        SharpDX.Direct2D1.Brush lineColor = thisABC.lineColor.ToDxBrush(RenderTarget);

                        // Draw the first line segment
                        SharpDX.Vector2 point1 = new SharpDX.Vector2 { X = xA, Y = yA };
                        SharpDX.Vector2 point2 = new SharpDX.Vector2 { X = xB, Y = yB };
                        RenderTarget.DrawLine(point1, point2, lineColor, width, strokeStyleABC);

                        SharpDX.Vector2 point3 = new SharpDX.Vector2 { X = xB, Y = yB };
                        SharpDX.Vector2 point4 = new SharpDX.Vector2 { X = xC, Y = yC };
                        RenderTarget.DrawLine(point3, point4, lineColor, width, strokeStyleABC);

                        SharpDX.Vector2 point5 = new SharpDX.Vector2 { X = xC, Y = yC };
                        SharpDX.Vector2 point6 = new SharpDX.Vector2 { X = xA, Y = yA };
                        RenderTarget.DrawLine(point5, point6, lineColor, widthRatio, strokeStyleABCc);

                        if (ShowABCLabel == true)
                        {
                            TextFormat textFormat = AbcTextFont.ToDirectWriteTextFormat();
                            if (thisABC.lineColor == AbcZigZagColorUp)
                            {
                                int xAlabel = chartControl.GetXByBarIndex(ChartBars, thisABC.aBar);
                                int yAlabel = chartScale.GetYByValue(thisABC.aY) + AbcTextOffsetLabel;
                                int xBlabel = chartControl.GetXByBarIndex(ChartBars, thisABC.bBar);
                                int yBlabel = chartScale.GetYByValue(thisABC.bY) - AbcTextOffsetLabel - 5;
                                int xClabel = chartControl.GetXByBarIndex(ChartBars, thisABC.cBar);
                                int yClabel = chartScale.GetYByValue(thisABC.cY) + AbcTextOffsetLabel + 5;

                                DrawABCText(halfBarWidth, "A", lineColor, yAlabel, xAlabel, textFormat);
                                DrawABCText(halfBarWidth, "B", lineColor, yBlabel, xBlabel, textFormat);
                                DrawABCText(halfBarWidth, "C", lineColor, yClabel, xClabel, textFormat);
                            }
                            else
                            {
                                int xAlabel = chartControl.GetXByBarIndex(ChartBars, thisABC.aBar);
                                int yAlabel = chartScale.GetYByValue(thisABC.aY) - AbcTextOffsetLabel - 5;
                                int xBlabel = chartControl.GetXByBarIndex(ChartBars, thisABC.bBar);
                                int yBlabel = chartScale.GetYByValue(thisABC.bY) + AbcTextOffsetLabel;
                                int xClabel = chartControl.GetXByBarIndex(ChartBars, thisABC.cBar);
                                int yClabel = chartScale.GetYByValue(thisABC.cY) - AbcTextOffsetLabel - 5;

                                DrawABCText(halfBarWidth, "A", lineColor, yAlabel, xAlabel, textFormat);
                                DrawABCText(halfBarWidth, "B", lineColor, yBlabel, xBlabel, textFormat);
                                DrawABCText(halfBarWidth, "C", lineColor, yClabel, xClabel, textFormat);
                            }
                        }

                        entryLineColor = lineColor;
                        lineColor.Dispose();
                    }
                    #endregion

                    #region Draw ABC entries (line segments)
                    if (ShowEntryLine == true)
                    {
                        ABC thisABC = ABCPlots.GetValueAt(idx);
                        if (thisABC != null)
                        {
                            if (thisABC.lineColor == AbcZigZagColorUp)
                            {
                                entryLineColor = EntryLineColorUp.ToDxBrush(RenderTarget);
                            }
                            else
                            {
                                entryLineColor = EntryLineColorDn.ToDxBrush(RenderTarget);
                            }
                        }

                        if (entryLineColor != null)
                        {
                            double entryLine = EntryLevelLine.GetValueAt(idx);
                            if (entryLine > 0)
                            {
                                SharpDX.Vector2 prevPoint;
                                SharpDX.Vector2 curPoint;

                                double previousLine = EntryLevelLine.GetValueAt(idx - 1);
                                if (previousLine == 0)
                                {
                                    // Start line
                                    prevPoint = new SharpDX.Vector2
                                    {
                                        X = chartControl.GetXByBarIndex(ChartBars, idx - 1),
                                        Y = chartScale.GetYByValue(entryLine)
                                    };

                                    curPoint = new SharpDX.Vector2
                                    {
                                        X = chartControl.GetXByBarIndex(ChartBars, idx),
                                        Y = chartScale.GetYByValue(entryLine)
                                    };

                                    RenderTarget.DrawLine(prevPoint, curPoint, entryLineColor, EntryLineWidth, strokeStyleEntries);
                                }
                                else
                                {
                                    // Continue line 
                                    prevPoint = new SharpDX.Vector2
                                    {
                                        X = chartControl.GetXByBarIndex(ChartBars, idx - 1),
                                        Y = chartScale.GetYByValue(previousLine)
                                    };

                                    curPoint = new SharpDX.Vector2
                                    {
                                        X = chartControl.GetXByBarIndex(ChartBars, idx),
                                        Y = chartScale.GetYByValue(entryLine)
                                    };

                                    RenderTarget.DrawLine(prevPoint, curPoint, entryLineColor, EntryLineWidth, strokeStyleEntries);
                                }
                            }
                        }
                    }
                    #endregion

                    #region Naked swings (rays)
                    // Methodology: We're going to use a path geometry to render the naked swings.  We already have a list of objects that contain
                    // the beginning bar and the price for each swing.  So all we need to do is convert the bar number and price to x,y 
                    // coordintates and store them in a temp array.  Then, when we break out of the ChartBars for loop, we'll use a PathGeom 
                    // object to draw the lines.

                    // So, there was a little bump in the road.  95% of the time, you draw your objects working your way from left to right
                    // as the ChartBars for loop gets accomplished.  Except for naked swings, suppose you have your unexpanded chart that 
                    // shows your naked swings, and you want to squish it up vertically to see if there are any naked swings above you.  Well,
                    // since those points that would be exposed have an idx less than FromIndex, you won't see them.  You should.

                    // GetVisiblePoints() is designed to return the points that are within the chart area.  From there I had to manipulate 
                    // the foreach code so that it draws those lines all the way across the screen.  *Another fork in the road: Then I had to
                    // make sure that when you scroll backwards, the line began at the bar it is supposed to, instead of remaining drawn
                    // completely across the chart.

                    // This part was pretty fun.  :D

                    // Collect the high points for PathGeom rendering (red lines)
                    Dictionary<double, int> highsList = GetVisiblePoints(chartScale, nakedcurHighSwingsList);
                    foreach (KeyValuePair<double, int> barPt in highsList)
                    {
                        float x = chartControl.GetXByBarIndex(ChartBars, idx);
                        float y = chartScale.GetYByValue(barPt.Key);

                        if (ChartBars.MaxValue < barPt.Key
                            && idx > barPt.Value)
                        {
                            if (SwingHighPoints.Count == 0)
                            {
                                // Start a new list
                                List<SharpDX.Vector2> HighPoints = new List<SharpDX.Vector2>
                                    {
                                        new SharpDX.Vector2(x, y)
                                    };

                                // Add the list to the appropriate dictionary
                                SwingHighPoints.Add(y, HighPoints);
                                yHighVals.Add(y);
                            }
                            else
                            {
                                // Same list... add a new KeyValuePair
                                if (SwingHighPoints.ContainsKey(y) == false)
                                {
                                    // Add a new item to the dictionary
                                    List<SharpDX.Vector2> newPt = new List<SharpDX.Vector2>
                                        {
                                            new SharpDX.Vector2(x, y)
                                        };
                                    SwingHighPoints.Add(y, newPt);
                                    yHighVals.Add(y);
                                }
                            }
                        }

                        if (idx == barPt.Value)
                        {
                            if (SwingHighPoints.Count == 0)
                            {
                                // Start a new list
                                List<SharpDX.Vector2> HighPoints = new List<SharpDX.Vector2>
                                    {
                                        new SharpDX.Vector2(x, y)
                                    };

                                // Add the list to the appropriate dictionary
                                SwingHighPoints.Add(y, HighPoints);
                                yHighVals.Add(y);
                            }
                            else
                            {
                                // Same list... add a new KeyValuePair
                                if (SwingHighPoints.ContainsKey(y) == false)
                                {
                                    // Add a new item to the dictionary
                                    List<SharpDX.Vector2> newPt = new List<SharpDX.Vector2>
                                    {
                                        new SharpDX.Vector2(x, y)
                                    };
                                    SwingHighPoints.Add(y, newPt);
                                    yHighVals.Add(y);
                                }
                            }
                        }
                        // Either the naked swing will begin within the left-right area of the chart....
                        if (idx > barPt.Value)
                        {
                            if (SwingHighPoints.ContainsKey(y) == true)
                            {
                                // Add the next point to the list for that key
                                SharpDX.Vector2 nextPt = new SharpDX.Vector2(x, y);
                                List<SharpDX.Vector2> thisList = SwingHighPoints[y];
                                thisList.Add(nextPt);
                            }
                        }
                        // or, it will have come from a previous point in history
                        else
                        {
                            if (SwingHighPoints.ContainsKey(y) == true)
                            {
                                // Add the next point to the list for that key
                                SharpDX.Vector2 nextPt = new SharpDX.Vector2(x, y);
                                List<SharpDX.Vector2> thisList = SwingHighPoints[y];
                                thisList.Add(nextPt);
                            }
                        }
                    }

                    // Collect the low points for PathGeom rendering (blue lines)
                    Dictionary<double, int> lowsList = GetVisiblePoints(chartScale, nakedcurLowSwingsList);
                    foreach (KeyValuePair<double, int> barPt in lowsList)
                    {
                        float x = chartControl.GetXByBarIndex(ChartBars, idx);
                        float y = chartScale.GetYByValue(barPt.Key);

                        if (ChartBars.MinValue > barPt.Key
                             && idx > barPt.Value)
                        {
                            if (SwingLowPoints.Count == 0)
                            {
                                // Start a new list
                                List<SharpDX.Vector2> LowPoints = new List<SharpDX.Vector2>
                                {
                                    new SharpDX.Vector2(x, y)
                                };

                                // Add the list to the appropriate dictionary
                                SwingLowPoints.Add(y, LowPoints);
                                yLowVals.Add(y);
                            }
                            else
                            {
                                // Same list... add a new KeyValuePair
                                if (SwingLowPoints.ContainsKey(y) == false)
                                {
                                    // Add a new item to the dictionary
                                    List<SharpDX.Vector2> newPt = new List<SharpDX.Vector2>
                                {
                                    new SharpDX.Vector2(x, y)
                                };
                                    SwingLowPoints.Add(y, newPt);
                                    yLowVals.Add(y);
                                }
                            }
                        }

                        if (idx == barPt.Value)
                        {
                            if (SwingLowPoints.Count == 0)
                            {
                                // Start a new list
                                List<SharpDX.Vector2> LowPoints = new List<SharpDX.Vector2>
                                {
                                    new SharpDX.Vector2(x, y)
                                };

                                // Add the list to the appropriate dictionary
                                SwingLowPoints.Add(y, LowPoints);
                                yLowVals.Add(y);
                            }
                            else
                            {
                                // Same list... add a new KeyValuePair
                                if (SwingLowPoints.ContainsKey(y) == false)
                                {
                                    // Add a new item to the dictionary
                                    List<SharpDX.Vector2> newPt = new List<SharpDX.Vector2>
                                {
                                    new SharpDX.Vector2(x, y)
                                };
                                    SwingLowPoints.Add(y, newPt);
                                    yLowVals.Add(y);
                                }
                            }
                        }
                        else if (idx > barPt.Value)
                        {
                            if (SwingLowPoints.ContainsKey(y) == true)
                            {
                                // Add the next point to the list for that key
                                SharpDX.Vector2 nextPt = new SharpDX.Vector2(x, y);
                                List<SharpDX.Vector2> thisList = SwingLowPoints[y];
                                thisList.Add(nextPt);
                            }
                        }
                    }

                    // Do the historical nakeds
                    if (ShowHistoricalNakedSwings == true)
                    {
                        List<OldNakeds> theseLows = oldNakedLows.FindAll(x => x.ToIdx == idx);
                        foreach (OldNakeds thisLine in theseLows)
                        {
                            // Draw the line segment
                            SharpDX.Vector2 point1 = new SharpDX.Vector2
                            {
                                X = chartControl.GetXByBarIndex(ChartBars, thisLine.FromIdx),
                                Y = chartScale.GetYByValue(thisLine.Price)
                            };

                            SharpDX.Vector2 point2 = new SharpDX.Vector2
                            {
                                X = chartControl.GetXByBarIndex(ChartBars, thisLine.ToIdx),
                                Y = chartScale.GetYByValue(thisLine.Price)
                            };

                            RenderTarget.DrawLine(point1, point2, nakedSwingBullBrush, NakedSwingLineWidth, strokeStyleNakedSwings);
                        }

                        List<OldNakeds> theseHighs = oldNakedHighs.FindAll(x => x.ToIdx == idx);
                        foreach (OldNakeds thisLine in theseHighs)
                        {
                            // Draw the line segment
                            SharpDX.Vector2 point1 = new SharpDX.Vector2
                            {
                                X = chartControl.GetXByBarIndex(ChartBars, thisLine.FromIdx),
                                Y = chartScale.GetYByValue(thisLine.Price)
                            };

                            SharpDX.Vector2 point2 = new SharpDX.Vector2
                            {
                                X = chartControl.GetXByBarIndex(ChartBars, thisLine.ToIdx),
                                Y = chartScale.GetYByValue(thisLine.Price)
                            };

                            RenderTarget.DrawLine(point1, point2, nakedSwingBearBrush, NakedSwingLineWidth, strokeStyleNakedSwings);
                        }
                    }
                    #endregion

                    #region Divergences (line segments)
                    List<Divergences> theseDivs = DivergenceLines.FindAll(x => x.EndIdx == idx);
                    foreach (Divergences thisDivs in theseDivs)
                    {
                        // Draw the line segment
                        SharpDX.Vector2 point1 = new SharpDX.Vector2
                        {
                            X = chartControl.GetXByBarIndex(ChartBars, thisDivs.StartIdx),
                            Y = chartScale.GetYByValue(thisDivs.StartY)
                        };

                        SharpDX.Vector2 point2 = new SharpDX.Vector2
                        {
                            X = chartControl.GetXByBarIndex(ChartBars, thisDivs.EndIdx),
                            Y = chartScale.GetYByValue(thisDivs.EndY)
                        };

                        RenderTarget.DrawLine(point1, point2, thisDivs.DivBias == DivergenceBias.Down ? divergenceBearBrush : divergenceBullBrush, DivLineWidth, strokeStyleDivergence);
                        if (ShowDivergenceText == true)
                        {
                            if (thisDivs.DivBias == DivergenceBias.Down)
                            {
                                SharpDX.Vector2 text1 = new SharpDX.Vector2
                                {
                                    X = chartControl.GetXByBarIndex(ChartBars, (thisDivs.StartIdx + thisDivs.EndIdx) / 2),
                                    Y = chartScale.GetYByValue((thisDivs.StartY + thisDivs.EndY) / 2 + 5)
                                };
                                DrawText(text1.X, text1.Y, divergenceBearBrush, thisDivs.DivType == DivergenceType.Regular ? "rDiv" : "hDiv", 0);
                            }
                            else if (thisDivs.DivBias == DivergenceBias.Up)
                            {
                                SharpDX.Vector2 text1 = new SharpDX.Vector2
                                {
                                    X = chartControl.GetXByBarIndex(ChartBars, (thisDivs.StartIdx + thisDivs.EndIdx) / 2),
                                    Y = chartScale.GetYByValue((thisDivs.StartY + thisDivs.EndY) / 2 - 3)
                                };
                                DrawText(text1.X, text1.Y, divergenceBullBrush, thisDivs.DivType == DivergenceType.Regular ? "rDiv" : "hDiv", 0);
                            }

                        }
                    }

                    #endregion
                }

                #region Maybe draw naked swings that were calculated earlier when we went through the chart bars for loop
                if (SwingHighPoints.Count > 0)
                {
                    // Now, navigate the dictionary, which contains all the points and draw each line
                    // Every item in yHighVals corresponds to an object in SwingHighPoints that make up
                    // the points for that line.
                    foreach (double y in yHighVals)
                    {
                        DrawNakedSwing(nakedSwingBearBrush, strokeStyleNakedSwings, SwingHighPoints, y);
                    }
                }
                if (SwingLowPoints.Count > 0)
                {
                    // Now, navigate the dictionary, which contains all the points and draw each line
                    // Every item in yLowVals corresponds to an object in SwingLowPoints that make up
                    // the points for that line.
                    foreach (double y in yLowVals)
                    {
                        DrawNakedSwing(nakedSwingBullBrush, strokeStyleNakedSwings, SwingLowPoints, y);
                    }
                }
                #endregion

                // If we created a brush, we need to dispose of it.  Here is a good place now that we're done with it
                bearLineBrush.Dispose();
                bullLineBrush.Dispose();
                nakedSwingBullBrush.Dispose();
                nakedSwingBearBrush.Dispose();
                divergenceBearBrush.Dispose();
                divergenceBullBrush.Dispose();
                if (entryLineColor != null)
                {
                    // And it will be (null) if the user didn't select ABC line entries
                    entryLineColor.Dispose();
                }

                // Same with stroke styles... if we created one, we should get rid of it.
                strokeStyle.Dispose();
                strokeStyleABC.Dispose();
                strokeStyleABCc.Dispose();
                strokeStyleEntries.Dispose();
                strokeStyleNakedSwings.Dispose();
                strokeStyleDivergence.Dispose();
            }
            catch (Exception e)
            {
                string message = string.Format("PriceActionSwingPro.OnRender: {0}", e.Message);
                Print(message);
                Log(message, Cbi.LogLevel.Warning);
            }
        }
        #endregion

        #region OnRender method definitions
        //=========================================================================================
        #region DrawText
        /// <summary>
        /// This method is called to render text after you've figured out x, y, and color.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="textColor"></param>
        /// <param name="text2print"></param>
        /// <param name="barWidth"></param>
        protected void DrawText(float x, float y, SharpDX.Direct2D1.Brush textColor, string text2print, float barWidth)
        {
            // 1. The first thing to do is to get a SharpDX font (we use our font object to go that)
            SharpDX.DirectWrite.TextFormat textFormat = TextFont.ToDirectWriteTextFormat();

            // Maybe add some alignment and word wrapping...
            textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
            textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;

            // 2. The next step is to create a rectange that bounds our text
            SharpDX.RectangleF layoutRect = new SharpDX.RectangleF(x, y, barWidth, (float)TextFont.Size);

            // 3. We need a layout object that holds are formatted text object
            SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text2print, textFormat, 12, textFormat.FontSize);

            // 4. Now we have everything we need - font, location, text, and color - to render our text
            RenderTarget.DrawText(text2print, textFormat, layoutRect, textColor);

            textFormat.Dispose();
            textLayout.Dispose();
        }
        #endregion

        #region GetDotSize
        protected int GetDotSize(string label)
        {
            if (label == "HH")
            {
                return HigherHighDotSize;
            }
            else if (label == "HL")
            {
                return HigherLowDotSize;
            }
            else if (label == "LH")
            {
                return LowerHighDotSize;
            }
            else if (label == "LL")
            {
                return LowerLowDotSize;
            }
            else if (label == "DT")
            {
                return DoubleTopDotSize;
            }
            else if (label == "DB")
            {
                return DoubleBottomDotSize;
            }
            else
            {
                return 2;
            }
        }
        #endregion

        #region StrokeProps
        /// <summary>
        /// Takes inStyle as a parameter and returns a stroke style object.  
        /// Note: it still comes out a little strange but this is the best way I've found (so far) to get dots to even render.  
        /// You get used to it but I still wish it draw lines (with user selected styles) as good as the framework draws plots.
        /// </summary>
        /// <param name="inStyle"></param>
        /// <returns></returns>
        protected SharpDX.Direct2D1.StrokeStyleProperties StrokeProps(DashStyleHelper inStyle)
        {
            SharpDX.Direct2D1.StrokeStyleProperties ssProps = new SharpDX.Direct2D1.StrokeStyleProperties
            {
                DashStyle = SharpDX.Direct2D1.DashStyle.Solid,
                DashCap = SharpDX.Direct2D1.CapStyle.Round
            };

            // Just keeping this as sample code... Not using it because the dots don't show up... Only solid and dashes show up.
            switch (inStyle)
            {
                case DashStyleHelper.Dash:
                    ssProps.DashStyle = SharpDX.Direct2D1.DashStyle.Dash;
                    break;
                case DashStyleHelper.DashDot:
                    ssProps.DashStyle = SharpDX.Direct2D1.DashStyle.DashDot;
                    break;
                case DashStyleHelper.DashDotDot:
                    ssProps.DashStyle = SharpDX.Direct2D1.DashStyle.DashDotDot;
                    break;
                case DashStyleHelper.Dot:
                    ssProps.DashStyle = SharpDX.Direct2D1.DashStyle.Dot;
                    break;
                case DashStyleHelper.Solid:
                    ssProps.DashStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    break;
                default:
                    ssProps.DashStyle = SharpDX.Direct2D1.DashStyle.Solid;
                    break;
            }

            return ssProps;
        }
        #endregion

        #region PlotDotColor
        private SharpDX.Direct2D1.Brush PlotDotColor(string label)
        {
            if (label == "HH")
            {
                return HigherHighDotColor.ToDxBrush(RenderTarget);
            }
            else if (label == "HL")
            {
                return HigherLowDotColor.ToDxBrush(RenderTarget);
            }
            else if (label == "LH")
            {
                return LowerHighDotColor.ToDxBrush(RenderTarget);
            }
            else if (label == "LL")
            {
                return LowerLowDotColor.ToDxBrush(RenderTarget);
            }
            else if (label == "DT")
            {
                return DoubleTopDotColor.ToDxBrush(RenderTarget);
            }
            else if (label == "DB")
            {
                return DoubleBottomDotColor.ToDxBrush(RenderTarget);
            }
            else
            {
                return Brushes.Transparent.ToDxBrush(RenderTarget);
            }
        }
        #endregion

        #region GetVisiblePoints
        /// <summary>
        /// Returns all points in inList that fall within the viewable area of the chart
        /// </summary>
        /// <param name="chartScale"></param>
        /// <param name="inList"></param>
        /// <returns></returns>
        private Dictionary<double, int> GetVisiblePoints(ChartScale chartScale, SortedList<double, int> inList)
        {
            Dictionary<double, int> outList = new Dictionary<double, int>();

            foreach (KeyValuePair<double, int> currentPair in inList)
            {
                if (currentPair.Key > chartScale.MinValue
                    && currentPair.Key < chartScale.MaxValue)
                {
                    outList.Add(currentPair.Key, currentPair.Value);
                }
            }

            return outList;
        }
        #endregion

        #region DrawNakedSwing
        /// <summary>
        /// Uses a path geometry to draw the rays
        /// </summary>
        /// <param name="nakedSwingBullBrush"></param>
        /// <param name="strokeStyleNakedSwings"></param>
        /// <param name="inPoints"></param>
        /// <param name="key"></param>
        private void DrawNakedSwing(SharpDX.Direct2D1.Brush nakedSwingBullBrush, SharpDX.Direct2D1.StrokeStyle strokeStyleNakedSwings,
                                     Dictionary<double, List<Vector2>> inPoints, double key)
        {
            // Draw each element in the list as a line represented by a PathGeom
            List<SharpDX.Vector2> thislist = inPoints[key];
            if (thislist.Count > 0)
            {
                SharpDX.Direct2D1.PathGeometry g;
                SharpDX.Direct2D1.GeometrySink sink;

                g = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
                sink = g.Open();
                sink.BeginFigure(new SharpDX.Vector2(thislist[0].X, thislist[0].Y), SharpDX.Direct2D1.FigureBegin.Hollow);

                // Add all the points from inPoints into the sink
                for (int k = 1; k < thislist.Count - 1; ++k)
                {
                    sink.AddLine(new SharpDX.Vector2(thislist[k].X, thislist[k].Y));
                }

                if (g != null
                    && sink != null)
                {
                    if (sink != null)
                    {
                        sink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
                        sink.Close();
                        sink.Dispose();
                    }
                    if (g != null)
                    {
                        var oldAntiAliasMode = RenderTarget.AntialiasMode;
                        RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
                        RenderTarget.DrawGeometry(g, nakedSwingBullBrush, NakedSwingLineWidth, strokeStyleNakedSwings);
                        RenderTarget.AntialiasMode = oldAntiAliasMode;
                        g.Dispose();
                    }
                }
            }
        }
        #endregion

        #region DrawABCText
        /// <summary>
        /// This method will print a text at the high volume node depending if the volume ratio, percentage or tail is selected
        /// </summary>
        /// <param name="halfHBarSpace"></param>
        /// <param name="text"></param>
        /// <param name="textColor"></param>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <param name="textFormat"></param>
        private void DrawABCText(int halfHBarSpace, string text, SharpDX.Direct2D1.Brush textColor, int y, int x, TextFormat textFormat)
        {
            TextLayout textLayout;
            SharpDX.RectangleF layoutRect = new SharpDX.RectangleF(x - halfHBarSpace, y - textFormat.FontSize / 2, halfHBarSpace * 2, textFormat.FontSize);

            textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
            textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;

            textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, 12, textFormat.FontSize);
            RenderTarget.DrawText(text, textFormat, layoutRect, textColor);

            textLayout.Dispose();
        }
        #endregion
        //=========================================================================================
        #endregion

        #region TruncIntToStr
        /// <summary>
        /// Converts long integer numbers in a number-string format.
        /// </summary>
        protected string TruncIntToStr(long number)
        {
            long numberAbs = Math.Abs(number);
            string output;
            double convertedNumber;
            if (numberAbs > 1000000000)
            {
                convertedNumber = Math.Round(number / 1000000000.0, 1, MidpointRounding.AwayFromZero);
                output = convertedNumber.ToString() + "B";
            }
            else if (numberAbs > 1000000)
            {
                convertedNumber = Math.Round(number / 1000000.0, 1, MidpointRounding.AwayFromZero);
                output = convertedNumber.ToString() + "M";
            }
            else if (numberAbs > 1000)
            {
                convertedNumber = Math.Round(number / 1000.0, 1, MidpointRounding.AwayFromZero);
                output = convertedNumber.ToString() + "K";
            }
            else
            {
                output = number.ToString();
            }
            return output;
        }
        #endregion

        #region ComputeVisualization
        protected void ComputeVisualization()
        {
            if (swingCalculation.SwingHigh.New > 1)
            {
                #region Visualize swing
                //=====================================================================================
                if (VisualizationType == VisualizationStyle.Dots
                    || VisualizationType == VisualizationStyle.Dots_ZigZag)
                {
                    switch (swingCalculation.SwingHigh.CurRelation)
                    {
                        case 1:
                            HigherHigh[CurrentBar - swingCalculation.SwingHigh.CurBar] = swingCalculation.SwingHigh.CurPrice;
                            break;
                        case -1:
                            LowerHigh[CurrentBar - swingCalculation.SwingHigh.CurBar] = swingCalculation.SwingHigh.CurPrice;
                            break;
                        case 0:
                            DoubleTop[CurrentBar - swingCalculation.SwingHigh.CurBar] = swingCalculation.SwingHigh.CurPrice;
                            break;
                    }
                }
                else if (VisualizationType == VisualizationStyle.GannStyle)
                {
                    // New swing high
                    if (swingCalculation.SwingHigh.New == 2)
                    {
                        for (int i = CurrentBar - swingCalculation.SwingLow.CurBar - 1; i >= 1; i--)
                        {
                            GannSwing[i] = swingCalculation.SwingLow.CurPrice;
                        }
                        GannSwing[0] = swingCalculation.SwingHigh.CurPrice;
                    }
                    // update
                    else
                    {
                        for (int i = CurrentBar - swingCalculation.CurrentSwing.SwingSlopeChangeBar; i >= 0; i--)
                        {
                            GannSwing[i] = swingCalculation.SwingHigh.CurPrice;
                        }
                    }
                }
                #endregion
            }
            else if (swingCalculation.SwingLow.New > 1)
            {
                #region Visualize swing
                if (VisualizationType == VisualizationStyle.Dots
                    || VisualizationType == VisualizationStyle.Dots_ZigZag)
                {
                    switch (swingCalculation.SwingLow.CurRelation)
                    {
                        case 1:
                            HigherLow[CurrentBar - swingCalculation.SwingLow.CurBar] = swingCalculation.SwingLow.CurPrice;
                            break;
                        case -1:
                            LowerLow[CurrentBar - swingCalculation.SwingLow.CurBar] = swingCalculation.SwingLow.CurPrice;
                            break;
                        case 0:
                            DoubleBottom[CurrentBar - swingCalculation.SwingLow.CurBar] = swingCalculation.SwingLow.CurPrice;
                            break;
                    }
                }
                else if (VisualizationType == VisualizationStyle.GannStyle)
                {
                    // New swing low
                    if (swingCalculation.SwingLow.New == 2)
                    {
                        for (int i = CurrentBar - swingCalculation.SwingHigh.CurBar - 1; i >= 1; i--)
                        {
                            GannSwing[i] = swingCalculation.SwingHigh.CurPrice;
                        }
                        GannSwing[0] = swingCalculation.SwingLow.CurPrice;
                    }
                    // Update
                    else
                    {
                        for (int i = CurrentBar - swingCalculation.CurrentSwing.SwingSlopeChangeBar; i >= 0; i--)
                        {
                            GannSwing[i] = swingCalculation.SwingLow.CurPrice;
                        }
                    }
                }
                #endregion
            }
            #region Swing switch
            if (ShowSwingSwitch)
            {
                if (swingCalculation.SwingHigh.New == 2)
                {
                    SwingSwitchUp[0] = Low[0] - SwingSwitchYTickOffsetValue;
                }
                else if (swingCalculation.SwingLow.New == 2)
                {
                    SwingSwitchDown[0] = High[0] + SwingSwitchYTickOffsetValue;
                }
            }
            #endregion
        }
        #endregion

        #region Naked swing
        private void DrawNakedSwings()
        {
            if (ShowNakedSwings == true)
            {
                // New down swing
                if (swingCalculation.SwingLow.New == 2)
                {
                    nakedcurHighSwingsList.Add(swingCalculation.SwingHigh.CurPrice, swingCalculation.SwingHigh.CurBar);
                }
                if (swingCalculation.SwingLow.New > 1)
                {
                    while (nakedcurLowSwingsList.Count > 0
                            && nakedcurLowSwingsList.Keys[nakedcurLowSwingsList.Count - 1] >= swingCalculation.SwingLow.CurPrice)
                    {
                        double nakedSwingLowPrice = nakedcurLowSwingsList.Keys[nakedcurLowSwingsList.Count - 1];

                        if (ShowHistoricalNakedSwings == true)
                        {
                            OldNakeds thisOldNakedLow = new OldNakeds(nakedcurLowSwingsList.Values[nakedcurLowSwingsList.Count - 1], swingCalculation.SwingLow.CurBar, nakedSwingLowPrice);
                            oldNakedLows.Add(thisOldNakedLow);
                        }
                        nakedcurLowSwingsList.RemoveAt(nakedcurLowSwingsList.Count - 1);
                    }
                }

                // New up swing
                if (swingCalculation.SwingHigh.New == 2)
                {
                    nakedcurLowSwingsList.Add(swingCalculation.SwingLow.CurPrice, swingCalculation.SwingLow.CurBar);
                }
                if (swingCalculation.SwingHigh.New > 1)
                {
                    while (nakedcurHighSwingsList.Count > 0
                           && nakedcurHighSwingsList.Keys[0] <= swingCalculation.SwingHigh.CurPrice)
                    {
                        double nakedSwingHighPrice = nakedcurHighSwingsList.Keys[0];

                        if (ShowHistoricalNakedSwings == true)
                        {
                            OldNakeds thisOldNakedHigh = new OldNakeds(nakedcurHighSwingsList.Values[0], swingCalculation.SwingHigh.CurBar, nakedSwingHighPrice);
                            oldNakedHighs.Add(thisOldNakedHigh);
                        }
                        nakedcurHighSwingsList.RemoveAt(0);
                    }
                }
            }
        }
        #endregion

        #region ABC Patterns
        private void DrawABC()
        {
            #region Reset values
            if (IsFirstTickOfBar == true)
            {
                if (AbcPattern != AbcPatternMode.Off)
                {
                    EntryLevelLine[0] = 0;
                    AbcSignals[0] = 0;

                    if (swingCalculation.SwingLow.CurPrice == 0.0 
                        || swingCalculation.SwingHigh.CurPrice == 0.0)
                    {
                        EntryLong[0] = 0;
                        EntryShort[0] = 0;
                    }
                    else
                    {
                        EntryLong[0] = EntryLong[1];
                        EntryShort[0] = EntryShort[1];
                    }
                }
            }
            #endregion

            #region ABC long pattern
            if (AbcPattern == AbcPatternMode.Long_Short
                || AbcPattern == AbcPatternMode.Long)
            {
                if (swingCalculation.SwingLow.New > 1
                    && abcLongChanceInProgress == false
                    && swingCalculation.SwingLow.LastRelation == -1
                    && swingCalculation.SwingLow.CurRelation == 1
                    && swingCalculation.SwingLow.CurPercent > AbcMinRetracement
                    && swingCalculation.SwingLow.CurPercent < AbcMaxRetracement)
                {
                    // ABC long chance in progress
                    lastABC = CurrentBar;
                    abcLongChanceInProgress = true;
                    entryLineStartBar = CurrentBar;
                    tmpCounter = swingCalculation.SwingLow.Counter;
                    AbcSignals[0] = 1;

                    ABC thisABC = new ABC
                    {
                        aBar = swingCalculation.SwingLow.LastBar,
                        aY = swingCalculation.SwingLow.LastPrice,
                        bBar = swingCalculation.SwingHigh.CurBar,
                        bY = swingCalculation.SwingHigh.CurPrice,
                        cBar = CurrentBar,
                        cY = swingCalculation.SwingLow.CurPrice,
                        lineColor = AbcZigZagColorUp
                    };
                    ABCPlots[0] = thisABC;

                    entryLevel = swingCalculation.SwingLow.CurPrice + Instrument.MasterInstrument.RoundToTickSize((Math.Abs(swingCalculation.SwingLow.CurLength) * TickSize) / 100 * retracementEntryValue);
                    EntryLevelLine[0] = entryLevel;

                    if (AlertAbc == true)
                    {
                        Alert("Alert_Abc_Long" + alertTag++.ToString(), AlertAbcPriority, "ABC Long" + " (" + Instrument.FullName + " " + BarsPeriod + ")", AlertAbcLongSoundFileName, 0, Brushes.White, Brushes.Blue);
                    }
                    EntryLong[0] = entryLevel;
                }

                if (abcLongChanceInProgress)
                {
                    if (swingCalculation.SwingLow.CurPercent > AbcMaxRetracement
                        && tmpCounter == swingCalculation.SwingLow.Counter)
                    {
                        abcLongChanceInProgress = false;
                        ABCPlots.Reset(CurrentBar - lastABC);

                        if (ShowHistoricalEntryLine == false)
                        {
                            for (int index = 0; index < CurrentBar - entryLineStartBar && entryLineStartBar > 0; index++)
                            {
                                EntryLevelLine[index] = 0;
                            }
                            entryLineStartBar = 0;
                        }
                    }
                    // New swing low
                    else if (swingCalculation.SwingLow.New == 2
                             && tmpCounter != swingCalculation.SwingLow.Counter)
                    {
                        abcLongChanceInProgress = false;
                        entryLineStartBar = 0;
                    }
                    // New swing high
                    else if (swingCalculation.SwingHigh.New == 2)
                    {
                        abcLongChanceInProgress = false;
                        entryLineStartBar = 0;
                    }
                    else
                    {
                        if (swingCalculation.SwingLow.New == 3 && tmpCounter == swingCalculation.SwingLow.Counter)
                        {
                            ABC thisABC = ABCPlots.GetValueAt(lastABC);
                            if (thisABC != null)
                            {
                                thisABC.aBar = swingCalculation.SwingLow.LastBar;
                                thisABC.aY = swingCalculation.SwingLow.LastPrice;
                                thisABC.bBar = swingCalculation.SwingHigh.CurBar;
                                thisABC.bY = swingCalculation.SwingHigh.CurPrice;
                                thisABC.cBar = CurrentBar;
                                thisABC.cY = swingCalculation.SwingLow.CurPrice;
                            }

                            entryLevel = swingCalculation.SwingLow.CurPrice + Instrument.MasterInstrument.RoundToTickSize((Math.Abs(swingCalculation.SwingLow.CurLength) * TickSize) / 100 * retracementEntryValue);
                            EntryLevelLine[0] = entryLevel;
                            EntryLong[0] = entryLevel;
                        }
                        else if (IsFirstTickOfBar)
                        {
                            EntryLevelLine[0] = entryLevel;
                        }
                        AbcSignals[0] = 1;

                        bool abcLong = false;
                        if (Calculate == Calculate.OnBarClose || State == State.Historical)
                        {
                            if (Close[0] > entryLevel)
                            {
                                abcLong = true;
                            }
                        }
                        else
                        {
                            if (IsFirstTickOfBar && Open[0] > entryLevel)
                            {
                                abcLong = true;
                            }
                        }

                        if (abcLong)
                        {
                            if (ShowEntryArrows)
                            {
                                ABCEntryArrowUp[0] = Low[0] - ABCEntryArrowYTickOffsetValue;
                            }
                            abcLongChanceInProgress = false;
                            entryLineStartBar = 0;
                            AbcSignals[0] = 2;
                            if (AlertAbcEntry)
                            {
                                Alert("Alert_Abc_Long_Entry" + alertTag++.ToString(), AlertAbcEntryPriority, 
                                    "ABC Long Entry" + " (" + Instrument.FullName + " " + BarsPeriod + ")", 
                                    AlertAbcLongEntrySoundFileName, 0, Brushes.Blue, Brushes.White);
                            }
                        }
                    }
                }
            }
            #endregion

            #region ABC short pattern
            if (AbcPattern == AbcPatternMode.Long_Short
                || AbcPattern == AbcPatternMode.Short)
            {
                if (swingCalculation.SwingHigh.New > 1
                     && abcShortChanceInProgress == false
                     && swingCalculation.SwingHigh.LastRelation == 1
                     && swingCalculation.SwingHigh.CurRelation == -1
                     && swingCalculation.SwingHigh.CurPercent > AbcMinRetracement
                     && swingCalculation.SwingHigh.CurPercent < AbcMaxRetracement)
                {
                    lastABC = CurrentBar;
                    abcShortChanceInProgress = true;
                    entryLineStartBar = CurrentBar;
                    tmpCounter = swingCalculation.SwingHigh.Counter;
                    AbcSignals[0] = -1;

                    ABC thisABC = new ABC
                    {
                        aBar = swingCalculation.SwingHigh.LastBar,
                        aY = swingCalculation.SwingHigh.LastPrice,
                        bBar = swingCalculation.SwingLow.CurBar,
                        bY = swingCalculation.SwingLow.CurPrice,
                        cBar = swingCalculation.SwingHigh.CurBar,
                        cY = swingCalculation.SwingHigh.CurPrice,
                        lineColor = AbcZigZagColorDn
                    };
                    ABCPlots[0] = thisABC;

                    entryLevel = swingCalculation.SwingHigh.CurPrice - Instrument.MasterInstrument.RoundToTickSize((swingCalculation.SwingHigh.CurLength * TickSize) / 100 * retracementEntryValue);

                    if (AlertAbc == true)
                    {
                        Alert("Alert_Abc_Short" + alertTag++.ToString(), AlertAbcPriority, "ABC Short" + " (" + Instrument.FullName + " " + BarsPeriod + ")", AlertAbcShortSoundFileName, 0, Brushes.White, Brushes.Red);
                    }
                    EntryShort[0] = entryLevel;
                }

                if (abcShortChanceInProgress)
                {
                    if (swingCalculation.SwingHigh.CurPercent > AbcMaxRetracement
                        && tmpCounter == swingCalculation.SwingHigh.Counter)
                    {
                        abcShortChanceInProgress = false;
                        ABCPlots.Reset(CurrentBar - lastABC);
                        if (ShowHistoricalEntryLine == false)
                        {
                            for (int index = 0; index < CurrentBar - entryLineStartBar + 1 && entryLineStartBar > 0; index++)
                            {
                                EntryLevelLine[index] = 0;
                            }
                            entryLineStartBar = 0;
                        }
                    }
                    // New swing high
                    else if (swingCalculation.SwingHigh.New == 2
                             && tmpCounter != swingCalculation.SwingHigh.Counter)
                    {
                        abcShortChanceInProgress = false;
                        entryLineStartBar = 0;
                    }
                    // New swing low
                    else if (swingCalculation.SwingLow.New == 2)
                    {
                        abcShortChanceInProgress = false;
                        entryLineStartBar = 0;
                    }
                    else
                    {
                        if (swingCalculation.SwingHigh.New == 3 && tmpCounter == swingCalculation.SwingHigh.Counter)
                        {
                            ABC thisABC = ABCPlots.GetValueAt(lastABC);
                            if (thisABC != null)
                            {
                                thisABC.aBar = swingCalculation.SwingHigh.LastBar;
                                thisABC.aY = swingCalculation.SwingHigh.LastPrice;
                                thisABC.bBar = swingCalculation.SwingLow.CurBar;
                                thisABC.bY = swingCalculation.SwingLow.CurPrice;
                                thisABC.cBar = CurrentBar;
                                thisABC.cY = swingCalculation.SwingHigh.CurPrice;
                            }

                            entryLevel = swingCalculation.SwingHigh.CurPrice - Instrument.MasterInstrument.RoundToTickSize((swingCalculation.SwingHigh.CurLength * TickSize) / 100 * retracementEntryValue);
                            EntryLevelLine[0] = entryLevel;

                            EntryShort[0] = entryLevel;
                        }
                        else if (IsFirstTickOfBar)
                        {
                            EntryLevelLine[0] = entryLevel;
                        }
                        AbcSignals[0] = -1;

                        bool abcShort = false;
                        if (Calculate == Calculate.OnBarClose || State == State.Historical)
                        {
                            if (Close[0] < entryLevel)
                            {
                                abcShort = true;
                            }
                        }
                        else
                        {
                            if (IsFirstTickOfBar && Open[0] < entryLevel)
                            {
                                abcShort = true;
                            }
                        }

                        if (abcShort)
                        {
                            if (ShowEntryArrows)
                            {
                                ABCEntryArrowDown[0] = High[0] + ABCEntryArrowYTickOffsetValue;
                            }
                            abcShortChanceInProgress = false;
                            entryLineStartBar = 0;
                            AbcSignals[0] = -2;
                            if (AlertAbcEntry)
                            {
                                Alert("Alert_Abc_Long_Entry" + alertTag++.ToString(), AlertAbcEntryPriority,
                                    "ABC Long Entry" + " (" + Instrument.FullName + " " + BarsPeriod + ")",
                                    AlertAbcLongEntrySoundFileName, 0, Brushes.Blue, Brushes.White);
                            }
                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Divergence
        private void DrawDivergence()
        {
            if (DivergenceIndicatorMode != DivergenceMode.Off)
            {
                #region Reset values
                if (IsFirstTickOfBar == true)
                {
                    if (DivergenceIndicatorMode != DivergenceMode.Off)
                    {
                        if (divHiddenShortActive == true)
                            DivergenceSignals[0] = 2;
                        else if (divRegularShortActive == true)
                            DivergenceSignals[0] = 1;
                        else if (divHiddenLongActive == true)
                            DivergenceSignals[0] = 2;
                        else if (divRegularLongActive == true)
                            DivergenceSignals[0] = 1;
                        else
                            DivergenceSignals[0] = 0;
                    }
                }
                #endregion

                if (DivergenceDirectionMode != DivergenceDirection.Short)
                {
                    if (swingCalculation.SwingHigh.New == 2)
                    {
                        drawTagDivDn++;
                        divLastSwing = swingCalculation.SwingHigh.LastPrice;
                        if (CurrentBar - swingCalculation.SwingHigh.LastBar - 1 >= 0)
                        {
                            divLastOscValue = Math.Max(DivergenceDataHigh[CurrentBar - swingCalculation.SwingHigh.LastBar + 1], Math.Max(DivergenceDataHigh[CurrentBar - swingCalculation.SwingHigh.LastBar], DivergenceDataHigh[CurrentBar - swingCalculation.SwingHigh.LastBar - 1]));
                        }
                        else return;
                    }

                    if (swingCalculation.SwingHigh.New > 0)
                    {
                        divCurSwing = swingCalculation.SwingHigh.CurPrice;
                        divCurOscValue = Math.Max(DivergenceDataHigh[CurrentBar - swingCalculation.SwingHigh.CurBar], DivergenceDataHigh[CurrentBar - swingCalculation.SwingHigh.CurBar + 1]);

                        if (ShowDivergenceHidden == true)
                        {
                            if (divLastSwing > divCurSwing
                                && divLastOscValue < divCurOscValue)
                            {
                                Divergences doesExist = DivergenceLines.Find(x => x.Id == drawTagDivDn && x.DivType == DivergenceType.Hidden && x.DivBias == DivergenceBias.Down);
                                if (doesExist == null)
                                {
                                    Divergences thisDivergence = new Divergences()
                                    {
                                        Id = drawTagDivDn,
                                        StartIdx = swingCalculation.SwingHigh.LastBar,
                                        StartY = swingCalculation.SwingHigh.LastPrice,
                                        EndIdx = swingCalculation.SwingHigh.CurBar,
                                        EndY = swingCalculation.SwingHigh.CurPrice,
                                        DivType = DivergenceType.Hidden,
                                        DivBias = DivergenceBias.Down
                                    };
                                    DivergenceLines.Add(thisDivergence);
                                }
                                else
                                {
                                    doesExist.StartIdx = swingCalculation.SwingHigh.LastBar;
                                    doesExist.StartY = swingCalculation.SwingHigh.LastPrice;
                                    doesExist.EndIdx = swingCalculation.SwingHigh.CurBar;
                                    doesExist.EndY = swingCalculation.SwingHigh.CurPrice;
                                }

                                divHiddenShortActive = true;
                                DivergenceSignals[0] = -2;
                            }
                            else
                            {
                                Divergences removeMe = DivergenceLines.Find(x => x.Id == drawTagDivDn && x.DivBias == DivergenceBias.Down && x.DivType == DivergenceType.Hidden);
                                DivergenceLines.Remove(removeMe);

                                divHiddenShortActive = false;
                            }
                        }

                        if (ShowDivergenceRegular == true)
                        {
                            if (divLastSwing < divCurSwing
                                && divLastOscValue > divCurOscValue)
                            {
                                Divergences doesExist = DivergenceLines.Find(x => x.Id == drawTagDivDn && x.DivBias == DivergenceBias.Down && x.DivType == DivergenceType.Regular);
                                if (doesExist == null)
                                {
                                    Divergences thisDivergence = new Divergences()
                                    {
                                        Id = drawTagDivDn,
                                        StartIdx = swingCalculation.SwingHigh.LastBar,
                                        StartY = swingCalculation.SwingHigh.LastPrice,
                                        EndIdx = swingCalculation.SwingHigh.CurBar,
                                        EndY = swingCalculation.SwingHigh.CurPrice,
                                        DivType = DivergenceType.Regular,
                                        DivBias = DivergenceBias.Down
                                    };
                                    DivergenceLines.Add(thisDivergence);
                                }
                                else
                                {
                                    doesExist.StartIdx = swingCalculation.SwingHigh.LastBar;
                                    doesExist.StartY = swingCalculation.SwingHigh.LastPrice;
                                    doesExist.EndIdx = swingCalculation.SwingHigh.CurBar;
                                    doesExist.EndY = swingCalculation.SwingHigh.CurPrice;
                                }

                                divRegularShortActive = true;
                                DivergenceSignals[0] = -1;
                            }
                            else
                            {
                                Divergences removeMe = DivergenceLines.Find(x => x.Id == drawTagDivDn && x.DivBias == DivergenceBias.Down && x.DivType == DivergenceType.Regular);
                                DivergenceLines.Remove(removeMe);

                                divRegularShortActive = false;
                            }
                        }
                    }
                }

                if (DivergenceDirectionMode != DivergenceDirection.Long)
                {
                    if (swingCalculation.SwingLow.New == 2)
                    {
                        drawTagDivUp++;
                        divLastSwing = swingCalculation.SwingLow.LastPrice;
                        if (CurrentBar - swingCalculation.SwingLow.LastBar - 1 >= 0)
                        {
                            divLastOscValue = Math.Min(DivergenceDataLow[CurrentBar - swingCalculation.SwingLow.LastBar + 1], Math.Min(DivergenceDataLow[CurrentBar - swingCalculation.SwingLow.LastBar],
                                              DivergenceDataLow[CurrentBar - swingCalculation.SwingLow.LastBar - 1]));
                        }
                        else return;
                    }

                    if (swingCalculation.SwingLow.New > 0)
                    {
                        divCurSwing = swingCalculation.SwingLow.CurPrice;
                        divCurOscValue = Math.Min(DivergenceDataLow[CurrentBar - swingCalculation.SwingLow.CurBar], DivergenceDataLow[CurrentBar - swingCalculation.SwingLow.CurBar + 1]);

                        if (ShowDivergenceHidden == true)
                        {
                            if (divLastSwing < divCurSwing
                                && divLastOscValue > divCurOscValue)
                            {
                                Divergences doesExist = DivergenceLines.Find(x => x.Id == drawTagDivUp && x.DivBias == DivergenceBias.Up && x.DivType == DivergenceType.Hidden);
                                if (doesExist == null)
                                {
                                    Divergences thisDivergence = new Divergences()
                                    {
                                        Id = drawTagDivUp,
                                        StartIdx = swingCalculation.SwingLow.LastBar,
                                        StartY = swingCalculation.SwingLow.LastPrice,
                                        EndIdx = swingCalculation.SwingLow.CurBar,
                                        EndY = swingCalculation.SwingLow.CurPrice,
                                        DivType = DivergenceType.Hidden,
                                        DivBias = DivergenceBias.Up
                                    };
                                    DivergenceLines.Add(thisDivergence);
                                }
                                else
                                {
                                    doesExist.StartIdx = swingCalculation.SwingLow.LastBar;
                                    doesExist.StartY = swingCalculation.SwingLow.LastPrice;
                                    doesExist.EndIdx = swingCalculation.SwingLow.CurBar;
                                    doesExist.EndY = swingCalculation.SwingLow.CurPrice;
                                }
                                divHiddenLongActive = true;
                                DivergenceSignals[0] = 2;
                            }
                            else
                            {
                                Divergences removeMe = DivergenceLines.Find(x => x.Id == drawTagDivUp && x.DivBias == DivergenceBias.Up && x.DivType == DivergenceType.Hidden);
                                DivergenceLines.Remove(removeMe);

                                divHiddenLongActive = false;
                            }
                        }

                        if (ShowDivergenceRegular == true)
                        {
                            if (divLastSwing > divCurSwing
                                && divLastOscValue < divCurOscValue)
                            {
                                Divergences doesExist = DivergenceLines.Find(x => x.Id == drawTagDivUp && x.DivBias == DivergenceBias.Up && x.DivType == DivergenceType.Regular);
                                if (doesExist == null)
                                {
                                    Divergences thisDivergence = new Divergences()
                                    {
                                        Id = drawTagDivUp,
                                        StartIdx = swingCalculation.SwingLow.LastBar,
                                        StartY = swingCalculation.SwingLow.LastPrice,
                                        EndIdx = swingCalculation.SwingLow.CurBar,
                                        EndY = swingCalculation.SwingLow.CurPrice,
                                        DivType = DivergenceType.Regular,
                                        DivBias = DivergenceBias.Up
                                    };
                                    DivergenceLines.Add(thisDivergence);
                                }
                                else
                                {
                                    doesExist.StartIdx = swingCalculation.SwingLow.LastBar;
                                    doesExist.StartY = swingCalculation.SwingLow.LastPrice;
                                    doesExist.EndIdx = swingCalculation.SwingLow.CurBar;
                                    doesExist.EndY = swingCalculation.SwingLow.CurPrice;
                                }

                                divRegularLongActive = true;
                                DivergenceSignals[0] = 1;
                            }
                            else
                            {
                                Divergences removeMe = DivergenceLines.Find(x => x.Id == drawTagDivUp && x.DivBias == DivergenceBias.Up && x.DivType == DivergenceType.Regular);
                                DivergenceLines.Remove(removeMe);

                                divRegularLongActive = false;
                            }
                        }
                    }
                }

                if (swingCalculation.SwingLow.New == 2)
                {
                    if (divRegularShortActive == true)
                    {
                        divRegularShortActive = false;
                        DivergenceSignals[0] = -3;
                    }
                    else if (divHiddenShortActive == true)
                    {
                        divHiddenShortActive = false;
                        DivergenceSignals[0] = -4;
                    }
                }
                else if (swingCalculation.SwingHigh.New == 2)
                {
                    if (divRegularLongActive == true)
                    {
                        divRegularLongActive = false;
                        DivergenceSignals[0] = 3;
                    }
                    else if (divHiddenLongActive == true)
                    {
                        divHiddenLongActive = false;
                        DivergenceSignals[0] = 4;
                    }
                }
            }
        }
        #endregion

        #region Statistic
        //#########################################################################################
        #region ComputeStatistic()
        //=========================================================================================
        private void ComputeStatistic()
        {
            if (Statistic != StatisticMode.Off)
            {
                // Up statistic
                if (swingCalculation.SwingLow.New == 2)
                {
                    upCount = swingCalculation.SwingHighs.Count - 1;
                    dnCount = swingCalculation.SwingLows.Count - 1;
                    if (upCount == 0)
                        return;

                    overallUpLength = overallUpLength + swingCalculation.SwingHighs[upCount].length;
                    overallAvgUpLength = Math.Round(overallUpLength / upCount, 0,
                        MidpointRounding.AwayFromZero);

                    overallUpDuration = overallUpDuration + swingCalculation.SwingHighs[upCount].duration;
                    overallAvgUpDuration = Math.Round(overallUpDuration / upCount, 0,
                        MidpointRounding.AwayFromZero);

                    if (upCount >= StatisticLength)
                    {
                        upLength = 0;
                        upDuration = 0;
                        for (int i = 0; i < StatisticLength; i++)
                        {
                            upLength = upLength + swingCalculation.SwingHighs[upCount - i].length;
                            upDuration = upDuration + swingCalculation.SwingHighs[upCount - i].duration;
                        }
                        avgUpLength = Math.Round(upLength / StatisticLength, 0,
                            MidpointRounding.AwayFromZero);
                        avgUpDuration = Math.Round(upDuration / StatisticLength, 0,
                            MidpointRounding.AwayFromZero);
                    }


                    if (upCount < 1 || dnCount == 0)
                        return;

                    if (swingCalculation.SwingHigh.LastRelation == 1)
                    {
                        hhCount++;

                        if (swingCalculation.SwingHigh.CurRelation == 1) hhCountHH++;
                        else if (swingCalculation.SwingHigh.CurRelation == -1) hhCountLH++;
                        else hhCountDT++;

                        if (swingCalculation.SwingLow.LastRelation == 1) hhCountHL++;
                        else if (swingCalculation.SwingLow.LastRelation == -1) hhCountLL++;
                        else hhCountDB++;

                        hhCountLHPercent = Math.Round(100.0 / hhCount * hhCountLH, 1,
                            MidpointRounding.AwayFromZero);
                        hhCountHHPercent = Math.Round(100.0 / hhCount * hhCountHH, 1,
                            MidpointRounding.AwayFromZero);
                        hhCountDTPercent = Math.Round(100.0 / hhCount * hhCountDT, 1,
                            MidpointRounding.AwayFromZero);

                        hhCountHLPercent = Math.Round(100.0 / hhCount * hhCountHL, 1,
                            MidpointRounding.AwayFromZero);
                        hhCountLLPercent = Math.Round(100.0 / hhCount * hhCountLL, 1,
                            MidpointRounding.AwayFromZero);
                        hhCountDBPercent = Math.Round(100.0 / hhCount * hhCountDB, 1,
                            MidpointRounding.AwayFromZero);
                    }
                    else if (swingCalculation.SwingHigh.LastRelation == -1)
                    {
                        lhCount++;

                        if (swingCalculation.SwingHigh.CurRelation == 1) lhCountHH++;
                        else if (swingCalculation.SwingHigh.CurRelation == -1) lhCountLH++;
                        else lhCountDT++;

                        if (swingCalculation.SwingLow.LastRelation == 1) lhCountHL++;
                        else if (swingCalculation.SwingLow.LastRelation == -1) lhCountLL++;
                        else lhCountDB++;

                        lhCountLHPercent = Math.Round(100.0 / lhCount * lhCountLH, 1,
                            MidpointRounding.AwayFromZero);
                        lhCountHHPercent = Math.Round(100.0 / lhCount * lhCountHH, 1,
                            MidpointRounding.AwayFromZero);
                        lhCountDTPercent = Math.Round(100.0 / lhCount * lhCountDT, 1,
                            MidpointRounding.AwayFromZero);

                        lhCountHLPercent = Math.Round(100.0 / lhCount * lhCountHL, 1,
                            MidpointRounding.AwayFromZero);
                        lhCountLLPercent = Math.Round(100.0 / lhCount * lhCountLL, 1,
                            MidpointRounding.AwayFromZero);
                        lhCountDBPercent = Math.Round(100.0 / lhCount * lhCountDB, 1,
                            MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        dtCount++;

                        if (swingCalculation.SwingHigh.CurRelation == 1) dtCountHH++;
                        else if (swingCalculation.SwingHigh.CurRelation == -1) dtCountLH++;
                        else dtCountDT++;

                        if (swingCalculation.SwingLow.LastRelation == 1) dtCountHL++;
                        else if (swingCalculation.SwingLow.LastRelation == -1) dtCountLL++;
                        else dtCountDB++;

                        dtCountLHPercent = Math.Round(100.0 / dtCount * dtCountLH, 1,
                            MidpointRounding.AwayFromZero);
                        dtCountHHPercent = Math.Round(100.0 / dtCount * dtCountHH, 1,
                            MidpointRounding.AwayFromZero);
                        dtCountDTPercent = Math.Round(100.0 / dtCount * dtCountDT, 1,
                            MidpointRounding.AwayFromZero);

                        dtCountHLPercent = Math.Round(100.0 / dtCount * dtCountHL, 1,
                            MidpointRounding.AwayFromZero);
                        dtCountLLPercent = Math.Round(100.0 / dtCount * dtCountLL, 1,
                            MidpointRounding.AwayFromZero);
                        dtCountDBPercent = Math.Round(100.0 / dtCount * dtCountDB, 1,
                            MidpointRounding.AwayFromZero);
                    }

                    PrintStatistic();
                }
                // Down statistic
                else if (swingCalculation.SwingHigh.New == 2)
                {
                    upCount = swingCalculation.SwingHighs.Count - 1;
                    dnCount = swingCalculation.SwingLows.Count - 1;
                    if (dnCount == 0)
                        return;

                    overallDnLength = overallDnLength + swingCalculation.SwingLows[dnCount].length;
                    overallAvgDnLength = Math.Round(overallDnLength / dnCount, 0,
                        MidpointRounding.AwayFromZero);

                    overallDnDuration = overallDnDuration + swingCalculation.SwingLows[dnCount].duration;
                    overallAvgDnDuration = Math.Round(overallDnDuration / dnCount, 0,
                        MidpointRounding.AwayFromZero);

                    if (dnCount >= StatisticLength)
                    {
                        dnLength = 0;
                        dnDuration = 0;
                        for (int i = 0; i < StatisticLength; i++)
                        {
                            dnLength = dnLength + swingCalculation.SwingLows[dnCount - i].length;
                            dnDuration = dnDuration + swingCalculation.SwingLows[dnCount - i].duration;
                        }
                        avgDnLength = Math.Round(dnLength / StatisticLength, 0,
                            MidpointRounding.AwayFromZero);
                        avgDnDuration = Math.Round(dnDuration / StatisticLength, 0,
                            MidpointRounding.AwayFromZero);
                    }

                    if (dnCount < 1 || upCount == 0)
                        return;

                    if (swingCalculation.SwingLow.LastRelation == -1)
                    {
                        llCount++;

                        if (swingCalculation.SwingHigh.LastRelation == 1) llCountHH++;
                        else if (swingCalculation.SwingHigh.LastRelation == -1) llCountLH++;
                        else llCountDT++;

                        if (swingCalculation.SwingLow.CurRelation == 1) llCountHL++;
                        else if (swingCalculation.SwingLow.CurRelation == -1) llCountLL++;
                        else llCountDB++;

                        llCountLHPercent = Math.Round(100.0 / llCount * llCountLH, 1,
                            MidpointRounding.AwayFromZero);
                        llCountHHPercent = Math.Round(100.0 / llCount * llCountHH, 1,
                            MidpointRounding.AwayFromZero);
                        llCountDTPercent = Math.Round(100.0 / llCount * llCountDT, 1,
                            MidpointRounding.AwayFromZero);

                        llCountHLPercent = Math.Round(100.0 / llCount * llCountHL, 1,
                            MidpointRounding.AwayFromZero);
                        llCountLLPercent = Math.Round(100.0 / llCount * llCountLL, 1,
                            MidpointRounding.AwayFromZero);
                        llCountDBPercent = Math.Round(100.0 / llCount * llCountDB, 1,
                            MidpointRounding.AwayFromZero);
                    }
                    else if (swingCalculation.SwingLow.LastRelation == 1)
                    {
                        hlCount++;

                        if (swingCalculation.SwingHigh.LastRelation == 1) hlCountHH++;
                        else if (swingCalculation.SwingHigh.LastRelation == -1) hlCountLH++;
                        else hlCountDT++;

                        if (swingCalculation.SwingLow.CurRelation == 1) hlCountHL++;
                        else if (swingCalculation.SwingLow.CurRelation == -1) hlCountLL++;
                        else hlCountDB++;

                        hlCountLHPercent = Math.Round(100.0 / hlCount * hlCountLH, 1,
                            MidpointRounding.AwayFromZero);
                        hlCountHHPercent = Math.Round(100.0 / hlCount * hlCountHH, 1,
                            MidpointRounding.AwayFromZero);
                        hlCountDTPercent = Math.Round(100.0 / hlCount * hlCountDT, 1,
                            MidpointRounding.AwayFromZero);

                        hlCountHLPercent = Math.Round(100.0 / hlCount * hlCountHL, 1,
                            MidpointRounding.AwayFromZero);
                        hlCountLLPercent = Math.Round(100.0 / hlCount * hlCountLL, 1,
                            MidpointRounding.AwayFromZero);
                        hlCountDBPercent = Math.Round(100.0 / hlCount * hlCountDB, 1,
                            MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        dbCount++;

                        if (swingCalculation.SwingHigh.LastRelation == 1) dbCountHH++;
                        else if (swingCalculation.SwingHigh.LastRelation == -1) dbCountLH++;
                        else dbCountDT++;

                        if (swingCalculation.SwingLow.CurRelation == 1) dbCountHL++;
                        else if (swingCalculation.SwingLow.CurRelation == -1) dbCountLL++;
                        else dbCountDB++;

                        dbCountLHPercent = Math.Round(100.0 / dbCount * dbCountLH, 1,
                            MidpointRounding.AwayFromZero);
                        dbCountHHPercent = Math.Round(100.0 / dbCount * dbCountHH, 1,
                            MidpointRounding.AwayFromZero);
                        dbCountDTPercent = Math.Round(100.0 / dbCount * dbCountDT, 1,
                            MidpointRounding.AwayFromZero);

                        dbCountHLPercent = Math.Round(100.0 / dbCount * dbCountHL, 1,
                            MidpointRounding.AwayFromZero);
                        dbCountLLPercent = Math.Round(100.0 / dbCount * dbCountLL, 1,
                            MidpointRounding.AwayFromZero);
                        dbCountDBPercent = Math.Round(100.0 / dbCount * dbCountDB, 1,
                            MidpointRounding.AwayFromZero);
                    }

                    PrintStatistic();
                }
            }
        }
        //=========================================================================================
        #endregion

        #region PrintStatistic()
        //=========================================================================================
        private void PrintStatistic()
        {
            switch (Statistic)
            {
                case StatisticMode.Off:
                    break;
                case StatisticMode.NinjaSciptOutput:
                    #region NinjaScriptOutput
                    if (ClearNinjaScriptOutputWindow)
                    {
                        ClearOutputWindow();
                    }

                    string[] displaySwing = { "HH", "LH", "DT", "HL", "LL", "DB" };
                    decimal[] displaySwingCount = { hhCount, lhCount, dtCount, hlCount, llCount, dbCount };
                    double[] displaySwingHH = { hhCountHHPercent, lhCountHHPercent, dtCountHHPercent,
                        hlCountHHPercent, llCountHHPercent, dbCountHHPercent};
                    double[] displaySwingLH = { hhCountLHPercent, lhCountLHPercent, dtCountLHPercent,
                        hlCountLHPercent, llCountLHPercent, dbCountLHPercent};
                    double[] displaySwingDT = { hhCountDTPercent, lhCountDTPercent, dtCountDTPercent,
                        hlCountDTPercent, llCountDTPercent, dbCountDTPercent};

                    double[] displaySwingHL = { hhCountHLPercent, lhCountHLPercent, dtCountHLPercent,
                        hlCountHLPercent, llCountHLPercent, dbCountHLPercent};
                    double[] displaySwingLL = { hhCountLLPercent, lhCountLLPercent, dtCountLLPercent,
                        hlCountLLPercent, llCountLLPercent, dbCountLLPercent};
                    double[] displaySwingDB = { hhCountDBPercent, lhCountDBPercent, dtCountDBPercent,
                        hlCountDBPercent, llCountDBPercent, dbCountDBPercent};

                    Print(Instrument.FullName + " " + Bars.BarsPeriod.Value + " " + Bars.BarsPeriod.BarsPeriodType);
                    string[] displayLength = { "Up", "Down" };
                    decimal[] displayCount = { upCount, dnCount };
                    double[] displayOverallAvgLength = { overallAvgUpLength, overallAvgDnLength };
                    double[] displayAvgLength = { avgUpLength, avgDnLength };
                    double[] displayOverallAvgDuration = { overallAvgUpDuration, overallAvgDnDuration };
                    double[] displayAvgDuration = { avgUpDuration, avgDnDuration };

                    Print("-----------------------------------------------------");
                    Print(String.Format("|{0,-5} {1,6} | {2,7} {3,7} | {4,8} {5,8} |\n",
                        "Swing", "Count", "Average", "Current", "Average", "Current"));
                    Print(String.Format("|{0,-5} {1,6} | {2,7} {3,7} | {4,8} {5,8} |\n",
                        "Swing", "Count", "Length", "Length", "Duration", "Duration"));
                    Print("-----------------------------------------------------");
                    for (int ctr = 0; ctr < displayLength.Length; ctr++)
                    {
                        Print(String.Format("|{0,-5} {1,6} | {2,7} {3,7} | {4,8} {5,8} |\n",
                            displayLength[ctr], displayCount[ctr],
                            displayOverallAvgLength[ctr], displayAvgLength[ctr],
                            displayOverallAvgDuration[ctr], displayAvgDuration[ctr]));
                    }
                    Print("-----------------------------------------------------");
                    Print("");

                    Print("----------------------------------------------------");
                    Print(String.Format("|{0,12} | {1,-16} | {2,-16}|\n",
                        "", "High followed by", "Low  followed by"));
                    Print(String.Format("|{0,-5} {1,6} |{2,5} {3,5} {4,5} |{5,5} {6,5} {7,5}|\n",
                        "Swing", "Count", "HH %", "LH %", "DT %", "HL %", "LL %", "DB %"));
                    Print("----------------------------------------------------");
                    for (int ctr = 0; ctr < displaySwing.Length; ctr++)
                    {
                        if (ctr == 3)
                            Print("----------------------------------------------------");

                        Print(String.Format(
                            "|{0,-5} {1,6} |{2,5:F1} {3,5:F1} {4,5:F1} |{5,5:F1} {6,5:F1} {7,5:F1}|",
                            displaySwing[ctr], displaySwingCount[ctr],
                            displaySwingHH[ctr], displaySwingLH[ctr], displaySwingDT[ctr],
                            displaySwingHL[ctr], displaySwingLL[ctr], displaySwingDB[ctr]));
                    }
                    Print("----------------------------------------------------");
                    break;
                #endregion
                case StatisticMode.Table:
                    #region Table
                    if (swingCalculation.SwingHigh.New == 2)
                    {
                        swingStatsList[1].count = dnCount;
                        swingStatsList[1].avgLength = overallAvgDnLength;
                        swingStatsList[1].curLength = avgDnLength;
                        swingStatsList[1].avgDuration = overallAvgDnDuration;
                        swingStatsList[1].curDuration = avgDnDuration;

                        swingRelationList[3].count = hlCount;
                        swingRelationList[3].followingHH = hlCountHHPercent.ToString("0.0");
                        swingRelationList[3].followingLH = hlCountLHPercent.ToString("0.0");
                        swingRelationList[3].followingDT = hlCountDTPercent.ToString("0.0");
                        swingRelationList[3].followingHL = hlCountHLPercent.ToString("0.0");
                        swingRelationList[3].followingLL = hlCountLLPercent.ToString("0.0");
                        swingRelationList[3].followingDB = hlCountDBPercent.ToString("0.0");

                        swingRelationList[4].count = llCount;
                        swingRelationList[4].followingHH = llCountHHPercent.ToString("0.0");
                        swingRelationList[4].followingLH = llCountLHPercent.ToString("0.0");
                        swingRelationList[4].followingDT = llCountDTPercent.ToString("0.0");
                        swingRelationList[4].followingHL = llCountHLPercent.ToString("0.0");
                        swingRelationList[4].followingLL = llCountLLPercent.ToString("0.0");
                        swingRelationList[4].followingDB = llCountDBPercent.ToString("0.0");

                        swingRelationList[5].count = dbCount;
                        swingRelationList[5].followingHH = dbCountHHPercent.ToString("0.0");
                        swingRelationList[5].followingLH = dbCountLHPercent.ToString("0.0");
                        swingRelationList[5].followingDT = dbCountDTPercent.ToString("0.0");
                        swingRelationList[5].followingHL = dbCountHLPercent.ToString("0.0");
                        swingRelationList[5].followingLL = dbCountLLPercent.ToString("0.0");
                        swingRelationList[5].followingDB = dbCountDBPercent.ToString("0.0");
                    }
                    else if (swingCalculation.SwingLow.New == 2)
                    {
                        swingStatsList[0].count = upCount;
                        swingStatsList[0].avgLength = overallAvgUpLength;
                        swingStatsList[0].curLength = avgUpLength;
                        swingStatsList[0].avgDuration = overallAvgUpDuration;
                        swingStatsList[0].curDuration = avgUpDuration;

                        swingRelationList[0].count = hhCount;
                        swingRelationList[0].followingHH = hhCountHHPercent.ToString("0.0");
                        swingRelationList[0].followingLH = hhCountLHPercent.ToString("0.0");
                        swingRelationList[0].followingDT = hhCountDTPercent.ToString("0.0");
                        swingRelationList[0].followingHL = hhCountHLPercent.ToString("0.0");
                        swingRelationList[0].followingLL = hhCountLLPercent.ToString("0.0");
                        swingRelationList[0].followingDB = hhCountDBPercent.ToString("0.0");

                        swingRelationList[1].count = lhCount;
                        swingRelationList[1].followingHH = lhCountHHPercent.ToString("0.0");
                        swingRelationList[1].followingLH = lhCountLHPercent.ToString("0.0");
                        swingRelationList[1].followingDT = lhCountDTPercent.ToString("0.0");
                        swingRelationList[1].followingHL = lhCountHLPercent.ToString("0.0");
                        swingRelationList[1].followingLL = lhCountLLPercent.ToString("0.0");
                        swingRelationList[1].followingDB = lhCountDBPercent.ToString("0.0");

                        swingRelationList[2].count = dtCount;
                        swingRelationList[2].followingHH = dtCountHHPercent.ToString("0.0");
                        swingRelationList[2].followingLH = dtCountLHPercent.ToString("0.0");
                        swingRelationList[2].followingDT = dtCountDTPercent.ToString("0.0");
                        swingRelationList[2].followingHL = dtCountHLPercent.ToString("0.0");
                        swingRelationList[2].followingLL = dtCountLLPercent.ToString("0.0");
                        swingRelationList[2].followingDB = dtCountDBPercent.ToString("0.0");
                    }

                    ChartControl.Dispatcher.InvokeAsync((System.Action)(() =>
                    {
                        lengthGrid.Items.Refresh();
                        relationGrid.Items.Refresh();
                    }));
                    #endregion
                    break;
            }
        }
        //=========================================================================================
        #endregion
        //#########################################################################################
        #endregion

        #region Fibs (this is not rendered since we're using the Ninjatrader Fib tools.)
        private void DrawFibonacci()
        {
            if (swingCalculation.SwingHigh.New > 1 || swingCalculation.SwingLow.New > 1)
            {
                #region Fibonacci extensions
                //---------------------------------------------------------------------------------
                if (AddSwingExtension == true)
                {
                    if (swingCalculation.SwingHigh.LastPrice == 0.0 || swingCalculation.SwingLow.LastPrice == 0.0)
                    {
                        return;
                    }

                    if (swingCalculation.SwingLows[swingCalculation.SwingLows.Count - 1].relation == 1
                        && swingCalculation.CurrentSwing.SwingSlope == -1)
                    {
                        int anchor1BarsAgo = CurrentBar - swingCalculation.SwingLow.LastBar;
                        int anchor2BarsAgo = CurrentBar - swingCalculation.SwingHigh.CurBar;
                        int anchor3BarsAgo = CurrentBar - swingCalculation.SwingLow.CurBar;
                        double anchor1Y = swingCalculation.SwingLow.LastPrice;
                        double anchor2Y = swingCalculation.SwingHigh.CurPrice;
                        double anchor3Y = swingCalculation.SwingLow.CurPrice;
                        Draw.FibonacciExtensions(this, "FibExtUp", false, anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else if (swingCalculation.SwingLows[swingCalculation.SwingLows.Count - 1].relation == 1
                             && swingCalculation.CurrentSwing.SwingSlope == 1)
                    {
                        int anchor1BarsAgo = CurrentBar - swingCalculation.SwingLow.LastBar;
                        int anchor2BarsAgo = CurrentBar - swingCalculation.SwingHigh.LastBar;
                        int anchor3BarsAgo = CurrentBar - swingCalculation.SwingLow.CurBar;
                        double anchor1Y = swingCalculation.SwingLow.LastPrice;
                        double anchor2Y = swingCalculation.SwingHigh.LastPrice;
                        double anchor3Y = swingCalculation.SwingLow.CurPrice;
                        Draw.FibonacciExtensions(this, "FibExtUp", false, anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else
                    {
                        RemoveDrawObject("FibExtUp");
                    }

                    if (swingCalculation.SwingHighs[swingCalculation.SwingHighs.Count - 1].relation == -1
                        && swingCalculation.CurrentSwing.SwingSlope == 1)
                    {
                        int anchor1BarsAgo = CurrentBar - swingCalculation.SwingHigh.LastBar;
                        int anchor2BarsAgo = CurrentBar - swingCalculation.SwingLow.CurBar;
                        int anchor3BarsAgo = CurrentBar - swingCalculation.SwingHigh.CurBar;
                        double anchor1Y = swingCalculation.SwingHigh.LastPrice;
                        double anchor2Y = swingCalculation.SwingLow.CurPrice;
                        double anchor3Y = swingCalculation.SwingHigh.CurPrice;
                        Draw.FibonacciExtensions(this, "FibExtDn", false, anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else if (swingCalculation.SwingHighs[swingCalculation.SwingHighs.Count - 1].relation == -1
                             && swingCalculation.CurrentSwing.SwingSlope == -1)
                    {
                        int anchor1BarsAgo = CurrentBar - swingCalculation.SwingHigh.LastBar;
                        int anchor2BarsAgo = CurrentBar - swingCalculation.SwingLow.LastBar;
                        int anchor3BarsAgo = CurrentBar - swingCalculation.SwingHigh.CurBar;
                        double anchor1Y = swingCalculation.SwingHigh.LastPrice;
                        double anchor2Y = swingCalculation.SwingLow.LastPrice;
                        double anchor3Y = swingCalculation.SwingHigh.CurPrice;
                        Draw.FibonacciExtensions(this, "FibExtDn", false, anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else
                    {
                        RemoveDrawObject("FibExtDn");
                    }
                }
                #endregion

                #region Fibonacci retracements
                if (AddSwingRetracementFast == true)
                {
                    int anchor1BarsAgo;
                    int anchor2BarsAgo;
                    double anchor1Y;
                    double anchor2Y;

                    if (swingCalculation.CurrentSwing.SwingSlope == 1)
                    {
                        anchor1BarsAgo = CurrentBar - swingCalculation.SwingLow.CurBar;
                        anchor1Y = swingCalculation.SwingLow.CurPrice;
                        anchor2BarsAgo = CurrentBar - swingCalculation.SwingHigh.CurBar;
                        anchor2Y = swingCalculation.SwingHigh.CurPrice;
                    }
                    else
                    {
                        anchor1BarsAgo = CurrentBar - swingCalculation.SwingHigh.CurBar;
                        anchor1Y = swingCalculation.SwingHigh.CurPrice;
                        anchor2BarsAgo = CurrentBar - swingCalculation.SwingLow.CurBar;
                        anchor2Y = swingCalculation.SwingLow.CurPrice;
                    }
                    Draw.FibonacciRetracements(this, "FastFibRet", IsAutoScale, anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y);
                }

                if (AddSwingRetracementSlow == true)
                {
                    if (swingCalculation.SwingHigh.LastPrice == 0.0 || swingCalculation.SwingLow.LastPrice == 0.0)
                    {
                        return;
                    }

                    int anchor1BarsAgo;
                    int anchor2BarsAgo;
                    double anchor1Y;
                    double anchor2Y;

                    if (swingCalculation.CurrentSwing.SwingSlope == 1)
                    {
                        anchor1BarsAgo = CurrentBar - swingCalculation.SwingHigh.LastBar;
                        anchor1Y = swingCalculation.SwingHigh.LastPrice;
                        anchor2BarsAgo = CurrentBar - swingCalculation.SwingLow.CurBar;
                        anchor2Y = swingCalculation.SwingLow.CurPrice;
                    }
                    else
                    {
                        anchor1BarsAgo = CurrentBar - swingCalculation.SwingLow.LastBar;
                        anchor1Y = swingCalculation.SwingLow.LastPrice;
                        anchor2BarsAgo = CurrentBar - swingCalculation.SwingHigh.CurBar;
                        anchor2Y = swingCalculation.SwingHigh.CurPrice;
                    }

                    if ((swingCalculation.CurrentSwing.SwingSlope == 1 && swingCalculation.SwingHigh.CurPrice < swingCalculation.SwingHigh.LastPrice)
                        || (swingCalculation.CurrentSwing.SwingSlope == -1 && swingCalculation.SwingLow.CurPrice > swingCalculation.SwingLow.LastPrice))
                    {
                        Draw.FibonacciRetracements(this, "SlowFibRet", IsAutoScale, anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y);
                    }
                    else
                    {
                        RemoveDrawObject("SlowFibRet");
                    }
                }
                #endregion
            }
        }
        #endregion

        #region Properties
        #region UI_Parameters
        [RefreshProperties(RefreshProperties.All)]
        [NinjaScriptProperty]
        [Display(Name = "Swing Type", Description = "Represents the swing type for the swings.", Order = 1, GroupName = "Parameters")]
        public SwingStyle SwingStyleType
        { get; set; }

        [Range(0.00000001, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Swing Size", Description = "Represents the swing size for the swings. E.g. 1 = small and 5 = bigger swings.", Order = 2, GroupName = "Parameters")]
        public double SwingSize
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Double Top/-Bottom Strength", Description = "Represents the double top/-bottom strength. Increase the value to get more DBs/DTs.", Order = 3, GroupName = "Parameters")]
        public int DtbStrength
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Close Values", Description = "Indicates if high and low prices are used for the swing calculations or close values.", Order = 4, GroupName = "Parameters")]
        public bool UseCloseValues
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Ignore Inside Bars", Description = "Indicates if inside bars are ignored. If set to true it is possible that between consecutive up/down bars are inside bars. Only used if calculationSize > 1.", Order = 5, GroupName = "Parameters")]
        public bool IgnoreInsideBars
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Breakouts", Description = "Indicates if the swings are updated if the last swing high/low is broken. Only used if calculationSize > 1.", Order = 6, GroupName = "Parameters")]
        public bool UseBreakouts
        { get; set; }
        #endregion

        #region Features
        [RefreshProperties(RefreshProperties.All)]
        [Display(Name = "ABC Pattern", Description = "Indicates if and for which direction AB=CD patterns are computed.", Order = 1, GroupName = "Features")]
        public AbcPatternMode AbcPattern
        { get; set; }

        [RefreshProperties(RefreshProperties.All)]
        [Display(Name = "Divergence indicator", Description = "Represents the indicator for the divergence calculations.", Order = 2, GroupName = "Features")]
        public DivergenceMode DivergenceIndicatorMode
        { get; set; }

        [Display(Name = "Statistic", Description = "Indicates if and where a swing statistic is shown.", Order = 3, GroupName = "Features")]
        public StatisticMode Statistic
        { get; set; }

        [RefreshProperties(RefreshProperties.All)]
        [Display(Name = "Naked swing lines", Description = "Indicates if naked swing lines are shown.", Order = 4, GroupName = "Features")]
        public bool ShowNakedSwings
        { get; set; }

        [Display(Name = "Swing extensions", Description = "Indicates if a swing extension is drawn on the chart.", Order = 5, GroupName = "Features")]
        public bool AddSwingExtension
        { get; set; }

        [Display(Name = "Swing retracement (current)", Description = "Indicates if a swing retracement is drawn on the chart for the current swing.", Order = 6, GroupName = "Features")]
        public bool AddSwingRetracementFast
        { get; set; }

        [Display(Name = "Swing retracement (last)", Description = "Indicates if a swing retracement is drawn on the chart for the last swing.", Order = 7, GroupName = "Features")]
        public bool AddSwingRetracementSlow
        { get; set; }

        [Display(Name = "Show swing switch", Description = "Represents if the bar is highlighted were the swing switch happened from up to down or down to up.", Order = 8, GroupName = "Features")]
        public bool ShowSwingSwitch
        { get; set; }
        #endregion

        #region UI Swings Values
        [Display(Name = "Length", Description = "Represents the swing length visualization type for the swings.", Order = 1, GroupName = "Swing Values")]
        public virtual SwingLengthStyle SwingLengthType
        { get; set; }

        [Display(Name = "Duration", Description = "Represents the swing duration visualization type for the swings.", Order = 2, GroupName = "Swing Values")]
        public virtual SwingDurationStyle SwingDurationType
        { get; set; }

        [Display(Name = "Labels", Description = "Indicates if the swing label is shown (HH, HL, LL, LH, DB, DT).", Order = 3, GroupName = "Swing Values")]
        public virtual bool ShowSwingLabel
        { get; set; }

        [Display(Name = "Percentage", Description = "Indicates if the swing percentage in relation to the last swing is shown.", Order = 4, GroupName = "Swing Values")]
        public virtual bool ShowSwingPercent
        { get; set; }

        [Display(Name = "Time", Description = "Represents the swing time visualization type for the swings.", Order = 5, GroupName = "Swing Values")]
        public virtual SwingTimeStyle SwingTimeType
        { get; set; }

        [Display(Name = "Volume", Description = "Represents the swing volume visualization type for the swings.", Order = 6, GroupName = "Swing Values")]
        public virtual SwingVolumeStyle SwingVolumeType
        { get; set; }
        #endregion

        #region UI Swing Visualization
        [RefreshProperties(RefreshProperties.All)]
        [Display(Name = "Visualization Type", Description = "Represents the swing visualization type for the swings.", Order = 1, GroupName = "Swing Visualization")]
        public virtual VisualizationStyle VisualizationType
        { get; set; }

        [Display(Name = "Zig-Zag Style", Description = "Represents the line style of the zig-zag lines.", Order = 2, GroupName = "Swing Visualization")]
        public virtual DashStyleHelper ZigZagStyle
        { get; set; }

        [Display(Name = "Zig-Zag Width", Description = "Represents the line width of the zig-zag lines.", Order = 3, GroupName = "Swing Visualization")]
        public virtual int ZigZagWidth
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Zig-Zag Color Up", Description = "Represents the color of the zig-zag up lines.", Order = 4, GroupName = "Swing Visualization")]
        public virtual Brush ZigZagColorUp
        { get; set; }

        [Browsable(false)]
        public string ZigZagColorUpSerializable
        {
            get { return Serialize.BrushToString(ZigZagColorUp); }
            set { ZigZagColorUp = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Zig-Zag Color Down", Description = "Represents the color of the zig-zag down lines.", Order = 5, GroupName = "Swing Visualization")]
        public virtual Brush ZigZagColorDn
        { get; set; }

        [Browsable(false)]
        public string ZigZagColorDnSerializable
        {
            get { return Serialize.BrushToString(ZigZagColorDn); }
            set { ZigZagColorDn = Serialize.StringToBrush(value); }
        }

        [Display(Name = "Text Font", Description = "Represents the text font for the swing value output.", Order = 6, GroupName = "Swing Visualization")]
        public virtual SimpleFont TextFont
        { get; set; }

        [Display(Name = "Text Offset Length/Duration", Description = "Represents the text offset in pixel for the swing length/duration.", Order = 7, GroupName = "Swing Visualization")]
        public virtual int TextOffsetLength
        { get; set; }

        [Display(Name = "Text Offset Swing Labels", Description = "Represents the text offset in pixel for the swing labels.", Order = 8, GroupName = "Swing Visualization")]
        public virtual int TextOffsetLabel
        { get; set; }

        [Display(Name = "Text Offset Volume", Description = "Represents the text offset in pixel for the swing volume.", Order = 9, GroupName = "Swing Visualization")]
        public virtual int TextOffsetVolume
        { get; set; }

        [Display(Name = "Text Offset Time", Description = "Represents the text offset in pixel for the time value.", Order = 10, GroupName = "Swing Visualization")]
        public virtual int TextOffsetTime
        { get; set; }

        [Display(Name = "Text Offset Percent", Description = "Represents the text offset in pixel for the retracement value.", Order = 11, GroupName = "Swing Visualization")]
        public virtual int TextOffsetPercent
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Text Color Higher High", Description = "Represents the color of the swing value output for higher highs.", Order = 12, GroupName = "Swing Visualization")]
        public virtual Brush TextColorHigherHigh
        { get; set; }

        [Browsable(false)]
        public virtual string TextColorHigherHighSerializable
        {
            get { return Serialize.BrushToString(TextColorHigherHigh); }
            set { TextColorHigherHigh = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Text Color Lower High", Description = "Represents the color of the swing value output for lower highs.", Order = 13, GroupName = "Swing Visualization")]
        public virtual Brush TextColorLowerHigh
        { get; set; }

        [Browsable(false)]
        public string TextColorLowerHighSerializable
        {
            get { return Serialize.BrushToString(TextColorLowerHigh); }
            set { TextColorLowerHigh = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Text Color Double Top", Description = "Represents the color of the swing value output for double tops.", Order = 14, GroupName = "Swing Visualization")]
        public virtual Brush TextColorDoubleTop
        { get; set; }

        [Browsable(false)]
        public string TextColorDoubleTopSerializable
        {
            get { return Serialize.BrushToString(TextColorDoubleTop); }
            set { TextColorDoubleTop = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Text Color Higher Low", Description = "Represents the color of the swing value output for higher lows.", Order = 15, GroupName = "Swing Visualization")]
        public virtual Brush TextColorHigherLow
        { get; set; }

        [Browsable(false)]
        public string TextColorHigherLowSerializable
        {
            get { return Serialize.BrushToString(TextColorHigherLow); }
            set { TextColorHigherLow = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Text Color Lower Low", Description = "Represents the color of the swing value output for lower lows.", Order = 16, GroupName = "Swing Visualization")]
        public virtual Brush TextColorLowerLow
        { get; set; }

        [Browsable(false)]
        public string TextColorLowerLowSerializable
        {
            get { return Serialize.BrushToString(TextColorLowerLow); }
            set { TextColorLowerLow = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Text Color Double Bottom", Description = "Represents the color of the swing value output for double bottoms.", Order = 17, GroupName = "Swing Visualization")]
        public virtual Brush TextColorDoubleBottom
        { get; set; }

        [Browsable(false)]
        public virtual string TextColorDoubleBottomSerializable
        {
            get { return Serialize.BrushToString(TextColorDoubleBottom); }
            set { TextColorDoubleBottom = Serialize.StringToBrush(value); }
        }

        [Range(0, int.MaxValue)]
        [Display(Name = "Swing switch y-tick offset", Description = "Represents the number of ticks the swing switch arrow is placed away.", Order = 18, GroupName = "Swing Visualization")]
        public int SwingSwitchYTickOffset
        { get; set; }
        #endregion

        #region Dot properties
        #region Double bottom
        [XmlIgnore]
        [Display(Name = "Double Bottom dot color", Description = "Represents the color of the swing value output for Double Bottoms.", Order = 1, GroupName = "Dot Visualization")]
        public virtual Brush DoubleBottomDotColor
        { get; set; }

        [Browsable(false)]
        public string DoubleBottomDotColorSerializable
        {
            get { return Serialize.BrushToString(DoubleBottomDotColor); }
            set { DoubleBottomDotColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10)]
        [Display(Name = "Double Bottom dot size", Description = "Represents the dot size for Double Bottoms.", Order = 2, GroupName = "Dot Visualization")]
        public virtual int DoubleBottomDotSize
        { get; set; }
        #endregion

        #region LowerLow
        [XmlIgnore]
        [Display(Name = "Lower Low dot color", Description = "Represents the color of the swing value output for Lower Lows.", Order = 3, GroupName = "Dot Visualization")]
        public virtual Brush LowerLowDotColor
        { get; set; }

        [Browsable(false)]
        public string LowerLowDotColorSerializable
        {
            get { return Serialize.BrushToString(LowerLowDotColor); }
            set { LowerLowDotColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10)]
        [Display(Name = "Lower Low dot size", Description = "Represents the dot size for Lower Lows.", Order = 4, GroupName = "Dot Visualization")]
        public virtual int LowerLowDotSize
        { get; set; }
        #endregion

        #region HigherLow
        [XmlIgnore]
        [Display(Name = "Higher Low dot color", Description = "Represents the color of the swing value output for Higher Lows.", Order = 5, GroupName = "Dot Visualization")]
        public virtual Brush HigherLowDotColor
        { get; set; }

        [Browsable(false)]
        public string HigherLowDotColorSerializable
        {
            get { return Serialize.BrushToString(HigherLowDotColor); }
            set { HigherLowDotColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10)]
        [Display(Name = "Higher Low dot size", Description = "Represents the dot size for Higher Lows.", Order = 6, GroupName = "Dot Visualization")]
        public virtual int HigherLowDotSize
        { get; set; }
        #endregion

        #region DoubleTop
        [XmlIgnore]
        [Display(Name = "Double Top dot color", Description = "Represents the color of the swing value output for Double Tops.", Order = 7, GroupName = "Dot Visualization")]
        public virtual Brush DoubleTopDotColor
        { get; set; }

        [Browsable(false)]
        public string DoubleTopDotColorSerializable
        {
            get { return Serialize.BrushToString(DoubleTopDotColor); }
            set { DoubleTopDotColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10)]
        [Display(Name = "Double Top dot size", Description = "Represents the dot size for Double Tops.", Order = 8, GroupName = "Dot Visualization")]
        public virtual int DoubleTopDotSize
        { get; set; }
        #endregion

        #region LowerHigh
        [XmlIgnore]
        [Display(Name = "Lower High dot color", Description = "Represents the color of the swing value output for Lower Highs.", Order = 9, GroupName = "Dot Visualization")]
        public virtual Brush LowerHighDotColor
        { get; set; }

        [Browsable(false)]
        public string LowerHighDotColorSerializable
        {
            get { return Serialize.BrushToString(LowerHighDotColor); }
            set { LowerHighDotColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10)]
        [Display(Name = "Lower High dot size", Description = "Represents the dot size for Lower Highs.", Order = 10, GroupName = "Dot Visualization")]
        public virtual int LowerHighDotSize
        { get; set; }
        #endregion

        #region HigherHigh
        [XmlIgnore]
        [Display(Name = "Higher High dot color", Description = "Represents the color of the swing value output for Higher Highs.", Order = 11, GroupName = "Dot Visualization")]
        public virtual Brush HigherHighDotColor
        { get; set; }

        [Browsable(false)]
        public string HigherHighDotColorSerializable
        {
            get { return Serialize.BrushToString(HigherHighDotColor); }
            set { HigherHighDotColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10)]
        [Display(Name = "Higher High dot size", Description = "Represents the dot size for Higher Highs.", Order = 12, GroupName = "Dot Visualization")]
        public virtual int HigherHighDotSize
        { get; set; }
        #endregion
        #endregion

        #region Series
        //Public series variables
        // Gives different possibilities for ABC signals, both for the pattern and for the entry
        // 0 = no entry
        // | 1 = ABC bullish pattern
        // | 2 = ABC bullish pattern confirmed
        // | -1 = ABC bearish pattern
        // | -2 = ABC bearish pattern confirmed
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> AbcSignals
        {
            get { return Values[0]; }
        }

        /// <summary>
        /// Gives different possibilities for divergence signals.
        /// 0 = no divergence 
        /// | 1 = possible long regular divergence 
        /// | 2 = possible long hidden divergence 
        /// | 3 = long regular divergence confirmed
        /// | 4 = long hidden divergence confirmed
        /// | -1 = possible short regular divergence 
        /// | -2 = possible short hidden divergence 
        /// | -3 = short regular divergence confirmed
        /// | -4 = short hidden divergence confirmed
        /// /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DivergenceSignals
        {
            get { return Values[1]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> GannSwing
        {
            get { return Values[2]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> SwingSwitchUp
        {
            get { return Values[3]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> SwingSwitchDown
        {
            get { return Values[4]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ABCEntryArrowUp
        {
            get { return Values[5]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ABCEntryArrowDown
        {
            get { return Values[6]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DoubleBottom
        {
            get; protected set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LowerLow
        {
            get; protected set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> HigherLow
        {
            get; protected set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DoubleTop
        {
            get; protected set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LowerHigh
        {
            get; protected set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> HigherHigh
        {
            get; protected set;
        }
        #endregion

        #region ABC visualization
        [Display(Name = "ABC label", Description = "Indicates if A-B-C label should be displayed", Order = 0, GroupName = "ABC Visualization")]
        public bool ShowABCLabel
        { get; set; }

        [Display(Name = "Line style", Description = "Represents the line style for pattern lines.", Order = 1, GroupName = "ABC Visualization")]
        public DashStyleHelper AbcLineStyle
        { get; set; }

        [Display(Name = "Line style ratio", Description = "Represents the line style for pattern ratio lines.", Order = 2, GroupName = "ABC Visualization")]
        public DashStyleHelper AbcLineStyleRatio
        { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Line width", Description = "Represents the line width for pattern lines.", Order = 3, GroupName = "ABC Visualization")]
        public int AbcLineWidth
        { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Line width ratio", Description = "Represents the line width for pattern ratio lines.", Order = 4, GroupName = "ABC Visualization")]
        public int AbcLineWidthRatio
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Text font", Description = "Represents the text font for the displayed swing information.", Order = 5, GroupName = "ABC Visualization")]
        public NinjaTrader.Gui.Tools.SimpleFont AbcTextFont
        { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Text offset label", Description = "Represents the offset value in pixels from within the text box area that display the swing label.", Order = 6, GroupName = "ABC Visualization")]
        public int AbcTextOffsetLabel
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Text color down", Description = "Represents the text color for down patterns.", Order = 7, GroupName = "ABC Visualization")]
        public System.Windows.Media.Brush AbcTextColorDn
        { get; set; }

        [Browsable(false)]
        public string AbcTextColorDnSerialize
        {
            get { return Serialize.BrushToString(AbcTextColorDn); }
            set { AbcTextColorDn = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Text color up", Description = "Represents the text color for up patterns.", Order = 8, GroupName = "ABC Visualization")]
        public System.Windows.Media.Brush AbcTextColorUp
        { get; set; }

        [Browsable(false)]
        public string AbcTextColorUpSerialize
        {
            get { return Serialize.BrushToString(AbcTextColorUp); }
            set { AbcTextColorUp = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Line color down", Description = "Represents the line color for down patterns.", Order = 9, GroupName = "ABC Visualization")]
        public System.Windows.Media.Brush AbcZigZagColorDn
        { get; set; }

        [Browsable(false)]
        public string AbcZigZagColorDnSerialize
        {
            get { return Serialize.BrushToString(AbcZigZagColorDn); }
            set { AbcZigZagColorDn = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Line color up", Description = "Represents the line color for up patterns.", Order = 10, GroupName = "ABC Visualization")]
        public System.Windows.Media.Brush AbcZigZagColorUp
        { get; set; }

        [Browsable(false)]
        public string AbcZigZagColorUpSerialize
        {
            get { return Serialize.BrushToString(AbcZigZagColorUp); }
            set { AbcZigZagColorUp = Serialize.StringToBrush(value); }
        }

        [Range(1, 99)]
        [Display(Name = "Retracement maximum (percent)", Description = "Represents the maximum value in percent for a retracement in relation to the last swing. The price must retrace between this two values, otherwise the pattern is not valid.", Order = 11, GroupName = "ABC Visualization")]
        public double AbcMaxRetracement
        { get; set; }

        [Range(1, 99)]
        [Display(Name = "Retracement minimum (percent)", Description = "Represents the minimum value in percent for a retracement in relation to the last swing. The price must retrace between this two values, otherwise the pattern is not valid.", Order = 12, GroupName = "ABC Visualization")]
        public double AbcMinRetracement
        { get; set; }

        [Display(Name = "Show entry arrows", Description = "Indicates if ABC entry arrows should be displayed", Order = 13, GroupName = "ABC Visualization")]
        public bool ShowEntryArrows
        { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Entry Arrow y-tick offset", Description = "Represents the number of ticks the entry arrow is placed away.", Order = 14, GroupName = "ABC Visualization")]
        public int ABCEntryArrowYTickOffset
        { get; set; }

        [Display(Name = "Show entry line", Description = "Indicates if entry lines are displayed.", Order = 15, GroupName = "ABC Visualization")]
        public bool ShowEntryLine
        { get; set; }

        [Display(Name = "Show historical entry lines?", Description = "Indicates if historical entry lines are displayed.", Order = 16, GroupName = "ABC Visualization")]
        public bool ShowHistoricalEntryLine
        { get; set; }

        [Display(Name = "Entry line style", Description = "Represents the line style for the entry lines.", Order = 17, GroupName = "ABC Visualization")]
        public DashStyleHelper EntryLineStyle
        { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Entry line width", Description = "Represents the line width for pattern lines.", Order = 18, GroupName = "ABC Visualization")]
        public int EntryLineWidth
        { get; set; }

        [Display(Name = "Entry retracement", Description = "If bar close above/below the entry retracement an entry is triggered.", Order = 19, GroupName = "ABC Visualization")]
        public double RetracementEntryValue
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Entry line color down", Description = "Represents the entry line color for down patterns.", Order = 20, GroupName = "ABC Visualization")]
        public Brush EntryLineColorDn
        { get; set; }

        [Browsable(false)]
        public string EntryLineColorDnSerialize
        {
            get { return Serialize.BrushToString(EntryLineColorDn); }
            set { EntryLineColorDn = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Entry line color up", Description = "Represents the entry line color for up patterns.", Order = 21, GroupName = "ABC Visualization")]
        public Brush EntryLineColorUp
        { get; set; }

        [Browsable(false)]
        public string EntryLineColorUpSerialize
        {
            get { return Serialize.BrushToString(EntryLineColorUp); }
            set { EntryLineColorUp = Serialize.StringToBrush(value); }
        }
        #endregion

        #region ABC Alerts
        [Display(Name = "Alert ABC", Order = 01, GroupName = "ABC Alerts")]
        public bool AlertAbc
        { get; set; }

        [Display(Name = "Alert ABC entry", Order = 02, GroupName = "ABC Alerts")]
        public bool AlertAbcEntry
        { get; set; }

        [Display(Name = "Priority", Order = 03, GroupName = "ABC Alerts")]
        public Priority AlertAbcPriority
        { get; set; }

        [Display(Name = "Entry Priority", Order = 4, GroupName = "ABC Alerts")]
        public Priority AlertAbcEntryPriority
        { get; set; }

        [PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "Wav Files (*.wav)|*.wav")]
        [Display(Name = "Long sound file name", Order = 05, GroupName = "ABC Alerts")]
        public string AlertAbcLongSoundFileName
        { get; set; }

        [PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "Wav Files (*.wav)|*.wav")]
        [Display(Name = "Long entry sound file name", Order = 06, GroupName = "ABC Alerts")]
        public string AlertAbcLongEntrySoundFileName
        { get; set; }

        [PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "Wav Files (*.wav)|*.wav")]
        [Display(Name = "Short sound file name", Order = 07, GroupName = "ABC Alerts")]
        public string AlertAbcShortSoundFileName
        { get; set; }

        [PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "Wav Files (*.wav)|*.wav")]
        [Display(Name = "Short entry sound file name", Order = 08, GroupName = "ABC Alerts")]
        public string AlertAbcShortEntrySoundFileName
        { get; set; }
        #endregion

        #region Naked swings visualization
        [Display(Name = "Show historical naked swing lines", Description = "Indicates if historical naked swing lines are shown.", Order = 1, GroupName = "Naked Swings")]
        public bool ShowHistoricalNakedSwings
        { get; set; }

        [Display(Name = "Naked swing line style", Description = "Represents the line style of the naked swing lines.", Order = 2, GroupName = "Naked Swings")]
        public DashStyleHelper NakedSwingDashStyle
        { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Naked swing line width", Description = "Represents the line width of the naked swing lines.", Order = 3, GroupName = "Naked Swings")]
        public int NakedSwingLineWidth
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Naked swing high color", Description = "Represents the color of the naked swing high lines.", Order = 4, GroupName = "Naked Swings")]
        public Brush NakedSwingHighColor
        { get; set; }

        [Browsable(false)]
        public string NakedSwingHighColorSerialize
        {
            get { return Serialize.BrushToString(NakedSwingHighColor); }
            set { NakedSwingHighColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Naked swing low color", Description = "Represents the color of the naked swing low lines.", Order = 5, GroupName = "Naked Swings")]
        public Brush NakedSwingLowColor
        { get; set; }

        [Browsable(false)]
        public string NakedSwingLowColorSerialize
        {
            get { return Serialize.BrushToString(NakedSwingLowColor); }
            set { NakedSwingLowColor = Serialize.StringToBrush(value); }
        }
        #endregion

        #region Divergence Visualization
        [Display(Name = "Divergence long and short", Description = "Represents the direction the divergences are calculated for.", Order = 1, GroupName = "Divergence Visualization")]
        public DivergenceDirection DivergenceDirectionMode
        { get; set; }

        [Display(Name = "Divergence Parameter 1", Description = "Represents the first parameter for the indicator choosen in 'Divergence indicator'.", Order = 2, GroupName = "Divergence Visualization")]
        public int DivParam1
        { get; set; }

        [Display(Name = "Divergence Parameter 2", Description = "Represents the first parameter for the indicator choosen in 'Divergence indicator'.", Order = 3, GroupName = "Divergence Visualization")]
        public int DivParam2
        { get; set; }

        [Display(Name = "Divergence Parameter 3", Description = "Represents the first parameter for the indicator choosen in 'Divergence indicator'.", Order = 4, GroupName = "Divergence Visualization")]
        public int DivParam3
        { get; set; }

        [Display(Name = "Show regular divergence", Description = "Indicates if regalur divergence is shown.", Order = 5, GroupName = "Divergence Visualization")]
        public bool ShowDivergenceRegular
        { get; set; }

        [Display(Name = "Show hidden divergence", Description = "Indicates if hidden divergence is shown.", Order = 6, GroupName = "Divergence Visualization")]
        public bool ShowDivergenceHidden
        { get; set; }

        [Display(Name = "Show text", Description = "True to show divergence text.", Order = 7, GroupName = "Divergence Visualization")]
        public bool ShowDivergenceText
        { get; set; }

        [Display(Name = "Div line style", Description = "Represents the line style for hidden divergence.", Order = 8, GroupName = "Divergence Visualization")]
        public DashStyleHelper DivLineStyle
        { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Div line width", Description = "Represents the line width for hidden divergence.", Order = 9, GroupName = "Divergence Visualization")]
        public int DivLineWidth
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Up color", Description = "Represents the text color for hidden divergence.", Order = 10, GroupName = "Divergence Visualization")]
        public Brush DivUpColor
        { get; set; }

        [Browsable(false)]
        public string DivUpLineColorSerialize
        {
            get { return Serialize.BrushToString(DivUpColor); }
            set { DivUpColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Down color", Description = "Represents the text color for regular divergence.", Order = 11, GroupName = "Divergence Visualization")]
        public Brush DivDnColor
        { get; set; }

        [Browsable(false)]
        public string DivDnLineColorSerialize
        {
            get { return Serialize.BrushToString(DivDnColor); }
            set { DivDnColor = Serialize.StringToBrush(value); }
        }

        #endregion

        #region Statistic
        [Display(Name = "Statistic number of swings", Description = "Indicates the number of swings which are used for the current swing statistic.", Order = 1, GroupName = "Statistic")]
        public int StatisticLength
        { get; set; }
        [Display(Name = "Clear output window", Description = "Indicates if the output window should be cleared everytime.", Order = 2, GroupName = "Statistic")]
        public bool ClearNinjaScriptOutputWindow
        { get; set; }
        #endregion
        #endregion
    }

    #region Show/hide properties
    // This custom TypeConverter is applied ot the entire indicator object and handles our use cases
    // IMPORTANT: Inherit from IndicatorBaseConverter so we get default NinjaTrader property handling logic
    // IMPORTANT: Not doing this will completely break the property grids!
    // If targeting a "Strategy", use the "StrategyBaseConverter" base type instead
    public class PriceActionSwingProConverter : IndicatorBaseConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
        {
            // we need the indicator instance which actually exists on the grid
            PriceActionSwingPro indicator = component as PriceActionSwingPro;

            // base.GetProperties ensures we have all the properties (and associated property grid editors)
            // NinjaTrader internal logic determines for a given indicator
            PropertyDescriptorCollection propertyDescriptorCollection = base.GetPropertiesSupported(context)
                                                                        ? base.GetProperties(context, component, attrs)
                                                                        : TypeDescriptor.GetProperties(component, attrs);

            if (indicator == null || propertyDescriptorCollection == null)
                return propertyDescriptorCollection;

            #region Gann
            // These values will be shown/hidden (toggled)
            PropertyDescriptor IgnoreInsideBarsPropertyDescriptor = propertyDescriptorCollection["IgnoreInsideBars"];
            PropertyDescriptor UseBreakoutsPropertyDescriptor = propertyDescriptorCollection["UseBreakouts"];
            // This removes the following properties from the grid to start off with
            // Parameters
            propertyDescriptorCollection.Remove(IgnoreInsideBarsPropertyDescriptor);
            propertyDescriptorCollection.Remove(UseBreakoutsPropertyDescriptor);

            // Now that We've removed the default property descriptors, we can decide if they need to be re-added
            // If "xxx" is set to true, re-add these values to the property collection
            if (indicator.SwingStyleType == SwingStyle.Gann)
            {
                propertyDescriptorCollection.Add(IgnoreInsideBarsPropertyDescriptor);
                propertyDescriptorCollection.Add(UseBreakoutsPropertyDescriptor);
            }
            #endregion

            #region ABC
            PropertyDescriptor ShowABCLabelPropertyDescriptor = propertyDescriptorCollection["ShowABCLabel"];
            PropertyDescriptor AbcLineStylePropertyDescriptor = propertyDescriptorCollection["AbcLineStyle"];
            PropertyDescriptor AbcLineStyleRatioPropertyDescriptor = propertyDescriptorCollection["AbcLineStyleRatio"];
            PropertyDescriptor AbcLineWidthPropertyDescriptor = propertyDescriptorCollection["AbcLineWidth"];
            PropertyDescriptor AbcLineWidthRatioPropertyDescriptor = propertyDescriptorCollection["AbcLineWidthRatio"];
            PropertyDescriptor AbcTextFontPropertyDescriptor = propertyDescriptorCollection["AbcTextFont"];
            PropertyDescriptor AbcTextOffsetLabelPropertyDescriptor = propertyDescriptorCollection["AbcTextOffsetLabel"];
            PropertyDescriptor AbcTextColorDnPropertyDescriptor = propertyDescriptorCollection["AbcTextColorDn"];
            PropertyDescriptor AbcTextColorUpPropertyDescriptor = propertyDescriptorCollection["AbcTextColorUp"];
            PropertyDescriptor AbcZigZagColorDnPropertyDescriptor = propertyDescriptorCollection["AbcZigZagColorDn"];
            PropertyDescriptor AbcZigZagColorUpPropertyDescriptor = propertyDescriptorCollection["AbcZigZagColorUp"];
            PropertyDescriptor AbcMaxRetracementPropertyDescriptor = propertyDescriptorCollection["AbcMaxRetracement"];
            PropertyDescriptor AbcMinRetracementPropertyDescriptor = propertyDescriptorCollection["AbcMinRetracement"];
            PropertyDescriptor ShowEntryArrowsPropertyDescriptor = propertyDescriptorCollection["ShowEntryArrows"];
            PropertyDescriptor ABCEntryArrowYTickOffsetPropertyDescriptor = propertyDescriptorCollection["ABCEntryArrowYTickOffset"];
            PropertyDescriptor EntryLineColorDnPropertyDescriptor = propertyDescriptorCollection["EntryLineColorDn"];
            PropertyDescriptor EntryLineColorUpPropertyDescriptor = propertyDescriptorCollection["EntryLineColorUp"];
            PropertyDescriptor EntryLineStylePropertyDescriptor = propertyDescriptorCollection["EntryLineStyle"];
            PropertyDescriptor EntryLineWidthPropertyDescriptor = propertyDescriptorCollection["EntryLineWidth"];
            PropertyDescriptor RetracementEntryValuePropertyDescriptor = propertyDescriptorCollection["RetracementEntryValue"];
            PropertyDescriptor ShowEntryLinePropertyDescriptor = propertyDescriptorCollection["ShowEntryLine"];
            PropertyDescriptor ShowHistoricalEntryLinePropertyDescriptor = propertyDescriptorCollection["ShowHistoricalEntryLine"];

            PropertyDescriptor AlertAbcPropertyDescriptor = propertyDescriptorCollection["AlertAbc"];
            PropertyDescriptor AlertAbcEntryPropertyDescriptor = propertyDescriptorCollection["AlertAbcEntry"];
            PropertyDescriptor AlertAbcPriorityPropertyDescriptor = propertyDescriptorCollection["AlertAbcPriority"];
            PropertyDescriptor AlertAbcEntryPriorityPropertyDescriptor = propertyDescriptorCollection["AlertAbcEntryPriority"];
            PropertyDescriptor AlertAbcLongSoundFileNamePropertyDescriptor = propertyDescriptorCollection["AlertAbcLongSoundFileName"];
            PropertyDescriptor AlertAbcLongEntrySoundFileNamePropertyDescriptor = propertyDescriptorCollection["AlertAbcLongEntrySoundFileName"];
            PropertyDescriptor AlertAbcShortSoundFileNamePropertyDescriptor = propertyDescriptorCollection["AlertAbcShortSoundFileName"];
            PropertyDescriptor AlertAbcShortEntrySoundFileNamePropertyDescriptor = propertyDescriptorCollection["AlertAbcShortEntrySoundFileName"];

            propertyDescriptorCollection.Remove(ShowABCLabelPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcLineStylePropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcLineStyleRatioPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcLineWidthPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcLineWidthRatioPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcTextFontPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcTextOffsetLabelPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcTextColorDnPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcTextColorUpPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcZigZagColorDnPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcZigZagColorUpPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcMaxRetracementPropertyDescriptor);
            propertyDescriptorCollection.Remove(AbcMinRetracementPropertyDescriptor);
            propertyDescriptorCollection.Remove(ShowEntryArrowsPropertyDescriptor);
            propertyDescriptorCollection.Remove(ABCEntryArrowYTickOffsetPropertyDescriptor);
            propertyDescriptorCollection.Remove(EntryLineColorDnPropertyDescriptor);
            propertyDescriptorCollection.Remove(EntryLineColorUpPropertyDescriptor);
            propertyDescriptorCollection.Remove(EntryLineStylePropertyDescriptor);
            propertyDescriptorCollection.Remove(EntryLineWidthPropertyDescriptor);
            propertyDescriptorCollection.Remove(RetracementEntryValuePropertyDescriptor);
            propertyDescriptorCollection.Remove(ShowEntryLinePropertyDescriptor);
            propertyDescriptorCollection.Remove(ShowHistoricalEntryLinePropertyDescriptor);

            propertyDescriptorCollection.Remove(AlertAbcPropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcEntryPropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcPriorityPropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcEntryPriorityPropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcLongSoundFileNamePropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcLongEntrySoundFileNamePropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcShortSoundFileNamePropertyDescriptor);
            propertyDescriptorCollection.Remove(AlertAbcShortEntrySoundFileNamePropertyDescriptor);

            if (indicator.AbcPattern != AbcPatternMode.Off)
            {
                propertyDescriptorCollection.Add(ShowABCLabelPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcLineStylePropertyDescriptor);
                propertyDescriptorCollection.Add(AbcLineStyleRatioPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcLineWidthPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcLineWidthRatioPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcTextFontPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcTextOffsetLabelPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcTextColorDnPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcTextColorUpPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcZigZagColorDnPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcZigZagColorUpPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcMaxRetracementPropertyDescriptor);
                propertyDescriptorCollection.Add(AbcMinRetracementPropertyDescriptor);
                propertyDescriptorCollection.Add(ShowEntryArrowsPropertyDescriptor);
                propertyDescriptorCollection.Add(ABCEntryArrowYTickOffsetPropertyDescriptor);
                propertyDescriptorCollection.Add(EntryLineColorDnPropertyDescriptor);
                propertyDescriptorCollection.Add(EntryLineColorUpPropertyDescriptor);
                propertyDescriptorCollection.Add(EntryLineStylePropertyDescriptor);
                propertyDescriptorCollection.Add(EntryLineWidthPropertyDescriptor);
                propertyDescriptorCollection.Add(RetracementEntryValuePropertyDescriptor);
                propertyDescriptorCollection.Add(ShowEntryLinePropertyDescriptor);
                propertyDescriptorCollection.Add(ShowHistoricalEntryLinePropertyDescriptor);

                propertyDescriptorCollection.Add(AlertAbcPropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcEntryPropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcPriorityPropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcEntryPriorityPropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcLongSoundFileNamePropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcLongEntrySoundFileNamePropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcShortSoundFileNamePropertyDescriptor);
                propertyDescriptorCollection.Add(AlertAbcShortEntrySoundFileNamePropertyDescriptor);
            }
            #endregion

            #region Dot Visualization
            PropertyDescriptor DoubleBottomDotColorPropertyDescriptor = propertyDescriptorCollection["DoubleBottomDotColor"];
            PropertyDescriptor DoubleBottomDotSizePropertyDescriptor = propertyDescriptorCollection["DoubleBottomDotSize"];
            PropertyDescriptor LowerLowDotColorPropertyDescriptor = propertyDescriptorCollection["LowerLowDotColor"];
            PropertyDescriptor LowerLowDotSizePropertyDescriptor = propertyDescriptorCollection["LowerLowDotSize"];
            PropertyDescriptor HigherLowDotColorPropertyDescriptor = propertyDescriptorCollection["HigherLowDotColor"];
            PropertyDescriptor HigherLowDotSizePropertyDescriptor = propertyDescriptorCollection["HigherLowDotSize"];
            PropertyDescriptor DoubleTopDotColorPropertyDescriptor = propertyDescriptorCollection["DoubleTopDotColor"];
            PropertyDescriptor DoubleTopDotSizePropertyDescriptor = propertyDescriptorCollection["DoubleTopDotSize"];
            PropertyDescriptor LowerHighDotColorPropertyDescriptor = propertyDescriptorCollection["LowerHighDotColor"];
            PropertyDescriptor LowerHighDotSizePropertyDescriptor = propertyDescriptorCollection["LowerHighDotSize"];
            PropertyDescriptor HigherHighDotColorPropertyDescriptor = propertyDescriptorCollection["HigherHighDotColor"];
            PropertyDescriptor HigherHighDotSizePropertyDescriptor = propertyDescriptorCollection["HigherHighDotSize"];

            propertyDescriptorCollection.Remove(DoubleBottomDotColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DoubleBottomDotSizePropertyDescriptor);
            propertyDescriptorCollection.Remove(LowerLowDotColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(LowerLowDotSizePropertyDescriptor);
            propertyDescriptorCollection.Remove(HigherLowDotColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(HigherLowDotSizePropertyDescriptor);
            propertyDescriptorCollection.Remove(DoubleTopDotColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DoubleTopDotSizePropertyDescriptor);
            propertyDescriptorCollection.Remove(LowerHighDotColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(LowerHighDotSizePropertyDescriptor);
            propertyDescriptorCollection.Remove(HigherHighDotColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(HigherHighDotSizePropertyDescriptor);

            if (indicator.VisualizationType == VisualizationStyle.Dots
                || indicator.VisualizationType == VisualizationStyle.Dots_ZigZag)
            {
                // Dots
                propertyDescriptorCollection.Add(DoubleBottomDotColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DoubleBottomDotSizePropertyDescriptor);
                propertyDescriptorCollection.Add(LowerLowDotColorPropertyDescriptor);
                propertyDescriptorCollection.Add(LowerLowDotSizePropertyDescriptor);
                propertyDescriptorCollection.Add(HigherLowDotColorPropertyDescriptor);
                propertyDescriptorCollection.Add(HigherLowDotSizePropertyDescriptor);
                propertyDescriptorCollection.Add(DoubleTopDotColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DoubleTopDotSizePropertyDescriptor);
                propertyDescriptorCollection.Add(LowerHighDotColorPropertyDescriptor);
                propertyDescriptorCollection.Add(LowerHighDotSizePropertyDescriptor);
                propertyDescriptorCollection.Add(HigherHighDotColorPropertyDescriptor);
                propertyDescriptorCollection.Add(HigherHighDotSizePropertyDescriptor);
            }
            #endregion

            #region Naked swings
            // These values will be shown/hidden (toggled)
            PropertyDescriptor ShowHistoricalNakedSwingsPropertyDescriptor = propertyDescriptorCollection["ShowHistoricalNakedSwings"];
            PropertyDescriptor NakedSwingHighColorPropertyDescriptor = propertyDescriptorCollection["NakedSwingHighColor"];
            PropertyDescriptor NakedSwingLowColorPropertyDescriptor = propertyDescriptorCollection["NakedSwingLowColor"];
            PropertyDescriptor NakedSwingDashStylePropertyDescriptor = propertyDescriptorCollection["NakedSwingDashStyle"];
            PropertyDescriptor NakedSwingLineWidthPropertyDescriptor = propertyDescriptorCollection["NakedSwingLineWidth"];
            // This removes the following properties from the grid to start off with
            // Parameters
            propertyDescriptorCollection.Remove(ShowHistoricalNakedSwingsPropertyDescriptor);
            propertyDescriptorCollection.Remove(NakedSwingHighColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(NakedSwingLowColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(NakedSwingDashStylePropertyDescriptor);
            propertyDescriptorCollection.Remove(NakedSwingLineWidthPropertyDescriptor);

            // Now that We've removed the default property descriptors, we can decide if they need to be re-added
            // If "xxx" is set to true, re-add these values to the property collection
            if (indicator.ShowNakedSwings == true)
            {
                propertyDescriptorCollection.Add(ShowHistoricalNakedSwingsPropertyDescriptor);
                propertyDescriptorCollection.Add(NakedSwingHighColorPropertyDescriptor);
                propertyDescriptorCollection.Add(NakedSwingLowColorPropertyDescriptor);
                propertyDescriptorCollection.Add(NakedSwingDashStylePropertyDescriptor);
                propertyDescriptorCollection.Add(NakedSwingLineWidthPropertyDescriptor);
            }
            #endregion

            #region Divergence
            // These values will be shown/hidden (toggled)
            PropertyDescriptor DivergenceDirectionModePropertyDescriptor = propertyDescriptorCollection["DivergenceDirectionMode"];
            PropertyDescriptor DivParam1PropertyDescriptor = propertyDescriptorCollection["DivParam1"];
            PropertyDescriptor DivParam2PropertyDescriptor = propertyDescriptorCollection["DivParam2"];
            PropertyDescriptor DivParam3PropertyDescriptor = propertyDescriptorCollection["DivParam3"];
            PropertyDescriptor ShowDivergenceRegularPropertyDescriptor = propertyDescriptorCollection["ShowDivergenceRegular"];
            PropertyDescriptor ShowDivergenceHiddenPropertyDescriptor = propertyDescriptorCollection["ShowDivergenceHidden"];
            PropertyDescriptor ShowDivergenceTextPropertyDescriptor = propertyDescriptorCollection["ShowDivergenceText"];
            PropertyDescriptor DivUpColorPropertyDescriptor = propertyDescriptorCollection["DivUpColor"];
            PropertyDescriptor DivDnColorPropertyDescriptor = propertyDescriptorCollection["DivDnColor"];
            PropertyDescriptor DivLineStylePropertyDescriptor = propertyDescriptorCollection["DivLineStyle"];
            PropertyDescriptor DivLineWidthPropertyDescriptor = propertyDescriptorCollection["DivLineWidth"];
            // This removes the following properties from the grid to start off with
            // Parameters
            propertyDescriptorCollection.Remove(DivergenceDirectionModePropertyDescriptor);
            propertyDescriptorCollection.Remove(DivParam1PropertyDescriptor);
            propertyDescriptorCollection.Remove(DivParam2PropertyDescriptor);
            propertyDescriptorCollection.Remove(DivParam3PropertyDescriptor);
            propertyDescriptorCollection.Remove(ShowDivergenceRegularPropertyDescriptor);
            propertyDescriptorCollection.Remove(ShowDivergenceHiddenPropertyDescriptor);
            propertyDescriptorCollection.Remove(ShowDivergenceTextPropertyDescriptor);
            propertyDescriptorCollection.Remove(DivUpColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DivDnColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DivLineStylePropertyDescriptor);
            propertyDescriptorCollection.Remove(DivLineWidthPropertyDescriptor);

            // Now that We've removed the default property descriptors, we can decide if they need to be re-added
            // If "xxx" is set to true, re-add these values to the property collection
            if (indicator.DivergenceIndicatorMode != DivergenceMode.Off)
            {
                propertyDescriptorCollection.Add(DivergenceDirectionModePropertyDescriptor);
                propertyDescriptorCollection.Add(DivParam1PropertyDescriptor);
                propertyDescriptorCollection.Add(DivParam2PropertyDescriptor);
                propertyDescriptorCollection.Add(DivParam3PropertyDescriptor);
                propertyDescriptorCollection.Add(ShowDivergenceRegularPropertyDescriptor);
                propertyDescriptorCollection.Add(ShowDivergenceHiddenPropertyDescriptor);
                propertyDescriptorCollection.Add(ShowDivergenceTextPropertyDescriptor);
                propertyDescriptorCollection.Add(DivUpColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DivDnColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DivLineStylePropertyDescriptor);
                propertyDescriptorCollection.Add(DivLineWidthPropertyDescriptor);
            }
            #endregion

            // otherwise, nothing else to do since they were already removed
            return propertyDescriptorCollection;
        }

        // Important: This must return true otherwise the type convetor will not be called
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        { return true; }
    }
    #endregion
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceActionSwing.PriceActionSwingPro[] cachePriceActionSwingPro;
		public PriceActionSwing.PriceActionSwingPro PriceActionSwingPro(SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts)
		{
			return PriceActionSwingPro(Input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts);
		}

		public PriceActionSwing.PriceActionSwingPro PriceActionSwingPro(ISeries<double> input, SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts)
		{
			if (cachePriceActionSwingPro != null)
				for (int idx = 0; idx < cachePriceActionSwingPro.Length; idx++)
					if (cachePriceActionSwingPro[idx] != null && cachePriceActionSwingPro[idx].SwingStyleType == swingStyleType && cachePriceActionSwingPro[idx].SwingSize == swingSize && cachePriceActionSwingPro[idx].DtbStrength == dtbStrength && cachePriceActionSwingPro[idx].UseCloseValues == useCloseValues && cachePriceActionSwingPro[idx].IgnoreInsideBars == ignoreInsideBars && cachePriceActionSwingPro[idx].UseBreakouts == useBreakouts && cachePriceActionSwingPro[idx].EqualsInput(input))
						return cachePriceActionSwingPro[idx];
			return CacheIndicator<PriceActionSwing.PriceActionSwingPro>(new PriceActionSwing.PriceActionSwingPro(){ SwingStyleType = swingStyleType, SwingSize = swingSize, DtbStrength = dtbStrength, UseCloseValues = useCloseValues, IgnoreInsideBars = ignoreInsideBars, UseBreakouts = useBreakouts }, input, ref cachePriceActionSwingPro);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceActionSwing.PriceActionSwingPro PriceActionSwingPro(SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts)
		{
			return indicator.PriceActionSwingPro(Input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts);
		}

		public Indicators.PriceActionSwing.PriceActionSwingPro PriceActionSwingPro(ISeries<double> input , SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts)
		{
			return indicator.PriceActionSwingPro(input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceActionSwing.PriceActionSwingPro PriceActionSwingPro(SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts)
		{
			return indicator.PriceActionSwingPro(Input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts);
		}

		public Indicators.PriceActionSwing.PriceActionSwingPro PriceActionSwingPro(ISeries<double> input , SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts)
		{
			return indicator.PriceActionSwingPro(input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts);
		}
	}
}

#endregion
