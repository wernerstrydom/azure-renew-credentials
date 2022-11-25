using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RenewCredentials;

public class Password
{
    public static string GeneratePassword(
        string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+{}|[]\\<>?/.,",
        int length = 32)
    {
        var bytes = new byte[length * 4];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(bytes);

        var result = Convert(bytes, alphabet);
        return result.Substring(0, length);
    }

    private static string Convert(byte[] bytes, string alphabet)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (alphabet == null) throw new ArgumentNullException(nameof(alphabet));
        if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
        if (string.IsNullOrWhiteSpace(alphabet))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(alphabet));

        var builder = new StringBuilder();

        var l = new BigInteger(alphabet.Length);
        var zero = new BigInteger(0);
        var n = new BigInteger(bytes);
        while (n != zero)
        {
            n = BigInteger.DivRem(n, l, out var remainder);
            builder.Insert(0, alphabet[(int)remainder]);
        }

        return builder.ToString();
    }
}