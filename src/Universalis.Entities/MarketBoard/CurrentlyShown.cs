﻿using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard
{
    public class CurrentlyShown
    {
        [BsonElement("itemID")]
        public uint ItemId { get; set; }

        [BsonElement("worldID")]
        public uint WorldId { get; set; }

        [BsonElement("lastUploadTime")]
        public uint LastUploadTimeUnixMilliseconds { get; set; }

        [BsonElement("uploaderID")]
        public string UploadIdHash { get; set; }

        [BsonElement("listings")]
        public List<Listing> Listings { get; set; }

        [BsonElement("recentHistory")]
        public List<Sale> RecentHistory { get; set; }
    }
}