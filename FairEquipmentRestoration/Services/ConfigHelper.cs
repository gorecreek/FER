using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
#pragma warning disable CS0618 // Type or member is obsolete
    public class ConfigHelper(ConfigServer configServer, ICloner cloner)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public LostOnDeathConfig GetInvertedLostOnDeathConfig()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var config = configServer.GetConfig<LostOnDeathConfig>();
#pragma warning restore CS0618 // Type or member is obsolete
            var newConfig = cloner.Clone(config)!;

            newConfig.SpecialSlotItems = !config.SpecialSlotItems;
            newConfig.QuestItems = !config.QuestItems;

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
