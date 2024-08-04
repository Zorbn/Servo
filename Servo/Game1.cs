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
    private DepthStencilState _maskStencilState;
    private DepthStencilState _maskedStencilState;

    private Map _map;
    private Map _backMap;
    private Tile _selectedTile = Tile.Air;

    private MouseState? _lastMouseState;

    private const float ItemDuctNetworkTickTime = 1f;
    private float _itemDuctNetworkTickTimer = 0f;

    private Effect _lightEffect;
    private RenderTarget2D _lightTarget;
    private RenderTarget2D _backLightTarget;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        _graphics.SynchronizeWithVerticalRetrace = false;

        _multiplyBlendState = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
        };

        _maskStencilState = new DepthStencilState
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1,
            DepthBufferEnable = false,
        };

        _maskedStencilState = new DepthStencilState
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.LessEqual,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferEnable = false,
        };
    }

    protected override void Initialize()
    {
        base.Initialize();

        _map = new Map(GraphicsDevice);
        _backMap = new Map(GraphicsDevice);

        _lightTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false,
            GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);

        _backLightTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false,
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

        var keyboardState = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        var mouseState = Mouse.GetState();
        var mouseTileX = mouseState.X / TileSize;
        var mouseTileY = mouseState.Y / TileSize;
        if ( /*_lastMouseState?.LeftButton != ButtonState.Pressed &&*/ mouseState.LeftButton == ButtonState.Pressed &&
                                                                       (int)_selectedTile >= 0 &&
                                                                       (int)_selectedTile < TileData.TileCount)
        {
            var isTargetingBack = keyboardState.IsKeyDown(Keys.LeftShift);
            var targetMap = isTargetingBack ? _backMap : _map;
            var otherMap = isTargetingBack ? _map : _backMap;

            targetMap.SetTile(mouseTileX, mouseTileY, _selectedTile, otherMap);
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
            ItemDuctNetwork.Tick(_map, _backMap);
            ItemDuctNetwork.Tick(_backMap, _map);
        }

        _map.UpdateFrontLighting(_backMap);
        _backMap.UpdateBackLighting(_map);

        base.Update(gameTime);
    }

    private void DrawLightToTarget(Map map, RenderTarget2D target, Matrix viewProjection, int width)
    {
        _lightEffect.Parameters["WorldViewProjection"].SetValue(viewProjection);
        _lightEffect.Parameters["Direction"].SetValue(new Vector2(1.0f / width, 0.0f));

        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _lightEffect);

        _spriteBatch.Draw(map.LightmapTexture, new Rectangle(0, 0, Map.Size * TileSize, Map.Size * TileSize),
            new Rectangle(0, 0, Map.Size, Map.Size),
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0f, Vector2.Zero, SpriteEffects.None, 0f);

        _spriteBatch.End();
    }

    private void DrawLightTargetToScreen(RenderTarget2D target, int width, int height, DepthStencilState? depthStencilState = null)
    {
        _lightEffect.Parameters["Direction"].SetValue(new Vector2(0.0f, 1.0f / height));

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: _multiplyBlendState, effect: _lightEffect, depthStencilState: depthStencilState);

        _spriteBatch.Draw(target, new Rectangle(0, 0, width, height), new Rectangle(0, 0, width, height),
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0f, Vector2.Zero, SpriteEffects.None, 0f);

        _spriteBatch.End();
    }

    private void DrawMap(Map map, Color color, DepthStencilState? depthStencilState = null)
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointWrap, depthStencilState: depthStencilState);

        for (var y = 0; y < Map.Size; y++)
        {
            for (var x = 0; x < Map.Size; x++)
            {
                var tile = map.GetTile(x, y);

                if (tile == Tile.Air)
                {
                    continue;
                }

                var tileColor = color;

                if (tile == Tile.ItemDuct)
                {
                    tileColor = ((ItemDuctEntity)map.GetTileEntity(x, y)!).Color;
                }

                _spriteBatch.Draw(_textureAtlas, new Vector2(x * TileSize, y * TileSize),
                    new Rectangle(tile.GetTextureIndex() * TileSize, 0, TileSize, TileSize), tileColor);
            }
        }

        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        Console.WriteLine($"{1.0 / gameTime.ElapsedGameTime.TotalSeconds}");

        Debug.Assert(_spriteBatch is not null && _textureAtlas is not null);

        var view = Matrix.Identity;
        var width = GraphicsDevice.Viewport.Width;
        var height = GraphicsDevice.Viewport.Height;
        var projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        var viewProjection = view * projection;

        DrawLightToTarget(_backMap, _backLightTarget, viewProjection, width);
        DrawLightToTarget(_map, _lightTarget, viewProjection, width);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        DrawMap(_backMap, Color.LightGray, _maskStencilState);
        DrawLightTargetToScreen(_backLightTarget, width, height, _maskedStencilState);
        DrawMap(_map, Color.White, _maskStencilState);
        DrawLightTargetToScreen(_lightTarget, width, height, _maskedStencilState);

        _spriteBatch.Begin(samplerState: SamplerState.PointWrap);

        _spriteBatch.Draw(_textureAtlas, Vector2.Zero,
            new Rectangle(_selectedTile.GetTextureIndex() * TileSize, 0, TileSize, TileSize), Color.White);
        _spriteBatch.Draw(_map.LightmapTexture, new Rectangle(0, 16, Map.Size, Map.Size),
            new Color(1.0f, 1.0f, 1.0f, 1.0f));

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}