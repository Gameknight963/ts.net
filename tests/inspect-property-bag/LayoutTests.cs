using System.Reflection;

static class LayoutTests
{
    internal static void Check(string path)
    {
        var assembly = Assembly.LoadFile(Path.GetFullPath(path));
        var fields = assembly.GetTypes().Where(type => type.BaseType == typeof(TsObject))
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        foreach (string name in new[] { "safe", "value" })
        {
            string encoded = "p_" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(name)).ToLowerInvariant();
            if (!fields.Any(field => field.Name == encoded &&
                (field.FieldType == typeof(double) || field.FieldType == typeof(double?))))
                throw new Exception($"Dynamic alias writes changed an unaffected numeric layout: {name}");
        }
        Console.WriteLine("PASS dynamic alias writes preserve unaffected numeric layouts");
    }
}
