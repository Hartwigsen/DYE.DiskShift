using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace DiskShift;
public static class SupportClient
{
    public const string Endpoint = "https://diskshift.dye.de/index.php";
    public static async Task<string> Send(object report, string code, HttpMessageHandler? handler = null)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Za-z0-9_-]{32,128}$")) throw new IOException("Bitte einen gültigen persönlichen Support-Code eingeben.");
        using var client = handler == null ? new HttpClient() : new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(40);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", code);
        using var response = await client.PostAsJsonAsync(Endpoint, report);
        var raw = await response.Content.ReadAsStringAsync();
        if (raw.Length > 16384) throw new IOException("Unerwartete Serverantwort. Bitte später erneut versuchen.");
        JsonDocument data;
        try { data = JsonDocument.Parse(raw); } catch (JsonException) { throw new IOException("Support-Dienst noch nicht eingerichtet oder nicht erreichbar. Bitte den Bericht kopieren und später erneut versuchen."); }
        using (data)
        {
            var message = data.RootElement.TryGetProperty("message", out var value) ? value.GetString() : "Support-Anfrage fehlgeschlagen.";
            if (!response.IsSuccessStatusCode) throw new IOException(message);
            if (!data.RootElement.TryGetProperty("id", out var id) || !Guid.TryParse(id.GetString(), out _)) throw new IOException("Unvollständige Empfangsbestätigung.");
            return message + "\nBerichtsnummer: " + id.GetString();
        }
    }
}
