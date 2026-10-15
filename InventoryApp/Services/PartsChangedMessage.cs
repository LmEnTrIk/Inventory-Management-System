namespace InventoryApp.Services;

/// <summary>Broadcast after any change to parts or stock so lists can reload.</summary>
public sealed record PartsChangedMessage;
