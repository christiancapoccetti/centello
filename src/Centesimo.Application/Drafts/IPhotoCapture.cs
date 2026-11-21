namespace Centesimo.Application;

/// <summary>The camera of the photo panel: the permission, then the shot.</summary>
public interface IPhotoCapture
{
    /// <summary>True when the camera may open: permission already there, or granted after asking.</summary>
    Task<bool> EnsureAccess();

    /// <returns>The photo, or null when the person came back without taking one.</returns>
    Task<Result<byte[]?>> Take();
}
