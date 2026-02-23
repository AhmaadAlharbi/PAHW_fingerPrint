using FingerprintManagementSystem.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AllowedUsersController : Controller
{
    private readonly IAllowedUsersAdmin _admin;

    public AllowedUsersController(IAllowedUsersAdmin admin)
    {
        _admin = admin;
    }
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "1";

        if (!isAdmin)
        {
            context.Result = Forbid();
            return;
        }

        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Fetch(int employeeId, CancellationToken ct)
    {
        var dto = await _admin.FetchFromSoapAsync(employeeId, ct);
        if (dto == null)
        {
            TempData["ErrorMsg"] = "لم يتم العثور على الموظف في SOAP.";
            return RedirectToAction("Create");
        }

        return View("Create", dto);
    }

    [HttpPost]
    public async Task<IActionResult> Save(int employeeId, string fullName, string email, string department,
        DateTime? validUntil, bool isAdmin, CancellationToken ct)
    {
        var ok = await _admin.AddAsync(new AllowedUserDto(employeeId, fullName, email, department), validUntil, isAdmin, ct);

        TempData["SuccessMsg"] = ok ? "تمت الإضافة." : "الموظف موجود مسبقًا.";
        return RedirectToAction("Create");
    }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _admin.ListAsync(ct);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int employeeId, bool makeActive, CancellationToken ct)
    {
        if (!int.TryParse(HttpContext.Session.GetString("EmpId"), out var currentEmpId))
        {
            TempData["ErrorMsg"] = "حدث خطأ في الجلسة.";
            return RedirectToAction("Index");
        }

        // ✅ لا تعطّل نفسك
        if (!makeActive && employeeId == currentEmpId)
        {
            TempData["ErrorMsg"] = "لا يمكنك تعطيل نفسك.";
            return RedirectToAction("Index");
        }

        var ok = await _admin.SetActiveAsync(employeeId, makeActive, ct);

        if (!ok)
            TempData["ErrorMsg"] = "لا يمكن تنفيذ العملية (قد يكون آخر مشرف).";
        else
            TempData["SuccessMsg"] = "تم تحديث الحالة بنجاح.";

        return RedirectToAction("Index");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int employeeId, CancellationToken ct)
    {
        if (!int.TryParse(HttpContext.Session.GetString("EmpId"), out var currentEmpId))
        {
            TempData["ErrorMsg"] = "حدث خطأ في الجلسة.";
            return RedirectToAction("Index");
        }

        // ✅ لا تسمح لنفسك تحذف نفسك
        if (employeeId == currentEmpId)
        {
            TempData["ErrorMsg"] = "لا يمكنك حذف نفسك.";
            return RedirectToAction("Index");
        }

        var ok = await _admin.DeleteAsync(employeeId, ct);

        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok
            ? "تم حذف المستخدم."
            : "لا يمكن حذف هذا المستخدم (قد يكون آخر مشرف).";

        return RedirectToAction("Index");
    }



}
