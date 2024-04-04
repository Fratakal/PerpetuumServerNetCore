using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Items;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.Looting
{
    public class LootService : ILootService
    {
        private ILookup<int, LootGeneratorItemInfo> _npcLootInfos;
        private ILookup<int, LootGeneratorItemInfo> _flockLootInfos;
        private IntrusionLootInfo[] _intrusionLootInfos;
        private readonly GlobalConfiguration _globalConfiguration;

        public LootService(GlobalConfiguration configuration)
        {
            _globalConfiguration = configuration;
        }

        public void Init()
        {
            _npcLootInfos = LoadNpcLootInfosFromDb(_globalConfiguration.LootConfiguration);
            _flockLootInfos = LoadFlockLootInfosFromDb(_globalConfiguration.LootConfiguration);
            _intrusionLootInfos = LoadIntrusionLootInfos(_globalConfiguration.LootConfiguration);
        }

        public IEnumerable<LootGeneratorItemInfo> GetNpcLootInfos(int definition)
        {
            return _npcLootInfos.GetOrEmpty(definition);
        }

        public IEnumerable<LootGeneratorItemInfo> GetFlockLootInfos(int flockID)
        {
            return _flockLootInfos.GetOrEmpty(flockID);
        }

        public IEnumerable<LootGeneratorItemInfo> GetIntrusionLootInfos(Outpost outpost,SAP sap)
        {
            var stability = outpost.GetIntrusionSiteInfo().Stability;
            var loots = _intrusionLootInfos.Where(i => i.siteDefinition == outpost.Definition && 
                                                       i.sapDefinition == sap.Definition && 
                                                       i.stabilityThreshold.Min <= stability && i.stabilityThreshold.Max >= stability);

            foreach (var loot in loots)
            {
                var item = new ItemInfo(loot.itemDefinition, loot.quantity.Min, loot.quantity.Max); //roll random on init
                yield return new LootGeneratorItemInfo(item, false, loot.probability);
            }
        }

        private static ILookup<int, LootGeneratorItemInfo> LoadNpcLootInfosFromDb(LootConfiguration lootConfiguration)
        {
            return Db.Query().CommandText("select * from npcloot").Execute().Select(r =>
            {
                return new
                {
                    definition = r.GetValue<int>("definition"),
                    info = CreateNpcLootInfoFromRecord(r, lootConfiguration)
                };
            }).ToLookup(i => i.definition, i => i.info);
        }

        private static ILookup<int, LootGeneratorItemInfo> LoadFlockLootInfosFromDb(LootConfiguration lootConfiguration)
        {
            return Db.Query().CommandText("select * from npcflockloot").Execute().Select(r =>
            {
                return new
                {
                    flockId = r.GetValue<int>("flockid"),
                    info = CreateNpcLootInfoFromRecord(r, lootConfiguration)
                };
            }).ToLookup(i => i.flockId, i => i.info);
        }

        private static LootGeneratorItemInfo CreateNpcLootInfoFromRecord(IDataRecord record, LootConfiguration lootConfiguration)
        {
            var definition = record.GetValue<int>(k.lootDefinition.ToLower());
            int minq = record.GetValue<int>("minquantity");
            int maxq = record.GetValue<int>(k.quantity);

            if (maxq > 1 || (maxq == 1 && lootConfiguration.LootOverrideMaxValueOfOne))
            {
                minq = minq * lootConfiguration.LootQuantityMultiplier;
                maxq = maxq * lootConfiguration.LootQuantityMultiplier;
            }

            var item = new ItemInfo(definition, minq, maxq)
            {
                IsRepackaged = record.GetValue<bool>(k.repackaged)
            };

            var damageit = !record.GetValue<bool>(k.dontdamage);
            var damaged = false;

            if (!item.EntityDefault.AttributeFlags.Repackable)
            {
                //force false
                item.IsRepackaged = false;
                damageit = false;
            }

            //is it forced to be repacked from config AND damageable? 
            if (!item.IsRepackaged && damageit)
            {
                //no, so damage it!
                damaged = true;
            }

            double probabilityBase = record.GetValue<double>(k.probability);
            double probabilityMult = probabilityBase * Convert.ToDouble(lootConfiguration.LootProbabilityMultiplier);
            return new LootGeneratorItemInfo(item, damaged, probabilityMult > 1 ? 1 : probabilityMult);
        }

        private class IntrusionLootInfo
        {
            public readonly int siteDefinition;
            public readonly int sapDefinition;
            public readonly int itemDefinition;
            public readonly IntRange quantity;
            public readonly IntRange stabilityThreshold;
            public readonly double probability;

            public IntrusionLootInfo(int siteDefinition, int sapDefinition, int itemDefinition, IntRange quantity, IntRange stabilityThreshold, double probability)
            {
                this.siteDefinition = siteDefinition;
                this.sapDefinition = sapDefinition;
                this.itemDefinition = itemDefinition;
                this.quantity = quantity;
                this.stabilityThreshold = stabilityThreshold;
                this.probability = probability;
            }
        }

        private IntrusionLootInfo[] LoadIntrusionLootInfos(LootConfiguration lootConfiguration)
        {
            var x = Db.Query().CommandText("select * from intrusionloot").Execute().Select(r =>
            {
                int minq = r.GetValue<int>("minquantity");
                int maxq = r.GetValue<int>("maxquantity");
                double probabilityBase = r.GetValue<double>(k.probability);
                double probabilityMult = probabilityBase * Convert.ToDouble(lootConfiguration.LootProbabilityMultiplier);

                if (maxq > 1 || (maxq == 1 && lootConfiguration.LootOverrideMaxValueOfOne))
                {
                    minq = minq * lootConfiguration.LootQuantityMultiplier;
                    maxq = maxq * lootConfiguration.LootQuantityMultiplier;
                }
                var siteDefinition = r.GetValue<int>("sitedefinition");
                var sapDefinition = r.GetValue<int>("sapdefinition");
                var itemDefinition = r.GetValue<int>("itemdefinition");
                var quantity = new IntRange(minq, maxq);
                var stabilityThreshold = new IntRange(r.GetValue<int>("minstabilitythreshold"), r.GetValue<int>("maxstabilitythreshold"));
                var probability = probabilityMult > 1 ? 1 : probabilityMult;

                return new IntrusionLootInfo(siteDefinition,sapDefinition,itemDefinition,quantity,stabilityThreshold,probability);
            }).ToArray();

            return x;
        }
    }
}