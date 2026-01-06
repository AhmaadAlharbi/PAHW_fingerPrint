using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FingerprintManagementSystem.ApiAdapter.Persistence;

[ApiController]
[Route("api/regions")]
public class RegionsController : ControllerBase
{
    private readonly LocalAppDbContext _db;

    public RegionsController(LocalAppDbContext db)
    {
        _db = db;
    }

    // GET: /api/regions
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var regions = await _db.Regions
            .OrderBy(r => r.Id)
            .Select(r => new
            {
                id = r.Id,
                name = r.Name
            })
            .ToListAsync();

        return Ok(regions);
    }

    // ✅ GET: /api/regions/{regionId}/terminals
    [HttpGet("{regionId:int}/terminals")]
    public async Task<IActionResult> GetRegionTerminals(int regionId)
    {
        var items = await _db.TerminalRegionMaps
            .Where(x => x.RegionId == regionId)
            .OrderBy(x => x.TerminalId)
            .Select(x => new
            {
                terminalId = x.TerminalId,
                regionId = x.RegionId
            })
            .ToListAsync();

        return Ok(items);
    }
}
