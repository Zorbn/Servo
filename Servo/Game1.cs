using System;
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
    private BlendState _multiplyBlendState;

    private Map _map;
    private Tile _selectedTile = Tile.Air;

    private MouseState? _lastMouseState;

    private const float ItemDuctNetworkTickTime = 1f;
    private float _itemDuctNetworkTickTimer = 0f;

    private Effect _lightEffect;
    private RenderTarget2D _lightTarget;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        _graphics.SynchronizeWithVerticalRetrace = false;

        _multiplyBlendState = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero
        };
    }

    protected override void Initialize()
    {
        base.Initialize();

        _map = new Map(GraphicsDevice);

        _lightTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false,
            GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
    }

    protected override void LoadContent()
    {
        _textureAtlas = Content.Load<Texture2D>("atlas");
        _lightEffect = Content.Load<Effect>("Blur");

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
        if ( /*_lastMouseState?.LeftButton != ButtonState.Pressed &&*/ mouseState.LeftButton == ButtonState.Pressed &&
                                                                       (int)_selectedTile >= 0 &&
                                                                       (int)_selectedTile < TileData.TileCount)
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

        _map.UpdateLighting();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Console.WriteLine($"{1.0 / gameTime.ElapsedGameTime.TotalSeconds}");

        Debug.Assert(_spriteBatch is not null && _textureAtlas is not null);

        var view = Matrix.Identity;
        var width = GraphicsDevice.Viewport.Width;
        var height = GraphicsDevice.Viewport.Height;
        var projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);

        _lightEffect.Parameters["WorldViewProjection"].SetValue(view * projection);
        _lightEffect.Parameters["Direction"].SetValue(new Vector2(1.0f / width, 0.0f));

        GraphicsDevice.SetRenderTarget(_lightTarget);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _lightEffect);

        _spriteBatch.Draw(_map.LightmapTexture, new Rectangle(0, 0, Map.Size * TileSize, Map.Size * TileSize),
            new Rectangle(0, 0, Map.Size, Map.Size),
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0f, Vector2.Zero, SpriteEffects.None, 0f);

        _spriteBatch.End();

        _lightEffect.Parameters["Direction"].SetValue(new Vector2(0.0f, 1.0f / height));

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointWrap);

        for (var y = 0; y < Map.Size; y++)
        {
            for (var x = 0; x < Map.Size; x++)
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
        _spriteBatch.Draw(_map.LightmapTexture, new Rectangle(0, 16, Map.Size, Map.Size),
            new Color(1.0f, 1.0f, 1.0f, 1.0f));

        _spriteBatch.End();

        // GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: _multiplyBlendState, effect: _lightEffect);

        _spriteBatch.Draw(_lightTarget, new Rectangle(0, 0, width, height), new Rectangle(0, 0, width, height),
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0f, Vector2.Zero, SpriteEffects.None, 0f);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}