namespace Alca.MonoGame.Kernel.Network.Messages;

/// <summary>Reserved message IDs used by the kernel's built-in networking system.</summary>
internal static class SystemMessageId
{
    internal const ushort Spawn = 0x0001;
    internal const ushort Despawn = 0x0002;
    internal const ushort FieldsSync = 0x0003;
    internal const ushort TransformSync = 0x0004;
}
