using System;
using System.Text;
using System.Net.Http.Headers;

public static partial class Js
{
    static string MultipartParameter(string value)
    {
        if (value == null || value.Length < 2 || value[0] != '"' || value[^1] != '"') return value;
        var result = new StringBuilder(value.Length-2);
        for (int index = 1; index < value.Length-1; index++) {
            char character = value[index];
            if (character == '\\' && index+1 < value.Length-1) character = value[++index];
            result.Append(character);
        }
        return result.ToString();
    }
    static int MultipartBoundary(byte[] bytes, byte[] marker, int start)
    {
        while (start <= bytes.Length-marker.Length) {
            int found = bytes.AsSpan(start).IndexOf(marker);
            if (found < 0) return -1;
            int index = start+found, end = index+marker.Length;
            bool line = index == 0 || index >= 2 && bytes[index-2] == 13 && bytes[index-1] == 10;
            bool suffix = end+1 < bytes.Length && (bytes[end] == 13 && bytes[end+1] == 10 || bytes[end] == 45 && bytes[end+1] == 45);
            if (line && suffix) return index;
            start = index+1;
        }
        return -1;
    }
    static string MultipartFieldName(string value) => MultipartParameter(value)?.Replace("%0D","\r",StringComparison.OrdinalIgnoreCase).Replace("%0A","\n",StringComparison.OrdinalIgnoreCase).Replace("%22","\"",StringComparison.OrdinalIgnoreCase);
    static FormDataValue ParseMultipartFormData(byte[] bytes, MediaTypeHeaderValue contentType)
    {
        string boundary = null;
        foreach (var parameter in contentType.Parameters)
            if (string.Equals(parameter.Name,"boundary",StringComparison.OrdinalIgnoreCase)) {boundary = MultipartParameter(parameter.Value);break;}
        if (string.IsNullOrEmpty(boundary) || boundary.Contains('\r') || boundary.Contains('\n'))
            throw new InvalidOperationException("Multipart form data requires a boundary");
        byte[] marker = Encoding.UTF8.GetBytes("--"+boundary);
        int position = MultipartBoundary(bytes,marker,0);
        var result = new FormDataValue();
        while (position >= 0) {
            position += marker.Length;
            if (bytes[position] == 45 && bytes[position+1] == 45) return result;
            position += 2;
            int next = MultipartBoundary(bytes,marker,position);
            if (next < 0) break;
            int end = next-2;
            if (end < position) throw new InvalidOperationException("Malformed multipart field");
            int headersLength = bytes.AsSpan(position,end-position).IndexOf("\r\n\r\n"u8);
            if (headersLength < 0) throw new InvalidOperationException("Multipart field headers are incomplete");
            string headers = Encoding.UTF8.GetString(bytes,position,headersLength);
            string dispositionText = null, typeText = null, transferEncoding = null;
            foreach (string line in headers.Split("\r\n")) {
                int colon = line.IndexOf(':');
                if (colon <= 0) throw new InvalidOperationException("Invalid multipart header");
                string name = HeaderName(line[..colon]), value = line[(colon+1)..].Trim(' ','\t');
                if (name == "content-disposition" && dispositionText == null) dispositionText = value;
                if (name == "content-type" && typeText == null) typeText = value;
                if (name == "content-transfer-encoding" && transferEncoding == null) transferEncoding = value;
            }
            if (!ContentDispositionHeaderValue.TryParse(dispositionText,out var disposition) ||
                !string.Equals(disposition.DispositionType,"form-data",StringComparison.OrdinalIgnoreCase) || disposition.Name == null)
                throw new InvalidOperationException("Multipart field requires a form-data disposition and name");
            {
                string name = MultipartFieldName(disposition.Name), filename = MultipartFieldName(disposition.FileName);
                var payload = bytes.AsSpan(position+headersLength+4,end-position-headersLength-4);
                if (filename != null) {
                    string fileType = MediaTypeHeaderValue.TryParse(typeText,out _) ? BlobType(typeText) : "text/plain";
                    byte[] content = string.Equals(transferEncoding,"base64",StringComparison.OrdinalIgnoreCase)
                        ? Convert.FromBase64String(Encoding.ASCII.GetString(payload)) : payload.ToArray();
                    result.Fields.Add(new(name,new FileValue(content,fileType,filename,DateNow())));
                } else result.Fields.Add(new(name,Encoding.UTF8.GetString(payload)));
            }
            position = next;
        }
        throw new InvalidOperationException("Multipart form data is incomplete");
    }
}
