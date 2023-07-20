using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Servo;

// TODO: Add special case for when this ItemDuct has only one neighboring ItemDuct to save resources.
public class ItemDuctEntity : ITileEntity
{
    private static readonly Stack<Point> NodesToUpdate = new();

    public Color Color => _network?.Color ?? Color.White;

    private ItemDuctNetwork? _network;
    private int _x;
    private int _y;

    public void OnPlace(Map map, int x, int y)
    {
        _x = x;
        _y = y;

        for (var sideI = 0; sideI < 4; sideI++)
        {
            ref var direction = ref Direction.Directions[sideI];
            var sidePosition = new Point(x + direction.X, y + direction.Y);
            DestroyNetworkAt(map, sidePosition.X, sidePosition.Y);
        }

        CreateNetworkAt(map, x, y);
    }

    public void OnPreBreak(Map map)
    {
        DestroyNetworkAt(map, _x, _y);
    }

    public void OnBreak(Map map)
    {
        for (var sideI = 0; sideI < 4; sideI++)
        {
            ref var direction = ref Direction.Directions[sideI];
            var sidePosition = new Point(_x + direction.X, _y + direction.Y);
            CreateNetworkAt(map, sidePosition.X, sidePosition.Y);
        }
    }

    private static void CreateNetworkAt(Map map, int startX, int startY)
    {
        if (map.GetTile(startX, startY) != Tile.ItemDuct)
        {
            return;
        }

        ItemDuctNetwork? network = null;

        NodesToUpdate.Push(new Point(startX, startY));

        while (NodesToUpdate.TryPop(out var node))
        {
            var tileEntity = (ItemDuctEntity?)map.GetTileEntity(node.X, node.Y);

            if (tileEntity is null || tileEntity._network is not null)
            {
                continue;
            }

            if (network is null)
            {
                network = new ItemDuctNetwork(node);
                tileEntity._network = network;
            }
            else
            {
                tileEntity._network = network;
                tileEntity._network.AddNode(node);
            }

            for (var sideI = 0; sideI < 4; sideI++)
            {
                ref var direction = ref Direction.Directions[sideI];
                var sidePosition = new Point(node.X + direction.X, node.Y + direction.Y);

                if (map.GetTile(sidePosition.X, sidePosition.Y) == Tile.ItemDuct)
                {
                    NodesToUpdate.Push(sidePosition);
                }
            }
        }
    }

    private static void DestroyNetworkAt(Map map, int startX, int startY)
    {
        if (map.GetTile(startX, startY) != Tile.ItemDuct)
        {
            return;
        }

        NodesToUpdate.Push(new Point(startX, startY));

        while (NodesToUpdate.TryPop(out var node))
        {
            var tileEntity = (ItemDuctEntity?)map.GetTileEntity(node.X, node.Y);

            if (tileEntity is null || tileEntity._network is null)
            {
                continue;
            }

            tileEntity._network.RemoveNode(node);
            tileEntity._network = null;

            for (var sideI = 0; sideI < 4; sideI++)
            {
                ref var direction = ref Direction.Directions[sideI];
                var sidePosition = new Point(node.X + direction.X, node.Y + direction.Y);

                if (map.GetTile(sidePosition.X, sidePosition.Y) == Tile.ItemDuct)
                {
                    NodesToUpdate.Push(sidePosition);
                }
            }
        }
    }
}