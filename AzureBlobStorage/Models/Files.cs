using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzureBlobStorage.Models
{
    [Table("Files")]
    public class Files
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string LocalFilePath { set; get; }
        public string PrimaryUri { set; get; }
        public byte[] DataFiles { get; set; }
        public int? Width { set; get; }
        public int? Height { set; get; }
        public double? HorizontalResolution { set; get; }
        public double? VerticalResolution { set; get; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsDelete { set; get; }
    }
}
