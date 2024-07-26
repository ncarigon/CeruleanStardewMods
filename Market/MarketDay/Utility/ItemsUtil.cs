﻿using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using MarketDay.API;
using StardewValley.Objects;

namespace MarketDay.Utility
{
    /// <summary>
    /// This class contains static utility methods used to handle items
    /// </summary>
    public static class ItemsUtil
    {
        // public static Dictionary<string, IDictionary<string, string>> ObjectInfoSource { get; set; }
        public static Dictionary<string, StardewValley.GameData.Objects.ObjectData> ObjectInfoSourceObject { get; set; }
        public static Dictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData> ObjectInfoSourceBigCraftable { get; set; }
        public static Dictionary<string, StardewValley.GameData.Shirts.ShirtData> ObjectInfoSourceShirt { get; set; }
        public static Dictionary<string, StardewValley.GameData.Pants.PantsData> ObjectInfoSourcePants { get; set; }
        public static Dictionary<string, StardewValley.GameData.Objects.ObjectData> ObjectInfoSourceRing { get; set; }
        public static Dictionary<string, string> ObjectInfoSourceHat { get; set; }
        public static Dictionary<string, string> ObjectInfoSourceBoot { get; set; }
        public static Dictionary<string, string> ObjectInfoSourceFurniture { get; set; }
        public static Dictionary<string, StardewValley.GameData.Weapons.WeaponData> ObjectInfoSourceWeapon { get; set; }

        public static List<string> RecipesList;
        private static Dictionary<string, StardewValley.GameData.FruitTrees.FruitTreeData> _fruitTreeData;
        private static Dictionary<string, StardewValley.GameData.Crops.CropData> _cropData;

        public static List<string> PacksToRemove = new List<string>();
        public static List<string> RecipePacksToRemove = new List<string>();
        public static List<string> ItemsToRemove = new List<string>();

        /// <summary>
        /// Loads up the onject information for all types, 
        /// done at the start of each save loaded so that JA info is up to date
        /// </summary>
        public static void UpdateObjectInfoSource()
        {
            //load up all the object information into a static dictionary

            ObjectInfoSourceObject = DataLoader.Objects(Game1.content);
            ObjectInfoSourceBigCraftable = DataLoader.BigCraftables(Game1.content);
            ObjectInfoSourceShirt = DataLoader.Shirts(Game1.content);
            ObjectInfoSourcePants = DataLoader.Pants(Game1.content);
            ObjectInfoSourceRing = DataLoader.Objects(Game1.content);
            ObjectInfoSourceHat = DataLoader.Hats(Game1.content);
            ObjectInfoSourceBoot = DataLoader.Boots(Game1.content);
            ObjectInfoSourceFurniture = DataLoader.Furniture(Game1.content);
            ObjectInfoSourceWeapon = DataLoader.Weapons(Game1.content);

            // ObjectInfoSource = new Dictionary<string, IDictionary<string, string>>
            // {
            //     {"Object", MarketDay.helper.GameContent.Load<Dictionary<string, string>>("Strings/Objects")},
            //     {"BigCraftable", MarketDay.helper.GameContent.Load<Dictionary<string, string>>("Strings/BigCraftables")},
            //     {"Shirt", MarketDay.helper.GameContent.Load<Dictionary<string, string>>("Strings/Shirts")},
            //     {"Pants", MarketDay.helper.GameContent.Load<Dictionary<string, string>>("Strings/Pants")},
            //     {"Ring", MarketDay.helper.GameContent.Load<Dictionary<string, string>>("Strings/Objects")},
            //     // {"Hat", MarketDay.helper.GameContent.Load<Dictionary<string, string>>(@"Strings/hats")},
            //     // {"Boot", MarketDay.helper.GameContent.Load<Dictionary<string, string>>(@"Strings/Boots")},
            //     {"Furniture", MarketDay.helper.GameContent.Load<Dictionary<string, string>>(@"Strings/Furniture")},
            //     {"Weapon", MarketDay.helper.GameContent.Load<Dictionary<string, string>>(@"Strings/weapons")}
            // };

            //load up recipe information
            RecipesList = MarketDay.helper.GameContent.Load<Dictionary<string, string>>(@"Data/CraftingRecipes").Keys
                .ToList();
            RecipesList.AddRange(MarketDay.helper.GameContent.Load<Dictionary<string, string>>(@"Data/CookingRecipes")
                .Keys.ToList());

            //add "recipe" to the end of every element
            RecipesList = RecipesList.Select(s => s + " Recipe").ToList();

            //load up tree and crop data
            _fruitTreeData = MarketDay.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.FruitTrees.FruitTreeData>>(@"Data/fruitTrees");
            _cropData = MarketDay.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.Crops.CropData>>(@"Data/Crops");
        }

        /// <summary>
        /// Given and ItemInventoryAndStock, and a maximum number, randomly reduce the stock until it hits that number
        /// </summary>
        /// <param name="inventory">the ItemPriceAndStock</param>
        /// <param name="maxNum">The maximum number of items we want for this stock</param>
        public static void RandomizeStock(Dictionary<ISalable, int[]> inventory, int maxNum)
        {
            while (inventory.Count > maxNum)
            {
                inventory.Remove(inventory.Keys.ElementAt(Game1.random.Next(inventory.Count)));
            }
        }

        internal static bool Equal(ISalable a, ISalable b)
        {
            if (a is null || b is null) return false;

            var dgaApi = APIs.dgaApi.Value;
            if (dgaApi is not null)
            {
                var aID = dgaApi.GetDGAItemId(a);
                var bID = dgaApi.GetDGAItemId(b);
                if (aID is not null && bID is not null)
                {
                    return aID == bID;
                }
            }

            switch (a)
            {
                case Hat aHat when b is Hat bHat:
                    return aHat.obsolete_which.Value == bHat.obsolete_which.Value;
                case Tool aTool when b is Tool bTool:  // includes weapons
                    return aTool.InitialParentTileIndex == bTool.InitialParentTileIndex;
                case Boots aBoots when b is Boots bBoots:
                    return aBoots.indexInTileSheet == bBoots.indexInTileSheet;
                case Item aItem when b is Item bItem:
                {
                    if (aItem.ParentSheetIndex > -1 && bItem.ParentSheetIndex > -1)
                    {
                        return aItem.ParentSheetIndex == bItem.ParentSheetIndex && aItem.Category == bItem.Category;
                    }
                    break;
                }
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
                    foreach (var (index, objectData) in ObjectInfoSourceObject){ if (objectData != null && objectData.Name == name) { return index; } } return "-1";
                case "BigCraftable":
                    foreach (var (index, objectData) in ObjectInfoSourceBigCraftable){ if (objectData != null && objectData.Name == name) { return index; } } return "-1";
                case "Shirt":
                    foreach (var (index, objectData) in ObjectInfoSourceShirt){ if (objectData != null && objectData.Name == name) { return index; } } return "-1";
                case "Pants":
                    foreach (var (index, objectData) in ObjectInfoSourcePants){ if (objectData != null && objectData.Name == name) { return index; } } return "-1";
                case "Ring":
                    foreach (var (index, objectData) in ObjectInfoSourceRing){ if (objectData != null && objectData.Name == name) { return index; } } return "-1";
                case "Hat":
                    foreach (var (index, objectData) in ObjectInfoSourceHat){ if (objectData != null && objectData.Split('/')[0] == name) { return index; } } return "-1";
                case "Boot":
                    foreach (var (index, objectData) in ObjectInfoSourceBoot){ if (objectData != null && objectData.Split('/')[0] == name) { return index; } } return "-1";
                case "Furniture":
                    foreach (var (index, objectData) in ObjectInfoSourceFurniture){ if (objectData != null && objectData.Split('/')[0] == name) { return index; } } return "-1";
                case "Weapon":
                    foreach (var (index, objectData) in ObjectInfoSourceWeapon){ if (objectData != null && objectData.Name == name) { return index; } } return "-1";
            }

            return "-1";
        }

        public static ISalable GetDGAObjectByName(string name, string itemType = "Object")
        {
            if (APIs.dgaApi.Value is not { } dgaApi)
            {
                MarketDay.Log($"{name}/{itemType}: could not get DGA API", LogLevel.Trace);
                return null;
            }

            var obj = dgaApi.SpawnDGAItem(name);
            switch (obj)
            {
                case null:
                    MarketDay.Log($"{name}/{itemType}: not a DGA object", LogLevel.Trace);
                    return null;
                case ISalable item:
                    return item;
                default:
                    MarketDay.Log($"{name}/{itemType}: not a saleable object", LogLevel.Trace);
                    return null;
            }
        }

        /// <summary>
        /// Get the itemID given a category and the object information that item belongs to
        /// </summary>
        /// <param name="needle">pattern to search for</param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static string GetIndexByCategory(string needle, string itemType = "Object")
        {
            var candidates = new List<string>();
            MarketDay.Log($"{itemType}/{needle}", LogLevel.Info);
            switch (itemType)
            {
                case "Object":
                    foreach (var (index, objectData) in ObjectInfoSourceObject){ if (objectData.Type.Contains(needle)){ candidates.Add(index); } }
                    break;
                // case "BigCraftable":
                //     foreach (var (index, objectData) in ObjectInfoSourceBigCraftable){ if (objectData.Type.Contains(needle)){ candidates.Add(index); } }
                //     break;
                // case "Shirt":
                //     foreach (var (index, objectData) in ObjectInfoSourceShirt){ if (objectData.Category.ToString().Contains(needle)){ candidates.Add(index); } }
                //     break;
                // case "Pants":
                //     foreach (var (index, objectData) in ObjectInfoSourcePants){ if (objectData.Category.ToString().Contains(needle)){ candidates.Add(index); } }
                //     break;
                case "Ring":
                    foreach (var (index, objectData) in ObjectInfoSourceRing){ if (objectData.Type.Contains(needle)){ candidates.Add(index); } }
                    break;
                case "Hat":
                    foreach (var (index, objectData) in ObjectInfoSourceHat){ if (objectData.Split('/')[3].Contains(needle)){ candidates.Add(index); } }
                    break;
                case "Boot":
                    foreach (var (index, objectData) in ObjectInfoSourceBoot){ if (objectData.Split('/')[3].Contains(needle)){ candidates.Add(index); } }
                    break;
                case "Furniture":
                    foreach (var (index, objectData) in ObjectInfoSourceFurniture){ if (objectData.Split('/')[3].Contains(needle)){ candidates.Add(index); } }
                    break;
                // case "Weapon":
                //     foreach (var (index, objectData) in ObjectInfoSourceWeapon){ if (objectData.Category.ToString().Contains(needle)){ candidates.Add(index); } }
                //     break;
            }

            if (candidates.Count > 0) {
                MarketDay.Log($"{candidates[0]}", LogLevel.Info);
                return candidates[Game1.random.Next(candidates.Count)].ToString();
            }
            return "-1";
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
                    foreach (var (index, objectData) in ObjectInfoSourceObject){ if (objectData.Name.Contains(needle)) { candidates.Add(index); } }
                    break;
                case "BigCraftable":
                    foreach (var (index, objectData) in ObjectInfoSourceBigCraftable){ if (objectData.Name.Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Shirt":
                    foreach (var (index, objectData) in ObjectInfoSourceShirt){ if (objectData.Name.Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Pants":
                    foreach (var (index, objectData) in ObjectInfoSourcePants){ if (objectData.Name.Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Ring":
                    foreach (var (index, objectData) in ObjectInfoSourceRing){ if (objectData.Name.Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Hat":
                    foreach (var (index, objectData) in ObjectInfoSourceHat){ if (objectData.Split('/')[0].Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Boot":
                    foreach (var (index, objectData) in ObjectInfoSourceBoot){ if (objectData.Split('/')[0].Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Furniture":
                    foreach (var (index, objectData) in ObjectInfoSourceFurniture){ if (objectData.Split('/')[0].Contains(needle)) { candidates.Add(index); } }
                    break;
                case "Weapon":
                    foreach (var (index, objectData) in ObjectInfoSourceWeapon){ if (objectData.Name.Contains(needle)) { candidates.Add(index); } }
                    break;
            }

            if (candidates.Count > 0) return candidates[Game1.random.Next(candidates.Count)].ToString();
            return "-1";
        }

        /// <summary>
        /// Checks if an itemtype is valid
        /// </summary>
        /// <param name="itemType">The name of the itemtype</param>
        /// <returns>True if it's a valid type, false if not</returns>
        public static bool CheckItemType(string itemType)
        {
            string searchString = "Object|BigCraftable|Shirt|Pants|Ring|Hat|Boot|Furniture|Weapon";

            return itemType == "Seed" || searchString.Contains(itemType);
        }

        /// <summary>
        /// Given the name of a crop, return the ID of its seed object
        /// </summary>
        /// <param name="cropName">The name of the crop object</param>
        /// <returns>The ID of the seed object if found, -1 if not</returns>
        // public static string GetSeedId(string cropName)
        // {
        //     //int cropID = MarketDay.JsonAssets.GetCropId(cropName);
        //     int cropId = APIs.JsonAssets.GetCropId(cropName);
        //     foreach (KeyValuePair<string, StardewValley.GameData.Crops.CropData> kvp in _cropData)
        //     {
        //         //find the tree id in crops information to get seed id
        //         Int32.TryParse(kvp.Value.ToString(), out int id);
        //         if (cropId == id)
        //             return kvp.Key;
        //     }

        //     return "-1";
        // }

        /// <summary>
        /// Given the name of a tree crop, return the ID of its sapling object
        /// </summary>
        /// <returns>The ID of the sapling object if found, -1 if not</returns>
        // public static string GetSaplingId(string treeName)
        // {
        //     string treeId = APIs.JsonAssets.GetFruitTreeId(treeName).ToString();
        //     foreach (KeyValuePair<string, StardewValley.GameData.FruitTrees.FruitTreeData> kvp in _fruitTreeData)
        //     {
        //         //find the tree id in fruitTrees information to get sapling id
        //         Int32.TryParse(kvp.Value.DisplayName, out int id);
        //         if (treeId == id.ToString())
        //             return kvp.Key.ToString();
        //     }

        //     return "-1";
        // }

        public static void RegisterItemsToRemove()
        {
            if (APIs.JsonAssets == null)
                return;

            foreach (string pack in PacksToRemove)
            {
                var items = APIs.JsonAssets.GetAllBigCraftablesFromContentPack(pack);
                if (items != null)
                    ItemsToRemove.AddRange(items);

                items = APIs.JsonAssets.GetAllClothingFromContentPack(pack);
                if (items != null)
                    ItemsToRemove.AddRange(items);

                items = APIs.JsonAssets.GetAllHatsFromContentPack(pack);
                if (items != null)
                    ItemsToRemove.AddRange(items);

                items = APIs.JsonAssets.GetAllObjectsFromContentPack(pack);
                if (items != null)
                {
                    ItemsToRemove.AddRange(items);
                }

                // var crops = APIs.JsonAssets.GetAllCropsFromContentPack(pack);

                // if (crops != null)
                // {
                //     foreach (string seedId in crops.Select(GetSeedId))
                //     {
                //         ItemsToRemove.Add(ObjectInfoSourceObject[seedId].Name);
                //     }
                // }

                // var trees = APIs.JsonAssets.GetAllFruitTreesFromContentPack(pack);
                // if (trees != null)
                // {
                //     foreach (string saplingID in trees.Select(GetSaplingId))
                //     {
                //         ItemsToRemove.Add(ObjectInfoSourceObject[saplingID].Name);
                //     }
                // }


                items = APIs.JsonAssets.GetAllWeaponsFromContentPack(pack);
                if (items != null)
                    ItemsToRemove.AddRange(items);
            }

            foreach (string pack in RecipePacksToRemove)
            {
                var items = APIs.JsonAssets.GetAllBigCraftablesFromContentPack(pack);
                if (items != null)
                    ItemsToRemove.AddRange(items.Select(i => (i + " Recipe")));

                items = APIs.JsonAssets.GetAllObjectsFromContentPack(pack);
                if (items != null)
                {
                    ItemsToRemove.AddRange(items.Select(i => (i + " Recipe")));
                }
            }
        }

        public static Dictionary<ISalable, int[]> RemoveSpecifiedJAPacks(Dictionary<ISalable, int[]> stock)
        {
            List<ISalable> removeItems = (stock.Keys.Where(item => ItemsToRemove.Contains(item.Name))).ToList();

            foreach (var item in removeItems)
            {
                stock.Remove(item);
            }

            return stock;
        }

        public static void RemoveSoldOutItems(Dictionary<ISalable, int[]> stock)
        {
            List<ISalable> keysToRemove = (stock.Where(kvp => kvp.Value[1] == 0).Select(kvp => kvp.Key)).ToList();
            foreach (ISalable item in keysToRemove)
                stock.Remove(item);
        }

        public static bool IsInSeasonCrop(string itemId)
        {
            if (_cropData.ContainsKey(itemId))
            {
                return _cropData[itemId].Seasons.Contains(Game1.season);
            }

            if (_fruitTreeData.ContainsKey(itemId))
            {
                return _fruitTreeData[itemId].Seasons.Contains(Game1.season);
            }

            return false;
        }
    }
}