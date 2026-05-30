using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;
using RaAccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using RaAccelMode = RawAccel.Contracts.AccelMode;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface ISynchronousAccelerationDefinitionModel : IAccelDefinitionModelSpecific<SynchronousAccel>
    {
        IEditableSettingSpecific<double> SyncSpeed { get; }

        IEditableSettingSpecific<double> Motivity { get; }

        IEditableSettingSpecific<double> Gamma { get; }

        IEditableSettingSpecific<double> Smoothness { get; }
    }

    public class SynchronousAccelerationDefinitionModel
        : FormulaAccelerationDefinitionModel<SynchronousAccel>,
        ISynchronousAccelerationDefinitionModel
    {
        public const string SyncSpeedDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(SyncSpeed)}";
        public const string MotivityDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(Motivity)}";
        public const string GammaDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(Gamma)}";
        public const string SmoothnessDIKey = $"{nameof(SynchronousAccelerationDefinitionModel)}.{nameof(Smoothness)}";

        public SynchronousAccelerationDefinitionModel(
            [FromKeyedServices(SyncSpeedDIKey)]IEditableSettingSpecific<double> syncSpeed,
            [FromKeyedServices(MotivityDIKey)]IEditableSettingSpecific<double> motivity,
            [FromKeyedServices(GammaDIKey)]IEditableSettingSpecific<double> gamma,
            [FromKeyedServices(SmoothnessDIKey)]IEditableSettingSpecific<double> smoothness)
            : base([syncSpeed, motivity, gamma, smoothness])
        {
            SyncSpeed = syncSpeed;
            Motivity = motivity;
            Gamma = gamma;
            Smoothness = smoothness;
        }

        public IEditableSettingSpecific<double> SyncSpeed { get; set; }

        public IEditableSettingSpecific<double> Motivity { get; set; }

        public IEditableSettingSpecific<double> Gamma { get; set; }

        public IEditableSettingSpecific<double> Smoothness { get; set; }

        public override RaAccelArgs MapToDriver()
        {
            return new RaAccelArgs
            {
                mode = RaAccelMode.synchronous,
                syncSpeed = SyncSpeed.ModelValue,
                motivity = Motivity.ModelValue,
                gamma = Gamma.ModelValue,
                smooth = Smoothness.ModelValue,
            };
        }

        public override SynchronousAccel MapToData()
        {
            return new SynchronousAccel()
            {
                SyncSpeed = SyncSpeed.ModelValue,
                Motivity = Motivity.ModelValue,
                Gamma = Gamma.ModelValue,
                Smoothness = Smoothness.ModelValue,
            };
        }

        protected override bool TryMapEditableSettingsFromData(SynchronousAccel data)
        {
            return SyncSpeed.TryUpdateModelDirectly(data.SyncSpeed)
                & Motivity.TryUpdateModelDirectly(data.Motivity)
                & Gamma.TryUpdateModelDirectly(data.Gamma)
                & Smoothness.TryUpdateModelDirectly(data.Smoothness);
        }
    }
}
