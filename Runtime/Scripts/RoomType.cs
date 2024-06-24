using UnityEngine;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Room Type", menuName = "ChanceGen/new Room Type", order = 0)]
    public class RoomType : ScriptableObject
    {
        [field: SerializeField] public bool IsSpecial { get; private set; }
        
        // TODO: make array of rooms that is randomly chosen from in this room
    }
}