using System;

namespace InventoryKamera
{
    /// <summary>
    /// Static validation provider for game data lookups.
    /// The WinForms project sets these delegates at startup so Core models
    /// can validate against live reference data without a circular dependency.
    /// </summary>
    public static class ModelValidator
    {
        public static Func<string, bool> IsValidWeapon { get; set; } = _ => true;
        public static Func<string, bool> IsValidCharacter { get; set; } = _ => true;
        public static Func<string, bool> IsValidElement { get; set; } = _ => true;
        public static Func<string, bool> IsValidSlot { get; set; } = _ => true;
        public static Func<string, bool> IsValidSetName { get; set; } = _ => true;
        public static Func<string, bool> IsValidStat { get; set; } = _ => true;
    }
}
