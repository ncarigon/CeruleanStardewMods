using System;
using System.Collections.Generic;
using System.Linq;
using MailFrameworkMod;
using MarketDay.Data;
using StardewModdingAPI;
using StardewValley;
using Object = StardewValley.Object;

namespace MarketDay.Utility
{
    public class Mail
    {
        public static void Send(string mailKey, string text, string player=null, int whichBG=0, int TextColor=-1, PrizeLevel prize=null)
        {
            var serializeKey = $"Mail/{mailKey}";
            MarketDay.SetSharedValue($"{serializeKey}/Key", mailKey);
            MarketDay.SetSharedValue($"{serializeKey}/Text", text);
            MarketDay.SetSharedValue($"{serializeKey}/TextColor", TextColor);
            MarketDay.SetSharedValue($"{serializeKey}/BG", whichBG);
            if (player is not null)
            {
                MarketDay.SetSharedValue($"{serializeKey}/Player", player);
            }
            if (prize is not null)
            {
                MarketDay.SetSharedValue($"{serializeKey}/ObjName", prize.Object);
                MarketDay.SetSharedValue($"{serializeKey}/Flavor", prize.Flavor);
                MarketDay.SetSharedValue($"{serializeKey}/Stack", prize.Stack);
                MarketDay.SetSharedValue($"{serializeKey}/Quality", prize.Quality);
            }

            LoadOneMail(mailKey);
        }

        public static void LoadMails()
        {
            var serializedKey = $"{MarketDay.SMod.ModManifest.UniqueID}/Mail";
            foreach (var kvp in Game1.getFarm().modData.Pairs.Where(kvp => kvp.Key.StartsWith(serializedKey) && kvp.Key.EndsWith("/Key")))
            {
                LoadOneMail(kvp.Value);
            }
        }

        private static void LoadOneMail(string mailKey)
        {
            var deserializeKey = $"Mail/{mailKey}";
            var Text = MarketDay.GetSharedString($"{deserializeKey}/Text");
            var player = MarketDay.GetSharedString($"{deserializeKey}/Player");
            var TextColor = MarketDay.GetSharedValue($"{deserializeKey}/TextColor");
            var whichBG = MarketDay.GetSharedValue($"{deserializeKey}/BG");
            var ObjName = MarketDay.GetSharedString($"{deserializeKey}/ObjName");
            var Flavor = MarketDay.GetSharedString($"{deserializeKey}/Flavor");
            var Stack = MarketDay.GetSharedValue($"{deserializeKey}/Stack");
            var Quality = MarketDay.GetSharedValue($"{deserializeKey}/Quality");

            if (player is not null && Game1.player.Name != player) return;
            
            if (ObjName is not null)
            {
                MarketDay.Log($"Loading prize mail {mailKey}", LogLevel.Trace);
                var attachment = AttachmentForPrizeMail(ObjName, Flavor, Stack, Quality);
                MailRepository.SaveLetter(
                    new Letter(mailKey, Text, new List<Item> {attachment}, 
                        l => !Game1.player.mailReceived.Contains(l.Id), 
                        l => Game1.player.mailReceived.Add(l.Id),
                        whichBG
                    ){TextColor=TextColor}
                );
            }
            else
            {
                MarketDay.Log($"Loading non-prize mail {mailKey}", LogLevel.Trace);
                MailRepository.SaveLetter(
                    new Letter(mailKey, Text, 
                        l => !Game1.player.mailReceived.Contains(l.Id), 
                        l => Game1.player.mailReceived.Add(l.Id),
                        whichBG
                    ){TextColor=TextColor}
                );
            }
        }

        private static Object AttachmentForPrizeMail(string ObjName, string Flavor, int Stack, int Quality=0)
        {
            var idx = ItemsUtil.GetIndexByName(ObjName);
            if (idx?.Equals("-1") == true)
            {
                MarketDay.Log($"Could not find prize object {ObjName}", LogLevel.Error);
                idx = "169";
            }

            var stack = Math.Max(Stack, 1);
            var attachment = new Object(idx, stack);
            if (Quality is 0 or 1 or 2 or 4) attachment.Quality = Quality;
            if (Flavor is null || Flavor.Length <= 0) return attachment;
            
            var prIdx = ItemsUtil.GetIndexByName(Flavor);
            if (prIdx?.Equals("-1") == true)
            {
                MarketDay.Log($"Could not find flavor object {Flavor}", LogLevel.Error);
                prIdx = "258";
            }
            attachment.preservedParentSheetIndex.Value = prIdx;
            if (Enum.TryParse(ObjName, out Object.PreserveType pt))
            {
                attachment.preserve.Value = pt;
            }

            return attachment;
        }
    }
}