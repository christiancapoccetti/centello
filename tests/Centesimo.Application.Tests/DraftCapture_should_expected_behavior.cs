namespace Centesimo.Application.Tests;

public sealed class DraftCapture_should_expected_behavior
{
    private static readonly Error Failure = new("Test.Failure", "Non riuscito.");

    private static DraftCapture Listening => DraftCapture.Idle.Listen();
    private static DraftCapture ReadingVoice => Listening.Read(DraftSource.Voice);
    private static DraftCapture Choosing => DraftCapture.Idle.ChoosePhoto();
    private static DraftCapture Picking => Choosing.Pick(DraftSource.Photo);
    private static DraftCapture PickingPdf => Choosing.Pick(DraftSource.Pdf);
    private static DraftCapture ReadingPdf => PickingPdf.Read(DraftSource.Pdf);
    private static DraftCapture ReadingPhoto => Picking.Read(DraftSource.Photo);
    private static DraftCapture Failed => ReadingVoice.Fail(Failure, "cento euro");

    [Fact]
    public void Start_idle_with_nothing_to_show()
    {
        var capture = DraftCapture.Idle;

        Assert.True(capture.IsIdle);
        Assert.Null(capture.Source);
        Assert.False(capture.IsSheetVisible);
        Assert.False(capture.IsListening);
        Assert.False(capture.IsChoosingPhoto);
        Assert.False(capture.HasError);
        Assert.False(capture.HasHeard);
        Assert.Equal("", capture.ErrorMessage);
    }

    [Fact]
    public void Listen_for_the_voice_without_opening_the_sheet()
    {
        var capture = Listening;

        Assert.True(capture.IsListening);
        Assert.Same(DraftSource.Voice, capture.Source);
        Assert.False(capture.IsIdle);
        Assert.False(capture.IsSheetVisible);
    }

    [Fact]
    public void Read_the_voice_on_the_sheet_with_no_reading_line()
    {
        var capture = ReadingVoice;

        Assert.True(capture.IsReading);
        Assert.True(capture.IsSheetVisible);
        Assert.False(capture.IsListening);
        Assert.Same(DraftSource.Voice, capture.Source);
        Assert.False(capture.HasReadingLine);
    }

    [Fact]
    public void Read_a_photo_on_the_sheet_with_its_reading_line()
    {
        var capture = ReadingPhoto;

        Assert.True(capture.IsReading);
        Assert.True(capture.IsSheetVisible);
        Assert.Same(DraftSource.Photo, capture.Source);
        Assert.True(capture.HasReadingLine);
    }

    [Fact]
    public void Read_a_pdf_on_the_sheet_with_the_pdf_source()
    {
        var capture = ReadingPdf;

        Assert.True(capture.IsReading);
        Assert.True(capture.IsSheetVisible);
        Assert.Same(DraftSource.Pdf, capture.Source);
        Assert.True(capture.HasReadingLine);
    }

    [Fact]
    public void Open_the_sheet_for_a_voice_start_that_failed_while_idle()
    {
        var capture = DraftCapture.Idle.Read(DraftSource.Voice).Fail(Failure);

        Assert.True(capture.HasError);
        Assert.True(capture.IsSheetVisible);
        Assert.Equal("Non riuscito.", capture.ErrorMessage);
    }

    [Fact]
    public void Keep_the_error_and_what_was_heard_until_closed()
    {
        var capture = Failed;

        Assert.True(capture.HasError);
        Assert.True(capture.IsSheetVisible);
        Assert.False(capture.IsReading);
        Assert.Equal("Non riuscito.", capture.ErrorMessage);
        Assert.Equal("cento euro", capture.Heard);
        Assert.True(capture.HasHeard);
        Assert.Same(DraftSource.Voice, capture.Source);
    }

    [Fact]
    public void Show_no_heard_phrase_when_the_failure_names_none()
    {
        var capture = ReadingPhoto.Fail(Failure);

        Assert.Equal("", capture.Heard);
        Assert.False(capture.HasHeard);
    }

    [Fact]
    public void Finish_a_reading_back_to_idle()
    {
        Assert.Equal(DraftCapture.Idle, ReadingVoice.Finish());
        Assert.Equal(DraftCapture.Idle, ReadingPhoto.Finish());
    }

    [Fact]
    public void Close_the_error_back_to_idle_and_forget_it()
    {
        var capture = Failed.Close();

        Assert.Equal(DraftCapture.Idle, capture);
        Assert.Equal("", capture.ErrorMessage);
        Assert.Equal("", capture.Heard);
    }

    [Fact]
    public void Close_the_menu_and_a_recording_back_to_idle()
    {
        Assert.Equal(DraftCapture.Idle, Choosing.Close());
        Assert.Equal(DraftCapture.Idle, Picking.Close());
        Assert.Equal(DraftCapture.Idle, Listening.Close());
    }

    [Fact]
    public void Keep_a_reading_sheet_open_when_closed()
    {
        var voice = ReadingVoice;
        var photo = ReadingPhoto;

        Assert.Same(voice, voice.Close());
        Assert.Same(photo, photo.Close());
        Assert.True(photo.Close().IsReading);
    }

    [Fact]
    public void Choose_a_photo_from_idle_for_the_photo_source()
    {
        var capture = Choosing;

        Assert.True(capture.IsChoosingPhoto);
        Assert.False(capture.IsIdle);
        Assert.False(capture.IsSheetVisible);
        Assert.Same(DraftSource.Photo, capture.Source);
    }

    [Fact]
    public void Pick_a_photo_once_from_the_menu()
    {
        var picking = Choosing.Pick(DraftSource.Photo);

        Assert.False(picking.IsChoosingPhoto);
        Assert.False(picking.IsIdle);
        Assert.False(picking.IsSheetVisible);
        Assert.Same(picking, picking.Pick(DraftSource.Photo));
    }

    [Fact]
    public void Start_nothing_while_another_capture_is_under_way()
    {
        var busy = new[] { Listening, ReadingVoice, Failed, Choosing, Picking, ReadingPhoto };

        Assert.All(busy, capture =>
        {
            Assert.Same(capture, capture.Listen());
            Assert.Same(capture, capture.ChoosePhoto());
        });
    }

    [Fact]
    public void Ignore_a_transition_that_does_not_apply_to_the_phase()
    {
        var idle = DraftCapture.Idle;

        Assert.Same(idle, idle.Pick(DraftSource.Photo));
        Assert.Same(idle, idle.Finish());
        Assert.Same(idle, idle.Fail(Failure));
        Assert.Same(idle, idle.Close());

        var choosing = Choosing;
        Assert.Same(choosing, choosing.Read(DraftSource.Photo));
        Assert.Same(choosing, choosing.Finish());

        var listening = Listening;
        Assert.Same(listening, listening.Finish());
        Assert.Same(listening, listening.Fail(Failure));
        Assert.Same(listening, listening.Pick(DraftSource.Photo));
    }

    [Fact]
    public void Ignore_a_late_event_after_the_reading_ended()
    {
        var failed = Failed;

        Assert.Same(failed, failed.Fail(Failure, "altro"));
        Assert.Same(failed, failed.Finish());
        Assert.Same(failed, failed.Read(DraftSource.Voice));
    }

    [Fact]
    public void Not_read_a_recording_as_a_photo_or_the_reverse()
    {
        var listening = Listening;
        var picking = Picking;

        Assert.Same(listening, listening.Read(DraftSource.Photo));
        Assert.Same(picking, picking.Read(DraftSource.Voice));
        Assert.Same(picking, picking.Read(DraftSource.Pdf));
        var pickingPdf = PickingPdf;
        Assert.Same(pickingPdf, pickingPdf.Read(DraftSource.Photo));
    }

    [Fact]
    public void Keep_the_reading_when_read_again()
    {
        var reading = ReadingVoice;

        Assert.Same(reading, reading.Read(DraftSource.Voice));
        Assert.Same(reading, reading.Read(DraftSource.Photo));
    }
}
