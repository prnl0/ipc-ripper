using System.Text;
using System.Security.Cryptography;

namespace IPCRipper
{
  public class Crypto
  {
    public static string DecryptEntitlementValue(
      string val, string iv = "", bool pkcs7_padding = true
    )
    {
      byte[] enc_data = Convert.FromBase64String(val);
      return Encoding.UTF8.GetString(
        GetAES(IPCAPI.IPEF.Book.zip_key, iv, pkcs7_padding)
        .CreateDecryptor()
        .TransformFinalBlock(enc_data, 0, enc_data.Length)
      );
    }

    public static void DecryptPDF(string infile, string outfile, string key, string iv)
    {
      Aes aes = Aes.Create();
      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.Zeros;
      aes.KeySize = 128;
      aes.BlockSize = 128;
      aes.Key = Encoding.UTF8.GetBytes(key);
      aes.IV = Encoding.UTF8.GetBytes(iv);
      var in_stream = new FileStream(infile, FileMode.Open);
      var crypto_stream = new CryptoStream(
        in_stream, aes.CreateDecryptor(), CryptoStreamMode.Read
      );
      var out_stream = new FileStream(outfile, FileMode.Create);
      for (int num; (num = crypto_stream.ReadByte()) != -1;) {
        out_stream.WriteByte((byte)num);
      }
      out_stream.Close();
      crypto_stream.Close();
      in_stream.Close();
    }

    private static Aes GetAES(string key, string iv, bool pkcs7_padding)
    {
      Aes aes = Aes.Create();
      aes.Mode = CipherMode.CBC;
      aes.Padding = pkcs7_padding ? PaddingMode.PKCS7 : PaddingMode.None;
      aes.KeySize = 128;
      aes.BlockSize = 128;
      aes.Key = Encoding.UTF8.GetBytes(key);
      aes.IV = Encoding.UTF8.GetBytes(iv.Length == 0 ? IPCAPI.IPEF.Book.zip_iv : iv);
      return aes;
    }
  }
}