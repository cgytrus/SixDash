namespace SixDash.Patches;

/// <summary>
/// Represents a patch that can be applied by <see cref="Util.ApplyAllPatches"/>.
/// </summary>
public interface IPatch {
    /// <summary>
    /// Called when the patch is applied.
    /// </summary>
    void Apply();
}
