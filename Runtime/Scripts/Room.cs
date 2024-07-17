using UnityEngine;

namespace ChanceGen
{
    public abstract class Room : MonoBehaviour
    {
        public abstract void EnableWalls(Connections nodeDataConnections);
    }
}