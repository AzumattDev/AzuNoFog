using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace AzuNoFog
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class AzuNoFogPlugin : BaseUnityPlugin
    {
        internal const string ModName = "AzuNoFog";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource AzuNoFogLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            NoFog = config("1 - General", "No Fog", Toggle.On,
                "If on, the mod will disable fog.");
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
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
                AzuNoFogLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                AzuNoFogLogger.LogError($"There was an issue loading your {ConfigFileName}");
                AzuNoFogLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        internal static ConfigEntry<Toggle> NoFog = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            //var configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        #endregion
    }
    
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    public static class EnvManSetEnvPatch
    {
        private static void Prefix(ref EnvMan __instance, ref EnvSetup env)
        {
            if (AzuNoFogPlugin.NoFog.Value == AzuNoFogPlugin.Toggle.On)
            {
                env.m_fogDensityNight = 0f;
                env.m_fogDensityMorning = 0f;
                env.m_fogDensityDay = 0f;
                env.m_fogDensityEvening = 0f;
            }

            if (EnvMan.instance == null) return;
            GameObject.Find("_GameMain/_Environment/FollowPlayer/GroundMist").SetActive(AzuNoFogPlugin.NoFog.Value == AzuNoFogPlugin.Toggle.Off);
            GameObject.Find("_GameMain/_Environment/FollowPlayer/FogClouds").SetActive(AzuNoFogPlugin.NoFog.Value == AzuNoFogPlugin.Toggle.Off);
            GameObject.Find("_GameMain/_Environment/OceanMist").SetActive(AzuNoFogPlugin.NoFog.Value == AzuNoFogPlugin.Toggle.Off);
            GameObject.Find("_GameMain/_Environment/Distant_fog_planes").SetActive(AzuNoFogPlugin.NoFog.Value == AzuNoFogPlugin.Toggle.Off);
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetParticleArrayEnabled))]
    public static class EnvManSetParticleArrayEnabledPatch
    {
        private static void Postfix(ref MistEmitter __instance, GameObject[] psystems, bool enabled)
        {
            if (AzuNoFogPlugin.NoFog.Value == AzuNoFogPlugin.Toggle.On)
            {
                foreach (GameObject gameObject in psystems)
                {
                    MistEmitter componentInChildren = gameObject.GetComponentInChildren<MistEmitter>();
                    if (componentInChildren)
                    {
                        componentInChildren.enabled = false;
                    }
                }
            }
        }
    }
}