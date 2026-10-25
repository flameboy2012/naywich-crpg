namespace Crpg.Core.Selection;

/// <summary>
/// One selection by portrait slot (1 to 4), from a number key or a portrait click, and whether shift was held.
/// </summary>
public readonly record struct SlotSelectEvent(int Slot, bool Shift = false);
