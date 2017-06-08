using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Kryptos.Models
{
    public class Encryption
    {
        #region Encryption
        /// <summary>
        /// Decrypt classAES 256
        /// </summary>
        /// <param name="cipherText"></param>     
        /// <function>DecryptStringAES</function>
        /// <remarks></remarks>
        /// <version> 6.0</version>
        /// <Developer>
        /// <Name>Sohan</Name>
        /// <CreatedOn>12.16.2016</CreatedOn>
        /// <ModifiedOn></ModifiedOn>
        /// </Developer>
        public string DecryptStringAES(string cipherText)
        {
            var keybytes = Encoding.UTF8.GetBytes("bbC2H19lkVbQDfakxcrtNMQdd0FloLyw");
            var iv = Encoding.UTF8.GetBytes("gqLOHUioQ0QjhuvI");
            var encrypted = Convert.FromBase64String(cipherText);
            var decriptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
            return string.Format(decriptedFromJavascript);
        }
        public string EncryptString(string sOrginalString)
        {
            string sCipherText = string.Empty;
            try
            {
                var keybytes = Encoding.UTF8.GetBytes("bbC2H19lkVbQDfakxcrtNMQdd0FloLyw");
                var iv = Encoding.UTF8.GetBytes("gqLOHUioQ0QjhuvI");


                // Encrypt the string to an array of bytes.
                byte[] encrypted = EncryptStringToBytes_Aes(sOrginalString, keybytes, iv);
                sCipherText = Convert.ToBase64String(encrypted);

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
            return sCipherText;
        }

        public byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }
         
        /// <summary>
        /// Decrypt String From Bytes
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="key"></param>
        /// <function>DecryptStringFromBytes</function>
        /// <remarks></remarks>
        /// <version> 6.0</version>
        /// <Developer>
        /// <Name>Sohan</Name>
        /// <CreatedOn>12.16.2016</CreatedOn>
        /// <ModifiedOn></ModifiedOn>
        /// </Developer>
        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (var rijAlg = new RijndaelManaged())
            {
                //Settings 
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;

                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform. 
                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                try
                {
                    // Create the streams used for decryption. 
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {

                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream 
                                // and place them in a string. 
                                plaintext = srDecrypt.ReadToEnd();

                            }

                        }
                    }
                }
                catch
                {
                    plaintext = "keyError";
                }
            }

            return plaintext;
        }
        #endregion
    }
}