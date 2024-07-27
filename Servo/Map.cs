using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Servo;

public class Map
{
    public const int Size = 64;
    private const int Length = Size * Size;

    private readonly Tile[] _tiles = new Tile[Length];
    private readonly ITileEntity?[] _tileEntities = new ITileEntity?[Length];
    private readonly byte[] _lightmap = new byte[Length];

    public readonly Texture2D LightmapTexture;
    private readonly Color[] _lightmapPixels = new Color[Size * Size];
    private readonly Stack<LightingUpdate> _lightingUpdates = new();
    private const byte SunlightMask = 0xf0;
    private const byte SunlightOffset = 4;
    private const byte LightMask = 0x0f;
    private const byte LightOffset = 0;
    private const byte MaxLightLevel = 15;
    private const byte MaxVisibleLightLevel = 10;

    public Map(GraphicsDevice graphicsDevice)
    {
        LightmapTexture = new Texture2D(graphicsDevice, Size, Size);
        Array.Fill(_lightmapPixels, new Color(1.0f, 1.0f, 1.0f, 1.0f));
        LightmapTexture.SetData(_lightmapPixels);
        Array.Fill(_lightmap, (byte)0xff);
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return;
        }

        var tileI = x + y * Size;
        _tileEntities[tileI]?.OnPreBreak(this);
        _tiles[tileI] = Tile.Air;
        _tileEntities[tileI]?.OnBreak(this);

        _tiles[tileI] = tile;
        _tileEntities[tileI] = TileData.Get(tile).TileEntitySpawner?.Invoke();
        _tileEntities[tileI]?.OnPlace(this, x, y);

        _lightingUpdates.Push(new LightingUpdate
        {
            X = x,
            Y = y
        });
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return Tile.Air;
        }

        return _tiles[x + y * Size];
    }

    public ITileEntity? GetTileEntity(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return null;
        }

        return _tileEntities[x + y * Size];
    }

    public void SetLight(int x, int y, byte lightLevel, byte mask, byte offset)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return;
        }

        var i = x + y * Size;
        _lightmap[i] = (byte)((_lightmap[i] & ~mask) | (lightLevel << offset));
    }

    public byte GetLight(int x, int y, byte mask, byte offset)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return 0;
        }

        return (byte)((_lightmap[x + y * Size] & mask) >> offset);
    }

    public void UpdateLighting()
    {
        while (_lightingUpdates.TryPop(out var current))
        {
            var tile = GetTile(current.X, current.Y);

            var oldSunlight = GetLight(current.X, current.Y, SunlightMask, SunlightOffset);
            var oldLight = GetLight(current.X, current.Y, LightMask, LightOffset);
            byte newSunlight = 0;
            byte newLight = 0;

            if (tile == Tile.Air)
            {
                newSunlight = MaxLightLevel;
            }

            if (tile == Tile.Dropper)
            {
                newLight = MaxLightLevel;
            }

            var opacity = tile == Tile.Air ? 1 : 3;

            for (var sideI = 0; sideI < 4; sideI++)
            {
                var neighborX = current.X + Direction.Directions[sideI].X;
                var neighborY = current.Y + Direction.Directions[sideI].Y;

                if (neighborX is >= 0 and < Size && neighborY is >= 0 and < Size)
                {
                    var neighborSunlightLevel = GetLight(neighborX, neighborY, SunlightMask, SunlightOffset);
                    newSunlight = (byte)Math.Max(Math.Max(neighborSunlightLevel - opacity, 0), newSunlight);

                    var neighborLightLevel = GetLight(neighborX, neighborY, LightMask, LightOffset);
                    newLight = (byte)Math.Max(Math.Max(neighborLightLevel - opacity, 0), newLight);
                }
            }

            var isLightDifferent = oldLight != newLight || oldSunlight != newSunlight;
            if (isLightDifferent)
            {
                SetLight(current.X, current.Y, newSunlight, SunlightMask, SunlightOffset);
                SetLight(current.X, current.Y, newLight, LightMask, LightOffset);

                var lightLevel = Math.Min(Math.Max(newSunlight, newLight) / (float)MaxVisibleLightLevel, 1.0f);
                _lightmapPixels[current.X + current.Y * Size] = new Color(lightLevel, lightLevel, lightLevel, 1.0f);

                for (var sideI = 0; sideI < 4; sideI++)
                {
                    var neighborX = current.X + Direction.Directions[sideI].X;
                    var neighborY = current.Y + Direction.Directions[sideI].Y;

                    if (neighborX is >= 0 and < Size && neighborY is >= 0 and < Size)
                    {
                        _lightingUpdates.Push(new LightingUpdate
                        {
                            X = neighborX,
                            Y = neighborY
                        });
                    }
                }
            }
        }

        LightmapTexture.SetData(_lightmapPixels);
    }

    struct LightingUpdate
    {
        public int X;
        public int Y;
    }
}