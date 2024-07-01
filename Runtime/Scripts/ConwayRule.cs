using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    [Serializable]
    public struct ConwayRule
    {
        /// <summary>
        /// Stands for Less Than or Equal To.
        /// The neighbor count must be less than or equal to this number for the rule to take effect.
        /// </summary>
        [field: SerializeField, Range(0, 8), Tooltip(
                    "Stands for Less Than or Equal To. The neighbor count must be less than or"
                    + "equal to this number for the rule to take effect.")]
        public byte LimitLTET { get; private set; }

        /// <summary>
        /// Stands for Greater Than or Equal To.
        /// The neighbor count must be greater than or equal to this number for the rule to take effect.
        /// </summary>
        [field: SerializeField, Range(0, 8), Tooltip(
                    "Stands for Greater Than or Equal To. The neighbor count must be greater than"
                    + "or equal to this number for the rule to take effect.")]
        public byte LimitGTET { get; private set; }

        /// <summary>
        /// The chance that the rule will take effect.
        /// </summary>
        [field: SerializeField, Range(0, 1), Tooltip("The chance that the rule will take effect.")]
        public float EffectChance { get; private set; }

        public ConwayRule(byte limitGTET, byte limitLTET, float effectChance)
        {
            LimitGTET = limitGTET;
            LimitLTET = limitLTET;
            EffectChance = effectChance;
        }

        public readonly void Deconstruct(out byte limitGTET, out byte limitLTET, out float effectChance)
        {
            limitGTET = LimitGTET;
            limitLTET = LimitLTET;
            effectChance = EffectChance;
        }

        /// <summary>
        /// Evaluates if the neighbor count is less than or equal to <see cref="LimitLTET"/> and greater than or equal to <see cref="LimitGTET"/>, using the <see cref="EffectChance"/>.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <param name="random">The random to use for the effect chance probability.</param>
        /// <returns>True if the neighbor count is less than or equal to LimitLTET and greater than or equal to LimitGTET. False otherwise.</returns>
        public readonly bool IfAnd(byte neighborCount, ref Random random) =>
            neighborCount <= LimitLTET && neighborCount >= LimitGTET && random.NextFloat() <= EffectChance;

        /// <summary>
        /// Evaluates if the neighbor count is either less than or equal to <see cref="LimitLTET"/> or greater than or equal to <see cref="LimitGTET"/>, using the <see cref="EffectChance"/>.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <param name="random">The random to use for the effect chance probability.</param>
        /// <returns>True if the neighbor count is less than or equal to LimitLTET or greater than or equal to LimitGTET. False otherwise.</returns>
        public readonly bool IfOr(byte neighborCount, ref Random random) =>
            (neighborCount <= LimitLTET || neighborCount >= LimitGTET) && random.NextFloat() <= EffectChance;

        /// <summary>
        /// Evaluates if the neighbor count is either less than or equal to <see cref="LimitLTET"/> or greater than or equal to <see cref="LimitGTET"/>, but not both. Uses the <see cref="EffectChance"/>.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <param name="random">The random to use for the effect chance probability.</param>
        /// <returns>True if the neighbor count is either less than or equal to LimitLTET or greater than or equal to LimitGTET, but not both. False otherwise.</returns>
        public readonly bool IfXor(byte neighborCount, ref Random random) =>
            neighborCount <= LimitLTET ^ neighborCount >= LimitGTET && random.NextFloat() <= EffectChance;

        /// <summary>
        /// Evaluates if the neighbor count is not less than or equal to <see cref="LimitLTET"/> and not greater than or equal to <see cref="LimitGTET"/>. Uses the <see cref="EffectChance"/> in an inverse way.
        /// </summary>
        /// <param name="neighborCount">The count of neighboring cells.</param>
        /// <param name="random">The random to use for the effect chance probability.</param>
        /// <returns>True if the neighbor count is not less than or equal to LimitLTET and not greater than or equal to LimitGTET. False otherwise.</returns>
        public readonly bool IfNot(byte neighborCount, ref Random random) => !IfOr(neighborCount, ref random);
    }
}