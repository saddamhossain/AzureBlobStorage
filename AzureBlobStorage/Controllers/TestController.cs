using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBlobStorage.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Index(IFormFile files)
        {
            int width = 100;
            int height = 100;
            string ext = string.Empty;
            ext = System.IO.Path.GetExtension(files.FileName.ToString()).ToLower();
            var newFile = DateTime.Now.ToString("ddMMyyyyhhmmsstt") + "_" + width + "X" + height + ext;
            // Save the file   
            using (var fileStream = new FileStream(newFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var image = System.Drawing.Image.FromStream(fileStream))
                {
                    Bitmap myImg = new Bitmap(width, height);
                    Graphics myImgGraph = Graphics.FromImage(myImg);
                    myImgGraph.CompositingQuality = CompositingQuality.HighQuality;
                    myImgGraph.SmoothingMode = SmoothingMode.HighQuality;
                    myImgGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    var imgRectangle = new Rectangle(0, 0, width, height);
                    myImgGraph.DrawImage(image, imgRectangle);
                    var path = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Test")).Root + $@"\{newFile}";
                    //var path = Path.Combine(Server.MapPath("~/ResizedImages"), newFile);
                    myImg.Save(path, image.RawFormat);
                }

            }

            return View();
        }
    }
}
