using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using userinterface.ViewModels.Profile;

namespace userinterface.Services
{
    public interface IProfileAnimationService
    {
        /// <summary>
        /// Animates adding a new profile item at the specified index
        /// </summary>
        Task AnimateAddAsync(ProfileListElementViewModel item, int index);
        
        /// <summary>
        /// Animates removing a profile item
        /// </summary>
        Task AnimateRemoveAsync(ProfileListElementViewModel item);
        
        /// <summary>
        /// Animates moving a profile item from one index to another
        /// </summary>
        Task AnimateMoveAsync(ProfileListElementViewModel item, int fromIndex, int toIndex);
        
        /// <summary>
        /// Animates multiple items to new positions simultaneously
        /// </summary>
        Task AnimateMultipleAsync(Dictionary<ProfileListElementViewModel, int> itemIndexPairs);
        
        /// <summary>
        /// Sets whether animations are enabled globally
        /// </summary>
        void SetAnimationEnabled(bool enabled);
        
        /// <summary>
        /// Gets whether animations are currently enabled
        /// </summary>
        bool IsAnimationEnabled { get; }
        
        /// <summary>
        /// Registers a control that can be animated
        /// </summary>
        void RegisterAnimatedControl(object control);
        
        /// <summary>
        /// Unregisters a control
        /// </summary>
        void UnregisterAnimatedControl(object control);
    }
}