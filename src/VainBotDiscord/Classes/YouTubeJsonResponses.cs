using System;
using System.Collections.Generic;

namespace VainBotDiscord.Classes
{
    public class YouTubePlaylistItemsResponse
    {
        public List<YouTubePlaylistItem> Items { get; set; }
    }

    public class YouTubePlaylistItem
    {
        public YouTubeVideoSnippet Snippet { get; set; }
    }

    public class YouTubeVideoSnippet
    {
        public DateTime PublishedAt { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public YouTubeVideoThumbnailWrapper Thumbnails { get; set; }

        public YouTubeResourceId ResourceId { get; set; }
    }

    public class YouTubeVideoThumbnailWrapper
    {
        public YouTubeVideoThumbnail Default { get; set; }

        public YouTubeVideoThumbnail Medium { get; set; }

        public YouTubeVideoThumbnail High { get; set; }

        public YouTubeVideoThumbnail Standard { get; set; }

        public YouTubeVideoThumbnail MaxRes { get; set; }
    }

    public class YouTubeVideoThumbnail
    {
        public string Url { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }

    public class YouTubeResourceId
    {
        public string Kind { get; set; }

        public string VideoId { get; set; }
    }
}
