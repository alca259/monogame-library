# Networking — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Network`

El módulo de networking usa **LiteNetLib 1.3.x** como transporte UDP y proporciona una API de mensajes tipados, replicación de campos y sincronización de transformaciones.

---

## Arquitectura

```
NetworkManagerBehaviour (GameBehaviour)
    │
    ├── NetworkServer      ← modos Server y Host
    └── NetworkClient      ← modos Client y Host

[por entidad replicada]:
    └── NetworkIdentity
            ├── NetworkTransformSync  ← sincroniza Transform
            ├── NetworkPhysicsSync    ← sincroniza RigidBody2D
            └── NetField[]            ← campos individuales (NetInt, NetFloat, NetVector2…)
```

---

## Modos de red

| Modo | Descripción |
|---|---|
| `Server` | Servidor dedicado; acepta conexiones, no participa como cliente |
| `Client` | Cliente puro; se conecta a un servidor remoto |
| `Host` | Listen-server; ejecuta servidor y cliente en el mismo proceso |

---

## Canales de entrega

| Canal | Descripción |
|---|---|
| `Unreliable` | Sin garantía; el más rápido. Ideal para posiciones |
| `ReliableUnordered` | Garantizado, puede llegar fuera de orden |
| `ReliableOrdered` | Garantizado en orden de envío |
| `Sequenced` | Solo el paquete más reciente; los obsoletos se descartan |

---

## Ver también

- [Server y Client →](server-client.md)
- [NetworkIdentity y replicación →](network-identity.md)
- [NetFields →](net-fields.md)
