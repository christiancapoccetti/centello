namespace Centello.Application.Tests;

public sealed class PanelFaces_should_expected_behavior
{
    private const string ListeningLead = "Dì cosa hai speso, per esempio “ieri 12 euro di pizza”.";

    [Fact]
    public void Wait_for_the_microphone_while_a_voice_panel_starts()
    {
        var face = PanelFaces.Of(DraftSource.Voice, new PanelState.Starting());

        Assert.Equal(
            new PanelFace.Recording("Ti ascolto…", ListeningLead, "0:00", "0:20", 0, "Tocca per inviare", MicStage.Waiting),
            face);
    }

    [Fact]
    public void Show_how_much_was_heard_while_listening()
    {
        var face = PanelFaces.Of(DraftSource.Voice, new PanelState.Listening(TimeSpan.FromSeconds(5)));

        Assert.Equal(
            new PanelFace.Recording("Ti ascolto…", ListeningLead, "0:05", "0:20", 0.25, "Tocca per inviare", MicStage.Listening),
            face);
    }

    [Fact]
    public void Show_the_minutes_in_the_clock()
    {
        var face = (PanelFace.Recording)PanelFaces.Of(DraftSource.Voice, new PanelState.Listening(TimeSpan.FromSeconds(75)));

        Assert.Equal("1:15", face.Elapsed);
        Assert.Equal(1, face.Fill);
    }

    [Fact]
    public void Show_the_sending_microphone_while_preparing()
    {
        var face = PanelFaces.Of(DraftSource.Voice, new PanelState.Preparing(TimeSpan.FromSeconds(10)));

        Assert.Equal(
            new PanelFace.Recording("Preparo la bozza…", "Poi la controlli prima di salvarla.", "0:10", "0:20", 0.5, "Invio in corso", MicStage.Sending),
            face);
    }

    [Fact]
    public void Ask_for_the_key_in_the_settings_on_the_voice_panel()
    {
        var face = PanelFaces.Of(DraftSource.Voice, new PanelState.NeedsConsent());

        Assert.Equal(
            new PanelFace.Message(
                "Inserimento vocale",
                DraftErrors.ConsentRequired.Message,
                PanelAction.OpenSettings),
            face);
    }

    [Fact]
    public void Ask_for_the_key_in_the_settings_on_the_photo_panel()
    {
        var face = PanelFaces.Of(DraftSource.Photo, new PanelState.NeedsConsent());

        Assert.Equal(
            new PanelFace.Message(
                "Lettura della foto",
                DraftErrors.ConsentRequired.Message,
                PanelAction.OpenSettings),
            face);
    }

    [Fact]
    public void Ask_for_the_key_in_the_settings_on_the_pdf_panel()
    {
        var face = PanelFaces.Of(DraftSource.Pdf, new PanelState.NeedsConsent());

        Assert.Equal(
            new PanelFace.Message(
                "Lettura del PDF",
                DraftErrors.ConsentRequired.Message,
                PanelAction.OpenSettings),
            face);
    }

    [Fact]
    public void Offer_a_retry_with_the_error_message_when_voice_failed()
    {
        var face = PanelFaces.Of(DraftSource.Voice, new PanelState.Failed(SpeechErrors.StartFailed));

        Assert.Equal(new PanelFace.Message("Inserimento vocale", SpeechErrors.StartFailed.Message, PanelAction.Retry), face);
    }

    [Fact]
    public void Offer_a_retry_with_the_error_message_when_the_photo_failed()
    {
        var face = PanelFaces.Of(DraftSource.Photo, new PanelState.Failed(PhotoErrors.CameraDenied));

        Assert.Equal(new PanelFace.Message("Lettura della foto", PhotoErrors.CameraDenied.Message, PanelAction.Retry), face);
    }

    [Fact]
    public void Read_the_photo_with_the_privacy_note()
    {
        var face = PanelFaces.Of(DraftSource.Photo, new PanelState.Reading());

        Assert.Equal(
            new PanelFace.Reading(
                "Lettura della foto",
                "Cerco importo, data e negozio…",
                MaterialSymbols.ReceiptLong,
                "Foto inviata a Google per preparare la bozza"),
            face);
    }

    [Fact]
    public void Read_the_pdf_with_the_privacy_note()
    {
        var face = PanelFaces.Of(DraftSource.Pdf, new PanelState.Reading());

        Assert.Equal(
            new PanelFace.Reading(
                "Lettura del PDF",
                "Cerco importo, data e negozio…",
                MaterialSymbols.ReceiptLong,
                "PDF inviato a Google per preparare la bozza"),
            face);
    }

    [Fact]
    public void Hide_the_photo_panel_before_the_shot()
    {
        Assert.Equal(new PanelFace.Hidden(), PanelFaces.Of(DraftSource.Photo, new PanelState.Starting()));
        Assert.Equal(new PanelFace.Hidden(), PanelFaces.Of(DraftSource.Photo, new PanelState.TakingPhoto()));
    }

    [Fact]
    public void Hide_the_voice_panel_while_taking_a_photo()
    {
        Assert.Equal(new PanelFace.Hidden(), PanelFaces.Of(DraftSource.Voice, new PanelState.TakingPhoto()));
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void Hide_the_panel_when_it_is_leaving(DraftSource source)
    {
        var draft = new ExpenseDraft(100, null, null, null, "", source, "");

        Assert.Equal(new PanelFace.Hidden(), PanelFaces.Of(source, new PanelState.Closed()));
        Assert.Equal(new PanelFace.Hidden(), PanelFaces.Of(source, new PanelState.DraftReady(draft)));
    }

    public static TheoryData<DraftSource> Sources => new() { DraftSource.Voice, DraftSource.Photo, DraftSource.Pdf };
}
