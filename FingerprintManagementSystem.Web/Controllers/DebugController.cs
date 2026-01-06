using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FingerprintManagementSystem.ApiAdapter.Persistence;

public class DebugController : Controller
{
    private readonly LocalAppDbContext _db;
    public DebugController(LocalAppDbContext db) => _db = db;

    [HttpGet("/debug/regions")]
    public async Task<IActionResult> Regions()
    {
        var regions = await _db.Regions.OrderBy(x => x.Id).ToListAsync();
        return Json(regions);
    }
}
