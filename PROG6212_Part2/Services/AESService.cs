using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PROG6212_Part2.Services
{
    // Static service for AES (Advanced Encryption Standard) encryption and decryption operations.
    // Provides methods to encrypt/decrypt both files and strings securely using a symmetric key. (dotnet-bot, 2025a)
    public static class AESService
    {
        // AES symmetric key loaded from appsettings.json
        private static readonly byte[] _key;

        // Static constructor - runs once when the class is first accessed.
        static AESService()
        {
            // Build configuration to read the encryption key from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Retrieve the key from configuration (stored under Encryption:Key)
            string keyString = config["Encryption:Key"];

            // Validate key existence
            if (string.IsNullOrEmpty(keyString))
                throw new Exception("AES key not found in appsettings.json.");

            // Convert key from string to byte array
            _key = Encoding.UTF8.GetBytes(keyString);

            // AES only supports key sizes of 128, 192, or 256 bits (16, 24, or 32 bytes)
            if (!(_key.Length == 16 || _key.Length == 24 || _key.Length == 32))
                throw new Exception("AES key must be 16, 24, or 32 bytes long.");
        }

        // Encrypts a file using AES and writes the IV at the start of the output file.
        public static void EncryptFile(string inputFile, string outputFile)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // Generate a random Initialization Vector
            aes.Padding = PaddingMode.PKCS7;

            // Create output file and write the IV at the beginning for later decryption
            using FileStream fsEncrypted = new FileStream(outputFile, FileMode.Create);
            fsEncrypted.Write(aes.IV, 0, aes.IV.Length);

            // Create encryption stream and copy plaintext data into it
            using CryptoStream cs = new CryptoStream(fsEncrypted, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using FileStream fsInput = new FileStream(inputFile, FileMode.Open);
            fsInput.CopyTo(cs);
        }

        // Decrypts an AES-encrypted file (expects IV to be stored at the start of the file)
        public static void DecryptFile(string inputFile, string outputFile)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Padding = PaddingMode.PKCS7;

            using FileStream fsEncrypted = new FileStream(inputFile, FileMode.Open);

            // Read the IV from the beginning of the encrypted file
            byte[] iv = new byte[aes.BlockSize / 8];
            fsEncrypted.Read(iv, 0, iv.Length);
            aes.IV = iv;

            // Decrypt the remaining file data
            using CryptoStream cs = new CryptoStream(fsEncrypted, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using FileStream fsDecrypted = new FileStream(outputFile, FileMode.Create);
            cs.CopyTo(fsDecrypted);
        }

        // Encrypts a string and returns a Base64-encoded ciphertext
        public static string EncryptString(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var ms = new MemoryStream();

            // Write the IV at the start for use during decryption
            ms.Write(aes.IV, 0, aes.IV.Length);

            // Create encryption stream and write the plaintext
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            // Convert the full encrypted payload (IV + ciphertext) to Base64
            return Convert.ToBase64String(ms.ToArray());
        }

        // Decrypts a Base64-encoded AES ciphertext back into plain text
        public static string DecryptString(string cipherText)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;

            // Extract IV from the start of the cipher
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Create decryption stream starting after IV bytes
            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            // Return decrypted plaintext
            return sr.ReadToEnd();
        }
    }
}
