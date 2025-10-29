using System;
using MarketDay.src.API;
using StardewModdingAPI;

namespace MarketDay.API
{
    /// <summary>
    /// This class is used to register external APIs and hold the instances of those APIs to be accessed
    /// by the rest of the mod
    /// </summary>
    class APIs
    {
        internal static IConditionsApi Conditions;

        /// <summary>
        /// Register the API for Expanded Preconditions Utility
        /// </summary>
        public static void RegisterExpandedPreconditionsUtility()
        {
            Conditions = MarketDay.helper.ModRegistry.GetApi<IConditionsApi>("Cherry.ExpandedPreconditionsUtility");

            if (Conditions == null)
            {
                MarketDay.Log("Expanded Preconditions Utility API not detected. Something went wrong, please check that your installation of Expanded Preconditions Utility is valid",
                    LogLevel.Error);
                return;
            }

            Conditions.Initialize(MarketDay.VerboseLogging, "ceruleandeep.MarketDay");

        }
    }
}
