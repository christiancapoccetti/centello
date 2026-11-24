using Centello.Application;

namespace Centello.App;

internal sealed class MicrophoneAccess(IConfirmation confirmation) : IMicrophoneAccess
{
    private const string Rationale = "Centello usa il microfono solo durante la registrazione. " +
        "L’audio registrato viene inviato a Google per preparare la bozza.";

    public async Task<bool> Ensure()
    {
        if (await Permissions.CheckStatusAsync<Permissions.Microphone>() == PermissionStatus.Granted)
            return true;

        if (!await confirmation.Ask("Microfono", Rationale, "Consenti", "Non ora"))
            return false;

        return await Permissions.RequestAsync<Permissions.Microphone>() == PermissionStatus.Granted;
    }
}
