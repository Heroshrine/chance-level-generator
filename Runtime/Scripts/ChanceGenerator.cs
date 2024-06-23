using System;
using System.Collections;
using System.Collections.Generic;
using ChanceGen.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    // TODO: needs constructor
    public class ChanceGenerator
    {
        public bool IsGenerating { get; private set; }
        public bool Used { get; private set; }

        private GenerationInfo _generationInfo;
        private ConwayRules _removeRules;
        private ConwayRules _addRules;
        private Memory<SpecialRule> _specialRules;
        private DebugInfoSettings _debugInfoSettings;

        private MemoryGrid<RoomInfo> _rooms;
        private Random _random;
        private SpiralIndexer _spiralIndexer;
        private float _chance;

        // TODO: make way to create generator in new ChanceGenerator script
        private ChanceGenerator() { }

        public IEnumerator Generate() { throw new NotImplementedException(); }

        private int GetSpecialIndex(int max) { throw new NotImplementedException(); }

        private IEnumerator<(int walkCount, RoomInfo[] orderedWalk)> Walk(int2 startPos, int walkDataIndex)
        {
            throw new NotImplementedException();
        }

        private void UpdateRoomConnections(RoomInfo[] buffer, RoomInfo room) { throw new NotImplementedException(); }

        private void GetNeighborsAdjacent(RoomInfo[] result, int x, int y) { throw new NotImplementedException(); }

        private byte GetNeighborCount(int x, int y) { throw new NotImplementedException(); }

        private void Log(string msg, DebugInfoSettings.DebugInfo debugType = DebugInfoSettings.DebugInfo.Minimal)
        {
            throw new NotImplementedException();
        }

        private struct SpiralIndexer
        {
            private byte _dir;
            private int2 _spiral;
            private int2 _allowedCall;
            private int _called;
            private readonly int _sideSize;

            public SpiralIndexer(byte direction, int sideSize)
            {
                _sideSize = sideSize;

                _allowedCall = int2.zero;
                _dir = direction;
                _spiral = new int2(sideSize / 2, sideSize / 2);

                if (direction is 0 or 2)
                    _allowedCall.y = 1;
                else
                    _allowedCall.x = 1;

                _called = 0;
            }

            /// <summary>
            /// Gets an index on the grid of a spiral, starting from the center and increasing every time.
            /// </summary>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException"></exception>
            public int2 GetIndexSpiral()
            {
                _called++;

                if ((_dir is 1 or 3 ? _allowedCall.y : _allowedCall.x) - _called <= 0)
                {
                    if (_dir is 1 or 3)
                        _allowedCall.y += 1;
                    else
                        _allowedCall.x += 1;

                    _called = 0;

                    // TODO: double check this works properly. Is replacing `if (_dir > 3) _dir = 0`.
                    _dir = (byte)(++_dir % 4);
                }

                return _dir switch
                {
                    0 => new int2(_spiral.x++, _spiral.y),
                    1 => new int2(_spiral.x, _spiral.y++),
                    2 => new int2(_spiral.x++, _spiral.y),
                    3 => new int2(_spiral.x, _spiral.y++),
                    _ => throw new InvalidOperationException()
                };
            }
        }

        private struct SortedRoomDataComparer : IComparer<(RoomInfo room, int walkCount)>
        {
            public int Compare((RoomInfo room, int walkCount) x, (RoomInfo room, int walkCount) y)
            {
                var r = y.room.walkData.Span[y.walkCount].walkValue - x.room.walkData.Span[x.walkCount].walkValue;
                return r == 0 ? -1 : r;
            }
        }
    }
}