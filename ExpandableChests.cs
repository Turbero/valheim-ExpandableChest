using BepInEx;
using HarmonyLib;

namespace ExpandableChests
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ExpandableChests : BaseUnityPlugin
    {
        public const string GUID = "Turbero.ExpandableChests";
        public const string NAME = "Expandable Chests";
        public const string VERSION = "1.0.0";

        private readonly Harmony harmony = new Harmony(GUID);

        void Awake()
        {
            ConfigurationFile.LoadConfig(this);
            harmony.PatchAll();
        }

        void onDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
