using System;

namespace ChanceGen
{
    [Flags]
    public enum Connections : byte
    {
        None = 0,
        Up = 1,
        Down = 1 << 2,
        Left = 1 << 3,
        Right = 1 << 1,
        All = Up | Down | Left | Right
    }

    public static class ConnectionsUtils
    {
        public static int CountConnections(Connections connections)
        {
            return (int)(connections & Connections.Up)
                   + ((int)(connections & Connections.Down) >> 2)
                   + ((int)(connections & Connections.Left) >> 3)
                   + ((int)(connections & Connections.Right) >> 1);
        }
    }
}