using System;
using System.IO;
using System.Text;

public static partial class Js
{
    static string FormLineEndings(string value) => value.Replace("\r\n","\n").Replace('\r','\n').Replace("\n","\r\n");
    static string FormQuoted(string value) => value.Replace("\r","%0D").Replace("\n","%0A").Replace("\"","%22");
    static (byte[] Bytes,string ContentType,BufferedBody Stream) SerializeFormData(FormDataValue form)
    {
        string boundary = "----tsnet-form-" + Guid.NewGuid().ToString("N");
        using var output = new MemoryStream();
        void Text(string value) => output.Write(Encoding.UTF8.GetBytes(value));
        foreach (var field in form.Fields) {
            Text("--" + boundary + "\r\nContent-Disposition: form-data; name=\"" + FormQuoted(FormLineEndings(field.Key)) + "\"");
            if (field.Value is FileValue file) {
                Text("; filename=\"" + FormQuoted(file.Name) + "\"\r\nContent-Type: " + (file.Type.Length == 0 ? "application/octet-stream" : file.Type) + "\r\n\r\n");
                output.Write(file.Bytes);
            } else {
                Text("\r\n\r\n"); Text(FormLineEndings((string)field.Value));
            }
            Text("\r\n");
        }
        Text("--" + boundary + "--\r\n");
        return (output.ToArray(),"multipart/form-data; boundary=" + boundary,null);
    }
}
