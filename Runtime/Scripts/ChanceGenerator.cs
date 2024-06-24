using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ChanceGen.Attributes;
using ChanceGen.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;
using static ChanceGen.DebugInfoSettings;

namespace ChanceGen
{
    // TODO: needs constructor
    public class ChanceGenerator
    {
        [field: SerializeField, ReadOnlyInInspector]
        public bool IsGenerating { get; private set; }

        [field: SerializeField, ReadOnlyInInspector]
        public bool Used { get; private set; }

        private GenerationInfo _generationInfo;
        private ConwayRules _removeRules;
        private ConwayRules _addRules;
        private SpecialRule _spawnRoomRule;
        private SpecialRule _bossRoomRule;
        private ReadOnlyMemory<SpecialRule> _additionalSpecialRules;
        private DebugInfoSettings _debugInfoSettings;

        private MemoryGrid<RoomInfo> _rooms;
        private Random _random;
        private SpiralIndexer _spiralIndexer;
        private float _chance;

        // DONE: make way to create generator in new ChanceGenerator script
        private ChanceGenerator(GenerationInfo generationInfo,
            ConwayRules removeRules,
            ConwayRules addRules,
            SpecialRule spawnRoomRule,
            SpecialRule bossRoomRule,
            ReadOnlyMemory<SpecialRule> additionalSpecialRules,
            DebugInfoSettings debugInfoSettings)
        {
            _generationInfo = generationInfo;
            _removeRules = removeRules;
            _addRules = addRules;
            _spawnRoomRule = spawnRoomRule;
            _bossRoomRule = bossRoomRule;
            _additionalSpecialRules = additionalSpecialRules;
            _debugInfoSettings = debugInfoSettings;

            _rooms = new RoomInfo[generationInfo.SideSize, generationInfo.SideSize];
            _random = new Random(generationInfo.seed);
            _spiralIndexer = new SpiralIndexer((byte)_random.NextInt(0, 4), generationInfo.SideSize);
        }

        public IEnumerator Generate()
        {
            if (Used)
                throw new InvalidOperationException("Cannot reuse generators.");

            if (IsGenerating)
            {
                Debug.LogWarning($"Trying to start generation on a {nameof(ChanceGenerator)} instance while it is "
                                 + $"already generating! This is not supported.");
                yield break;
            }

            IsGenerating = true;

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

                roomsSpan[spiralIndex.x, spiralIndex.y] = SpawnRoom(spiralIndex);

                if ((_chance += _generationInfo.RandomChanceIncrease) > 1)
                    break;

                if (_debugInfoSettings.GenerationSpeed != 0)
                    yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
            }

            yield return null;


            // do generation add/remove rules
            for (var i = 0; i < _generationInfo.SideSize; i++)
            {
                for (var j = 0; j < _generationInfo.SideSize; j++)
                {
                    var neighborCount = GetNeighborCount(i, j);

                    // remove rooms based on remove rules
                    if ((neighborCount <= _removeRules.LimitLTET || neighborCount > _removeRules.LimitGT)
                        && !(i == _generationInfo.SideSize / 2 && j == _generationInfo.SideSize / 2)
                        && _random.NextFloat() < _removeRules.ActionChance
                        && roomsSpan[i, j] != null
                       )
                    {
                        Object.Destroy(roomsSpan[i, j].gameObject);
                        Log($"Destroyed by RemoveRooms: {roomsSpan[i, j].gameObject.name}");
                        roomsSpan[i, j] = null;

                        if (_debugInfoSettings.GenerationSpeed != 0)
                            yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
                    }
                    // add rooms based on add rules
                    else if (neighborCount <= _addRules.LimitLTET
                             && neighborCount > _addRules.LimitGT
                             && roomsSpan[i, j] == null
                             && _random.NextFloat() < _addRules.ActionChance
                            )
                    {
                        roomsSpan[i, j] = SpawnRoom(new int2(i, j));
                        Log($"Added by AddRooms: {roomsSpan[i, j].gameObject.name}");

                        if (_debugInfoSettings.GenerationSpeed != 0)
                            yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
                    }
                }
            }

            yield return null;

            IEnumerator<Tuple<int, Memory<RoomInfo>>> walk1 =
                Walk(new int2(_generationInfo.SideSize / 2, _generationInfo.SideSize / 2), 0);
            Tuple<int, Memory<RoomInfo>> walkResults = null;

            // do first walk with enumerator returned from Walk method
            while (walk1.MoveNext())
            {
                walkResults = walk1.Current;
                if (_debugInfoSettings.GenerationSpeed == 0) continue;

                if (walkResults?.Item1 != -1)
                    yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
                else
                    yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed / 4f);
            }

            var walkCount = walkResults!.Item1;
            Span<RoomInfo> orderedWalk = walkResults.Item2.Span;

            // if walk count too low, regenerate
            if (walkCount < _generationInfo.RegenerateLimit)
            {
                Log($"walkCount was {walkCount} and RegenerateLimit is {_generationInfo.RegenerateLimit}, "
                    + $"regenerating...");

                if (_debugInfoSettings.GenerationSpeed != 0)
                {
                    for (var i = 0; i < orderedWalk.Length; i++)
                    {
                        if (orderedWalk[i] == null) continue;

                        orderedWalk[i].Invalid = true;

                        yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed / 2f);
                    }
                }

                yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);

                ResetGenerationLoop();
                goto retry;
            }

            // if walk count too high, remove rooms
            if (walkCount > _generationInfo.ShrinkLimit)
            {
                var i = 0;
                for (; i < orderedWalk.Length; i++)
                {
                    Log($"Destroyed by Trim: {orderedWalk[i].gameObject.name}");

                    Object.Destroy(orderedWalk[i].gameObject);
                    roomsSpan[orderedWalk[i].gridPosition.x, orderedWalk[i].gridPosition.y] = null;
                    orderedWalk[i] = null;
                    walkCount--;

                    if (walkCount <= _generationInfo.ShrinkLimit)
                        break;

                    if (_debugInfoSettings.GenerationSpeed != 0)
                        yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
                }

                orderedWalk = orderedWalk[(i + 1)..];
                Span<RoomInfo> buffer4 = new RoomInfo[4]; // TODO: look into using pointer

                for (var j = 0; j < orderedWalk.Length; j++)
                    UpdateRoomConnections(buffer4, orderedWalk[j]);
            }

            yield return null;

            // delete rooms not covered by first walk
            for (var i = 0; i < _generationInfo.SideSize; i++)
            {
                for (var j = 0; j < _generationInfo.SideSize; j++)
                {
                    if (roomsSpan[i, j] == null || roomsSpan[i, j].walkData[0].walkValue == 0) continue;

                    Log($"Destroyed by walk: {roomsSpan[i, j].gameObject.name}");
                    Object.Destroy(roomsSpan[i, j].gameObject);
                    roomsSpan[i, j] = null;

                    if (_debugInfoSettings.GenerationSpeed != 0)
                        yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
                }
            }

            yield return null;

            // do new walk to set special rooms
            var spawnIndex = GetSpecialIndex(_spawnRoomRule.MaxSteps, _spawnRoomRule.ChanceIncreasePerStepAttempt);
            orderedWalk[spawnIndex].roomType = _spawnRoomRule.RoomType;

            IEnumerator<Tuple<int, Memory<RoomInfo>>> walk2 = Walk(orderedWalk[spawnIndex].gridPosition, 1);

            while (walk2.MoveNext())
            {
                walkResults = walk2.Current;
                if (_debugInfoSettings.GenerationSpeed == 0) continue;

                if (walkResults?.Item1 != -1)
                    yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed);
                else
                    yield return new WaitForSeconds(_debugInfoSettings.GenerationSpeed / 4f);
            }

            orderedWalk = walkResults!.Item2.Span;
            var bossIndex = GetSpecialIndex(_bossRoomRule.MaxSteps, _bossRoomRule.ChanceIncreasePerStepAttempt);
            orderedWalk[bossIndex].roomType = _bossRoomRule.RoomType;

            yield return null;

            // do additional room rules, skipping if chosen room is spawn or boss if 2 tries reached
            ReadOnlySpan<SpecialRule> additionalRoomRules = _additionalSpecialRules.Span;
            Span<RoomInfo> neighborsBuffer4 = new RoomInfo[4];
            for (var i = 0; i < additionalRoomRules.Length; i++)
            {
                const int tries = 2;
                for (var j = 0; j < tries; j++)
                {
                    var index = GetSpecialIndex(additionalRoomRules[i].MaxSteps,
                        additionalRoomRules[i].ChanceIncreasePerStepAttempt);

                    var index2D = orderedWalk[index].gridPosition;
                    GetNeighborsAdjacent(neighborsBuffer4, index2D.x, index2D.y);

                    if (orderedWalk[i]) continue;

                    if (additionalRoomRules[i].ShouldGenerate(neighborsBuffer4, index, index2D))
                        orderedWalk[index].roomType = additionalRoomRules[i].RoomType;

                    break;
                }
            }

            yield return null;

            // finish
            IsGenerating = false;
            Used = true;

            yield break;

            void ResetGenerationLoop()
            {
                DestroyRooms();
                _spiralIndexer.Reset((byte)_random.NextInt(0, 4));
            }
        }

        private RoomInfo SpawnRoom(int2 index)
        {
            GameObject n = new()
            {
                name = $"{index.x}, {index.y}",
                transform =
                {
                    // TODO: allow axis selection
                    position = new Vector3(index.x, 0, index.y)
                }
            };

            var r = n.AddComponent<RoomInfo>();
            r.gridPosition = index;

            Log($"spawning room: {r.gameObject.name}", DebugInfo.Full);
            return r;
        }

        private int GetSpecialIndex(int max, float chanceIncreasePerStep)
        {
            var result = 0;
            var addChance = 0f;

            while (addChance <= 1)
            {
                if (_random.NextFloat() >= addChance)
                    result++;

                addChance += chanceIncreasePerStep;
            }

            return math.clamp(result, 0, max);
        }

        /* TODO: look into getting away from IEnumerator so can use Span.
         * Probably need wrapper class to make it easy for people to generate, with IEnumerable to wait on in wrapper class.
         * In Unity 6, use Awaitable instead? Shall see how Span can be passed around.
         */
        // Walks rooms using walk algorithm, returning
        private IEnumerator<Tuple<int, Memory<RoomInfo>>> Walk(int2 startPos, int walkDataIndex)
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
                GetNeighborsAdjacent(neighbors, working.gridPosition.x, working.gridPosition.y);
                var min = int.MaxValue;
                // for every neighbor, check its walk value and check if queued
                for (var i = 0; i < neighbors.Length; i++)
                {
                    if (neighbors[i] == null) continue;

                    if (neighbors[i].walkData[walkDataIndex].walkValue < min)
                        min = neighbors[i].walkData[walkDataIndex].walkValue;

                    working.connections |= (RoomConnections)(1 << i);

                    if (neighbors[i].walkData[walkDataIndex].walkValue != 0
                        || neighbors[i].walkData[walkDataIndex].queued
                       ) continue;

                    open.Enqueue(neighbors[i]);
                    neighbors[i].walkData[walkDataIndex].queued = true;
                    walkCount++;
                }

                // TODO: check that this works, before would use max found not min found.
                working.walkData[walkDataIndex].walkValue = min + 1; // sets this walk to the smallest value found + 1.
                orderedSet.Add((working, walkDataIndex));

                if (!_debugInfoSettings.ShowWalk) continue; // if not debugging, don't show walk

                working.Selected = 1;
                yield return null;
                working.Selected = 2;
            }

            Memory<RoomInfo> orderedWalk = Array.ConvertAll(orderedSet.ToArray(), item => item.Item1);

            if (!_debugInfoSettings.ShowWalk)
            {
                yield return new Tuple<int, Memory<RoomInfo>>(walkCount, orderedWalk);
            }
            else // if debugging, show walk
            {
                Tuple<int, Memory<RoomInfo>> result = new(walkCount, orderedWalk);

                Span<RoomInfo> orderedRoomSpan = result.Item2.Span;

                for (var i = 1; i <= orderedRoomSpan.Length; i++)
                {
                    orderedRoomSpan[^1].Selected = 0;
                    yield return new Tuple<int, Memory<RoomInfo>>(-1, null);
                }

                yield return result;
            }
        }

        private void DestroyRooms()
        {
            SpanGrid<RoomInfo> rooms = _rooms.SpanGrid;

            for (var i = 0; i < _generationInfo.SideSize; i++)
            {
                for (var j = 0; j < _generationInfo.SideSize; j++)
                {
                    if (rooms[i, j] != null)
                    {
                        Object.Destroy(rooms[i, j].gameObject);
                        Log($"Destroyed {rooms[i, j]} from DestroyRooms method", DebugInfo.Full);
                        rooms[i, j] = null;
                    }

                    rooms[i, j] = null;
                }
            }
        }

        private void UpdateRoomConnections(Span<RoomInfo> buffer, RoomInfo room)
        {
            GetNeighborsAdjacent(buffer, room.gridPosition.x, room.gridPosition.y);

            for (var i = 0; i < 4; i++)
            {
                Log(
                    $"Updating room connections for {room.gameObject.name} - {(RoomConnections)(1 << i)}: {buffer[i].gameObject.name}",
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
                result--;

            if (y - 1 < 0)
            {
                result--;
                vertFlags.y = true;
            }
            else if (roomsSpan[x, y - 1] == null)
                result--;

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
                result -= 3;

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
                result -= 3;

            return result;
        }

        private void Log(string msg, DebugInfo debugType = DebugInfo.Minimal)
        {
            if (debugType == DebugInfo.Full && _debugInfoSettings.DebuggingInfo != DebugInfo.Full)
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