using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.sims
{
    public class LifeRoleEntry
    {
        public LifeRoleDef role;
        public float weight = 1f;
        public int minCount;       // 保底人數（如衛兵至少 2）
    }

    public class LifeProfileDef : Def
    {
        public List<FactionDef> factionDefs;       // 明示匹配派系
        public List<TechLevel> techLevels;         // 按科技層匹配
        public bool isDefault;                     // 全域 fallback
        public List<LifeRoleEntry> roles = new List<LifeRoleEntry>();
        public Type assignmentWorker = typeof(RoleAssignmentWorker);   // 可被 patch 換實作

        [Unsaved] private RoleAssignmentWorker workerInt;

        public RoleAssignmentWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (RoleAssignmentWorker)Activator.CreateInstance(assignmentWorker);
                }
                return workerInt;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (roles.NullOrEmpty())
            {
                yield return "LifeProfileDef " + defName + " has no roles.";
            }
            if (assignmentWorker != null && !typeof(RoleAssignmentWorker).IsAssignableFrom(assignmentWorker))
            {
                yield return "LifeProfileDef " + defName + " assignmentWorker is not a RoleAssignmentWorker.";
            }
        }
    }
}
