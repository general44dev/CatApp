using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace CatApp.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Nombre")]
        [BsonElement("Name")]
        public string ProductName { get; set; }

        [Display(Name = "Stock")]
        public decimal Stock { get; set; }

        [Display(Name = "File Name")]
        public string Category { get; set; }

        public string Description { get; set; }

        [BsonElement("Photo")]
        public ProdPhoto ProdPhoto { get; set; }

    }
    public class ProdPhoto
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.Binary)]
        public byte[] Content { get; set; }

        [BsonElement("fuName")]
        public string UntrustedName { get; set; }

        [BsonElement("Note")]
        public string Note { get; set; }

        [BsonElement("Size")]
        [BsonRepresentation(BsonType.Double)]

        public long Size { get; set; }

        [BsonElement("dt")]
        [BsonRepresentation(BsonType.DateTime)]

        public DateTime UploadDT { get; set; }
    }

}