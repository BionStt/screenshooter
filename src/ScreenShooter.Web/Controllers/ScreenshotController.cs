using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ScreenShooter.Gun;
using ScreenShooter.Web.Filters;
using ScreenShooter.Web.Requests;

namespace ScreenShooter.Web.Controllers
{
    [Route("v{version}/screenshot")]
    public class ScreenshotController : ControllerBase
    {
        private readonly ILogger<ScreenshotController> _logger;
        private readonly ShotGun _shooterManager;

        public ScreenshotController(ShotGun shooterManager, 
                                    ILogger<ScreenshotController> logger)
        {
            _shooterManager = shooterManager;
            _logger = logger;
        }

        [HttpGet("img")]
        [ModelValidation]
        public async Task<IActionResult> GetWebPageAsImage([FromRoute] Int32 version, ImageRequest request)
        {
            try
            {
                var cancel = Request.HttpContext.RequestAborted;
                var shot = await _shooterManager.ShotImageAsync(request.ToShotOptions(), cancel);
                return File(shot.Bytes, shot.MimeType, shot.FileName);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error on getting image screen shot");
                return StatusCode(500);
            }
        }
        
        [HttpGet("pdf")]
        [ModelValidation]
        public async Task<IActionResult> GetWebPageAsPdf([FromRoute] Int32 version, PdfRequest request)
        {
            try
            {
                var cancel = Request.HttpContext.RequestAborted;
                var shot = await _shooterManager.ShotPdfAsync(request.ToShotOptions(), cancel);
                return File(shot.Bytes, shot.MimeType, shot.FileName);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error on getting pdf screen shot");
                return StatusCode(500);
            }
        }
    }
}