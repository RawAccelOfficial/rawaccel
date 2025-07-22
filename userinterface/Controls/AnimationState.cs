namespace userinterface.Controls
{
    /// <summary>
    /// Simplified animation states - reduced from 6 to 3 for better maintainability
    /// </summary>
    public enum AnimationState
    {
        /// <summary>
        /// No animation in progress, item is in final position
        /// </summary>
        Idle,
        
        /// <summary>
        /// Any animation in progress (enter, move, or exit)
        /// </summary>
        Animating,
        
        /// <summary>
        /// Item is marked for removal and should not respond to new animations
        /// </summary>
        PendingRemoval
    }
}