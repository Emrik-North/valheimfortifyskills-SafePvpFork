using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FortifySkills
{
    class ExtraSkillData
    {

        public static Dictionary<Skills.SkillType,ExtraSkillData> extraSkillValues = new Dictionary<Skills.SkillType, ExtraSkillData>();
        
        public static Player associatedPlayer;

        public float fortifyLevel;
        public float fortifyAccumulator;
        public Skills.SkillDef skillInfo;

        public ExtraSkillData(Skills.SkillDef newSkillDef)
        {
            fortifyAccumulator = 0f;
            fortifyLevel = 0f;
            skillInfo = newSkillDef;
        }
        public ExtraSkillData(Skills.SkillDef newSkillDef, float newLevel, float newAccumulator)
        {
            fortifyAccumulator = newAccumulator;
            fortifyLevel = newLevel;
            skillInfo = newSkillDef;
        }

    }

}
