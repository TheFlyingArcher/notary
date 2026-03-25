using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notary.Interface.Service;

namespace Notary.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CrlController : ControllerBase
{
    public CrlController(ICertificateRevokeService crs)
    {
        CertificateRevokeService = crs;
    }

    public ICertificateRevokeService CertificateRevokeService { get; }

    [Route("{caSlug}")]
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCrl(string caSlug)
    {
        var crl = await CertificateRevokeService.GenerateCrl(caSlug);

        return Ok(crl);
    }
}