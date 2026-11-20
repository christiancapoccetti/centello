# Third-party notices

The notices distributed with the Android app are in `src/Centesimo.App/Resources/Raw/` and are shown on the `Licenze` screen:

- `THIRD_PARTY_NOTICES.txt`: one entry per bundled library and font, followed by the full text of every license they name (MIT, BSD 2-Clause, Apache 2.0, SIL OFL 1.1). Each license heading is framed by rules of dashes, which is how the app tells it apart from the license text.
- `DOTNET_RUNTIME_THIRD_PARTY_NOTICES.txt` and `DOTNET_ANDROID_THIRD_PARTY_NOTICES.txt`: verbatim copies of the `THIRD-PARTY-NOTICES.TXT` files of the `Microsoft.NETCore.App.Runtime.Mono.android-arm64` and `Microsoft.Android.Runtime.Mono.36.android-arm64` packs, covering what the runtimes compile in.

Review and update the notices whenever dependencies, assets or the .NET SDK change. The packages actually bundled are listed in `src/Centesimo.App/obj/project.assets.json` after a restore.
