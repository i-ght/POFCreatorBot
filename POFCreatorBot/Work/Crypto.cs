using DankLibWaifuz.Etc;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace POFCreatorBot.Work
{
    static class Crypto
    {
        private static readonly Random Random = new Random();
        private static readonly byte[] Key = { 200, 33, 18, 2, 17, 206, 168, 139, 242, 246, 150, 23, 179, 217, 233, 254 };

        public static string PofDecrypt(byte[] data)
        {
            Xor(data, 4);

            byte[] iv;
            using (var ms = new MemoryStream())
            {
                ms.Write(data, 0, 16);
                iv = ms.ToArray();
            }

            const int c = 16;
            const int n = 17;
            var b = data[c];
            var n2 = n;
            var n3 = 0;
            int n5;
            byte b3;

            for (byte b2 = 0; b2 < b; ++b2, n3 = b3, n2 = n5)
            {
                int n4 = n3 * 256;
                n5 = n2 + 1;
                b3 = (byte)(n4 + data[n2]);
            }

            using (var aes = new AesCryptoServiceProvider
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {
                using (var decryptor = aes.CreateDecryptor(Key, iv))
                {
                    var decryptedData = decryptor.TransformFinalBlock(data, n2, data.Length - n2);
                    var decompressedData = GZipWaifu.Decompress(decryptedData);
                    var decryptedStr = Encoding.UTF8.GetString(decompressedData);
                    return decryptedStr;
                }
            }
        }


        public static byte[] PofEncrypt(string request)
        {
            var iv = new byte[16];
            Random.NextBytes(iv);

            return PofEncrypt(iv, request);
        }

        public static byte[] PofEncrypt(byte[] iv, string request)
        {
            var gzippedData = GZipWaifu.Compress(request);

            byte[] encryptedData;
            using (var aes = new AesCryptoServiceProvider
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {
                using (var encryptor = aes.CreateEncryptor(Key, iv))
                {
                    encryptedData = encryptor.TransformFinalBlock(gzippedData, 0, gzippedData.Length);
                }
            }

            var n = 1;
            for (long n2 = 256L; gzippedData.Length >= n2; n2 *= 256L, ++n) { }

            using (var ms = new MemoryStream())
            {
                ms.Write(iv, 0, iv.Length);
                ms.WriteByte((byte)n);

                var array4 = new byte[n];
                byte[] array5 = BitConverter.GetBytes((long)gzippedData.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(array5);

                for (var i = 0; i < n; i++)
                {
                    var idx = -1 + array5.Length - i;
                    array4[i] = array5[idx];
                }

                ms.Write(array4, 0, array4.Length);
                ms.Write(encryptedData, 0, encryptedData.Length);

                var ret = ms.ToArray();
                Xor(ret, 4);
                return ret;
            }
        }

        public static void Xor(byte[] arg4, int arg5)
        {
            for (var v0 = arg5; v0 < arg4.Length - 8; ++v0)
            {
                arg4[v0] = ((byte)(arg4[v0] ^ arg4[v0 % arg5]));
            }
        }
    }
}
