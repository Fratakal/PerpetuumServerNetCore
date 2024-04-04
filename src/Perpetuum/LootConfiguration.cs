using Newtonsoft.Json;
using System.ComponentModel;

namespace Perpetuum
{
    public class LootConfiguration
    {
        [DefaultValue(false), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool LootOverrideMaxValueOfOne { get; set; }

        [DefaultValue(1), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int LootQuantityMultiplier { get; set; }

        [DefaultValue(1), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int LootProbabilityMultiplier { get; set; }

    }
}
