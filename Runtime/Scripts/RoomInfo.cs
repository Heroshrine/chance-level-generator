using ChanceGen.Attributes;
using UnityEngine;
using System;
using Unity.Mathematics;

namespace ChanceGen
{
    // TODO: needs to be monobehaviour? probably not.
    public class RoomInfo : MonoBehaviour
    {
        [field: SerializeField, ReadOnlyInInspector]
        public bool Contiguous { get; internal set; }

        public byte Selected { get; internal set; }
        public bool Invalid { get; internal set; }

        [HideInInspector] public bool markedForDeletion;
        public readonly WalkData[] walkData = new WalkData[2]; // TODO: look into using pointers
        public int2 gridPosition;

        public RoomConnections connections;
        public RoomType roomType;

        public void OnDrawGizmos()
        {
            // TODO: remove drawing gizmos

            Color usingColor;
            if (Contiguous)
            {
                usingColor = roomType.name switch
                {
                    "RoomType_Spawn" => Color.green,
                    "RoomType_Boss" => Color.gray,
                    _ => Color.cyan
                };

                usingColor.a = 0.8f;
            }
            else
            {
                usingColor = Color.blue;
                usingColor.a = 0.7f;
            }

            switch (Selected)
            {
                case 1:
                    usingColor = Color.magenta;
                    usingColor.a = 0.9f;
                    break;
                case 2:
                    usingColor = Color.magenta / 2;
                    usingColor.a = 0.55f;
                    break;
                default:
                    usingColor = Color.red;
                    usingColor.a = 1;
                    break;
            }

            if (Invalid)
                usingColor = Color.red;

            Gizmos.color = usingColor;

            Gizmos.DrawCube(transform.position, Vector3.one * 0.75f);

            Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.6f);

            for (var i = 0; i < 4; i++)
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
}