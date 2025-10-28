using StardewValley;
using StardewValley.GameData.Characters;

namespace MarketDay.Utility
{
    public static class NPCUtility
    {
        internal static bool IsChild(NPC npc)
        {
            if (npc is StardewValley.Characters.Child) return true; //should get vanilla player-children
            return npc.Age == (int)NpcAge.Child; //should get any remaining NPC children
        }
    }
}