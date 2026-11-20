namespace Centesimo.Application;

public static class PhotoErrors
{
    public static readonly Error Unsupported = new("Photo.Unsupported", "Questa immagine non si può leggere. Prova con una foto JPEG o PNG.");
    public static readonly Error TooLarge = new("Photo.TooLarge", "La foto è troppo grande. Riprova con un'altra foto.");
    public static readonly Error PdfTooLarge = new("Photo.PdfTooLarge", "Il PDF è troppo grande: usa un file sotto i 5 MB.");
    public static readonly Error CameraDenied = new("Photo.CameraDenied", "Consenti l'accesso alla fotocamera per fotografare una spesa.");
    public static readonly Error CameraUnavailable = new("Photo.CameraUnavailable", "Non trovo una fotocamera su questo telefono. Prova con la galleria.");
    public static readonly Error Unavailable = new("Photo.Unavailable", "Non riesco ad aprire la fotocamera o la galleria.");
    public static readonly Error ProcessingFailed = new("Photo.ProcessingFailed", "Non riesco a leggere la foto.");
}
