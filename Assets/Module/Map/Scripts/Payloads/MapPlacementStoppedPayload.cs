namespace Core.Module.Map
{
    public readonly struct MapPlacementStoppedPayload
    {
        /// True when the player ended the placement themselves. A stop the game performed on their
        /// behalf leaves the menu closed, or it would reopen over the cell the next step points at.
        public readonly bool ReopenPicker;

        public MapPlacementStoppedPayload(bool reopenPicker) => ReopenPicker = reopenPicker;
    }
}
