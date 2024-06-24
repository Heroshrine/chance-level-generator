using System;
using UnityEngine;

namespace ChanceGen
{
    [Serializable]
    public struct ConwayRules
    {
        // less than or equal to, tries to take action if true
        [field: SerializeField, Range(0, 4)] public int LimitLTET { get; private set; }

        // greater than, tries to take action if true
        [field: SerializeField, Range(0, 4)] public int LimitGT { get; private set; }

        // chance for rules to take effect on a given room
        [field: SerializeField, Range(0f, 1f)] public float ActionChance { get; private set; }

        public ConwayRules(int limitLTET, int limitGT, float actionChance)
        {
            LimitLTET = limitLTET;
            LimitGT = limitGT;
            ActionChance = actionChance;
        }
    }
}