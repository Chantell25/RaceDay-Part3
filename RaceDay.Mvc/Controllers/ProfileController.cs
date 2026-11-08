using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class ProfileController(ApiClient api):Controller
{
    private bool Allowed()=>HttpContext.Session.GetString("Role") is "Organiser" or "Participant";
    public async Task<IActionResult> Index(){if(!Allowed())return RedirectToAction("Login","Account")!;var r=await api.SendAsync(HttpMethod.Get,"api/profile");if(!r.Ok)return View(new ProfileVm());var p=ApiClient.Parse(r.Body);ViewBag.ProfilePictureUrl=p.TryGetProperty("profilePictureUrl",out var pic)&&pic.ValueKind==System.Text.Json.JsonValueKind.String?pic.GetString():null;return View(new ProfileVm{FirstName=p.GetProperty("firstName").GetString()??"",LastName=p.GetProperty("lastName").GetString()??""});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Index(ProfileVm vm){if(!Allowed())return RedirectToAction("Login","Account")!;if(!ModelState.IsValid)return View(vm);var r=await api.SendAsync(HttpMethod.Put,"api/profile",new{vm.FirstName,vm.LastName});if(vm.Picture is {Length:>0}){var up=await api.UploadAsync("api/profile/picture",vm.Picture);if(!up.Ok)TempData["Error"]="Profile saved, but picture upload failed: "+up.Body;}TempData[r.Ok?"Success":"Error"]=r.Ok?"Profile updated.":r.Body;return View(vm);}
}
