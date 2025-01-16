namespace MarketDay.Data
{
    // ChroniclerCherry
    public abstract class ItemStockModel
    {
        public string ItemType { get; set; }
        public bool IsRecipe { get; set; } = false;
        public int StockPrice { get; set; } = -1;
        public double SellPriceMultiplier { get; set; } = -1;
        public int SellPriceMode { get; set; }
        public string StockItemCurrency { get; set; } = "Money";
        public int StockCurrencyStack { get; set; } = 1;
        public int Quality { get; set; } = 0;
        public string[] ItemIDs { get; set; } = null;
        public string[] JAPacks { get; set; } = null;
        public string[] ExcludeFromJAPacks { get; set; } = null;
        public string[] ItemNames { get; set; } = null;
        public bool FilterSeedsBySeason { get; set; } = true;
        public int Stock { get; set; } = int.MaxValue;
        public int MaxNumItemsSoldInItemStock { get; set; } = int.MaxValue;
        public string[] When { get; set; } = null;
    }
}