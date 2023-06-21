using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using ServerSync;
using System.Reflection.Emit;
using Debug = UnityEngine.Debug;

namespace VBQOL
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    class VBQOL : BaseUnityPlugin
    {
        private const string ModName = "VBQOL";
        private const string ModVersion = "0.0.4";
        private const string ModGUID = "VBQOL";
        private Harmony harmony = new(ModGUID);

        #region ConfigOptions
        internal static VBQOL self;
    //    internal Assembly assembly;
        private static ConfigEntry<bool> serverConfigLocked = null!;
    /*    private static readonly string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        DateTime LastConfigChange;

        public static readonly ManualLogSource CreatureManagerModTemplateLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);*/

        private static readonly ConfigSync configSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = self.Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        #endregion

        #region use equipment in water
        public static ConfigEntry<bool> modEnabledUEIWConfig = null!;
        public static ConfigEntry<FilterMode> filterModeConfig = null!;
        public static ConfigEntry<string> itemBlacklistConfig = null!;
        public static ConfigEntry<string> itemWhitelistConfig = null!;

        /*    internal static bool modEnabledUEIW;
            internal static FilterMode filterMode;
            internal static string itemBlacklist;
            internal static string itemWhitelist;*/

        public static List<string> itemBlacklistStrings = new();
        public static List<string> itemWhitelistStrings = new();
        public enum FilterMode
        {
            Blacklist = 0,
            Whitelist = 1
        }
        #endregion

        #region BuildDamage
        public static ConfigEntry<bool> enableModBDConfig;
        public static ConfigEntry<float> creatorDamageMultConfig;
        public static ConfigEntry<float> nonCreatorDamageMultConfig;
        public static ConfigEntry<float> uncreatedDamageMultConfig;
        public static ConfigEntry<float> naturalDamageMultConfig;

        /*   internal static bool enableModBD;
           internal static float creatorDamageMult;
           internal static float nonCreatorDamageMult;
           internal static float uncreatedDamageMult;
           internal static float naturalDamageMult;*/
        #endregion

        #region DayCycle
        public static long vanillaDayLengthSec;
        public static ConfigEntry<bool> enableModDCConfig;
        //  internal static bool enableModDC;
        #endregion

        #region FirePlaceUtilites
        public static Dictionary<string, int> customBurnDict = new Dictionary<string, int>();
        public static string timeOfDay = "Day";
        public static List<string> notAllowed;
        public static KeyCode configBurnKey;
        public static KeyCode configPOKey;
        public static KeyCode returnKey;
        public static KeyCode timeToggleKey;

        public static ConfigEntry<bool> enableModFPUConfig;
        public static ConfigEntry<bool> burnItemsConfig;
        public static ConfigEntry<bool> extinguishItemsConfig;
        public static ConfigEntry<bool> disableTorchesConfig;
        public static ConfigEntry<bool> returnFuelConfig;
        public static ConfigEntry<bool> torchUseCoalConfig;
        public static ConfigEntry<bool> customBurnTimesConfig;
        public static ConfigEntry<bool> torchBurnConfig;
        public static ConfigEntry<bool> giveCoalConfig;
        public static ConfigEntry<string> blacklistBurnConfig;
        public static ConfigEntry<string> burnItemStringConfig;
        public static ConfigEntry<float> coalAmountConfig;
        public static ConfigEntry<string> keyBurnCodeStringConfig;
        public static ConfigEntry<string> keyBurnTextStringConfig;
        public static ConfigEntry<string> extinguishStringConfig;
        public static ConfigEntry<string> igniteStringConfig;
        public static ConfigEntry<string> keyPOCodeStringConfig;
        public static ConfigEntry<string> keyPOTextStringConfig;
        public static ConfigEntry<string> returnStringConfig;
        public static ConfigEntry<string> returnCodeStringConfig;
        public static ConfigEntry<string> returnTextStringConfig;
        public static ConfigEntry<string> timeToggleStringConfig;
        public static ConfigEntry<string> timeToggleOffStringConfig;
        public static ConfigEntry<string> timeToggleCodeStringConfig;
        public static ConfigEntry<string> timeToggleTextStringConfig;
        public static ConfigEntry<int> firepitBurnTimeConfig;
        public static ConfigEntry<int> groundtorchwoodBurnTimeConfig;
        public static ConfigEntry<int> bonfireBurnTimeConfig;
        public static ConfigEntry<int> hearthBurnTimeConfig;
        public static ConfigEntry<int> walltorchBurnTimeConfig;
        public static ConfigEntry<int> groundtorchironBurnTimeConfig;
        public static ConfigEntry<int> groundtorchgreenBurnTimeConfig;
        public static ConfigEntry<int> braziercBurnTimeConfig;

    /*    internal static bool enableModFPU;
        internal static bool burnItems;
        internal static bool extinguishItems;
        internal static bool disableTorches;
        internal static bool returnFuel;
        internal static bool torchUseCoal;
        internal static bool customBurnTimes;
        internal static bool torchBurn;
        internal static bool giveCoal;
        internal static string blacklistBurn;
        internal static string burnItemString;
        internal static float coalAmount;
        internal static string keyBurnCodeString;
        internal static string keyBurnTextString;
        internal static string extinguishString;
        internal static string igniteString;
        internal static string keyPOCodeString;
        internal static string keyPOTextString;
        internal static string returnString;
        internal static string returnCodeString;
        internal static string returnTextString;
        internal static string timeToggleString;
        internal static string timeToggleOffString;
        internal static string timeToggleCodeString;
        internal static string timeToggleTextString;
        internal static int firepitBurnTime;
        internal static int groundtorchwoodBurnTime;
        internal static int bonfireBurnTime;
        internal static int hearthBurnTime;
        internal static int walltorchBurnTime;
        internal static int groundtorchironBurnTime;
        internal static int groundtorchgreenBurnTime;
        internal static int braziercBurnTime;*/
        #endregion

        #region BetterPickupNotifications
        public static ConfigEntry<float> MessageLifetime;
        public static ConfigEntry<float> MessageFadeTime;
        public static ConfigEntry<float> MessageBumpTime;
        public static ConfigEntry<bool> ResetMessageTimerOnDupePickup;
        public static ConfigEntry<float> MessageVerticalSpacingModifier;
        public static ConfigEntry<float> MessageTextHorizontalSpacingModifier;
        public static ConfigEntry<float> MessageTextVerticalModifier;
        public static ConfigEntry<bool> enableModBPN;
        #endregion

        #region QuickTeleport
        public static ConfigEntry<bool> enableModQT;
        #endregion

        #region AddAllFuel
    //    private static readonly bool _debug = true;

     //   static ManualLogSource _logger;

        public static ConfigEntry<bool> ModEnabledAAFConfig;
        public static ConfigEntry<bool> ExcludeFinewoodConfig;
        public static ConfigEntry<KeyCode> ModifierKeyConfig;

    /*    internal static bool ModEnabledAAF;
        internal static bool ExcludeFinewood;
        internal static KeyCode ModifierKey;*/
        #endregion

        #region AutoFeed
        public static ConfigEntry<bool> modEnabledAFConfig;
        public static ConfigEntry<bool> isOnConfig;
        public static ConfigEntry<float> containerRangeConfig;
        public static ConfigEntry<float> moveProximityConfig;
        public static ConfigEntry<string> feedDisallowTypesConfig;
        public static ConfigEntry<string> animalDisallowTypesConfig;
        public static ConfigEntry<string> toggleKeyConfig;
        public static ConfigEntry<string> toggleStringConfig;
        public static ConfigEntry<bool> requireMoveConfig;
        public static ConfigEntry<bool> requireOnlyFoodConfig;
        public static float lastFeed;
        public static int feedCount;

        /*   internal static bool modEnabledAF;
           internal static bool isOn;
           internal static float containerRange;
           internal static float moveProximity;
           internal static string feedDisallowTypes;
           internal static string animalDisallowTypes;
           internal static string toggleKey;
           internal static string toggleString;
           internal static bool requireMove;
           internal static bool requireOnlyFood;*/
        #endregion

        private void Awake()
        {
            #region ConfigOptions
            self = this;
            Config.SaveOnConfigSet = false;
            serverConfigLocked = config("1 - General", "Lock Configuration", true, new ConfigDescription("Если включено, конфигурация заблокирована и может быть изменена только администраторами сервера."), true);
            configSync.AddLockingConfigEntry(serverConfigLocked);
            #endregion

            #region use equipment in water
            modEnabledUEIWConfig = config("2 - Equipment in Water", "Enable Section", true, "Enabled section Use Equip in Water");

            filterModeConfig = config("2 - Equipment in Water", "Filter Mode", FilterMode.Blacklist, "Choose the Method of which to Filter Items used in Water");
            itemBlacklistConfig = config("2 - Equipment in Water", "Items Blacklisted in Water", "", new ConfigDescription("List of Prefab names to Blacklist from the Player being able to use while Swimming. Separated by a sign - ;"));
            itemBlacklistConfig.SettingChanged += (_, _) => itemBlacklistStrings = itemBlacklistConfig.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            itemBlacklistStrings = itemBlacklistConfig.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            itemWhitelistConfig = config("2 - Equipment in Water", "Items Whitelisted in Water", "", new ConfigDescription("List of Prefab names to Whitelist the Player to e able to use while Swimming. Separated by a sign - ;"));
            itemWhitelistConfig.SettingChanged += (_, _) => itemWhitelistStrings = itemWhitelistConfig.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            itemWhitelistStrings = itemWhitelistConfig.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

      //      Harmony harmony = new(ModGUID);

            harmony.Patch(
                AccessTools.Method(typeof(Player), "Update"),
                transpiler: new HarmonyMethod(typeof(EquipmentInWater.HS_EquipInWaterPatches), nameof(EquipmentInWater.HS_EquipInWaterPatches.HS_PatchPlayerUpdateWaterCheck))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Humanoid), "EquipItem"),
                transpiler: new HarmonyMethod(typeof(EquipmentInWater.HS_EquipInWaterPatches), nameof(EquipmentInWater.HS_EquipInWaterPatches.HS_InjectWaterItemCheck))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Humanoid), "UpdateEquipment"),
                transpiler: new HarmonyMethod(typeof(EquipmentInWater.HS_EquipInWaterPatches), nameof(EquipmentInWater.HS_EquipInWaterPatches.HS_PatchFixedUpdatedWaterCheck))
            );
            #endregion

            #region BuildDamage
            enableModBDConfig = config("3 - BuildDamage", "Enable Section", true, new ConfigDescription("Enable or disable this section"), true);
            if (!enableModBDConfig.Value)
                return;
            creatorDamageMultConfig = config("3 - BuildDamage", "CreatorDamageMult", 0.75f, new ConfigDescription("Multiply damage by creators by this much"), true);
            nonCreatorDamageMultConfig = config("3 - BuildDamage", "NonCreatorDamageMult", 0.05f, new ConfigDescription("Multiply damage by non-creators by this much"), true);
            uncreatedDamageMultConfig = config("3 - BuildDamage", "UncreatedDamageMult", 0.75f, new ConfigDescription("Multiply damage to uncreated buildings by this much"), true);
            naturalDamageMultConfig = config("3 - BuildDamage", "NaturalDamageMult", 0.75f, new ConfigDescription("Multiply the damage from natural deterioration of buildings or damage from mobs by this value"), true);
            #endregion

            #region DayCycle
            enableModDCConfig = config("4 - DayCycle", "Enable Section", true, new ConfigDescription("Enable or disable this section"), true);
            if (!enableModDCConfig.Value)
                return;
            #endregion

            #region FirePlaceUtilites
            enableModFPUConfig = config("5.0 - FPU - General", "Enable Section", true, new ConfigDescription("Enable or disable this Section"), true);
            if (!enableModFPUConfig.Value)
            {
                return;
            }
            burnItemsConfig = config("5.1 - FPU - Toggles", "Burn Items In Fire", true, new ConfigDescription("Allows you to burn items in fires"), true);
            extinguishItemsConfig = config("5.1 - FPU - Toggles", "Extinguish Fires", true, new ConfigDescription("Allows you to turn fires off/on"), true);
            disableTorchesConfig = config("5.1 - FPU - Toggles", "Disable Fires During The Day", false, new ConfigDescription("Allows you to make fires turn off during the day, you must press a key on each item to let it toggle"), true);
            returnFuelConfig = config("5.1 - FPU - Toggles", "Return Fuel", false, new ConfigDescription("Allows you to press a key to return the fuel left in a fire"), true);
            torchUseCoalConfig = config("5.1 - FPU - Toggles", "Torch and Sconce Use Coal", false, new ConfigDescription("Makes the Wood/Iron Torch and Sconce use Coal as fuel instead of resin"), true);
            customBurnTimesConfig = config("5.1 - FPU - Toggles", "Custom Burn Times", false, new ConfigDescription("Enable custom burn times for all fireplaces, the default values are the games vanilla values"), true);

            torchBurnConfig = config("5.2 - FPU - Burn Items In Fire", "Burn In Torches", false, new ConfigDescription("Allows items to be burnt in ground torches, wall torches or braziers"), true);
            giveCoalConfig = config("5.2 - FPU - Burn Items In Fire", "Give Coal", true, new ConfigDescription("Returns coal when burning an item"), true);
            blacklistBurnConfig = config("5.2 - FPU - Burn Items In Fire", "Blacklist Items", "$item_wood", new ConfigDescription("Items that aren't allowed to be burned. Seperate items by a comma. Wood should remain as a default so that way it doesn't take your wood twice when lighting a fire, if you have a mod that allows other wood types to burn, put them on this list."), true);
            burnItemStringConfig = config("5.2 - FPU - Burn Items In Fire", "Burn Item Text", "Сжечь предмет", new ConfigDescription("The text to show when hovering over the fire"), false);
            coalAmountConfig = config("5.2 - FPU - Burn Items In Fire", "Coal Amount", 1f, new ConfigDescription("Amount of coal to give when burning an item"), true);
            keyBurnCodeStringConfig = config("5.2 - FPU - Burn Items In Fire", "Burn Key", "LeftShift", new ConfigDescription("The key to use in combination with the hotkeys. KeyCodes can be found here https://docs.unity3d.com/ScriptReference/KeyCode.html"), false);
            keyBurnTextStringConfig = config("5.2 - FPU - Burn Items In Fire", "Burn Key Text", "L.Shift", new ConfigDescription("The custom text to show for the string, if you set it to \"none\" then it'll use what you have in the 'Key' config option."), false);

            extinguishStringConfig = config("5.3 - FPU - Extinguish Fires", "Extinguish Fire Text", "Тушить огонь", new ConfigDescription("The text to show when hovering over the fire"), false);
            igniteStringConfig = config("5.3 - FPU - Extinguish Fires", "Ignite Fire Text", "Разжечь огонь", new ConfigDescription("The text to show when hovering over the fire if the fire is extinguished"), false);
            keyPOCodeStringConfig = config("5.3 - FPU - Extinguish Fires", "Put Out Fire Key", "LeftAlt", new ConfigDescription("The key to use to put out a fire. KeyCodes can be found here https://docs.unity3d.com/ScriptReference/KeyCode.html"), false);
            keyPOTextStringConfig = config("5.3 - FPU - Extinguish Fires", "Put Out Fire Key Text", "L.Alt", new ConfigDescription("The custom text to show for the string, if you set it to \"none\" then it'll use what you have in the 'Key' config option."), false);

            returnStringConfig = config("5.4 - FPU - Return Fuel", "Return Fuel Text", "Вернуть топливо", new ConfigDescription("The text to show when hovering over the fire"), false);
            returnCodeStringConfig = config("5.4 - FPU - Return Fuel", "Return Fuel Key", "LeftControl", new ConfigDescription("The key to use to return the fuel. KeyCodes can be found here https://docs.unity3d.com/ScriptReference/KeyCode.html"), false);
            returnTextStringConfig = config("5.4 - FPU - Return Fuel", "Return Fuel Key Text", "L.Ctrl", new ConfigDescription("The custom text to show for the string, if you set it to \"none\" then it'll use what you have in the 'Key' config option."), false);

            timeToggleStringConfig = config("5.5 - FPU - Disable Fires During The Day", "Time Toggle On Text", "Вкл Таймер", new ConfigDescription("The text to show when hovering over the fire to enable the timer"), false);
            timeToggleOffStringConfig = config("5.5 - FPU - Disable Fires During The Day", "Time Toggle Off Text", "Выкл Таймер", new ConfigDescription("The text to show when hovering over the fire to disable the timer"), false);
            timeToggleCodeStringConfig = config("5.5 - FPU - Disable Fires During The Day", "Time Toggle Key", "Equals", new ConfigDescription("The key to use to return the fuel. KeyCodes can be found here https://docs.unity3d.com/ScriptReference/KeyCode.html"), false);
            timeToggleTextStringConfig = config("5.5 - FPU - Disable Fires During The Day", "Time Toggle Key Text", "=", new ConfigDescription("The custom text to show for the string, if you set it to \"none\" then it'll use what you have in the 'Key' config option."), false);

            firepitBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Firepit", 5000, new ConfigDescription("Custom burntime for the standard firepit"), true);
            groundtorchwoodBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Wood Ground Torch", 10000, new ConfigDescription("Custom burntime for the wooden ground torch"), true);
            bonfireBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Bonfire", 5000, new ConfigDescription("Custom burntime for the bonfire"), true);
            hearthBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Hearth", 5000, new ConfigDescription("Custom burntime for the hearth"), true);
            walltorchBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Sconce", 20000, new ConfigDescription("Custom burntime for the sconce"), true);
            groundtorchironBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Iron Ground Torch", 20000, new ConfigDescription("Custom burntime for the iron ground torch"), true);
            groundtorchgreenBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Green Ground Torch", 20000, new ConfigDescription("Custom burntime for the green ground torch"), true);
            braziercBurnTimeConfig = config("5.6 - FPU - Custom Burn Times", "Brazier", 20000, new ConfigDescription("Custom burntime for the brazier"), true);
            if (customBurnTimesConfig.Value)
            {
                customBurnDict.Add("fire_pit", firepitBurnTimeConfig.Value);
                customBurnDict.Add("piece_groundtorch_wood", groundtorchwoodBurnTimeConfig.Value);
                customBurnDict.Add("bonfire", bonfireBurnTimeConfig.Value);
                customBurnDict.Add("hearth", hearthBurnTimeConfig.Value);
                customBurnDict.Add("piece_walltorch", walltorchBurnTimeConfig.Value);
                customBurnDict.Add("piece_groundtorch", groundtorchironBurnTimeConfig.Value);
                customBurnDict.Add("piece_groundtorch_green", groundtorchgreenBurnTimeConfig.Value);
                customBurnDict.Add("piece_brazierceiling01", braziercBurnTimeConfig.Value);
            }
            notAllowed = blacklistBurnConfig.Value.Replace(" ", "").Split(new char[]
            {
                ','
            }).ToList<string>();
            configBurnKey = (KeyCode)Enum.Parse(typeof(KeyCode), keyBurnCodeStringConfig.Value);
            configPOKey = (KeyCode)Enum.Parse(typeof(KeyCode), keyPOCodeStringConfig.Value);
            returnKey = (KeyCode)Enum.Parse(typeof(KeyCode), returnCodeStringConfig.Value);
            timeToggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), timeToggleCodeStringConfig.Value);
            #endregion

            #region BetterPickupNotifications
            enableModBPN = config("6 - BetterPickupNotifications", "Enable Section", true, new ConfigDescription("Enable or disable this section"), false);
            if (!enableModBPN.Value)
                return;
            MessageLifetime = config("6 - BetterPickupNotifications", "MessageLifetime", 4f, new ConfigDescription("How long a notification displays on the HUD before fading away"), false);
            MessageFadeTime = config("6 - BetterPickupNotifications", "MessageFadeTime", 2f, new ConfigDescription("How long a notification takes to fade away"), false);
            MessageBumpTime = config("6 - BetterPickupNotifications", "MessageBumpTime", 1f, new ConfigDescription("How much time to add to the life of a notification when picking up a duplicate item"), false);
            ResetMessageTimerOnDupePickup = config("6 - BetterPickupNotifications", "ResetMessageTimerOnDupePickup", false, new ConfigDescription("Resets a notification's timer to max lifetime when picking up a duplicate item"), false);
            MessageVerticalSpacingModifier = config("6 - BetterPickupNotifications", "MessageVerticalSpacingModifier", 1.25f, new ConfigDescription("How much to modify the vertical separation space between messages"), true);
            MessageTextHorizontalSpacingModifier = config("6 - BetterPickupNotifications", "MessageTextHorizontalSpacingModifier", 2f, new ConfigDescription("How much to modify the horizontal spacing between icon and text for messages"), false);
            MessageTextVerticalModifier = config("6 - BetterPickupNotifications", "MessageTextVerticalModifier", 1f, new ConfigDescription("How much to modify the vertical alignment of the text for messages"), false);
            #endregion

            #region QuickTeleport
            enableModQT = config("7 - QuickTeleport", "Enable Section", true, new ConfigDescription("Enable or disable this section"), false);
            if (!enableModQT.Value)
                return;
            #endregion

            #region AddAllFuel
            ModEnabledAAFConfig = config("8 - AddAllFuel", "Enabled Section", true, new ConfigDescription("Globally enable or disable this section."), true);
            ExcludeFinewoodConfig = config("8 - AddAllFuel", "excludeFinewood", true, new ConfigDescription("Filter finewood out of items to add to kilns"), true);
           // ModifierKeyConfig = config("8 - AddAllFuel", "ModifierKey", "RightShift", new ConfigDescription("The key to use in combination with the hotkeys. KeyCodes can be found here https://docs.unity3d.com/ScriptReference/KeyCode.html"), false);
            ModifierKeyConfig = config("8 - AddAllFuel", "ModifierKey", KeyCode.RightShift, new ConfigDescription("Modifier key to hold for using add all feature."), false);

            //harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: ModGUID);
            #endregion

            #region AutoFeed
            modEnabledAFConfig = config("9 - AutoFeed", "Enabled", true, new ConfigDescription("Enable this mod"), true);
            if (!modEnabledAFConfig.Value)
                return;
            isOnConfig = config("9 - AutoFeed", "IsOn", true, new ConfigDescription("Behaviour is currently on or not"), true);
            containerRangeConfig = config("9 - AutoFeed", "ContainerRange", 15f, new ConfigDescription("Container range in metres."), true);
            feedDisallowTypesConfig = config("9 - AutoFeed", "FeedDisallowTypes", "", new ConfigDescription("Types of item to disallow as feed, comma-separated."), true);
            animalDisallowTypesConfig = config("9 - AutoFeed", "AnimalDisallowTypes", "", new ConfigDescription("Types of creature to disallow to feed, comma-separated."), true);
            requireMoveConfig = config("9 - AutoFeed", "RequireMove", true, new ConfigDescription("Require animals to move to container to feed."), true);
            requireOnlyFoodConfig = config("9 - AutoFeed", "RequireOnlyFood", false, new ConfigDescription("Don't allow feeding from containers that have items that the animal will not eat as well."), true);
            moveProximityConfig = config("9 - AutoFeed", "MoveProximity", 2f, new ConfigDescription("How close to move towards the container if RequireMove is true."), true);
            toggleKeyConfig = config("9 - AutoFeed", "ToggleKey", "", new ConfigDescription("Key to toggle behaviour. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html"), false);
            toggleStringConfig = config("9 - AutoFeed", "ToggleString", "Автоподача: {0}", new ConfigDescription("Text to show on toggle. {0} is replaced with true/false"), false);


            #endregion

            #region ConfigOptions
            /*   
                SetupWatcherOnConfigFile();
                Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
                Config.SaveOnConfigSet = true;
                SetupWatcher();*/

            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Harmony.CreateAndPatchAll(typeof(BlastFurnaceTalesAll));

            Config.SaveOnConfigSet = true;
            Config.Save();
            #endregion
        }

        #region EquipmentInWater
        #endregion

        #region QuickTeleport
        #endregion

        #region AutoFeed
        #endregion

        #region ConfigOptions
    /*    private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }
        public void SetupWatcherOnConfigFile()
        {
            FileSystemWatcher fileSystemWatcherOnConfig = new(Paths.ConfigPath, ConfigFileName);
            fileSystemWatcherOnConfig.Changed += ConfigChanged;
            fileSystemWatcherOnConfig.IncludeSubdirectories = true;
            fileSystemWatcherOnConfig.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcherOnConfig.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                CreatureManagerModTemplateLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                CreatureManagerModTemplateLogger.LogError($"There was an issue loading your {ConfigFileName}");
                CreatureManagerModTemplateLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
        private void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
                // Debug("Reloading Config...");
            }
            catch
            {
                // DebugError("Can't reload Config");
            }
        }
        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }
        private void UpdateConfiguration()
        {
            Task task = null;
            task = Task.Run(() =>
            {
                #region FirePlaceUtilites
                enableModFPU = enableModFPUConfig.Value;
                burnItems = burnItemsConfig.Value;
                extinguishItems = extinguishItemsConfig.Value;
                disableTorches = disableTorchesConfig.Value;
                returnFuel = returnFuelConfig.Value;
                torchUseCoal = torchUseCoalConfig.Value;
                customBurnTimes = customBurnTimesConfig.Value;
                torchBurn = torchBurnConfig.Value;
                giveCoal = giveCoalConfig.Value;
                blacklistBurn = blacklistBurnConfig.Value;
                burnItemString = burnItemStringConfig.Value;
                coalAmount = coalAmountConfig.Value;
                keyBurnCodeString = keyBurnCodeStringConfig.Value;
                keyBurnTextString = keyBurnTextStringConfig.Value;
                extinguishString = extinguishStringConfig.Value;
                igniteString = igniteStringConfig.Value;
                keyPOCodeString = keyPOCodeStringConfig.Value;
                keyPOTextString = keyPOTextStringConfig.Value;
                returnString = returnStringConfig.Value;
                returnCodeString = returnCodeStringConfig.Value;
                returnTextString = returnTextStringConfig.Value;
                timeToggleString = timeToggleStringConfig.Value;
                timeToggleOffString = timeToggleOffStringConfig.Value;
                timeToggleCodeString = timeToggleCodeStringConfig.Value;
                timeToggleTextString = timeToggleTextStringConfig.Value;
                firepitBurnTime = firepitBurnTimeConfig.Value;
                groundtorchwoodBurnTime = groundtorchwoodBurnTimeConfig.Value;
                bonfireBurnTime = bonfireBurnTimeConfig.Value;
                hearthBurnTime = hearthBurnTimeConfig.Value;
                walltorchBurnTime = walltorchBurnTimeConfig.Value;
                groundtorchironBurnTime = groundtorchironBurnTimeConfig.Value;
                groundtorchgreenBurnTime = groundtorchgreenBurnTimeConfig.Value;
                braziercBurnTime = braziercBurnTimeConfig.Value;
                #endregion

                #region BuildDamage
                enableModBD = enableModBDConfig.Value;
                creatorDamageMult = creatorDamageMultConfig.Value;
                nonCreatorDamageMult = nonCreatorDamageMultConfig.Value;
                uncreatedDamageMult = uncreatedDamageMultConfig.Value;
                naturalDamageMult = naturalDamageMultConfig.Value;
                #endregion

                #region DayCycle
                enableModDC = enableModDCConfig.Value;
                #endregion

                #region AddAllFuel
                ModEnabledAAF = ModEnabledAAFConfig.Value;
                ExcludeFinewood = ExcludeFinewoodConfig.Value;
                ModifierKey = ModifierKeyConfig.Value;
                #endregion

                #region AutoFeed
                modEnabledAF = modEnabledAFConfig.Value;
                isOn = isOnConfig.Value;
                containerRange = containerRangeConfig.Value;
                moveProximity = moveProximityConfig.Value;
                feedDisallowTypes = feedDisallowTypesConfig.Value;
                animalDisallowTypes = animalDisallowTypesConfig.Value;
                toggleKey = toggleKeyConfig.Value;
                toggleString = toggleStringConfig.Value;
                requireMove = requireMoveConfig.Value;
                requireOnlyFood = requireOnlyFoodConfig.Value;
                #endregion

                #region Equipment in Water
                modEnabledUEIW = modEnabledUEIWConfig.Value;
                filterMode = filterModeConfig.Value;
                itemBlacklist = itemBlacklistConfig.Value;
                itemWhitelist = itemWhitelistConfig.Value;
                #endregion
            });

        Task.WaitAll();
            // Debug("Configuration Received");
        }*/
        #endregion

        private void destroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
