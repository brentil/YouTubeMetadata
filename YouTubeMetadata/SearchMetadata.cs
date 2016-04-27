using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Net;
using System.Configuration;

using System.IO;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace YouTubeMetadata
{
    internal class SearchMetadata
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static class ymGlobals
        {
            public static string    ymAPIKey        = "NoAPI";
            public static string    ymScanLocation  = "NoLocation";
            public static string[]  ymVideoExt      = { "NoExt" };
            public static bool      ymDisableMove   = true;
        }


        [STAThread]
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            Console.WriteLine("YouTubeMetadata Search");
            Console.WriteLine("======================");

            try
            {
                ReadAllSettings();

                string[] thisVideoFileArray = Directory.GetFiles(@ymGlobals.ymScanLocation);

                SearchMetadata thisSearch = new SearchMetadata();
                foreach (string thisVideo in thisVideoFileArray)
                {
                    if (isVideoFile(thisVideo))
                    {
                        Console.WriteLine("Old file name: " + thisVideo);
                        log.Info("Old file name: " + thisVideo);
                        thisSearch.Run(thisVideo).Wait();
                        Console.WriteLine("");
                    }
                }
                
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                    log.Error(e.Message);
                }
            }

            Console.WriteLine("==========================");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }


        private async Task Run(string thisVideo)
        {

            string thisSearchTerm;

            thisSearchTerm = Path.GetFileNameWithoutExtension(thisVideo);

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = ymGlobals.ymAPIKey,
                ApplicationName = this.GetType().ToString()
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = thisSearchTerm;
            searchListRequest.MaxResults = 1;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                        break;

                    case "youtube#channel":
                        break;

                    case "youtube#playlist":
                        break;
                }
            }

            Console.WriteLine(String.Format("Search Result: {0}", string.Join("\n", videos)));
            log.Info(String.Format("Search Result: {0}", string.Join("\n", videos)));

            if(searchListResponse.Items.Count > 0)
            {
                if(searchListResponse.Items[0].Id.Kind.Contains("youtube#video"))
                {
                    string newChannelFolder = ymGlobals.ymScanLocation 
                        + "\\" 
                        + searchListResponse.Items[0].Snippet.ChannelTitle.ToString();

                    string newFileName = searchListResponse.Items[0].Snippet.ChannelTitle
                        + " - "
                        + searchListResponse.Items[0].Snippet.PublishedAtRaw.Remove(searchListResponse.Items[0].Snippet.PublishedAtRaw.IndexOf("T"))
                        + " - "
                        + thisSearchTerm
                        + Path.GetExtension(thisVideo);

                    Directory.CreateDirectory(newChannelFolder);

                    newFileName = newFileName.Replace("_", "");

                    if(ymGlobals.ymDisableMove)
                    {
                        Console.WriteLine("New file name: " + newChannelFolder + "\\" + newFileName);
                        log.Info("New file name: " + newChannelFolder + "\\" + newFileName);
                        File.Copy(thisVideo, newChannelFolder + "\\" + newFileName);
                    }
                    else
                    {
                        Console.WriteLine("New file name: " + newChannelFolder + "\\" + newFileName);
                        log.Info("New file name: " + newChannelFolder + "\\" + newFileName);
                        File.Move(thisVideo, newChannelFolder + "\\" + newFileName);
                    }

                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(searchListResponse.Items[0].Snippet.Thumbnails.High.Url, @newChannelFolder + "\\channelBanner.jpg");
                    }

                    TagLib.File fileToTag = null;

                    try
                    {
                        fileToTag = TagLib.File.Create(newChannelFolder + "\\" + newFileName);

                        // Set comment tag
                        fileToTag.Tag.Comment = searchListResponse.Items[0].Snippet.Description.ToString();
                        fileToTag.Save();

                        // Check if comment tag set
                        fileToTag = TagLib.File.Create(newChannelFolder + "\\" + newFileName);

                        if (fileToTag.Tag.Comment != searchListResponse.Items[0].Snippet.Description.ToString())
                            throw new Exception("Could not set comment tag. This file format is not supported.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        log.Error(ex.Message);
                    }
                    finally // Always called, even when throwing in Exception block
                    {
                        if (fileToTag != null) fileToTag.Dispose(); // Clean up
                    }

                }

            }
            
        }

        private static void ReadAllSettings()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                ymGlobals.ymAPIKey = appSettings["APIKey"] ?? "NotFound";
                ymGlobals.ymScanLocation = appSettings["ScanLocation"] ?? "NotFound";
                ymGlobals.ymVideoExt = appSettings["VideoExtensions"].Split(',');
                ymGlobals.ymDisableMove = Convert.ToBoolean(appSettings["DisableMove"]);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                log.Error("Error reading app settings");
            }
        }


        private static bool isVideoFile(string thisVideo)
        {
            return -1 != Array.IndexOf(ymGlobals.ymVideoExt, Path.GetExtension(thisVideo).ToUpperInvariant());
        }

    }
}
