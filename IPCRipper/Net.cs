using System.Net;
using System.Text;
using System.Text.Json;

namespace IPCRipper
{
  internal class Net
  {
    public static Task<HttpResponseMessage> HttpPost<T>(
      string url, T param_list, bool use_common_api = false
    )
    {
      try {
        var req = new HttpRequestMessage(HttpMethod.Post, url) {
          Content = new StringContent(
            Serialize(param_list, use_common_api),
            Encoding.UTF8,
            "application/x-www-form-urlencoded"
          )
        };
        return http_client.SendAsync(req);
      } catch (HttpRequestException ex) {
        Utils.Exit(
          "[HttpRequestException] Server returned '" + ex.Message +
          "' with status code " + ex.StatusCode + "."
        );
#pragma warning disable CS8603 // possible null reference return
        return null;
#pragma warning restore CS8603 // possible null reference return
      }
    }

    public static async Task Download(
      string uri_str, string outfile, Cookie? cookie = null, bool overwrite = true
    )
    {
      var uri = new Uri(uri_str);
      if (cookie != null) {
        http_handler.CookieContainer = cookie_container = new CookieContainer();
        cookie_container.Add(uri, cookie);
      }
      var res = await http_client.GetAsync(uri);
      using (
        var fs = new FileStream(outfile, overwrite ? FileMode.Create : FileMode.CreateNew)
      ) {
        await res.Content.CopyToAsync(fs);
      }
    }

    private static string Serialize<T>(T obj, bool with_common_api)
    {
      return with_common_api
        ? JsonSerializer.Serialize(IPCAPI.CommonValues.instance)[0..^1] + ',' +
          JsonSerializer.Serialize(obj)[1..]
        : JsonSerializer.Serialize(obj);
    }

    private static CookieContainer cookie_container = new();
    private static readonly HttpClientHandler http_handler = new();
    private static readonly HttpClient http_client = new(http_handler);
  }
}
