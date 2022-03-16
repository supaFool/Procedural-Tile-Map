using Assets.Scripts.MapBuilder;
using UnityEngine;

public class Map : MonoBehaviour
{

    public Struct_Tile[,] Tiles { get; private set; }

    private void Awake()
    {
        Tiles = new Struct_Tile[MapSettings.Width, MapSettings.Height];
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
