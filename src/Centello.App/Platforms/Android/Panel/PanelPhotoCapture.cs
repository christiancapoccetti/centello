using Centello.Application;
using Centello.Domain;

namespace Centello.App;

internal sealed class PanelPhotoCapture(PhotoPicker photos, IConfirmation confirmation) : IPhotoCapture
{
    public async Task<bool> EnsureAccess()
    {
        if (!PhotoPicker.CanCapture)
            return true;

        if (await Permissions.CheckStatusAsync<Permissions.Camera>() == PermissionStatus.Granted)
            return true;

        if (!await confirmation.Ask("Fotocamera", PhotoPicker.CameraRationale, "Consenti", "Non ora"))
            return false;

        return await Permissions.RequestAsync<Permissions.Camera>() == PermissionStatus.Granted;
    }

    public Task<Result<byte[]?>> Take() => PhotoPicker.CanCapture ? photos.Capture() : photos.Pick();
}
