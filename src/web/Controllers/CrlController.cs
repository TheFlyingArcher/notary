using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Notary.Interface.Service;

namespace Notary.Web.Controllers
{
    /// <summary>
    /// Publishes the certificate revocation list of each certificate authority (RFC 5280).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CrlController(ICertificateRevokeService certificateRevokeService) : ControllerBase
    {
        private const string CrlContentType = "application/pkix-crl";

        // How long caches may reuse a response. A revocation becomes visible to clients within this window.
        private const int MaxAgeSeconds = 3600;

        [HttpGet("{caSlug}"), HttpGet("{caSlug}.crl")]
        [AllowAnonymous]
        [EnableRateLimiting("crl")]
        [Produces(CrlContentType)]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCrl(string caSlug, CancellationToken cancellationToken)
        {
            var result = await certificateRevokeService.GetCrlAsync(caSlug, cancellationToken);
            if (!result.IsSuccess)
            {
                // Don't reveal whether the slug exists, is not a CA, or has an unusable certificate
                return NotFound();
            }

            var crl = result.Value;
            Response.Headers.CacheControl = $"public, max-age={MaxAgeSeconds}";

            return File(
                crl.Data,
                CrlContentType,
                new DateTimeOffset(crl.ThisUpdate, TimeSpan.Zero),
                new EntityTagHeaderValue($"\"{crl.Number}\""));
        }
    }
}
