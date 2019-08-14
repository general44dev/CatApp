using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CatApp.ViewModels
{
    public class ProdVM
    {
        public ProdVM(string id, string productName, decimal stock, string category, string description, string pId, byte[] content, string untrustedName, string note, long size, DateTime uploadDT)
        {
            Id = id;
            ProductName = productName;
            Stock = stock;
            Category = category;
            Description = description;
            PId = pId;
          //  Content = content;
            Base64String = "data:image/png;base64," + Convert.ToBase64String(content, 0, content.Length);
            UntrustedName = untrustedName;
            Note = note;
            Size = size;
            UploadDT = uploadDT;
        }

        [Display(Name = "ID")]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Producto")]
        [StringLength(50, MinimumLength = 0)]
        public string ProductName { get; set; }

        [Required]
        [Display(Name = "Stock")]
        public decimal Stock { get; set; }

        [Display(Name = "Categoria")]
        [StringLength(50, MinimumLength = 0)]
        public string Category { get; set; }

        [Display(Name = "Descrpcion")]
        [StringLength(100, MinimumLength = 0)]
        public string Description { get; set; }

        public string PId { get; set; }

        [Display(Name = "Imagen")]
        public byte[] Content { get; set; }

        public string Base64String { get; set; }

        [Display(Name = "Nombre de la Imagen")]
        public string UntrustedName { get; set; }

        [Display(Name = "Nota de la Imagen")]
        public string Note { get; set; }

        [Display(Name = "Tamaño de la Imagen")]
        public long Size { get; set; }

        [Display(Name = "Release Date")]
        [DataType(DataType.Date)]
        public DateTime UploadDT { get; set; }
    }

}

