using System;
using System.Text;
using System.Net.Http.Headers;

public static partial class Js
{
    static object ParseHttpFormData(byte[] bytes, HeadersValue headers)
    {
        string contentType = headers.Fields.TryGetValue("content-type",out var values) ? HeadersValue.Joined("content-type",values) : "";
        if (!MediaTypeHeaderValue.TryParse(contentType,out var type)) throw new InvalidOperationException("Form data requires a valid content type");
        if (string.Equals(type.MediaType,"multipart/form-data",StringComparison.OrdinalIgnoreCase)) return ParseMultipartFormData(bytes,type);
        if (!string.Equals(type.MediaType,"application/x-www-form-urlencoded",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This content type cannot currently be read as form data");
        var query = CreateSearchParams(Encoding.UTF8.GetString(bytes));
        var result = new FormDataValue();
        foreach (var field in query.Pairs) result.Fields.Add(new(field.Key,field.Value));
        return result;
    }
}
