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
        internal const string ModVersion = "1.0.1";
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

    [HarmonyPatch(typeof(UISlotInventoryStorage), nameof(UISlotInventoryStorage.OnPointerRightClick))]
    static class UIInventoryRightClickOnBackpackSlotPatch
    {
        static bool Prefix(UISlotInventoryStorage __instance, PointerEventData eventData)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return true;

            if (__instance.Item == null)
                return true;
            ClickToDestroyPlugin.ClickToDestroyLogger.LogInfo($"Destroying item");
            __instance.Item.StorageBelongTo.RemoveItem(__instance.Item);
            Global.code.uiInventory.Refresh();
            return false;
        }
    }
}