#if UNITY_EDITOR
//#define CHANCEGEN_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;
using static ChanceGen.NeighborUtils;
using static ChanceGen.WalkUtils;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Heroshrine.ChanceLevelGenerator.Tests")]
#endif

namespace ChanceGen
{
    /// <summary>
    /// Delegate for method type that runs during NodePosition generation. Happens before node generation, data
    /// from nodes cannot be accessed during this phase. Only thread-safe unity API (such as Unity.Mathematics or Debug.Log)
    /// can be used by this delegate.
    /// </summary>
    public delegate void PositionGenerator(HashSet<NodePosition> generatedPositions,
        HashSet<NodePosition> blockedPositions,
        ref Random random);

    /// <summary>
    /// Delegate for method type that runs during Node generation. Happens after NodePosition generation, data from
    /// node positions cannot be accessed during this phase (except <see cref="Node"/>.<see cref="Node.position"/>). Only thread-safe
    /// unity API (such as Unity.Mathematics or Debug.Log) can be used by this delegate.
    /// </summary>
    public delegate void NodeGenerator(HashSet<Node> generatedNodes, ref Random random);

    /// <summary>
    /// Delegate for method type that runs after nodes are walked. No new nodes can be generated during this phase.
    /// </summary>
    public delegate void PostWalkGenerator(ReadOnlySpan<Node> generatedNodes, ref Random random);

    /// <summary>
    /// The Chance Generator class generates a set of nodes using a modified diffuse method and a ruleset inspired by
    /// Conway's Game of Life. To start using the Chance Generator, create an instance of it using
    /// <see cref="ChanceGenerator.Create"/>.
    /// <br/><br/>
    /// The generator can be customized by adding additional methods to run by using
    /// <see cref="Builder.AddPositionGenerator"/>, <see cref="Builder.AddNodeGenerator"/>,
    /// and <see cref="Builder.AddPostWalkGenerator"/>.
    /// </summary>
    public sealed partial class ChanceGenerator
    {
        // TODO: decide what variables should be protected or private.

        #region properties and fields

        /// <summary>
        /// Blocked positions, used during position generation. These will not be turned into nodes, unless debug is
        /// enabled.
        /// </summary>
        internal HashSet<NodePosition> BlockedPositions
        {
            get
            {
                Assert.IsFalse(_isNodeAccessAllowed >= 2,
                    "Cannot access blocked positions hash set after node generation started!");
                return _bnps;
            }
            private set { _bnps = value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private HashSet<NodePosition> _bnps = new();

        /// <summary>
        /// Generated positions, used during position generation. These will be turned into nodes.
        /// </summary>
        internal HashSet<NodePosition> GeneratedPositions
        {
            get
            {
                Assert.IsFalse(_isNodeAccessAllowed >= 2,
                    "Cannot access positions hash set after node generation started!");
                return _gnps;
            }
            private set { _gnps = value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private HashSet<NodePosition> _gnps = new();

        /// <summary>
        /// Nodes generated using the node positions in <see cref="GeneratedPositions"/>.
        /// </summary>
        internal HashSet<Node> Generated
        {
            get
            {
                Assert.IsTrue(_isNodeAccessAllowed >= 1, "Cannot access nodes hash set until node generation started!");
                return _gns;
            }
            private set { _gns = value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private HashSet<Node> _gns;

        /// <summary>
        /// The maximum number of nodes that can be generated by diffusion.
        /// </summary>
        private readonly int _diffuseMax;

        /// <summary>
        /// The minimum number of nodes that can be generated by diffusion. If extreme values are supplied to
        /// the chance generator, this value may not be met and a warning will be generated.
        /// </summary>
        private readonly int _diffuseMinimum;

        /// <summary>
        /// The chance for diffusion to produce a blocked position.
        /// </summary>
        private readonly float _diffuseBlockChance;

        /// <summary>
        /// Controls if a position can be selected by the diffuse generator. <br/>
        /// Uses <see cref="ConwayRule.IfAnd"/> to check if the position can be blocked.
        /// </summary>
        private readonly ConwayRule _diffuseSelectionRule;

        /// <summary>
        /// Controls if a position can be selected by the generator for removal. <br/>
        /// Uses <see cref="ConwayRule.IfAnd"/> to check if the position can be removed.
        /// </summary>
        private readonly ConwayRule _removeRule;

        /// <summary>
        /// Controls if a position can be selected by the generator for addition. <br/>
        /// Uses <see cref="ConwayRule.IfAnd"/> to check if the position can be added.
        /// </summary>
        private readonly ConwayRule _addRule;

        /// <summary>
        /// The number that determines if a node is a branch. The default value supplied is 3.
        /// </summary>
        private readonly int _branchRequirement;

        // random instance used for generation
        private Random _random;

        // used to determine whether node access or position access is allowed.
        private byte _isNodeAccessAllowed;

        /// <summary>
        /// Called at the end of position generation, just before transitioning to node generation.
        /// </summary>
        public event PositionGenerator AdditionalPositionGeneration;

        /// <summary>
        /// Called at the end of node generation, just before walking the nodes.
        /// </summary>
        public event NodeGenerator AdditionalNodeGeneration;

        /// <summary>
        /// Called after nodes are walked, just before returning generated nodes.
        /// </summary>
        public event PostWalkGenerator PostWalkGeneration;

        #endregion

        // internal constructor, use create method + builder in other assemblies.
        internal ChanceGenerator(int diffuseMax,
            int diffuseMinimum,
            float diffuseBlockChance,
            uint seed,
            ConwayRule diffuseSelectionRule,
            ConwayRule removeRule,
            ConwayRule addRule,
            int branchRequirement,
            PositionGenerator additionalPositionGeneration,
            NodeGenerator additionalNodeGeneration,
            PostWalkGenerator postWalkGeneration)
        {
            _diffuseMax = diffuseMax;
            _diffuseBlockChance = diffuseBlockChance;
            _diffuseSelectionRule = diffuseSelectionRule;
            _removeRule = removeRule;
            _addRule = addRule;
            _branchRequirement = branchRequirement;
            _diffuseMinimum = diffuseMinimum;
            _random = new Random(seed);

            Generated = new HashSet<Node>(diffuseMinimum, new Node.NodeComparer());

            AdditionalPositionGeneration = additionalPositionGeneration;
            AdditionalNodeGeneration = additionalNodeGeneration;
            PostWalkGeneration = postWalkGeneration;
        }

        // TODO: require cancellation token as will most likely be called from MonoBehaviour. 
        public async Task<ReadOnlyMemory<Node>> Generate(CancellationToken requiredToken)
        {
            if (requiredToken.Equals(default) || !requiredToken.CanBeCanceled)
                Debug.LogWarning("A cancellation token that cannot be cancelled was supplied!"
                                 + "This is probably unwanted and could result in memory leaks.");

            // position generation steps
            GeneratedPositions.Add(new NodePosition(0, 0));
            await Task.Run(NeighborDiffuse, Application.exitCancellationToken); // diffuse path
            await Task.Run(ConwayRemovePass, Application.exitCancellationToken); // conway remove
            await Task.Run(ConwayAddPass, Application.exitCancellationToken); // conway add
            await Task.Run(() => BridgeIslands(GeneratedPositions, BlockedPositions, ref _random),
                Application.exitCancellationToken); // reconnect islands
            await Task.Run(
                () => AdditionalPositionGeneration?.Invoke(GeneratedPositions, BlockedPositions, ref _random),
                Application.exitCancellationToken);

            // transitional steps
            _isNodeAccessAllowed = 1;
            await Task.Run(GenerateNodes, Application.exitCancellationToken); // generate nodes
            _isNodeAccessAllowed = 2;
            BlockedPositions = null;
            GeneratedPositions = null;

            // node generation steps
            await Task.Run(() => AdditionalNodeGeneration?.Invoke(Generated, ref _random),
                Application.exitCancellationToken);
            Generated.TryGetValue(new Node(0, 0), out var found);
            Assert.IsNotNull(found, "Start node not found in generated nodes!");
            await Task.Run(() => WalkNodesAndApplyValues(found, Generated, _branchRequirement),
                Application.exitCancellationToken); // walk nodes

            // node modification steps
            Memory<Node> generated = await Task.Run(() => Generated.ToArray(), requiredToken);
            await Task.Run(() => PostWalkGeneration?.Invoke(generated.Span, ref _random),
                Application.exitCancellationToken);

            return generated;
        }

        // generates nodes from node positions
        private void GenerateNodes()
        {
            foreach (var position in GeneratedPositions)
            {
                var node = new Node(position);
                Generated.Add(node);
            }

            GenerateNodesDebug();
        }

        // generates nodes from blocked positions for debugging purposes
        [Conditional("CHANCEGEN_DEBUG")]
        private void GenerateNodesDebug()
        {
            foreach (var position in BlockedPositions)
            {
                var node = new Node(position);
                node.blocked = true;
                Generated.Add(node);
            }
        }

        // diffuses from start node, adding neighbors to GeneratedPositions set.
        private void NeighborDiffuse()
        {
            while (GeneratedPositions.Count + BlockedPositions.Count < _diffuseMax
                   || GeneratedPositions.Count < _diffuseMinimum)
            {
                Span<NodePosition> allNeighbors = GetAllAdjacentNeighbors(GeneratedPositions, BlockedPositions);

                if (allNeighbors.Length == 0)
                {
                    Debug.LogWarning("Was not able to generate minimum number of nodes! All open spaces blocked.");
                    break;
                }

                var index = _random.NextInt(0, allNeighbors.Length);
                var neighborsCount = GetFullNeighborCount(allNeighbors[index], GeneratedPositions);
                byte blockedType = 0;

                // TODO: only calculate this if Generated.Count > diffuseMinimum
                if (_diffuseSelectionRule.IfAnd(neighborsCount, ref _random))
                    blockedType = 1;
                else if (_random.NextFloat() <= _diffuseBlockChance)
                    blockedType = 2;

                if (GeneratedPositions.Count > _diffuseMinimum
                    && blockedType is 1 or 2)
                {
                    BlockedPositions.Add(allNeighbors[index]);
                    continue;
                }

                GeneratedPositions.Add(allNeighbors[index]);
            }
        }

        // uses remove rules to determine if a node should be removed.
        private void ConwayRemovePass()
        {
            foreach (var nodePosition in GeneratedPositions)
            {
                var neighborCount = GetFullNeighborCount(nodePosition, GeneratedPositions);

                if (!_removeRule.IfAnd(neighborCount, ref _random))
                    continue;

                if (nodePosition == new NodePosition(0, 0))
                    continue;

                BlockedPositions.Add(nodePosition);
            }

            foreach (var pos in BlockedPositions)
            {
                if (GeneratedPositions.Contains(pos))
                    GeneratedPositions.Remove(pos);
            }
        }

        // uses add rules to determine if a node should be added.
        private void ConwayAddPass()
        {
            Span<NodePosition> allNodePositions = GetAllAdjacentNeighbors(GeneratedPositions, BlockedPositions);
            foreach (var position in allNodePositions)
            {
                var neighborCount = GetFullNeighborCount(position, GeneratedPositions);
                if (_addRule.IfAnd(neighborCount, ref _random))
                    GeneratedPositions.Add(position);
            }
        }
    }
}