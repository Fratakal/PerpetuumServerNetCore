using Newtonsoft.Json;
using System.ComponentModel;

namespace Perpetuum
{
    public class EPConfiguration
    {
        [DefaultValue(1), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int EPMultiplier { get; set; }


        [DefaultValue(1), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int EPDistributionPerDay { get; set; }
        [DefaultValue(1440), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int EPBasePointPerCycle { get; set; }
        [DefaultValue(2), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int EPBonusPointMultiplier { get; set; }

        [DefaultValue(false), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsEpCycleUseMultiply { get; set; }


        [DefaultValue(true), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool EPCycleTimeEnable { get; set; }
        [DefaultValue(8), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int EPCycleFromTime { get; set; }
        [DefaultValue(11), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int EPCycleToTime { get; set; }
    }
}
