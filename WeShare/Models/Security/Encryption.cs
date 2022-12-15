using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Models.Security;

public abstract class Encryption
{
    /// <summary>
    ///     Encrypts a password by hashing and salting it.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="encryptedValue"></param>
    /// <param name="salt"></param>
    public static void Create(string value, out string encryptedValue, out string salt)
    {
        salt = CreateSalt();
        encryptedValue = SaltHash(CreateHash(value), salt);
    }

    /// <summary>
    ///     Compares a new input with the encrypted password stored in the database using the salt.
    ///     Always use the same salt that was used to encrypt the password.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="encryptedValue"></param>
    /// <param name="salt"></param>
    /// <returns>
    ///     A boolean value indicating whether the input matches the encrypted password.
    /// </returns>
    public static bool Compare(string input, string encryptedValue, string salt)
    {
        var saltedHashedInput = SaltHash(CreateHash(input), salt);
        return saltedHashedInput == encryptedValue;
    }

    private static string CreateHash(string password)
    {
        // Create a new instance of the MD5CryptoServiceProvider object.
        var md5Hasher = new MD5CryptoServiceProvider();

        // Convert the input string to a byte array and compute the hash.
        var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(password));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        var sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        foreach (var t in data)
            sBuilder.Append(t.ToString("x2"));

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    private static string CreateSalt()
    {
        // Generate a cryptographic random number.
        var rng = new Random();
        var buff = new byte[16];
        rng.NextBytes(buff);

        // Return a Base64 string representation of the random number.
        return Convert.ToBase64String(buff);
    }

    private static string SaltHash(string hash, string salt)
    {
        var hashBytes = Convert.FromBase64String(hash);
        var saltBytes = Convert.FromBase64String(salt);
        var toHash = new byte[hashBytes.Length + saltBytes.Length];

        Buffer.BlockCopy(hashBytes, 0, toHash, 0, hashBytes.Length);
        Buffer.BlockCopy(saltBytes, 0, toHash, hashBytes.Length, saltBytes.Length);

        var hashed = new SHA256Managed().ComputeHash(toHash);

        return Convert.ToBase64String(hashed);
    }
}