public class MapSettings
{
    private string m_worldName;

    public MapSettings()
    {
        
    }

    public int Width { get; set; } = 200;
    public int Height { get; set; } = 200;

    public string WorldName { get => m_worldName; set => m_worldName = value; }
}