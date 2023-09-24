using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickToDestroy
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ClickToDestroyPlugin : BaseUnityPlugin

    {
        internal const string ModName = "ClickToDestroy";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource ClickToDestroyLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static ConfigEntry<KeyCode> modifierKey = null!;

        private void Awake()
        {
            modifierKey = Config.Bind("1 - General", "Modifier Key", KeyCode.LeftControl, "The key that must be held down to destroy items");

            _harmony.PatchAll();
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                ClickToDestroyLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                ClickToDestroyLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ClickToDestroyLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
    }

    [HarmonyPatch(typeof(UIInventory), nameof(UIInventory.RightClickOnBackpackSlot))]
    static class UIInventoryRightClickOnBackpackSlotPatch
    {
        static bool Prefix(UIInventory __instance, PointerEventData eventData, Item itemInSlot)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return true;
            if (!itemInSlot)
                return true;
            itemInSlot.StorageBelongTo.RemoveItem(itemInSlot);
            __instance.Refresh();
            return false;
        }
    }
}