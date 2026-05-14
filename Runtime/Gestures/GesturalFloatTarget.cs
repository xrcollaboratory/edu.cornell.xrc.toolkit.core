using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Abstract base for components that provide a float value to <see cref="GesturalFloatLogic"/>.
    /// Implement <see cref="GetValue"/> and <see cref="SetValue"/> to bridge the gestural command
    /// to any float-valued property without coupling the gesture logic to the target component.
    /// </summary>
    public abstract class GesturalFloatTarget : MonoBehaviour
    {
        /// <summary>
        /// Returns the current float value from the target component.
        /// </summary>
        /// <returns>The current value.</returns>
        public abstract float GetValue();

        /// <summary>
        /// Sets the float value on the target component.
        /// </summary>
        /// <param name="value">The new value to apply.</param>
        public abstract void SetValue(float value);
    }
}
