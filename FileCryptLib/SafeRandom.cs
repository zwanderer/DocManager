using System.Security.Cryptography;

namespace FileCryptLib;

/// <summary>
/// Utility class to safely generate cryptographically sound random numbers.
/// </summary>
public static class SafeRandom
{
    private const int POOL_SIZE = 4096;
    private static readonly Lazy<RandomNumberGenerator> RNG = new(RandomNumberGenerator.Create());
    private static readonly object LOCK = new();
    private static readonly Lazy<byte[]> POOL = new(() => GeneratePool(new byte[POOL_SIZE]));
    private static int _position;

    /// <inheritdoc cref="Random.Next()"/>
    public static int GetNext()
    {
        while (true)
        {
            int result = (int)(GetRandomUInt32() & int.MaxValue);

            if (result != int.MaxValue)
                return result;
        }
    }

    /// <inheritdoc cref="Random.Next(int)"/>
    public static int GetNext(int maxValue)
    {
        return maxValue < 1
            ? throw new ArgumentException("Must be greater than zero.", nameof(maxValue))
            : GetNext(0, maxValue);
    }

    /// <inheritdoc cref="Random.Next(int, int)"/>
    public static int GetNext(int minValue, int maxValue)
    {
        const long MAX = 1 + (long)uint.MaxValue;

        if (minValue >= maxValue)
            throw new ArgumentException($"{nameof(minValue)} is greater than or equal to {nameof(maxValue)}");

        long diff = maxValue - minValue;
        long limit = MAX - (MAX % diff);

        while (true)
        {
            uint rand = GetRandomUInt32();
            if (rand < limit)
                return (int)(minValue + (rand % diff));
        }
    }

    /// <inheritdoc cref="Random.NextBytes(byte[])"/>
    public static void GetNextBytes(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Length < POOL_SIZE)
        {
            lock (LOCK)
            {
                if (POOL_SIZE - _position < buffer.Length)
                    GeneratePool(POOL.Value);

                Buffer.BlockCopy(POOL.Value, _position, buffer, 0, buffer.Length);
                _position += buffer.Length;
            }
        }
        else
        {
            RNG.Value.GetBytes(buffer);
        }
    }

    /// <inheritdoc cref="Random.NextDouble()"/>
    public static double GetNextDouble() => GetRandomUInt32() / (1.0 + uint.MaxValue);

    private static byte[] GeneratePool(byte[] buffer)
    {
        _position = 0;
        RNG.Value.GetBytes(buffer);
        return buffer;
    }

    private static uint GetRandomUInt32()
    {
        uint result;
        lock (LOCK)
        {
            if (POOL_SIZE - _position < sizeof(uint))
                GeneratePool(POOL.Value);

            result = BitConverter.ToUInt32(POOL.Value, _position);
            _position += sizeof(uint);
        }

        return result;
    }
}
