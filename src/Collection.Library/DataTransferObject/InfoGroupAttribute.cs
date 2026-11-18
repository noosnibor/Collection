namespace Collection.Library.DataTransferObject;

/// <summary>
/// Puts a property into a named section of the review panel (InfoCard) in the web project.
/// Properties without it are shown in a single default section.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class InfoGroupAttribute(string name) : Attribute
{
    /// <summary>Section heading, e.g. "Member" or "Collection".</summary>
    public string Name { get; } = name;
}
