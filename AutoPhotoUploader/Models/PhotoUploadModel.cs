using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPhotoUploader.Models
{
    public class PhotoUploadModel
    {
        public string? FileName { get; set; }
        public byte[]? fileData { get; set; }
    }
}
