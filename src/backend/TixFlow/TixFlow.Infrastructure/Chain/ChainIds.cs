using System.Numerics;

namespace TixFlow.Infrastructure.Chain;

/// <summary>Maps database identifiers to the values the contracts use, in both directions.</summary>
public static class ChainIds
{
    /// <summary>An order's on-chain nonce is its GUID read as a 128-bit big-endian integer.</summary>
    public static BigInteger OrderNonce(Guid orderId) =>
        new(orderId.ToByteArray(bigEndian: true), isUnsigned: true, isBigEndian: true);

    public static Guid? OrderIdFromNonce(BigInteger nonce)
    {
        if (nonce.Sign < 0) return null;
        var bytes = nonce.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length > 16) return null;

        Span<byte> padded = stackalloc byte[16];
        bytes.CopyTo(padded[(16 - bytes.Length)..]);
        return new Guid(padded, bigEndian: true);
    }

    /// <summary>An event's on-chain ID (the CREATE2 salt) is its GUID left-padded to 32 bytes.</summary>
    public static byte[] EventId(Guid eventId)
    {
        var result = new byte[32];
        eventId.ToByteArray(bigEndian: true).CopyTo(result, 16);
        return result;
    }

    public static Guid? EventIdFromBytes(byte[] eventId)
    {
        if (eventId.Length != 32 || eventId.AsSpan(0, 16).ContainsAnyExcept((byte)0)) return null;
        return new Guid(eventId.AsSpan(16, 16), bigEndian: true);
    }

    /// <summary>USDC uses 6 decimals.</summary>
    public static BigInteger UsdcUnits(decimal amount) => new(decimal.Round(amount * 1_000_000m));

    public static byte[] ToBytes32(long value)
    {
        var result = new byte[32];
        var bytes = new BigInteger(value).ToByteArray(isUnsigned: true, isBigEndian: true);
        bytes.CopyTo(result, 32 - bytes.Length);
        return result;
    }
}
