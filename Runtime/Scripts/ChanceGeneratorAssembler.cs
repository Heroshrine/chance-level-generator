using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace ChanceGen
{
    public sealed partial class ChanceGenerator
    {
        /// <summary>
        /// Creates a new <see cref="ChanceGenerator"/> using the <see cref="ChanceGenerator.Builder"/> class,
        /// which implicitly converts to a <see cref="ChanceGenerator"/> instance. <br/>
        /// Further customization of the generator can be accomplished by using the builder methods.
        /// </summary>
        /// <param name="seed">The seed for the generator to use.</param>
        /// <param name="diffuseMinimum">The minimum number of positions to diffuse. If extreme values are supplied to
        /// the generator, this value may not be met and a warning will be generated.</param>
        /// <param name="diffuseMaximum">The maximum number of positions to diffuse.</param>
        /// <param name="diffuseBlockChance">The chance a position will be blocked during diffuse generation.</param>
        /// <param name="branchRequirement">The number of connections a node needs to be considered the root of a branch.</param>
        /// <returns>A Builder instance that implicitly converts to a <see cref="ChanceGenerator"/>.</returns>
        public static Builder Create(uint seed,
            int diffuseMinimum,
            int diffuseMaximum,
            float diffuseBlockChance = 0.09f,
            int branchRequirement = 3)
        {
            return new Builder(seed, diffuseMinimum, diffuseMaximum, diffuseBlockChance,
                branchRequirement);
        }

        /// <summary>
        /// Class that is used to build a <see cref="ChanceGenerator"/> instance.
        /// </summary>
        public sealed class Builder
        {
            private readonly uint _seed;

            private readonly int _diffuseMinimum;
            private readonly int _diffuseMaximum;
            private readonly float _diffuseBlockChance;
            private ConwayRule _diffuseSelectionRule;

            private readonly int _branchRequirement;

            private ConwayRule _removeRule;
            private ConwayRule _addRule;

            private PositionGenerator _positionGenerators;
            private NodeGenerator _nodeGenerators;
            private PostWalkGenerator _postWalkGenerators;

            // constructor used by Create method
            internal Builder(uint seed,
                int diffuseMinimum,
                int diffuseMaximum,
                float diffuseBlockChance,
                int branchRequirement)
            {
                _seed = seed;

                _diffuseMinimum = diffuseMinimum;
                _diffuseMaximum = diffuseMaximum;
                _diffuseBlockChance = diffuseBlockChance;

                _branchRequirement = branchRequirement;

                _diffuseSelectionRule = default;
                _removeRule = default;
                _addRule = default;
            }

            /// <summary>
            /// Adds a diffuse selection rule to the generator. If <see cref="ConwayRule.IfAnd"/> returns true,
            /// diffuse generation will be blocked at that position.
            /// <br/><br/>
            /// Only one diffuse selection rule can be added to a generator.
            /// </summary>
            /// <param name="rule">The conway rule to use.</param>
            public Builder WithDiffuseSelectionRule(ConwayRule rule)
            {
                Assert.IsTrue(_diffuseSelectionRule.Equals(default(ConwayRule)),
                    "Diffuse selection rule already set! You are overriding the previous rule!");
                _diffuseSelectionRule = rule;
                return this;
            }

            /// <summary>
            /// Adds a diffuse selection rule to the generator. If <see cref="ConwayRule.IfAnd"/> returns true,
            /// diffuse generation will be blocked at that position.
            /// <br/><br/>
            /// Only one diffuse selection rule can be added to a generator.
            /// </summary>
            /// <param name="limitGTET">Supplies <see cref="ConwayRule.LimitGTET"/> for the rule.</param>
            /// <param name="limitLTET">Supplies <see cref="ConwayRule.LimitLTET"/> for the rule.</param>
            /// <param name="effectChance">Supplies <see cref="ConwayRule.EffectChance"/> for the rule.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Builder WithDiffuseSelectionRule(byte limitGTET, byte limitLTET, float effectChance) =>
                WithDiffuseSelectionRule(new ConwayRule(limitGTET, limitLTET, effectChance));

            /// <summary>
            /// Adds a removal rule to the generator. If <see cref="ConwayRule.IfAnd"/> returns true, the position
            /// will be blocked. Runs before the add rule.
            /// <br/><br/>
            /// Only one removal rule can be added to a generator.
            /// </summary>
            /// <param name="rule">The conway rule to use.</param>
            public Builder WithRemoveRule(ConwayRule rule)
            {
                Assert.IsTrue(_removeRule.Equals(default(ConwayRule)), "Remove rule already set! "
                                                                       + "You are overriding the previous rule!");
                _removeRule = rule;
                return this;
            }

            /// <summary>
            /// Adds a removal rule to the generator. If <see cref="ConwayRule.IfAnd"/> returns true, the position
            /// will be blocked. Runs before the add rule.
            /// <br/><br/>
            /// Only one removal rule can be added to a generator.
            /// </summary>
            /// <param name="limitGTET">Supplies <see cref="ConwayRule.LimitGTET"/> for the rule.</param>
            /// <param name="limitLTET">Supplies <see cref="ConwayRule.LimitLTET"/> for the rule.</param>
            /// <param name="effectChance">Supplies <see cref="ConwayRule.EffectChance"/> for the rule.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Builder WithRemoveRule(byte limitGTET, byte limitLTET, float effectChance) =>
                WithRemoveRule(new ConwayRule(limitGTET, limitLTET, effectChance));

            /// <summary>
            /// Adds an add rule to the generator. If <see cref="ConwayRule.IfAnd"/> returns true, the empty position
            /// will be filled. Runs after the removal rule. Since the removal rule blocks, removed positions cannot
            /// be added back.
            /// <br/><br/>
            /// Only one add rule can be added to a generator.
            /// </summary>
            /// <param name="rule">The conway rule to use.</param>
            public Builder WithAddRule(ConwayRule rule)
            {
                Assert.IsTrue(_addRule.Equals(default(ConwayRule)), "Add rule already set! "
                                                                    + "You are overriding the previous rule!");
                _addRule = rule;
                return this;
            }

            /// <summary>
            /// Adds an add rule to the generator. If <see cref="ConwayRule.IfAnd"/> returns true, the empty position
            /// will be filled. Runs after the removal rule. Since the removal rule blocks, removed positions cannot
            /// be added back.
            /// <br/><br/>
            /// Only one add rule can be added to a generator.
            /// </summary>
            /// <param name="limitGTET">Supplies <see cref="ConwayRule.LimitGTET"/> for the rule.</param>
            /// <param name="limitLTET">Supplies <see cref="ConwayRule.LimitLTET"/> for the rule.</param>
            /// <param name="effectChance">Supplies <see cref="ConwayRule.EffectChance"/> for the rule.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Builder WithAddRule(byte limitGTET, byte limitLTET, float effectChance) =>
                WithAddRule(new ConwayRule(limitGTET, limitLTET, effectChance));

            /// <summary>
            /// Adds a position generator method that will be called before transitioning to node generation.
            /// Node information cannot be accessed during position generation. See <see cref="PositionGenerator"/> for
            /// more information.
            /// </summary>
            /// <param name="additionalGenerator">The position generation method to add.</param>
            public Builder AddPositionGenerator(PositionGenerator additionalGenerator)
            {
                _positionGenerators += additionalGenerator;
                return this;
            }

            /// <summary>
            /// Adds a node generator method that will be called after nodes have been generated from positional data.
            /// See <see cref="NodeGenerator"/> for more information.
            /// </summary>
            /// <param name="additionalGenerator">The node generation method to add.</param>
            public Builder AddNodeGenerator(NodeGenerator additionalGenerator)
            {
                _nodeGenerators += additionalGenerator;
                return this;
            }

            /// <summary>
            /// Adds a post walk generator method that will be called after the node generation has been completed.
            /// No new nodes can be created during this phase.
            /// </summary>
            /// <param name="postWalkGenerator">The node modification method to add.</param>
            public Builder AddPostWalkGenerator(PostWalkGenerator postWalkGenerator)
            {
                _postWalkGenerators += postWalkGenerator;
                return this;
            }

            public static implicit operator ChanceGenerator(Builder builder) =>
                new ChanceGenerator(builder._diffuseMaximum, builder._diffuseMinimum,
                    builder._diffuseBlockChance, builder._seed,
                    builder._diffuseSelectionRule, builder._removeRule, builder._addRule,
                    builder._branchRequirement, builder._positionGenerators,
                    builder._nodeGenerators, builder._postWalkGenerators);
        }
    }
}