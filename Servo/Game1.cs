﻿using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Servo;

public class Game1 : Game
{
    private const int TileSize = 16;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _textureAtlas;

    private Map _map = new();
    private Tile _selectedTile = Tile.Air;

    private MouseState? _lastMouseState;

    private const float ItemDuctNetworkTickTime = 1f;
    private float _itemDuctNetworkTickTimer = 0f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        _graphics.SynchronizeWithVerticalRetrace = false;
    }

    protected override void Initialize()
    {
        base.Initialize();

        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                _map.SetTile(x, y, Tile.ItemDuct);
            }
        }
    }

    protected override void LoadContent()
    {
        _textureAtlas = Content.Load<Texture2D>("atlas");

        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var mouseState = Mouse.GetState();
        var mouseTileX = mouseState.X / TileSize;
        var mouseTileY = mouseState.Y / TileSize;
        if (/*_lastMouseState?.LeftButton != ButtonState.Pressed &&*/ mouseState.LeftButton == ButtonState.Pressed &&
            (int)_selectedTile >= 0 && (int)_selectedTile < TileData.TileCount)
        {
            _map.SetTile(mouseTileX, mouseTileY, _selectedTile);
        }

        if (_lastMouseState is not null)
        {
            if (mouseState.ScrollWheelValue > _lastMouseState.Value.ScrollWheelValue)
            {
                _selectedTile = (Tile)((int)_selectedTile + 1);
            }

            if (mouseState.ScrollWheelValue < _lastMouseState.Value.ScrollWheelValue)
            {
                _selectedTile = (Tile)((int)_selectedTile - 1);
            }
        }

        _lastMouseState = mouseState;

        _itemDuctNetworkTickTimer += deltaTime;

        while (_itemDuctNetworkTickTimer > ItemDuctNetworkTickTime)
        {
            _itemDuctNetworkTickTimer -= ItemDuctNetworkTickTime;
            ItemDuctNetwork.Tick(_map);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Debug.Assert(_spriteBatch is not null && _textureAtlas is not null);

        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointWrap);

        for (var y = 0; y < Map.MapSize; y++)
        {
            for (var x = 0; x < Map.MapSize; x++)
            {
                var tile = _map.GetTile(x, y);

                if (tile == Tile.Air)
                {
                    continue;
                }

                var color = Color.White;

                if (tile == Tile.ItemDuct)
                {
                    color = ((ItemDuctEntity)_map.GetTileEntity(x, y)!).Color;
                }

                _spriteBatch.Draw(_textureAtlas, new Vector2(x * TileSize, y * TileSize),
                    new Rectangle(tile.GetTextureIndex() * TileSize, 0, TileSize, TileSize), color);
            }
        }

        _spriteBatch.Draw(_textureAtlas, Vector2.Zero,
            new Rectangle(_selectedTile.GetTextureIndex() * TileSize, 0, TileSize, TileSize), Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}