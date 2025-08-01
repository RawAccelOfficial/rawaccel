using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace grapher.Models.Options.LUT
{
    public class LUTPanelOptions : OptionBase
    {
        public const int PanelPadding = 5;
        public const int PanelHeight = 100;

        public LUTPanelOptions(RichTextBox pointsTextBox, RichTextBox activeValuesTextBox)
        {
            PointsTextBox = pointsTextBox;
            PointsTextBox.Height = PanelHeight;

            ActiveValuesTextBox = activeValuesTextBox;
            ActiveValuesTextBox.Height = PanelHeight;
            ActiveValuesTextBox.ReadOnly = true;
        }

        public RichTextBox PointsTextBox
        {
            get;
        }

        public RichTextBox ActiveValuesTextBox
        {
            get;
        }

        public override bool Visible
        {
            get
            {
                return PointsTextBox.Visible || ShouldShow;
            }
        }

        public override int Top
        {
            get
            {
                return PointsTextBox.Top;
            }
            set
            {
                PointsTextBox.Top = value;
                ActiveValuesTextBox.Top = value;
            }
        }

        public override int Height
        {
            get
            {
                return PointsTextBox.Height;
            }
        }

        public override int Left
        {
            get
            {
                return PointsTextBox.Left;
            }
            set
            {
                PointsTextBox.Left = value;
                AlignActivePanelToPointsTextBox();
            }
        }

        public override int Width
        {
            get
            {
                return PointsTextBox.Width;
            }
            set
            {
                var panelWidth = value / 2 - PanelPadding;
                PointsTextBox.Width = panelWidth;
                ActiveValuesTextBox.Width = panelWidth;
                AlignActivePanelToPointsTextBox();
            }
        }

        private bool ShouldShow { get; set; }

        public override void Hide()
        {
            PointsTextBox.Hide();
            ActiveValuesTextBox.Hide();
            ShouldShow = false;
        }

        public override void Show(string name)
        {
            PointsTextBox.Show();
            ActiveValuesTextBox.Show();
            ShouldShow = true;
        }

        public override void AlignActiveValues()
        {
            // Nothing to do here.
        }

        public void SetActiveValues(IEnumerable<float> rawData, int length, AccelMode mode)
        {
            if (mode == AccelMode.lut && length > 1 && rawData.First() != 0)
            {
                var pointsLen = length / 2;
                var points = new Vec2<float>[pointsLen];
                for (int i = 0; i < pointsLen; i++)
                {
                    var data_idx = i * 2;
                    points[i] = new Vec2<float>
                    {
                        x = rawData.ElementAt(data_idx),
                        y = rawData.ElementAt(data_idx + 1)
                    };
                }
                ActiveValuesTextBox.Text = PointsToActiveValuesText(points, pointsLen);

                if (string.IsNullOrWhiteSpace(PointsTextBox.Text))
                {
                    PointsTextBox.Text = PointsToEntryTextBoxText(points, pointsLen);
                }
            }
            else
            {
                ActiveValuesTextBox.Text = string.Empty;
            }
        }

        public (Vec2<float>[], int length) GetPoints()
        {
            return UserTextToPoints(PointsTextBox.Text);
        }

        private static (Vec2<float>[], int length) UserTextToPoints(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                throw new ApplicationException("Text must be entered in text box to fill Look Up Table.");
            }

            Vec2<float>[] points = new Vec2<float>[AccelArgs.MaxLutPoints];

            var userTextSplit = userText.Trim().Trim(';').Split(';');
            int index = 0;
            float lastX = 0;

            int pointsCount = userTextSplit.Count();

            if (pointsCount < 2)
            {
                throw new ApplicationException("At least 2 points required");
            }

            if (pointsCount > AccelArgs.MaxLutPoints)
            {
                throw new ApplicationException($"Number of points exceeds max ({AccelArgs.MaxLutPoints})");
            }

            foreach(var pointEntry in userTextSplit)
            {
                var pointSplit = pointEntry.Trim().Split(',');

                if (pointSplit.Length != 2)
                {
                    throw new ApplicationException($"Point at index {index} is malformed. Expected format: x,y; Given: {pointEntry.Trim()}");
                }

                float x;
                float y;

                try
                {
                    x = float.Parse(pointSplit[0]);
                }
                catch (Exception)
                {
                    throw new ApplicationException($"X-value for point at index {index} is malformed. Expected: float. Given: {pointSplit[0]}", ex);
                }

                if (x <= 0)
                {
                    throw new ApplicationException($"X-value for point at index {index} is less than or equal to 0. Point (0,0) is implied and should not be specified in points text.");
                }

                if (x <= lastX)
                {
                    throw new ApplicationException($"X-value for point at index {index} is less than or equal to previous x-value. Value: {x} Previous: {lastX}");
                }

                lastX = x;

                try
                {
                    y = float.Parse(pointSplit[1]);
                }
                catch (Exception)
                {
                    throw new ApplicationException($"Y-value for point at index {index} is malformed. Expected: float. Given: {pointSplit[1]}", ex);
                }

                if (y <= 0)
                {
                    throw new ApplicationException($"Y-value for point at index {index} is less than or equal to 0. Value: {y}");
                }

                points[index] = new Vec2<float> { x = x, y = y };

                index++;
            }

            return (points, userTextSplit.Length);
        }

        private void AlignActivePanelToPointsTextBox()
        {
            ActiveValuesTextBox.Left = PointsTextBox.Right + PanelPadding;
        }

        private string PointsToActiveValuesText(IEnumerable<Vec2<float>> points, int length)
        {
            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < length; i++)
            {
                var point = points.ElementAt(i);
                builder.AppendLine($"{point.x},{point.y};");
            }

            return builder.ToString();
        }

        private string PointsToEntryTextBoxText(IEnumerable<Vec2<float>> points, int length)
        {
            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < length; i++)
            {
                var point = points.ElementAt(i);
                builder.Append($"{point.x},{point.y};");
            }

            return builder.ToString();
        }
    }
}
