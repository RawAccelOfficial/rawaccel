﻿using grapher.Layouts;
using grapher.Models.Options;
using grapher.Models.Options.LUT;
using grapher.Models.Serialized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace grapher
{
    public class AccelTypeOptions : OptionBase
    {
        #region Fields

        public static readonly LayoutBase Linear = new LinearLayout();
        public static readonly LayoutBase Classic = new ClassicLayout();
        public static readonly LayoutBase Jump = new JumpLayout();
        public static readonly LayoutBase Natural = new NaturalLayout();
        public static readonly LayoutBase Motivity = new MotivityLayout();
        public static readonly LayoutBase Power = new PowerLayout();
        public static readonly LayoutBase LUT = new LUTLayout();
        public static readonly LayoutBase Off = new OffLayout();

        #endregion Fields

        #region Constructors

        public AccelTypeOptions(
            ComboBox accelDropdown,
            CheckBoxOption gainSwitch,
            Option acceleration,
            Option decayRate,
            Option growthRate,
            Option smooth,
            Option scale,
            Option cap,
            Option weight,
            Option offset,
            Option limit,
            Option powerClassic,
            Option exponent,
            Option midpoint,
            TextOption lutText,
            LUTPanelOptions lutPanelOptions,
            LutApplyOptions lutApplyOptions,
            Button writeButton,
            ActiveValueLabel accelTypeActiveValue)
        {
            AccelDropdown = accelDropdown;
            AccelDropdown.Items.Clear();
            AccelDropdown.Items.AddRange(
                new LayoutBase[]
                {
                    Linear,
                    Classic,
                    Jump,
                    Natural,
                    Motivity,
                    Power,
                    LUT,
                    Off
                });

            AccelDropdown.SelectedIndexChanged += new System.EventHandler(OnIndexChanged);

            GainSwitch = gainSwitch;
            Acceleration = acceleration;
            DecayRate = decayRate;
            GrowthRate = growthRate;
            Smooth = smooth;
            Scale = scale;
            Cap = cap;
            Weight = weight;
            Offset = offset;
            Limit = limit;
            PowerClassic = powerClassic;
            Exponent = exponent;
            Midpoint = midpoint;
            WriteButton = writeButton;
            AccelTypeActiveValue = accelTypeActiveValue;
            LutText = lutText;
            LutPanel = lutPanelOptions;
            LutApply = lutApplyOptions;

            AccelTypeActiveValue.Left = AccelDropdown.Left + AccelDropdown.Width;
            AccelTypeActiveValue.Height = AccelDropdown.Height;
            GainSwitch.Left = Acceleration.Field.Left;

            LutPanel.Left = AccelDropdown.Left;
            LutPanel.Width = AccelDropdown.Width + AccelTypeActiveValue.Width;

            LutText.SetText(TextOption.LUTLayoutExpandedText, TextOption.LUTLayoutShortenedText);

            AccelerationType = Off;
            Layout();
            ShowingDefault = true;
        }

        #endregion Constructors

        #region Properties
        public AccelCharts AccelCharts { get; }

        public Button WriteButton { get; }

        public ComboBox AccelDropdown { get; }

        public ActiveValueLabel AccelTypeActiveValue { get; }

        public Option Acceleration { get; }

        public Option DecayRate { get; }

        public Option GrowthRate { get; }

        public Option Smooth { get; }

        public Option Scale { get; }

        public Option Cap { get; }

        public Option Weight { get; }

        public Option Offset { get; }

        public Option Limit { get; }

        /// <summary>
        /// This is the option for the power parameter for Classic style,
        /// and has nothing to do with the Power style.
        /// </summary>
        public Option PowerClassic { get; }

        public Option Exponent { get; }

        public Option Midpoint { get; }

        public TextOption LutText { get; }

        public CheckBoxOption GainSwitch { get; }

        public LUTPanelOptions LutPanel { get; }

        public LutApplyOptions LutApply { get; }

        public LayoutBase AccelerationType
        {
            get
            {
                return AccelDropdown.SelectedItem as LayoutBase;
            }
            private set
            {
                AccelDropdown.SelectedItem = value;
            }
        }

        public override int Top 
        {
            get
            {
                return AccelDropdown.Top;
            } 
            set
            {
                AccelDropdown.Top = value;
                AccelTypeActiveValue.Top = value;
                Layout(value + AccelDropdown.Height + Constants.OptionVerticalSeperation);
            }
        }

        public override int Height
        {
            get
            {
                return AccelDropdown.Height;
            } 
        }

        public override int Left
        {
            get
            {
                return AccelDropdown.Left;
            } 
            set
            {
                AccelDropdown.Left = value;
                LutText.Left = value;
                LutPanel.Left = value;
                LutApply.Left = value;
            }
        }

        public override int Width
        {
            get
            {
                return AccelDropdown.Width;
            }
            set
            {
                AccelDropdown.Width = value;
                LutText.Width = value;
                LutPanel.Width = AccelTypeActiveValue.CenteringLabel.Right - AccelDropdown.Left;
                LutApply.Width = value;
            }
        }

        public override bool Visible
        {
            get
            {
                return AccelDropdown.Visible;
            }
        }

        private bool ShowingDefault { get; set; }

        #endregion Properties

        #region Methods

        public override void Hide()
        {
            AccelDropdown.Hide();
            AccelTypeActiveValue.Hide();

            GainSwitch.Hide();
            Acceleration.Hide();
            DecayRate.Hide();
            GrowthRate.Hide();
            Smooth.Hide();
            Scale.Hide();
            Cap.Hide();
            Weight.Hide();
            Offset.Hide();
            Limit.Hide();
            PowerClassic.Hide();
            Exponent.Hide();
            Midpoint.Hide();
            LutText.Hide();
            LutPanel.Hide();
            LutApply.Hide();
        }

        public void Show()
        {
            AccelDropdown.Show();
            AccelTypeActiveValue.Show();
            Layout(AccelDropdown.Bottom + Constants.OptionVerticalSeperation);
        }

        public override void Show(string name)
        {
            Show();
        }

        public void SetActiveValues(ref AccelArgs args)
        {
            AccelerationType = AccelTypeFromSettings(ref args);
            AccelTypeActiveValue.SetValue(AccelerationType.ActiveName);
            GainSwitch.SetActiveValue(args.gain);
            Weight.SetActiveValue(args.weight);
            Cap.SetActiveValue(args.cap.x);
            Offset.SetActiveValue(args.offset);
            Acceleration.SetActiveValue(args.acceleration);
            DecayRate.SetActiveValue(args.decayRate);
            GrowthRate.SetActiveValue(args.growthRate);
            Smooth.SetActiveValue(args.smooth);
            Scale.SetActiveValue(args.scale);
            Limit.SetActiveValue(args.limit);
            PowerClassic.SetActiveValue(args.exponentClassic);
            Exponent.SetActiveValue(args.exponentPower);
            Midpoint.SetActiveValue(args.midpoint);
            LutPanel.SetActiveValues(args.data, args.length, args.mode);
            // TODO - use GainSwitch only?
            LutApply.SetActiveValue(args.gain);
        }

        public void ShowFull()
        {
            if (ShowingDefault)
            {
                AccelDropdown.Text = Constants.AccelDropDownDefaultFullText;
            }

            Left = Acceleration.Left + Constants.DropDownLeftSeparation;
            Width = Acceleration.Width - Constants.DropDownLeftSeparation;

            LutText.Expand();
            HandleLUTOptionsOnResize();
        }

        public void ShowShortened()
        {
            if (ShowingDefault)
            {
                AccelDropdown.Text = Constants.AccelDropDownDefaultShortText;
            }

            Left = Acceleration.Field.Left;
            Width = Acceleration.Field.Width;

            LutText.Shorten();
        }

        public void SetArgs(ref AccelArgs args)
        {
            args.mode = AccelerationType.Mode;
            args.gain = GainSwitch.CheckBox.Checked;

            if (Acceleration.Visible) args.acceleration = Acceleration.Field.Data;
            if (DecayRate.Visible) args.decayRate = DecayRate.Field.Data;
            if (GrowthRate.Visible) args.growthRate = GrowthRate.Field.Data;
            if (Smooth.Visible) args.smooth = Smooth.Field.Data;
            if (Scale.Visible) args.scale = Scale.Field.Data;
            // TODO - make field for output and in_out cap
            if (Cap.Visible) args.cap.x = Cap.Field.Data;
            if (Limit.Visible) args.limit = Limit.Field.Data;
            if (PowerClassic.Visible) args.exponentClassic = PowerClassic.Field.Data;
            if (Exponent.Visible) args.exponentPower = Exponent.Field.Data;
            if (Offset.Visible) args.offset = Offset.Field.Data;
            if (Midpoint.Visible) args.midpoint = Midpoint.Field.Data;
            if (Weight.Visible) args.weight = Weight.Field.Data;
            if (LutPanel.Visible)
            {
                (var points, var length) = LutPanel.GetPoints();
                args.length = length * 2;

                for (int i = 0; i < length; i++)
                {
                    ref var p = ref points[i];
                    var data_idx = i * 2;
                    args.data[data_idx] = p.x;
                    args.data[data_idx + 1] = p.y;
                }
            }

        }

        public override void AlignActiveValues()
        {
            AccelTypeActiveValue.Align();
            GainSwitch.AlignActiveValues();
            Acceleration.AlignActiveValues();
            DecayRate.AlignActiveValues();
            GrowthRate.AlignActiveValues();
            Smooth.AlignActiveValues();
            Scale.AlignActiveValues();
            Cap.AlignActiveValues();
            Offset.AlignActiveValues();
            Weight.AlignActiveValues();
            Limit.AlignActiveValues();
            PowerClassic.AlignActiveValues();
            Exponent.AlignActiveValues();
            Midpoint.AlignActiveValues();
            LutApply.AlignActiveValues();
        }

        public void HandleLUTOptionsOnResize()
        {
            LutText.Left = AccelDropdown.Left;
            LutPanel.Left = GainSwitch.Left - 100;
            LutPanel.Width = Acceleration.ActiveValueLabel.CenteringLabel.Right - LutPanel.Left;
            LutApply.Left = LutPanel.Left;
            LutApply.Width = AccelDropdown.Right - LutPanel.Left;
        }

        private void OnIndexChanged(object sender, EventArgs e)
        {
            Layout(Beneath);
            ShowingDefault = false;
        }

        private void Layout(int top = -1)
        {
            if (top < 0)
            {
                top = GainSwitch.Top;
            }

            AccelerationType.Layout(
                GainSwitch,
                Acceleration,
                DecayRate,
                GrowthRate,
                Smooth,
                Scale,
                Cap,
                Weight,
                Offset,
                Limit,
                PowerClassic,
                Exponent,
                Midpoint,
                LutText,
                LutPanel,
                LutApply,
                top);
        }

        private LayoutBase AccelTypeFromSettings(ref AccelArgs args)
        { 
            switch (args.mode)
            {
                case AccelMode.classic:  return (args.exponentClassic == 2) ? Linear : Classic;
                case AccelMode.jump:     return Jump;
                case AccelMode.natural:  return Natural;
                case AccelMode.motivity: return Motivity;
                case AccelMode.power:    return Power;
                case AccelMode.lut:      return LUT;
                default:                 return Off;
            }
        }

        #endregion Methods
    }
}
