#if UNITY_EDITOR
#define CHANCEGEN_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class ChanceGenerator
    {
        // TODO: decide what variables should be protected or private.

        #region properties and fields

        protected internal HashSet<NodePosition> BlockedPositions
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

        protected internal HashSet<NodePosition> GeneratedPositions
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

        protected internal HashSet<Node> Generated
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

        protected readonly int generateAmount;
        protected readonly int diffuseMinimum;
        protected readonly float diffuseBlockChance;

        /// <summary>
        /// Controls if a cell can be selected by the diffuse generator, <br/>
        /// uses <see cref="ConwayRule.IfAnd"/> to check if the position can be blocked.
        /// </summary>
        protected readonly ConwayRule diffuseSelectionRule;

        protected readonly ConwayRule removeRule;
        protected readonly ConwayRule addRule;

        protected readonly int branchRequirement;

        protected Random random;

        private byte _isNodeAccessAllowed;

        #endregion

        public ChanceGenerator(int generateAmount,
            int diffuseMinimum,
            float diffuseBlockChance,
            uint seed,
            ConwayRule diffuseSelectionRule,
            ConwayRule removeRule,
            ConwayRule addRule,
            int branchRequirement)
        {
            this.generateAmount = generateAmount;
            this.diffuseBlockChance = diffuseBlockChance;
            this.diffuseSelectionRule = diffuseSelectionRule;
            this.removeRule = removeRule;
            this.addRule = addRule;
            this.branchRequirement = branchRequirement;
            this.diffuseMinimum = diffuseMinimum;
            random = new Random(seed);

            Generated = new HashSet<Node>(diffuseMinimum, new Node.NodeComparer());
        }

        // TODO: require cancellation token as will most likely be called from MonoBehaviour. 
        public async Task<ReadOnlyMemory<Node>> Generate()
        {
            var startNode = new Node(0, 0);
            GeneratedPositions.Add(startNode.position);

            await Task.Run(() => NeighborDiffuse(in startNode), Application.exitCancellationToken); // diffuse path
            await Task.Run(ConwayPass, Application.exitCancellationToken); // conway pass
            await Task.Run(() => BridgeIslands(GeneratedPositions, BlockedPositions, ref random),
                Application.exitCancellationToken); // reconnect islands
            _isNodeAccessAllowed = 1;
            await Task.Run(GenerateNodes, Application.exitCancellationToken); // generate nodes
            _isNodeAccessAllowed = 2;
            BlockedPositions = null;
            GeneratedPositions = null;

            Generated.TryGetValue(startNode, out var found);

            Assert.IsNotNull(found, "Start node not found in generated nodes!");

            await Task.Run(() => WalkNodesAndApplyValues(found, Generated, branchRequirement),
                Application.exitCancellationToken); // walk nodes

            return Generated.ToArray();
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
        protected virtual void NeighborDiffuse(in Node startNode)
        {
            while (GeneratedPositions.Count + BlockedPositions.Count < generateAmount
                   || GeneratedPositions.Count < diffuseMinimum)
            {
                Span<NodePosition> allNeighbors = GetAllAdjacentNeighbors(GeneratedPositions, BlockedPositions);

                if (allNeighbors.Length == 0)
                {
                    Debug.LogWarning("Was not able to generate minimum number of nodes! All open spaces blocked.");
                    break;
                }

                var index = random.NextInt(0, allNeighbors.Length);
                var neighborsCount = GetFullNeighborCount(allNeighbors[index], GeneratedPositions);
                byte blockedType = 0;

                // TODO: only calculate this if Generated.Count > diffuseMinimum
                if (diffuseSelectionRule.IfAnd(neighborsCount, ref random))
                    blockedType = 1;
                else if (random.NextFloat() <= diffuseBlockChance)
                    blockedType = 2;

                if (GeneratedPositions.Count > diffuseMinimum
                    && blockedType is 1 or 2)
                {
                    BlockedPositions.Add(allNeighbors[index]);
                    continue;
                }

                GeneratedPositions.Add(allNeighbors[index]);
            }
        }

        // uses remove and add rules to determine if a node should be removed or added.
        protected virtual void ConwayPass()
        {
            Span<NodePosition> removing = GeneratedPositions.ToArray();

            foreach (var nodePosition in removing)
            {
                var neighborCount = GetFullNeighborCount(nodePosition, GeneratedPositions);

                if (GeneratedPositions.Contains(nodePosition) && removeRule.IfAnd(neighborCount, ref random))
                {
                    GeneratedPositions.Remove(nodePosition);
                    BlockedPositions.Add(nodePosition);
                }
            }

            Span<NodePosition> allNodePositions = GetAllAdjacentNeighbors(GeneratedPositions, BlockedPositions);
            foreach (var position in allNodePositions)
            {
                var neighborCount = GetFullNeighborCount(position, GeneratedPositions);
                if (addRule.IfAnd(neighborCount, ref random))
                    GeneratedPositions.Add(position);
            }
        }
    }
}