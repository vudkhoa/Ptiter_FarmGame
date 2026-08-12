namespace Core.Module.Map
{
    public readonly struct MapPlacementStoppedPayload
    {
        /// <summary>
        /// True when the player ended the placement themselves, which is when the build menu is
        /// expected to come back. A stop the game performed on their behalf - the tutorial taking
        /// the brush away after one plot - leaves the menu closed, or it would reopen over the
        /// very cell the next step tells them to tap.
        /// </summary>
        public readonly bool ReopenPicker;

        public MapPlacementStoppedPayload(bool reopenPicker) => ReopenPicker = reopenPicker;
    }
}
