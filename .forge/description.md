In this episode of Keep Your Equipment™ mod series:
- Keep the gear you had at the start of the raid (wow)
- Preserve item state: anything worn, consumed, or lost stays that way after death
- Handle insurance, transfers and the mail service
- Configurable behavior for secure slots
- Configuration toggles for most other mod features

## Tabs {.tabset}

### Description

An SPT mod that provides a configurable equipment restoration mechanics with the goal of delivering a more balanced experience. 

In vanilla, you lose most of your equipment on death or MIA. You also have to heal your wounds and satisfy thirst and hunger regardless of the outcome of the raid. This mod applies the same concept to your equipment: it is not lost, but always has to be taken care of, which should give you a sense of consistent progression.

Put simply, you will need to repair your weapons and armor, replace meds and food, and refill ammo after both successful and failed raids.

### Installation

Drag and drop the contents of the downloaded ZIP to the SPT root folder, just like other mods.
![instructions](https://i.imgur.com/34vXXDj.gif)

### FAQ

**Q**: I went in raid, found some equipment better than I had on me and dropped the old piece. Then I died and my old equipment was not restored.

**A**: This is the default behavior that can be changed by setting **restoreLostItems** to true. The items you dropped are treated as lost/consumed and are not restored. Read more in **How this works** and **Configuration** sections.

***

**Q**: Is this mod compatible with Fika?

**A**: There are reports of them working well together, so you can try it yourself. Make sure to let me know how it works!

***

**Q**: How is this different from other equipment restoration mods or Softcore option in SVM?

**A**: Usually, they restore your inventory to how it was before you started the raid. This mod does that too, but also remembers what happened to your gear during the raid and applies these changes. SVM option just causes the game not to strip your items on death, so you keep all found stuff too. 

***

**Q**: Can I trust this mod with my stuff?

**A**: I played through the whole game with this mod and around 30 other mods on SPT 4.0 and haven't noticed a single issue. Your use case might be different, so make sure to backup your profile and do a test run.

### Configuration

The default configuration assumes that item state should always be preserved after a raid. Secure slots are not affected. You are free to configure it to behave almost like a full rollback (item-wise) if you want though. Changes to lostondeath.json are not supported, as they may break insurance handling in some cases.

***

**restoreLostItems**: If disabled, items that you brought into the raid but lost or consumed entirely will not be restored. Examples include consumed food, meds, spent ammo, a backpack dropped to take a better one etc. Disabled by default.

**restoreItemCondition**: If disabled, item wear accumulated during the raid will not be restored. For example, if you died from a chest shot, your chest plate will remain damaged after restoration. Weapons will also eventually need repairs even if you never survive raids. Disabled by default.

**restoreOnlyLostOnDeathSlots**: If enabled, only items from lost-on-death slots (defined by `SPT_Runtime\SPT_Data\configs\lostondeath.json`) are processed. Your secure container, special slots, melee and similar are unaffected by the mod. Enabled by default.

**updateOnTransfer**: If enabled, the saved inventory is updated on transfer. Disabled by default.

**restoreInsurance**: If enabled, insurance is restored on items returned via this mod's restoration process. Enabled by default.

**restoreFoundInRaid**: If enabled, the Found in Raid flag is restored on items returned via this mod's restoration process. Disabled by default.

### How this works

When a raid ends, the mod has access to your pre-raid inventory, your post-raid inventory, and a list of transferred and lost insured items.

The pre-raid inventory is compared to the post-raid inventory using unique item IDs. If an item exists in both inventories, it's state in the pre-raid inventory is updated with it's state in the post-raid inventory (controlled by **restoreItemCondition** config option). If an item exists only in the pre-raid inventory, it is treated as lost and removed from the pre-raid inventory (controlled by **restoreLostItems** config option). Then your inventory is set to this updated pre-raid inventory.

Items in safe slots are excluded from this process. Transferred items are not returned, even if the config option is on. Insured items that are restored by the mod do not go through the insurance system.

This creates several edge cases where the mod is unable to place an item back into your pre-raid inventory. In such cases, the item is sent via the game's mail service (a message from SYSTEM).

Also note that the mod does not restore quest progress, so in-raid quest rewards from BTR driver or Lightkeeper will not be restored if you lose them.

As a bonus, the mod fixes an existing SPT issue where insured items thrown directly out of the secure container are not returned via insurance.

***

This is a server-only mod.

### Compatibility

- Mods that modify raid-end behavior are likely incompatible
- Judging by user reports, Fika appears to be compatible. Some weird situations can happen, though: if a bot or someone else loots your stuff, it will not be restored (when using restoreLostItems=false), because the server will not see these items in your post-raid inventory. In singleplayer the raid end server call happens right after your character's death and nobody can snatch your items between these events.
- SVM is compatible, but Softcore and Safe Exit must be disabled to avoid issues
- lostondeath.json wipeOnRaidStart=true prevents the mod from working because the mod does not get the pre-raid inventory, keep set to false
- lostondeath.json individual slot changes will work, but some setups can allow item duplication via insurance, either don't change it or don't use insurance
- MergeConsumables is compatible: if you restore the condition of a starting item with some other item (by dragging and dropping it onto the starting item) during a raid and then die, it will return correctly, with its condition depending on whether you had restoreCondition enabled. However, if you fully consume that item to restore the condition of another item, it will be treated as lost.

### Known issues

- **restoreLostItems=false** Splitting item stacks in raid causes the split portion to be treated as lost and not returned, even if it was still in your inventory on death
- **restoreLostItems=false** Removing ammo from a magazine is treated as a loss for the same reason: stack splitting creates a new item with a new ID
- **restoreInsurance=true, restoreLostItems=false** Items returned via the mail service (not through insurance) will not retain insurance
- **[1.1.0]** **restoreLostItems=false** Ammo in mags sometimes returns in pre-raid quantities

***

If you encounter something that is not on this list, report it [here](https://github.com/gorecreek/FER/issues)

{.endtabset}