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
        public static ConfigEntry<float> bonusRate;
        public static ConfigEntry<float> fortifyLevelRate;
        public static ConfigEntry<float> fortifyMaxRate;
        

        void Awake()
        {

            modEnabled = Config.Bind("General",
                                     "ModEnabled",
                                     true,
                                     "Used to toggle the mod on and off.");
            safePVPEnabled = Config.Bind("General",
                                     "No Skill Loss With PVP Enabled",
                                     true,
                                     "If set to true, you will not lose skills if PVP is Enabled when you die.");
            bonusRate = Config.Bind("Mechanics",
                "BonusXPRate",
                1.5f,
                new ConfigDescription("Used to control the rate at which the active level increases, 1=base game, 1.5=50% bonus xp awarded, 0.8=20% less xp awarded. Default:1.5",new AcceptableValueRange<float>(0.0f,10f)));

            fortifyLevelRate = Config.Bind("Mechanics",
                "FortifyXPPerLevelRate",
                0.1f,
                new ConfigDescription("Used to control the rate at which the fortified skill XP increases PER LEVEL behind the active level. 0.1=Will gain 10% XP for every level behind the active level. Default:0.1", new AcceptableValueRange<float>(0.0f, 1f)));

            fortifyMaxRate = Config.Bind("Mechanics",
                "FortifyMaxXPRate",
                0.8f,
                new ConfigDescription("Used to control the maximum rate of XP earned for the fortified skill. Caps FortifyXPPerLevelRate. Values less than 1 mean the fortify skill will always increase more slowly than the active level. 0.8=Will gain a max of 80% of the XP gained for the active skill. Default 0.8", new AcceptableValueRange<float>(0.0f, 2f)));


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
