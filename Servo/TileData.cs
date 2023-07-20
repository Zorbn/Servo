using System;

namespace Servo;

public enum Tile
{
    Air = 0,
    Dirt = 1,
    Grass = 2,
    ItemDuct = 3,
    Miner = 4,
    Dropper = 5
}

public class TileData
{
    public const int TileCount = 6;
    public static readonly TileData[] Registry = new TileData[TileCount];

    public readonly Func<ITileEntity>? TileEntitySpawner;

    public TileData(Func<ITileEntity>? tileEntitySpawner = null)
    {
        TileEntitySpawner = tileEntitySpawner;
    }

    public static TileData Get(Tile tile)
    {
        return Registry[(int)tile];
    }

    private static void Register(Tile tile, TileData tileData)
    {
        Registry[(int)tile] = tileData;
    }

    static TileData()
    {
        Register(Tile.Air, new TileData());
        Register(Tile.Dirt, new TileData());
        Register(Tile.Grass, new TileData());
        Register(Tile.ItemDuct, new TileData(() => new ItemDuctEntity()));
        Register(Tile.Miner, new TileData());
        Register(Tile.Dropper, new TileData());
    }
}