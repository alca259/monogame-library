# Spec: Fase 10.x — Networking (Cliente/Servidor básico)

## Objetivo

Proporcionar comunicación en red UDP con capas de fiabilidad opcionales para juegos multijugador pequeños (hasta ~64 peers). La arquitectura sigue el modelo cliente/servidor clásico, con integración ECS vía `GameBehaviour` y registro en DI. **Zero-alloc en el game loop**: todas las allocations ocurren en `Initialize`/`LoadContent` o fuera del hilo de Update.

**Dependencia NuGet:**
```xml
<PackageReference Include="LiteNetLib" Version="1.3.*" />
```

---

## Diseño general

```
NetworkServer ────┐
                  ├── NetworkManager (singleton DI)
NetworkClient ────┘
      │
NetworkTransport (abstracción sobre LiteNetLib)
      │
INetworkMessage ──► NetworkWriter / NetworkReader (ref struct, Span-based)
```

Los mensajes se identifican por un `ushort MessageId`. La serialización es manual y basada en `Span<byte>` para evitar allocations.

---

## Milestone 1 — NetworkChannel y tipos base

**`Network/NetworkChannel.cs`** — `enum NetworkChannel`

```csharp
public enum NetworkChannel : byte
{
    /// <summary>UDP puro: máxima velocidad, puede perderse o llegar fuera de orden.</summary>
    Unreliable,
    /// <summary>Garantiza entrega, sin orden.</summary>
    ReliableUnordered,
    /// <summary>Garantiza entrega y orden.</summary>
    ReliableOrdered,
    /// <summary>Solo el último paquete importa; descarta anteriores si llegan tarde.</summary>
    Sequenced,
}
```

**`Network/INetworkMessage.cs`** — `interface INetworkMessage`

```csharp
public interface INetworkMessage
{
    /// <summary>Identificador único del tipo de mensaje (16 bit).</summary>
    ushort MessageId { get; }

    /// <summary>Serializa el mensaje en el writer.</summary>
    void Serialize(ref NetworkWriter writer);

    /// <summary>Deserializa el mensaje desde el reader.</summary>
    void Deserialize(ref NetworkReader reader);
}
```

---

## Milestone 2 — NetworkWriter y NetworkReader

**`Network/NetworkWriter.cs`** — `ref struct NetworkWriter`

Buffer de escritura sobre un `Span<byte>` pre-allocated (el caller aporta el buffer).

```csharp
public ref struct NetworkWriter
{
    public NetworkWriter(Span<byte> buffer);

    public int Position { get; }
    public int Remaining { get; }

    public void Write(bool value);
    public void Write(byte value);
    public void Write(short value);
    public void Write(ushort value);
    public void Write(int value);
    public void Write(uint value);
    public void Write(float value);
    public void Write(double value);
    /// <summary>Escribe longitud (ushort) + bytes UTF-8. Lanza si supera 65535 bytes.</summary>
    public void WriteString(ReadOnlySpan<char> value);
    public void Write(Vector2 value);
    public void Write(Vector3 value);

    /// <summary>Slice del buffer relleno hasta <see cref="Position"/>.</summary>
    public ReadOnlySpan<byte> WrittenSpan { get; }
}
```

**`Network/NetworkReader.cs`** — `ref struct NetworkReader`

```csharp
public ref struct NetworkReader
{
    public NetworkReader(ReadOnlySpan<byte> data);

    public int Position { get; }
    public int Remaining { get; }

    public bool ReadBool();
    public byte ReadByte();
    public short ReadShort();
    public ushort ReadUShort();
    public int ReadInt();
    public uint ReadUInt();
    public float ReadFloat();
    public double ReadDouble();
    /// <summary>Devuelve el string leído; alloc inevitable para el string gestionado.</summary>
    public string ReadString();
    public Vector2 ReadVector2();
    public Vector3 ReadVector3();
}
```

> **Nota de diseño:** `NetworkWriter`/`NetworkReader` son `ref struct` (stack-only). El buffer de bytes de envío se pre-alloca en `NetworkServer`/`NetworkClient` como campo `byte[]` fijo y se reutiliza cada frame.

---

## Milestone 3 — NetworkServer

**`Network/NetworkServer.cs`** — `sealed class NetworkServer`

```csharp
public sealed class NetworkServer : IDisposable
{
    // Constructor
    public NetworkServer(int maxConnections = 64, string connectionKey = "AlcaNet");

    // Configuración (debe setearse antes de Start)
    public int MaxConnections { get; }
    public int Port { get; private set; }

    // Estado
    public bool IsRunning { get; }
    public int ConnectedPeers { get; }

    // Ciclo de vida
    /// <summary>Abre el socket UDP en el puerto indicado.</summary>
    public void Start(int port);
    /// <summary>Cierra el socket y desconecta todos los peers.</summary>
    public void Stop();

    // Envío
    /// <summary>Envía un mensaje a un peer concreto.</summary>
    public void Send<T>(int peerId, ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage;
    /// <summary>Envía un mensaje a todos los peers conectados.</summary>
    public void Broadcast<T>(ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage;
    /// <summary>Desconecta un peer.</summary>
    public void Kick(int peerId);

    // Recepción — registrar antes de llamar a Poll()
    public void RegisterHandler<T>(Action<int, T> handler) where T : INetworkMessage, new();
    public void UnregisterHandler<T>() where T : INetworkMessage;

    // Game loop — llamar UNA VEZ por frame desde Update()
    /// <summary>Procesa todos los paquetes pendientes de LiteNetLib. Sin alloc si no hay mensajes.</summary>
    public void Poll();

    // Eventos (no usados en Update; se disparan dentro de Poll)
    public event Action<int>? OnClientConnected;     // peerId
    public event Action<int>? OnClientDisconnected;  // peerId
    public event Action<int, string>? OnClientError; // peerId, reason
}
```

**Notas de implementación:**
- El buffer de serialización (`byte[]`, 4 KB por defecto) se alloca en el constructor y se reusa en cada `Send`.
- `Poll()` itera los eventos de LiteNetLib con un `for` clásico; sin LINQ.
- Los handlers registrados se guardan en `Dictionary<ushort, Action<int, ReadOnlySpan<byte>>>` pre-allocated con capacidad 16; las closures se crean en `RegisterHandler`, no en `Poll`.

---

## Milestone 4 — NetworkClient

**`Network/NetworkClient.cs`** — `sealed class NetworkClient`

```csharp
public sealed class NetworkClient : IDisposable
{
    public NetworkClient(string connectionKey = "AlcaNet");

    // Estado
    public bool IsConnected { get; }
    public int Ping { get; }              // RTT en milisegundos (actualizado por LiteNetLib)
    public int LocalPeerId { get; }       // ID asignado por el servidor

    // Conexión
    public void Connect(string host, int port);
    public void Disconnect();

    // Envío
    public void Send<T>(ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage;

    // Recepción
    public void RegisterHandler<T>(Action<T> handler) where T : INetworkMessage, new();
    public void UnregisterHandler<T>() where T : INetworkMessage;

    // Game loop
    public void Poll();

    // Eventos
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;  // reason
    public event Action<string>? OnConnectionFailed;
}
```

---

## Milestone 5 — NetworkManagerBehaviour (integración ECS)

**`Network/NetworkManagerBehaviour.cs`** — `sealed class NetworkManagerBehaviour : GameBehaviour`

Wrapper de ciclo de vida ECS para servidor o cliente. El juego instancia uno de los dos modos.

```csharp
public sealed class NetworkManagerBehaviour : GameBehaviour
{
    // Solo uno de los dos será no-null según el modo
    public NetworkServer? Server { get; private set; }
    public NetworkClient? Client { get; private set; }
    public NetworkMode Mode { get; private set; }     // enum: Server | Client | Host

    /// <summary>Inicia en modo servidor.</summary>
    public void StartServer(int port, int maxConnections = 64);
    /// <summary>Inicia en modo cliente y conecta.</summary>
    public void StartClient(string host, int port);
    /// <summary>Inicia servidor + cliente local (host).</summary>
    public void StartHost(int port);

    public void Stop();

    // GameBehaviour lifecycle
    public override void Update(GameTime gameTime);   // llama Server?.Poll() y/o Client?.Poll()
    protected override void OnDestroy();              // llama Stop()
}
```

**`Network/NetworkMode.cs`** — `enum NetworkMode`

```csharp
public enum NetworkMode { Server, Client, Host }
```

---

## Milestone 6 — NetworkIdentity (entidades en red)

**`Network/NetworkIdentity.cs`** — `sealed class NetworkIdentity : GameBehaviour`

Marca una entidad como objeto de red con identidad replicada.

```csharp
public sealed class NetworkIdentity : GameBehaviour
{
    /// <summary>ID único asignado por el servidor. 0 = no asignado.</summary>
    public uint NetworkId { get; internal set; }

    /// <summary>True si el peer local es el dueño autoritativo de esta entidad.</summary>
    public bool IsOwner { get; internal set; }

    /// <summary>True si el peer local es el servidor.</summary>
    public bool IsServer { get; internal set; }
}
```

> La asignación de `NetworkId` es responsabilidad del servidor. El cliente recibe el ID via un mensaje de spawn. No se implementa sincronización automática de Transform en esta iteración — es responsabilidad del usuario enviar mensajes de posición manualmente.

---

## Milestone 7 — NetworkStats

**`Network/NetworkStats.cs`** — `readonly struct NetworkStats`

```csharp
public readonly struct NetworkStats
{
    public int PacketsSentTotal { get; init; }
    public int PacketsReceivedTotal { get; init; }
    public long BytesSentTotal { get; init; }
    public long BytesReceivedTotal { get; init; }
    public int PacketLossPercent { get; init; }
    public int Ping { get; init; }
}
```

- `NetworkClient.GetStats()` → `NetworkStats`
- `NetworkServer.GetStats(int peerId)` → `NetworkStats`

---

## Milestone 8 — GameWorld (extensión)

**`ECS/GameWorld.cs`** — **MODIFICAR**

```csharp
public NetworkServer? NetworkServer { get; internal set; }
public NetworkClient? NetworkClient { get; internal set; }
```

El `NetworkManagerBehaviour` setea estas referencias en `StartServer`/`StartClient`/`StartHost`.

---

## Tests esperados

`UnitTests/Network/NetworkWriterReaderTests.cs`
- `Write_ReadBool_RoundTrip`
- `Write_ReadInt_RoundTrip`
- `Write_ReadVector2_RoundTrip`
- `WriteString_ReadString_RoundTrip`
- `WriteString_ExceedsMaxLength_Throws`
- `NetworkWriter_OverflowBuffer_Throws`
- `NetworkReader_ReadPastEnd_Throws`

`UnitTests/Network/NetworkServerTests.cs`
- `Start_ValidPort_IsRunningTrue`
- `Start_AlreadyRunning_Throws`
- `Stop_WhenRunning_IsRunningFalse`
- `RegisterHandler_Duplicate_Overwrites`

`UnitTests/Network/NetworkClientTests.cs`
- `Connect_BeforeServer_EventuallyFiresOnConnectionFailed`
- `Send_WhenNotConnected_Throws`
- `RegisterHandler_Duplicate_Overwrites`

`UnitTests/Network/NetworkManagerBehaviourTests.cs`
- `StartServer_SetsServerProperty`
- `StartClient_SetsClientProperty`
- `Stop_DisposesResources`

---

## Estructura de carpetas

```
src/Library/Alca.MonoGame.Kernel/
└── Network/
    ├── INetworkMessage.cs
    ├── NetworkChannel.cs          (enum)
    ├── NetworkClient.cs
    ├── NetworkIdentity.cs         (GameBehaviour)
    ├── NetworkManagerBehaviour.cs (GameBehaviour)
    ├── NetworkMode.cs             (enum)
    ├── NetworkReader.cs           (ref struct)
    ├── NetworkServer.cs
    ├── NetworkStats.cs            (readonly struct)
    └── NetworkWriter.cs           (ref struct)
```

---

## Verificación

1. Arrancar `NetworkServer` en puerto 7777.
2. Conectar `NetworkClient` a `localhost:7777`.
3. `Server.ConnectedPeers == 1` tras el handshake.
4. Enviar un mensaje custom desde cliente a servidor; el handler del servidor recibe los datos correctamente.
5. `Server.Broadcast` alcanza al cliente; el handler del cliente recibe los datos.
6. Llamar `Client.Disconnect()` → `Server.OnClientDisconnected` se dispara, `Server.ConnectedPeers == 0`.

---

## Limitaciones de esta iteración

- No hay sincronización automática de Transform ni estado de entidades (snapshot interpolation, lag compensation, dead reckoning). Quedan para una iteración futura.
- No hay lobby ni descubrimiento de sesiones.
- No hay cifrado ni autenticación más allá de la `connectionKey`.
- Máximo ~64 peers (límite práctico de LiteNetLib para juegos indie; suficiente para el objetivo de la librería).
