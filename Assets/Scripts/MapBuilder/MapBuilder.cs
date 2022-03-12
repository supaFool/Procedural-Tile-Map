using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class MapBuilder : MonoBehaviour
{
    [Header("Grid Settings")]
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
    private Tilemap FirstPassMap;

    [SerializeField]
    private Tilemap SecondPassMap;

    [SerializeField]
    private Tilemap TreeMap;

    [SerializeField]
    private Tile SecondPassTile;

    [SerializeField]
    private Tile BaseTile;

    [SerializeField]
    private Tile FirstPassTile;

    [Header("Build Settings")]
    [Space(10)]
    [SerializeField]
    private bool m_simFirstPassMap;

    [SerializeField]
    private bool m_simSecondPassMap;


    //Temps

    //Rivers
    //28,3,2,4




    //Spawn Settings

    /// <summary>
    /// The starting land heigth
    /// The higher the number, the more islands will be spawned for the algorithm to run on
    /// </summary>
    [Range(15, 55)]
    public int InitialLandHeight = 26;
    //public Slider LandHeightSlider;

    //[Range(1, 30)]
    private int LandMapIterations = 6;
    public Slider LandIterationsSlider;
    public TMPro.TextMeshProUGUI LandMapIterationsText;

    private int m_actualIterations;

    /// <summary>
    /// Higher the number, Higher the cells per tick that have a chance to reproduce
    /// </summary>
    //[Range(1, 16)]
    private int LandBirthLimit = 3;
    public Slider LandBirthLimitSlider;
    public TMPro.TextMeshProUGUI LandBirthText;

    /// <summary>
    /// The higher the number, the higher the probability of reproduction failure
    /// </summary>
    //[Range(1, 16)]
    private int LandDeathLimit = 2;
    public Slider LandDeathLimitSlider;
    public TMPro.TextMeshProUGUI LandDeathText; 

    /// <summary>
    /// How many times to sample the map per tick, Higher numbers creates smoother bordered maps
    /// </summary>
    //[Range(1, 10)]
    private int LandMapSamples = 6;
    public Slider LandSamplesSlider;
    public TMPro.TextMeshProUGUI LandSampleText; 

    //[Range(1, 100)]
    private int InitialPopulationDensity = 2;
    public Slider ForestDensitySlider;
    public TMPro.TextMeshProUGUI PopulationDensityText; 

    //[Range(1, 16)]
    private int PopulationBirthFactor = 5;
    public Slider PopulationBirthFactorSlider;
    public TMPro.TextMeshProUGUI PopulationBirthText;

    //[Range(1, 16)]
    private int PopulationDeathFactor = 11;
    public Slider PopulationDeathSlider;
    public TMPro.TextMeshProUGUI PopulationDeathText;

    //[Range(1, 10)]
    private int PopulationSamples = 10;
    public Slider PopulationSampleSlider;
    public TMPro.TextMeshProUGUI PopulationSamplesText;

    public Button SimButton;

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
        SimButton.onClick.AddListener(SimButtonAction) ;
        SetSliders();
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
            m_actualIterations++;
            UpdateMap();
            if (m_actualIterations >= LandMapIterations)
            {
                m_simFirstPassMap = false;
            }
        }

        #region Controls
        //if (Input.GetMouseButtonDown(0))
        //{
        //    if (startRendering == true)
        //    {
        //        startRendering = false;
        //    }
        //    else
        //    {
        //        startRendering = true;
        //    }
        //    if (m_newMap && startRendering)
        //    {
        //        Simulate();
        //        m_newMap = false;
        //    }
        //    //update = false;
        //    //Simulate();
        //}

        if (Input.GetMouseButtonDown(1))
        {
            startRendering = false;
            m_newMap = true;
            ClearMap(true);
            m_actualIterations = 0;
            m_simFirstPassMap = true;
            //m_iterations = 0;
            //m_currentCellGap = 0;
            MapGrid.cellGap = new Vector3(0, 0, 0);
            SimButton.GetComponentInChildren<Text>().text = "Simulate";
        }
        #endregion
    }
    public void SimButtonAction()
    {
        startRendering = !startRendering;
        if (m_newMap && startRendering)
        {
            Simulate();
            m_newMap = false;
        }

        if (SimButton.GetComponentInChildren<Text>().text == "Simulate")
        {
            SimButton.GetComponentInChildren<Text>().text = "Stop";
        }
        else
            SimButton.GetComponentInChildren<Text>().text = "Simulate";
    }

    private void UpdateMap()
    {
        MapGrid.cellGap = new Vector3(m_spacing, m_spacing, 0);

        var oldTerraMap = m_terrainMap;

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
        if (m_simFirstPassMap)
        {
            SimulateTerrain(LandMapSamples);
        }
        if (m_simSecondPassMap)
        {
            SimulateTrees(PopulationSamples);
        }
        RenderMap();

    }

    #endregion Overrides

    #region Functions
    private void SetSliders()
    {
        //LandHeightSlider.onValueChanged.AddListener(delegate { SetStartingTerrainHeight(((int)LandHeightSlider.value)); });
        LandIterationsSlider.onValueChanged.AddListener(delegate { SetLandIteration(((int)LandIterationsSlider.value)); });
        LandBirthLimitSlider.onValueChanged.AddListener(delegate { SetLandBirthLimiter(((int)LandBirthLimitSlider.value)); });
        LandDeathLimitSlider.onValueChanged.AddListener(delegate { SetLandDeathLimit(((int)LandDeathLimitSlider.value)); });
        LandSamplesSlider.onValueChanged.AddListener(delegate { SetLandSamples(((int)LandSamplesSlider.value)); });
        ForestDensitySlider.onValueChanged.AddListener(delegate { SetInitialPopDensity(((int)ForestDensitySlider.value)); });
        PopulationBirthFactorSlider.onValueChanged.AddListener(delegate { SetPopulationBirthFactor(((int)PopulationBirthFactorSlider.value)); });
        PopulationDeathSlider.onValueChanged.AddListener(delegate { SetPopulationDeathFactor(((int)PopulationDeathSlider.value)); });
        PopulationSampleSlider.onValueChanged.AddListener(delegate { SetPopulationSamples(((int)PopulationSampleSlider.value)); });
        InitSliders();
    }
    private void InitSliders()
    {
        LandMapIterationsText.text = LandMapIterations.ToString();
        LandBirthText.text = LandBirthLimit.ToString();
        LandDeathText.text = LandDeathLimit.ToString();
        LandSampleText.text = LandMapSamples.ToString();
        PopulationDensityText.text = InitialPopulationDensity.ToString();
        PopulationBirthText.text = PopulationBirthFactor.ToString();
        PopulationDeathText.text = PopulationDeathFactor.ToString();
        PopulationSamplesText.text = PopulationSamples.ToString();
    }
    public void SetInitialPopDensity(int h)
    {
        InitialPopulationDensity = h;
        PopulationDensityText.text = InitialPopulationDensity.ToString();
        Debug.Log($"Forest Density set to: {h}");
    }

    public void SetPopulationBirthFactor(int h)
    {
        PopulationBirthFactor = h;
        PopulationBirthText.text = PopulationBirthFactor.ToString();
        Debug.Log($"Drought Factor set to: {h}");
    }

    public void SetPopulationSamples(int h)
    {
        PopulationSamples = h;
        PopulationSamplesText.text = PopulationSamples.ToString();
        Debug.Log($"Drought Samples set to: {h}");
    }

    public void SetPopulationDeathFactor(int h)
    {
        PopulationDeathFactor = h;
        PopulationDeathText.text = PopulationDeathFactor.ToString();
        Debug.Log($"Drought Death Limit set to: {h}");
    }

    public void SetStartingTerrainHeight(int h)
    {
        InitialLandHeight = h;
        Debug.Log($"Land Height set to: {h}");
    }


    public void SetLandSamples(int h)
    {
        LandMapSamples = h;
        LandSampleText.text = $"{h}";
        Debug.Log($"Land Samples set to: {h}");
    }

    public void SetLandDeathLimit(int h)
    {
        LandDeathLimit = h;
        LandDeathText.text = $"{h}";
        Debug.Log($"Land Death Limit set to: {h}");
    }

    public void SetLandBirthLimiter(int h)
    {
        LandBirthLimit = h;
        LandBirthText.text = $"{h}";
        Debug.Log($"Land Birth Limit set to: {h}");

    }

    public void SetLandIteration(int h)
    {
        LandMapIterations = h;
        LandMapIterationsText.text = $"{h}";
        Debug.Log($"Land Iterations set to: {h}");
    }

    private void SimulateTerrain(int samples)
    {
        SampleMap(samples);
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
                    FirstPassMap.SetTile(new Vector3Int(-x + (m_terrainMap.GetLength(0)) / 2, -y + (m_terrainMap.GetLength(1)) / 2, 0), FirstPassTile);

                    var r = Random.Range(0, 100);
                    if (r <= 3)
                    {
                        m_treeMap[x, y] = 9;
                    }
                }

                if (m_terrainMap[x, y] == 0)
                {
                    FirstPassMap.SetTile(new Vector3Int(-x + (m_terrainMap.GetLength(0)) / 2, -y + (m_terrainMap.GetLength(1)) / 2, 0), BaseTile);

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
                    TreeMap.SetTile(new Vector3Int(-x + mapSettings.Width / 2, -y + mapSettings.Height / 2, 0), SecondPassTile);
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
        SimulateTrees(PopulationSamples);
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
                    m_treeMap[x, y] = Random.Range(1, 101) < InitialPopulationDensity ? 3 : 4;
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
                    if (neighbor < PopulationDeathFactor) newMap[x, y] = 4;
                    else
                    {
                        newMap[x, y] = 3;
                    }
                }

                if (tempMap[x, y] == 4)
                {
                    if (neighbor > PopulationBirthFactor) newMap[x, y] = 3;
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
                    m_treeMap[x, y] = Random.Range(1, 101) < InitialPopulationDensity ? 3 : 4;
                }
            }
        }
    }

    private void ClearMap(bool v)
    {
        FirstPassMap.ClearAllTiles();
        SecondPassMap.ClearAllTiles();
        TreeMap.ClearAllTiles();

        if (v)
        {
            Debug.Log("Finished Clearing all");
            m_terrainMap = null;
        }
    }

    #endregion Functions
}