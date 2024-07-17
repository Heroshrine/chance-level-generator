using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Placement Rule", menuName = "ChanceGen/Placement Rule")]
    public class PlacementRule : RoomRule
    {
        [SerializeField, Tooltip("How placement is determined.")]
        private PlacementMode _placementMode;

        [SerializeField, Tooltip("How rooms will be oriented.")]
        private PlacementOrientation _placementOrientation;

        [SerializeField, Tooltip("What the range of placement will be, proportional to the max walk count.")]
        private float2 _placementRangeP;


        [SerializeField, Tooltip("What the range of placement will be.")]
        private int2 _placementRangeE;

        [SerializeField, Tooltip("Should the range of placement be clamped to the max walk value?")]
        private bool _clampable = true;


        [Min(0), SerializeField, Tooltip("How many times this rule can be placed. 0 is infinite number of times.")]
        private int _maxPlacements;

        [SerializeField, Range(0f, 1f), Tooltip("The chance to place this rule.")]
        private float _placementChance;

        [SerializeField, Tooltip("Unique rooms cannot have other unique rooms next to it.")]
        private bool _unique;


        public override void PlaceRoom(ReadOnlySpan<Node> nodes,
            HashSet<Node> generatedNodes,
            int maxWalkValue,
            float nodeSideSize,
            ref Random random)
        {
            var placements = _maxPlacements;

            if (_placementMode == PlacementMode.Exact)
                PlaceRoomExact(nodes, generatedNodes, maxWalkValue, nodeSideSize, placements, ref random);
            else
                PlaceRoomProportional(nodes, generatedNodes, maxWalkValue, nodeSideSize, placements, ref random);
        }

        private void PlaceRoomExact(ReadOnlySpan<Node> nodes,
            HashSet<Node> generatedNodes,
            int maxWalkValue,
            float nodeSideSize,
            int placements,
            ref Random random)
        {
            // if clamping, clamp values:
            if (_clampable && _placementRangeE.y > maxWalkValue) _placementRangeE.y = maxWalkValue;
            if (_clampable && _placementRangeE.x > _placementRangeE.y) _placementRangeE.x = _placementRangeE.y;
            Span<Node> buffer4 = new Node[4];

            foreach (var node in nodes)
            {
                if (node is null) continue;
                if (node.consumed) continue;

                var walkCount = node.nodeData.walkCount;
                if (walkCount < _placementRangeE.x || walkCount > _placementRangeE.y) continue; // skip if not in range

                if (!(random.NextFloat() <= _placementChance)) continue;
                if (_unique && CheckUniqueness(node, buffer4, generatedNodes)) continue;

                node.consumed = true;
                PlaceRoomAt(node, nodeSideSize, ref random);

                placements--;

                if (placements == 0)
                    return;
            }
        }


        private void PlaceRoomProportional(ReadOnlySpan<Node> nodes,
            HashSet<Node> generatedNodes,
            int maxWalkValue,
            float nodeSideSize,
            int placements,
            ref Random random)
        {
            Span<Node> buffer4 = new Node[4];

            foreach (var node in nodes)
            {
                if (node is null) continue;
                if (node.consumed) continue;

                var range = node.nodeData.walkCount / (float)maxWalkValue;
                if (range < _placementRangeP.x || range > _placementRangeP.y) continue; // skip if not in range

                if (!(random.NextFloat() <= _placementChance)) continue;
                if (_unique && CheckUniqueness(node, buffer4, generatedNodes)) continue;

                node.consumed = true;
                PlaceRoomAt(node, nodeSideSize, ref random);

                placements--;

                if (placements == 0)
                    return;
            }
        }

        private bool CheckUniqueness(Node node, Span<Node> buffer4, HashSet<Node> generatedNodes)
        {
            NeighborUtils.GetAdjacentNeighbors(node, ref buffer4, generatedNodes);

            var unique = false;
            foreach (var neighbor in buffer4)
            {
                if (neighbor is null) continue;
                if (!neighbor.unique) continue;

                unique = true;
                break;
            }

            return unique;
        }

        private void PlaceRoomAt(Node node, float nodeSideSize, ref Random random)
        {
            var placing = roomPrefabs[random.NextInt(0, roomPrefabs.Length)];

            Vector3 placingPosition = new Vector2(node.position.x, node.position.y) * nodeSideSize;
            if (_placementOrientation == PlacementOrientation.Horizontal)
                placingPosition = new Vector3(placingPosition.x, 0, placingPosition.y);

            var placed = Instantiate(placing, placingPosition, Quaternion.identity);
            placed.name = $"{node.position}";

            placed.EnableWalls(node.nodeData.connections);
        }
    }

    public enum PlacementMode
    {
        Exact,
        Proportional
    }

    public enum PlacementOrientation
    {
        Horizontal,
        Vertical
    }
}