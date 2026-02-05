In this episode of Keep Your Equipment™ mod series:
- Keep what you had at the start of the raid (wow)
- Preserve item state: anything worn, consumed, or lost stays that way after death
- Handle insurance, transfers and the mail service
- Configuration toggles for most features

## Tabs {.tabset}

### Description

An SPT mod that provides a configurable softcore implementation with the goal of delivering a more balanced experience.
If you’ve ever noticed the inconsistency where softcore implementations restore all your spent ammo, meds, and gear durability, yet your character still needs to heal and satisfy thirst and hunger, this mod is for you. It aims to provide a more consistent sense of progression by also tracking the state of your equipment - what items you consumed or broke.

You will need to repair your weapons and armor, replace meds and food, and refill ammo after both successful and failed raids.

### Installation

Drag and drop the contents of the downloaded ZIP to the SPT root folder, just like other mods.
![instructions](https://i.imgur.com/34vXXDj.gif)

### Configuration

The default configuration assumes that item state should always be preserved after a raid. You are free to configure it to behave almost like a full rollback (item-wise) if you want though. Changes to lostondeath.json are not supported, as they may break insurance handling in some cases.

***

**restoreLostItems**: If disabled, items that you brought into the raid but lost or consumed entirely will not be restored. Examples include consumed food, meds, spent ammo, a backpack dropped to take a better one etc. Disabled by default.

**restoreItemCondition**: If disabled, item wear accumulated during the raid will not be restored. For example, if you died from a chest shot, your chest plate will remain damaged after restoration. Weapons will also eventually need repairs even if you never survive raids. Disabled by default.

**restoreOnlyLostOnDeathSlots**: If enabled, only items from lost-on-death slots (defined by `SPT\SPT_Data\configs\lostondeath.json`) are processed. Your secure container, special slots, melee and similar are unaffected by the mod. Enabled by default.

**updateOnTransfer**: If enabled, the saved inventory is updated on transfer. Disabled by default.

**restoreInsurance**: If enabled, insurance is restored on items returned via this mod's restoration process. Enabled by default.

**restoreFoundInRaid**: If enabled, the Found in Raid flag is restored on items returned via this mod's restoration process. Disabled by default.

### How this works

When a raid ends, the mod has access to your pre-raid inventory, your post-raid inventory, and a list of transferred and lost insured items.

Pre-raid inventory items are updated with their post-raid state if they still exist. Items that were lost are removed from the pre-raid inventory. Items are compared by their unique IDs.

Items in safe slots are excluded from this process. Transferred items are not returned, even if the config option is on. Insured items that are restored by the mod do not go through the insurance system.

This creates several edge cases where the mod is unable to place an item back into your pre-raid inventory. In such cases, the item is sent via the game's mail service (a message from SYSTEM).

As a bonus, the mod fixes an existing SPT issue where insured items thrown directly out of the secure container are not returned via insurance.

***

This is a server-only mod.

### Compatibility

- Mods that modify raid-end behavior are likely incompatible
- Fika compatibility unknown
- SVM is compatible, but Softcore and Safe Exit must be disabled to avoid issues
- lostondeath.json wipeOnRaidStart=true prevents the mod from working because the mod does not get the pre-raid inventory, keep set to false
- lostondeath.json individual slot changes will work, but some setups can allow item duplication via insurance, either don't change it or don't use insurance
- MergeConsumables is compatible: if you restore the condition of a starting item with some other item (by dragging and dropping it onto the starting item) during a raid and then die, it will return correctly, with its condition depending on whether you had restoreCondition enabled. However, if you fully consume that item to restore the condition of another item, it will be treated as lost.

### Known issues

- **restoreLostItems=false** Splitting item stacks in raid causes the split portion to be treated as lost and not returned, even if it was still in your inventory on death
- **restoreLostItems=false** Removing ammo from a magazine is treated as a loss for the same reason: stack splitting creates a new item with a new ID
- **restoreInsurance=true, restoreLost=false** Items returned via the mail service (not through insurance) will not retain insurance

***

If you encounter something that is not on this list, report it [here](https://github.com/gorecreek/FER/issues)

{.endtabset}