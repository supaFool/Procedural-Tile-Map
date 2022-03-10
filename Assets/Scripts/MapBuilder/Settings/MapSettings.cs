public class MapSettings
{
    private string m_worldName;

    private int mapWidth;

    private int mapHeight;

    public MapSettings()
    {
        mapWidth = 200;
        mapHeight = 200;
    }

    public int Width { get => mapWidth; }
    public int Height { get => mapHeight; }

    public string WorldName { get => m_worldName; set => m_worldName = value; }
}