using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Scanning.Ammos
{
    public class OneTileScannerAmmo : GeoScannerAmmo
    {
        public OneTileScannerAmmo(GlobalConfiguration globalConfiguration, AggregateField miningProbeRange = AggregateField.undefined, AggregateField miningProbeRangeModifier = AggregateField.undefined) : base(globalConfiguration, miningProbeRange, miningProbeRangeModifier)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }
    }
}