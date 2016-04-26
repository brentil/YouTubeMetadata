using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

using System.IO;
using System.Reflection;
using System.Threading;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace YouTubeMetadata
{
    internal class SearchMetadata
    {
        public static class ymGlobals
        {
            public static string ymAPIKey = "NoAPI";
            public static string ymScanLocation = "D:\\YouTube\\";
            public static string ymLogLocation = "D:\\YouTube\\";
            public static bool ymDisableMove = true;
        }

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("YouTubeMetadata Search");
            Console.WriteLine("======================");

            try
            {
                ReadAllSettings();

                string[] thisVideoFileArray = Directory.GetFiles(@ymGlobals.ymScanLocation);

                SearchMetadata thisSearch = new SearchMetadata();
                foreach (string thisVideo in thisVideoFileArray)
                {

                    thisSearch.Run(thisVideo).Wait();
                }
                
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("==========================");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }


        private async Task Run(string videoTitle)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = ymGlobals.ymAPIKey,
                ApplicationName = this.GetType().ToString()
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = videoTitle;
            searchListRequest.MaxResults = 3;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();
            //List<string> channels = new List<string>();
            //List<string> playlists = new List<string>();

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
                        //channels.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.ChannelId));
                        break;

                    case "youtube#playlist":
                        //playlists.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.PlaylistId));
                        break;
                }
            }

            Console.WriteLine(String.Format("Videos:\n{0}\n", string.Join("\n", videos)));
            //Console.WriteLine(String.Format("Channels:\n{0}\n", string.Join("\n", channels)));
            //Console.WriteLine(String.Format("Playlists:\n{0}\n", string.Join("\n", playlists)));
        }

        static void ReadAllSettings()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                ymGlobals.ymAPIKey = appSettings["APIKey"] ?? "NotFound";
                ymGlobals.ymScanLocation = appSettings["ScanLocation"] ?? "NotFound";
                ymGlobals.ymLogLocation = appSettings["LogLocation"] ?? "NotFound";
                ymGlobals.ymDisableMove = Convert.ToBoolean(appSettings["DisableMove"]);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }
        }

    }
}
