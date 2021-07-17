using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace FortifySkills
{
    [BepInPlugin("net.merlyn42.fortifyskills", "FortifySkills", "1.4.0")]
    public class FortifySkillsPlugin : BaseUnityPlugin
    {
        private const string V = "V1.4";
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> safePVPEnabled; // Emrik added
        public static float bonusRate;
        public static float fortifyLevelRate;
        public static float fortifyMaxRate;
        

        void Awake()
        {

            modEnabled = Config.Bind("General",
                                     "ModEnabled",
                                     true,
                                     "Used to toggle the mod on and off.");
            safePVPEnabled = Config.Bind("General", // Emrik added
                                     "No Skill Loss With PVP Enabled",
                                     true,
                                     "If set to true, you will not lose skills if PVP is Enabled when you die.");
            // Emrik setting these variables manually to their default value, so they no longer show up on the configuration menu. This is to prevent people on the server from setting their own xp modifier values.
            bonusRate = 1.5f;
            fortifyLevelRate = 0.1f;
            fortifyMaxRate = 0.8f;

            Config.Bind<int>("NexusID", "NexusID", 172, "Nexus mod ID for updates, Don't change");

            if (modEnabled.Value)
            {
                UnityEngine.Debug.Log("Fortify Skills Mod "+ V + " Enabled");
                var harmony = new Harmony("mod.fortify_skills");
                harmony.PatchAll();
            }
            else
            {
                UnityEngine.Debug.Log("Fortify Skills Mod "+V+" Disabled");
            }
        }
    }
}
