namespace DungeonGuide;

/// <summary>Reads world identity only after the network has received its world descriptor.</summary>
internal static class GameWorld
{
    /// <summary>Current world UID, or null before world initialization and after disconnection.</summary>
    public static long? Id
    {
        get
        {
            var network = ZNet.instance;
            return network ? network.GetWorld()?.m_uid : null;
        }
    }
}
