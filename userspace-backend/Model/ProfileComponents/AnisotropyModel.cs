using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;
using Vec2D = RawAccel.Contracts.Vec2<double>;

namespace userspace_backend.Model.ProfileComponents
{
    public interface IAnisotropyModel : IEditableSettingsCollectionSpecific<Anisotropy>
    {
        IEditableSettingSpecific<double> DomainX { get; }

        IEditableSettingSpecific<double> DomainY { get; }

        IEditableSettingSpecific<double> RangeX { get; }

        IEditableSettingSpecific<double> RangeY { get; }

        IEditableSettingSpecific<double> LPNorm { get; }

        IEditableSettingSpecific<bool> CombineXYComponents { get; }

        Vec2D MapDomainToDriver();

        Vec2D MapRangeToDriver();
    }

    public class AnisotropyModel : EditableSettingsCollectionV2<Anisotropy>, IAnisotropyModel
    {
        public const string DomainXDIKey = $"{nameof(AnisotropyModel)}.{nameof(DomainX)}";
        public const string DomainYDIKey = $"{nameof(AnisotropyModel)}.{nameof(DomainY)}";
        public const string RangeXDIKey = $"{nameof(AnisotropyModel)}.{nameof(RangeX)}";
        public const string RangeYDIKey = $"{nameof(AnisotropyModel)}.{nameof(RangeY)}";
        public const string LPNormDIKey = $"{nameof(AnisotropyModel)}.{nameof(LPNorm)}";
        public const string CombineXYComponentsDIKey = $"{nameof(AnisotropyModel)}.{nameof(CombineXYComponents)}";

        public AnisotropyModel(
            [FromKeyedServices(DomainXDIKey)]IEditableSettingSpecific<double> domainX,
            [FromKeyedServices(DomainYDIKey)]IEditableSettingSpecific<double> domainY,
            [FromKeyedServices(RangeXDIKey)]IEditableSettingSpecific<double> rangeX,
            [FromKeyedServices(RangeYDIKey)]IEditableSettingSpecific<double> rangeY,
            [FromKeyedServices(LPNormDIKey)]IEditableSettingSpecific<double> lpNorm,
            [FromKeyedServices(CombineXYComponentsDIKey)]IEditableSettingSpecific<bool> combineXYComponents
            ) : base([domainX, domainY, rangeX, rangeY, lpNorm, combineXYComponents], [])
        {
            DomainX = domainX;
            DomainY = domainY;
            RangeX = rangeX;
            RangeY = rangeY;
            LPNorm = lpNorm;
            CombineXYComponents = combineXYComponents;
        }

        public IEditableSettingSpecific<double> DomainX { get; set; }

        public IEditableSettingSpecific<double> DomainY { get; set; }

        public IEditableSettingSpecific<double> RangeX { get; set; }

        public IEditableSettingSpecific<double> RangeY { get; set; }

        public IEditableSettingSpecific<double> LPNorm { get; set; }

        public IEditableSettingSpecific<bool> CombineXYComponents { get; set; }

        public Vec2D MapDomainToDriver() => new Vec2D { x = DomainX.ModelValue, y = DomainY.ModelValue };

        public Vec2D MapRangeToDriver() => new Vec2D { x = RangeX.ModelValue, y = RangeY.ModelValue };

        public override Anisotropy MapToData()
        {
            return new Anisotropy()
            {
                Domain = new Vector2() { X = DomainX.ModelValue, Y = DomainY.ModelValue },
                Range = new Vector2() { X = RangeX.ModelValue, Y = RangeY.ModelValue },
                LPNorm = LPNorm.ModelValue,
                CombineXYComponents = CombineXYComponents.ModelValue,
            };
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(Anisotropy data)
        {
            // Nothing to do here
            return true;
        }

        protected override bool TryMapEditableSettingsFromData(Anisotropy data)
        {
            if (data == null) return false;

            // Identity weights ((1,1),(1,1)) are the only meaningful defaults; a (0,0) vector
            // was written by an older buggy fallback and produces a degenerate flat curve.
            // Substitute identity for that specific corrupted shape on load.
            Vector2 domain = (data.Domain == null || (data.Domain.X == 0 && data.Domain.Y == 0))
                ? new Vector2 { X = 1, Y = 1 }
                : data.Domain;
            Vector2 range = (data.Range == null || (data.Range.X == 0 && data.Range.Y == 0))
                ? new Vector2 { X = 1, Y = 1 }
                : data.Range;

            return DomainX.TryUpdateModelDirectly(domain.X)
                & DomainY.TryUpdateModelDirectly(domain.Y)
                & RangeX.TryUpdateModelDirectly(range.X)
                & RangeY.TryUpdateModelDirectly(range.Y)
                & LPNorm.TryUpdateModelDirectly(data.LPNorm)
                & CombineXYComponents.TryUpdateModelDirectly(data.CombineXYComponents);
        }
    }
}
