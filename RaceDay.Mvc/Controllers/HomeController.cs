using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class HomeController(ApiClient api):Controller
{
    public async Task<IActionResult> Index(string? search,string? city,DateTime? fromDate)
    {
        var qs=new List<string>(); if(!string.IsNullOrWhiteSpace(city))qs.Add("city="+Uri.EscapeDataString(city));
        if(fromDate.HasValue)qs.Add("fromDate="+fromDate.Value.ToString("O"));
        var r=await api.SendAsync(HttpMethod.Get,"api/events"+(qs.Count>0?"?"+string.Join("&",qs):""));
        ViewBag.Search=search; ViewBag.City=city;
        if(!r.Ok) TempData["Error"]=ApiClient.UnavailableMessage;
        var data=ApiClient.Parse(r.Ok?r.Body:"[]");
        if(!string.IsNullOrWhiteSpace(search)) data=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(data.EnumerateArray().Where(x=>x.GetProperty("eventName").GetString()!.Contains(search,StringComparison.OrdinalIgnoreCase)))).RootElement.Clone();
        return View(data);
    }
    public IActionResult Error()=>View();
}
