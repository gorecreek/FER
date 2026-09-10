using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils.Cloners;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class ConfigHelper(LostOnDeathConfig lostOnDeathConfig, ICloner cloner)
    {
        public LostOnDeathConfig GetInvertedLostOnDeathConfig()
        {
            var newConfig = cloner.Clone(lostOnDeathConfig)!;

            newConfig.SpecialSlotItems = !lostOnDeathConfig.SpecialSlotItems;
            newConfig.QuestItems = !lostOnDeathConfig.QuestItems;

            InvertLostEquipment(newConfig.Equipment);

            return newConfig;
        }

        private static void InvertLostEquipment(LostEquipment config)
        {
            var traverse = Traverse.Create(config);
            foreach (var propName in traverse.Properties())
            {
                var prop = traverse.Property(propName);

                if (prop.GetValueType() == typeof(bool) && prop.IsWriteable)
                {
                    var current = prop.GetValue<bool>();
                    prop.SetValue(!current);
                }
            }
        }
    }
}
