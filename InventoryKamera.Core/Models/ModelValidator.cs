using System;
using System.Diagnostics;

namespace InventoryKamera
{
    /// <summary>
    /// Static validation provider for game data lookups.
    /// The WinForms project sets these delegates at startup so Core models
    /// can validate against live reference data without a circular dependency.
    /// </summary>
    public static class ModelValidator
    {
        /// <summary>
        /// Static validation delegates for model classes. These must be initialized by the WinForms
        /// application startup code (specifically, GenshinProcesor's static constructor) before any
        /// model IsValid() calls are made. The defaults return true so Core compiles without WinForms,
        /// but validation is silently disabled until wired up.
        ///
        /// Boot-order dependency: GenshinProcesor static constructor must execute before validation.
        /// This class is a temporary bridge that will be removed when validation logic is refactored
        /// to not depend on game data lookups (future milestone).
        /// </summary>
        public static bool IsWiredUp { get; private set; }

        public static void MarkWiredUp()
        {
            IsWiredUp = true;
        }

        [Conditional("DEBUG")]
        public static void AssertWired()
        {
            Debug.Assert(IsWiredUp, "ModelValidator has not been wired up — IsValid() calls on models return vacuous true. Call GenshinProcesor initialization before using model validation.");
        }

        public static Func<string, bool> IsValidWeapon { get; set; } = _ => true;
        public static Func<string, bool> IsValidCharacter { get; set; } = _ => true;
        public static Func<string, bool> IsValidElement { get; set; } = _ => true;
        public static Func<string, bool> IsValidSlot { get; set; } = _ => true;
        public static Func<string, bool> IsValidSetName { get; set; } = _ => true;
        public static Func<string, bool> IsValidStat { get; set; } = _ => true;
    }
}
