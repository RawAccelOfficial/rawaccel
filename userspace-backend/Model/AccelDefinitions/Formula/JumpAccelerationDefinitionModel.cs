using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;
using RaAccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using RaAccelMode = RawAccel.Contracts.AccelMode;
using Vec2D = RawAccel.Contracts.Vec2<double>;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface IJumpAccelerationDefinitionModel : IAccelDefinitionModelSpecific<JumpAccel>
    {
    }

    public class JumpAccelerationDefinitionModel
        : FormulaAccelerationDefinitionModel<JumpAccel>,
        IJumpAccelerationDefinitionModel
    {
        public const string SmoothDIKey = $"{nameof(JumpAccelerationDefinitionModel)}.{nameof(Smooth)}";
        public const string InputDIKey = $"{nameof(JumpAccelerationDefinitionModel)}.{nameof(Input)}";
        public const string OutputDIKey = $"{nameof(JumpAccelerationDefinitionModel)}.{nameof(Output)}";

        public JumpAccelerationDefinitionModel(
            [FromKeyedServices(SmoothDIKey)]IEditableSettingSpecific<double> smooth,
            [FromKeyedServices(InputDIKey)]IEditableSettingSpecific<double> input,
            [FromKeyedServices(OutputDIKey)]IEditableSettingSpecific<double> output)
            : base([smooth, input, output])
        {
            Smooth = smooth;
            Input = input;
            Output = output;
        }

        public IEditableSettingSpecific<double> Smooth { get; set; }

        public IEditableSettingSpecific<double> Input { get; set; }

        public IEditableSettingSpecific<double> Output { get; set; }

        public override RaAccelArgs MapToDriver()
        {
            return new RaAccelArgs
            {
                mode = RaAccelMode.jump,
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
    }
}
