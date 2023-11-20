using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField]
    public Camera _mainCamera;
    [SerializeField]
    public float _speed;
    [SerializeField]
    private GameObject _grid;
    private bool isMoving = false;
    public Vector3 start;
    public Vector3 end;
    private List<Spot> openList;
    private List<Spot> closeList;
    public Spot[][] grid;
    public bool pathReady = false;
    private List<Vector3> path;
    private bool isSecond = false;
    private Terrain gridTerrain;

    void Start()
    {
        path = new List<Vector3>();
        openList = new List<Spot>();
        closeList = new List<Spot>();

        gridTerrain = _grid.GetComponent<Terrain>();
        grid = new Spot[(int) gridTerrain.terrainData.size.x][];

        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = new Spot[(int)gridTerrain.terrainData.size.z];

            for (int j = 0; j < grid[i].Length; j++)
            {
                int x = i + (int)_grid.transform.position.x;
                int z = j + (int)_grid.transform.position.z;
                grid[i][j] = new Spot(x, z, grid.Length, grid[i].Length, (int)_grid.transform.position.x, (int)_grid.transform.position.z);
            }
        }

        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                grid[i][j].AddNeighbors(grid);
            }
        }
    }

    void Update()
    {
        start = transform.position;

        if (Input.GetMouseButton(1) && !isMoving)
        {
            isMoving = true;
            pathReady = false;

            Ray position = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(position, out var hit))
            {
                if (hit.collider == null || hit.collider != _grid.GetComponent<TerrainCollider>())
                {
                    Debug.LogError("Not Terrain");
                    isMoving = false;

                    return;
                } else
                {
                    end = new Vector3(hit.point.x, 0.5f, hit.point.z);
                }
            } else
            {
                Debug.LogError("Not Terrain");
                isMoving = false;

                return;
            }

            Spot playerPosition = null;
            path.Clear();
            openList.Clear();
            closeList.Clear();

            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid[i].Length; j++)
                {
                    grid[i][j].Heuristic(new Vector2(end.x, end.z));

                    if (grid[i][j].position.x == (int)transform.position.x && grid[i][j].position.y == (int)transform.position.z)
                    {
                        playerPosition = grid[i][j];
                    }
                }
            }

            if (playerPosition == null)
            {
                Debug.LogWarning(playerPosition);
                isMoving = false;
                return;
            }

            openList.Add(playerPosition);

            while (openList.Count > 0)
            {
                if (openList.Count == 0)
                {
                    Debug.Log("No solution");
                    isMoving = false;

                    break;
                }

                int lowestIndex = 0;

                for (int i = 0; i < openList.Count; i++) 
                {
                    if (openList[i].f < openList[lowestIndex].f)
                    {
                        lowestIndex = i;
                    }
                }

                Spot current = openList[lowestIndex];

                if (current != null && current.position.x == (int) end.x && current.position.y == (int) end.z)
                {
                    Debug.Log("Done");

                    if (current.previous == null)
                    {
                        Debug.Log("Already here");

                        isMoving = false;
                        
                        return;
                    }

                    while (current.previous != null)
                    {
                        Spot previousNode = current.previous;
                        path.Add(new Vector3(current.position.x, 0.5f, current.position.y));

                        current.previous = null;
                        current = previousNode;
                    }

                    pathReady = path.Count > 0;

                    break; 
                }

                openList.Remove(current);
                closeList.Add(current);

                for (int i = 0; i < current.neighbors.Count; i++)
                {
                    if (closeList.Contains(current.neighbors[i])) { continue; }

                    Spot neighbor = current.neighbors[i];
                    float tempG = current.g + 1;

                    if (openList.Contains(neighbor))
                    {
                        if (tempG < neighbor.g)
                        {
                            neighbor.g = tempG;
                            neighbor.f = neighbor.g + neighbor.h;
                            neighbor.previous = current;
                        }
                    }
                    else
                    {
                        neighbor.g = tempG;
                        neighbor.f = neighbor.g + neighbor.h;
                        neighbor.previous = current;
                        openList.Add(neighbor);
                    }
                }
            }
        }

        if (isMoving && pathReady)
        {
            Vector3 step = path[path.Count - 1];
            transform.position = Vector3.MoveTowards(transform.position, step, _speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, step) < 0.01f)
            {
                transform.position = step;
                path.RemoveAt(path.Count - 1);
            }

            if (path.Count == 0)
            {
                isMoving = false;
                pathReady = false;
                isSecond = true;

                Debug.Log("Finished Moving");
            }
        }
    }
}

public class Spot
{
    public Vector2 position;
    public float f, g, h;
    public List<Spot> neighbors;
    public Spot previous;
    private int sizeX, sizeZ, posX, posZ;

    public Spot(int x, int z, int gridSizeX, int gridSizeZ, int gridPosX, int gridPosZ)
    {
        position = new Vector2(x, z);
        f = g = h = 0.0f;
        sizeX = gridSizeX;
        sizeZ = gridSizeZ;
        posX = gridPosX;
        posZ = gridPosZ;
        neighbors = new List<Spot>();
    }

    public void AddNeighbors(Spot[][] grid)
    {
        int x = (int)position.x - posX;
        int z = (int)position.y - posZ;

        if (z > 0)
        {
            neighbors.Add(grid[x][z - 1]);
        }

        if (x < sizeX - 1)
        {
            neighbors.Add(grid[x + 1][z]);
        }

        if (z < sizeZ - 1)
        {
            neighbors.Add(grid[x][z + 1]);
        }

        if (x > 0)
        {
            neighbors.Add(grid[x - 1][z]);
        }

        if (z > 0 && x < sizeX - 1)
        {
            neighbors.Add(grid[x + 1][z - 1]);
        }

        if (x < sizeX - 1 && z < sizeZ - 1)
        {
            neighbors.Add(grid[x + 1][z + 1]);
        }        

        if (z < sizeZ - 1 && x > 0)
        {
            neighbors.Add(grid[x - 1][z + 1]);
        }

        if (x > 0 && z > 0)
        {
            neighbors.Add(grid[x - 1][z - 1]);
        }
    }

    public void Instantiate(int[] color, bool isPath = false) {}

    public void Heuristic(Vector2 goal) 
    {
        h = Vector2.Distance(position, goal);
    }
}
