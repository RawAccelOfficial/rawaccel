using System;
using System.Collections.ObjectModel;
using System.Linq;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model.AccelDefinitions;
using BEData = userspace_backend.Data.Profiles.Accel.FormulaAccel;

namespace userinterface.ViewModels.Profile
{
    public class AccelerationFormulaSettingsViewModel : ViewModelBase
    {
        public static ObservableCollection<string> FormulaTypes { get; } =
            new(Enum.GetValues(typeof(BEData.AccelerationFormulaType))
                .Cast<BEData.AccelerationFormulaType>()
                .Select(formulaType => formulaType.ToString()));

        public AccelerationFormulaSettingsViewModel(BE.FormulaAccelModel formulaAccel)
        {
            FormulaAccelBE = formulaAccel;

            SynchronousSettings = new SynchronousSettings((formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Synchronous)
                as BE.Formula.SynchronousAccelerationDefinitionModel)!);

            LinearSettings = new LinearSettings((formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Linear)
                as BE.Formula.LinearAccelerationDefinitionModel)!);

            ClassicSettings = new ClassicSettings((formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Classic)
                as BE.Formula.ClassicAccelerationDefinitionModel)!);

            PowerSettings = new PowerSettings((formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Power)
                as BE.Formula.PowerAccelerationDefinitionModel)!);

            NaturalSettings = new NaturalSettings((formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Natural)
                as BE.Formula.NaturalAccelerationDefinitionModel)!);

            JumpSettings = new JumpSettings((formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Jump)
                as BE.Formula.JumpAccelerationDefinitionModel)!);
        }

        public BE.FormulaAccelModel FormulaAccelBE { get; }

        public ObservableCollection<string> FormulaTypesLocal => FormulaTypes;

        public SynchronousSettings SynchronousSettings { get; }

        public LinearSettings LinearSettings { get; }

        public ClassicSettings ClassicSettings { get; }

        public PowerSettings PowerSettings { get; }

        public NaturalSettings NaturalSettings { get; }

        public JumpSettings JumpSettings { get; }
    }

    public class SynchronousSettings
    {
        public SynchronousSettings(BE.Formula.SynchronousAccelerationDefinitionModel synchronousAccelModelBE)
        {
            SyncSpeed = new EditableFieldViewModel(synchronousAccelModelBE.SyncSpeed);
            Motivity = new EditableFieldViewModel(synchronousAccelModelBE.Motivity);
            Gamma = new EditableFieldViewModel(synchronousAccelModelBE.Gamma);
            Smoothness = new EditableFieldViewModel(synchronousAccelModelBE.Smoothness);
        }

        public EditableFieldViewModel SyncSpeed { get; set; }

        public EditableFieldViewModel Motivity { get; set; }

        public EditableFieldViewModel Gamma { get; set; }

        public EditableFieldViewModel Smoothness { get; set; }
    }

    public class LinearSettings
    {
        public LinearSettings(BE.Formula.LinearAccelerationDefinitionModel linearAccelModelBE)
        {
            Acceleration = new EditableFieldViewModel(linearAccelModelBE.Acceleration);
            Offset = new EditableFieldViewModel(linearAccelModelBE.Offset);
            Cap = new EditableFieldViewModel(linearAccelModelBE.Cap);
        }

        public EditableFieldViewModel Acceleration { get; set; }

        public EditableFieldViewModel Offset { get; set; }

        public EditableFieldViewModel Cap { get; set; }
    }

    public class ClassicSettings
    {
        public ClassicSettings(BE.Formula.ClassicAccelerationDefinitionModel classicAccelModelBE)
        {
            Acceleration = new EditableFieldViewModel(classicAccelModelBE.Acceleration);
            Exponent = new EditableFieldViewModel(classicAccelModelBE.Exponent);
            Offset = new EditableFieldViewModel(classicAccelModelBE.Offset);
            Cap = new EditableFieldViewModel(classicAccelModelBE.Cap);
        }

        public EditableFieldViewModel Acceleration { get; set; }

        public EditableFieldViewModel Exponent { get; set; }

        public EditableFieldViewModel Offset { get; set; }

        public EditableFieldViewModel Cap { get; set; }
    }

    public class PowerSettings
    {
        public PowerSettings(BE.Formula.PowerAccelerationDefinitionModel powerAccelModelBE)
        {
            Scale = new EditableFieldViewModel(powerAccelModelBE.Scale);
            Exponent = new EditableFieldViewModel(powerAccelModelBE.Exponent);
            OutputOffset = new EditableFieldViewModel(powerAccelModelBE.OutputOffset);
            Cap = new EditableFieldViewModel(powerAccelModelBE.Cap);
        }

        public EditableFieldViewModel Scale { get; set; }

        public EditableFieldViewModel Exponent { get; set; }

        public EditableFieldViewModel OutputOffset { get; set; }

        public EditableFieldViewModel Cap { get; set; }
    }

    public class NaturalSettings
    {
        public NaturalSettings(BE.Formula.NaturalAccelerationDefinitionModel naturalAccelModelBE)
        {
            DecayRate = new EditableFieldViewModel(naturalAccelModelBE.DecayRate);
            InputOffset = new EditableFieldViewModel(naturalAccelModelBE.InputOffset);
            Limit = new EditableFieldViewModel(naturalAccelModelBE.Limit);
        }

        public EditableFieldViewModel DecayRate { get; set; }

        public EditableFieldViewModel InputOffset { get; set; }

        public EditableFieldViewModel Limit { get; set; }
    }

    public class JumpSettings
    {
        public JumpSettings(BE.Formula.JumpAccelerationDefinitionModel jumpAccelModelBE)
        {
            Smooth = new EditableFieldViewModel(jumpAccelModelBE.Smooth);
            Input = new EditableFieldViewModel(jumpAccelModelBE.Input);
            Output = new EditableFieldViewModel(jumpAccelModelBE.Output);
        }

        public EditableFieldViewModel Smooth { get; set; }

        public EditableFieldViewModel Input { get; set; }

        public EditableFieldViewModel Output { get; set; }
    }
}
