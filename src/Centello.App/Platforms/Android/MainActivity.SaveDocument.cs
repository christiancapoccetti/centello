using Android.Content;
using Centello.Application;

namespace Centello.App;

public partial class MainActivity
{
    private const int SaveDocumentRequest = 0x4345;
    private TaskCompletionSource<Android.Net.Uri?>? _saveLocation;

    internal async Task<Android.Net.Uri?> ChooseSaveLocation(ExportedFile file)
    {
        if (_saveLocation is not null)
            return null;

        var completion = new TaskCompletionSource<Android.Net.Uri?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _saveLocation = completion;
        try
        {
            using var intent = new Intent(Intent.ActionCreateDocument);
            intent.SetType(file.ContentType);
            intent.AddCategory(Intent.CategoryOpenable!);
            intent.PutExtra(Intent.ExtraTitle, Path.GetFileName(file.FilePath));
            StartActivityForResult(intent, SaveDocumentRequest);
            return await completion.Task;
        }
        finally
        {
            _saveLocation = null;
        }
    }

    protected override void OnActivityResult(int requestCode, Android.App.Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == SaveDocumentRequest)
            _saveLocation?.TrySetResult(resultCode == Android.App.Result.Ok ? data?.Data : null);
    }
}
