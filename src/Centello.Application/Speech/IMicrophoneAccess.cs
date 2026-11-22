namespace Centello.Application;

/// <summary>Permission to record, asked for with the panel's explanation the first time. True when granted.</summary>
public interface IMicrophoneAccess
{
    Task<bool> Ensure();
}
