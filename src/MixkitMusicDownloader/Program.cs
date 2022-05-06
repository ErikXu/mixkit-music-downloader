using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;

namespace MixkitMusicDownloader
{
    internal class Program
    {
        static void Main()
        {
            var root = @"C:\mixkit";

            var genres = new Dictionary<string, string>
            {
                { "ambient", "Ambient" },
                { "cinematic", "Cinematic" },
                { "corporate", "Corporate" },
                { "drum-and-bass", "Drum And Bass" },
                { "experimental", "Experimental" },
                { "funk", "Funk" },
                { "hip-hop", "Hip Hop" },
                { "pop", "Pop" },
                { "percussion", "Percussion" },
                { "children", "Children" },
                { "classical", "Classical" },
                { "country", "Country" },
                { "house-and-electronica", "House And Electronica" },
                { "acoustic", "Acoustic" },
                { "r-and-b", "R&B" },
                { "jazz", "Jazz" },
                { "rock", "Rock" },
                { "trap", "Trap" }
            };

            foreach (var genre in genres)
            {
                var directory = Path.Combine(root, genre.Value);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var musicList = new List<Music>();
                var jsonPath = Path.Combine(directory, "record.json");
                if (File.Exists(jsonPath))
                {
                    var content = File.ReadAllText(jsonPath);
                    musicList = JsonConvert.DeserializeObject<List<Music>>(content);
                }

                var url = $"https://mixkit.co/free-stock-music/{genre.Key}/";
                var htmlWeb = new HtmlWeb();
                var doc = htmlWeb.LoadFromWebAsync(url).Result;
                var countText = doc.DocumentNode.SelectSingleNode("//p[@class='hero-music__description']").InnerText.Trim().Split(' ')[0];

                var count = int.Parse(countText);
                var pageNum = 36;
                var pageCount = count / pageNum;

                if (count % pageNum > 0)
                {
                    pageCount++;
                }

                for (var i = 1; i <= pageCount; i++)
                {
                    url = $"https://mixkit.co/free-stock-music/{genre.Key}/?page={i}";
                    htmlWeb = new HtmlWeb();
                    doc = htmlWeb.LoadFromWebAsync(url).Result;

                    var divs = doc.DocumentNode.SelectNodes("//div[@class='page-music-category__container']/div/div");

                    foreach (var div in divs)
                    {
                        var music = new Music
                        {
                            Date = DateTime.Now.ToString("yyyy-MM-dd"),
                            Title = div.SelectSingleNode("div/div[2]/h4").InnerText.Trim(),
                            Author = div.SelectSingleNode("div/div[2]/p").InnerText.Trim()
                        };

                        music.Description = $"[{genre.Value}]{music.Title} {music.Author}";
                        music.File = $"[{genre.Value}]{music.Title.Replace(" ", "-")}_{music.Author.Replace(" ", "-")}.mp3";

                        var linkPart = div.SelectSingleNode("div/div[2]/div/div[3]/button").Attributes["data-download--button-modal-url-value"].Value;
                        doc = htmlWeb.LoadFromWebAsync($"https://mixkit.co{linkPart}").Result;
                        var downloadUrl = doc.DocumentNode.SelectSingleNode("div").Attributes["data-download--modal-url-value"].Value;
                        DownloadFile(downloadUrl, directory, music.File);
                        Console.WriteLine($"{music.File} downloaded");

                        if (!musicList.Any(n => n.Description.Equals(music.Description, StringComparison.OrdinalIgnoreCase)))
                        {
                            musicList.Add(music);
                        }
                    }
                    Console.WriteLine($"[{genre.Value}]Page-{i} downloaded");
                }

                var newContent = JsonConvert.SerializeObject(musicList, Formatting.Indented);
                File.WriteAllText(jsonPath, newContent);
            }
        }

        private static void DownloadFile(string url, string directory, string fileName)
        {
            var bytes = GetUrlContent(url);
            if (bytes != null)
            {
                File.WriteAllBytes(Path.Combine(directory, fileName), bytes);
            }
        }

        private static byte[] GetUrlContent(string url)
        {
            using var client = new HttpClient();
            var result = client.GetAsync(url).Result;
            return result.IsSuccessStatusCode ? result.Content.ReadAsByteArrayAsync().Result : null;
        }
    }

    internal class Music
    {
        public string Date { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public string File { get; set; }

        public string Description { get; set; }
    }
}
