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
    private readonly int[] _heightmap = new int[Size];

    public readonly Texture2D LightmapTexture;
    private readonly Color[] _lightmapPixels = new Color[Size * Size];
    private readonly Stack<LightingUpdate> _lightingUpdates = new();
    private const byte SunlightMask = 0xf0;
    private const byte SunlightOffset = 4;
    private const byte LightMask = 0x0f;
    private const byte LightOffset = 0;
    private const byte MaxLightLevel = 15;
    private const byte MaxVisibleLightLevel = 10;

    public VertexBuffer? LightmapVertexBuffer;
    public IndexBuffer? LightmapIndexBuffer;
    public int LightmapPrimitiveCount { get; private set; }

    private ArrayList<VertexPositionColor> _lightmapVertices = new();
    private ArrayList<ushort> _lightmapIndices = new();
    private float[] _lightBuffer = new float[4];

    public Map(GraphicsDevice graphicsDevice)
    {
        LightmapTexture = new Texture2D(graphicsDevice, Size, Size);
        Array.Fill(_lightmapPixels, new Color(1.0f, 1.0f, 1.0f, 1.0f));
        LightmapTexture.SetData(_lightmapPixels);
        Array.Fill(_lightmap, (byte)0xff);
        Array.Fill(_heightmap, Size);
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
        UpdateHeightmap(x, y, tile);
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

    private void UpdateHeightmap(int x, int y, Tile tile)
    {
        if (tile == Tile.Air)
        {
            int iy;
            for (iy = 0; iy < Size; iy++)
            {
                if (GetTile(x, iy) != Tile.Air)
                {
                    break;
                }
            }

            _heightmap[x] = iy;
        }
        else
        {
            if (y < _heightmap[x])
            {
                _heightmap[x] = y;
            }
        }
    }

    public void UpdateLighting()
    {
        while (_lightingUpdates.TryPop(out var current))
        {
            var maxHeight = _heightmap[current.X];
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

    // TODO:
    private static readonly ushort[] FaceIndices = { 0, 2, 1, 0, 3, 2 };

    private byte GetVisibleLight(int x, int y)
    {
        var sunlight = GetLight(x, y, SunlightMask, SunlightOffset);
        var light = GetLight(x, y, LightMask, LightOffset);

        return Math.Max(sunlight, light);
    }

    public void UpdateLightmapMesh(GraphicsDevice graphicsDevice)
    {
        _lightmapVertices.Clear();
        _lightmapIndices.Clear();

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                if (GetTile(x, y) == Tile.Air) continue;

                for (var i = 0; i < FaceIndices.Length; i++)
                    _lightmapIndices.Add((ushort)(_lightmapVertices.Count + FaceIndices[i]));

                for (var cornerI = 0; cornerI < 4; cornerI++)
                {
                    var vertex = new VertexPositionColor();
                    vertex.Color = Color.DarkRed;
                    var light = 0;

                    switch (cornerI)
                    {
                        case 0:
                            vertex.Position = new Vector3(x + 0, y + 0, 0) * Game1.TileSize;
                            light += GetVisibleLight(x, y - 1) + GetVisibleLight(x - 1, y) + GetVisibleLight(x - 1, y - 1);
                            vertex.Color = Color.Green;
                            break;
                        case 1:
                            vertex.Position = new Vector3(x + 0, y + 1, 0) * Game1.TileSize;
                            light += GetVisibleLight(x, y + 1) + GetVisibleLight(x - 1, y) + GetVisibleLight(x - 1, y + 1);
                            break;
                        case 2:
                            vertex.Position = new Vector3(x + 1, y + 1, 0) * Game1.TileSize;
                            light += GetVisibleLight(x, y + 1) + GetVisibleLight(x + 1, y) + GetVisibleLight(x + 1, y + 1);
                            break;
                        case 3:
                            vertex.Position = new Vector3(x + 1, y + 0, 0) * Game1.TileSize;
                            light += GetVisibleLight(x, y - 1) + GetVisibleLight(x + 1, y) + GetVisibleLight(x + 1, y - 1);
                            vertex.Color = Color.Yellow;
                            break;
                    }

                    var lightShade = Math.Min(light / 4.0f / MaxVisibleLightLevel, 1.0f);
                    _lightBuffer[cornerI] = lightShade;
                    vertex.Color = new Color(0.0f, 0.0f, 0.0f, 1.0f - lightShade);

                    _lightmapVertices.Add(vertex);

                }

                OrientFace();
            }
        }

        // TODO:
        if (_lightmapVertices.Count == 0 || _lightmapIndices.Count == 0) return;

        if (LightmapVertexBuffer is null || LightmapVertexBuffer.VertexCount < _lightmapVertices.Count)
        {
            LightmapVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), _lightmapVertices.Array.Length,
                BufferUsage.WriteOnly);
        }

        if (LightmapIndexBuffer is null || LightmapIndexBuffer.IndexCount < _lightmapIndices.Count)
        {
            LightmapIndexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort), _lightmapIndices.Array.Length, BufferUsage.WriteOnly);
        }

        LightmapVertexBuffer.SetData(_lightmapVertices.Array, 0, _lightmapVertices.Count);
        LightmapIndexBuffer.SetData(_lightmapIndices.Array, 0, _lightmapIndices.Count);

        LightmapPrimitiveCount = _lightmapIndices.Count / 3;
    }

    private void OrientFace()
    {
        if (_lightBuffer[0] + _lightBuffer[2] > _lightBuffer[1] + _lightBuffer[3]) return;

        var faceStart = _lightmapVertices.Count - 4;
        var v0 = _lightmapVertices[faceStart];
        var v1 = _lightmapVertices[faceStart + 1];
        var v2 = _lightmapVertices[faceStart + 2];
        var v3 = _lightmapVertices[faceStart + 3];

        _lightmapVertices[faceStart] = v3;
        _lightmapVertices[faceStart + 1] = v0;
        _lightmapVertices[faceStart + 2] = v1;
        _lightmapVertices[faceStart + 3] = v2;
    }

    struct LightingUpdate
    {
        public int X;
        public int Y;
    }
}