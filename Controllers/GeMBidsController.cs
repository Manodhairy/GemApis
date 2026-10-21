using GemApi.DTOs.Request;
using GemApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace GemApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GeMBidsController : ControllerBase
    {
        private readonly IGeMBidService _service;

        public GeMBidsController(IGeMBidService service)
        {
            _service = service;
        }





        

        

        [HttpGet]
        public async Task<IActionResult> GetBids([FromQuery] BidFilterRequestDto request)
        {
            var result = await _service.GetBidsAsync(request);
            return Ok(result);
        }

        // GET api/gembids/filters
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters([FromQuery] BidFilterRequestDto request)
        {
            var result = await _service.GetFiltersAsync(request);
            return Ok(result);
        }

        // GET api/gembids/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _service.GetDashboardAsync();
            return Ok(result);
        }


        //Xl export endpoint


        [HttpGet("export")]
        public async Task<IActionResult> ExportBids(
          [FromQuery] BidFilterRequestDto request)
        {
            var fileBytes = await _service.ExportBidsAsync(request);

            var fileName =
                $"GeM_Bids_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // GET api/gembids/pdf?bidNumber=GEM/2026/B/7806411&download=false
        [AllowAnonymous]
        [HttpGet("pdf")]
        public async Task<IActionResult> GetBidPdf(
            [FromQuery] string? bidNumber,
            [FromQuery] string? url,
            [FromQuery] bool download = false,
            CancellationToken ct = default)
        {
            var targetIdentifier = !string.IsNullOrWhiteSpace(bidNumber) ? bidNumber : url;
            if (string.IsNullOrWhiteSpace(targetIdentifier))
            {
                return BadRequest(new { message = "Bid number or URL parameter is required." });
            }

            var (fileBytes, fileName, errorMessage) = await _service.GetBidPdfAsync(targetIdentifier, ct);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new { message = errorMessage ?? $"Tender PDF document for '{targetIdentifier}' could not be found or retrieved." });
            }

            var disposition = download ? "attachment" : "inline";
            Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{fileName}\"";

            return File(fileBytes, "application/pdf", download ? fileName : null);
        }

        [HttpGet("{*bidNumber}")]
        public async Task<IActionResult> GetBidDetails(string bidNumber)
        {
            var result = await _service.GetBidDetailsAsync(bidNumber);
            if (result == null)
                return NotFound(new { message = $"Bid '{bidNumber}' was not found." });

            return Ok(result);
        }
    }
}