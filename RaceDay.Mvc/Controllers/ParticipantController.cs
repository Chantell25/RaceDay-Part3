using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class ParticipantController(ApiClient api):Controller
{
    private bool Allowed()=>HttpContext.Session.GetString("Role")=="Participant";
    private IActionResult Guard()=>RedirectToAction("Login","Account")!;
    public async Task<IActionResult> Index(){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Get,"api/enrolments/mine");return View(ApiClient.Parse(r.Ok?r.Body:"[]"));}
    public async Task<IActionResult> Details(int id){var r=await api.SendAsync(HttpMethod.Get,$"api/events/{id}");if(!r.Ok)return NotFound();return View(ApiClient.Parse(r.Body));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Enrol(int eventId,int categoryId){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Post,$"api/enrolments/{eventId}",new{categoryId});TempData[r.Ok?"Success":"Error"]=r.Ok?"You are enrolled!":r.Body;return RedirectToAction("Details",new{id=eventId});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Cancel(int id){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Delete,$"api/enrolments/{id}");TempData[r.Ok?"Success":"Error"]=r.Ok?"Enrolment cancelled.":r.Body;return RedirectToAction(nameof(Index));}
    public async Task<IActionResult> Results(){if(!Allowed())return Guard();var r=await api.SendAsync(HttpMethod.Get,"api/results/mine");return View(ApiClient.Parse(r.Ok?r.Body:"[]"));}
}
