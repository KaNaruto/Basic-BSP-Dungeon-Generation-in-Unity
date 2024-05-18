using System.Collections.Generic;
using Random = UnityEngine.Random;

public class Container
{
    private readonly int _minLeafSize;

    // Container position
    public readonly int X;
    public readonly int Y;
    // Container size
    public readonly int Width;
    public readonly int Height;

    public Container LChild;
    public Container RChild;
    
    public Room room;
    public Container(int x, int y, int width, int height,int minLeafSize)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        _minLeafSize = minLeafSize;
    }

    public bool Split()
    {
        if (_minLeafSize == 0)
            return false;
        if (LChild != null || RChild != null)
            return false; // Already split

        /* Split direction */
        // Choose random direction
        bool splitHorizontal = Random.Range(0, 2) == 0;

        // if the width is >25% larger than height, we split vertically
        if (Width > Height && Width / Height >= 1.25f)
            splitHorizontal = false;
        else if (Height > Width && Height / Width >= 1.25f)
            splitHorizontal = true;

        int maxSize = (splitHorizontal ? Height : Width) - _minLeafSize;

        if (maxSize < _minLeafSize)
            return false; // Too small

        int splitSize = Random.Range(_minLeafSize, maxSize); // Random size between min and max size it can get
        if (splitHorizontal)
        {
            LChild = new Container(X, Y, Width, splitSize, _minLeafSize);
            RChild = new Container(X, Y + splitSize, Width, Height - splitSize, _minLeafSize);
        }
        else
        {
            LChild = new Container(X, Y, splitSize, Height, _minLeafSize);
            RChild = new Container(X + splitSize, Y, Width - splitSize, Height, _minLeafSize);
        }

        return true;
    }

    public struct Room
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public readonly List<Room> ConnectedRooms;

        public Room(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ConnectedRooms = new List<Room>();
        }

        
    }
}