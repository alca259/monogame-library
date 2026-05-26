namespace Alca.MonoGame.Kernel.Network;

/// <summary>Defines the role of the local peer in the network session.</summary>
public enum NetworkMode
{
    /// <summary>This peer acts as a dedicated server: accepts connections but does not participate as a client.</summary>
    Server,

    /// <summary>This peer acts as a pure client: connects to a remote server.</summary>
    Client,

    /// <summary>
    /// This peer runs both server and client simultaneously (listen-server).
    /// Commonly used for peer-to-peer or host-migration scenarios.
    /// </summary>
    Host
}
