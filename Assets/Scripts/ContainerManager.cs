
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class ContainerManager : MonoBehaviour
{
   

    // Dungeon generation parameters
    [SerializeField] public int minLeafSize;
    [SerializeField] public int maxLeafSize;
    [SerializeField] private int padding; // Minimum distance from the edges of the container
    [SerializeField] private bool showContainers;

    // Internal variables
    private List<Container> _containers;
    private Container _root;
    private Transform _dungeonHolder;
    private Transform _hallHolder;

    // Prefabs for rooms and halls
    [SerializeField] private GameObject containerPrefab;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject hallPrefab;

    // Start is called before the first frame update
    void Start()
    {
        _containers = new List<Container>();
        Generate();
    }

    public void Generate()
    {
        if (Camera.main != null)
        {
            var main = Camera.main;
            float cameraHeight = 2f * main.orthographicSize * 0.9f;
            float cameraWidth = cameraHeight * main.aspect;
            _root = new Container((int)(-cameraWidth / 2), (int)(-cameraHeight / 2), (int)cameraWidth,
                (int)cameraHeight,
                minLeafSize);
        }

        StartSplitting();
    }

    private void StartSplitting()
    {
        if (maxLeafSize == 0)
            return;

        _containers.Clear();
        _containers.Add(this._root);

        if (_dungeonHolder != null)
            DestroyImmediate(_dungeonHolder.gameObject);

        _dungeonHolder = new GameObject("Generated Dungeon").transform;
        _hallHolder = new GameObject("Halls").transform;
        _hallHolder.transform.parent = _dungeonHolder;

        bool didSplit = true;
        while (didSplit)
        {
            didSplit = false;
            int i = 0;
            while (i < _containers.Count)
            {
                Container container = _containers[i];
                if (container.LChild == null && container.RChild == null)
                {
                    if (container.Width > maxLeafSize || container.Height > maxLeafSize || Random.value > 0.25f)
                    {
                        if (container.Split())
                        {
                            _containers.Remove(container);
                            _containers.Add(container.LChild);
                            _containers.Add(container.RChild);
                            didSplit = true;
                        }
                    }
                }

                i++;
            }
        }

        if (showContainers)
            ShowContainers();

        CreateRooms();
        ConnectAllRooms();
    }

    private void ShowContainers()
    {
        Transform containerHolder = new GameObject("Containers").transform;
        containerHolder.transform.parent = _dungeonHolder;

        foreach (Container leaf in _containers)
        {
            GameObject container = Instantiate(containerPrefab,
                new Vector3(leaf.X + leaf.Width / 2f, leaf.Y + leaf.Height / 2f, 3),
                Quaternion.identity);
            container.transform.localScale = new Vector3(leaf.Width, leaf.Height, 1);
            container.transform.parent = containerHolder;
        }
    }

    private void CreateRooms()
    {
        

        foreach (Container container in _containers)
        {
            // Constrain room creation within the container's boundaries with padding
            int x = Mathf.Max(container.X + padding, container.X + (int)Random.Range(padding, Mathf.Floor(container.Width / 3)));
            int y = Mathf.Max(container.Y + padding, container.Y + (int)Random.Range(padding, Mathf.Floor(container.Height / 3)));
            int width = Mathf.Min(container.Width - padding * 2, container.Width - (x - container.X) - padding);
            int height = Mathf.Min(container.Height - padding * 2, container.Height - (y - container.Y) - padding);

            width = Mathf.Max(1, width - Random.Range(0, container.Width / 3));
            height = Mathf.Max(1, height - Random.Range(0, container.Height / 3));

            container.room = new Container.Room(x, y, width, height);
        }

        ShowRooms();
    }


    private void ShowRooms()
    {
        Transform roomHolder = new GameObject("Rooms").transform;
        roomHolder.transform.parent = _dungeonHolder;
        foreach (Container container in _containers)
        {
            GameObject room = Instantiate(roomPrefab,
                new Vector3(container.room.X + container.room.Width / 2f, container.room.Y + container.room.Height / 2f,
                    1),
                Quaternion.identity);
            room.transform.localScale = new Vector3(container.room.Width, container.room.Height, 1);
            room.transform.parent = roomHolder;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showContainers) return;

        foreach (var container in _containers)
        {
            DrawContainer(container);
        }
    }

    void DrawContainer(Container container)
    {
        int centerX = container.X + container.Width / 2;
        int centerY = container.Y + container.Height / 2;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(centerX, centerY, 0.01f),
            new Vector3(container.Width, container.Height, 0.01f));
    }
    
    void ConnectAllRooms()
    {
        // Step 1: Create a graph representation
        foreach (var room in _containers.Select(c => c.room))
        {
            // Find adjacent rooms and connect them
            foreach (var otherRoom in _containers.Select(c => c.room))
            {
                if (AreRoomsAdjacent(room, otherRoom))
                {
                    ConnectRooms(room,otherRoom);
                    room.ConnectedRooms.Add(otherRoom);
                    otherRoom.ConnectedRooms.Add(room);
                }
            }
        }

        // Step 2 & 3: Connect all disconnected subgraphs
        var allRooms = new HashSet<Container.Room>(_containers.Select(c => c.room));
        while (allRooms.Any())
        {
            var subgraph = new HashSet<Container.Room>();
            var queue = new Queue<Container.Room>();
            var startRoom = allRooms.First();
            queue.Enqueue(startRoom);
            while (queue.Any())
            {
                var currentRoom = queue.Dequeue();
                subgraph.Add(currentRoom);
                allRooms.Remove(currentRoom);

                foreach (var connectedRoom in currentRoom.ConnectedRooms)
                {
                    if (!subgraph.Contains(connectedRoom))
                    {
                        queue.Enqueue(connectedRoom);
                    }
                }
            }

            // If there are still rooms left, it means we have disconnected subgraphs
            if (allRooms.Any())
            {
                var closestRoom = FindClosestRoomOutsideSubgraph(subgraph, allRooms);
                ConnectRooms(startRoom, closestRoom);
            }
        }
        
        
    }

    bool AreRoomsAdjacent(Container.Room a, Container.Room b)
    {
        float adjacencyThreshold = minLeafSize;

        
        Vector2 centerA = new Vector2(a.X + a.Width / 2, a.Y + a.Height / 2);
        Vector2 centerB = new Vector2(b.X + b.Width / 2, b.Y + b.Height / 2);

        // Distance between the two centers
        float distance = Vector2.Distance(centerA, centerB);

        
        return distance < adjacencyThreshold;
    }

    Container.Room FindClosestRoomOutsideSubgraph(HashSet<Container.Room> subgraph, HashSet<Container.Room> allRooms)
    {
        Container.Room closestRoom = default;
        float closestDistance = float.MaxValue;

        foreach (var roomInSubgraph in subgraph)
        {
            Vector2 centerInSubgraph = new Vector2(roomInSubgraph.X + roomInSubgraph.Width / 2, roomInSubgraph.Y + roomInSubgraph.Height / 2);

            foreach (var roomOutsideSubgraph in allRooms)
            {
                if (!subgraph.Contains(roomOutsideSubgraph) && !roomInSubgraph.ConnectedRooms.Contains(roomOutsideSubgraph))
                {
                    Vector2 centerOutsideSubgraph = new Vector2(roomOutsideSubgraph.X + roomOutsideSubgraph.Width / 2, roomOutsideSubgraph.Y + roomOutsideSubgraph.Height / 2);
                    float distance = Vector2.Distance(centerInSubgraph, centerOutsideSubgraph);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestRoom = roomOutsideSubgraph;
                    }
                }
            }
        }

        return closestRoom;
    }
    
    // Connect these two rooms together with hallways.
    private void ConnectRooms(Container.Room room1, Container.Room room2)
    {
        Point point1 = new Point(
            Random.Range(room1.X + 1, room1.X + room1.Width - 1),
            Random.Range(room1.Y + 1, room1.Y + room1.Height - 1)
        );

        Point point2 = new Point(
            Random.Range(room2.X + 1, room2.X + room2.Width - 1),
            Random.Range(room2.Y + 1, room2.Y + room2.Height - 1)
        );

        int width = point2.X - point1.X;
        int height = point2.Y - point1.Y;

        if (width < 0)
        {
            if (height < 0)
            {
                if (Random.value < 0.5f)
                {
                    CreateHall(point2.X, point1.Y, Mathf.Abs(width), 1);
                    CreateHall(point2.X, point2.Y, 1, Mathf.Abs(height));
                }
                else
                {
                    CreateHall(point2.X, point2.Y, Mathf.Abs(width), 1);
                    CreateHall(point1.X, point2.Y, 1, Mathf.Abs(height));
                }
            }
            else if (height > 0)
            {
                if (Random.value < 0.5f)
                {
                    CreateHall(point2.X, point1.Y, Mathf.Abs(width), 1);
                    CreateHall(point2.X, point1.Y, 1, Mathf.Abs(height));
                }
                else
                {
                    CreateHall(point2.X, point2.Y, Mathf.Abs(width), 1);
                    CreateHall(point1.X, point1.Y, 1, Mathf.Abs(height));
                }
            }
            else
            {
                CreateHall(point2.X, point2.Y, Mathf.Abs(width), 1);
            }
        }
        else if (width > 0)
        {
            if (height < 0)
            {
                if (Random.value < 0.5f)
                {
                    CreateHall(point1.X, point2.Y, Mathf.Abs(width), 1);
                    CreateHall(point1.X, point2.Y, 1, Mathf.Abs(height));
                }
                else
                {
                    CreateHall(point1.X, point1.Y, Mathf.Abs(width), 1);
                    CreateHall(point2.X, point2.Y, 1, Mathf.Abs(height));
                }
            }
            else if (height > 0)
            {
                if (Random.value < 0.5f)
                {
                    CreateHall(point1.X, point1.Y, Mathf.Abs(width), 1);
                    CreateHall(point2.X, point1.Y, 1, Mathf.Abs(height));
                }
                else
                {
                    CreateHall(point1.X, point2.Y, Mathf.Abs(width), 1);
                    CreateHall(point1.X, point1.Y, 1, Mathf.Abs(height));
                }
            }
            else
            {
                CreateHall(point1.X, point1.Y, Mathf.Abs(width), 1);
            }
        }
        else
        {
            if (height < 0)
            {
                CreateHall(point2.X, point2.Y, 1, Mathf.Abs(height));
            }
            else if (height > 0)
            {
                CreateHall(point1.X, point1.Y, 1, Mathf.Abs(height));
            }
        }
    }
    
    private void CreateHall(int x, int y, int width, int height)
    {
        // Ensure width and height are positive
        width = Mathf.Abs(width);
        height = Mathf.Abs(height);

        GameObject hall = Instantiate(hallPrefab, new Vector3(x + width / 2f, y + height / 2f, 2),
            Quaternion.identity);
        hall.transform.localScale = new Vector3(width, height, 1);
        hall.transform.parent = _hallHolder;
    }
}