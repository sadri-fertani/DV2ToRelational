using System.Security.Cryptography;
using System.Text;

namespace CustomORM.Core.Extensions;

public static class CryptographyExtensions
{
    /// <summary>
    /// Use sha256 to crypt data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToSha256<T>(this T value) where T : IConvertible
    {
        using SHA256 hasher = SHA256.Create();
        byte[] data = hasher.ComputeHash(Encoding.Unicode.GetBytes(value!.ToString()!));

        return string.Concat(data.Select(x => x.ToString("x2")));
    }
}
