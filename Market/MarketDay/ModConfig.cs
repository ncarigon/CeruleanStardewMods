using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace MarketDay
{
    internal class ModConfig
    {
        public bool Progression { get; set; } = true;

        public bool SharedShop { get; set; } = true;
        
        public bool OnNextDayIfCancelled { get; set; } = true;

        public int DayOfWeek { get; set; } = 6;
        public bool UseAdvancedOpeningOptions { get; set; }
        public bool OpenOnMon { get; set; }
        public bool OpenOnTue { get; set; }
        public bool OpenOnWed { get; set; }
        public bool OpenOnThu { get; set; }
        public bool OpenOnFri { get; set; }
        public bool OpenOnSat { get; set; } = true;
        public bool OpenOnSun { get; set; }
        public bool OpenInSpring { get; set; } = true;
        public bool OpenInSummer { get; set; } = true;
        public bool OpenInFall { get; set; } = true;
        public bool OpenInWinter { get; set; } = true;
        public bool OpenWeek1 { get; set; } = true;
        public bool OpenWeek2 { get; set; } = true;
        public bool OpenWeek3 { get; set; } = true;
        public bool OpenWeek4 { get; set; } = true;
        public int OpeningTime { get; set; } = 8;
        public int ClosingTime { get; set; } = 18;
        public int NumberOfShops { get; set; } = 6;
        public bool OpenInRain { get; set; }
        public bool OpenInSnow { get; set; }
        public bool GMMCompat { get; set; } = true;
        public int RestockItemsPerHour { get; set; } = 3;
        public float StallVisitChance { get; set; } = 0.9f;
        public bool ReceiveMessages { get; set; } = true;
        public bool PeekIntoChests { get; set; }
        public bool NoFreeItems { get; set; } = true;
        public bool RuinTheFurniture { get; set; }
        public Dictionary<string, bool> ShopsEnabled = new();
        public bool VerboseLogging { get; set; }
        public bool ShowMultiplayerMessages { get; set; } = true;
        public bool ShowShopPositions { get; set; }
        public bool NPCVisitors { get; set; } = true;
        public bool NPCOwnerRescheduling { get; set; } = true;
        public bool NPCVisitorRescheduling { get; set; } = true;
        public bool NPCScheduleReplacement { get; set; } = true;
        public int NumberOfTownieVisitors { get; set; } = 25;
        public bool AlwaysMarketDay { get; set; }
        public bool DebugKeybinds { get; set; }
        public SButton OpenConfigKeybind { get; set; } = SButton.V;
        public SButton WarpKeybind { get; set; } = SButton.Z;
        public SButton ReloadKeybind { get; set; } = SButton.R;
        public SButton StatusKeybind { get; set; } = SButton.Q;

        public string[] NonChildItemTags { get; set; } = new[] { "alcohol_item", "mature_item" };

        public float SellBonusLike { get; set; } = 0.1f;
        public float SellBonusLove { get; set; } = 0.2f;

        public float SellBonusTalk { get; set; } = 0.1f;

        public float SellBonusNearby { get; set; } = 0.2f;

        public float SellBonusScore { get; set; } = 0.1f;

        public float SellBonusHearts { get; set; } = 0.1f;

        public float SellBonusSign { get; set; } = 0.1f;

        public bool GetProgression() {
            var val = Progression;
            var farm = Game1.hasLoadedGame ? Game1.getFarm() : null;
            if (farm is null) return val;
            string v;
            if (Game1.IsMasterGame) {
                if (farm.modData?.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/Config.Progression", out v) != true || val.ToString().ToLower() != v) {
                    SetProgression(val);
                }
                return val;
            }
            return farm.modData?.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/Config.Progression", out v) == true && v == "true";
        }

        public void SetProgression(bool value) {
            Progression = value;
            var farm = Game1.hasLoadedGame ? Game1.getFarm() : null;
            if (farm is null) return;
            if (Game1.IsMasterGame) {
                Game1.getFarm().modData[$"{MarketDay.SMod.ModManifest.UniqueID}/Config.Progression"] = value.ToString().ToLower();
            }
        }

        public bool GetSharedShop() {
            var val = SharedShop;
            var farm = Game1.hasLoadedGame ? Game1.getFarm() : null;
            if (farm is null) return val;
            string v;
            if (Game1.IsMasterGame) {
                if (farm.modData?.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/Config.SharedShop", out v) != true || val.ToString().ToLower() != v) {
                    SetSharedShop(val);
                }
                return val;
            }
            return farm.modData?.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/Config.SharedShop", out v) == true && v == "true";
        }

        public void SetSharedShop(bool value) {
            SharedShop = value;
            var farm = Game1.hasLoadedGame ? Game1.getFarm() : null;
            if (farm is null) return;
            if (Game1.IsMasterGame) {
                Game1.getFarm().modData[$"{MarketDay.SMod.ModManifest.UniqueID}/Config.SharedShop"] = value.ToString().ToLower();
            }
        }

        public bool GetRuinTheFurniture() {
            var val = RuinTheFurniture;
            var farm = Game1.hasLoadedGame ? Game1.getFarm() : null;
            if (farm is null) return val;
            string v;
            if (Game1.IsMasterGame) {
                if (farm.modData?.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/Config.RuinTheFurniture", out v) != true || val.ToString().ToLower() != v) {
                    SetRuinTheFurniture(val);
                }
                return val;
            }
            return farm.modData?.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/Config.RuinTheFurniture", out v) == true && v == "true";
        }

        public void SetRuinTheFurniture(bool value) {
            RuinTheFurniture = value;
            var farm = Game1.hasLoadedGame ? Game1.getFarm() : null;
            if (farm is null) return;
            if (Game1.IsMasterGame) {
                Game1.getFarm().modData[$"{MarketDay.SMod.ModManifest.UniqueID}/Config.RuinTheFurniture"] = value.ToString().ToLower();
            }
        }
    }
}
