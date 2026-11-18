namespace Collection.Library.DataTransferObject;

// ---------------------------------------------------------------------------------------------
// Snapshot (session replay) write objects - what the recorder sends to the repository.
// ---------------------------------------------------------------------------------------------

/// <summary>Created once when a signed-in user's recording starts.</summary>
public class SnapshotSessionDataTransferObject
{
    public Guid flngKey { get; set; } = Guid.NewGuid();

    /// <summary>Taken from the signed-in user, never from the browser.</summary>
    public string fstrUsername { get; set; } = string.Empty;
    public string fstrFullName { get; set; } = string.Empty;
    public string fstrLocationKey { get; set; } = string.Empty;
    public string fstrRole { get; set; } = string.Empty;

    /// <summary>Development / Staging / Production - keeps rows identifiable after a production-to-staging copy.</summary>
    public string fstrEnvironment { get; set; } = string.Empty;
    public string fstrEntryUrl { get; set; } = string.Empty;
    public int flngViewportW { get; set; }
    public int flngViewportH { get; set; }
    public string fstrUserAgent { get; set; } = string.Empty;
    public string? fstrWho { get; set; }
}

/// <summary>One click / page change / error sent by the browser together with its chunk.</summary>
public class SnapshotMarkerDataTransferObject
{
    /// <summary>Unix time in milliseconds (same clock as the recording).</summary>
    public long flngTimestampMs { get; set; }
    public string fstrKind { get; set; } = "click";
    public string fstrLabel { get; set; } = string.Empty;
    public string? fstrUrl { get; set; }
}

/// <summary>The next few seconds of a recording plus the markers from the same period.</summary>
public class SnapshotChunkDataTransferObject
{
    public Guid flngKey { get; set; } = Guid.NewGuid();
    public Guid flngSessionKey { get; set; }
    public int flngSequence { get; set; }
    public int flngEventCount { get; set; }
    public long flngFirstEventMs { get; set; }
    public long flngLastEventMs { get; set; }
    public byte[] fbytData { get; set; } = [];
    public List<SnapshotMarkerDataTransferObject> flstMarkers { get; set; } = [];

    /// <summary>The signed-in user; the stored procedure only accepts the chunk when this person owns the recording.</summary>
    public string? fstrWho { get; set; }
}
