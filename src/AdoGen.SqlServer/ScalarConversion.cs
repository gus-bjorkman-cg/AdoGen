using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AdoGen.SqlServer;

/// <summary>
/// Converts a value returned by <c>ExecuteScalarAsync</c> to the requested type without reflection.
/// </summary>
internal static class ScalarConversion
{
    /// <summary>
    /// Returns <c>default</c> for no row / <c>NULL</c>, the value itself when it already is a <typeparamref name="T"/>,
    /// and otherwise a checked numeric conversion (e.g. <c>bigint</c> → <c>int</c>). Throws <see cref="OverflowException"/>
    /// when the value does not fit and <see cref="InvalidCastException"/> for unsupported conversions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Convert<T>(object? value)
    {
        if (value is null or DBNull) return default;
        if (value is T typed) return typed;

        return ConvertNumeric<T>(value);
    }

    // typeof(T) checks are JIT/AOT constant-folded per instantiation; boxing only happens on this mismatch path.
    private static T ConvertNumeric<T>(object value)
    {
        var c = CultureInfo.InvariantCulture;

        if (typeof(T) == typeof(int) || typeof(T) == typeof(int?)) return (T)(object)System.Convert.ToInt32(value, c);
        if (typeof(T) == typeof(long) || typeof(T) == typeof(long?)) return (T)(object)System.Convert.ToInt64(value, c);
        if (typeof(T) == typeof(short) || typeof(T) == typeof(short?)) return (T)(object)System.Convert.ToInt16(value, c);
        if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?)) return (T)(object)System.Convert.ToByte(value, c);
        if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?)) return (T)(object)System.Convert.ToDecimal(value, c);
        if (typeof(T) == typeof(double) || typeof(T) == typeof(double?)) return (T)(object)System.Convert.ToDouble(value, c);
        if (typeof(T) == typeof(float) || typeof(T) == typeof(float?)) return (T)(object)System.Convert.ToSingle(value, c);

        throw new InvalidCastException(
            $"Cannot convert scalar value of type '{value.GetType().Name}' to '{typeof(T).Name}'. Only numeric conversions are supported.");
    }
}
