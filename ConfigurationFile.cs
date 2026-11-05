using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ServerSync;
using TMPro;

namespace ExpandableChests
{
    internal class ConfigurationFile
    {
        private static ConfigEntry<bool> _serverConfigLocked;
        public static ConfigEntry<bool> debug;
        
        public static ConfigEntry<bool> allowAddRow;
        public static ConfigEntry<bool> allowReset;
        public static ConfigEntry<int> maxExpandedRows;
        public static ConfigEntry<int> expandCost;

        public static ConfigEntry<string> translateAddRow;
        public static ConfigEntry<string> translateReset;
        public static ConfigEntry<string> translateContainerCapacityIncreased;
        public static ConfigEntry<string> translateContainerCapacityReset;
        public static ConfigEntry<string> translateContainerCapacityAtMinimumCapacity;
        public static ConfigEntry<string> translateMaxExpansionsReached;
        public static ConfigEntry<string> translateNotEnoughMaterials;

        private static ConfigFile configFile;
        private static readonly string ConfigFileName = ExpandableChests.GUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        
        private static readonly ConfigSync ConfigSync = new ConfigSync(ExpandableChests.GUID)
        {
            DisplayName = ExpandableChests.NAME,
            CurrentVersion = ExpandableChests.VERSION,
            MinimumRequiredVersion = ExpandableChests.VERSION
        };

        internal static void LoadConfig(BaseUnityPlugin plugin)
        {
            {
                configFile = plugin.Config;

                _serverConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
                _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
                debug = config("1 - General", "DebugMode", false, "Enabling/Disabling the debugging in the console (default = false)", false);

                allowAddRow = config("2 - Config", "Allow Expand Containers", true, "Enabling/Disabling increasing container sizes (default = true)");
                allowReset = config("2 - Config", "Allow Reset Containers Expansions", true, "Enabling/Disabling resetting expanded containers (default = true)");
                expandCost = config("2 - Config", "Expand Cost (%)", 50, "Expand Cost of the original cost in percentage (0 is free, 100 is same amount as original cost, 200 double cost, etc)");
                maxExpandedRows = config("2 - Config", "Max Expansions", 2, "Max Expanded Rows in allowed containers");
                
                translateAddRow = config("3 - Translations", "Add Row", "Add Row", "Translation for 'Add Row'");
                translateReset = config("3 - Translations", "Reset", "Reset", "Translation for 'Reset'");
                translateContainerCapacityIncreased = config("3 - Translations", "Container Capacity Increased", "Container Capacity Increased", "Translation for 'Container Capacity Increased'");
                translateContainerCapacityReset = config("3 - Translations", "Container Capacity Reset", "Container Capacity Reset", "Translation for 'Container Capacity Reset'");
                translateContainerCapacityAtMinimumCapacity = config("3 - Translations", "Container Capacity At Minimum Capacity", "Container Capacity At Minimum Capacity", "Translation for 'Container Capacity At Minimum Capacity'");
                translateMaxExpansionsReached = config("3 - Translations", "Max Expansions Reached", "Max Expansions Reached", "Translation for 'Max Expansions Reached'");
                translateNotEnoughMaterials = config("3 - Translations", "Not Enough Materials", "Not Enough Materials", "Translation for 'Not Enough Materials'");
                
                SetupWatcher();
            }
        }
        
        private static void SetupWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private static void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            Logger.Log("Attempting to reload configuration...");
            configFile.Reload();
            SettingsChanged(null, null);
        }

        private static void SettingsChanged(object sender, EventArgs e)
        {
            InventoryGui_Container_Patch.containerIncreaseBtnGO?.SetActive(allowAddRow.Value);
            InventoryGui_Container_Patch.containerResetBtnGO?.SetActive(allowReset.Value);
            if (InventoryGui_Container_Patch.containerIncreaseBtnGO != null)
                InventoryGui_Container_Patch.containerIncreaseBtnGO.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = translateAddRow.Value;
            if (InventoryGui_Container_Patch.containerResetBtnGO != null)
                InventoryGui_Container_Patch.containerResetBtnGO.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = translateReset.Value;
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = configFile.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
    }
}
