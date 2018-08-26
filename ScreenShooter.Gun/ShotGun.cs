using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ImageMagick;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using ScreenShooter.Gun.Core;
using ScreenShooter.Gun.Extensions;
using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;

namespace ScreenShooter.Gun
{
    public class ShotGun : IDisposable
    {
        private const Int32 ScrollWidthOffset = 15;
        private const Int32 MenuHeightOffset = 110;

        private readonly RemoteWebDriver _remoteWebDriver;
        private readonly IHttpClientFactory _httpClientFactory;


        public ShotGun(IHttpClientFactory httpClientFactory, GunOptions options)
        {
            _httpClientFactory = httpClientFactory;
            _remoteWebDriver = new RemoteWebDriver(new Uri(options.Host),
                                                   new ChromeOptions());
        }

        public async Task<ImageShot> ShotImageAsync(ShotOptions options,
                                                    CancellationToken cancel = default)
        {
            var url = await TryGetMobileVersionAsync(options, cancel);
            _remoteWebDriver.Navigate().GoToUrl(url);
            
            var fullPageImage = GetFullPageImage(options);
            if (fullPageImage == null)
            {
                return null;
            }

            return new ImageShot(fullPageImage, _remoteWebDriver.Title, options.ImageFormat);
        }

        public async Task<ShotResult> ShotPdfAsync(ShotOptions options,
                                                   CancellationToken cancel = default)
        {
            var url = await TryGetMobileVersionAsync(options, cancel);
            _remoteWebDriver.Navigate().GoToUrl(url);

            var fullPageImage = GetFullPageImage(options);

            if (fullPageImage == null ||
                cancel.IsCancellationRequested)
            {
                return null;
            }

            return GetPdfDocument(fullPageImage, options);
        }

        private PdfShot GetPdfDocument(Image fullPageImage, ShotOptions options)
        {
            var magicImage = new MagickImage(fullPageImage.ToMemoryStream());
            magicImage.Strip();

            if (options.IsGrayscale)
            {
                magicImage.Grayscale(PixelIntensityMethod.Lightness);
                magicImage.Contrast();
            }

            var partsCount = Math.Ceiling((Decimal) magicImage.Height / options.Height);
            partsCount = Math.Ceiling((magicImage.Height + options.OverlaySize * partsCount) / options.Height);

            var pageImageParts = new List<IMagickImage>();
            for (var i = 0; i < partsCount; i++)
            {
                var y = i * options.Height - i * options.OverlaySize;
                var images = magicImage.CropToTiles(new MagickGeometry(0,
                                                                       y,
                                                                       options.Width,
                                                                       options.Height))
                                       .ToList();
                pageImageParts.Add(images.First());
            }

            var document = new Document(new iTextSharp.text.Rectangle(options.Width, options.Height), 0, 0, 0, 0);

            Byte[] documentBytes;
            using (var documentStream = new MemoryStream())
            {
                var pdf = new PdfCopy(document, documentStream);
                document.Open();

                foreach (var pageImagePart in pageImageParts)
                {
                    document.NewPage();
                    var imageDocument = new Document(new iTextSharp.text.Rectangle(options.Width, options.Height), 0, 0, 0, 0);

                    using (var imageStream = new MemoryStream())
                    {
                        var imageDocumentWriter = PdfWriter.GetInstance(imageDocument, imageStream);
                        imageDocument.Open();
                        if (!imageDocument.NewPage())
                        {
                            throw new Exception("Unable add page");
                        }

                        var image = iTextSharp.text.Image.GetInstance(pageImagePart.ToByteArray());
                        image.Alignment = Element.ALIGN_TOP;
                        image.ScaleToFitHeight = true;
                        image.ScaleToFit(options.Width, options.Height);

                        if (!imageDocument.Add(image))
                        {
                            throw new Exception("Unable add image");
                        }

                        imageDocument.Close();
                        imageDocumentWriter.Close();

                        var imageDocumentReader = new PdfReader(imageStream.ToArray());
                        var page = pdf.GetImportedPage(imageDocumentReader, 1);
                        pdf.AddPage(page);
                        imageDocumentReader.Close();
                    }
                }

                if (document.IsOpen())
                {
                    document.Close();
                }

                documentBytes = documentStream.ToArray();
            }

            return new PdfShot(documentBytes, _remoteWebDriver.Title);
        }

        private Image GetFullPageImage(ShotOptions options)
        {
            var driver = (IWebDriver) _remoteWebDriver;
            driver.Manage().Window.Size = new Size(options.Width + ScrollWidthOffset,
                                                   options.Height + MenuHeightOffset);
            driver.Manage().Window.Position = new Point(0, 0);
            var jsExecutor = (IJavaScriptExecutor) driver;

            LoadAllPageHeight(jsExecutor);

            var totalHeight = (Int32) (Int64) jsExecutor.ExecuteScript("return document.body.parentNode.scrollHeight");
            var viewportWidth = (Int32) (Int64) jsExecutor.ExecuteScript("return document.body.clientWidth");
            var viewportHeight = (Int32) (Int64) jsExecutor.ExecuteScript("return window.innerHeight");

            var rectangles = new List<Rectangle>();
            for (var yScroll = 0; yScroll < totalHeight; yScroll += viewportHeight)
            {
                var rectangleHeight = viewportHeight;
                if (yScroll + viewportHeight > totalHeight)
                {
                    rectangleHeight = totalHeight - yScroll;
                }

                var currRect = new Rectangle(0, yScroll, viewportWidth, rectangleHeight);
                rectangles.Add(currRect);
            }

            var takerScreenshot = (ITakesScreenshot) driver;
            var fullPageImage = new Bitmap(viewportWidth, totalHeight);
            var graphics = Graphics.FromImage(fullPageImage);

            var yPosition = 0;
            for (var rectangleIndex = 0; rectangleIndex < rectangles.Count; rectangleIndex++)
            {
                jsExecutor.ExecuteScript($"window.scroll(0, {yPosition.ToString()})");
                var rectangle = rectangles[rectangleIndex];

                var screenshot = takerScreenshot.GetScreenshot();
                var screenshotImage = ScreenshotToImage(screenshot);

                var sourceRectangle = new Rectangle(0,
                                                    viewportHeight - rectangle.Height,
                                                    rectangle.Width,
                                                    rectangle.Height);

                graphics.DrawImage(screenshotImage, rectangle, sourceRectangle, GraphicsUnit.Pixel);

                yPosition = rectangle.Bottom;
                if (rectangleIndex == 0)
                {
                    jsExecutor.ExecuteScript("(function(){x=document.querySelectorAll('*');for(i=0;i<x.length;i++){elementStyle=getComputedStyle(x[i]);if(elementStyle.position==\"fixed\"||elementStyle.position==\"sticky\"){x[i].style.opacity=\"0\";}}}())");
                }
            }

            return fullPageImage;
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

            var client = _httpClientFactory.CreateClient();
            var result = await client.GetAsync(mobileUrl, cancel);

            return result.IsSuccessStatusCode
                       ? mobileUrl
                       : options.Uri.OriginalString;
        }

        public void Dispose()
        {
            _remoteWebDriver?.Quit();
            _remoteWebDriver?.Dispose();
        }
    }
}