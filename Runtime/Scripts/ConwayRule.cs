using System;
using UnityEngine;

namespace ChanceGen
{
    [Serializable]
    public struct ConwayRule
    {
        /// <summary>
        /// Stands for Less Than or Equal To.
        /// The neighbor count must be less than or equal to this number for the rule to take effect.
        /// </summary>
        [field: SerializeField, Tooltip("Stands for Less Than or Equal To. The neighbor count must be less than or"
                                        + "equal to this number for the rule to take effect.")]
        public byte LimitLTET { get; private set; }

        /// <summary>
        /// Stands for Greater Than or Equal To.
        /// THe neighbor count must be greater than or equal to this number for the rule to take effect.
        /// </summary>
        [field: SerializeField, Tooltip("Stands for Greater Than or Equal To. The neighbor count must be greater than"
                                        + "or equal to this number for the rule to take effect.")]
        public byte LimitGTET { get; private set; }

        /// <summary>
        /// The chance that the rule will take effect.
        /// </summary>
        [field: SerializeField, Tooltip("The chance that the rule will take effect.")]
        public float EffectChance { get; private set; }

        public ConwayRule(byte limitLTET, byte limitGTET, float effectChance)
        {
            LimitLTET = limitLTET;
            LimitGTET = limitGTET;
            EffectChance = effectChance;
        }

        /// <summary>
        /// Evaluates if the neighbor count is less than or equal to LimitLTET and greater than or equal to LimitGTET.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <returns>True if the neighbor count is less than or equal to LimitLTET and greater than or equal to LimitGTET. False otherwise.</returns>
        public bool IfAnd(byte neighborCount) => neighborCount <= LimitLTET && neighborCount >= LimitGTET;

        /// <summary>
        /// Evaluates if the neighbor count is either less than or equal to LimitLTET or greater than or equal to LimitGTET.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <returns>True if the neighbor count is less than or equal to LimitLTET or greater than or equal to LimitGTET. False otherwise.</returns>
        public bool IfOr(byte neighborCount) => neighborCount <= LimitLTET || neighborCount >= LimitGTET;

        /// <summary>
        /// Evaluates if the neighbor count is either less than or equal to LimitLTET or greater than or equal to LimitGTET, but not both.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <returns>True if the neighbor count is either less than or equal to LimitLTET or greater than or equal to LimitGTET, but not both. False otherwise.</returns>
        public bool IfXor(byte neighborCount) => neighborCount <= LimitLTET ^ neighborCount >= LimitGTET;

        /// <summary>
        /// Evaluates if the neighbor count is not less than or equal to LimitLTET and not greater than or equal to LimitGTET.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <returns>True if the neighbor count is not less than or equal to LimitLTET and not greater than or equal to LimitGTET. False otherwise.</returns>
        public bool IfNot(byte neighborCount) => !IfOr(neighborCount);
    }
}