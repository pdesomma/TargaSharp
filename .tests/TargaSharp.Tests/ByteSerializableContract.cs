namespace TargaSharp.Tests;

/// <summary>
/// Shared assertions for the fixed-size "byte[] constructor + <c>ToBytes()</c>" contract repeated across
/// most TGA field types (<see cref="TargaSharp.TgaColorKey"/>, <see cref="TargaSharp.TgaHeader"/>, etc.):
/// a <see langword="null"/> array throws, a wrong-length array throws, and a round trip through
/// <c>ToBytes()</c> reproduces an equal instance. Centralizing the assertion here replaces the
/// near-identical <c>Ctor_NullBytes_Throws</c> / <c>Ctor_WrongLength_Throws</c> / <c>Ctor_RoundTrips</c>
/// triplet that used to be hand-written in every such type's test file.
/// </summary>
internal static class ByteSerializableContract
{
    /// <summary>
    /// Asserts the standard byte[] constructor contract for a fixed-size TGA field type.
    /// <para>A <see langword="null"/> array throws <see cref="ArgumentNullException"/>. An array one byte
    /// short of the valid length throws <see cref="ArgumentOutOfRangeException"/>. When
    /// <paramref name="minLength"/> is omitted, the type is assumed to require an exact length, so an
    /// array one byte too long is asserted to throw <see cref="ArgumentOutOfRangeException"/> as well;
    /// when supplied, <paramref name="minLength"/> is used as the lower bound instead of
    /// <paramref name="size"/> and the "too long" case is not asserted, since such types (e.g. a fixed
    /// header followed by variable trailing data) legitimately accept longer arrays.</para>
    /// <para>Finally, <paramref name="sample"/> is round-tripped through <paramref name="toBytes"/> and
    /// back through <paramref name="construct"/>: the produced instance must equal the original, and the
    /// serialized byte length must equal <paramref name="size"/>.</para>
    /// </summary>
    /// <typeparam name="T">The TGA field type under test.</typeparam>
    /// <param name="construct">The type's byte[] constructor, e.g. <c>bytes => new TgaColorKey(bytes)</c>.</param>
    /// <param name="toBytes">The type's <c>ToBytes()</c> method, e.g. <c>x => x.ToBytes()</c>.</param>
    /// <param name="size">The exact byte length <paramref name="sample"/> is expected to serialize to.</param>
    /// <param name="sample">Produces a representative, non-default instance to round-trip.</param>
    /// <param name="minLength">The minimum valid byte length, for types whose constructor enforces a
    /// lower bound rather than an exact length (variable-length trailing data). Omit for types that
    /// require an exact length.</param>
    internal static void AssertByteCtorContract<T>(Func<byte[], T> construct, Func<T, byte[]> toBytes, int size, Func<T> sample, int? minLength = null)
    {
        ArgumentNullException.ThrowIfNull(construct);
        ArgumentNullException.ThrowIfNull(toBytes);
        ArgumentNullException.ThrowIfNull(sample);

        Assert.ThrowsExactly<ArgumentNullException>(() => construct(null!));

        int floor = minLength ?? size;
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => construct(new byte[floor - 1]));

        if (minLength is null)
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => construct(new byte[size + 1]));

        T original = sample();
        byte[] bytes = toBytes(original);
        Assert.AreEqual(size, bytes.Length);

        T roundTripped = construct(bytes);
        Assert.AreEqual(original, roundTripped);
    }
}
