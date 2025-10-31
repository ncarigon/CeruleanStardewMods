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
using SObject = StardewValley.Object;
using MarketDay.Shop;
using StardewValley.Locations;

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
                var offset = CategorySearchPrefix.Length;
                id = ItemsUtil.GetIndexByCategory(itemName[offset..], _itemStock.ItemType);
            }
            else if (itemName.StartsWith(NameSearchPrefix))
            {
                var offset = NameSearchPrefix.Length;
                id = ItemsUtil.GetIndexByMatch(itemName[offset..], _itemStock.ItemType);
            } else {
                id = ItemsUtil.GetIndexByName(itemName, _itemStock.ItemType);
            }

            if (id != "-1" && id != "0") return AddSpecificItemToStock(id, priceMultiplier);
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

            var item = CreateItem(itemId);

            if (_itemStock.FilterBySeason && item is SObject o
                && new[] { SObject.SeedsCategory, SObject.FruitsCategory, SObject.VegetableCategory, SObject.GreensCategory, SObject.flowersCategory, SObject.FishCategory }.Contains(o.Category)
            )
            {
                if (!ItemsUtil.IsInSeason(o)) return false;
            }

            if (item is Clothing c)
            {
                c.Dye(new Color((uint)Game1.random.NextInt64()) { A = byte.MaxValue }, 1f);
            }
            
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
            if (!(priceStockCurrency.Price > 0 || (priceStockCurrency.TradeItem != null && priceStockCurrency.TradeItemCount > 0))) {
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
                    return new SObject(itemId, _itemStock.Stock, _itemStock.IsRecipe, quality: _itemStock.Quality);
                case "BigCraftable":
                    return new SObject(Vector2.Zero, itemId) { Stack = _itemStock.Stock, IsRecipe = _itemStock.IsRecipe };
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

        private static bool TryGetShopItemData(string itemId, out ParsedItemData itemData) {
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
            if (_itemStock.CurrencyObjectId == "-1") // no currency item
            {
                var price = _itemStock.StockPrice;
                if (price < 1)
                {
                    var sellPrice = item.salePrice();
                    TryGetShopPrice(_itemStock, item.QualifiedItemId, out var shopPrice);
                    if (sellPrice > 0 && shopPrice > 0)
                    {
                        price = (int)((sellPrice * 0.9) + (shopPrice * 0.1));
                    } else if (sellPrice > 0 && shopPrice < 1)
                    {
                        price = sellPrice;
                    } else if (sellPrice < 1 && shopPrice > 0)
                    {
                        price = shopPrice;
                    }

                    // apply mult
                    price = (int)(price * _itemStock.DefaultSellPriceMultiplier);
                }
                // apply When mult
                price = (int)(Math.Max(0, price) * priceMultiplier);
                priceStockCurrency = new(price, _itemStock.Stock);
            }
            else
            {
                priceStockCurrency = new(0, _itemStock.Stock, _itemStock.CurrencyObjectId, Math.Max(1, _itemStock.StockCurrencyStack));
            }

            var highestMult = 0f;
            if (IsMuseumItem(item))
            {
                if (MarketDay.Config.MuseumItemMult >= 1.0f)
                {
                    highestMult = MarketDay.Config.MuseumItemMult;
                } else
                {
                    priceStockCurrency.Stock = 0;
                }
            }

            if (IsBundleItem(item))
            {
                if (MarketDay.Config.BundleItemMult >= 1.0f)
                {
                    if (MarketDay.Config.BundleItemMult > highestMult)
                    {
                        highestMult = MarketDay.Config.BundleItemMult;
                    }
                } else
                {
                    priceStockCurrency.Stock = 0;
                }
            }

            if (highestMult > 0)
            {
                // prevents double mult for an item that is both museum and bundle
                priceStockCurrency.Price = (int)(priceStockCurrency.Price * highestMult);
            }

            return priceStockCurrency;
        }

        private static bool IsBundleItem(ISalable item)
        {
            return item is SObject i && Game1.getLocationFromName("CommunityCenter") is CommunityCenter c && c.couldThisIngredienteBeUsedInABundle(i);
        }

        private static readonly string[] MuseumTypes = new[] { "Arch", "Minerals" };

        private static bool IsMuseumItem(ISalable item)
        {
            return item is SObject o && MuseumTypes.Contains(o.Type) && !LibraryMuseum.HasDonatedArtifact(item.QualifiedItemId);
        }
    }
}
