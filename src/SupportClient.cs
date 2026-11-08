using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
namespace DiskShift;
public static class SupportClient
{
    public const string Endpoint = "https://diskshift.dye.de/index.php";
    public static async Task<string> Send(object report, HttpMessageHandler? handler = null)
    {
        using var client = handler == null ? new HttpClient() : new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(40);
        using var response = await client.PostAsJsonAsync(Endpoint, report);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new IOException("Der Berichtsdienst auf diskshift.dye.de ist unter /index.php nicht eingerichtet (HTTP 404). Das liegt nicht an deinen Angaben. Bitte Bericht kopieren und den Betreiber informieren.");
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
