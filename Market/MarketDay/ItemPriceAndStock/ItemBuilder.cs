using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Collections.Generic;
using MarketDay.Utility;
using System.Linq;
using System;
using StardewValley.ItemTypeDefinitions;

namespace MarketDay.ItemPriceAndStock
{
    /// <summary>
    /// This class stores the global data for each itemstock, in order to generate and add items by ID or name
    /// to the stock
    /// </summary>
    class ItemBuilder
    {
        private Dictionary<ISalable, ItemStockInformation> _itemPriceAndStock;
        private readonly ItemStock _itemStock;
        private const string CategorySearchPrefix = "%Category:";
        private const string NameSearchPrefix = "%Match:";

        public ItemBuilder(ItemStock itemStock)
        {
            _itemStock = itemStock;
        }

        /// <param name="itemPriceAndStock">the ItemPriceAndStock this builder will add items to</param>
        public void SetItemPriceAndStock(Dictionary<ISalable, ItemStockInformation> itemPriceAndStock)
        {
            _itemPriceAndStock = itemPriceAndStock;
        }

        /// <summary>
        /// Takes an item name, and adds that item to the stock
        /// </summary>
        /// <param name="itemName">name of the item</param>
        /// <param name="priceMultiplier"></param>
        /// <returns></returns>
        public bool AddItemToStock(string itemName, double priceMultiplier = 1)
        {
            string id;
            if (itemName.StartsWith(CategorySearchPrefix))
            {
                var offset = CategorySearchPrefix.Length + 1;
                id = ItemsUtil.GetIndexByCategory(itemName[offset..], _itemStock.ItemType);
            }
            else if (itemName.StartsWith(NameSearchPrefix))
            {
                var offset = NameSearchPrefix.Length + 1;
                id = ItemsUtil.GetIndexByMatch(itemName[offset..], _itemStock.ItemType);
            } else {
                id = ItemsUtil.GetIndexByName(itemName, _itemStock.ItemType);
            }
            
            if (id != "-1" && id != "0") return AddSpecificItemToStock(id, priceMultiplier);
            var item = ItemsUtil.GetDGAObjectByName(itemName, _itemStock.ItemType);
            if (item is not null) return AddSpecificItemToStock(item, priceMultiplier);
            MarketDay.Log($"{_itemStock.ItemType} named \"{itemName}\" could not be added to the Shop {_itemStock.ShopName}", LogLevel.Trace);
            return false;
        }

        /// <summary>
        /// Takes an item id, and adds that item to the stock
        /// </summary>
        /// <param name="itemId">the id of the item</param>
        /// <param name="priceMultiplier"></param>
        /// <returns></returns>
        public bool AddSpecificItemToStock(string itemId, double priceMultiplier = 1)
        {

            MarketDay.Log($"Adding item ID {itemId} to {_itemStock.ShopName}", LogLevel.Debug, true);

            if (itemId == "-1")
            {
                MarketDay.Log($"{_itemStock.ItemType} of ID {itemId} could not be added to the Shop {_itemStock.ShopName}", LogLevel.Trace);
                return false;
            }

            if (_itemStock.ItemType == "Seed" && _itemStock.FilterSeedsBySeason)
            {
                if (!ItemsUtil.IsInSeasonCrop(itemId)) return false;
            }

            var item = CreateItem(itemId);
            return item != null && AddSpecificItemToStock(item, priceMultiplier);
        }

        private bool AddSpecificItemToStock(ISalable item, double priceMultiplier)
        {
            if (item is null)
            {
                MarketDay.Log($"Null {_itemStock.ItemType} could not be added to the Shop {_itemStock.ShopName}", LogLevel.Trace);
                return false;
            }
            
            if (_itemStock.IsRecipe)
            {
                if (!DataLoader.CraftingRecipes(Game1.content).Keys.Any(c => string.Compare($"{c} Recipe", item?.Name) == 0)
                    && !DataLoader.CookingRecipes(Game1.content).Keys.Any(c => string.Compare($"{c} Recipe", item?.Name) == 0))
                {
                    MarketDay.Log($"{item.Name} is not a valid recipe and won't be added.", LogLevel.Trace);
                    return false;
                }
            }

            var priceStockCurrency = GetPriceStockAndCurrency(item, priceMultiplier);
            if (priceStockCurrency.Price < 1) {
                if (MarketDay.Config.NoFreeItems) {
                    MarketDay.Log($"{item.Name} does not have a valid price and will not be stocked.", LogLevel.Warn);
                    return false;
                } else {
                    MarketDay.Log($"{item.Name} does not have a valid price and will be free.", LogLevel.Warn);
                }
            }
            _itemPriceAndStock.Add(item, priceStockCurrency);

            return true;
        }

        /// <summary>
        /// Given an itemID, return an instance of that item with the parameters saved in this builder
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private ISalable CreateItem(string itemId)
        {
            switch (_itemStock.ItemType)
            {
                case "Object":
                case "Seed":
                    return new StardewValley.Object(itemId, _itemStock.Stock, _itemStock.IsRecipe, quality: _itemStock.Quality);
                case "BigCraftable":
                    return new StardewValley.Object(Vector2.Zero, itemId) { Stack = _itemStock.Stock, IsRecipe = _itemStock.IsRecipe };
                case "Shirt":
                case "Pants":
                    return new Clothing(itemId);
                case "Ring":
                    return new Ring(itemId);
                case "Hat":
                    return new Hat(itemId);
                case "Boot":
                    return new Boots(itemId);
                case "Furniture":
                    return new Furniture(itemId, Vector2.Zero);
                case "Weapon":
                    return new MeleeWeapon(itemId);
                default: return null;
            }
        }

        private static bool TryGetShopItemData(string? itemId, out ParsedItemData? itemData) {
            itemData = ItemRegistry.GetDataOrErrorItem(itemId);
            return !itemData.IsErrorItem;
        }

        private static bool TryGetShopPrice(ItemStock _itemStock, string qId, out int price) {
            var curr = _itemStock.CurrencyObjectId == "-1" ? "0" : _itemStock.CurrencyObjectId;
            price = DataLoader.Shops(Game1.content) // check all shops
                .Where(s => s.Value?.Currency.ToString()?.Equals(curr) == true) // must use same currency
                .SelectMany(s => s.Value.Items // check all items
                    .Where(i => string.IsNullOrEmpty(i?.TradeItemId) || i?.TradeItemId?.Equals(curr) == true) // uses the same currency
                    .Where(i => TryGetShopItemData(i?.ItemId, out var shopItem) // shop item exists
                        && shopItem?.QualifiedItemId?.Equals(qId) == true) // must be same item
                ).OrderByDescending(i => i.Price) // most expensive first
                .Select(i => i.Price) // we only need the price
                .FirstOrDefault(); // most expensive only
            return price > 0;
        }

        /// <summary>
        /// Creates the second parameter in ItemStockAndPrice, an array that holds info on the price, stock,
        /// and if it exists, the item currency it takes
        /// </summary>
        /// <param name="item">An instance of the item</param>
        /// <param name="priceMultiplier"></param>
        /// <returns>The array that's the second parameter in ItemPriceAndStock</returns>
        private ItemStockInformation GetPriceStockAndCurrency(ISalable item, double priceMultiplier)
        {
            ItemStockInformation priceStockCurrency;
            var price = _itemStock.StockPrice;
            if (price < 1) {
                //if no price, try another method
                switch (_itemStock.SellPriceMode) {
                    case 1: // sale price, then shop price; prone to errors with non-valued items
                        if ((price = item.salePrice()) < 1) {
                            TryGetShopPrice(_itemStock, item.QualifiedItemId, out price);
                        }
                        break;
                    case 2: // only shop price; prone to errors with items that aren't sold elsewhere
                        TryGetShopPrice(_itemStock, item.QualifiedItemId, out price);
                        break;
                    case 3: // only sale price; matches legacy mode
                        price = item.salePrice();
                        break;
                    default: // shop price, then sale price; less error prone
                        if (!TryGetShopPrice(_itemStock, item.QualifiedItemId, out price)) {
                            price = item.salePrice();
                        }
                        break;
                }
                // multiplied by defaultSellPriceMultiplier
                price = (int)(price * _itemStock.DefaultSellPriceMultiplier);
            }
            price = (int)(price*priceMultiplier);

            if (_itemStock.CurrencyObjectId == "-1") // no currency item
            {
                priceStockCurrency = new(price, _itemStock.Stock);
            }
            else if (_itemStock.StockCurrencyStack == -1) //no stack provided for currency item so defaults to 1
            {
                priceStockCurrency = new(price, _itemStock.Stock, _itemStock.CurrencyObjectId);
            }
            else //both currency item and stack provided
            {
                priceStockCurrency = new(price, _itemStock.Stock, _itemStock.CurrencyObjectId, _itemStock.StockCurrencyStack);
            }

            return priceStockCurrency;
        }
    }
}
