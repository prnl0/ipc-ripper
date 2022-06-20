using System.Runtime.Serialization;

/* Surpress "Non-nullable field must contain a non-null value when exiting
 * constructor. Consider declaring as nullable." warnings in ``[DataContract]``
 * classes taken from decompiled IPC Reader code. */
#pragma warning disable CS8618

namespace IPCRipper
{
  namespace IPCAPI
  {
    public class Base
    {
      public enum Action
      {
        RegisterApp,
        GetEntitlementInfo
      }

      public static string BuildURL(Action action)
      {
        return url + rel_path_dict[action] + action_dict[action];
      }

      public static string url = "";
      public static string uuid = "";
      public static string signature = "";

      public static readonly string api_key = "CXJRJ81WXT7dfJ5DVEG2";
      public static readonly string app_id = "Impe1sys510@Jan@dd2014";
      public static readonly string device_type_id = "6";
      public static readonly Dictionary<Action, string> rel_path_dict = new() {
        [Action.RegisterApp] = "api/1.0/device/",
        [Action.GetEntitlementInfo] = "api/1.0/entitlement/"
      };
      public static readonly Dictionary<Action, string> action_dict = new() {
        [Action.RegisterApp] = "register",
        [Action.GetEntitlementInfo] = "getentitlementfile"
      };
    }

    public class CommonValues
    {
      public CommonValues(
        string dev_name, string dev_os, string dev_os_name, string dev_model,
        string app_ver, string build_ver
      )
      {
        deviceName = dev_name;
        deviceOS = dev_os;
        deviceOSName = dev_os_name;
        deviceModel = dev_model;
        applicationVersion = app_ver;
        buildVersion = build_ver;
      }

      public string deviceName { get; set; }
      public string deviceOS { get; set; }
      public string deviceOSName { get; set; }
      public string deviceModel { get; set; }
      public string applicationVersion { get; set; }
      public string buildVersion { get; set; }

      public static readonly CommonValues instance = new(
        dev_name: Environment.MachineName,
        dev_os: System.Text.RegularExpressions.Regex.Replace(
          Utils.GetOSFriendlyName(), "[.\\D+][^0-9]", ""
        ),
        dev_os_name: Utils.GetOSFriendlyName(),
        dev_model: "Desktop",
        app_ver: "7.4.7",
        build_ver: "1"
      );
    }

    public class AppRegistrationParams
    {
      public AppRegistrationParams(string api_key, Dictionary<string, string> app_det)
      {
        apiKey = api_key;
        appDetails = app_det;
      }

      public string apiKey { get; set; }
      public Dictionary<string, string> appDetails { get; set; }
    }

    public class EntitlementRetrievalParams
    {
      public EntitlementRetrievalParams(
        string api_key, string nonce_key, string uuid, string sig,
        Dictionary<string, string> user_det
      )
      {
        apiKey = api_key;
        nonceKey = nonce_key;
        UUID = uuid;
        signature = sig;
        userDetails = user_det;
      }

      public string apiKey { get; set; }
      public string nonceKey { get; set; }
      public string UUID { get; set; }
      public string signature { get; set; }
      public Dictionary<string, string> userDetails { get; set; }
    }

    public class IPEF
    {
      public class Book
      {
        public string title = "";
        public string url = "";
        public string bk = "";
        public string biv = "";

        public static readonly string zip_key = "CDD3sAFS34dSDFS3";
        public static readonly string zip_iv = "3NJ1J81W4T7dfJnD";
      }

      public string book_id = "";
      public string site_id = "";
      public string book_format = "";
      public string user_id = "";
      public string institution_id = "";
      public string nonce = "";

      public Book book = new();
    }

    namespace JSON
    {
      [DataContract]
      public class AppRegisterResponse
      {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "errorCode")]
        public string ErrorCode { get; set; }

        [DataMember(Name = "timestamp")]
        public string TimeStamp { get; set; }

        [DataMember(Name = "uuid")]
        public string UUID { get; set; }

        [DataMember(Name = "signature")]
        public string Signature { get; set; }

        [DataMember(Name = "appId")]
        public string AppId { get; set; }
      }

      [DataContract]
      public class ItemResourceParameters
      {
        [DataMember(Name = "itemCoverArtURL")]
        public string ItemCoverArtURL { get; set; }

        [DataMember(Name = "itemContentPack")]
        public string ItemContentPack { get; set; }
      }

      [DataContract]
      public class ItemDataParameters
      {
        [DataMember(Name = "itemName")]
        public string ItemName { get; set; }

        [DataMember(Name = "itemDescription")]
        public string ItemDescription { get; set; }

        [DataMember(Name = "itemIdentifier")]
        public string ItemIdentifier { get; set; }

        [DataMember(Name = "itemSystemID")]
        public string ItemSystemID { get; set; }

        [DataMember(Name = "itemType")]
        public string ItemType { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "itemResource")]
        public ItemResourceParameters ItemResource { get; set; }

        [DataMember(Name = "itemSiteID")]
        public string ItemSiteID { get; set; }
      }

      [DataContract]
      public class EntitlementInfoParameters
      {
        [DataMember(Name = "expires")]
        public string Expires { get; set; }

        [DataMember(Name = "bk")]
        public string Bk { get; set; }

        [DataMember(Name = "biv")]
        public string Biv { get; set; }

        [DataMember(Name = "et")]
        public string Et { get; set; }

        [DataMember(Name = "ev")]
        public string Ev { get; set; }

        [DataMember(Name = "transactiontime")]
        public string Transactiontime { get; set; }
      }

      [DataContract]
      public class EntitlementFileDataParameters
      {
        [DataMember(EmitDefaultValue = false, Name = "item")]
        public ItemDataParameters Item { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "entitlementInfo")]
        public EntitlementInfoParameters EntitlementInfo { get; set; }
      }

      [DataContract]
      public class GetEntitlementFileResponse
      {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "errorCode")]
        public string ErrorCode { get; set; }

        [DataMember(Name = "timestamp")]
        public string TimeStamp { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "data")]
        public EntitlementFileDataParameters EntitlementFileData { get; set; }
      }
    }
  }
}