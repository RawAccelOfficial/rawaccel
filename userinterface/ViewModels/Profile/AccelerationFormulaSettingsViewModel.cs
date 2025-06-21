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
        public static ObservableCollection<string> FormulaTypes =
            new ObservableCollection<string>(
                Enum.GetValues(typeof(BEData.AccelerationFormulaType)).Cast<BEData.AccelerationFormulaType>()
                .Select(d => d.ToString()));
        public AccelerationFormulaSettingsViewModel(BE.FormulaAccelModel formulaAccel)
        {
            FormulaAccelBE = formulaAccel;
            SynchronousSettings = new SynchronousSettings(formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Synchronous) as BE.Formula.SynchronousAccelerationDefinitionModel);
            LinearSettings = new LinearSettings(formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Linear) as BE.Formula.LinearAccelerationDefinitionModel);
            ClassicSettings = new ClassicSettings(formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Classic) as BE.Formula.ClassicAccelerationDefinitionModel);
            PowerSettings = new PowerSettings(formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Power) as BE.Formula.PowerAccelerationDefinitionModel);
            NaturalSettings = new NaturalSettings(formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Natural) as BE.Formula.NaturalAccelerationDefinitionModel);
            JumpSettings = new JumpSettings(formulaAccel.GetAccelerationModelOfType(BEData.AccelerationFormulaType.Jump) as BE.Formula.JumpAccelerationDefinitionModel);
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
            SyncSpeed = new NamedEditableFieldViewModel(synchronousAccelModelBE.SyncSpeed);
            Motivity = new NamedEditableFieldViewModel(synchronousAccelModelBE.Motivity);
            Gamma = new NamedEditableFieldViewModel(synchronousAccelModelBE.Gamma);
            Smoothness = new NamedEditableFieldViewModel(synchronousAccelModelBE.Smoothness);
        }

        public NamedEditableFieldViewModel SyncSpeed { get; set; }

        public NamedEditableFieldViewModel Motivity { get; set; }

        public NamedEditableFieldViewModel Gamma { get; set; }

        public NamedEditableFieldViewModel Smoothness { get; set; }
    }

    public class LinearSettings
    {
        public LinearSettings(BE.Formula.LinearAccelerationDefinitionModel linearAccelModelBE)
        {
            Acceleration = new NamedEditableFieldViewModel(linearAccelModelBE.Acceleration);
            Offset = new NamedEditableFieldViewModel(linearAccelModelBE.Offset);
            Cap = new NamedEditableFieldViewModel(linearAccelModelBE.Cap);
        }

        public NamedEditableFieldViewModel Acceleration { get; set; }

        public NamedEditableFieldViewModel Offset { get; set; }

        public NamedEditableFieldViewModel Cap { get; set; }
    }

    public class ClassicSettings
    {
        public ClassicSettings(BE.Formula.ClassicAccelerationDefinitionModel classicAccelModelBE)
        {
            Acceleration = new NamedEditableFieldViewModel(classicAccelModelBE.Acceleration);
            Exponent = new NamedEditableFieldViewModel(classicAccelModelBE.Exponent);
            Offset = new NamedEditableFieldViewModel(classicAccelModelBE.Offset);
            Cap = new NamedEditableFieldViewModel(classicAccelModelBE.Cap);
        }

        public NamedEditableFieldViewModel Acceleration { get; set; }

        public NamedEditableFieldViewModel Exponent { get; set; }

        public NamedEditableFieldViewModel Offset { get; set; }

        public NamedEditableFieldViewModel Cap { get; set; }
    }

    public class PowerSettings
    {
        public PowerSettings(BE.Formula.PowerAccelerationDefinitionModel powerAccelModelBE)
        {
            Scale = new NamedEditableFieldViewModel(powerAccelModelBE.Scale);
            Exponent = new NamedEditableFieldViewModel(powerAccelModelBE.Exponent);
            OutputOffset = new NamedEditableFieldViewModel(powerAccelModelBE.OutputOffset);
            Cap = new NamedEditableFieldViewModel(powerAccelModelBE.Cap);
        }

        public NamedEditableFieldViewModel Scale { get; set; }

        public NamedEditableFieldViewModel Exponent { get; set; }

        public NamedEditableFieldViewModel OutputOffset { get; set; }

        public NamedEditableFieldViewModel Cap { get; set; }
    }

    public class NaturalSettings
    {
        public NaturalSettings(BE.Formula.NaturalAccelerationDefinitionModel naturalAccelModelBE)
        {
            DecayRate = new NamedEditableFieldViewModel(naturalAccelModelBE.DecayRate);
            InputOffset = new NamedEditableFieldViewModel(naturalAccelModelBE.InputOffset);
            Limit = new NamedEditableFieldViewModel(naturalAccelModelBE.Limit);
        }

        public NamedEditableFieldViewModel DecayRate { get; set; }

        public NamedEditableFieldViewModel InputOffset { get; set; }

        public NamedEditableFieldViewModel Limit { get; set; }
    }

    public class JumpSettings
    {
        public JumpSettings(BE.Formula.JumpAccelerationDefinitionModel jumpAccelModelBE)
        {
            Smooth = new NamedEditableFieldViewModel(jumpAccelModelBE.Smooth);
            Input = new NamedEditableFieldViewModel(jumpAccelModelBE.Input);
            Output = new NamedEditableFieldViewModel(jumpAccelModelBE.Output);
        }

        public NamedEditableFieldViewModel Smooth { get; set; }

        public NamedEditableFieldViewModel Input { get; set; }

        public NamedEditableFieldViewModel Output { get; set; }
    }
}
