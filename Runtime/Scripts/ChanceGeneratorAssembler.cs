using System;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace ChanceGen
{
    public sealed partial class ChanceGenerator
    {
        public static GeneratorAssembler Create(uint seed,
            int diffuseMinimum,
            int diffuseMaximum,
            float diffuseBlockChance = 0.09f,
            int branchRequirement = 3)
        {
            return new GeneratorAssembler(seed, diffuseMinimum, diffuseMaximum, diffuseBlockChance,
                branchRequirement);
        }

        public sealed class GeneratorAssembler
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

            internal GeneratorAssembler(uint seed,
                int diffuseMinimum,
                int diffuseMaximum,
                float diffuseBlockChance,
                int branchRequirement)
            {
                this._seed = seed;

                this._diffuseMinimum = diffuseMinimum;
                this._diffuseMaximum = diffuseMaximum;
                this._diffuseBlockChance = diffuseBlockChance;

                this._branchRequirement = branchRequirement;

                _diffuseSelectionRule = default;
                _removeRule = default;
                _addRule = default;
            }

            public GeneratorAssembler WithDiffuseSelectionRule(ConwayRule rule)
            {
                Assert.IsTrue(_diffuseSelectionRule.Equals(default(ConwayRule)),
                    "Diffuse selection rule already set! You are overriding the previous rule!");
                _diffuseSelectionRule = rule;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GeneratorAssembler WithDiffuseSelectionRule(byte limitGTET, byte limitLTET, float effectChance) =>
                WithDiffuseSelectionRule(new ConwayRule(limitGTET, limitLTET, effectChance));

            public GeneratorAssembler WithRemoveRule(ConwayRule rule)
            {
                Assert.IsTrue(_removeRule.Equals(default(ConwayRule)), "Remove rule already set! "
                                                                       + "You are overriding the previous rule!");
                _removeRule = rule;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GeneratorAssembler WithRemoveRule(byte limitGTET, byte limitLTET, float effectChance) =>
                WithRemoveRule(new ConwayRule(limitGTET, limitLTET, effectChance));

            public GeneratorAssembler WithAddRule(ConwayRule rule)
            {
                Assert.IsTrue(_addRule.Equals(default(ConwayRule)), "Add rule already set! "
                                                                    + "You are overriding the previous rule!");
                _addRule = rule;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GeneratorAssembler WithAddRule(byte limitGTET, byte limitLTET, float effectChance) =>
                WithAddRule(new ConwayRule(limitGTET, limitLTET, effectChance));


            public GeneratorAssembler AddPositionGenerator(PositionGenerator additionalGenerator)
            {
                _positionGenerators += additionalGenerator;
                return this;
            }

            public GeneratorAssembler AddNodeGenerator(NodeGenerator additionalGenerator)
            {
                _nodeGenerators += additionalGenerator;
                return this;
            }


            public static implicit operator ChanceGenerator(GeneratorAssembler assembler) =>
                new ChanceGenerator(assembler._diffuseMaximum, assembler._diffuseMinimum,
                    assembler._diffuseBlockChance, assembler._seed,
                    assembler._diffuseSelectionRule, assembler._removeRule, assembler._addRule,
                    assembler._branchRequirement, assembler._positionGenerators,
                    assembler._nodeGenerators);
        }
    }
}