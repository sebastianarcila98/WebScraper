using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using PuppeteerSharp;

class Program
{
    static async Task Main(string[] args)
    {
        // The address to search for
        var address = "10007 N 10th St, Tampa, FL 33612"; // Replace with the desired address

        // Download Chromium
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        // Launch the browser
        using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = false }))
        {
            // Open a new page
            using (var page = await browser.NewPageAsync())
            {
                // Navigate to Zillow
                await page.GoToAsync("https://www.realtor.com/");

                // Wait for the search bar to be available with an increased timeout
                try
                {
                    await page.WaitForSelectorAsync("input[id='search-bar']", new WaitForSelectorOptions { Timeout = 60000 });
                }
                catch (PuppeteerSharp.WaitTaskTimeoutException)
                {
                    Console.WriteLine("The search bar was not found within the timeout period.");
                    return;
                }

                // Type the address into the search bar and press Enter
                await page.TypeAsync("input[id='search-bar']", address);
                await page.Keyboard.PressAsync("Enter");

                // Wait for the navigation to the property page
                await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });

                // Get the page content
                var content = await page.GetContentAsync();
                Console.WriteLine(content);

                // Load the content into HtmlAgilityPack
                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                // Extract the first image URL using HtmlAgilityPack
                var imageNode = doc.DocumentNode.SelectSingleNode("//img[class='carousel-photo')]");
                if (imageNode != null)
                {
                    var imageUrl = imageNode.GetAttributeValue("src", string.Empty);
                    Console.WriteLine($"Image URL: {imageUrl}");

                    // Download the image
                    await DownloadImageAsync(imageUrl);
                }
                else
                {
                    Console.WriteLine("Image not found.");
                }
            }
        }
    }

    static async Task DownloadImageAsync(string imageUrl)
    {
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            // Get the file name from the URL
            var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);

            // Save the image to the local file system
            await File.WriteAllBytesAsync(fileName, imageBytes);
            Console.WriteLine($"Downloaded {fileName}");
        }
    }
}
