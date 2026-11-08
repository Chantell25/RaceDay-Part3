using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class AccountController(ApiClient api):Controller
{
    [HttpGet] public IActionResult Login()=>View(new LoginVm());
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginVm vm)
    {
        if(!ModelState.IsValid)return View(vm);
        var r=await api.LoginAsync(vm.Email,vm.Password);
        if(!r.Ok){ModelState.AddModelError("","Email or password is incorrect.");return View(vm);}
        var u=ApiClient.Parse(r.Body); HttpContext.Session.SetInt32("UserId",u.GetProperty("userId").GetInt32());
        HttpContext.Session.SetString("Role",u.GetProperty("role").GetString()??"");
        HttpContext.Session.SetString("Email",vm.Email);
        return RedirectToAction("Index", "Home");
    }
    [HttpGet] public IActionResult Register()=>View(new RegisterVm());
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterVm vm)
    {
        if(vm.Role=="Organiser"&&(string.IsNullOrWhiteSpace(vm.OrganisationName)||string.IsNullOrWhiteSpace(vm.ContactNumber))) ModelState.AddModelError("OrganisationName","Organisation name and contact number are required.");
        if(!ModelState.IsValid)return View(vm);
        var payload=new {vm.FirstName,vm.LastName,vm.Email,vm.Password,vm.Role,vm.OrganisationName,vm.ContactNumber,vm.DateOfBirth,EmergencyContactName=(string?)null,EmergencyContactNumber=(string?)null};
        var r=await api.SendAsync(HttpMethod.Post,"api/auth/register",payload);
        if(!r.Ok){ModelState.AddModelError("", "Registration could not be completed. "+r.Body);return View(vm);}
        TempData["Success"]="Account created. Please log in."; return RedirectToAction(nameof(Login));
    }
    public async Task<IActionResult> Logout(){await api.SendAsync(HttpMethod.Post,"api/auth/logout");api.ClearSession();return RedirectToAction("Index","Home");}
}
