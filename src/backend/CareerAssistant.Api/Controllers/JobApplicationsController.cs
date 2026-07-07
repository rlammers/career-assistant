using CareerAssistant.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareerAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobApplicationsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<JobApplication>> Get()
    {
        return Ok(Array.Empty<JobApplication>());
    }
}