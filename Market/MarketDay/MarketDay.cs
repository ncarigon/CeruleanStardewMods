using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MarketDay.API;
using MarketDay.Data;
using MarketDay.ItemPriceAndStock;
using MarketDay.Shop;
using MarketDay.Utility;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.ObjectModel;
using SObject = StardewValley.Object;

namespace MarketDay
{
    public class SalesRecord
    {
        public Item item;
        public int price;
        public NPC npc;
        public double mult;
        public int timeOfDay;
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class MarketDay : Mod
    {
        // set true when NPC schedules have been re-planned 
        // following CP map patching
        private static bool MapChangesSynced;

        // set true when hot reload is done 
        // following a GMCM change to market day
        private static bool GMCMChangesSynced = true;
        
        internal static ModConfig Config;
        internal static IModHelper helper;
        internal static IMonitor monitor;
        internal static Mod SMod;
        
        // ChroniclerCherry
        //The following variables are to help revert hardcoded warps done by the carpenter and
        //animal shop menus
        internal static GameLocation SourceLocation;
        private static Vector2 _playerPos = Vector2.Zero;
        internal static bool VerboseLogging = false;
        
        private IGenericModConfigMenuApi configMenu;
        private IContentPatcherAPI ContentPatcherApi;
        private static bool GMMJosephPresent;
        private static bool GMMPaisleyPresent;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="h">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper h)
        {
            helper = h;
            monitor = Monitor;
            SMod = this;

            Helper.Events.GameLoop.GameLaunched += OnLaunched;
            Helper.Events.GameLoop.GameLaunched += OnLaunched_STFRegistrations;
            Helper.Events.GameLoop.GameLaunched += OnLaunched_STFInit;
            // Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded_ReadConfig;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded_DestroyFurniture;
            Helper.Events.GameLoop.DayStarted += OnDayStarted_MakePlayerShops;
            Helper.Events.GameLoop.DayStarted += OnDayStarted_UpdateSTFStock_SendPrompt;
            // Helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
            Helper.Events.GameLoop.OneSecondUpdateTicking += OnOneSecondUpdateTicking_SyncMapOpenShops;
            Helper.Events.GameLoop.OneSecondUpdateTicking += OnOneSecondUpdateTicking_InteractWithNPCs;
            Helper.Events.GameLoop.TimeChanged += OnTimeChanged_RestockThroughDay;
            Helper.Events.GameLoop.DayEnding += OnDayEnding_EmptyGrangeAndDestroyFurniture;
            Helper.Events.GameLoop.Saving += OnSaving_WriteConfig;
            Helper.Events.GameLoop.Saved += OnSaved_DoNothing;
            Helper.Events.Input.ButtonPressed += OnButtonPressed_ShowShopOrGrangeOrStats;

            var harmony = new Harmony("ceruleandeep.MarketDay");
            harmony.PatchAll();

            helper.ConsoleCommands.Add("fm_furniture", "Remove stray furniture", RemoveStrayFurniture);
            helper.ConsoleCommands.Add("fm_reload", "Reload shop data", HotReload);
            helper.ConsoleCommands.Add("fm_tiles", "List shop tiles", ListShopTiles);
        }

        // private static void Entry_InitSTF()
        // {
        //     ShopManager.LoadContentPacks();
        //     MakePlayerShop();
        // }

        private static void RemovePlayerShops()
        {
            foreach (var (shopKey, grangeShop) in ShopManager.GrangeShops)
            {
                if (grangeShop.IsPlayerShop()) ShopManager.GrangeShops.Remove(shopKey);
            }
        }
        
        private static void MakePlayerShops()
        {
            foreach (var farmer in Game1.getAllFarmers())
            {
                if (!farmer.isActive()) continue;
                var PlayerShop = new GrangeShop()
                {
                    ShopName = $"Farmer:{farmer.Name}",
                    Quote = "Farmer store",
                    ItemStocks = Array.Empty<ItemStock>(),
                    PlayerID = farmer.UniqueMultiplayerID
                };
                ShopManager.GrangeShops.Add(PlayerShop.ShopKey, PlayerShop);
                Log($"Added shop {PlayerShop.ShopKey} for {PlayerShop.ShopName} ({PlayerShop.PlayerID})", LogLevel.Trace);
            }
        }

        // void OnFieldChanged(IManifest mod, Action<string, object> onChange);

        private static void GMCMFieldChanged(string fieldID, object val)
        {
            Log($"GMCMFieldChanged: {fieldID} = {val}", LogLevel.Trace);
            if (fieldID.StartsWith("fm_")) GMCMChangesSynced = false;
        }

        private static void SyncAfterGMCMChanges()
        {
            GMCMChangesSynced = true;
            if (!Context.IsMainPlayer) return;
            OnDayEnding_EmptyGrangeAndDestroyFurniture(null, null);

            OnDayStarted_UpdateSTFStock_SendPrompt(null, null);

            helper.ConsoleCommands.Trigger("patch", new[]{"reload", SMod.ModManifest.UniqueID+".CP"});
            MapChangesSynced = false;
            OnOneSecondUpdateTicking_SyncMapOpenShops(null, null);
            LogModState();
        }
        
        private static void HotReload(string command=null, string[] args=null)
        {
            if (!Context.IsMainPlayer) return;

            Log($"HotReload", LogLevel.Info);

            Log($"    Closing {MapUtility.ShopAtTile().Values.Count} stores", LogLevel.Debug);
            OnDayEnding_EmptyGrangeAndDestroyFurniture(null, null);

            helper.ConsoleCommands.Trigger("patch", new[]{"reload", SMod.ModManifest.UniqueID+".CP"});
            
            // this bit is just for hot reload of data, not normal life cycle
            Log($"    Removing non-player stores", LogLevel.Debug);
            foreach (var (ShopKey, shop) in ShopManager.GrangeShops)
                ShopManager.GrangeShops.Remove(ShopKey);

            helper.WriteConfig(Config);
            
            Log($"    Loading content packs", LogLevel.Debug);
            OnLaunched_STFInit(null, null);
            OnSaveLoaded_DestroyFurniture(null, null);
            // end: this bit is just for hot reload of data, not normal life cycle

            Log($"    Rebuilding player shops", LogLevel.Debug);
            OnDayStarted_MakePlayerShops(null, null);

            Log($"    Updating stock", LogLevel.Debug);
            OnDayStarted_UpdateSTFStock_SendPrompt(null, null);

            Log($"    Opening stores", LogLevel.Debug);
            OnOneSecondUpdateTicking_SyncMapOpenShops(null, null);

            LogModState();
        }

        private static void LogModState()
        {
            var state = new List<string>();
            state.Add($"{SMod.ModManifest.Name} {SMod.ModManifest.Version} state:");
            state.Add($"Time  {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}");
            var festival = StardewValley.Utility.isFestivalDay(Game1.dayOfMonth, Game1.currentSeason);
            state.Add($"Weather  Rain: {Game1.isRaining}  Snow: {Game1.isSnowing}  Festival: {festival}");
            state.Add($"Market Day: {IsMarketDay()}");
            state.Add($"GMM Compat  Enabled: {Config.GMMCompat}  GMM Day: {isGMMDay()}  Joseph: {GMMJosephPresent}  Paisley: {GMMPaisleyPresent}");
            
            var positions = string.Join(", ", ShopPositions());
            state.Add($"Shop positions: {Config.ShopLayout} shops: {positions}");

            state.Add($"Shop config:");
            var enabled = Config.ShopsEnabled.Select(kvp => $"    {kvp.Key}: {kvp.Value}").ToList();
            if (enabled.Count == 0)
                state.Add("    No shops enabled/disabled in config.json");
            state.AddRange(enabled);

            var apsk = string.Join(", ", AvailablePlayerShopKeys);
            state.Add($"Available player shops:  {AvailablePlayerShopKeys.Count} shops: {apsk}");

            var ansk = string.Join(", ", AvailableNPCShopKeys);
            state.Add($"Available NPC shops:  {AvailableNPCShopKeys.Count} shops: {ansk}");


            foreach (var line in state) Log(line, LogLevel.Debug);
        }

        private static void ListShopTiles(string command, string[] args)
        {
            Log($"ListShopTiles: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
            var town = Game1.getLocationFromName("Town");
            if (town is null)
            {
                Log($"    Town location not available", LogLevel.Error);
                return;
            }
            
            var layerWidth = town.map.Layers[0].LayerWidth;
            var layerHeight = town.map.Layers[0].LayerHeight;

            Log($"    Map dimensions {layerWidth} {layerHeight}", LogLevel.Trace);

            // top left corner is z_MarketDay 253
            for (var x = 0; x < layerWidth; x++)
            {
                for (var y = 0; y < layerHeight; y++)
                {
                    var tileSheetIdAt = town.getTileSheetIDAt(x, y, "Buildings");
                    if (! tileSheetIdAt.StartsWith("z_MarketDay")) continue;
                    var tileIndexAt = town.getTileIndexAt(x, y, "Buildings");
                    if (tileIndexAt != 253) continue;

                    Log($"    {x} {y}: {tileSheetIdAt} {tileIndexAt}", LogLevel.Trace);
                }
            }
        }

        /// <summary>Raised after the game is saved</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaving_WriteConfig(object sender, SavingEventArgs e)
        {
            Log($"OnSaving: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
            ListShopTiles("", null);
            Helper.WriteConfig(Config);
            Log($"OnSaving: complete at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

        }

        private static void OnSaved_DoNothing(object sender, SavedEventArgs e)
        {
            Log($"OnSaved: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
            ListShopTiles("", null);
            Log($"OnSaved: complete at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.
        ///
        /// For STF compat:
        /// On a save loaded, store the language for translation purposes. Done on save loaded in
        /// case it's changed between saves
        /// 
        /// Also retrieve all object informations. This is done on save loaded because that's
        /// when JA adds custom items
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnSaveLoaded_ReadConfig(object sender, EventArgs e)
        {
            // reload the config to pick up any changes made in GMCM on the title screen
            Config = helper.ReadConfig<ModConfig>();
        }
        
        private static void OnSaveLoaded_DestroyFurniture(object sender, EventArgs e)
        {
            Log($"OnSaveLoaded_DestroyFurniture: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

            // get a clean slate in case previous debugging runs have left furniture lying around
            RemoveStrayFurniture();
            
            Log($"OnSaveLoaded_DestroyFurniture: complete at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

        }

        private static void OnDayStarted_UpdateSTFStock_SendPrompt(object sender, EventArgs e)
        {
            Log($"OnDayStarted: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

            MapChangesSynced = false;
            Log($"OnDayStarted: MapChangesSynced set {MapChangesSynced} at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

            if (!IsMarketDay()) return;

            // STF 
            ShopManager.UpdateStock();
            
            // send market day prompt
            var openingTime = (Config.OpeningTime*100).ToString();
            openingTime = openingTime[..^2] + ":" + openingTime[^2..];
                
            var prompt = Get("market-day", new {openingTime});
            MessageUtility.SendMessage(prompt);
            
            Log($"OnDayStarted: complete at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
        }

        private static void OnDayStarted_MakePlayerShops(object sender, EventArgs e)
        {
            RemovePlayerShops();
            MakePlayerShops();
        }

        // private static void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        // {
        //     if (!e.IsMultipleOf(10)) return;
        //     if (!Context.IsWorldReady) return;
        // }

        private static void OnOneSecondUpdateTicking_SyncMapOpenShops(object sender, OneSecondUpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!Context.IsMainPlayer) return;
            if (!IsMarketDay()) return;
            
            if (MapChangesSynced) return;
            SyncMapChanges();
            
            if (!MapChangesSynced) return;
            OpenShops();
            LogModState();
        }

        private static void OnOneSecondUpdateTicking_InteractWithNPCs(object sender, OneSecondUpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!Context.IsMainPlayer) return;
            if (!IsMarketDay()) return;

            foreach (var shop in MapUtility.ShopAtTile().Values) shop.InteractWithNearbyNPCs();
        }

        private static void OnTimeChanged_RestockThroughDay(object sender, EventArgs e)
        {
            if (Game1.timeOfDay % 100 > 0) return;
            foreach (var store in MapUtility.ShopAtTile().Values) store.RestockThroughDay(IsMarketDay());
        }

        private static void SyncMapChanges()
        {
            if (!IsMarketDay()) return;
            if (!Context.IsMainPlayer) return;
            
            Log($"SyncMapChangesAndOpenShops: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

            if (MapUtility.ShopTiles is null)
            {
                Log($"SyncMapChangesAndOpenShops: MarketDay.ShopLocations is null, called too early", LogLevel.Trace);
                return;
            }
            
            if (MapUtility.ShopTiles.Count == 0)
            {
                Log($"SyncMapChangesAndOpenShops: MarketDay.ShopLocations.Count {MapUtility.ShopTiles.Count}, called too early", LogLevel.Trace);
                return;
            }
            Log($"    SyncMapChangesAndOpenShops: {Game1.ticks}", LogLevel.Trace);

            RecalculateSchedules();
            MapChangesSynced = true;
            Log($"SyncMapChangesAndOpenShops: MapChangesSynced set {MapChangesSynced} at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

            // we used to open the shops here
            
            Log($"SyncMapChangesAndOpenShops: completed at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
        }
        
        private static void RecalculateSchedules()
        {
            Log($"RecalculateSchedules: begins at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);

            foreach (var npc in StardewValley.Utility.getAllCharacters())
            {
                if (npc is null) continue;
                if (!npc.isVillager()) continue;
                npc.Schedule = npc.getSchedule(Game1.dayOfMonth);
            }
            Log($"RecalculateSchedules: completed at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
        }

        private static void OpenShops()
        {
            if (!MapChangesSynced) throw new Exception("Map changes not synced");

            StardewValley.Utility.Shuffle(Game1.random, MapUtility.ShopTiles);

            var availableShopKeys = AvailableNPCShopKeys;
            StardewValley.Utility.Shuffle(Game1.random, availableShopKeys);
            availableShopKeys.InsertRange(0, AvailablePlayerShopKeys);
            
            var strNames = string.Join(", ", availableShopKeys);
            Log($"OpenShops: Adding stores ({MapUtility.ShopTiles.Count} of {strNames})", LogLevel.Trace);

            foreach (var ShopLocation in MapUtility.ShopTiles)
            {
                if (availableShopKeys.Count == 0) break;
                var ShopKey = availableShopKeys[0];
                availableShopKeys.RemoveAt(0);
                if (ShopManager.GrangeShops.ContainsKey(ShopKey))
                {
                    Log($"    {ShopKey} at {ShopLocation}", LogLevel.Trace);
                    ShopManager.GrangeShops[ShopKey].OpenAt(ShopLocation);
                    PruneBushes(ShopLocation);
                }
                else
                {
                    Log($"    {ShopKey} is not in shopManager.GrangeShops", LogLevel.Trace);
                }
            }
        }

        private static void PruneBushes(Vector2 ShopTile)
        {
            var town = Game1.getLocationFromName("Town");
            // location.getLargeTerrainFeatureAt(x, y) is TerrainFeature tf and not null
            for (var dx = -2; dx < 5; dx++)
            {
                for (var dy = -1; dy < 5; dy++)
                {
                    var (x, y) = ShopTile - new Vector2(dx, dy);
                    if (town.getLargeTerrainFeatureAt((int)x, (int)y) is Bush bush)
                    {
                        town.largeTerrainFeatures.Remove(bush);
                    }
                }
            }
        }

        private static List<string> AvailableNPCShopKeys
        {
            get
            {
                var availableShopKeys = new List<string>();
                foreach (var (ShopKey, shop) in ShopManager.GrangeShops)
                {
                    if (shop.IsPlayerShop()) continue;
                    if (Config.ShopsEnabled.TryGetValue(ShopKey, out var enabled) && !enabled) continue;
                    if (shop.When is not null)
                    {
                        if (!APIs.Conditions.CheckConditions(shop.When)) continue;
                    }
                    availableShopKeys.Add(ShopKey);
                }
                return availableShopKeys;
            }
        }

        private static List<string> AvailablePlayerShopKeys
        {
            get
            {
                var availableShopKeys = new List<string>();
                foreach (var (ShopKey, shop) in ShopManager.GrangeShops)
                {
                    if (!shop.IsPlayerShop()) continue;
                    if (Config.ShopsEnabled.TryGetValue(ShopKey, out var enabled) && !enabled) continue;
                    if (shop.When is not null)
                    {
                        if (!APIs.Conditions.CheckConditions(shop.When)) continue;
                    }
                    availableShopKeys.Add(ShopKey);
                }
                return availableShopKeys;
            }
        }

        private static void OnDayEnding_EmptyGrangeAndDestroyFurniture(object sender, EventArgs e)
        {
            Log($"OnDayEnding: {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
            if (!Context.IsMainPlayer) return;
            
            foreach (var store in MapUtility.ShopAtTile().Values) store.EmptyGrangeAndDestroyFurniture();
            RemoveStrayFurniture();
            
            Log($"OnDayEnding: complete at {Game1.currentSeason} {Game1.dayOfMonth} {Game1.timeOfDay} {Game1.ticks}", LogLevel.Trace);
        }

        private static void RemoveStrayFurniture(string command=null, string[] args=null)
        {
            if (!Context.IsWorldReady) return;
            if (!Context.IsMainPlayer) return;

            Log($"DestroyAllFurniture", LogLevel.Trace);

            var toRemove = new Dictionary<Vector2, SObject>();
            var location = Game1.getLocationFromName("Town");
            string owner;
            
            foreach (var (tile, item) in location.Objects.Pairs)
            {
                if (item.modData.TryGetValue($"{SMod.ModManifest.UniqueID}/{GrangeShop.GrangeChestKey}", out owner))
                {
                    Log($"    {owner} {GrangeShop.GrangeChestKey} at {item.TileLocation}", LogLevel.Trace);
                    toRemove[tile] = item;
                }

                if (item.modData.TryGetValue($"{SMod.ModManifest.UniqueID}/{GrangeShop.StockChestKey}", out owner))
                {
                    Log($"    {owner} {GrangeShop.StockChestKey} at {item.TileLocation}", LogLevel.Trace);
                    toRemove[tile] = item;
                }

                if (item.modData.TryGetValue($"{SMod.ModManifest.UniqueID}/{GrangeShop.ShopSignKey}", out owner))
                {
                    Log($"    {owner} {GrangeShop.ShopSignKey} at {item.TileLocation}", LogLevel.Trace);
                    toRemove[tile] = item;
                }
            }

            foreach (var (tile, item) in toRemove)
            {
                if (item is not Chest && item is not Sign) continue;
                Log($"    Removing {item} from {tile}", LogLevel.Trace);
                location.Objects.Remove(tile);
            }
        }


        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed_ShowShopOrGrangeOrStats(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (Game1.activeClickableMenu is not null) return;
            
            if (Config.DebugKeybinds) CheckDebugKeybinds(e);
            
            string signOwner = null;
            
            if (Game1.currentLocation is null) return;
            if (Game1.currentLocation.objects.TryGetValue(e.Cursor.GrabTile, out var objectAt))
            {
                objectAt.modData.TryGetValue($"{ModManifest.UniqueID}/{GrangeShop.ShopSignKey}", out signOwner);
            }
            
            if (e.Button.IsUseToolButton() && objectAt is not null && signOwner is not null)
            {
                Log($"Tool use on {objectAt.Name} owned by {signOwner}", LogLevel.Trace);
                if (signOwner.StartsWith("Farmer:"))
                {
                    // clicked on player shop sign, show the summary
                    Helper.Input.Suppress(e.Button);
                    ShopManager.GrangeShops[signOwner].ShowSummary();
                    return;
                }
                    
                // clicked on NPC shop sign, open the store
                Helper.Input.Suppress(e.Button);
                ShopManager.GrangeShops[signOwner].DisplayShop();
                return;
            }

            if (!e.Button.IsActionButton()) return;
            
            // refer clicks on grange tiles to each shop
            var gtShop = MapUtility.ShopNearTile(e.Cursor.GrabTile);
            gtShop?.OnActionButton(e);

            //
            // var tileIndexAt = Game1.currentLocation.getTileIndexAt((int) x, (int) y, "Buildings");
            // Log($"ActionButton {tileIndexAt}", LogLevel.Debug, true);
            // if (tileIndexAt is < 349 or > 351) return;
            //
            // var signTile = new Vector2(352 - tileIndexAt + x, y);
            // Log($"    checking {signTile}", LogLevel.Debug, true);
            // if (Game1.currentLocation.objects.TryGetValue(signTile, out var sign) && sign is Sign)
            // {
            //     sign.modData.TryGetValue($"{ModManifest.UniqueID}/{GrangeShop.ShopSignKey}", out var shopOwner);
            //     Log($"OnButtonPressed ActionButton for {shopOwner}", LogLevel.Debug, true);
            //
            //     var shop = ShopManager.GrangeShops[shopOwner];
            //     shop.OnActionButton(e);
            // }
        }

        private void CheckDebugKeybinds(ButtonPressedEventArgs e)
        {
            if (e.Button == Config.ReloadKeybind)
            {
                HotReload("", null);
                Helper.Input.Suppress(e.Button);
            }

            string oldOutput = Game1.debugOutput;
            if (e.Button == Config.WarpKeybind)
            {
                Log("Warping", LogLevel.Debug);

                var debugCommand = Game1.player.currentLocation.Name == "Town"
                    ? "warp FarmHouse"
                    : "warp Town";
                Game1.game1.parseDebugInput(debugCommand);
                
                // show result
                Log(Game1.debugOutput != oldOutput
                    ? $"> {Game1.debugOutput}"
                    : $"Sent debug command '{debugCommand}' to the game, but there was no output.", LogLevel.Info);

                Helper.Input.Suppress(e.Button);
            }

            if (e.Button == Config.OpenConfigKeybind)
            {
                configMenu.OpenModMenu(ModManifest);
                Helper.Input.Suppress(e.Button);
            }

            if (e.Button == Config.StatusKeybind)
            {
                LogModState();
                Helper.Input.Suppress(e.Button);
            }
        }


        /// <summary>
        /// When input is received, check that the player is free and used an action button
        /// If so, attempt open the shop if it exists
        ///
        /// From STF/ChroniclerCherry
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void STF_Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            //context and button check
            if (!Context.CanPlayerMove)
                return;

            //Resets the boolean I use to check if a menu used to move the player around came from my mod
            //and lets me return them to their original location
            SourceLocation = null;
            _playerPos = Vector2.Zero;

            if (Constants.TargetPlatform == GamePlatform.Android)
            {
                if (e.Button != SButton.MouseLeft)
                    return;
                if (e.Cursor.GrabTile != e.Cursor.Tile)
                    return;

                if (VerboseLogging) Log("Input detected!", LogLevel.Trace);
            }
            else if (!e.Button.IsActionButton())
                return;

            Vector2 clickedTile = Helper.Input.GetCursorPosition().GrabTile;

            //check if there is a tile property on Buildings layer
            IPropertyCollection tileProperty = TileUtility.GetTileProperty(Game1.currentLocation, "Buildings", clickedTile);

            if (tileProperty == null)
                return;

            //if there is a tile property, attempt to open shop if it exists
            CheckForShopToOpen(tileProperty,e);
        }

        /// <summary>
        /// Checks the tile property for shops, and open them
        /// </summary>
        /// <param name="tileProperty"></param>
        /// <param name="e"></param>
        private void CheckForShopToOpen(IPropertyCollection tileProperty, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            //check if there is a Shop property on clicked tile
            tileProperty.TryGetValue("Shop", out var shopProperty);
            if (VerboseLogging) Log($"Shop Property value is: {shopProperty}", LogLevel.Trace);
            if (shopProperty != null) //There was a `Shop` property so attempt to open shop
            {
                //check if the property is for a vanilla shop, and gets the shopmenu for that shop if it exists
                IClickableMenu menu = TileUtility.CheckVanillaShop(shopProperty, out bool warpingShop);
                if (menu != null)
                {
                    if (warpingShop)
                    {
                        SourceLocation = Game1.currentLocation;
                        _playerPos = Game1.player.position.Get();
                    }

                    //stop the click action from going through after the menu has been opened
                    helper.Input.Suppress(e.Button);
                    Game1.activeClickableMenu = menu;

                }
                else //no vanilla shop found
                {
                    //Extract the tile property value
                    string ShopKey = shopProperty.ToString();

                    if (ShopManager.GrangeShops.ContainsKey(ShopKey))
                    {
                        //stop the click action from going through after the menu has been opened
                        helper.Input.Suppress(e.Button);
                        ShopManager.GrangeShops[ShopKey].DisplayShop();
                    }
                    else
                    {
                        Log($"A Shop tile was clicked, but a shop by the name \"{ShopKey}\" " +
                            $"was not found.", LogLevel.Debug);
                    }
                }
            }
            else //no shop property found
            {
                tileProperty.TryGetValue("AnimalShop", out shopProperty); //see if there's an AnimalShop property
                if (shopProperty != null) //no animal shop found
                {
                    string ShopKey = shopProperty.ToString();
                    if (ShopManager.AnimalShops.ContainsKey(ShopKey))
                    {
                        //stop the click action from going through after the menu has been opened
                        helper.Input.Suppress(e.Button);
                        ShopManager.AnimalShops[ShopKey].DisplayShop();
                    }
                    else
                    {
                        Log($"An Animal Shop tile was clicked, but a shop by the name \"{ShopKey}\" " +
                            $"was not found.", LogLevel.Debug);
                    }
                }

            } //end shopProperty null check
        }
        
        private void OnLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log($"OnLaunched: {Game1.ticks}", LogLevel.Trace);

            Config = Helper.ReadConfig<ModConfig>();
            
            setupGMCM();
            ContentPatcherApi =
                Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            ContentPatcherApi.RegisterToken(ModManifest, "IsMarketDay",
                () => { return Context.IsWorldReady ? new[] {IsMarketDay() ? "true" : "false"} : null; });
            ContentPatcherApi.RegisterToken(ModManifest, "ShopPositions", ShopPositions);
            
            GMMJosephPresent = this.Helper.ModRegistry.IsLoaded("Elaho.JosephsSeedShopDisplayCP");
            GMMPaisleyPresent = this.Helper.ModRegistry.IsLoaded("Elaho.PaisleysBridalBoutiqueCP");
            
            Log($"OnLaunched: complete at {Game1.ticks}", LogLevel.Trace);

        }

        /// <summary>
        /// On game launched initialize all the shops and register all external APIs
        ///
        /// From STF/ChroniclerCherry
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLaunched_STFRegistrations(object sender, GameLaunchedEventArgs e)
        {
            APIs.RegisterJsonAssets();
            if (APIs.JsonAssets != null)
                APIs.JsonAssets.AddedItemsToShop += JsonAssets_AddedItemsToShop;

            APIs.RegisterExpandedPreconditionsUtility();
            APIs.RegisterBFAV();
            APIs.RegisterFAVR();
        }

        private static void OnLaunched_STFInit(object sender, GameLaunchedEventArgs e)
        {
            
            // some hooks for STF
            ShopManager.LoadContentPacks();
            
            Translations.UpdateSelectedLanguage();
            ShopManager.UpdateTranslations();

            ItemsUtil.UpdateObjectInfoSource();

            ShopManager.InitializeShops();
            ShopManager.InitializeItemStocks();

            ItemsUtil.RegisterItemsToRemove();
        }
        
        private void JsonAssets_AddedItemsToShop(object sender, System.EventArgs e)
        {
            if (Game1.activeClickableMenu is ShopMenu shop)
            {
                shop.setItemPriceAndStock(ItemsUtil.RemoveSpecifiedJAPacks(shop.itemPriceAndStock));
            }
        }

        private void setupGMCM() {
            configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Unregister(ModManifest);
            configMenu.Register(ModManifest, () => Config = new ModConfig(), SaveConfig);
            configMenu.OnFieldChanged(ModManifest, GMCMFieldChanged);

            configMenu.SetTitleScreenOnlyForNextOptions(ModManifest, false);

            configMenu.AddSectionTitle(ModManifest,
                () => Helper.Translation.Get("cfg.main-settings"));
            
            configMenu.AddNumberOption(ModManifest,
                () => Config.DayOfWeek,
                val => Config.DayOfWeek = val,
                () => Helper.Translation.Get("cfg.day-of-week"),
                () => Helper.Translation.Get("cfg.day-of-week.msg"),
                min: 0,
                max: 6,
                fieldId: "fm_DayOfWeek"
            );

            configMenu.AddBoolOption(ModManifest,
                () => Config.OpenInRain,
                val => Config.OpenInRain = val,
                () => Helper.Translation.Get("cfg.open-in-rain"),
                () => Helper.Translation.Get("cfg.open-in-rain.msg"),
                "fm_OpenInRain"
            );
            
            configMenu.AddBoolOption(ModManifest,
                () => Config.OpenInSnow,
                val => Config.OpenInSnow = val,
                () => Helper.Translation.Get("cfg.open-in-snow"),
                () => Helper.Translation.Get("cfg.open-in-snow.msg"),
                "fm_OpenInSnow"
            );
            
            configMenu.AddNumberOption(ModManifest,
                () => Config.OpeningTime,
                val => Config.OpeningTime = val,
                () => Helper.Translation.Get("cfg.opening-time"),
                () => Helper.Translation.Get("cfg.opening-time.msg"),
                min: 6,
                max: 26
            );
            
            configMenu.AddNumberOption(ModManifest,
                () => Config.ClosingTime,
                val => Config.ClosingTime = val,
                () => Helper.Translation.Get("cfg.closing-time"),
                () => Helper.Translation.Get("cfg.closing-time.msg"),
                min: 6,
                max: 26
            );
            
            configMenu.AddNumberOption(ModManifest,
                () => Config.ShopLayout,
                val => Config.ShopLayout = val,
                () => Helper.Translation.Get("cfg.shop-layout"),
                () => Helper.Translation.Get("cfg.shop-layout.msg"),
                0,
                10,
                fieldId: "fm_ShopLayout"
            );
            
            configMenu.AddBoolOption(ModManifest,
                () => Config.GMMCompat,
                val => Config.GMMCompat = val,
                () => Helper.Translation.Get("cfg.gmm-compat"),
                () => Helper.Translation.Get("cfg.gmm-compat.msg"),
                fieldId: "fm_GMMCompat"
            );

            configMenu.AddNumberOption(ModManifest,
                () => Config.RestockItemsPerHour,
                val => Config.RestockItemsPerHour = val,
                () => Helper.Translation.Get("cfg.restock-per-hour"),
                () => Helper.Translation.Get("cfg.restock-per-hour.msg"),
                0,
                9
            );
            
            configMenu.AddNumberOption(ModManifest,
                () => Config.StallVisitChance,
                val => Config.StallVisitChance = val,
                () => Helper.Translation.Get("cfg.npc-stall-visit-chance"),
                () => Helper.Translation.Get("cfg.npc-stall-visit-chance"),
                min: 0f,
                max: 1f
            );

            configMenu.AddSectionTitle(ModManifest,
                () => Helper.Translation.Get("cfg.debug-settings"));

            configMenu.AddBoolOption(ModManifest,
                () => Config.PeekIntoChests,
                val => Config.PeekIntoChests = val,
                () => Helper.Translation.Get("cfg.peek-into-chests"),
                () => Helper.Translation.Get("cfg.peek-into-chests.msg")
            );

            configMenu.AddBoolOption(ModManifest,
                () => Config.RuinTheFurniture,
                val => Config.RuinTheFurniture = val,
                () => Helper.Translation.Get("cfg.ruin-furniture"),
                () => Helper.Translation.Get("cfg.ruin-furniture.msg")
            );
            
            configMenu.AddBoolOption(ModManifest,
                () => Config.NPCVisitors,
                val => Config.NPCVisitors = val,
                () => Helper.Translation.Get("cfg.npc-visitors"),
                () => Helper.Translation.Get("cfg.npc-visitors.msg")
            );

            configMenu.AddBoolOption(ModManifest,
                () => Config.VerboseLogging,
                val => Config.VerboseLogging = val,
                () => Helper.Translation.Get("cfg.verbose-logging"),
                () => Helper.Translation.Get("cfg.verbose-logging.msg")
            );
            
            configMenu.AddBoolOption(ModManifest,
                () => Config.DebugKeybinds,
                val => Config.DebugKeybinds = val,
                () => Helper.Translation.Get("cfg.debug-keybinds"),
                () => Helper.Translation.Get("cfg.debug-keybinds.msg")
            );
            
            configMenu.AddKeybind(ModManifest,
                () => Config.OpenConfigKeybind,
                val => Config.OpenConfigKeybind = val,
                () => Helper.Translation.Get("cfg.open-config"),
                () => ""
            );
            
            configMenu.AddKeybind(ModManifest,
                () => Config.ReloadKeybind,
                val => Config.ReloadKeybind = val,
                () => Helper.Translation.Get("cfg.reload"),
                () => ""
            );
            
            configMenu.AddKeybind(ModManifest,
                () => Config.WarpKeybind,
                val => Config.WarpKeybind = val,
                () => Helper.Translation.Get("cfg.warp"),
                () => ""
            );
            
            // configMenu.AddKeybind(ModManifest,
            //     () => Config.StatusKeybind,
            //     val => Config.StatusKeybind = val,
            //     () => Helper.Translation.Get("cfg.status"),
            //     () => ""
            // );
        }

        private void SaveConfig() {
            Helper.WriteConfig(Config);
            if (! GMCMChangesSynced) SyncAfterGMCMChanges();
        }
        
        internal static bool IsMarketDay()
        {
            var md = Game1.dayOfMonth % 7 == Config.DayOfWeek
                     && !StardewValley.Utility.isFestivalDay(Game1.dayOfMonth, Game1.currentSeason);
            md = md && (Config.OpenInRain || !Game1.isRaining);
            md = md && (Config.OpenInSnow || !Game1.isSnowing);
            return md;
        }

        internal static Dictionary<string, List<int>> ShopLayouts = new()
        {
            ["0 Shops"] = new List<int> {},
            ["1 Shop"] = new List<int>  {1},
            ["2 Shops"] = new List<int> {1, 6},
            ["3 Shops"] = new List<int> {1, 7, 8},
            ["4 Shops"] = new List<int> {1, 5, 6, 8},
            ["5 Shops"] = new List<int> {1, 5, 6, 7, 8},
            ["6 Shops"] = new List<int> {1, 2, 6, 7, 8, 9},
            ["7 Shops"] = new List<int> {1, 2, 5, 6, 7, 8, 9},
            ["8 Shops"] = new List<int> {0, 1, 2, 5, 6, 7, 8, 9}, 
            ["9 Shops"] = new List<int> {0, 1, 2, 3, 5, 6, 7, 8, 9},
            ["10 Shops"] = new List<int> {0, 1, 2, 3, 4, 5, 6, 7, 8, 9},
        };

        private static bool isGMMDay()
        {
            return Game1.dayOfMonth is 6 or 7 or 20 or 21;
        }
        
        internal static string[] ShopPositions()
        {
            if (!Context.IsWorldReady) return null;
            var key = $"{Config.ShopLayout} Shops";
            if (!ShopLayouts.ContainsKey(key)) key = "6 Shops";
            if (!ShopLayouts.TryGetValue(key, out var layout)) return null;

            if (Config.GMMCompat && isGMMDay())
            {
                if (GMMPaisleyPresent) layout.Remove(5);
                if (GMMJosephPresent) layout.Remove(2);
                if (GMMJosephPresent) layout.Remove(7);
            }
            
            return layout.Select(i => $"Shop{i}").ToArray();
        }

        private static string Get(string key, object tokens)
        {
            return helper.Translation.Get(key, tokens);
        }

        internal static void Log(string message, LogLevel level, bool VerboseOnly = false)
        {
            if (VerboseOnly && Config is not null && !Config.VerboseLogging) return;
            if (!Context.IsWorldReady)
                monitor.Log($"{message}", level);
            else
                monitor.Log($"[{Game1.player.Name}] {message}", level);
        }

    }
}