using FairEquipmentRestoration.Config;
using FairEquipmentRestoration.Extensions;
using Microsoft.Extensions.Logging;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using System.Text;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class ItemLogHelper(
        ISptLogger<ItemLogHelper> logger,
        TemplateTable templateTable,
        FairEquipmentRestorationConfig config)
    {
        public void LogItems(LogLevel level, IReadOnlyList<Item>? items, IReadOnlyList<Item>? inventoryItems, string? message = null)
        {
            if (!config.EnableItemDebugLogging)
            {
                return;
            }

            if (items is null)
            {
                LogInvalid(level, "items", message);
                return;
            }

            if (message is not null)
            {
                logger.Log(level, message);
            }

            foreach (var item in items)
            {
                LogItem(level, item, inventoryItems);
            }
        }

        public void LogInsuredItems(LogLevel level, IReadOnlyList<InsuredItem>? items, IReadOnlyList<Item>? inventoryItems, string? message = null)
        {
            if (!config.EnableItemDebugLogging)
            {
                return;
            }

            if (items is null)
            {
                LogInvalid(level, "items", message);
                return;
            }

            if (inventoryItems is null)
            {
                LogInvalid(level, "inventory", message);
                return;
            }

            if (message is not null)
            {
                logger.Log(level, message);
            }

            foreach (var insuredItem in items)
            {
                var item = inventoryItems.FirstOrDefault(x => x.Id == insuredItem.ItemId);
                if (item is not null)
                {
                    LogItem(level, item, inventoryItems);
                }
                else
                {
                    if (insuredItem.ItemId is null)
                    {
                        continue;
                    }

                    logger.Log(level, insuredItem.ItemId);
                }
            }
        }

        public void LogItem(LogLevel level, Item? item, IReadOnlyList<Item>? inventoryItems, string? message = null)
        {
            if (!config.EnableItemDebugLogging)
            {
                return;
            }

            if (item is null)
            {
                LogInvalid(level, "item", message);
                return;
            }

            if (inventoryItems is null)
            {
                LogInvalid(level, "inventory", message);
                return;
            }

            var dbItems = templateTable.Items;
            var template = dbItems.GetValueOrDefault(item.Template);
            var parentTemplate = GetItemTemplate(item.ParentId, inventoryItems, dbItems);

            Log(level, item, template, parentTemplate, message);
        }

        public void LogInventory(LogLevel level, PmcData? profile, bool logAllItems = false, string? message = null)
        {
            if (!config.EnableItemDebugLogging)
            {
                return;
            }

            List<Item>? inventoryItems;
            if (!logAllItems)
            {
                inventoryItems = profile.GetEquipmentAndQuestItems();
            }
            else
            {
                inventoryItems = profile?.Inventory?.Items;
            }

            if (inventoryItems is null)
            {
                LogInvalid(level, "inventory", message);
                return;
            }

            if (message is not null)
            {
                logger.Log(level, message);
            }

            var dbItems = templateTable.Items;
            foreach (var item in inventoryItems)
            {
                var template = dbItems.GetValueOrDefault(item.Template);
                var parentTemplate = GetItemTemplate(item.ParentId, inventoryItems, dbItems);

                Log(level, item, template, parentTemplate);
            }
        }

        private static TemplateItem? GetItemTemplate(
            string? idString,
            IReadOnlyList<Item> inventoryItems,
            Dictionary<MongoId, TemplateItem> items)
        {
            if (idString is null)
            {
                return null;
            }

            MongoId id = new(idString);
            var item = inventoryItems.FirstOrDefault(x => x.Id == id);
            if (item is null)
            {
                return null;
            }

            return items.GetValueOrDefault(item.Template);
        }

        private void Log(LogLevel level, Item item, TemplateItem? itemTemplate, TemplateItem? parentTemplate, string? message = null)
        {
            var sb = new StringBuilder();

            if (message is not null)
            {
                sb.Append($"{message} ");
            }

            sb.Append(item.Id);
            if (itemTemplate is not null)
            {
                sb.Append($" {itemTemplate.Name}");
            }

            sb.Append($", parent = {item.ParentId}");
            if (parentTemplate is not null)
            {
                sb.Append($" {parentTemplate.Name}");
            }

            sb.Append($", slot = {item.SlotId}");

            if (item.Upd?.StackObjectsCount is not null)
            {
                sb.Append($", stack = {item.Upd.StackObjectsCount}");
            }

            if (item.Upd?.SpawnedInSession is not null)
            {
                sb.Append($", FiR = {item.Upd.SpawnedInSession}");
            }

            logger.Log(level, sb.ToString());
        }

        private void LogInvalid(LogLevel level, string entity, string? message = null)
        {
            if (message is not null)
            {
                logger.Log(level, $"{message} invalid {entity}");
            }
            else
            {
                logger.Log(level, $"Invalid {entity}");
            }
        }
    }
}
