using UnityEngine;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Room Type", menuName = "ChanceGen/new Room Type", order = 0)]
    public class RoomType : ScriptableObject
    {
        [field: SerializeField] public bool IsSpecial { get; private set; }
    }
}