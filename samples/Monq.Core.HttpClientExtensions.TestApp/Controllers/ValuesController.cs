using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.TestApp.Controllers;

[Route("api/test")]
public class TestController : Controller
{
    readonly ITestService _service;

    public TestController(ITestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _service.TestApi();

        return Ok(result);
    }
}
