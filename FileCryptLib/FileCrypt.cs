// Ignore Spelling: PWD

using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace FileCryptLib;

/// <summary>
/// Utility class to encrypt and decrypt streams using AES-GCM.
/// </summary>
public static class FileCrypt
{
    public const string DEFAULT_KEY = "AYAYAYA!";
    public static readonly byte[] SALT_DATA = Encoding.UTF8.GetBytes("davidbonafewashere!");
    public const int KEY_SIZE = 32;
    public const int CHUNK_SIZE = 32 * 1024;
    public const int PWD_ITERATION_COUNT = 4096;

    /// <summary>
    /// Encrypts the content of <paramref name="input"/> stream from current position until the end and saves into <paramref name="output"/> stream.
    /// </summary>
    /// <param name="input">Input stream</param>
    /// <param name="output">Output stream</param>
    /// <param name="key">Key to use during encryption</param>
    /// <param name="ct">Cancellation Token</param>
    public static async ValueTask Encrypt(Stream input, Stream output, string key = DEFAULT_KEY, CancellationToken ct = default)
    {
        var pool = ArrayPool<byte>.Shared;
        using var aesg = new AesGcm(GenerateKey(key), AesGcm.TagByteSizes.MaxSize);

        byte[] nonce = pool.Rent(AesGcm.NonceByteSizes.MaxSize);
        byte[] tag = pool.Rent(AesGcm.TagByteSizes.MaxSize);
        byte[] plainBuffer = pool.Rent(CHUNK_SIZE);
        byte[] cipherBuffer = pool.Rent(CHUNK_SIZE);
        try
        {
            SafeRandom.GetNextBytes(nonce);

            var nonceMem = nonce.AsMemory()[..AesGcm.NonceByteSizes.MaxSize];
            var tagMem = tag.AsMemory()[..AesGcm.TagByteSizes.MaxSize];
            var plainBufferMem = plainBuffer.AsMemory()[..CHUNK_SIZE];
            var cipherBufferMem = cipherBuffer.AsMemory()[..CHUNK_SIZE];

            await output.WriteAsync(nonceMem, ct);

            int bytesRead;
            while ((bytesRead = await input.ReadAsync(plainBufferMem, ct)) > 0)
            {
                aesg.Encrypt(nonceMem.Span, plainBufferMem[..bytesRead].Span, cipherBufferMem[..bytesRead].Span, tagMem.Span);
                await output.WriteAsync(tagMem, ct);
                await output.WriteAsync(cipherBufferMem[..bytesRead], ct);
            }
        }
        finally
        {
            pool.Return(nonce);
            pool.Return(tag);
            pool.Return(plainBuffer);
            pool.Return(cipherBuffer);
        }
    }

    /// <summary>
    /// Decrypts the content of <paramref name="input"/> stream from current position until the end and saves into <paramref name="output"/> stream.
    /// </summary>
    /// <param name="input">Input stream</param>
    /// <param name="output">Output stream</param>
    /// <param name="key">Key to use during decryption</param>
    /// <param name="ct">Cancellation Token</param>
    public static async ValueTask Decrypt(Stream input, Stream output, string key = DEFAULT_KEY, CancellationToken ct = default)
    {
        var pool = ArrayPool<byte>.Shared;
        using var aesg = new AesGcm(GenerateKey(key), AesGcm.TagByteSizes.MaxSize);

        byte[] nonce = pool.Rent(AesGcm.NonceByteSizes.MaxSize);
        byte[] tag = pool.Rent(AesGcm.TagByteSizes.MaxSize);
        byte[] plainBuffer = pool.Rent(CHUNK_SIZE);
        byte[] cipherBuffer = pool.Rent(CHUNK_SIZE);
        try
        {

            var nonceMem = nonce.AsMemory()[..AesGcm.NonceByteSizes.MaxSize];
            var tagMem = tag.AsMemory()[..AesGcm.TagByteSizes.MaxSize];
            var plainBufferMem = plainBuffer.AsMemory()[..CHUNK_SIZE];
            var cipherBufferMem = cipherBuffer.AsMemory()[..CHUNK_SIZE];

            if (await input.ReadAsync(nonceMem, ct) != AesGcm.NonceByteSizes.MaxSize)
                throw new InvalidOperationException("Could not read nonce!");

            int tagBytesRead;
            int bytesRead;

            while ((tagBytesRead = await input.ReadAsync(tagMem, ct)) > 0 &&
                   (bytesRead = await input.ReadAsync(cipherBufferMem, ct)) > 0)
            {
                if (tagBytesRead != AesGcm.TagByteSizes.MaxSize)
                    throw new InvalidOperationException("Could not read tag!");

                aesg.Decrypt(nonceMem.Span, cipherBufferMem[..bytesRead].Span, tagMem.Span, plainBufferMem[..bytesRead].Span);
                await output.WriteAsync(plainBufferMem[..bytesRead], ct);
            }

            if (input.Position < input.Length)
                throw new InvalidOperationException("Input was not read until the end!");
        }
        finally
        {
            pool.Return(nonce);
            pool.Return(tag);
            pool.Return(plainBuffer);
            pool.Return(cipherBuffer);
        }
    }

    private static ReadOnlySpan<byte> GenerateKey(string key)
    {
        using var deriv = new Rfc2898DeriveBytes(key, SALT_DATA, PWD_ITERATION_COUNT, HashAlgorithmName.SHA512);
        return deriv.GetBytes(KEY_SIZE);
    }
}
