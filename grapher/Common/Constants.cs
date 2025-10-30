using System.Drawing;
using System.Globalization;

namespace grapher.Common
{
    public static class Constants
    {
        #region Constants

        /// <summary> DPI by which charts are scaled if none is set by user. </summary>
        public const int DefaultChartsScalingDPI = 1200;

        /// <summary> Poll rate by which charts are scaled if none is set by user. </summary>
        public const int DefaultChartsScalingPollRate = 1000;

        /// <summary> Resolution of chart calulation. </summary>
        public const int Resolution = 500;

        /// <summary> Multiplied by DPI over poll rate to find rough max expected velocity. </summary>
        public const double MaxMultiplier = .05;

        /// <summary> Separation between X and Y active value labels, in pixels. </summary>
        public const int ActiveLabelXYSeparation = 2;

        /// <summary> Vertical separation between charts, in pixels. </summary>
        public const int ChartSeparationVertical = 10;

        /// <summary> Needed to show full contents in form. Unsure why. </summary>
        public const int FormHeightPadding = 35;

        /// <summary> Horizontal separation between charts, in pixels. </summary>
        public const int ChartSeparationHorizontal = 10;

        /// <summary> Default horizontal separation between x and y fields, in pixels. </summary>
        public const int DefaultFieldSeparation = 4;

        /// <summary> Default horizontal separation between an option's label and box, in pixels. </summary>
        public const int OptionLabelBoxSeperation = 10;

        /// <summary> Default horizontal separation between an option's label and box, in pixels. </summary>
        public const int OptionVerticalSeperation = 4;

        /// <summary> Horizontal separation between left side of single dropdown and left side of labels beneath dropdown </summary>
        public const int DropDownLeftSeparation = 10;

        /// <summary> Height of sensitivity chart when displayed alone. </summary>
        public const int SensitivityChartAloneHeight = 510;

        /// <summary> Height of sensitivity chart when displayed alongside Velocity and Gain charts. </summary>
        public const int SensitivityChartTogetherHeight = 328;

        /// <summary> Width of charts when widened </summary>
        public const int WideChartWidth = 723;

        /// <summary> Left placement of charts when widened </summary>
        public const int WideChartLeft = 333;

        /// <summary> Width of charts when narrowed </summary>
        public const int NarrowChartWidth = 698;

        /// <summary> Left placement of charts when narrowed </summary>
        public const int NarrowChartLeft = 482;

        /// <summary> Vertical placement of write button above bottom of sensitivity graph </summary>
        public const int ButtonVerticalOffset = 60;

        /// <summary> Vertical placement of directionality panel below top of containing form </summary>
        public const int DirectionalityVerticalOffset = 315;

        /// <summary> Padding between directionality title and containing panel </summary>
        public const int DirectionalityTitlePad = 4;

        public const float SmallButtonSizeFactor = 0.666f;

        /// <summary> Number of divisions between 0 and 90 degrees for directional lookup. For 19: 0, 5, 10... 85, 90.</summary>
        public const int AngleDivisions = 19;

        /// <summary> Format string for shortened x and y textboxes. </summary>
        public const string ShortenedFormatString = "0.###";

        /// <summary> Format string for default active value labels. </summary>
        public const string DefaultActiveValueFormatString = "0.######";

        /// <summary> Format string for default textboxes. </summary>
        public const string DefaultFieldFormatString = "0.#########";

        /// <summary> Format string for shortened x and y fields. </summary>
        public const string ShortenedFieldFormatString = "0.###";

        /// <summary> Format string for gain cap active value label. </summary>
        public const string GainCapFormatString = "0.##";

        /// <summary> Format string for shortened x and y dropdowns. </summary>
        public const string AccelDropDownDefaultFullText = "Acceleration Type";

        /// <summary> Format string for default dropdowns. </summary>
        public const string AccelDropDownDefaultShortText = "Accel Type";

        /// <summary> Default text to be displayed on write button. </summary>
        public const string WriteButtonDefaultText = "Apply";

        /// <summary> Default text to be displayed on toggle button. </summary>
        public const string ToggleButtonDefaultText = "Toggle";

        /// <summary> Default text to be displayed on button delay. </summary>
        public const string ButtonDelayText = "Delay";
        
        /// <summary> Default text to be displayed on button delay. </summary>
        public const string ResetButtonText = "Reset";
        
        /// <summary> Title of sensitivity chart. </summary>
        public const string SensitivityChartTitle = "Sensitivity";

        /// <summary> Title of velocity chart. </summary>
        public const string VelocityChartTitle = "Velocity";

        /// <summary> Title of gain chart. </summary>
        public const string GainChartTitle = "Gain";

        /// <summary> Text for x component. </summary>
        public const string XComponent = "X";
        
        /// <summary> Text for y component. </summary>
        public const string YComponent = "Y";

        /// <summary> Text to directionality panel title when panel is closed. </summary>
        public const string DirectionalityTitleClosed = "Anisotropy \u25BC";

        /// <summary> Text to directionality panel title when panel is open. </summary>
        public const string DirectionalityTitleOpen = "Anisotropy \u25B2";

        /// <summary> Style used by System.Double.Parse </summary>
        public const NumberStyles FloatStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        /// <summary> Font Size for Chart Titles </summary>
        public const float ChartTitleFontSize = 15;

        /// <summary> Font Size for Chart Axis Titles </summary>
        public const float ChartAxisFontSize = 12;

        /// <summary> Line Width For Series data on chart </summary>
        public const int ChartSeriesLineWidth = 3;

        /// <summary> DPI to which driver is normalizing inputs </summary>
        public const double DriverNormalizedDPI = 1000.0;
        #endregion Constants

        #region ReadOnly

        /// <summary> Default last mouse move label text format. </summary>
        public static readonly string MouseMoveDefaultFormat = "Last (x, y): ({0}, {1})";

        /// <summary> Last mouse move label text format when last input was from a dpi normalized device. </summary>
        public static readonly string MouseMoveNormalizedFormat = $"{MouseMoveDefaultFormat} (n)";

        /// <summary> Marker size for last-mouse-move chart series. </summary>
        public const int DotMarkerSize = 7;

        public static readonly Point Origin = new Point(0);
        public static readonly Size MaxSize = new Size(9999, 9999);

        /// <summary> Amount of rows when only the sensitivity chart is shown. </summary>
        public static readonly int RegularRowCount = 1;
        /// <summary> Height of each row when only the sensitivity chart is shown. </summary>
        public static readonly float RegularRowHeight = 100f;

        /// <summary> Amount of rows when the sensitivity, velocity and gain charts are shown. </summary>
        public static readonly int VelocityAndGainRowCount = 3;
        /// <summary> Height of each row when the sensitivity, velocity and gain charts are shown. </summary>
        public static readonly float VelocityAndGainRowHeight = 33.3f;

        /// <summary> Amount of columns when the charts are combined. </summary>
        public static readonly int CombinedChartColumnCount = 1;
        /// <summary> Width of each column when the charts are combined. </summary>
        public static readonly float CombinedChartColumnWidth = 100f;

        /// <summary> Amount of columns when both the X and Y chart are shown. </summary>
        public static readonly int SeparateChartColumnCount = 2;
        /// <summary> Width of each column when both the X and Y chart are shown. </summary>
        public static readonly float SeparateChartColumnWidth = 50f;

        #endregion ReadOnly
    }
}
