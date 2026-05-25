using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;
using AccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using AccelMode = RawAccel.Contracts.AccelMode;
using Vec2D = RawAccel.Contracts.Vec2<double>;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface IJumpAccelerationDefinitionModel : IAccelDefinitionModelSpecific<JumpAccel>
    {
    }

    public class JumpAccelerationDefinitionModel
        : EditableSettingsSelectable<JumpAccel, FormulaAccel>,
        IJumpAccelerationDefinitionModel
    {
        public const string SmoothDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Smooth)}";
        public const string InputDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Input)}";
        public const string OutputDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Output)}";

        public JumpAccelerationDefinitionModel(
            [FromKeyedServices(SmoothDIKey)]IEditableSettingSpecific<double> smooth,
            [FromKeyedServices(InputDIKey)]IEditableSettingSpecific<double> input,
            [FromKeyedServices(OutputDIKey)]IEditableSettingSpecific<double> output)
            : base([smooth, input, output], [])
        {
            Smooth = smooth;
            Input = input;
            Output = output;
        }

        public IEditableSettingSpecific<double> Smooth { get; set; }

        public IEditableSettingSpecific<double> Input { get; set; }

        public IEditableSettingSpecific<double> Output { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.jump,
                smooth = Smooth.ModelValue,
                cap = new Vec2D { x = Input.ModelValue, y = Output.ModelValue },
            };
        }

        public override JumpAccel MapToData()
        {
            return new JumpAccel()
            {
                Smooth = Smooth.ModelValue,
                Input = Input.ModelValue,
                Output = Output.ModelValue
            };
        }

        protected override bool TryMapEditableSettingsFromData(JumpAccel data)
        {
            return Smooth.TryUpdateModelDirectly(data.Smooth)
                & Input.TryUpdateModelDirectly(data.Input)
                & Output.TryUpdateModelDirectly(data.Output);
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(JumpAccel data)
        {
            return true;
        }
    }
}
