using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using MarketDay.API;
using MarketDay.Data;
using MarketDay.Utility;
using System;

namespace MarketDay.ItemPriceAndStock
{
    /// <summary>
    /// This class stores the data for each stock, with a stock being a list of items of the same itemtype
    /// and sharing the same store parameters such as price
    /// </summary>
    public class ItemStock : ItemStockModel
    {
        internal string CurrencyObjectId;
        internal double DefaultSellPriceMultiplier;
        internal Dictionary<double, string[]> PriceMultiplierWhen;
        internal string ShopName;

        private Dictionary<double, string[]> _priceMultiplierWhen;

        private ItemBuilder _builder;
        private Dictionary<ISalable, ItemStockInformation> _itemPriceAndStock;

        /// <summary>
        /// Initialize the ItemStock, doing error checking on the quality, and setting the price to the store price
        /// if none is given specifically for this stock.
        /// Creates the builder
        /// </summary>
        /// <param name="shopName"></param>
        /// <param name="price"></param>
        /// <param name="defaultSellPriceMultiplier"></param>
        /// <param name="priceMultiplierWhen"></param>
        internal void Initialize(string shopName, int price, double defaultSellPriceMultiplier, Dictionary<double, string[]> priceMultiplierWhen)
        {
            ShopName = shopName;
            DefaultSellPriceMultiplier = SellPriceMultiplier <= 0 ? defaultSellPriceMultiplier : SellPriceMultiplier;
            PriceMultiplierWhen = priceMultiplierWhen;

            if (Quality is < 0 or 3 or > 4)
            {
                Quality = 0;
                MarketDay.Log("Item quality can only be 0,1,2, or 4. Defaulting to 0", LogLevel.Warn);
            }

            CurrencyObjectId = ItemsUtil.GetIndexByName(StockItemCurrency);

            //sets price to the store price if no stock price is given
            if (StockPrice < 1)
            {
                StockPrice = price;
            }
            _priceMultiplierWhen = priceMultiplierWhen;

            if (IsRecipe)
                Stock = 1;

            _builder = new ItemBuilder(this);
        }

        /// <summary>
        /// Resets the items of this item stock, with condition checks and randomization
        /// </summary>
        /// <returns></returns>
        public Dictionary<ISalable, ItemStockInformation> Update()
        {
            if (When != null && !APIs.Conditions.CheckConditions(When))
                return null; //did not pass conditions

            if (!ItemsUtil.CheckItemType(ItemType)) //check that itemtype is valid
            {
                MarketDay.Log($"\t\"{ItemType}\" is not a valid ItemType. No items from this stock will be added."
                    , LogLevel.Warn);
                return null;
            }

            _itemPriceAndStock = new Dictionary<ISalable, ItemStockInformation>();
            _builder.SetItemPriceAndStock(_itemPriceAndStock);

            double priceMultiplier = 1;
            if (_priceMultiplierWhen != null)
            {
                foreach (KeyValuePair<double,string[]> kvp in _priceMultiplierWhen)
                {
                    if (APIs.Conditions.CheckConditions(kvp.Value))
                    {
                        priceMultiplier = kvp.Key;
                        break;
                    }
                }
            }

            AddById(priceMultiplier);
            AddByName(priceMultiplier);
            ItemsUtil.RandomizeStock(_itemPriceAndStock, MaxNumItemsSoldInItemStock);
            return _itemPriceAndStock;
        }

        /// <summary>
        /// Add all items listed in the ItemIDs section
        /// </summary>
        private void AddById(double priceMultiplier)
        {
            if (ItemIDs == null)
                return;

            foreach (var itemId in ItemIDs)
            {
                _builder.AddSpecificItemToStock(itemId, priceMultiplier);
            }
        }

        /// <summary>
        /// Add all items listed in the ItemNames section
        /// </summary>
        private void AddByName(double priceMultiplier)
        {
            if (ItemNames == null)
                return;

            foreach (var itemName in ItemNames)
            {
                _builder.AddItemToStock(itemName, priceMultiplier);
            }

        }
    }
}
