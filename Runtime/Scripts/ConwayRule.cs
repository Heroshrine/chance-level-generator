using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ChanceGen
{
    [Serializable]
    public struct ConwayRule
    {
        // less than or equal to, tries to take action if true
        [field: SerializeField, Range(0, 8)] public int LimitLTET { get; private set; }

        // greater than, tries to take action if true
        [field: SerializeField, Range(0, 8)] public int LimitGT { get; private set; }

        // chance for rules to take effect on a given room
        [field: SerializeField, Range(0f, 1f)] public float ActionChance { get; private set; }

        public ConwayRule(int limitLTET, int limitGT, float actionChance)
        {
            LimitLTET = limitLTET;
            LimitGT = limitGT;
            ActionChance = actionChance;
        }


        /// <summary>
        /// Returns true if count is less than or equal to <see cref="LimitLTET"/> <b>AND</b> 
        /// greater than <see cref="LimitGT"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IfAnd(byte count) => count <= LimitLTET && count > LimitGT;

        /// <summary>
        /// Returns true if count is less than or equal to <see cref="LimitLTET"/> <b>OR</b> 
        /// greater than <see cref="LimitGT"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IfOr(byte count) => count <= LimitLTET || count > LimitGT;

        /// <summary>
        /// Returns true if count is <b>EITHER</b> less than or equal to <see cref="LimitLTET"/> <b>OR</b> greater 
        /// than <see cref="LimitGT"/> but <b>NOT</b> both.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IfXor(byte count) => count <= LimitLTET ^ count > LimitGT;

        public enum IfRule
        {
            And,
            Or,
            Xor
        }
    }
}