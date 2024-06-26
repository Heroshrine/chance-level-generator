using ChanceGen.Attributes;
using ChanceGen.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    // DONE: needs constructor
    public class ChanceGenerator
    {
        [field: SerializeField, ReadOnlyInInspector]
        public bool IsGenerating { get; private set; }

        [field: SerializeField, ReadOnlyInInspector]
        public bool Used { get; private set; }

        // TODO: add stage reporting through atomic writes

        public event Action OnFinishGenerating;

        private readonly GenerationInfo _generationInfo;
        private readonly ConwayRule _removeRule;
        private readonly ConwayRule _addRule;
        private readonly SpecialStepRule _spawnRoomRule;
        private readonly SpecialStepRule _bossRoomRule;
        private readonly ReadOnlyMemory<SpecialRule> _additionalSpecialRules;
        private readonly DebugInfo _debugInfoSettings;

        private MemoryGrid<RoomInfo> _rooms;
        private Random _random;
        private SpiralIndexer _spiralIndexer;
        private float _chance;

        // DONE: make way to create generator in new ChanceGenerator script
        [Preserve]
        public ChanceGenerator(GenerationInfo generationInfo,
            ConwayRule removeRule,
            ConwayRule addRule,
            SpecialStepRule spawnRoomRule,
            SpecialStepRule bossRoomRule,
            ReadOnlyMemory<SpecialRule> additionalSpecialRules,
            DebugInfo debugInfoSettings)
        {
            Assert.IsNotNull(spawnRoomRule, "spawnRoomRule cannot be null");
            Assert.IsNotNull(bossRoomRule, "spawnRoomRule cannot be null");

            _generationInfo = generationInfo;
            _removeRule = removeRule;
            _addRule = addRule;
            _spawnRoomRule = spawnRoomRule;
            _bossRoomRule = bossRoomRule;
            _additionalSpecialRules = additionalSpecialRules;
            _debugInfoSettings = debugInfoSettings;

            _rooms = new RoomInfo[generationInfo.SideSize, generationInfo.SideSize];
            _random = new Random(generationInfo.seed);
            _spiralIndexer = new SpiralIndexer((byte)_random.NextInt(0, 4), generationInfo.SideSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryGrid<RoomInfo> GetRoomsAsMemoryGrid() // TODO: readonly memory grid?
        {
            Assert.IsTrue(Used && !IsGenerating, "Rooms cannot be retrieved until after generation!");
            return _rooms;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanGrid<RoomInfo> GetRoomsAsSpanGrid() // TODO: readonly span grid?
        {
            Assert.IsTrue(Used && !IsGenerating, "Rooms cannot be retrieved until after generation!");
            return _rooms.SpanGrid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<RoomInfo> GetRoomsAsMemory() // TODO: readonly memory?
        {
            Assert.IsTrue(Used && !IsGenerating, "Rooms cannot be retrieved until after generation!");
            return (Memory<RoomInfo>)_rooms;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<RoomInfo> GetRoomsAsSpan() // TODO: readonly span?
        {
            Assert.IsTrue(Used && !IsGenerating, "Rooms cannot be retrieved until after generation!");
            return (Span<RoomInfo>)_rooms.SpanGrid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Preserve]
        public RoomInfo[] GetRoomsAsArray()
        {
            Assert.IsTrue(Used && !IsGenerating, "Rooms cannot be retrieved until after generation!");
            return _rooms.SpanGrid.ToFlattenedArray();
        }


#if !UNITY_WEBGL
        // TODO: Check for "AggressiveOptimization" Method Implementation Attribute on all generation methods.
        [Preserve]
        public async Task Generate(CancellationToken requiredToken)
        {
            if (Used)
                throw new InvalidOperationException("Cannot reuse generators.");

            Assert.IsFalse(IsGenerating, $"Trying to start generation on a {nameof(ChanceGenerator)} "
                                         + $"instance while it is already generating! This is not supported.");

            IsGenerating = true;
            await Task.Run(Generate_Internal, requiredToken);
            IsGenerating = false;
            Used = true;
            OnFinishGenerating?.Invoke();
        }
#else
#pragma warning disable CS1998
// disable warning as async keyword is only used for compatability reasons between WebGL and non WebGL builds.
        [Preserve]
        public async Task Generate(CancellationToken _ = default)
        {
#if UNITY_EDITOR || DEBUG
            if (_ != default)
                Debug.Log("Supplied cancellation token for ChanceGenerator.Generate is not needed if "
                          + "only targeting WebGL platform. This message will only be logged in the editor "
                          + "and debug builds.");

            Debug.Log("The target platform is WebGL, generation will happen on the main thread! This message will "
                      + "only be logged in the editor and debug builds.");
#endif
            if (Used)
                throw new InvalidOperationException("Cannot reuse generators.");

            Assert.IsFalse(IsGenerating, $"Trying to start generation on a {nameof(ChanceGenerator)} "
                                         + $"instance while it is already generating! This is not supported.");

            IsGenerating = true;
            Generate_Internal();
            IsGenerating = false;
            Used = true;
            OnFinishGenerating?.Invoke();
        }
#pragma warning restore CS1998
#endif

        private void Generate_Internal()
        {
            Log($"Generation started with seed: {_generationInfo.seed}");
            Log($"Level SideSize: {_generationInfo.SideSize}", DebugInfo.Full);
            Log($"Level Area: {_generationInfo.Area}", DebugInfo.Full);

            retry:

            SpanGrid<RoomInfo> roomsSpan = _rooms.SpanGrid;

            // generate rooms with an increasing chance to skip generated rooms.
            for (var i = 0; i < _generationInfo.SideSize; i++)
            {
                var spiralIndex = _spiralIndexer.GetIndexSpiral();

                if (_random.NextFloat() < _chance) continue;

                roomsSpan[spiralIndex.x, spiralIndex.y] = new RoomInfo(spiralIndex, null); // TODO: replace null

                // DONE: ensure this works:
                if ((_chance += _generationInfo.RandomChanceIncrease) > 1)
                    break;
            }

            // do generation add/remove rules
            for (var i = 0; i < _generationInfo.SideSize; i++)
            {
                for (var j = 0; j < _generationInfo.SideSize; j++)
                {
                    var neighborCount = GetNeighborCount(i, j);

                    // remove rooms based on remove rules
                    if (_removeRule.IfOr(neighborCount)
                        && !(i == _generationInfo.SideSize / 2 && j == _generationInfo.SideSize / 2)
                        && _random.NextFloat() < _removeRule.ActionChance
                        && roomsSpan[i, j] != null
                       )
                    {
                        Log($"Destroyed by RemoveRooms: {roomsSpan[i, j]}", DebugInfo.Full);
                        roomsSpan[i, j] = null;
                    }
                    // add rooms based on add rules
                    else if (_addRule.IfAnd(neighborCount)
                             && roomsSpan[i, j] == null
                             && _random.NextFloat() < _addRule.ActionChance
                            )
                    {
                        roomsSpan[i, j] = new RoomInfo(new int2(i, j), null); // TODO: replace null
                        Log($"Added by AddRooms: {roomsSpan[i, j]}", DebugInfo.Full);
                    }
                }
            }

            Assert.IsNotNull(roomsSpan[_generationInfo.SideSize / 2, _generationInfo.SideSize / 2],
                "RoomInfo in the center of the grid is null! Double check generator parameters.");

            // DONE: remove Enumerator
            Tuple<int, Memory<RoomInfo>> walk1 =
                Walk(new int2(_generationInfo.SideSize / 2, _generationInfo.SideSize / 2), 0);
            var walkCount = walk1!.Item1;
            Span<RoomInfo> orderedWalk = walk1.Item2.Span;

            // if walk count too low, regenerate
            if (walkCount < _generationInfo.RegenerateLimit)
            {
                Log($"walkCount was {walkCount} and RegenerateLimit is {_generationInfo.RegenerateLimit}, "
                    + $"regenerating...");

                ResetGenerationLoop();
                goto retry;
            }

            Debug.Log("walk count: " + walkCount);

            // if walk count too high, remove rooms
            if (walkCount > _generationInfo.ShrinkLimit)
            {
                var i = 0;
                for (; i < orderedWalk.Length; i++)
                {
                    Log($"Destroyed by Trim: {orderedWalk[i]}");

                    // DONE: check if works, pretty sure both should be same object and so setting null twice is not needed.
                    roomsSpan[orderedWalk[i].GridPosition.x, orderedWalk[i].GridPosition.y] = null;
                    orderedWalk[i] = null;
                    walkCount--;

                    if (walkCount <= _generationInfo.ShrinkLimit)
                        break;
                }

                orderedWalk = orderedWalk[(i + 1)..];
                Span<RoomInfo> buffer4 = new RoomInfo[4]; // TODO: look into using pointer

                foreach (var room in orderedWalk)
                    UpdateRoomConnections(buffer4, room);
            }

            // delete rooms not covered by first walk
            for (var i = 0; i < _generationInfo.SideSize; i++)
            {
                for (var j = 0; j < _generationInfo.SideSize; j++)
                {
                    if (roomsSpan[i, j] == null || roomsSpan[i, j].walkData[0].walkValue != 0) continue;

                    Log($"Destroyed by walk: {roomsSpan[i, j]}", DebugInfo.Full);
                    roomsSpan[i, j] = null;
                }
            }

            // do new walk to set special rooms
            ReadOnlySpan<RoomInfo> readOnlyWalk = orderedWalk;
            ReadOnlySpan<(int start, int end)> walkIndexRanges = CacheWalkRanges(readOnlyWalk, 0);

            var (min, max) = _spawnRoomRule.GetWalkValueRange(in readOnlyWalk, 0);
            var (start, end) = walkIndexRanges[_random.NextInt(min, max)];
            var spawnIndex = _random.NextInt(start, end);
            orderedWalk[spawnIndex].roomType = _spawnRoomRule.RoomType;

            // DONE: remove enumerator
            Tuple<int, Memory<RoomInfo>> walk2 = Walk(orderedWalk[spawnIndex].GridPosition, 1);
            readOnlyWalk = orderedWalk = walk2!.Item2.Span;
            walkIndexRanges = CacheWalkRanges(readOnlyWalk, 0);

            (min, max) = _bossRoomRule.GetWalkValueRange(in readOnlyWalk, 1);
            (start, end) = walkIndexRanges[_random.NextInt(min, max)];
            var bossIndex = _random.NextInt(start, end);
            orderedWalk[bossIndex].roomType = _bossRoomRule.RoomType;

            // do additional room rules, skipping if chosen room is spawn or boss
            ReadOnlySpan<SpecialRule> additionalRoomRules = _additionalSpecialRules.Span;
            Span<RoomInfo> neighborsBuffer4 = new RoomInfo[4]; // TODO: look into using pointer

            foreach (var rule in additionalRoomRules)
            {
                var (ruleMin, ruleMax) = rule.GetWalkValueRange(in readOnlyWalk, 1);
                ruleMin = walkIndexRanges[^ruleMin].start;
                ruleMax = walkIndexRanges[^(ruleMax - 1)].end;

                Debug.LogWarning($"ruleMin: {ruleMin}");
                Debug.LogWarning($"ruleMax: {ruleMax}");

                for (var i = ruleMin; i < ruleMax; i++)
                {
                    var index2D = readOnlyWalk[i].GridPosition;
                    var fullNeighborCount = GetNeighborCount(index2D.x, index2D.y);
                    GetNeighborsAdjacent(neighborsBuffer4, index2D.x, index2D.y);
                    ReadOnlySpan<RoomInfo> adjacentBuffer4 = neighborsBuffer4;

                    // force spawn unique rooms if not going to spawn
                    if (rule.IsUnique
                        && ((i + 1 < ruleMax
                             && i + 2 >= ruleMax
                             && readOnlyWalk[i + 1].roomType == _bossRoomRule.RoomType)
                            || i + 1 >= ruleMax))
                    {
                        Log($"Force spawned unique room at {readOnlyWalk[i].GridPosition}");
                        orderedWalk[i].roomType = rule.RoomType;
                        break;
                    }

                    if (!rule.ShouldGenerate(in readOnlyWalk, in adjacentBuffer4, i, 1,
                            fullNeighborCount, index2D, (ruleMin, ruleMax), ref _random)) continue;

                    if (readOnlyWalk[i].roomType == _bossRoomRule.RoomType) continue;

                    orderedWalk[i].roomType = rule.RoomType;
                    if (rule.IsUnique)
                        break;
                }
            }

            return;

            void ResetGenerationLoop()
            {
                DestroyRooms();
                _chance = 0;
                _spiralIndexer.Reset((byte)_random.NextInt(0, 4));
            }
        }

        private ReadOnlySpan<(int start, int end)> CacheWalkRanges(in ReadOnlySpan<RoomInfo> ordered, int walkDataIndex)
        {
            Span<(int start, int end)> result = new (int, int)[ordered[0].walkData[walkDataIndex].walkValue];

            var lastHit = ordered[^1].walkData[walkDataIndex].walkValue;

            // TODO: check this works properly for start, mid, and end values.
            for (int i = 1, v = 1, j = 0; i <= ordered.Length; i++)
            {
                if (ordered[^i].walkData[walkDataIndex].walkValue != lastHit)
                {
                    /* length - (i - 1) to get previous index, length - v to get index last changed.
                     length - v - (length - (i - 1)) is length, so previous + length is end index.
                    */
                    result[j++] = (ordered.Length - (i - 1),
                        ordered.Length - (i - 1) + (ordered.Length - v - (ordered.Length - (i - 1))));
                    v = i;
                }

                lastHit = ordered[^i].walkData[walkDataIndex].walkValue;
            }

            return result;
        }

        /* DONE: look into getting away from IEnumerator so can use Span.
         * Probably need wrapper class to make it easy for people to generate, with IEnumerable to wait on in wrapper class.
         * In Unity 6, use Awaitable instead? Shall see how Span can be passed around.
         */
        // Walks rooms using walk algorithm, returning
        private Tuple<int, Memory<RoomInfo>> Walk(int2 startPos, int walkDataIndex)
        {
            // setup used local variables
            var orderedSet = new SortedSet<(RoomInfo room, int walkCount)>(new SortedRoomInfoComparer());
            Queue<RoomInfo> open = new();
            Span<RoomInfo> neighbors = new RoomInfo[4];
            SpanGrid<RoomInfo> roomsSpan = _rooms.SpanGrid;

            open.Enqueue(roomsSpan[startPos.x, startPos.y]);

            roomsSpan[startPos.x, startPos.y].walkData[walkDataIndex] = new WalkData(0, true);
            var walkCount = 1;

            // do this loop until no more open rooms
            while (open.Count > 0)
            {
                var working = open.Dequeue();
                working.Contiguous = true; // if we reached here, it's contiguous

                // get adjacent neighbors, putting them into neighbors span.
                GetNeighborsAdjacent(neighbors, working.GridPosition.x, working.GridPosition.y);
                var min = int.MaxValue;
                // for every neighbor, check its walk value and check if queued
                for (var i = 0; i < neighbors.Length; i++)
                {
                    if (neighbors[i] == null) continue;

                    if (neighbors[i].walkData[walkDataIndex].walkValue > 0
                        && neighbors[i].walkData[walkDataIndex].walkValue < min
                       )
                        min = neighbors[i].walkData[walkDataIndex].walkValue;

                    working.connections |= (RoomConnections)(1 << i);

                    if (neighbors[i].walkData[walkDataIndex].walkValue != 0
                        || neighbors[i].walkData[walkDataIndex].queued
                       ) continue;

                    open.Enqueue(neighbors[i]);
                    neighbors[i].walkData[walkDataIndex].queued = true;
                    walkCount++;
                }

                // DONE: check that this works, before would use max found not min found.
                working.walkData[walkDataIndex].walkValue =
                    min != int.MaxValue ? min + 1 : 1; // sets this walk to the smallest value found + 1.
                orderedSet.Add((working, walkDataIndex));
            }

            Memory<RoomInfo> orderedWalk = Array.ConvertAll(orderedSet.ToArray(), item => item.Item1);

            return new Tuple<int, Memory<RoomInfo>>(walkCount, orderedWalk);
        }

        private void DestroyRooms()
        {
            Log($"Destroyed all rooms from DestroyRooms method", DebugInfo.Full);
            _rooms = new RoomInfo[_generationInfo.SideSize, _generationInfo.SideSize];
        }

        private void UpdateRoomConnections(Span<RoomInfo> buffer, RoomInfo room)
        {
            GetNeighborsAdjacent(buffer, room.GridPosition.x, room.GridPosition.y);

            for (var i = 0; i < 4; i++)
            {
                Log($"Updating room connections for {room} - {(RoomConnections)(1 << i)}: {buffer[i]}",
                    DebugInfo.Full);

                if (buffer[i] == null)
                    room.connections &= ~(RoomConnections)(1 << i);
                else
                    room.connections |= (RoomConnections)(1 << i);
            }
        }

        private void GetNeighborsAdjacent(Span<RoomInfo> result, int x, int y)
        {
            SpanGrid<RoomInfo> roomsSpan = _rooms.SpanGrid;

            if (y + 1 < _generationInfo.SideSize)
                result[0] = roomsSpan[x, y + 1];
            else
                result[0] = null;

            if (y - 1 >= 0)
                result[1] = roomsSpan[x, y - 1];
            else
                result[1] = null;

            if (x + 1 < _generationInfo.SideSize)
                result[2] = roomsSpan[x + 1, y];
            else
                result[2] = null;

            if (x - 1 >= 0)
                result[3] = roomsSpan[x - 1, y];
            else
                result[3] = null;
        }

        // DONE: look at suggestions in this method
        private byte GetNeighborCount(int x, int y)
        {
            SpanGrid<RoomInfo> roomsSpan = _rooms.SpanGrid;

            byte result = 8;
            bool2 vertFlags = new(false, false);

            if (y + 1 >= _generationInfo.SideSize)
            {
                result--;
                vertFlags.x = true;
            }
            else if (roomsSpan[x, y + 1] == null)
            {
                result--;
            }

            if (y - 1 < 0)
            {
                result--;
                vertFlags.y = true;
            }
            else if (roomsSpan[x, y - 1] == null)
            {
                result--;
            }

            if (x + 1 < _generationInfo.SideSize)
            {
                if (roomsSpan[x + 1, y] == null)
                    result--;

                if (vertFlags.x || roomsSpan[x + 1, y + 1] == null)
                    result--;
                if (vertFlags.y || roomsSpan[x + 1, y - 1] == null)
                    result--;
            }
            else
            {
                result -= 3;
            }

            if (x - 1 >= 0)
            {
                if (roomsSpan[x - 1, y] == null)
                    result--;

                if (vertFlags.x || roomsSpan[x - 1, y + 1] == null)
                    result--;
                if (vertFlags.y || roomsSpan[x - 1, y - 1] == null)
                    result--;
            }
            else
            {
                result -= 3;
            }

            return result;
        }

        [HideInCallstack]
        private void Log(string msg, DebugInfo debugType = DebugInfo.Minimal)
        {
            if (debugType == DebugInfo.Full && _debugInfoSettings != DebugInfo.Full)
                return;

            Debug.Log(msg);
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
                _spiral = new int2(_sideSize / 2, _sideSize / 2);

                if (direction is 0 or 2)
                    _allowedCall.y = 1;
                else
                    _allowedCall.x = 1;

                _called = 0;
            }

            /// <summary>
            /// Resets spiral indexer object.
            /// </summary>
            public void Reset(byte direction)
            {
                _allowedCall = int2.zero;
                _dir = direction;
                _spiral = new int2(_sideSize / 2, _sideSize / 2);

                if (direction is 0 or 2)
                    _allowedCall.y = 1;
                else
                    _allowedCall.x = 1;

                _called = 0;
            }

            /// <summary>
            /// Gets an index on the grid of a spiral, starting from the center and increasing every time.
            /// </summary>
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

                    // DONE: double check this works properly. Is replacing `if (_dir > 3) _dir = 0`.
                    _dir = (byte)(++_dir % 4);
                }

                // DONE: if getting weird stair pattern or anything major wrong with generation, convert to old way
                // think old way was causing index out of bounds exceptions, changed to how it is now.
                var result = _spiral;

                switch (_dir)
                {
                    case 0:
                        _spiral.x++;
                        break;
                    case 1:
                        _spiral.y++;
                        break;
                    case 2:
                        _spiral.x--;
                        break;
                    case 3:
                        _spiral.y--;
                        break;
                }

                return result;
            }
        }

        private struct SortedRoomInfoComparer : IComparer<(RoomInfo room, int walkCount)>
        {
            public int Compare((RoomInfo room, int walkCount) x, (RoomInfo room, int walkCount) y)
            {
                var r = y.room.walkData[y.walkCount].walkValue - x.room.walkData[x.walkCount].walkValue;
                return r == 0 ? -1 : r;
            }
        }
    }
}