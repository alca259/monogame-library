using Alca.MonoGame.Kernel.Network;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 40 — NetworkServer and NetworkClient loopback demo with position replication.</summary>
public sealed class NetworkingScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly NetworkServer _server = new();
    private readonly NetworkClient _client = new();

    private Vector2 _serverEntityPos = new(640, 360);
    private Vector2 _clientMirrorPos = new(640, 360);

    private int _serverSent;
    private int _clientReceived;
    private float _sendAccum;
    private const float SendInterval = 0.05f; // 20Hz

    private Label _serverStateLabel = null!;
    private Label _serverClientsLabel = null!;
    private Label _serverSentLabel = null!;
    private Label _serverPosLabel = null!;
    private Label _clientStateLabel = null!;
    private Label _clientPingLabel = null!;
    private Label _clientRecvLabel = null!;
    private Label _clientPosLabel = null!;

    private readonly System.Text.StringBuilder _sb = new(64);

    // INetworkMessage for position sync (ref struct layout for NetworkWriter/Reader)
    private struct PositionMessage : INetworkMessage
    {
        public const ushort MsgId = 1;
        ushort INetworkMessage.MessageId => MsgId;
        public float X;
        public float Y;

        public void Serialize(ref NetworkWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
        }

        public void Deserialize(ref NetworkReader reader)
        {
            X = reader.ReadFloat();
            Y = reader.ReadFloat();
        }
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _server.OnClientConnected += peerId => { };
        _client.RegisterHandler<PositionMessage>(msg => { _clientMirrorPos = new Vector2(msg.X, msg.Y); _clientReceived++; });

        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 40 };

        // --- Server panel ---
        var serverCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () =>
        {
            _server.Stop();
            _client.Disconnect();
            Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        };
        serverCol.Add(backBtn);

        serverCol.Add(new Label { Font = _font, Text = "Servidor", Color = Color.Yellow });

        var startServerBtn = new Button(_font, "Iniciar servidor (7777)") { BackgroundPixel = _pixel };
        startServerBtn.Clicked += () => { if (!_server.IsRunning) _server.Start(7777); };
        serverCol.Add(startServerBtn);

        var stopServerBtn = new Button(_font, "Detener servidor") { BackgroundPixel = _pixel };
        stopServerBtn.Clicked += () => _server.Stop();
        serverCol.Add(stopServerBtn);

        _serverStateLabel = new Label { Font = _font, Text = "Estado: Offline", Color = Color.LightGray };
        _serverClientsLabel = new Label { Font = _font, Text = "Clientes: 0", Color = Color.LightGray };
        _serverSentLabel = new Label { Font = _font, Text = "Enviados: 0", Color = Color.LightGray };
        _serverPosLabel = new Label { Font = _font, Text = "Pos: 0, 0", Color = Color.LightGreen };
        serverCol.Add(_serverStateLabel);
        serverCol.Add(_serverClientsLabel);
        serverCol.Add(_serverSentLabel);
        serverCol.Add(_serverPosLabel);
        serverCol.Add(new Label { Font = _font, Text = "WASD: mover entidad (server)", Color = Color.LightGray });

        root.Add(serverCol);

        // --- Client panel ---
        var clientCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        clientCol.Add(new Label { Font = _font, Text = "Cliente", Color = Color.Yellow });

        var connectBtn = new Button(_font, "Conectar a localhost:7777") { BackgroundPixel = _pixel };
        connectBtn.Clicked += () => { if (!_client.IsConnected) _client.Connect("127.0.0.1", 7777); };
        clientCol.Add(connectBtn);

        var disconnectBtn = new Button(_font, "Desconectar") { BackgroundPixel = _pixel };
        disconnectBtn.Clicked += () => _client.Disconnect();
        clientCol.Add(disconnectBtn);

        _clientStateLabel = new Label { Font = _font, Text = "Estado: Offline", Color = Color.LightGray };
        _clientPingLabel = new Label { Font = _font, Text = "Ping: — ms", Color = Color.LightGray };
        _clientRecvLabel = new Label { Font = _font, Text = "Recibidos: 0", Color = Color.LightGray };
        _clientPosLabel = new Label { Font = _font, Text = "Pos recibida: 0, 0", Color = Color.LightGreen };
        clientCol.Add(_clientStateLabel);
        clientCol.Add(_clientPingLabel);
        clientCol.Add(_clientRecvLabel);
        clientCol.Add(_clientPosLabel);

        root.Add(clientCol);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(root, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _server.Poll();
        _client.Poll();

        // Server: WASD moves entity
        if (_server.IsRunning)
        {
            KeyboardState ks = Keyboard.GetState();
            const float Speed = 200f;
            int w = Core.GraphicsDevice.Viewport.Width;
            int h = Core.GraphicsDevice.Viewport.Height;

            if (ks.IsKeyDown(Keys.W)) _serverEntityPos.Y -= Speed * dt;
            if (ks.IsKeyDown(Keys.S)) _serverEntityPos.Y += Speed * dt;
            if (ks.IsKeyDown(Keys.A)) _serverEntityPos.X -= Speed * dt;
            if (ks.IsKeyDown(Keys.D)) _serverEntityPos.X += Speed * dt;
            _serverEntityPos.X = Math.Clamp(_serverEntityPos.X, 0, w);
            _serverEntityPos.Y = Math.Clamp(_serverEntityPos.Y, 0, h);

            _sendAccum += dt;
            if (_sendAccum >= SendInterval && _server.ConnectedPeers > 0)
            {
                _sendAccum = 0f;
                var msg = new PositionMessage { X = _serverEntityPos.X, Y = _serverEntityPos.Y };
                _server.Broadcast(ref msg, NetworkChannel.Unreliable);
                _serverSent++;
            }
        }

        // Update labels
        _serverStateLabel.Text = _server.IsRunning ? "Estado: Running" : "Estado: Offline";
        _serverStateLabel.Color = _server.IsRunning ? Color.LimeGreen : Color.Gray;

        _sb.Clear();
        _sb.Append("Clientes: ");
        _sb.Append(_server.ConnectedPeers);
        _serverClientsLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Enviados: ");
        _sb.Append(_serverSent);
        _serverSentLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Pos: ");
        _sb.Append(((int)_serverEntityPos.X).ToString());
        _sb.Append(", ");
        _sb.Append(((int)_serverEntityPos.Y).ToString());
        _serverPosLabel.Text = _sb.ToString();

        _clientStateLabel.Text = _client.IsConnected ? "Estado: Connected" : "Estado: Offline";
        _clientStateLabel.Color = _client.IsConnected ? Color.LimeGreen : Color.Gray;

        _sb.Clear();
        _sb.Append("Ping: ");
        _sb.Append(_client.IsConnected ? _client.Ping.ToString() : "—");
        _sb.Append(" ms");
        _clientPingLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Recibidos: ");
        _sb.Append(_clientReceived);
        _clientRecvLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Pos recibida: ");
        _sb.Append(((int)_clientMirrorPos.X).ToString());
        _sb.Append(", ");
        _sb.Append(((int)_clientMirrorPos.Y).ToString());
        _clientPosLabel.Text = _sb.ToString();

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Server entity (blue)
        Core.SpriteBatch.Draw(_pixel,
            new Rectangle((int)_serverEntityPos.X - 16, (int)_serverEntityPos.Y - 16, 32, 32),
            Color.DeepSkyBlue);
        Core.SpriteBatch.DrawString(_font, "S", new Vector2(_serverEntityPos.X - 6, _serverEntityPos.Y - 8), Color.White);

        // Client mirror (green, slightly offset)
        if (_client.IsConnected)
        {
            Core.SpriteBatch.Draw(_pixel,
                new Rectangle((int)_clientMirrorPos.X - 12, (int)_clientMirrorPos.Y - 12, 24, 24),
                Color.LimeGreen * 0.7f);
            Core.SpriteBatch.DrawString(_font, "C", new Vector2(_clientMirrorPos.X - 6, _clientMirrorPos.Y - 8), Color.White);
        }

        Core.SpriteBatch.DrawString(_font, "Mover con WASD (lado servidor)", new Vector2(400, 350), Color.DimGray);

        Core.SpriteBatch.End();
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _server.Stop();
            _server.Dispose();
            _client.Disconnect();
            _client.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
