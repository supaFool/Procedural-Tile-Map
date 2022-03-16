using UnityEngine;

public class MapSettings : MonoBehaviour
{
    private string m_worldName;

    public MapSettings()
    {

    }

    public static int Width { get; set; } = 200;
    public static int Height { get; set; } = 200;

    public string WorldName { get => m_worldName; set => m_worldName = value; }
}