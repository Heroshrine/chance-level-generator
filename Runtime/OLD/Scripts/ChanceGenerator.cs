using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OLD
{
    public class ChanceGenerator : IDisposable
    {
        public bool isGenerating;

        private GenerationInfo generationInfo;
        private ConwayRules removeRules;
        private ConwayRules addRules;
        private SpecialRoomRules specialRoomRules;
        private DebugInfoSettings debugInfoSettings;

        private RoomData[,] rooms;
        private Unity.Mathematics.Random random;
        private SpiralIndexer spiralIndexer;
        private float chance;

        private Iterator iterator;

        // DONE: make way to create generator in new ChanceGenerator script
        private ChanceGenerator() { }

        /// <summary>
        /// Stops the generate coroutine that is running and destroys all objects generated.
        /// </summary>
        public void Dispose()
        {
            if (iterator != null)
            {
                iterator.StopAllCoroutines();
                Object.Destroy(iterator.gameObject);
            }

            DeleteArray();
            isGenerating = false;
        }

        private void DeleteArray()
        {
            for (int i = 0; i < generationInfo.Side; i++)
            {
                for (int j = 0; j < generationInfo.Side; j++)
                {
                    if (rooms[i, j] != null)
                    {
                        Object.Destroy(rooms[i, j].gameObject);
                        Log($"Destroyed {rooms[i, j]} from Destroy Function", DebugInfoSettings.DebugInfo.Detailed);
                        rooms[i, j] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Starts a the coroutine that generates a level.
        /// </summary>
        /// <returns>True if generation started, false if not.</returns>
        public bool Generate(GenerationInfo generationInfo,
            ConwayRules removeRules,
            ConwayRules addRules,
            SpecialRoomRules specialRoomRules,
            DebugInfoSettings debugInfoSettings)
        {
            if (isGenerating)
                return false;

            this.generationInfo = generationInfo;
            this.removeRules = removeRules;
            this.addRules = addRules;
            this.specialRoomRules = specialRoomRules;
            this.debugInfoSettings = debugInfoSettings;

            rooms = new RoomData[generationInfo.Side, generationInfo.Side];
            random = new Unity.Mathematics.Random(generationInfo.seed);

            iterator = new GameObject() { name = "Level Generator Iterator" }.AddComponent<Iterator>();
            iterator.StartCoroutine(Generate());
            return true;
        }

        private IEnumerator Generate()
        {
            Log($"Starting generation with seed: {generationInfo.seed}", DebugInfoSettings.DebugInfo.Minimal);
            Log($"generationInfo.Side: {generationInfo.Side}");
            Log($"generationInfo.SideSize: {generationInfo.Size}");

            isGenerating = true;
            retry:
            spiralIndexer = new((byte)random.NextInt(0, 4), generationInfo.Side);
            chance = 0;

            // spiral place rooms
            for (int i = 0; i < generationInfo.Size; i++)
            {
                int2 j = spiralIndexer.GetIndexSpiral();

                if (random.NextFloat() >= chance)
                {
                    rooms[j.x, j.y] = SpawnRoom(j.x, j.y);

                    chance += generationInfo.RandomChanceDecrease;
                    if (chance > 1)
                        break;

                    if (debugInfoSettings.GenerationSpeed != 0)
                        yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                }
            }

            yield return null;

            // remove rooms based on rules
            for (int i = 0; i < generationInfo.Side; i++)
            {
                for (int j = 0; j < generationInfo.Side; j++)
                {
                    byte gnr = GetNeighborCount(i, j);

                    if ((gnr <= removeRules.LimitLTET || gnr > removeRules.LimitGT)
                        && !(i == generationInfo.Side / 2 && j == generationInfo.Side / 2)
                        && random.NextFloat() <= removeRules.ActionChance // DONE: convert to just less than in rewrite
                        && rooms[i, j] != null)
                    {
                        Object.Destroy(rooms[i, j].gameObject);
                        Log($"Destroyed By RemoveRooms: {rooms[i, j]}");
                        rooms[i, j] = null;

                        if (debugInfoSettings.GenerationSpeed != 0)
                            yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                    }
                    else if ((gnr <= addRules.LimitLTET && gnr > addRules.LimitGT)
                             && rooms[i, j] == null
                             && random.NextFloat()
                             <= addRules.ActionChance) // DONE: convert to just less than in rewrite
                    {
                        rooms[i, j] = SpawnRoom(i, j);
                        Log($"Added By AddRooms: {rooms[i, j]}");

                        if (debugInfoSettings.GenerationSpeed != 0)
                            yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                    }
                }
            }

            yield return null;

            // add rooms based on rules
            //for (int i = 0; i < generationInfo.Side; i++)
            //{
            //    for (int j = 0; j != generationInfo.Side; j++)
            //    {
            //        byte gnr = GetNeighborCount(i, j);

            //        if ((gnr <= addRules.LimitLTET && gnr > addRules.LimitGT) && rooms[i, j] == null && random.NextFloat() <= addRules.ActionChance)
            //        {
            //            rooms[i, j] = SpawnRoom(i, j);
            //            Log($"Added By AddRooms: {rooms[i, j]}");

            //            if (debugInfoSettings.GenerationSpeed != 0)
            //                yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
            //        }
            //    }
            //}

            //yield return null;

            // walk rooms
            IEnumerator<System.Tuple<int, RoomData[]>> walk1 =
                Walk(new int2(generationInfo.Side / 2, generationInfo.Side / 2), 0);

            System.Tuple<int, RoomData[]> result = null;
            while (walk1.MoveNext())
            {
                result = walk1.Current;
                if (debugInfoSettings.GenerationSpeed != 0)
                {
                    if (result?.Item1 != -1)
                        yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                    else
                        yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed / 4);
                }
            }

            int walkCount = result.Item1;
            RoomData[] orderedWalk = result.Item2;

            // regen if low room count
            if (walkCount < generationInfo.RegenerateLimit)
            {
                Log($"walkCount was {walkCount}, regenerating...", DebugInfoSettings.DebugInfo.Minimal);

                if (debugInfoSettings.GenerationSpeed != 0)
                    for (int i = 0; i < orderedWalk.Length; i++)
                    {
                        if (orderedWalk[i] != null)
                        {
                            orderedWalk[i].invalid = true;

                            yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                        }
                    }

                DeleteArray();

                for (int i = 0; i < generationInfo.Side; i++)
                {
                    for (int j = 0; j < generationInfo.Side; j++)
                    {
                        rooms[i, j] = null;
                    }
                }

                yield return null;
                goto retry;
            }

            // trim if high room count
            if (walkCount > generationInfo.ShrinkLimit)
            {
                RoomData[] buffer4 = new RoomData[4];
                int i = 0;
                for (; i < orderedWalk.Length; i++)
                {
                    Object.Destroy(orderedWalk[i].gameObject);
                    rooms[orderedWalk[i].gridPosition.x, orderedWalk[i].gridPosition.y] = null;
                    orderedWalk[i] = null;
                    walkCount--;

                    Log($"Destroyed by Trim: {orderedWalk[i]}");

                    if (walkCount <= generationInfo.ShrinkLimit)
                        break;

                    if (debugInfoSettings.GenerationSpeed != 0)
                        yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                }

                orderedWalk = orderedWalk[(i + 1)..];

                for (int j = 0; j < orderedWalk.Length; j++)
                    UpdateRoomConnections(buffer4, orderedWalk[j]);
            }

            yield return null;

            for (int i = 0; i < generationInfo.Side; i++)
            {
                for (int j = 0; j < generationInfo.Side; j++)
                {
                    if (rooms[i, j] != null && rooms[i, j].walkData[0].walkValue == 0)
                    {
                        Object.Destroy(rooms[i, j].gameObject);
                        Log($"Destroyed By Walk: {rooms[i, j]}");
                        rooms[i, j] = null;

                        if (debugInfoSettings.GenerationSpeed != 0)
                            yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                    }
                }
            }

            yield return null;

            // set special rooms
            int bossIndex = GetSpecialIndex(specialRoomRules.BossMaxSteps);
            orderedWalk[bossIndex].roomType = RoomType.Boss;

            IEnumerator<System.Tuple<int, RoomData[]>> walk2 = Walk(orderedWalk[bossIndex].gridPosition, 1);

            while (walk2.MoveNext())
            {
                result = walk2.Current;
                if (debugInfoSettings.GenerationSpeed != 0)
                {
                    if (result?.Item1 != -1)
                        yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed);
                    else
                        yield return new WaitForSeconds(debugInfoSettings.GenerationSpeed / 4);
                }
            }

            orderedWalk = result.Item2;

            orderedWalk[GetSpecialIndex(specialRoomRules.SpawnMaxSteps)].roomType = RoomType.Spawn;

            // finish
            isGenerating = false;

            Object.Destroy(iterator.gameObject);
            iterator = null;

            RoomData SpawnRoom(int i, int j)
            {
                GameObject n = new()
                {
                    name = $"{i}, {j}",
                };
                n.transform.position = new Vector3(i, 0, j);
                RoomData r = n.AddComponent<RoomData>();
                r.gridPosition = new int2(i, j);

                Log($"spawning: {r}", DebugInfoSettings.DebugInfo.Detailed);
                return r;
            }
        }

        private int GetSpecialIndex(int max)
        {
            int result = 0;
            float addChance = 0.1f;

            while (addChance <= 1)
            {
                if (random.NextFloat() >= addChance)
                {
                    result++;
                }

                addChance += specialRoomRules.ChanceDecreasePerStep;
            }

            return math.clamp(result, 0, max);
        }

        private IEnumerator<System.Tuple<int, RoomData[]>> Walk(int2 startPos, int walkDataIndex)
        {
            SortedSet<(RoomData, int)> orderedSet = new(new SortedRoomDataComparer());
            Queue<RoomData> open = new();
            RoomData[] neighborRoomArray = new RoomData[4];

            open.Enqueue(rooms[startPos.x, startPos.y]);

            rooms[startPos.x, startPos.y].walkData[walkDataIndex].walkValue = 1;
            rooms[startPos.x, startPos.y].walkData[walkDataIndex].queued = true;
            int walkCount = 1;
            while (open.Count > 0)
            {
                RoomData working = open.Dequeue();
                working.contiguous = true;

                GetNeighborsAdjacent(neighborRoomArray, working.gridPosition.x, working.gridPosition.y);
                int max = 0;
                for (int i = 0; i < neighborRoomArray.Length; i++)
                {
                    if (neighborRoomArray[i] != null)
                    {
                        if (neighborRoomArray[i].walkData[walkDataIndex].walkValue > max)
                            max = neighborRoomArray[i].walkData[walkDataIndex].walkValue;

                        working.connections |= (Connections)(1 << i);

                        if (neighborRoomArray[i].walkData[walkDataIndex].walkValue == 0
                            && !neighborRoomArray[i].walkData[walkDataIndex].queued)
                        {
                            open.Enqueue(neighborRoomArray[i]);
                            neighborRoomArray[i].walkData[walkDataIndex].queued = true;
                            walkCount++;
                        }
                    }
                }

                working.walkData[walkDataIndex].walkValue = max + 1;
                orderedSet.Add((working, walkDataIndex));

                if (debugInfoSettings.ShowWalk)
                {
                    working.selected = 1;
                    yield return null;
                    working.selected = 2;
                }
            }

            RoomData[] orderedWalk = System.Array.ConvertAll(orderedSet.ToArray(), item => item.Item1);

            if (!debugInfoSettings.ShowWalk)
                yield return new System.Tuple<int, RoomData[]>(walkCount, orderedWalk);
            else
            {
                System.Tuple<int, RoomData[]> result = new(walkCount, orderedWalk);

                for (int i = 1; i <= orderedWalk.Length; i++)
                {
                    orderedWalk[^i].selected = 0;
                    yield return new(-1, null);
                }

                yield return result;
            }
        }

        private void UpdateRoomConnections(RoomData[] buffer, RoomData room)
        {
            GetNeighborsAdjacent(buffer, room.gridPosition.x, room.gridPosition.y);

            for (int i = 0; i < 4; i++)
            {
                Log($"Updating Room Connections for {room} - {i}: {buffer[i]}", DebugInfoSettings.DebugInfo.Detailed);

                if (buffer[i] == null)
                    room.connections &= ~(Connections)(1 << i);
                else
                    room.connections |= (Connections)(1 << i);
            }
        }

        private void GetNeighborsAdjacent(RoomData[] result, int x, int y)
        {
            if (y + 1 < generationInfo.Side)
                result[0] = rooms[x, y + 1];
            else
                result[0] = null;

            if (y - 1 >= 0)
                result[1] = rooms[x, y - 1];
            else
                result[1] = null;

            if (x + 1 < generationInfo.Side)
                result[2] = rooms[x + 1, y];
            else
                result[2] = null;

            if (x - 1 >= 0)
                result[3] = rooms[x - 1, y];
            else
                result[3] = null;
        }

        private byte GetNeighborCount(int x, int y)
        {
            byte result = 8;
            bool2 vertFlags = new(false, false);

            if (y + 1 >= generationInfo.Side)
            {
                result--;
                vertFlags.x = true;
            }
            else if (rooms[x, y + 1] == null)
                result--;

            if (y - 1 < 0)
            {
                result--;
                vertFlags.y = true;
            }
            else if (rooms[x, y - 1] == null)
                result--;

            if (x + 1 < generationInfo.Side)
            {
                if (rooms[x + 1, y] == null)
                    result--;

                if (vertFlags.x || rooms[x + 1, y + 1] == null)
                    result--;
                if (vertFlags.y || rooms[x + 1, y - 1] == null)
                    result--;
            }
            else
                result -= 3;

            if (x - 1 >= 0)
            {
                if (rooms[x - 1, y] == null)
                    result--;

                if (vertFlags.x || rooms[x - 1, y + 1] == null)
                    result--;
                if (vertFlags.y || rooms[x - 1, y - 1] == null)
                    result--;
            }
            else
                result -= 3;

            return result;
        }

        private struct SpiralIndexer
        {
            public SpiralIndexer(byte dir, int sideSize)
            {
                allowedCall = int2.zero;
                this.dir = dir;

                spiral = new(sideSize / 2, sideSize / 2);

                if (dir == 0 || dir == 2)
                    allowedCall.y = 1;
                else
                    allowedCall.x = 1;

                called = 0;
            }

            public void CalledOveride(int value) => called = value;

            byte dir;
            int2 spiral;
            int2 allowedCall;
            int called;

            public int2 GetIndexSpiral()
            {
                called++;

                if (((dir == 1 || dir == 3) ? allowedCall.y : allowedCall.x) - called <= 0)
                {
                    if (dir == 1 || dir == 3)
                        allowedCall.y += 1;
                    else
                        allowedCall.x += 1;

                    called = 0;

                    dir++;
                    if (dir > 3)
                        dir = 0;
                }

                int2 result = spiral;

                switch (dir)
                {
                    case 0:
                        spiral.x++;
                        break;
                    case 1:
                        spiral.y++;
                        break;
                    case 2:
                        spiral.x--;
                        break;
                    case 3:
                        spiral.y--;
                        break;
                }

                return result;
            }
        }

        [System.Serializable]
        private struct WalkData
        {
            public int walkValue;
            public bool queued;
        }

        private class RoomData : MonoBehaviour
        {
            public bool contiguous;
            public byte selected;
            public bool invalid;

            public WalkData[] walkData = new WalkData[2];

            public int2 gridPosition;

            public Connections connections;
            public RoomType roomType;

            private void OnDrawGizmos()
            {
                Color use = Color.clear;
                if (contiguous)
                {
                    switch (roomType)
                    {
                        case RoomType.Normal:
                            use = Color.cyan;
                            break;
                        case RoomType.Spawn:
                            use = Color.green;
                            break;
                        case RoomType.Boss:
                            use = Color.gray;
                            break;
                            ;
                    }

                    use.a = 0.8f;
                }
                else
                {
                    use = Color.blue;
                    use.a = 0.8f;
                }

                if (selected == 1)
                {
                    use = Color.magenta;
                    use.a = 0.9f;
                }
                else if (selected == 2)
                {
                    use = Color.magenta / 2;
                    use.a = 0.55f;
                }

                if (invalid)
                    use = Color.red;

                Gizmos.color = use;

                Gizmos.DrawCube(transform.position, Vector3.one * 0.75f);

                Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.6f);

                for (int i = 0; i < 4; i++)
                {
                    if (((int)connections & (1 << i)) != 0)
                    {
                        switch (i)
                        {
                            case 0:
                                Gizmos.DrawCube(transform.position + new Vector3(0, 0, 0.25f), Vector3.one * 0.25f);
                                break;
                            case 1:
                                Gizmos.DrawCube(transform.position + new Vector3(0, 0, -0.25f), Vector3.one * 0.25f);
                                break;
                            case 2:
                                Gizmos.DrawCube(transform.position + new Vector3(0.25f, 0, 0), Vector3.one * 0.25f);
                                break;
                            case 3:
                                Gizmos.DrawCube(transform.position + new Vector3(-0.25f, 0, 0), Vector3.one * 0.25f);
                                break;
                        }
                    }
                }
            }
        }

        private void Log(string msg, DebugInfoSettings.DebugInfo debugType = DebugInfoSettings.DebugInfo.Extra)
        {
            if (debugInfoSettings.DebuggingInfo == DebugInfoSettings.DebugInfo.Detailed
                || (debugInfoSettings.DebuggingInfo == DebugInfoSettings.DebugInfo.Extra
                    && debugType != DebugInfoSettings.DebugInfo.Detailed)
                || debugType == DebugInfoSettings.DebugInfo.Minimal)
                Debug.Log(msg);
        }

        public struct SortedRoomDataComparer : IComparer<(RoomData, int)>
        {
            int IComparer<(RoomData, int)>.Compare((RoomData, int) x, (RoomData, int) y)
            {
                int r = y.Item1.walkData[y.Item2].walkValue - x.Item1.walkData[x.Item2].walkValue;
                return r == 0 ? -1 : r;
            }
        }

        // REFACTOR: done
        [System.Flags]
        public enum Connections
        {
            None = 0,
            Up = 1,
            Down = 2,
            Right = 4,
            Left = 8,
            All = Up | Down | Left | Right
        }

        // REFACTOR: done
        public enum RoomType
        {
            Normal,
            Boss,
            Spawn
        }

        private class Iterator : MonoBehaviour { }
    }

    [System.Serializable]
    public struct GenerationInfo
    {
        public readonly uint seed;
        public int Side => size;
        public int Size => sizeFull;
        public float RandomChanceDecrease => randomChanceDecrease;
        public int RegenerateLimit => regenerateLimit;
        public int ShrinkLimit => shrinkLimit;

        [SerializeField] int size;
        readonly int sizeFull;

        [SerializeField] float randomChanceDecrease;

        [SerializeField] int regenerateLimit;
        [SerializeField] int shrinkLimit;

        public GenerationInfo(uint seed, int size, float randomChanceDecrease, int regenerateLimit, int shrinkLimit)
        {
            Debug.Assert(size % 2 == 0, "size should be odd");

            this.seed = seed;
            this.size = size;
            sizeFull = size * size;
            this.randomChanceDecrease = randomChanceDecrease;
            this.regenerateLimit = regenerateLimit;
            this.shrinkLimit = shrinkLimit;
        }
    }

    [System.Serializable]
    public struct ConwayRules
    {
        public int LimitLTET => limitLTET;
        public int LimitGT => limitGT;
        public float ActionChance => actionChance;

        [SerializeField] int limitLTET;
        [SerializeField] int limitGT;

        [SerializeField] float actionChance;

        public ConwayRules(int limitLTET, int limitGT, float actionChance)
        {
            this.limitLTET = limitLTET;
            this.limitGT = limitGT;
            this.actionChance = actionChance;
        }
    }

    [System.Serializable]
    public struct SpecialRoomRules
    {
        public int BossMaxSteps => bossMaxSteps;
        public int SpawnMaxSteps => spawnMaxSteps;
        public float ChanceDecreasePerStep => chanceDecreasePerStep;

        [SerializeField] int bossMaxSteps;
        [SerializeField] int spawnMaxSteps;
        [Space] [SerializeField] float chanceDecreasePerStep;

        public SpecialRoomRules(int bossMaxSteps, int spawnMaxSteps, int chanceDecreasePerStep)
        {
            this.bossMaxSteps = bossMaxSteps;
            this.spawnMaxSteps = spawnMaxSteps;
            this.chanceDecreasePerStep = chanceDecreasePerStep;
        }
    }

    [System.Serializable]
    public record DebugInfoSettings
    {
        public float GenerationSpeed => generationSpeed;
        public bool ShowWalk => showWalk;
        public DebugInfo DebuggingInfo => debugInfo;

        [Range(0, 0.25f)] [SerializeField] float generationSpeed;
        [SerializeField] bool showWalk;
        [SerializeField] DebugInfo debugInfo;

        public DebugInfoSettings(float generationSpeed, bool showWalk, DebugInfo debugInfo)
        {
            this.debugInfo = debugInfo;
            this.showWalk = showWalk;
            this.generationSpeed = generationSpeed;
        }

        public enum DebugInfo
        {
            Minimal,
            Extra,
            Detailed
        }
    }
}