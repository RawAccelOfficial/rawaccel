﻿using grapher.Models.Calculations;
using grapher.Models.Devices;
using grapher.Models.Mouse;
using grapher.Models.Options;
using grapher.Models.Options.Cap;
using grapher.Models.Options.Directionality;
using grapher.Models.Options.LUT;
using grapher.Models.Serialized;
using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace grapher.Models
{
    public static class AccelGUIFactory
    {
        #region Methods

        public static AccelGUI Construct(
            RawAcceleration form,
            Chart accelerationChart,
            Chart accelerationChartY,
            Chart velocityChart,
            Chart velocityChartY,
            Chart gainChart,
            Chart gainChartY,
            ComboBox accelTypeDropX,
            ComboBox accelTypeDropY,
            ComboBox lutApplyDropdownX,
            ComboBox lutApplyDropdownY,
            ComboBox capTypeDropdownX,
            ComboBox capTypeDropdownY,
            Button writeButton,
            ButtonBase toggleButton,
            ToolStripMenuItem showVelocityGainToolStripMenuItem,
            ToolStripMenuItem showLastMouseMoveMenuItem,
            ToolStripMenuItem streamingModeToolStripMenuItem,
            ToolStripMenuItem autoWriteMenuItem,
            ToolStripMenuItem deviceMenuItem,
            ToolStripMenuItem scaleMenuItem,
            ToolStripTextBox dpiTextBox,
            ToolStripTextBox pollRateTextBox,
            Panel directionalityPanel,
            TextBox sensitivityBoxX,
            TextBox sensitivityBoxY,
            TextBox rotationBox,
            TextBox weightBoxX,
            TextBox weightBoxY,
            TextBox inCapBoxX,
            TextBox inCapBoxY,
            TextBox outCapBoxX,
            TextBox outCapBoxY,
            TextBox offsetBoxX,
            TextBox offsetBoxY,
            TextBox accelerationBoxX,
            TextBox accelerationBoxY,
            TextBox decayRateBoxX,
            TextBox decayRateBoxY,
            TextBox growthRateBoxX,
            TextBox growthRateBoxY,
            TextBox smoothBoxX,
            TextBox smoothBoxY,
            TextBox scaleBoxX,
            TextBox scaleBoxY,
            TextBox limitBoxX,
            TextBox limitBoxY,
            TextBox powerClassicBoxX,
            TextBox powerClassicBoxY,
            TextBox expBoxX,
            TextBox expBoxY,
            TextBox midpointBoxX,
            TextBox midpointBoxY,
            TextBox domainBoxX,
            TextBox domainBoxY,
            TextBox rangeBoxX,
            TextBox rangeBoxY,
            TextBox lpNormBox,
            CheckBox sensXYLock,
            CheckBox byComponentXYLock,
            CheckBox fakeBox,
            CheckBox wholeCheckBox,
            CheckBox byComponentCheckBox,
            CheckBox gainSwitchX,
            CheckBox gainSwitchY,
            RichTextBox xLutActiveValuesBox,
            RichTextBox yLutActiveValuesBox,
            RichTextBox xLutPointsBox,
            RichTextBox yLutPointsBox,
            Label lockXYLabel,
            Label sensitivityLabel,
            Label yxRatioLabel,
            Label rotationLabel,
            Label weightLabelX,
            Label weightLabelY,
            Label inCapLabelX,
            Label inCapLabelY,
            Label outCapLabelX,
            Label outCapLabelY,
            Label capTypeLabelX,
            Label capTypeLabelY,
            Label offsetLabelX,
            Label offsetLabelY,
            Label constantOneLabelX,
            Label constantOneLabelY,
            Label decayRateLabelX,
            Label decayRateLabelY,
            Label growthRateLabelX,
            Label growthRateLabelY,
            Label smoothLabelX,
            Label smoothLabelY,
            Label scaleLabelX,
            Label scaleLabelY,
            Label limitLabelX,
            Label limitLabelY,
            Label powerClassicLabelX,
            Label powerClassicLabelY,
            Label expLabelX,
            Label expLabelY,
            Label lutTextLabelX,
            Label lutTextLabelY,
            Label constantThreeLabelX,
            Label constantThreeLabelY,
            Label activeValueTitleX,
            Label activeValueTitleY,
            Label sensitivityActiveLabel,
            Label yxRatioActiveLabel,
            Label rotationActiveLabel,
            Label weightActiveXLabel,
            Label weightActiveYLabel,
            Label inCapActiveXLabel,
            Label inCapActiveYLabel,
            Label outCapActiveXLabel,
            Label outCapActiveYLabel,
            Label capTypeActiveXLabel,
            Label capTypeActiveYLabel,
            Label offsetActiveLabelX,
            Label offsetActiveLabelY,
            Label accelerationActiveLabelX,
            Label accelerationActiveLabelY,
            Label decayRateActiveLabelX,
            Label decayRateActiveLabelY,
            Label growthRateActiveLabelX,
            Label growthRateActiveLabelY,
            Label smoothActiveLabelX,
            Label smoothActiveLabelY,
            Label scaleActiveLabelX,
            Label scaleActiveLabelY,
            Label limitActiveLabelX,
            Label limitActiveLabelY,
            Label powerClassicActiveLabelX,
            Label powerClassicActiveLabelY,
            Label expActiveLabelX,
            Label expActiveLabelY,
            Label midpointActiveLabelX,
            Label midpointActiveLabelY,
            Label accelTypeActiveLabelX,
            Label accelTypeActiveLabelY,
            Label gainSwitchActiveLabelX,
            Label gainSwitchActiveLabelY,
            Label optionSetXTitle,
            Label optionSetYTitle,
            Label mouseLabel,
            Label directionalityLabel,
            Label directionalityX,
            Label directionalityY,
            Label direcionalityActiveValueTitle,
            Label lpNormLabel,
            Label lpNormActiveLabel,
            Label domainLabel,
            Label domainActiveValueX,
            Label domainActiveValueY,
            Label rangeLabel,
            Label rangeActiveValueX,
            Label rangeActiveValueY,
            Label lutApplyLabelX,
            Label lutApplyLabelY,
            Label lutApplyActiveValueX,
            Label lutApplyActiveValueY)
        {
            fakeBox.Checked = false;
            fakeBox.Hide();

            var accelCalculator = new AccelCalculator(
                new Field(dpiTextBox.TextBox, form, Constants.DefaultDPI, 1),
                new Field(pollRateTextBox.TextBox, form, Constants.DefaultPollRate, 1));

            var accelCharts = new AccelCharts(
                                form,
                                new ChartXY(accelerationChart, accelerationChartY, Constants.SensitivityChartTitle),
                                new ChartXY(velocityChart, velocityChartY, Constants.VelocityChartTitle),
                                new ChartXY(gainChart, gainChartY, Constants.GainChartTitle),
                                showVelocityGainToolStripMenuItem,
                                showLastMouseMoveMenuItem,
                                streamingModeToolStripMenuItem,
                                writeButton,
                                accelCalculator);

            var sensitivity = new Option(
                sensitivityBoxX,
                form,
                1,
                sensitivityLabel,
                0,
                new ActiveValueLabel(sensitivityActiveLabel, activeValueTitleX),
                "Sens Multiplier");

            var yxRatio = new LockableOption(
                new Option(
                    sensitivityBoxY,
                    form,
                    1,
                    yxRatioLabel,
                    0,
                    new ActiveValueLabel(yxRatioActiveLabel, activeValueTitleX),
                    "Y/X Ratio"),
                sensXYLock,
                1);

            var rotation = new Option(
                rotationBox,
                form,
                0,
                rotationLabel,
                0,
                new ActiveValueLabel(rotationActiveLabel, activeValueTitleX),
                "Rotation");

            var optionSetYLeft = activeValueTitleX.Left + activeValueTitleX.Width;

            var directionalityLeft = directionalityPanel.Left;

            var weightX = new Option(
                weightBoxX,
                form,
                1,
                weightLabelX,
                0,
                new ActiveValueLabel(weightActiveXLabel, activeValueTitleX),
                "Weight");

            var weightY = new Option(
                weightBoxY,
                form,
                1,
                weightLabelY,
                optionSetYLeft,
                new ActiveValueLabel(weightActiveYLabel, activeValueTitleY),
                "Weight");

            var offsetX = new Option(
                offsetBoxX,
                form,
                0,
                offsetLabelX,
                0,
                new ActiveValueLabel(offsetActiveLabelX, activeValueTitleX),
                "Offset");

            var offsetY = new Option(
                offsetBoxY,
                form,
                0,
                offsetLabelY,
                optionSetYLeft,
                new ActiveValueLabel(offsetActiveLabelY, activeValueTitleY),
                "Offset");

            var accelerationX = new Option(
                new Field(accelerationBoxX, form, 0),
                constantOneLabelX,
                new ActiveValueLabel(accelerationActiveLabelX, activeValueTitleX),
                0);

            var accelerationY = new Option(
                new Field(accelerationBoxY, form, 0),
                constantOneLabelY,
                new ActiveValueLabel(accelerationActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var decayRateX = new Option(
                new Field(decayRateBoxX, form, 0),
                decayRateLabelX,
                new ActiveValueLabel(decayRateActiveLabelX, activeValueTitleX),
                0);

            var decayRateY = new Option(
                new Field(decayRateBoxY, form, 0),
                decayRateLabelY,
                new ActiveValueLabel(decayRateActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var growthRateX = new Option(
                new Field(growthRateBoxX, form, 0),
                growthRateLabelX,
                new ActiveValueLabel(growthRateActiveLabelX, activeValueTitleX),
                0);

            var growthRateY = new Option(
                new Field(growthRateBoxY, form, 0),
                growthRateLabelY,
                new ActiveValueLabel(growthRateActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var smoothX = new Option(
                new Field(smoothBoxX, form, 0),
                smoothLabelX,
                new ActiveValueLabel(smoothActiveLabelX, activeValueTitleX),
                0);

            var smoothY = new Option(
                new Field(smoothBoxY, form, 0),
                smoothLabelY,
                new ActiveValueLabel(smoothActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var scaleX = new Option(
                new Field(scaleBoxX, form, 0),
                scaleLabelX,
                new ActiveValueLabel(scaleActiveLabelX, activeValueTitleX),
                0);

            var scaleY = new Option(
                new Field(scaleBoxY, form, 0),
                scaleLabelY,
                new ActiveValueLabel(scaleActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var limitX = new Option(
                new Field(limitBoxX, form, 2),
                limitLabelX,
                new ActiveValueLabel(limitActiveLabelX, activeValueTitleX),
                0);

            var limitY = new Option(
                new Field(limitBoxY, form, 2),
                limitLabelY,
                new ActiveValueLabel(limitActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var powerClassicX = new Option(
                new Field(powerClassicBoxX, form, 2),
                powerClassicLabelX,
                new ActiveValueLabel(powerClassicActiveLabelX, activeValueTitleX),
                0);

            var powerClassicY = new Option(
                new Field(powerClassicBoxY, form, 2),
                powerClassicLabelY,
                new ActiveValueLabel(powerClassicActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var exponentX = new Option(
                new Field(expBoxX, form, 2),
                expLabelX,
                new ActiveValueLabel(expActiveLabelX, activeValueTitleX),
                0);

            var exponentY = new Option(
                new Field(expBoxY, form, 2),
                expLabelY,
                new ActiveValueLabel(expActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var midpointX = new Option(
                new Field(midpointBoxX, form, 0),
                constantThreeLabelX,
                new ActiveValueLabel(midpointActiveLabelX, activeValueTitleX),
                0);

            var midpointY = new Option(
                new Field(midpointBoxY, form, 0),
                constantThreeLabelY,
                new ActiveValueLabel(midpointActiveLabelY, activeValueTitleY),
                optionSetYLeft);

            var inCapX = new Option(
                inCapBoxX,
                form,
                0,
                inCapLabelX,
                0,
                new ActiveValueLabel(inCapActiveXLabel, activeValueTitleX),
                "Cap: Input");

            var inCapY = new Option(
                inCapBoxY,
                form,
                0,
                inCapLabelY,
                optionSetYLeft,
                new ActiveValueLabel(inCapActiveYLabel, activeValueTitleY),
                "Cap");

            var outCapX = new Option(
                outCapBoxX,
                form,
                0,
                outCapLabelX,
                0,
                new ActiveValueLabel(outCapActiveXLabel, activeValueTitleX),
                "Cap: Input");

            var outCapY = new Option(
                outCapBoxY,
                form,
                0,
                outCapLabelY,
                optionSetYLeft,
                new ActiveValueLabel(outCapActiveYLabel, activeValueTitleY),
                "Cap");

            var capTypeX = new CapTypeOptions(
                capTypeLabelX,
                capTypeDropdownX,
                new ActiveValueLabel(capTypeActiveXLabel, activeValueTitleX));

            var capTypeY = new CapTypeOptions(
                capTypeLabelY,
                capTypeDropdownY,
                new ActiveValueLabel(capTypeActiveYLabel, activeValueTitleY));

            var accelCapOptionsX = new CapOptions(
                capTypeX,
                inCapX,
                outCapX,
                accelerationX);

            var accelCapOptionsY = new CapOptions(
                capTypeY,
                inCapY,
                outCapY,
                accelerationY);

            var powerCapOptionsX = new CapOptions(
                capTypeX,
                inCapX,
                outCapX,
                accelerationX);

            var powerCapOptionsY = new CapOptions(
                capTypeY,
                inCapY,
                outCapY,
                accelerationY);

            var lpNorm = new Option(
                new Field(lpNormBox, form, 2),
                lpNormLabel,
                new ActiveValueLabel(lpNormActiveLabel, direcionalityActiveValueTitle),
                directionalityLeft);

            var domain = new OptionXY(
                domainBoxX,
                domainBoxY,
                fakeBox,
                form,
                1,
                domainLabel,
                new ActiveValueLabelXY(
                    new ActiveValueLabel(domainActiveValueX, direcionalityActiveValueTitle),
                    new ActiveValueLabel(domainActiveValueY, direcionalityActiveValueTitle)),
                false);

            var range = new OptionXY(
                rangeBoxX,
                rangeBoxY,
                fakeBox,
                form,
                1,
                rangeLabel,
                new ActiveValueLabelXY(
                    new ActiveValueLabel(rangeActiveValueX, direcionalityActiveValueTitle),
                    new ActiveValueLabel(rangeActiveValueY, direcionalityActiveValueTitle)),
                false);


            var lutTextX = new TextOption(lutTextLabelX);
            var lutTextY = new TextOption(lutTextLabelY);
            var gainSwitchOptionX = new CheckBoxOption(
                                            gainSwitchX,
                                            new ActiveValueLabel(gainSwitchActiveLabelX, activeValueTitleX));
            var gainSwitchOptionY = new CheckBoxOption(
                                            gainSwitchY,
                                            new ActiveValueLabel(gainSwitchActiveLabelY, activeValueTitleY));

            var accelerationOptionsX = new AccelTypeOptions(
                accelTypeDropX,
                gainSwitchOptionX,
                accelCapOptionsX,
                powerCapOptionsX,
                decayRateX,
                growthRateX,
                smoothX,
                weightX,
                offsetX,
                limitX,
                powerClassicX,
                exponentX,
                midpointX,
                lutTextX,
                new LUTPanelOptions(xLutPointsBox, xLutActiveValuesBox),
                new LutApplyOptions(
                    lutApplyLabelX,
                    lutApplyDropdownX,
                    new ActiveValueLabel(lutApplyActiveValueX, activeValueTitleX)),
                writeButton,
                new ActiveValueLabel(accelTypeActiveLabelX, activeValueTitleX));

            var accelerationOptionsY = new AccelTypeOptions(
                accelTypeDropY,
                gainSwitchOptionY,
                accelCapOptionsY,
                powerCapOptionsY,
                decayRateY,
                growthRateY,
                smoothY,
                weightY,
                offsetY,
                limitY,
                powerClassicY,
                exponentY,
                midpointY,
                lutTextY,
                new LUTPanelOptions(yLutPointsBox, yLutActiveValuesBox),
                new LutApplyOptions(
                    lutApplyLabelY,
                    lutApplyDropdownY,
                    new ActiveValueLabel(lutApplyActiveValueY, activeValueTitleY)),
                writeButton,
                new ActiveValueLabel(accelTypeActiveLabelY, activeValueTitleY));

            var optionsSetX = new AccelOptionSet(
                optionSetXTitle,
                activeValueTitleX,
                rotationBox.Top + rotationBox.Height + Constants.OptionVerticalSeperation,
                accelerationOptionsX);

            var optionsSetY = new AccelOptionSet(
                optionSetYTitle,
                activeValueTitleY,
                rotationBox.Top + rotationBox.Height + Constants.OptionVerticalSeperation,
                accelerationOptionsY);

            var directionalOptions = new DirectionalityOptions(
                directionalityPanel,
                directionalityLabel,
                directionalityX,
                directionalityY,
                direcionalityActiveValueTitle,
                lpNorm,
                domain,
                range,
                wholeCheckBox,
                byComponentCheckBox,
                Constants.DirectionalityVerticalOffset);

            var applyOptions = new ApplyOptions(
                byComponentXYLock,
                optionsSetX,
                optionsSetY,
                directionalOptions,
                sensitivity,
                yxRatio,
                rotation,
                lockXYLabel,
                accelCharts);

            var settings = new SettingsManager(
                accelCalculator.DPI,
                accelCalculator.PollRate,
                autoWriteMenuItem,
                showLastMouseMoveMenuItem,
                showVelocityGainToolStripMenuItem,
                streamingModeToolStripMenuItem,
                deviceMenuItem);

            var mouseWatcher = new MouseWatcher(form, mouseLabel, accelCharts, settings);

            return new AccelGUI(
                form,
                accelCalculator,
                accelCharts,
                settings,
                applyOptions,
                writeButton,
                toggleButton,
                mouseWatcher,
                scaleMenuItem);
        }

        #endregion Methods
    }
}
