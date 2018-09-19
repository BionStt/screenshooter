using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using ScreenShooter.Gun.Core;
using ScreenShooter.Gun.Extensions;
using ScreenShooter.Gun.Pdf.Contracts;
using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;

namespace ScreenShooter.Gun
{
    public class ShotGun
    {
        private const Int32 ScrollWidthOffset = 15;
        private const Int32 MenuHeightOffset = 110;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPdfCreator _pdfCreator;
        private readonly GunOptions _options;

        public ShotGun(IHttpClientFactory httpClientFactory,
                       IPdfCreator pdfCreator,
                       GunOptions options)
        {
            _httpClientFactory = httpClientFactory;
            _pdfCreator = pdfCreator;
            _options = options;
        }

        public async Task<ImageShot> ShotImageAsync(ShotOptions options,
                                                    CancellationToken cancel = default)
        {
            var url = await TryGetMobileVersionAsync(options, cancel);

            var driver = new RemoteWebDriver(new Uri(_options.Host), new ChromeOptions());

            try
            {
                driver.Navigate().GoToUrl(url);
                var title = driver.Title;

                var fullPageImage = GetFullPageImage(driver, options);

                if (fullPageImage == null ||
                    cancel.IsCancellationRequested)
                {
                    return null;
                }

                return new ImageShot(fullPageImage, title, options.ImageFormat);
            }
            finally
            {
                driver.Close();
                driver.Dispose();
            }
        }

        public async Task<ShotResult> ShotPdfAsync(ShotOptions options,
                                                   CancellationToken cancel = default)
        {
            var url = await TryGetMobileVersionAsync(options, cancel);
            var driver = new RemoteWebDriver(new Uri(_options.Host), new ChromeOptions());
            try
            {
                driver.Navigate().GoToUrl(url);
                var title = driver.Title;

                var fullPageImage = GetFullPageImage(driver, options);


                if (fullPageImage == null ||
                    cancel.IsCancellationRequested)
                {
                    return null;
                }

                return GetPdfDocument(fullPageImage, options, title);
            }
            finally
            {
                driver.Close();
                driver.Dispose();
            }
        }

        private PdfShot GetPdfDocument(Image fullPageImage, ShotOptions options, String title)
        {
            var magicImage = new MagickImage(fullPageImage.ToMemoryStream());
            magicImage.Strip();

            if (options.IsGrayscale)
            {
                magicImage.Grayscale(PixelIntensityMethod.Lightness);
                magicImage.Contrast();
            }

            var partsCount = Math.Ceiling((Decimal) magicImage.Height / options.StepHeight);
            partsCount = Math.Ceiling((magicImage.Height + options.OverlaySize * partsCount) / options.StepHeight);

            var pageImageParts = new List<Byte[]>();
            for (var i = 0; i < partsCount; i++)
            {
                var y = i * options.StepHeight - i * options.OverlaySize;
                var images = magicImage.CropToTiles(new MagickGeometry(0,
                                                                       y,
                                                                       options.Width,
                                                                       options.StepHeight))
                                       .ToList();
                pageImageParts.Add(images.First().ToByteArray());
            }

            var pdfBytes = _pdfCreator.CreateDocument(options.Width, options.StepHeight, pageImageParts);

            return new PdfShot(pdfBytes, GetFileName(title));
        }

        private String GetFileName(String title)
        {
            var regex = new Regex(@"([^\p{L}\s\d\-_\[\]\(\).'])");
            var fileName = regex.Replace(title, String.Empty);

            var regexSpaces = new Regex(@"\s{2,}");
            return regexSpaces.Replace(fileName, " ");
        }

        private void ResizeBrowser(IWebDriver driver, Int32 width, Int32 height)
        {
            driver.Manage().Window.Size = new Size(width, height);
            driver.Manage().Window.Position = new Point(0, 0);
        }

        private Image GetFullPageImage(IWebDriver driver, ShotOptions options)
        {
            ResizeBrowser(driver,
                          options.Width + ScrollWidthOffset,
                          options.StepHeight + MenuHeightOffset);

            var jsExecutor = (IJavaScriptExecutor) driver;

            LoadAllPageHeight(jsExecutor);

            var pageSizes = GetWebPageSizes(jsExecutor);
            if (pageSizes.TotalWidth > pageSizes.ViewportWidth)
            {
                ResizeBrowser(driver,
                              options.Width + ScrollWidthOffset,
                              options.StepHeight + MenuHeightOffset + ScrollWidthOffset);
            }

            var rectangles = new List<Rectangle>();
            for (var yScroll = 0; yScroll < pageSizes.TotalHeight; yScroll += pageSizes.ViewportHeight)
            {
                var rectangleHeight = pageSizes.ViewportHeight;
                if (yScroll + pageSizes.ViewportHeight > pageSizes.TotalHeight)
                {
                    rectangleHeight = pageSizes.TotalHeight - yScroll;
                }

                var currRect = new Rectangle(0, yScroll, options.Width, rectangleHeight);
                rectangles.Add(currRect);
            }

            var takerScreenshot = (ITakesScreenshot) driver;
            var fullPageImage = new Bitmap(options.Width, pageSizes.TotalHeight);
            var graphics = Graphics.FromImage(fullPageImage);

            var yPosition = 0;
            for (var rectangleIndex = 0; rectangleIndex < rectangles.Count; rectangleIndex++)
            {
                jsExecutor.ExecuteScript($"window.scroll(0, {yPosition.ToString()})");

                if (rectangleIndex > 0 ||
                    options.HideOverlayElementsImmediate)
                {
                    HideFloatingElements(jsExecutor);
                }

                var rectangle = rectangles[rectangleIndex];
                var screenshot = takerScreenshot.GetScreenshot();
                var screenshotImage = ScreenshotToImage(screenshot);

                var sourceRectangle = new Rectangle(0,
                                                    pageSizes.ViewportHeight - rectangle.Height,
                                                    rectangle.Width,
                                                    rectangle.Height);

                graphics.DrawImage(screenshotImage, rectangle, sourceRectangle, GraphicsUnit.Pixel);

                yPosition = rectangle.Bottom;
            }

            return fullPageImage;
        }

        private WebPageSizes GetWebPageSizes(IJavaScriptExecutor jsExecutor)
        {
            var totalHeight = (Int32) (Int64) jsExecutor.ExecuteScript("return document.body.parentNode.scrollHeight");
            var totalWidth = (Int32) (Int64) jsExecutor.ExecuteScript("return document.body.parentNode.scrollWidth");
            var viewportWidth = (Int32) (Int64) jsExecutor.ExecuteScript("return document.body.clientWidth");
            var viewportHeight = (Int32) (Int64) jsExecutor.ExecuteScript("return window.innerHeight");

            return new WebPageSizes
                   {
                       TotalHeight = totalHeight,
                       TotalWidth = totalWidth,
                       ViewportWidth = viewportWidth,
                       ViewportHeight = viewportHeight
                   };
        }

        private void HideFloatingElements(IJavaScriptExecutor jsExecutor)
        {
            jsExecutor.ExecuteScript("(function(){x=document.querySelectorAll('*');for(i=0;i<x.length;i++){elementStyle=getComputedStyle(x[i]);if(elementStyle.position==\"fixed\"||elementStyle.position==\"sticky\"){x[i].style.opacity=\"0\";x[i].style.position=\"absolute\";x[i].style.left=\"-99999px\";}}}())");
        }

        private Image ScreenshotToImage(Screenshot screenshot)
        {
            Image screenshotImage;
            using (var memStream = new MemoryStream(screenshot.AsByteArray))
            {
                screenshotImage = Image.FromStream(memStream);
            }

            return screenshotImage;
        }

        private void LoadAllPageHeight(IJavaScriptExecutor jsExecutor)
        {
            var initPageHeight = (Int64) jsExecutor.ExecuteScript("return document.body.parentNode.scrollHeight");
            var totalPageHeightWithOverflow = initPageHeight * 2;
            for (var i = 0; i < totalPageHeightWithOverflow; i += 768)
            {
                jsExecutor.ExecuteScript($"window.scrollTo(0, {i.ToString()})");
                Thread.Sleep(150);
            }

            Thread.Sleep(300);
            jsExecutor.ExecuteScript("window.scrollTo(0,0)");
        }

        private async Task<String> TryGetMobileVersionAsync(ShotOptions options,
                                                            CancellationToken cancel = default)
        {
            if (!options.TryMobile)
            {
                return options.Uri.OriginalString;
            }

            var uriBuilder = new UriBuilder(options.Uri);
            uriBuilder.Host = $"m.{options.Uri.Host}";
            var mobileUrl = uriBuilder.Uri.OriginalString;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var result = await client.GetAsync(mobileUrl, cancel);

                return result.IsSuccessStatusCode
                           ? mobileUrl
                           : options.Uri.OriginalString;
            }
            catch (Exception)
            {
                return options.Uri.OriginalString;
            }
        }
    }
}