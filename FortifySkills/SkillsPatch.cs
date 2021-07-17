using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace FortifySkills
{
    [HarmonyPatch(typeof(Skills), "Load")]
    public static class ApplyLoadChanges
    {
        private static void Prefix(ref Skills __instance, ZPackage pkg, ref Player ___m_player)
        {
#if DEBUG
            UnityEngine.Debug.Log("Custom Load");
#endif
            MethodInfo IsSkillValid = __instance.GetType().GetMethod("IsSkillValid", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo GetSkill = __instance.GetType().GetMethod("GetSkill", BindingFlags.NonPublic | BindingFlags.Instance);

            int currentPos = pkg.GetPos();

            ExtraSkillData.extraSkillValues.Clear();
            ExtraSkillData.associatedPlayer = ___m_player;

            int num = pkg.ReadInt();
            int num2 = pkg.ReadInt();
            for (int i = 0; i < num2; i++)
            {
                Skills.SkillType skillType = (Skills.SkillType)pkg.ReadInt();
                float level = pkg.ReadSingle();
                float accumulator = (num >= 2) ? pkg.ReadSingle() : 0f;
                if ((bool)IsSkillValid.Invoke(__instance, new System.Object[] { skillType }))
                {
                    Skills.Skill skill = (Skills.Skill)GetSkill.Invoke(__instance, new System.Object[] { skillType });
                    //init Fortify skill to 95% of current skill if it doesn't already exist in Dict. Will be overridden by stored value if it is saved to the character
                    if (!ExtraSkillData.extraSkillValues.ContainsKey(skillType))
                    {
                        ExtraSkillData.extraSkillValues[skillType] = new ExtraSkillData(skill.m_info, level * 0.95f, 0f);
                    }
                }
                else
                {
                    Skills.SkillType extraSkillType = (Skills.SkillType)(int.MaxValue - (int)skillType);
                    if ((bool)IsSkillValid.Invoke(__instance, new System.Object[] { extraSkillType }))
                    {
#if DEBUG
                        UnityEngine.Debug.Log("Fortify Skill mapped to:" + extraSkillType.ToString() + " @:" + level);
#endif

                        Skills.Skill skill = (Skills.Skill) GetSkill.Invoke(__instance, new System.Object[] { extraSkillType });
                        ExtraSkillData.extraSkillValues[extraSkillType] = new ExtraSkillData(skill.m_info, level, accumulator);
                    }
                    else
                    {
#if DEBUG
                        UnityEngine.Debug.Log("Unrecognised Fortify skill!");
#endif
                    }
                }
            }
            pkg.SetPos(currentPos);
        }
    }

    [HarmonyPatch(typeof(Skills), "Save")]
    public static class ApplySaveChanges
    {
        private static void Prefix(ref Skills __instance, ZPackage pkg, out Dictionary<Skills.SkillType, Skills.Skill> __state, ref Dictionary<Skills.SkillType, Skills.Skill> ___m_skillData, ref Player ___m_player)
        {


#if DEBUG
            UnityEngine.Debug.Log("Custom Save");
#endif
            __state = ___m_skillData;
            if (ExtraSkillData.associatedPlayer == ___m_player)
            {
                
                ___m_skillData = new Dictionary<Skills.SkillType, Skills.Skill>();
                foreach (KeyValuePair<Skills.SkillType, Skills.Skill> pair in __state)
                {
#if DEBUG
                    UnityEngine.Debug.Log("Copying" + pair.Value.m_info.m_skill.ToString());
#endif
                    ___m_skillData[pair.Key] = pair.Value;
                }

                foreach (KeyValuePair<Skills.SkillType, ExtraSkillData> pair in ExtraSkillData.extraSkillValues)
                {
#if DEBUG
                    UnityEngine.Debug.Log("Making Fake skill for " + pair.Value.skillInfo.m_skill.ToString());
#endif
                    Skills.SkillDef fakeSkillInfo = new Skills.SkillDef();
                    fakeSkillInfo.m_skill = (Skills.SkillType)(int.MaxValue - (int)pair.Value.skillInfo.m_skill);
                    Skills.Skill fakeSkill = new Skills.Skill(fakeSkillInfo);
                    fakeSkill.m_accumulator = pair.Value.fortifyAccumulator;
                    fakeSkill.m_level = pair.Value.fortifyLevel;
                    ___m_skillData[fakeSkillInfo.m_skill] = fakeSkill;
                }
            }
            else
            {
                UnityEngine.Debug.Log("New character: skipping saving Fortified Skill data");
            }
        }

        private static void Postfix(ref Skills __instance, Dictionary<Skills.SkillType, Skills.Skill> __state, ref Dictionary<Skills.SkillType, Skills.Skill> ___m_skillData)
        {
#if DEBUG
            UnityEngine.Debug.Log("Fixing Skill Data");
#endif

            ___m_skillData = __state;

        }


    }


    [HarmonyPatch(typeof(Skills.Skill), "Raise")]
    public static class ApplyRaiseChanges
    {
        private static void Prefix(ref Skills.Skill __instance, ref float factor)
        {
#if DEBUG
            UnityEngine.Debug.Log("Custom Raise");
#endif
            float num = __instance.m_info.m_increseStep * factor;

            ExtraSkillData extra;
            if (ExtraSkillData.extraSkillValues.ContainsKey(__instance.m_info.m_skill))
            {
                extra = ExtraSkillData.extraSkillValues[__instance.m_info.m_skill];
            }
            else
            {
                extra = new ExtraSkillData(__instance.m_info, __instance.m_level, 0f);
                ExtraSkillData.extraSkillValues[__instance.m_info.m_skill] = extra;
            }

            if (extra.fortifyLevel < 100f)
            {
                extra.fortifyAccumulator = extra.fortifyAccumulator + (num * Mathf.Clamp((__instance.m_level - extra.fortifyLevel) * FortifySkillsPlugin.fortifyLevelRate, 0.0f, FortifySkillsPlugin.fortifyMaxRate));
#if DEBUG
                UnityEngine.Debug.Log("Fortify xp:" + extra.fortifyAccumulator);
#endif
                if (extra.fortifyAccumulator >= GetNextLevelRequirement(extra.fortifyLevel))
                {
                    //display effect
                    Player player = Player.m_localPlayer;
                    Transform playerHead = (Transform)player.GetType().GetField("m_head", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
                    GameObject vfx_prefab = ZNetScene.instance.GetPrefab("vfx_ColdBall_launch");
                    GameObject sfx_prefab = player.m_skillLevelupEffects.m_effectPrefabs[1].m_prefab;

                    UnityEngine.Object.Instantiate<GameObject>(vfx_prefab, player.GetHeadPoint(), Quaternion.Euler(-90f, 0, 0));
                    UnityEngine.Object.Instantiate<GameObject>(sfx_prefab, player.GetHeadPoint(), Quaternion.identity);



                    //show message
                    MessageHud.MessageType type = ((int)extra.fortifyLevel == 0) ? MessageHud.MessageType.Center : MessageHud.MessageType.TopLeft;
                    player.Message(type, string.Concat(new object[]
                    {
                        "Fortified skill improved $skill_",
                        extra.skillInfo.m_skill.ToString().ToLower(),
                        ": ",
                        (int)extra.fortifyLevel+1f
                    }), 0, extra.skillInfo.m_icon); ;

                    extra.fortifyLevel += 1f;
                    extra.fortifyLevel = Mathf.Clamp(extra.fortifyLevel, 0f, 100f);
                    extra.fortifyAccumulator = 0f;
#if DEBUG
                    UnityEngine.Debug.Log("Fortify level:" + extra.fortifyLevel);
#endif
                }
            }
            factor *= FortifySkillsPlugin.bonusRate;

        }

        private static float GetNextLevelRequirement(float level)
        {
            return Mathf.Pow(level + 1f, 1.5f) * 0.5f + 0.5f;
        }

    }

    [HarmonyPatch(typeof(Skills), "OnDeath")]
    public static class applyOnDeathChanges
    {
        // Variable should be set to true if pvp is enabled, or false if it is not.
        private static bool pvpbool;
        private static bool safePVPEnabled = FortifySkillsPlugin.safePVPEnabled.Value;
        // The first Harmony Prefix method that returns false will skip the original method (the original method here being the one that reduces skills by 5%), and go straight to implementing Postfixes.
        // This Prefix method returns false is PVP is enabled, and true if PVP is not enabled.
        private static bool Prefix(Skills __instance)
        {
            Player player = __instance.GetComponent<Player>();
            pvpbool = player.IsPVPEnabled(); // Sets the variable.

            if (player != null && safePVPEnabled)
            {
                return !pvpbool;
            }
            return true;

        }

        private static void Postfix(ref Skills __instance, ref Dictionary<Skills.SkillType, Skills.Skill> ___m_skillData)
        {
#if DEBUG
            UnityEngine.Debug.Log("Custom OnDeath");
#endif
            // If PVP is enabled, skip the rest of this Postfix method (i.e. don't set the levels to their fortified levels).
            if (pvpbool && safePVPEnabled) { return; }
            
            foreach (KeyValuePair<Skills.SkillType, Skills.Skill> pair in ___m_skillData)
            {
                if (ExtraSkillData.extraSkillValues.ContainsKey(pair.Key))
                {

                    ExtraSkillData fortify = ExtraSkillData.extraSkillValues[pair.Key];
#if DEBUG
                    UnityEngine.Debug.Log("Setting " + pair.Key + " to fortify level:" + fortify.fortifyLevel);
#endif
                    pair.Value.m_level = fortify.fortifyLevel;
                    pair.Value.m_accumulator = 0f;
                }

            }
        }
    }

    [HarmonyPatch(typeof(SkillsDialog), "Setup")]

    public static class applySetupChanges
    {
        [HarmonyPriority(Priority.Low)]
        [HarmonyAfter(new string[] { "MK_BetterUI" })]
        private static void Postfix(ref SkillsDialog __instance, ref List<GameObject> ___m_elements, Player player)
        {
#if DEBUG
            UnityEngine.Debug.Log("Custom Setup");
#endif
            List<Skills.Skill> skillList = player.GetSkills().GetSkillList();
            for (int i = 0; i < ___m_elements.Count; i++)
            {
                String description = ___m_elements[i].GetComponentInChildren<UITooltip>().m_text;
                Skills.Skill foundSkill;
                for (int j = 0; i < skillList.Count; j++)
                {
                    if (skillList[j].m_info.m_description== description)
                    {
                        foundSkill = skillList[j];

                        //check if we have a fortified skill level for this skill
                        if (ExtraSkillData.extraSkillValues.ContainsKey(foundSkill.m_info.m_skill))
                        {
                            Text nameTextComponent = Utils.FindChild(___m_elements[i].transform, "name").GetComponent<Text>();
                            String fortLevelText = " (" + ((int)ExtraSkillData.extraSkillValues[foundSkill.m_info.m_skill].fortifyLevel) + ")";

                            if (nameTextComponent.text.Contains("</size>"))
                            {
#if DEBUG
                                UnityEngine.Debug.Log("Probably have BetterUI installed, altering their text");
#endif
                                nameTextComponent.text = nameTextComponent.text.Replace("</size>", fortLevelText + "</size>");
                            }
                            else
                            {
                                Text levelTextComponent = Utils.FindChild(___m_elements[i].transform, "leveltext").GetComponent<Text>();
                                levelTextComponent.text = levelTextComponent.text + fortLevelText;
                            }
                        }
                        else
                        {
#if DEBUG
                            UnityEngine.Debug.Log("No Fortified skill found for" + foundSkill.m_info.m_skill.ToString());
#endif
                        }

                        break;
                    }

                }
            }
        }
    }

}
