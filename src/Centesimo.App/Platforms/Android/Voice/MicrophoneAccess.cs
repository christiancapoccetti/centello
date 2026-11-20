using Centesimo.Application;

namespace Centesimo.App;

/// <summary>The microphone for the voice panel: the explanation the first time, then the system question. A person
/// who already granted it is never asked again.</summary>
internal sealed class MicrophoneAccess(IConfirmation confirmation) : IMicrophoneAccess
{
    private const string Rationale = "Centesimo usa il microfono solo mentre il pannello è aperto. " +
        "L'audio registrato viene inviato a OpenAI per la trascrizione.";

    public async Task<bool> Ensure()
    {
        if (await Permissions.CheckStatusAsync<Permissions.Microphone>() == PermissionStatus.Granted)
            return true;

        if (!await confirmation.Ask("Microfono", Rationale, "Consenti", "Non ora"))
            return false;

        return await Permissions.RequestAsync<Permissions.Microphone>() == PermissionStatus.Granted;
    }
}
