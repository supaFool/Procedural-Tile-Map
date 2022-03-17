using UnityEngine;

public class MapSettings
{
    private string m_worldName;


    public static int Width { get; set; } = 200;
    public static int Height { get; set; } = 200;

    public string WorldName { get => m_worldName; set => m_worldName = value; }
}