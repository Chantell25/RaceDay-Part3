using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
namespace RaceDay.Mvc.Services;
public class ApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private ISession? Session => accessor.HttpContext?.Session;
    public const string UnavailableMessage = "The RaceDay service is not reachable right now. Please make sure the RaceDay API is running and try again.";
    private static readonly string Unavailable = "{\"message\":\"" + UnavailableMessage + "\"}";
    // Returns null (instead of throwing) when the API cannot be reached, so users see a friendly message rather than a crash page.
    private async Task<HttpResponseMessage?> TrySend(HttpRequestMessage req)
    {
        try { return await http.SendAsync(req); }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }
    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, path);
        var cookie = Session?.GetString("ApiCookie");
        if (!string.IsNullOrWhiteSpace(cookie)) req.Headers.TryAddWithoutValidation("Cookie", cookie);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return req;
    }
    public async Task<(bool Ok,string Body)> SendAsync(HttpMethod method,string path,object? data=null)
    {
        using var req=Request(method,path);
        if(data is not null) req.Content=new StringContent(JsonSerializer.Serialize(data,Json),Encoding.UTF8,"application/json");
        using var response=await TrySend(req); if(response is null) return (false,Unavailable);
        SaveCookie(response);
        return (response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());
    }
    public async Task<(bool Ok,string Body)> UploadAsync(string path,IFormFile file)
    {
        using var req=Request(HttpMethod.Post,path);
        using var content=new MultipartFormDataContent();
        await using var stream=file.OpenReadStream();
        var fileContent=new StreamContent(stream);
        fileContent.Headers.ContentType=new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent,"file",Path.GetFileName(file.FileName)); req.Content=content;
        using var response=await TrySend(req); if(response is null) return (false,Unavailable); SaveCookie(response);
        return (response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());
    }
    public async Task<(bool Ok,string Body)> LoginAsync(string email,string password)
    {
        using var req=new HttpRequestMessage(HttpMethod.Post,"api/auth/login");
        req.Content=new StringContent(JsonSerializer.Serialize(new {email,password},Json),Encoding.UTF8,"application/json");
        using var response=await TrySend(req); if(response is null) return (false,Unavailable);
        SaveCookie(response);
        return (response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());
    }
    private void SaveCookie(HttpResponseMessage response)
    {
        if(response.Headers.TryGetValues("Set-Cookie",out var values))
        {
            var cookie=string.Join("; ",values.Select(x=>x.Split(';')[0]));
            Session?.SetString("ApiCookie",cookie);
        }
    }
    public void ClearSession(){ Session?.Clear(); }
    public static JsonElement Parse(string body)
    {
        using var doc=JsonDocument.Parse(string.IsNullOrWhiteSpace(body)?"[]":body);
        return doc.RootElement.Clone();
    }
}
