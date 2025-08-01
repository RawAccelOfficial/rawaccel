using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using grapher.Models;
using System.IO;
using grapher.Models.Serialized;
using grapher.Models.Theming;
using grapher.Common;

namespace grapher
{
    public partial class RawAcceleration : Form
    {

        #region Constructor


        public RawAcceleration()
        {
            InitializeComponent();


            Version driverVersion = VersionHelper.ValidOrThrow();

            ToolStripMenuItem HelpMenuItem = new ToolStripMenuItem("&Help");

            HelpMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                    new ToolStripMenuItem("&About", null, (s, e) => {
                        using (var form = new AboutBox(driverVersion))
                        {
                            Theme.Apply(form);

                            form.ShowDialog();
                        }
                    })
            });
            
            //
            // load on startup addition
            //
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupFolder, "rawaccel.lnk");
            AutoLoadStartupItem.Checked = File.Exists(shortcutPath);

            AutoLoadStartupItem.Click += AutoLoadStartupItem_Click;



            var schemes = ColorSchemeManager.LoadSchemes().ToList();
            var themeMenuItem = new ToolStripMenuItem("&Themes");

            themeMenuItem.DropDownItemClicked += (s, e) =>
            {
                if (e.ClickedItem == null) return;

                foreach (ToolStripMenuItem item in themeMenuItem.DropDownItems)
                {
                    item.Checked = e.ClickedItem.Text == item.Text;
                }
            };

            var settings = GUISettings.MaybeLoad();

            Theme.CurrentScheme = ColorSchemeManager.GetSelected(settings, schemes);
            
            foreach (var colorScheme in schemes)
            {
                var menuItem = new ToolStripMenuItem(colorScheme.Name);

                menuItem.Checked = settings?.CurrentColorScheme == colorScheme.Name;
                themeMenuItem.DropDownItems.Add(menuItem);
            }

            menuStrip1.Items.AddRange(new ToolStripItem[] { themeMenuItem, HelpMenuItem });

            Theme.Apply(this, menuStrip1);

            AccelGUI = AccelGUIFactory.Construct(
                this,
                AccelerationChart,
                AccelerationChartY,
                VelocityChart,
                VelocityChartY,
                GainChart,
                GainChartY,
                chartContainer,
                accelTypeDropX,
                accelTypeDropY,
                XLutApplyDropdown,
                YLutApplyDropdown,
                CapTypeDropdownXClassic,
                CapTypeDropdownYClassic,
                CapTypeDropdownXPower,
                CapTypeDropdownYPower,
                writeButton,
                toggleButton,
                showVelocityGainToolStripMenuItem,
                showLastMouseMoveToolStripMenuItem,
                AutoWriteMenuItem,
                DeviceMenuItem,
                
                ScaleMenuItem,
                themeMenuItem,
                DPITextBox,
                PollRateTextBox,
                DirectionalityPanel,
                sensitivityBoxX,
                VertHorzRatioBox,
                rotationBox,
                inCapBoxXClassic,
                inCapBoxYClassic,
                outCapBoxXClassic,
                outCapBoxYClassic,
                inCapBoxXPower,
                inCapBoxYPower,
                outCapBoxXPower,
                outCapBoxYPower,
                inputJumpBoxX,
                inputJumpBoxY,
                outputJumpBoxX,
                outputJumpBoxY,
                inputOffsetBoxX,
                inputOffsetBoxY,
                outputOffsetBoxX,
                outputOffsetBoxY,
                accelerationBoxX,
                accelerationBoxY,
                decayRateBoxX,
                decayRateBoxY,
                gammaBoxX,
                gammaBoxY,
                smoothBoxX,
                smoothBoxY,
                scaleBoxX,
                scaleBoxY,
                limitBoxX,
                limitBoxY,
                powerBoxX,
                powerBoxY,
                expBoxX,
                expBoxY,
                syncSpeedBoxX,
                syncSpeedBoxY,
                DomainBoxX,
                DomainBoxY,
                RangeBoxX,
                RangeBoxY,
                LpNormBox,
                sensXYLock,
                ByComponentXYLock,
                FakeBox,
                WholeCheckBox,
                ByComponentCheckBox,
                gainSwitchX,
                gainSwitchY,
                XLutActiveValuesBox,
                YLutActiveValuesBox,
                XLutPointsBox,
                YLutPointsBox,
                LockXYLabel,
                sensitivityLabel,
                VertHorzRatioLabel,
                rotationLabel,
                inCapLabelXClassic,
                inCapLabelYClassic,
                outCapLabelXClassic,
                outCapLabelYClassic,
                CapTypeLabelXClassic,
                CapTypeLabelYClassic,
                inCapLabelXPower,
                inCapLabelYPower,
                outCapLabelXPower,
                outCapLabelYPower,
                CapTypeLabelXPower,
                CapTypeLabelYPower,
                inputJumpLabelX,
                inputJumpLabelY,
                outputJumpLabelX,
                outputJumpLabelY,
                inputOffsetLabelX,
                inputOffsetLabelY,
                outputOffsetLabelX,
                outputOffsetLabelY,
                constantOneLabelX,
                constantOneLabelY,
                decayRateLabelX,
                decayRateLabelY,
                gammaLabelX,
                gammaLabelY,
                smoothLabelX,
                smoothLabelY,
                scaleLabelX,
                scaleLabelY,
                limitLabelX,
                limitLabelY,
                powerLabelX,
                powerLabelY,
                expLabelX,
                expLabelY,
                LUTTextLabelX,
                LUTTextLabelY,
                constantThreeLabelX,
                constantThreeLabelY,
                ActiveValueTitle,
                ActiveValueTitleY,
                SensitivityMultiplierActiveLabel,
                VertHorzRatioActiveLabel,
                RotationActiveLabel,
                InCapActiveXLabelClassic,
                InCapActiveYLabelClassic,
                OutCapActiveXLabelClassic,
                OutCapActiveYLabelClassic,
                CapTypeActiveXLabelClassic,
                CapTypeActiveYLabelClassic,
                InCapActiveXLabelPower,
                InCapActiveYLabelPower,
                OutCapActiveXLabelPower,
                OutCapActiveYLabelPower,
                CapTypeActiveXLabelPower,
                CapTypeActiveYLabelPower,
                InputJumpActiveXLabel,
                InputJumpActiveYLabel,
                OutputJumpActiveXLabel,
                OutputJumpActiveYLabel,
                InputOffsetActiveXLabel,
                InputOffsetActiveYLabel,
                OutputOffsetActiveXLabel,
                OutputOffsetActiveYLabel,
                AccelerationActiveLabelX,
                AccelerationActiveLabelY,
                DecayRateActiveXLabel,
                DecayRateActiveYLabel,
                GammaActiveXLabel,
                GammaActiveYLabel,
                SmoothActiveXLabel,
                SmoothActiveYLabel,
                ScaleActiveXLabel,
                ScaleActiveYLabel,
                LimitActiveXLabel,
                LimitActiveYLabel,
                PowerClassicActiveXLabel,
                PowerClassicActiveYLabel,
                ExpActiveXLabel,
                ExpActiveYLabel,
                SyncSpeedActiveXLabel,
                SyncSpeedActiveYLabel,
                AccelTypeActiveLabelX,
                AccelTypeActiveLabelY,
                gainSwitchActiveLabelX,
                gainSwitchActiveLabelY,
                OptionSetXTitle,
                OptionSetYTitle,
                MouseLabel,
                DirectionalityLabel,
                DirectionalityX,
                DirectionalityY,
                DirectionalityActiveValueTitle,
                LPNormLabel,
                LpNormActiveValue,
                DirectionalDomainLabel,
                DomainActiveValueX,
                DomainActiveValueY,
                DirectionalityRangeLabel,
                RangeActiveValueX,
                RangeActiveValueY,
                XLutApplyLabel,
                YLutApplyLabel,
                LutApplyActiveXLabel,
                LutApplyActiveYLabel);

            ResizeAndCenter();
        }

        #endregion Constructor

        #region Properties

        public AccelGUI AccelGUI { get; }

        #endregion Properties

        #region Methods

        protected override void WndProc(ref Message m)
        {
            if (!(AccelGUI is null))
            {
                if (m.Msg == 0x00ff) // WM_INPUT
                {
                    AccelGUI.MouseWatcher.ReadMouseMove(m);
                }
                else if (m.Msg == 0x00fe) // WM_INPUT_DEVICE_CHANGE
                {
                    AccelGUI.Settings.OnDeviceChangeMessage();
                }
            }

            base.WndProc(ref m);
        }

        public void ResetAutoScroll()
        {
            chartsPanel.AutoScrollPosition = Constants.Origin;
        }

        public void ResizeAndCenter()
        {
            ResetAutoScroll();

            var workingArea = Screen.FromControl(this).WorkingArea;
            var chartsPreferredSize = chartsPanel.GetPreferredSize(Constants.MaxSize);

            Size = new Size
            {
                Width = Math.Min(workingArea.Width, optionsPanel.Size.Width + chartsPreferredSize.Width),
                Height = Math.Min(workingArea.Height, chartsPreferredSize.Height + 48)
            };

            Location = new Point
            {
                X = workingArea.X + (workingArea.Width - Size.Width) / 2,
                Y = workingArea.Y + (workingArea.Height - Size.Height) / 2
            };

        }

        #endregion Method

        static void MakeStartupShortcut(bool gui)
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            if (string.IsNullOrEmpty(startupFolder))
            {
                throw new Exception("Startup folder does not exist");
            }

            //Windows Script Host Shell Object
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
            dynamic shell = Activator.CreateInstance(t);

            try
            {
                // Delete any other RA related startup shortcuts
                var candidates = new[] { "rawaccel", "raw accel", "writer" };

                foreach (string path in Directory.EnumerateFiles(startupFolder, "*.lnk")
                    .Where(f => candidates.Any(f.Substring(startupFolder.Length).ToLower().Contains)))
                {
                    var link = shell.CreateShortcut(path);
                    try
                    {
                        string targetPath = link.TargetPath;

                        if (!(targetPath is null) && 
                            (targetPath.EndsWith("rawaccel.exe") ||
                                targetPath.EndsWith("writer.exe") &&
                                    new FileInfo(targetPath).Directory.GetFiles("rawaccel.exe").Any()))
                        {
                            File.Delete(path);
                        }
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(link);
                    }
                }

                var name = gui ? "rawaccel" : "writer";

                var lnk = shell.CreateShortcut($@"{startupFolder}\{name}.lnk");

                try
                {
                    if (!gui) lnk.Arguments = Constants.DefaultSettingsFileName;
                    lnk.TargetPath = $@"{Application.StartupPath}\{name}.exe";
                    
                    // Set "start in" directory to the application path
                    lnk.WorkingDirectory = Application.StartupPath;
                    
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }

            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        private void RemoveStartupShortcut()
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupFolder, "rawaccel.lnk");

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }

        private void RawAcceleration_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Location = Location;
            Properties.Settings.Default.Size = Size;

            Properties.Settings.Default.Save();
        }

        private void RawAcceleration_Load(object sender, EventArgs e)
        {
            Location = Properties.Settings.Default.Location;
            Size = Properties.Settings.Default.Size;
        }
        
        private void AutoLoadStartupItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Toggle shortcut creation/removal based on check box
                if (AutoLoadStartupItem.Checked)
                {
                    MakeStartupShortcut(true);
                }
                else
                {
                    RemoveStartupShortcut();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed To Update Startup Shortcut: {ex.Message}","ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
