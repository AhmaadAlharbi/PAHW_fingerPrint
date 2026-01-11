using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FingerprintManagementSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILoginApi _login;

        public HomeController(ILoginApi login) => _login = login;
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string empId, string password, CancellationToken ct)
        {
            var result = await _login.LoginAsync(empId, password, ct);

            // فشل
            if (result.ResultCode != 1)
            {
                TempData["ErrorMsg"] = result.Message;   // ✅ نفس المفتاح اللي تقراه Index.cshtml
                return RedirectToAction("Index");
            }

            // نجاح
            TempData["SuccessMsg"] = $"Welcome {result.EmployeeName}"; // ✅ بنعرضه في Index

            // إذا تبي "تسجيل دخول" حقيقي، فعّل Session وخزّن:
            // HttpContext.Session.SetString("SessionKey", result.SessionKey);
            // HttpContext.Session.SetString("EmpName", result.EmployeeName);

            return RedirectToAction("Index", "Employees");

        }
        public IActionResult Logout()
        {
            // حذف كل بيانات الجلسة
            HttpContext.Session.Clear();

            // (اختياري) رسالة
            TempData["SuccessMsg"] = "تم تسجيل الخروج بنجاح";

            // الرجوع لصفحة الدخول
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
