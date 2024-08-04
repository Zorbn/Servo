using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Servo;

public class ItemDuctNetwork
{
    public static readonly List<ItemDuctNetwork> Networks = new();
    private static readonly Stopwatch Stopwatch = new();

    public readonly Color Color;
    private readonly List<Point> _nodes = new();

    public ItemDuctNetwork(Point node)
    {
        Color = new Color(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
        AddNode(node);

        Networks.Add(this);
    }

    public void RemoveNode(Point node)
    {
        _nodes.Remove(node);

        if (_nodes.Count == 0)
        {
            Networks.Remove(this);
        }
    }

    public void AddNode(Point node)
    {
        _nodes.Add(node);
    }

    public static void Tick(Map map, Map otherMap)
    {
        Stopwatch.Restart();

        foreach (var network in Networks)
        {
            foreach (var node in network._nodes)
            {
                for (var sideI = 0; sideI < 4; sideI++)
                {
                    ref var direction = ref Direction.Directions[sideI];
                    var sidePosition = new Point(node.X + direction.X, node.Y + direction.Y);

                    if (map.GetTile(sidePosition.X, sidePosition.Y) == Tile.Dirt)
                    {
                        map.SetTile(sidePosition.X, sidePosition.Y, Tile.Grass, otherMap);
                    }
                }
            }
        }

        Console.WriteLine($"Ticked ({Networks.Count}) networks in: {Stopwatch.Elapsed.TotalMilliseconds}ms");
    }
}