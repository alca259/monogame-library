# NetworkServer y NetworkClient

**Namespace:** `Alca.MonoGame.Kernel.Network`

`NetworkManagerBehaviour` es el punto de entrada al sistema de red. Gestiona el ciclo de vida del `NetworkServer` y/o `NetworkClient` según el modo seleccionado.

---

## NetworkManagerBehaviour

`GameBehaviour` que actúa como fachada para iniciar y detener la red.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Server` | `NetworkServer?` | Instancia del servidor (null si es modo Client) |
| `Client` | `NetworkClient?` | Instancia del cliente (null si es modo Server) |
| `Mode` | `NetworkMode` | Modo activo |

### Métodos

| Método | Descripción |
|---|---|
| `StartServer(port, maxConnections)` | Inicia el servidor en el puerto dado |
| `StartClient(host, port)` | Conecta al servidor remoto |
| `StartHost(port)` | Inicia servidor + cliente en el mismo proceso |
| `Stop()` | Detiene toda la red |

---

## NetworkServer

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsRunning` | `bool` | Si el servidor está activo |
| `ConnectedPeers` | `int` | Número de clientes conectados |

### Eventos

| Evento | Descripción |
|---|---|
| `OnClientConnected` | `Action<int>` — peer ID del cliente conectado |
| `OnClientDisconnected` | `Action<int>` — peer ID del cliente desconectado |
| `OnClientError` | `Action<int, string>` — peer ID + mensaje de error |

### Métodos de envío

```csharp
// Enviar a un cliente específico
server.Send(peerId, ref message, NetworkChannel.ReliableOrdered);

// Broadcast a todos
server.Broadcast(ref message, NetworkChannel.Unreliable);

// Broadcast excluyendo un peer
server.BroadcastExcept(excludePeerId, ref message, NetworkChannel.ReliableOrdered);

// Expulsar cliente
server.Kick(peerId);
```

### Registro de manejadores

```csharp
server.RegisterHandler<ChatMessage>((peerId, msg) =>
{
    Console.WriteLine($"[{peerId}] {msg.Text}");
    server.BroadcastExcept(peerId, ref msg); // reenviar al resto
});
server.UnregisterHandler<ChatMessage>();
```

---

## NetworkClient

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsConnected` | `bool` | Si está conectado al servidor |
| `Ping` | `int` | Latencia actual en ms |
| `LocalPeerId` | `int` | ID asignado por el servidor |

### Eventos

| Evento | Descripción |
|---|---|
| `OnConnected` | `Action` — conexión establecida |
| `OnDisconnected` | `Action<string>` — razón de desconexión |
| `OnConnectionFailed` | `Action` — no se pudo conectar |

### Métodos

```csharp
client.Connect("127.0.0.1", 9050);
client.Send(ref message, NetworkChannel.ReliableOrdered);
client.Disconnect();
```

---

## INetworkMessage

Contrato de serialización para mensajes personalizados.

```csharp
public interface INetworkMessage
{
    ushort MessageId { get; }
    void Serialize(ref NetworkWriter writer)
    void Deserialize(ref NetworkReader reader)
}
```

`NetworkWriter` y `NetworkReader` son `ref struct` sin allocations. Soportan bool, byte, short, int, uint, float, double, Vector2, Vector3 y string.

---

## Ejemplo: juego multijugador local

```csharp
// 1. Definir un mensaje
public readonly struct MoveMessage : INetworkMessage
{
    public ushort MessageId => 1;
    public uint   PlayerId  { get; init; }
    public Vector2 Position { get; init; }

    public void Serialize(ref NetworkWriter w)
    {
        w.Write(PlayerId);
        w.Write(Position);
    }
    public void Deserialize(ref NetworkReader r)
    {
        // Usa init; en runtime se reconstruye como new struct
        Unsafe.AsRef(in PlayerId)  = r.ReadUInt();
        Unsafe.AsRef(in Position) = r.ReadVector2();
    }
}

// 2. Escena de red
public sealed class MultiplayerScene : Scene
{
    private NetworkManagerBehaviour _net = null!;

    protected override void InitializeWorld()
    {
        var netEntity = World!.CreateEntity("Network", Vector2.Zero);
        _net = netEntity.AddComponent<NetworkManagerBehaviour>();

        // Servidor o cliente según línea de comandos
        if (IsHost())
            _net.StartHost(port: 9050);
        else
            _net.StartClient("127.0.0.1", 9050);

        // Registrar manejador en el cliente
        _net.Client?.RegisterHandler<MoveMessage>(msg =>
        {
            // Actualizar posición del jugador remoto
            UpdateRemotePlayer(msg.PlayerId, msg.Position);
        });
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Enviar posición del jugador local
        var msg = new MoveMessage { PlayerId = 1, Position = LocalPlayerPosition() };
        _net.Client?.Send(ref msg, NetworkChannel.Unreliable);
    }
}
```

---

## Notas

- `NetworkServer.Poll()` y `NetworkClient.Poll()` son llamados automáticamente por `NetworkManagerBehaviour.Update`.
- `NetworkStats GetStats(peerId)` devuelve paquetes enviados/recibidos, bytes y porcentaje de pérdida.
- La `connectionKey` (default `"AlcaNet"`) debe coincidir entre servidor y cliente.

---

## Ver también

- [NetworkIdentity →](network-identity.md)
- [NetFields →](net-fields.md)
