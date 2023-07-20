namespace Servo;

public static class TileExtensions
{
    public static int GetTextureIndex(this Tile tile)
    {
        return (int)tile - 1;
    }
}