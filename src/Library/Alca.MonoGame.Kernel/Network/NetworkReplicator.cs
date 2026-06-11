using System.Reflection;
using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network.NetFields;
using Alca.MonoGame.Kernel.Network.NetSync;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Scans all <see cref="GameBehaviour"/> components on the same entity for members decorated with
/// <see cref="NetSyncAttribute"/> and automatically registers matching <see cref="NetField"/> instances
/// with the entity's <see cref="NetworkIdentity"/>.
/// </summary>
public sealed class NetworkReplicator : GameBehaviour
{
    private const int MaxEntries = 64;

    private NetworkIdentity? _identity;
    private readonly ReplicatedEntry[] _entries = new ReplicatedEntry[MaxEntries];
    private int _entryCount;

    /// <inheritdoc/>
    public override void Awake()
    {
        _identity = Entity.GetComponent<NetworkIdentity>()
            ?? throw new InvalidOperationException(
                "NetworkReplicator requires a NetworkIdentity on the same entity.");

        ScanBehaviours();
        _identity.OnFieldsApplied += OnFieldsApplied;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        for (int i = 0; i < _entryCount; i++)
        {
            ref var entry = ref _entries[i];
            object? current = entry._member.GetValue(entry._owner);
            if (current is null) continue;
            entry._netField.SetValue(current);
        }
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        if (_identity is not null)
            _identity.OnFieldsApplied -= OnFieldsApplied;
    }

    #region Internal
    private void ScanBehaviours()
    {
        var behaviours = Entity.GetAllComponents();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var behaviour in behaviours)
        {
            if (behaviour is NetworkReplicator) continue;

            Type type = behaviour.GetType();

            foreach (var prop in type.GetProperties(flags))
            {
                var attr = prop.GetCustomAttribute<NetSyncAttribute>();
                if (attr is null) continue;
                RegisterMember(behaviour, new PropertyMemberAccessor(prop), prop.PropertyType, attr);
            }

            foreach (var field in type.GetFields(flags))
            {
                var attr = field.GetCustomAttribute<NetSyncAttribute>();
                if (attr is null) continue;
                RegisterMember(behaviour, new FieldMemberAccessor(field), field.FieldType, attr);
            }
        }
    }

    private void RegisterMember(GameBehaviour owner, IMemberAccessor member, Type memberType,
        NetSyncAttribute attr)
    {
        if (_entryCount >= MaxEntries) return;

        NetField netField = attr.NetFieldType is not null
            ? CreateNetField(attr.NetFieldType)
            : CreateNetFieldForType(memberType);

        _identity!.RegisterField(netField);
        _entries[_entryCount++] = new ReplicatedEntry(owner, member, netField);

    }

    private static NetField CreateNetFieldForType(Type type)
    {
        if (type == typeof(bool))   return new NetBool();
        if (type == typeof(byte))   return new NetByte();
        if (type == typeof(int))    return new NetInt();
        if (type == typeof(uint))   return new NetUInt();
        if (type == typeof(float))  return new NetFloat();
        if (type == typeof(double)) return new NetDouble();
        if (type == typeof(string)) return new NetString();
        if (type == typeof(Vector2)) return new NetVector2();
        if (type == typeof(Vector3)) return new NetVector3();

        throw new NotSupportedException(
            $"[NetSync] type '{type.Name}' is not supported. Supported types: " +
            "bool, byte, int, uint, float, double, string, Vector2, Vector3.");
    }

    private static NetField CreateNetField(Type netFieldType)
    {
        return (NetField)(Activator.CreateInstance(netFieldType)
            ?? throw new InvalidOperationException($"Could not create instance of {netFieldType.Name}."));
    }

    private void OnFieldsApplied()
    {
        for (int i = 0; i < _entryCount; i++)
        {
            ref var entry = ref _entries[i];
            object? value = entry._netField.GetValue();
            if (value is null) continue;
            entry._member.SetValue(entry._owner, value);
        }
    }
    #endregion

    #region Internal types
    private readonly struct ReplicatedEntry
    {
        internal readonly GameBehaviour _owner;
        internal readonly IMemberAccessor _member;
        internal readonly NetField _netField;

        internal ReplicatedEntry(GameBehaviour owner, IMemberAccessor member, NetField netField)
        {
            _owner = owner;
            _member = member;
            _netField = netField;
        }
    }

    private interface IMemberAccessor
    {
        object? GetValue(object instance);
        void SetValue(object instance, object? value);
    }

    private sealed class PropertyMemberAccessor : IMemberAccessor
    {
        private readonly PropertyInfo _prop;
        internal PropertyMemberAccessor(PropertyInfo prop) => _prop = prop;
        public object? GetValue(object instance) => _prop.GetValue(instance);
        public void SetValue(object instance, object? value) => _prop.SetValue(instance, value);
    }

    private sealed class FieldMemberAccessor : IMemberAccessor
    {
        private readonly FieldInfo _field;
        internal FieldMemberAccessor(FieldInfo field) => _field = field;
        public object? GetValue(object instance) => _field.GetValue(instance);
        public void SetValue(object instance, object? value) => _field.SetValue(instance, value);
    }
    #endregion
}
