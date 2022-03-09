using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class MapBuilder : MonoBehaviour
{
    [SerializeField]
    private Grid MapGrid;

    private MapSettings mapSettings;

    //private bool update = false; // Updates existing map if true, Creates a new map if false
    private bool startRendering = false; // Starts the Map Gen loops

    #region Vars

    #region GridSpacing vars

    private float m_spacing = 0.0f;//Space between each tile

    #endregion GridSpacing vars

    [SerializeField]
    private Tilemap TopMap;

    [SerializeField]
    private Tilemap BottomMap;

    [SerializeField]
    private Tilemap TreeMap;

    [SerializeField]
    private Tile TopTile;

    [SerializeField]
    private Tile TreeTile;

    [SerializeField]
    private Tile BottomTile;

    [SerializeField]
    private Tile FiveTile;

    //Spawn Settings
    [Range(15, 55)]
    public int InitialLandHeight;

    [Range(1, 16)]
    public int LandBirthLimit;

    [Range(1, 16)]
    public int LandDeathLimit;

    [Range(1, 10)]
    public int LandMapSamples;

    [Range(1, 100)]
    public int InitialForestDensity;

    [Range(1, 16)]
    public int DroughtFactor;

    [Range(1, 16)]
    public int DroughtDeathLimit;

    [Range(1, 10)]
    public int DroughtSamples;

    private bool m_newMap;
    private int[,] m_terrainMap;
    private int[,] m_treeMap;

    public GameObject WorldCache;
    private WorldCacheComp m_worldCacheComp;

    #endregion Vars

    #region Overrides

    private void Awake()
    {
        mapSettings = new MapSettings();
        m_newMap = true;
        if(m_worldCacheComp = WorldCache.GetComponent(typeof(WorldCacheComp)) as WorldCacheComp)
        {
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (m_newMap && startRendering)
        {
            Simulate();
        }

        if (startRendering)
        {
            //m_iterations++;
            UpdateMap();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (startRendering == true)
            {
                startRendering = false;
            }
            else
            {
                startRendering = true;
            }
            if (m_newMap && startRendering)
            {
                Simulate();
                m_newMap = false;
            }
            //update = false;
            //Simulate();
        }

        if (Input.GetMouseButtonDown(1))
        {
            startRendering = false;
            m_newMap = true;
            ClearMap(true);
            //m_iterations = 0;
            //m_currentCellGap = 0;
            MapGrid.cellGap = new Vector3(0, 0, 0);
        }
    }

    private void UpdateMap()
    {
        //update = true;
        //m_currentCellGap += m_iterations;
        MapGrid.cellGap = new Vector3(m_spacing, m_spacing, 0);
        Debug.Log(TopMap.cellGap);

        var oldTerraMap = m_terrainMap;
        var oldForMap = m_treeMap;

        int[,] newMap = new int[oldTerraMap.GetLength(0), oldTerraMap.GetLength(1)];

        //Init New Map to all 5's
        for (int x = 0; x < newMap.GetLength(0); x++)
        {
            for (int y = 0; y < newMap.GetLength(1); y++)
            {
                newMap[x, y] = 5;
            }
        }

        //Add old map
        for (int x = 0; x < oldTerraMap.GetLength(0); x++)
        {
            for (int y = 0; y < oldTerraMap.GetLength(1); y++)
            {
                newMap[x, y] = oldTerraMap[x, y];
            }
        }
        SimulateTerrain(LandMapSamples);
        SimulateTrees(DroughtSamples);
    }

    #endregion Overrides

    #region Functions

    private void SimulateTerrain(int samples)
    {
        SampleMap(samples);

        RenderMap();
    }

    private void SampleMap(int samples)
    {
        for (int i = 0; i < samples; i++)
        {
            //Update Terrain map for the amount of samples
            m_terrainMap = BuildTerrain(m_terrainMap, false);
        }
    }

    private void RenderMap()
    {
        for (int x = 0; x < m_terrainMap.GetLength(0); x++)
        {
            for (int y = 0; y < m_terrainMap.GetLength(1); y++)
            {
                m_treeMap[x, y] = 0;

                if (m_terrainMap[x, y] == 1)
                {
                    TopMap.SetTile(new Vector3Int(-x + (m_terrainMap.GetLength(0)) / 2, -y + (m_terrainMap.GetLength(1)) / 2, 0), FiveTile);
                    var r = Random.Range(0, 100);
                    if (r <= 3)
                    {
                        m_treeMap[x, y] = 9;
                    }
                }

                if (m_terrainMap[x, y] == 0)
                {
                    TopMap.SetTile(new Vector3Int(-x + (m_terrainMap.GetLength(0)) / 2, -y + (m_terrainMap.GetLength(1)) / 2, 0), BottomTile);
                    m_treeMap[x, y] = 0;
                }
            }
        }
    }

    private void SimulateTrees(int samples)
    {
        for (int i = 0; i < samples; i++)
        {
            m_treeMap = BuildForest(m_treeMap);
        }
        for (int x = 0; x < mapSettings.Width; x++)
        {
            for (int y = 0; y < mapSettings.Height; y++)
            {
                if (m_treeMap[x, y] == 3)
                {
                    TreeMap.SetTile(new Vector3Int(-x + mapSettings.Width / 2, -y + mapSettings.Height / 2, 0), TopTile);
                }
            }
        }
    }

    private void Simulate()
    {
        Debug.Log("Building Terrain");
        ClearMap(false);
        InitMapGrid();
        SimulateTerrain(LandMapSamples);
        SimulateTrees(DroughtSamples);
    }

    private void InitMapGrid()
    {
        if (m_terrainMap == null)
        {
            m_terrainMap = new int[mapSettings.Width, mapSettings.Height];
            m_treeMap = new int[mapSettings.Width, mapSettings.Height];
            InitPos();
        }
    }

    private int[,] BuildTerrain(int[,] tempMap, bool updatemap)
    {
        int[,] updatedMap;
        int neighbor;
        BoundsInt bounds = new BoundsInt(-1, -1, 0, 3, 3, 1);

        #region UpdatedMap

        if (updatemap)
        {
            updatedMap = new int[tempMap.GetLength(0), tempMap.GetLength(1)];
            for (int x = 0; x < tempMap.GetLength(0); x++)
            {
                for (int y = 0; y < tempMap.GetLength(1); y++)
                {
                    if (tempMap[x, y] == 5)
                    {
                        tempMap[x, y] = Random.Range(1, 101) < InitialLandHeight ? 1 : 0;
                    }

                    neighbor = 0;
                    foreach (var b in bounds.allPositionsWithin)
                    {
                        if (b.x == 0 && b.y == 0) continue;
                        if (x + b.x >= 0 && x + b.x < tempMap.GetLength(0) && y + b.y >= 0 && y + b.y < tempMap.GetLength(1))
                        {
                            neighbor += tempMap[x + b.x, y + b.y];
                        }
                        else
                        {
                            //Draw Border
                            //neighbor++;
                        }
                    }
                    if (tempMap[x, y] == 1)
                    {
                        if (neighbor < LandDeathLimit) updatedMap[x, y] = 0;
                        else
                        {
                            updatedMap[x, y] = 1;
                        }
                    }
                    if (tempMap[x, y] == 0)
                    {
                        if (neighbor > LandBirthLimit) updatedMap[x, y] = 1;
                        else
                        {
                            updatedMap[x, y] = 0;
                        }
                    }
                }
            }
        }

        #endregion UpdatedMap

        #region Not Update

        else
        {
            updatedMap = new int[m_terrainMap.GetLength(0), m_terrainMap.GetLength(1)];
            for (int x = 0; x < mapSettings.Width; x++)
            {
                for (int y = 0; y < mapSettings.Height; y++)
                {
                    neighbor = 0;
                    foreach (var b in bounds.allPositionsWithin)
                    {
                        if (b.x == 0 && b.y == 0) continue;
                        if (x + b.x >= 0 && x + b.x < m_terrainMap.GetLength(0) && y + b.y >= 0 && y + b.y < m_terrainMap.GetLength(1))
                        {
                            neighbor += tempMap[x + b.x, y + b.y];
                        }
                        else
                        {
                            //Draw Border
                            //neighbor++;
                        }
                    }
                    if (tempMap[x, y] == 1)
                    {
                        if (neighbor < LandDeathLimit) updatedMap[x, y] = 0;
                        else
                        {
                            updatedMap[x, y] = 1;
                        }
                    }
                    if (tempMap[x, y] == 0)
                    {
                        if (neighbor > LandBirthLimit) updatedMap[x, y] = 1;
                        else
                        {
                            updatedMap[x, y] = 0;
                        }
                    }
                }
            }
        }

        #endregion Not Update

        return updatedMap;
    }

    private int[,] BuildForest(int[,] tempMap)
    {
        int[,] newMap = new int[mapSettings.Width, mapSettings.Height];
        int neighbor;
        BoundsInt bounds = new BoundsInt(-1, -1, 0, 3, 3, 1);

        #region Build Forest

        for (int x = 0; x < mapSettings.Width; x++)
        {
            for (int y = 0; y < mapSettings.Height; y++)
            {
                //Init Treemap
                if (m_treeMap[x, y] == 9)
                {
                    m_treeMap[x, y] = Random.Range(1, 101) < InitialForestDensity ? 3 : 4;
                }
                neighbor = 0;
                foreach (var b in bounds.allPositionsWithin)
                {
                    if (b.x == 0 && b.y == 0) continue;
                    if (x + b.x >= 0 && x + b.x < mapSettings.Width && y + b.y >= 0 && y + b.y < mapSettings.Height)
                    {
                        neighbor += tempMap[x + b.x, y + b.y];
                    }
                    else
                    {
                        //Draw Border
                        //neighbor++;
                    }
                }
                if (tempMap[x, y] == 3)
                {
                    if (neighbor < DroughtDeathLimit) newMap[x, y] = 4;
                    else
                    {
                        newMap[x, y] = 3;
                    }
                }

                if (tempMap[x, y] == 4)
                {
                    if (neighbor > LandBirthLimit) newMap[x, y] = 3;
                    else
                    {
                        newMap[x, y] = 4;
                    }
                }
            }
        }

        #endregion Build Forest

        return newMap;
    }

    private void InitPos()
    {
        Debug.Log("InitPos, Map Width = " + mapSettings.Width);
        for (int x = 0; x < mapSettings.Width; x++)
        {
            for (int y = 0; y < mapSettings.Height; y++)
            {
                m_terrainMap[x, y] = Random.Range(1, 101) < InitialLandHeight ? 1 : 0;
            }
        }
    }

    private void InitTreeMap()
    {
        for (int x = 0; x < mapSettings.Width; x++)
        {
            for (int y = 0; y < mapSettings.Height; y++)
            {
                //Init Treemap
                if (m_treeMap[x, y] == 9)
                {
                    m_treeMap[x, y] = Random.Range(1, 101) < InitialForestDensity ? 3 : 4;
                }
            }
        }
    }

    private void ClearMap(bool v)
    {
        TopMap.ClearAllTiles();
        BottomMap.ClearAllTiles();
        TreeMap.ClearAllTiles();

        if (v)
        {
            Debug.Log("Finished Clearing all");
            m_terrainMap = null;
        }
    }

    #endregion Functions
}