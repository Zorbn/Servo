namespace Servo;

public class Map
{
    public const int MapSize = 64;
    private const int MapLength = MapSize * MapSize;

    private readonly Tile[] _tiles = new Tile[MapLength];
    private readonly ITileEntity?[] _tileEntities = new ITileEntity?[MapLength];

    public void SetTile(int x, int y, Tile tile)
    {
        if (x < 0 || x >= MapSize || y < 0 || y >= MapSize)
        {
            return;
        }

        var tileI = x + y * MapSize;
        _tileEntities[tileI]?.OnPreBreak(this);
        _tiles[tileI] = tile;
        _tileEntities[tileI]?.OnBreak(this);
        _tileEntities[tileI] = TileData.Get(tile).TileEntitySpawner?.Invoke();
        _tileEntities[tileI]?.OnPlace(this, x, y);
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= MapSize || y < 0 || y >= MapSize)
        {
            return Tile.Air;
        }

        return _tiles[x + y * MapSize];
    }

    public ITileEntity? GetTileEntity(int x, int y)
    {
        if (x < 0 || x >= MapSize || y < 0 || y >= MapSize)
        {
            return null;
        }

        return _tileEntities[x + y * MapSize];
    }
}