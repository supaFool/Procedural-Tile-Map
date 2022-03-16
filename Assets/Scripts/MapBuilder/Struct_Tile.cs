namespace Assets.Scripts.MapBuilder
{
    public struct Struct_Tile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public TileType Type { get; set; }

        public static Struct_Tile CreateTile(int x, int y, TileType type) => new Struct_Tile()
        {
            X = x,
            Y = y,
            Type = type
        };
    }
}
