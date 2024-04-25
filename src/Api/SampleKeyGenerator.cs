using System.Text;

namespace Api;

public static class SampleKeyGenerator
{
    public static string GenerateKey(string message) => Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
}