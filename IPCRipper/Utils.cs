using iText.Kernel.Pdf;

namespace IPCRipper
{
  public class Utils
  {
    public static string GetOSFriendlyName()
    {
#pragma warning disable CA1416 // validate platform compatibility
      using (
        var enumerator = new System.Management.ManagementObjectSearcher(
          "SELECT * FROM Win32_OperatingSystem"
        ).Get().GetEnumerator()
      ) {
        if (enumerator.MoveNext()) {
          return enumerator.Current["Caption"].ToString() ?? string.Empty;
        }
      }
#pragma warning restore CA1416 // validate platform compatibility
      return "";
    }

    public static void RemovePDFPassword(string infile, string password, string outfile)
    {
      foreach (char c in Path.GetInvalidPathChars()) {
        outfile = outfile.Replace(c, '-');
      }
      var reader = new PdfReader(
        infile,
        new ReaderProperties().SetPassword(System.Text.Encoding.UTF8.GetBytes(password))
      );
      reader.SetUnethicalReading(true);
      var doc_in = new PdfDocument(reader);
      var doc_out = new PdfDocument(new PdfWriter(outfile));
      doc_in.CopyPagesTo(1, doc_in.GetNumberOfPages(), doc_out);
      doc_out.Close();
      doc_in.Close();
      reader.Close();
    }

    public static string ReadInputUntil(
        string input_text, string invalid_text,
        Func<string, bool> validate_func
    )
    {
      while (true) {
        Console.Write(input_text);
        var str = Console.ReadLine();
        if (str == null || !validate_func(str)) {
          Console.WriteLine(invalid_text);
        } else {
          return str;
        }
      }
    }

    public static void Exit(string msg = "", bool error = false, params object?[]? arg)
    {
      if (msg != "") {
        Write(msg, arg);
      }
      Write("[INFO] Job {0}. Press any key to exit.", error ? "failed" : "finished");
      Console.ReadKey();
      System.Environment.Exit(1);
    }

    public static void ExitIfNull<T>(T obj, string error_msg = "", params object?[]? arg)
    {
      if (obj == null) {
        Exit(error_msg, true, arg);
      }
    }

    public static void Write(string msg, params object?[]? arg)
    {
      Console.WriteLine(msg, arg);
    }
  }
}