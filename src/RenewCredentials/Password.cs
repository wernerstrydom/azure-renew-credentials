using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RenewCredentials;

public class Password
{
    public static string GeneratePassword(
        string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+{}|[]\\<>?/.,",
        int minLength = 32,
        int maxLength = 56)
    {
        var bytes = new byte[maxLength * 1024];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(bytes);

        var result = Convert(bytes, alphabet);
        if (result.Length < minLength)
        {
            throw new InvalidOperationException("Unable to generate password.");
        }

        var r = Random.Shared.Next(minLength, maxLength);
        if (r > result.Length)
        {
            r = result.Length;
        }
        return result[..r];
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
        var n = BigInteger.Abs(new BigInteger(bytes));
        while (n != BigInteger.Zero)
        {
            n = BigInteger.DivRem(n, l, out var remainder);
            var r = (int)BigInteger.Abs(remainder);
            builder.Insert(0, alphabet[r]);
        }

        return builder.ToString();
    }
}