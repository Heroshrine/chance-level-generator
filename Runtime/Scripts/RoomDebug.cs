using UnityEngine;

namespace ChanceGen
{
    public class RoomDebug : MonoBehaviour
    {
        public RoomInfo roomInfo;

        public void OnDrawGizmos()
        {
            // TODO: remove drawing gizmos

            Color usingColor;
            if (roomInfo.Contiguous)
            {
                usingColor = roomInfo.roomType.name switch
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

            if (roomInfo.Invalid)
                usingColor = Color.red;

            Gizmos.color = usingColor;

            Gizmos.DrawCube(transform.position, Vector3.one * 0.75f);

            Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.6f);

            for (var i = 0; i < 4; i++)
            {
                if (((int)roomInfo.connections & (1 << i)) != 0)
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