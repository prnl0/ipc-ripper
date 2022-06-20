using System.Text;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

#pragma warning disable CS8602 // dereference of a possibly null reference

namespace IPCRipper
{
  public class Program
  {
    public static void Main(string[] args)
    {
      /* Accept all certificates as per IPC Reader. */
      ServicePointManager.ServerCertificateValidationCallback =
        new RemoteCertificateValidationCallback(
          (object sender, X509Certificate? certification,
          X509Chain? chain, SslPolicyErrors ssl_policy_errors) => {
            return true;
          }
        );
      ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls | SecurityProtocolType.Tls12;
      if (args.Length > 0 && Path.GetExtension(args[0]).ToLower() == ".ipef") {
        ObtainPDF(args[0]);
        Utils.Exit();
      } else {
        Utils.Exit(
          args.Length == 0
          ? "[INIT] No files passed in args."
          : "[INIT] First argument is not a filepath to an .ipef.",
          true
        );
      }
    }

    private static void ObtainPDF(string ipef_filepath)
    {
      Utils.Write("[IPEF] Attempting to parse .ipef.");
      ParseIPEF(ipef_filepath);
      Utils.Write("[IPEF] .ipef successfully parsed.");

      Utils.Write("[APPREG] Attempting to register app.");
      var ar_res = RegisterApp().Result;
      Utils.ExitIfNull(
        ar_res, "[APPREG:FAILURE] Failed to register app/obtain UUID and signature."
      );
      Utils.Write("[APPREG] App successfully registered:");
      Utils.Write("           - UUID: {0}", ar_res.UUID);
      Utils.Write("           - signature: {0}", ar_res.Signature);
      IPCAPI.Base.uuid = ar_res.UUID;
      IPCAPI.Base.signature = ar_res.Signature;

      Utils.Write("[ENTINFO] Attempting to obtain entitlement info.");
      var ef_res = GetEntitlementInfo().Result;
      if (ef_res.Status == "1") {
        ipef.book.title = Encoding.UTF8.GetString(
          Convert.FromBase64String(ef_res.EntitlementFileData.Item.ItemName)
        );
        ipef.book.url = Crypto.DecryptEntitlementValue(
          ef_res.EntitlementFileData.Item.ItemResource.ItemContentPack,
          ef_res.EntitlementFileData.EntitlementInfo.Transactiontime
        );
        ipef.book.bk = Crypto.DecryptEntitlementValue(
          ef_res.EntitlementFileData.EntitlementInfo.Bk,
          ef_res.EntitlementFileData.EntitlementInfo.Transactiontime,
          false
        );
        ipef.book.biv = Crypto.DecryptEntitlementValue(
          ef_res.EntitlementFileData.EntitlementInfo.Biv,
          ef_res.EntitlementFileData.EntitlementInfo.Transactiontime,
          false
        );
        Utils.Write("[ENTINFO] Entitlement info. successfully obtained:");
        Utils.Write("            - title = {0}", ipef.book.title);
        Utils.Write("            - url = {0}", ipef.book.url);
        Utils.Write("            - bk = {0}", ipef.book.bk);
        Utils.Write("            - biv = {0}", ipef.book.biv);
        DownloadAndDecrypt().Wait();
      } else {
        Utils.Exit(
          "[ENTINFO:FAILURE] Unable to retrieve entitlement info. Message: {0} ({1}). " +
          "(Most probably because the .ipef has already been used.)", true,
          ef_res.Message.ToLower(), ef_res.ErrorCode
        );
      }
    }

    private static void ParseIPEF(string filepath)
    {
      try {
        var doc = new XmlDocument();
        Utils.Write("[IPEF:LOAD] Loading .ipef.");
        doc.Load(filepath);
        Utils.Write("[IPEF:LOAD] .ipef successfully loaded.");

        var body = doc.DocumentElement;
        Utils.ExitIfNull(body, "[IPEF:ERROR] Failed to load .ipef.");
        var item = body.SelectSingleNode("/ipcPreEntitlement/item");
        Utils.ExitIfNull(item, "[IPEF:ERROR] <item> not found.");
        var ent_info = body.SelectSingleNode("/ipcPreEntitlement/entitlementInfo");
        Utils.ExitIfNull(item, "[IPEF:ERROR] <entitlementInfo> not found.");

        IPCAPI.Base.url = body.SelectSingleNode(
          "/ipcPreEntitlement/entitlementEndPoint"
        ).InnerText;
        Utils.Write("[IPEF] IPC URL: {0}", IPCAPI.Base.url);

        Utils.Write("[IPEF:DEC] Decrypting necessary .ipef values.");
        ipef = new IPCAPI.IPEF {
          book_id = Crypto.DecryptEntitlementValue(
            item.SelectSingleNode("itemSystemID").InnerText
          ),
          site_id = Crypto.DecryptEntitlementValue(
            item.SelectSingleNode("itemSiteID").InnerText
          ),
          book_format = Crypto.DecryptEntitlementValue(
            item.SelectSingleNode("itemFormat").InnerText
          ),
          user_id = Crypto.DecryptEntitlementValue(
            ent_info.SelectSingleNode("info1").InnerText
          ),
          institution_id = Crypto.DecryptEntitlementValue(
            ent_info.SelectSingleNode("info2").InnerText
          ),
          nonce = Crypto.DecryptEntitlementValue(
            ent_info.SelectSingleNode("nonce").InnerText
          )
        };
        Utils.Write("[IPEF:DEC] Decrypted values:");
        Utils.Write("             - book_id = {0}", ipef.book_id);
        Utils.Write("             - site_id = {0}", ipef.site_id);
        Utils.Write("             - book_format = {0}", ipef.book_format);
        Utils.Write("             - user_id = {0}", ipef.user_id);
        Utils.Write("             - institution_id = {0}", ipef.institution_id);
        Utils.Write("             - nonce = {0}", ipef.nonce);
      } catch (Exception ex) {
        Utils.Exit("[IPEF:ERROR] Exception: {0}", true, ex.ToString());
      }
    }

    private async static Task<IPCAPI.JSON.AppRegisterResponse?> RegisterApp()
    {
      var param_list = new IPCAPI.AppRegistrationParams(
        api_key: IPCAPI.Base.api_key,
        app_det: new Dictionary<string, string> {
          ["appId"] = IPCAPI.Base.app_id,
          ["deviceTypeId"] = IPCAPI.Base.device_type_id
        }
      );
      using (
        var res = await Net.HttpPost(
          IPCAPI.Base.BuildURL(IPCAPI.Base.Action.RegisterApp), param_list
        )
      ) {
        return
          new DataContractJsonSerializer(typeof(IPCAPI.JSON.AppRegisterResponse))
            .ReadObject(stream: res.Content.ReadAsStream())
          as IPCAPI.JSON.AppRegisterResponse;
      }
    }

    private async static Task<IPCAPI.JSON.GetEntitlementFileResponse?> GetEntitlementInfo()
    {
      var param_list = new IPCAPI.EntitlementRetrievalParams(
        api_key: IPCAPI.Base.api_key,
        nonce_key: ipef.nonce,
        uuid: IPCAPI.Base.uuid,
        sig: IPCAPI.Base.signature,
        user_det: new Dictionary<string, string> {
          ["userId"] = ipef.user_id,
          ["institutionId"] = ipef.institution_id,
          ["bookId"] = ipef.book_id,
          ["FileFormat"] = ipef.book_format,
          ["siteCode"] = ipef.site_id
        }
      );
      using (
        var res = await Net.HttpPost(
          IPCAPI.Base.BuildURL(IPCAPI.Base.Action.GetEntitlementInfo), param_list
        )
      ) {
        return
          new DataContractJsonSerializer(
            typeof(IPCAPI.JSON.GetEntitlementFileResponse)
          ).ReadObject(stream: res.Content.ReadAsStream())
          as IPCAPI.JSON.GetEntitlementFileResponse;
      }
    }

    private static async Task DownloadAndDecrypt()
    {
      var filename = "enc-" + new Random().NextInt64(1000000, Int64.MaxValue) + ".pdf";
      var filepath = Directory.GetCurrentDirectory() + "\\" + filename;
      var filepaths = new List<string>() { filepath };
      Utils.Write("[POST:DL] Attempting to download encrypted book from \"{0}\".", ipef.book.url);
      await Net.Download(ipef.book.url, filepath);
      Utils.Write(
        "[POST:DL] Encrypted book successfully downloaded and stored in \"{0}\".", filepath
      );
      Utils.Write("[POST:DEC] Attempting to decrypt it.");
      try {
        Utils.Write("[POST:DEC] Using AES:");
        Utils.Write("             - mode = CBC");
        Utils.Write("             - padding = zeros");
        Utils.Write("             - key_size = 128");
        Utils.Write("             - block_size = 128");
        Utils.Write("             - key = {0}", ipef.book.bk);
        Utils.Write("             - iv = {0}", ipef.book.biv);
        Crypto.DecryptPDF(
          filepath, filepath = filepath.Replace(
            filename, filename = filename.Replace("enc", "dec")
          ),
          ipef.book.bk, ipef.book.biv
        );
        filepaths.Add(filepath);
        Utils.Write("[POST:DEC] Book successfully decrypted.");
        Utils.Write("[POST:RMPW] Attempting to remove password ({0}).", ipef.book.bk);
        var final_filepath =
          Directory.GetCurrentDirectory() +
          "\\" + ipef.book.title.Trim().Replace(' ', '_') + ".pdf";
        Utils.RemovePDFPassword(filepath, ipef.book.bk, final_filepath);
        Utils.Write("[POST:RMPW] Password successfully removed.");
        Utils.Write("[POST] Final book has been saved in \"{0}\".", final_filepath);
        Utils.Write(
          "[POST:INFO] The .pdf has an associated \"Producer\" metadata " +
          "field, since it was produced with iText7 using an open-source license."
        );
      } catch (Exception ex) {
        Utils.Write("[POST:ERROR] Exception: {0}", ex.ToString());
        if (ex.ToString().ToLower().Contains("pdf header not found")) {
          Utils.Write(
            "[POST:ERROR:INFO] PDF header malformed due to incorrect " +
            "encryption/decryption of the book."
          );
        }
      }
      Utils.Write("[MISC] Removing leftover files:");
      foreach (var fp in filepaths) {
        Utils.Write("         - {0}", fp);
        File.Delete(fp);
      }
    }

    private static IPCAPI.IPEF? ipef;
  }
}