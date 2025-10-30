using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MarketDay.API;
using MarketDay.ItemPriceAndStock;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace MarketDay.Utility
{
    /// <summary>
    /// This class contains static utility methods used to handle items
    /// </summary>
    public static class ItemsUtil
    {

        /// <summary>
        /// Given and ItemInventoryAndStock, and a maximum number, randomly reduce the stock until it hits that number
        /// </summary>
        /// <param name="inventory">the ItemPriceAndStock</param>
        /// <param name="maxNum">The maximum number of items we want for this stock</param>
        public static void RandomizeStock(Dictionary<ISalable, ItemStockInformation> inventory, int maxNum)
        {
            while (inventory.Count > maxNum)
            {
                inventory.Remove(inventory.Keys.ElementAt(Game1.random.Next(inventory.Count)));
            }
        }

        internal static bool Equal(ISalable a, ISalable b)
        {
            if (a is null || b is null) return false;

            switch (a)
            {
                case Hat aHat when b is Hat bHat:
                    return aHat.ItemId == bHat.ItemId;
                case Tool aTool when b is Tool bTool:  // includes weapons
                    return aTool.InitialParentTileIndex == bTool.InitialParentTileIndex;
                case Boots aBoots when b is Boots bBoots:
                    return aBoots.indexInTileSheet == bBoots.indexInTileSheet;
                case Item aItem when b is Item bItem:
                    return aItem.QualifiedItemId.Equals(bItem.QualifiedItemId);
            }

            if (a is not Item) MarketDay.Log($"Equal: {a.Name} not an item", LogLevel.Warn);
            if (b is not Item) MarketDay.Log($"Equal: {b.Name} not an item", LogLevel.Warn);
            return a.Name == b.Name;
        }

        /// <summary>
        /// Get the itemID given a name and the object information that item belongs to
        /// </summary>
        /// <param name="name">name of the item</param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static string GetIndexByName(string name, string itemType = "Object")
        {
            switch (itemType)
            {
                case "Object":
                    foreach (var (index, objectData) in DataLoader.Objects(Game1.content)) { if (objectData?.Name?.Equals(name) == true) { return index; } } return "-1";
                case "BigCraftable":
                    foreach (var (index, objectData) in DataLoader.BigCraftables(Game1.content)) { if (objectData?.Name?.Equals(name) == true) { return index; } } return "-1";
                case "Shirt":
                    foreach (var (index, objectData) in DataLoader.Shirts(Game1.content)) { if (objectData?.Name?.Equals(name) == true) { return index; } } return "-1";
                case "Pants":
                    foreach (var (index, objectData) in DataLoader.Pants(Game1.content)) { if (objectData?.Name?.Equals(name) == true) { return index; } } return "-1";
                case "Ring":
                    foreach (var (index, objectData) in DataLoader.Objects(Game1.content)) { if (objectData?.Name?.Equals(name) == true) { return index; } } return "-1";
                case "Hat":
                    foreach (var (index, objectData) in DataLoader.Hats(Game1.content)) { if (objectData?.Split('/')?.ElementAtOrDefault(0)?.Equals(name) == true) { return index; } } return "-1";
                case "Boot":
                    foreach (var (index, objectData) in DataLoader.Boots(Game1.content)) { if (objectData?.Split('/')?.ElementAtOrDefault(0)?.Equals(name) == true) { return index; } } return "-1";
                case "Furniture":
                    foreach (var (index, objectData) in DataLoader.Furniture(Game1.content)) { if (objectData?.Split('/')?.ElementAtOrDefault(0)?.Equals(name) == true) { return index; } } return "-1";
                case "Weapon":
                    foreach (var (index, objectData) in DataLoader.Weapons(Game1.content)) { if (objectData?.Name?.Equals(name) == true) { return index; } } return "-1";
            }

            return "-1";
        }

        private static readonly Dictionary<int, string[]> ObjectCategories = new()
        {
            { SObject.artisanGoodsCategory, new[]{ "Artisan Good", "Artisan Goods" } },
            { SObject.baitCategory, new[]{ "Bait" } } ,
            //{ SObject.BigCraftableCategory, new[]{ "Big Craftable" } } ,
            { SObject.booksCategory, new[]{ "Book" } } ,
            //{ SObject.bootsCategory, new[]{ "Boots" } } ,
            //{ SObject.clothingCategory, new[]{ "Clothing" } } ,
            { SObject.CookingCategory, new[]{ "Cooking" } } ,
            { SObject.CraftingCategory, new[]{ "Crafting" } } ,
            { SObject.EggCategory, new[]{ "Egg" } } ,
            //{ SObject.equipmentCategory, new[]{ "Equipment" } } ,
            { SObject.fertilizerCategory, new[]{ "Fertilizer" } } ,
            { SObject.FishCategory, new[]{ "Fish" } } ,
            { SObject.flowersCategory, new[]{ "Flower" } } ,
            { SObject.FruitsCategory, new[]{ "Fruit" } } ,
            { SObject.furnitureCategory, Array.Empty<string>() } , // "Flooring"
            { SObject.GemCategory, new[]{ "Gem" } } ,
            { SObject.GreensCategory, new[]{ "Greens", "Forage" }  },
            { SObject.hatCategory, new[]{ "Hat" } } ,
            { SObject.ingredientsCategory, new[]{ "Ingredient" } } ,
            { SObject.junkCategory, new[]{ "Junk" } } ,
            { SObject.litterCategory, new[]{ "Litter" }  },
            { SObject.meatCategory, new[]{ "Meat" } } ,
            { SObject.MilkCategory, new[]{ "Milk" } } ,
            { SObject.mineralsCategory, new[]{ "Mineral" } } ,
            { SObject.monsterLootCategory, new[]{ "Monster Loot" } } ,
            { SObject.ringCategory, new[]{ "Ring" } } ,
            { SObject.SeedsCategory, new[]{ "Seed" } } ,
            { SObject.sellAtFishShopCategory, new[]{ "Sell at Fish Shop" } } ,
            { SObject.skillBooksCategory, new[]{ "Skill Book" } } ,
            { SObject.syrupCategory, new[]{ "Syrup" } } ,
            { SObject.tackleCategory, new[]{ "Tackle" } } ,
            //{ SObject.toolCategory, new[]{ "Tool" } } ,
            //{ SObject.trinketCategory, new[]{ "Trinket" } } ,
            { SObject.VegetableCategory, new[]{ "Vegetable" } } ,
            //{ SObject.weaponCategory, new[]{ "Weapon" } } ,
            { 0, new[]{ "Arch", "Artifact" } }
        };

        /// <summary>
        /// Get the itemID given a category and the object information that item belongs to
        /// </summary>
        /// <param name="needle">pattern to search for</param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static string GetIndexByCategory(string needle, string itemType = "Object")
        {
            var candidates = new List<string>();
            //MarketDay.Log($"{itemType}/{needle}", LogLevel.Info);
            switch (itemType)
            {
                case "Object":
                    var match = ObjectCategories
                        .Where(c => c.Key.ToString().Equals(needle) || c.Value.Any(v => v.Equals(needle, StringComparison.OrdinalIgnoreCase)))
                        .FirstOrDefault();
                    foreach (var (index, objectData) in DataLoader.Objects(Game1.content)) {
                        if (match.Value?.Any(v => v.Equals(objectData?.Type)) == true
                            || (objectData?.Category.Equals(match.Key) == true && match.Key != 0)
                        ) {
                            candidates.Add(index);
                        }
                    }
                    break;
                case "BigCraftable":
                    foreach (var (index, _) in DataLoader.BigCraftables(Game1.content)) { candidates.Add(index); }
                    break;
                case "Shirt":
                    foreach (var (index, _) in DataLoader.Shirts(Game1.content)) { candidates.Add(index); }
                    break;
                case "Pants":
                    foreach (var (index, _) in DataLoader.Pants(Game1.content)) { candidates.Add(index); }
                    break;
                case "Ring":
                    foreach (var (index, objectData) in DataLoader.Objects(Game1.content)) {
                        if (objectData?.Category == 0 && objectData?.Type?.Equals(itemType) == true) {
                            candidates.Add(index);
                        }
                    }
                    break;
                case "Hat":
                    foreach (var (index, _) in DataLoader.Hats(Game1.content)) { candidates.Add(index); }
                    break;
                case "Boot":
                    foreach (var (index, _) in DataLoader.Boots(Game1.content)) { candidates.Add(index); }
                    break;
                case "Furniture":
                    foreach (var (index, _) in DataLoader.Furniture(Game1.content)) { candidates.Add(index); }
                    break;
                case "Weapon":
                    foreach (var (index, _) in DataLoader.Weapons(Game1.content)) { candidates.Add(index); }
                    break;
            }
            return candidates.Any() ? candidates[Game1.random.Next(candidates.Count)] : "-1";
        }

        /// <summary>
        /// Get the itemID given a pattern and the object information that item belongs to
        /// </summary>
        /// <param name="needle">pattern to search for</param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static string GetIndexByMatch(string needle, string itemType = "Object")
        {
            var candidates = new List<string>();
            switch (itemType)
            {
                case "Object":
                    foreach (var (index, objectData) in DataLoader.Objects(Game1.content)) { if (objectData?.Name?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "BigCraftable":
                    foreach (var (index, objectData) in DataLoader.BigCraftables(Game1.content)) { if (objectData?.Name?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Shirt":
                    foreach (var (index, objectData) in DataLoader.Shirts(Game1.content)) { if (objectData?.Name?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Pants":
                    foreach (var (index, objectData) in DataLoader.Pants(Game1.content)) { if (objectData?.Name?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Ring":
                    foreach (var (index, objectData) in DataLoader.Objects(Game1.content)) { if (objectData?.Name?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Hat":
                    foreach (var (index, objectData) in DataLoader.Hats(Game1.content)) { if (objectData?.Split('/')?.ElementAtOrDefault(0)?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Boot":
                    foreach (var (index, objectData) in DataLoader.Boots(Game1.content)) { if (objectData?.Split('/')?.ElementAtOrDefault(0)?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Furniture":
                    foreach (var (index, objectData) in DataLoader.Furniture(Game1.content)) { if (objectData?.Split('/')?.ElementAtOrDefault(0)?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
                case "Weapon":
                    foreach (var (index, objectData) in DataLoader.Weapons(Game1.content)) { if (objectData?.Name?.Contains(needle) == true) { candidates.Add(index); } }
                    break;
            }
            return candidates.Any() ? candidates[Game1.random.Next(candidates.Count)] : "-1";
        }

        private static readonly string[] ValidTypes = new[] { "Object", "BigCraftable", "Shirt", "Pants", "Ring", "Hat", "Boot", "Furniture", "Weapon" };

        /// <summary>
        /// Checks if an itemtype is valid
        /// </summary>
        /// <param name="itemType">The name of the itemtype</param>
        /// <returns>True if it's a valid type, false if not</returns>
        public static bool CheckItemType(string itemType)
        {
            return ValidTypes.Any(i => string.Compare(i, itemType, true) == 0);
        }

        public static void RemoveSoldOutItems(Dictionary<ISalable, ItemStockInformation> stock)
        {
            List<ISalable> keysToRemove = stock.Where(kvp => kvp.Value.Stock < 1).Select(kvp => kvp.Key).ToList();
            foreach (ISalable item in keysToRemove)
                stock.Remove(item);
        }

        public static bool IsInSeasonCrop(string itemId)
        {
            if (Game1.cropData.TryGetValue(itemId, out var cd))
            {
                return cd.Seasons.Contains(Game1.season);
            }

            if (Game1.fruitTreeData.TryGetValue(itemId, out var fd))
            {
                return fd.Seasons.Contains(Game1.season);
            }

            return false;
        }
    }
}