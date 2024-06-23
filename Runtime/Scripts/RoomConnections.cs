using System;

namespace ChanceGen
{
    [Flags]
    public enum RoomConnections
    {
        None = 0,
        Up = 1,
        Down = 2,
        Right = 4,
        Left = 8,
        All = Up | Down | Left | Right
    }
}