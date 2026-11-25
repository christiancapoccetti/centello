using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Android.Security.Keystore;
using Centello.Application;
using Java.Security;
using Javax.Crypto;

namespace Centello.App;

internal sealed class AndroidAiAccessStore : IAiAccessStore
{
    private const string Provider = "AndroidKeyStore";
    private const string Alias = "centello-ai-access-v1";
    private const string InitializedAlias = "centello-ai-access-initialized-v1";
    private const string Algorithm = "HmacSHA256";
    private const int HeaderLength = sizeof(byte) + sizeof(int);
    private const int TagLength = 32;
    private const int MaximumInvitationBytes = 16_384;
    private const byte FormatVersion = 1;
    private readonly object _gate = new();

    private static string StatePath => Path.Combine(
        global::Android.App.Application.Context.NoBackupFilesDir?.AbsolutePath ??
        throw new IOException("No private AI access directory is available."),
        "ai-access-v1");

    public string InstallationId
    {
        get
        {
            lock (_gate)
            {
                using var key = LoadKey(StatePath);
                var digest = Sign(key, "centello-ai-installation-id-v1"u8.ToArray());
                return Convert.ToHexStringLower(digest.AsSpan(0, 16));
            }
        }
    }

    public AiAccessState Read()
    {
        lock (_gate)
        {
            var path = StatePath;
            using var key = LoadKey(path);
            return ReadState(path, key);
        }
    }

    public void Write(AiAccessState state)
    {
        lock (_gate)
        {
            var path = StatePath;
            using var key = LoadKey(path);
            ReadState(path, key);
            WriteState(path, key, state);
        }
    }

    private static IKey LoadKey(string path)
    {
        try
        {
            using var store = KeyStore.GetInstance(Provider) ?? throw new IOException("Android Keystore is unavailable.");
            store.Load(null, null);
            var initialized = store.ContainsAlias(InitializedAlias);
            var key = store.GetKey(Alias, null);
            if (key is null)
            {
                if (initialized || File.Exists(path) || File.Exists(path + ".new") || store.ContainsAlias(Alias))
                    throw new IOException("The Android Keystore key for AI access is missing.");

                key = GenerateKey(Alias);
            }

            if (initialized)
                return key;

            try
            {
                // Mark full initialization in Keystore before handing the caller any allowance.
                // An interrupted first write can then resume, while deleting established files cannot reset the trial.
                if (File.Exists(path))
                    ReadState(path, key);
                else
                    WriteState(path, key, new AiAccessState(0, ""));

                using var marker = GenerateKey(InitializedAlias);
                return key;
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }
        catch (Java.Lang.Exception exception)
        {
            throw new IOException("The Android Keystore could not load the AI access key.", exception);
        }
    }

    private static IKey GenerateKey(string alias)
    {
        using var generator = KeyGenerator.GetInstance(Algorithm, Provider) ?? throw new IOException("The Android HMAC generator is unavailable.");
        using var spec = new KeyGenParameterSpec.Builder(alias, KeyStorePurpose.Sign)
            .SetKeySize(TagLength * 8)
            .Build();
        generator.Init(spec);
        return generator.GenerateKey() ?? throw new IOException("Android Keystore did not create a key.");
    }

    private static AiAccessState ReadState(string path, IKey key)
    {
        using var source = File.OpenRead(path);
        if (source.Length is < HeaderLength + TagLength or > HeaderLength + TagLength + MaximumInvitationBytes)
            throw new IOException("The AI access state has an invalid size.");

        var record = new byte[(int)source.Length];
        source.ReadExactly(record);
        var payload = record.AsSpan(0, record.Length - TagLength);
        var tag = SignState(key, payload);
        if (!CryptographicOperations.FixedTimeEquals(tag, record.AsSpan(payload.Length)) || payload[0] != FormatVersion)
            throw new IOException("The AI access state failed authentication.");

        var used = BinaryPrimitives.ReadInt32LittleEndian(payload[1..]);
        if (used < 0)
            throw new IOException("The AI access counter is invalid.");

        return new AiAccessState(used, Encoding.UTF8.GetString(payload[HeaderLength..]));
    }

    private static void WriteState(string path, IKey key, AiAccessState state)
    {
        var invitation = Encoding.UTF8.GetBytes(state.Invitation);
        if (state.Used < 0 || invitation.Length > MaximumInvitationBytes)
            throw new IOException("The AI access state is too large or its counter is invalid.");

        var payload = new byte[HeaderLength + invitation.Length];
        payload[0] = FormatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(1), state.Used);
        invitation.CopyTo(payload, HeaderLength);
        var tag = SignState(key, payload);

        var stagingPath = path + ".new";
        using (var destination = File.Create(stagingPath))
        {
            destination.Write(payload);
            destination.Write(tag);
            destination.Flush(flushToDisk: true);
        }

        File.Move(stagingPath, path, overwrite: true);
    }

    private static byte[] SignState(IKey key, ReadOnlySpan<byte> payload) =>
        Sign(key, [.. "centello-ai-access-state-v1\0"u8, .. payload]);

    private static byte[] Sign(IKey key, byte[] bytes)
    {
        try
        {
            using var mac = Mac.GetInstance(Algorithm) ?? throw new IOException("The Android HMAC provider is unavailable.");
            mac.Init(key);
            return mac.DoFinal(bytes) ?? throw new IOException("Android Keystore did not return an authentication tag.");
        }
        catch (Java.Lang.Exception exception)
        {
            throw new IOException("The Android Keystore could not authenticate AI access data.", exception);
        }
    }
}
