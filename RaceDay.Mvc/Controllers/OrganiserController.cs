using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class OrganiserController(ApiClient api):Controller
{
    private bool Allowed()=>HttpContext.Session.GetString("Role")=="Organiser";
    private IActionResult Guard()=>RedirectToAction("Login","Account")!;
    public async Task<IActionResult> Index(){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Get,"api/organiser/events");return View(ApiClient.Parse(r.Ok?r.Body:"[]"));}
    public IActionResult Create(){if(!Allowed())return Guard();return View(new EventVm());}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(EventVm vm){if(!Allowed())return Guard();if(!ModelState.IsValid)return View(vm);
        var r=await api.SendAsync(HttpMethod.Post,"api/events",new {vm.EventName,vm.Description,vm.EventDate,Venue=vm.Venue,City=vm.City,vm.Status,vm.EventType});
        if(!r.Ok){ModelState.AddModelError("","Event could not be saved. "+r.Body);return View(vm);}
        var id=ApiClient.Parse(r.Body).GetProperty("eventId").GetInt32();
        if(vm.Banner is {Length:>0}){var up=await api.UploadAsync($"api/events/{id}/banner",vm.Banner);if(!up.Ok)TempData["Error"]="Event created, but banner upload failed: "+up.Body;}
        TempData["Success"]="Event created.";return RedirectToAction(nameof(Index));}
    public async Task<IActionResult> Edit(int id){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Get,$"api/events/{id}");if(!r.Ok)return NotFound();var e=ApiClient.Parse(r.Body);return View(new EventVm{EventId=id,EventName=e.GetProperty("eventName").GetString()??"",Description=e.GetProperty("description").ValueKind==JsonValueKind.Null?null:e.GetProperty("description").GetString(),EventDate=e.GetProperty("eventDate").GetDateTime(),Venue=e.GetProperty("venue").GetString()??"",City=e.GetProperty("city").GetString()??"",Status=e.GetProperty("status").GetString()??"Planned",EventType=e.TryGetProperty("eventType",out var type)?type.GetString()??"Road Race":"Road Race"});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(EventVm vm){if(!Allowed())return Guard();if(!ModelState.IsValid)return View(vm);var r=await api.SendAsync(HttpMethod.Put,$"api/events/{vm.EventId}",new{vm.EventName,vm.Description,vm.EventDate,vm.Venue,vm.City,vm.Status,vm.EventType});if(!r.Ok){ModelState.AddModelError("","Update failed. "+r.Body);return View(vm);}if(vm.Banner is {Length:>0})await api.UploadAsync($"api/events/{vm.EventId}/banner",vm.Banner);TempData["Success"]="Event updated.";return RedirectToAction(nameof(Index));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Delete,$"api/events/{id}");TempData[r.Ok?"Success":"Error"]=r.Ok?"Event deleted.":r.Body;return RedirectToAction(nameof(Index));}
    public async Task<IActionResult> Categories(int id){if(!Allowed())return Guard();ViewBag.EventId=id;var r=await api.SendAsync(HttpMethod.Get,$"api/events/{id}/categories");return View(ApiClient.Parse(r.Ok?r.Body:"[]"));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> AddCategory(int id,CategoryVm vm){if(!Allowed())return Guard();if(!ModelState.IsValid){TempData["Error"]="Check category details.";return RedirectToAction(nameof(Categories),new{id});}var r=await api.SendAsync(HttpMethod.Post,$"api/events/{id}/categories",vm);TempData[r.Ok?"Success":"Error"]=r.Ok?"Category added.":r.Body;return RedirectToAction(nameof(Categories),new{id});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> DeleteCategory(int id,int eventId){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Delete,$"api/categories/{id}");TempData[r.Ok?"Success":"Error"]=r.Ok?"Category deleted.":r.Body;return RedirectToAction(nameof(Categories),new{id=eventId});}
    public async Task<IActionResult> Enrolments(int id){if(!Allowed())return Guard();ViewBag.EventId=id;var r=await api.SendAsync(HttpMethod.Get,$"api/events/{id}/enrolments");return View(ApiClient.Parse(r.Ok?r.Body:"[]"));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> RecordResult(int eventId,int enrollmentId,int? resultId,string? finishTime,int? position,string resultStatus="Finished"){if(!Allowed())return Guard();TimeSpan? time=TimeSpan.TryParse(finishTime,out var parsed)?parsed:null;var payload=new{finishTime=time,position,resultStatus};var r=resultId.HasValue?await api.SendAsync(HttpMethod.Put,$"api/results/{resultId.Value}",payload):await api.SendAsync(HttpMethod.Post,$"api/events/{eventId}/results/{enrollmentId}",payload);TempData[r.Ok?"Success":"Error"]=r.Ok?"Result saved and published.":r.Body;return RedirectToAction(nameof(Enrolments),new{id=eventId});}
}
