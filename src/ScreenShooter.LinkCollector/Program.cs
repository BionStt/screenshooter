using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ScreenShooter.Gun;
using ScreenShooter.Gun.Core;

namespace ScreenShooter.LinkCollector
{
    class Program
    {
        private const String DefaultFileName = "export-list.txt";
        private const String DefaultExportFolder = "links-export";
        private const String DefaultFileCompletePrefix = "complete";
        private const String DefaultSeleniumHost = "http://localhost:4444";

        static void Main(String[] args)
        {
            var seleniumHost = args.Length > 0
                                   ? args[0]
                                   : DefaultSeleniumHost;
            
            var fileName = args.Length > 1
                               ? args[1]
                               : DefaultFileName;
            
            var directoryName = args.Length > 2
                                    ? args[2]
                                    : DefaultExportFolder;

            Console.WriteLine($"Start export pdfs for {fileName}");

            CollectPdfs(seleniumHost, fileName, directoryName).GetAwaiter().GetResult();

            Console.WriteLine("Done");
        }

        static async Task CollectPdfs(String seleniumHost, String fileName, String exportDirectory)
        {
            if (!Directory.Exists(exportDirectory))
            {
                Directory.CreateDirectory(exportDirectory);
            }

            var service = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            var gun = new ShotGun(service.GetService<IHttpClientFactory>(),
                                  new GunOptions($"{seleniumHost}/wd/hub"));


            var completeFileName = $"{DefaultFileCompletePrefix}-{fileName}";

            var completeLinks = new String[0];
            if (File.Exists(completeFileName))
            {
                completeLinks = await File.ReadAllLinesAsync(completeFileName);
            }
            else
            {
                File.Create(completeFileName);
            }

            var links = await File.ReadAllLinesAsync(fileName);
            links = links.Distinct()
                         .Except(completeLinks)
                         .ToArray();

            var options = new ShotOptions
                          {
                              Width = 754,
                              StepHeight = 1024,
                              ImageFormat = "png",
                              IsGrayscale = true,
                              OverlaySize = 20,
                              TryMobile = true,
                              HideOverlayElementsImmediate = true
                          };

            var index = 1;

            foreach (var link in links)
            {
                try
                {
                    options.Uri = new Uri(link);

                    var shot = await gun.ShotPdfAsync(options);
                    if (File.Exists($"{exportDirectory}/{shot.FileName}"))
                    {
                        Console.WriteLine($"Skip exist file : {shot.FileName}");
                        await File.AppendAllLinesAsync(completeFileName, new List<String> {link});
                        continue;
                    }

                    await File.WriteAllBytesAsync($"{exportDirectory}/{shot.FileName}", shot.Bytes);
                    await File.AppendAllLinesAsync(completeFileName, new List<String> {link});
                    
                    Console.WriteLine($"{shot.FileName} is exported. {index++.ToString()}/{links.Length.ToString()}");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }
}