namespace Collection.Library.Model;

// ---------------------------------------------------------------------------------------------
// Snapshot (session replay) read models. Property names match the stored procedure columns,
// which is how Dapper maps them (same convention as LocationModel).
// ---------------------------------------------------------------------------------------------

/// <summary>One row of the "Users" list: a person who was recorded in the chosen period.</summary>
public class SnapshotUserModel
{
    public string fstrUsername { get; set; } = string.Empty;
    public string fstrFullName { get; set; } = string.Empty;
    public string fstrLocationKey { get; set; } = string.Empty;
    public int flngSessionCount { get; set; }

    /// <summary>UTC.</summary>
    public DateTime fdtmLastSeen { get; set; }
}

/// <summary>One row of the "Visits" list for a user.</summary>
public class SnapshotSessionModel
{
    public Guid flngKey { get; set; }

    /// <summary>UTC.</summary>
    public DateTime fdtmStart { get; set; }
    public long flngDurationMs { get; set; }
    public string fstrEntryUrl { get; set; } = string.Empty;
    public int flngMarkerCount { get; set; }
    public string fstrEnvironment { get; set; } = string.Empty;
    public int flngViewportW { get; set; }
    public int flngViewportH { get; set; }
}

/// <summary>One thing that happened during a visit (a click, a page change, an error).</summary>
public class SnapshotMarkerModel
{
    public long flngTimestampMs { get; set; }

    /// <summary>click | navigate | error</summary>
    public string fstrKind { get; set; } = "click";
    public string fstrLabel { get; set; } = string.Empty;
    public string? fstrUrl { get; set; }

    /// <summary>Milliseconds from the start of the recording (used to jump the player).</summary>
    public long flngOffsetMs { get; set; }
}

/// <summary>A few seconds of recording: gzip-compressed JSON of rrweb events.</summary>
public class SnapshotChunkModel
{
    public Guid flngKey { get; set; }
    public Guid flngSessionKey { get; set; }
    public int flngSequence { get; set; }
    public int flngEventCount { get; set; }
    public long flngFirstEventMs { get; set; }
    public long flngLastEventMs { get; set; }
    public byte[] fbytData { get; set; } = [];
}
