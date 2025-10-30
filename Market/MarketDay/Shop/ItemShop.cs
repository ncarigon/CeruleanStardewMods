using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using MarketDay.API;
using MarketDay.Data;
using MarketDay.ItemPriceAndStock;
using MarketDay.Utility;

namespace MarketDay.Shop
{
    /// <summary>
    /// This class holds all the information for each custom item shop
    /// </summary>
    public class ItemShop : ItemShopModel
    {
        protected Texture2D _portrait;
        public ItemPriceAndStockManager StockManager { get; set; }

        public IContentPack ContentPack { set; get; }

        /// <summary>
        /// Initializes the stock manager, done at game loaded so that content packs have finished loading in
        /// </summary>
        public void Initialize()
        {
            StockManager = new ItemPriceAndStockManager(this);
        }

        /// <summary>
        /// Loads the portrait, if it exists, and use the seasonal version if one is found for the current season
        /// </summary>
        public void UpdatePortrait()
        {
            if (PortraitPath == null)
                return;

            //construct seasonal path to the portrait
            string seasonalPath = PortraitPath.Insert(PortraitPath.IndexOf('.'), "_" + Game1.currentSeason);
            try
            {
                //if the seasonal version exists, load it
                if (ContentPack.HasFile(seasonalPath)) 
                {
                    _portrait = ContentPack.ModContent.Load<Texture2D>(seasonalPath);
                }
                //if the seasonal version doesn't exist, try to load the default
                else if (ContentPack.HasFile(PortraitPath))
                {
                    _portrait = ContentPack.ModContent.Load<Texture2D>(PortraitPath);
                }
            }
            catch (Exception ex) //couldn't load the image
            {
                MarketDay.Log(ex.Message+ex.StackTrace, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Refreshes the contents of all stores
        /// and sets the flag for if the store has been opened yet today to false
        /// </summary>
        public void UpdateItemPriceAndStock()
        {
            MarketDay.Log($"Generating stock for {ShopName}", LogLevel.Trace, true);
            StockManager.Update();
        }

        /// <summary>
        /// Translate what needs to be translated on game saved, in case of the language being changed
        /// </summary>
        internal void UpdateTranslations()
        {
            Quote = Translations.Localize(Quote, LocalizedQuote);
            ClosedMessage = Translations.Localize(ClosedMessage, LocalizedClosedMessage);
        }
    }
}
