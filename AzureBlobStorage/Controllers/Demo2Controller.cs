using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureBlobStorage.Models;
using LazZiya.ImageResize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobStorage.Controllers
{
    //This controller using Azure.Storage.Blobs package
    public class Demo2Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly DatabaseContext _context;
        const string SessionKeyName = "_blobName";

        public Demo2Controller(IConfiguration configuration, BlobServiceClient blobServiceClient, DatabaseContext context)
        {
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(IFormFile files)
        {
            if (files != null)
            {
                if (files.Length > 0)
                {
                    Files obj = new Files();

                    // file save in project folder(wwwroot/Images) Start
                    var fileName = Path.GetFileName(files.FileName);     
                    var uniqueFileName = Convert.ToString(Guid.NewGuid());
                    var fileExtension = Path.GetExtension(fileName).ToLower();
                    var new_file_name = string.Concat(uniqueFileName, fileExtension);

                    // Checking Start
                    var supportedTypes = new[] { ".png", ".jpg", ".jpeg" };
                    long FileSize = files.Length;

                    if (!supportedTypes.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "File Extension Is InValid - Please upload the impage picture jpg/jpeg/png File";
                        return RedirectToAction("Create", "Demo2");
                    }
                    else if (FileSize > 1048576) // 1 MB
                    {
                        TempData["ErrorMessage"] = "File size Should Be UpTo 1 MB";
                        return RedirectToAction("Create", "Demo2");
                    }
                    // Checking End

                    var filepath = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images")).Root + $@"\{new_file_name}";
                    using (FileStream fs = System.IO.File.Create(filepath))
                    {
                        files.CopyTo(fs);
                        fs.Flush();
                    }
                    // file save in project folder(wwwroot/Images) End

                    #region Azure Blob Storage upload task start
                    var containerClient = _blobServiceClient.GetBlobContainerClient("products");
                    var blobClient = containerClient.GetBlobClient(new_file_name);
                    await blobClient.UploadAsync(filepath, new BlobHttpHeaders { ContentType = filepath.GetContentType() });
                    #endregion


                    // Imgae resize start
                    var filepath_resize = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ImagesResize")).Root + new_file_name;
                    var get_image_in_file = System.Drawing.Image.FromFile(filepath);
                    var scale_image = ImageResize.Scale(get_image_in_file, 200, 200);
                    scale_image.Save(filepath_resize);
                    // Imgae resize end

                    #region Azure Blob Storage upload task start for resize
                    var containerClient_resize = _blobServiceClient.GetBlobContainerClient("products-thumbnail");
                    var blobClient_resize = containerClient_resize.GetBlobClient(new_file_name);
                    await blobClient_resize.UploadAsync(filepath_resize, new BlobHttpHeaders { ContentType = filepath.GetContentType() });
                    #endregion


                    // Save in database start
                    var Width = 0;
                    var Height = 0;
                    var HorizontalResolution = 0.0;
                    var VerticalResolution = 0.0;
                    using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var image = System.Drawing.Image.FromStream(fileStream, false, false))
                        {
                            Width = image.Width;
                            Height = image.Height;
                            HorizontalResolution = image.HorizontalResolution;
                            VerticalResolution = image.VerticalResolution;
                        }
                    }

                    using (var target = new MemoryStream())
                    {
                        files.CopyTo(target);
                        obj.DataFiles = target.ToArray();

                        obj.DocumentId = 0;
                        obj.FileName = new_file_name;
                        obj.FileType = fileExtension;
                        obj.LocalFilePath = filepath;
                        obj.Width = Width;
                        obj.Height = Height;
                        obj.HorizontalResolution = HorizontalResolution;
                        obj.VerticalResolution = VerticalResolution;
                        obj.PrimaryUri = blobClient.Uri.AbsoluteUri.ToString();
                        obj.CreatedOn = DateTime.Now;
                    }
                    await _context.Files.AddAsync(obj);
                    await _context.SaveChangesAsync();
                    // save in database end
                    TempData["SuccessMessage"] = "Image successfully Uploaded!";
                }
            }
            return View();
        }


        [HttpGet]
        public IActionResult Update(string blobName)
        {
            HttpContext.Session.SetString(SessionKeyName, blobName);
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Update(IFormFile files)
        {
            if (files != null)
            {
                if (files.Length > 0)
                {
                    Files obj = new Files();

                    var fileName = Path.GetFileName(files.FileName);
                    var fileExtension = Path.GetExtension(fileName).ToLower();

                    // Checking Start
                    var supportedTypes = new[] { ".png", ".jpg", ".jpeg" };
                    long FileSize = files.Length;

                    if (!supportedTypes.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "File Extension Is InValid - Please upload the impage picture jpg/jpeg/png File";
                        return RedirectToAction("Create", "Demo");
                    }
                    else if (FileSize > 1048576) // 1 MB
                    {
                        TempData["ErrorMessage"] = "File size Should Be UpTo 1 MB";
                        return RedirectToAction("Create", "Demo");
                    }

                    // Checking End
                    var blobName = HttpContext.Session.GetString(SessionKeyName).ToString();
                    var filepath = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images")).Root + blobName;
                    using (FileStream fs = System.IO.File.Create(filepath))
                    {
                        files.CopyTo(fs);
                        fs.Flush();
                    }

                    var updated_filepath = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images")).Root + blobName;
                    var containerClient = _blobServiceClient.GetBlobContainerClient("products");
                    var blobClient = containerClient.GetBlobClient(blobName);
                    await blobClient.UploadAsync(updated_filepath, new BlobHttpHeaders { ContentType = updated_filepath.GetContentType() });


                    // Update in database start
                    var Width = 0;
                    var Height = 0;
                    var HorizontalResolution = 0.0;
                    var VerticalResolution = 0.0;
                    using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var image = System.Drawing.Image.FromStream(fileStream, false, false))
                        {
                            Width = image.Width;
                            Height = image.Height;
                            HorizontalResolution = image.HorizontalResolution;
                            VerticalResolution = image.VerticalResolution;
                        }
                    }

                    var exisiting_file_name = await _context.Files.Where(s => s.FileName == blobName).FirstOrDefaultAsync();


                    using (var target = new MemoryStream())
                    {
                        files.CopyTo(target);
                        exisiting_file_name.DataFiles = target.ToArray();

                        // obj.DocumentId = 0;
                        exisiting_file_name.FileName = blobName;
                        exisiting_file_name.FileType = fileExtension;
                        exisiting_file_name.LocalFilePath = filepath;
                        exisiting_file_name.Width = Width;
                        exisiting_file_name.Height = Height;
                        exisiting_file_name.HorizontalResolution = HorizontalResolution;
                        exisiting_file_name.VerticalResolution = VerticalResolution;
                        exisiting_file_name.PrimaryUri = blobClient.Uri.AbsoluteUri.ToString();
                        exisiting_file_name.UpdatedOn = DateTime.Now;
                    }

                    await _context.SaveChangesAsync(true);
                    // Update in database end
                    TempData["SuccessMessage"] = "Image successfully Updated!";
                    HttpContext.Session.SetString(SessionKeyName, "");
                }
            }
            return View();
        }


        public IActionResult ShowSingleBlob(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("products");
            var blobClient = containerClient.GetBlobClient(blobName);
            var imageFullPath = blobClient.Uri.ToString();
            ViewBag.LatestImage = Convert.ToString(imageFullPath);
            return View();
        }

        public async Task<IActionResult> ShowAllBlobs()
        {
            List<FileData> fileList = new List<FileData>();
            var containerClient = _blobServiceClient.GetBlobContainerClient("products");


            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);

                fileList.Add(new FileData()
                {
                    FileName = blobItem.Name,
                    ImageFullPath = blobClient.Uri.ToString(),
                    FileSize = blobItem.Properties.ContentLength.ToString(),
                    ModifiedOn = DateTime.Parse(blobItem.Properties.LastModified.ToString()).ToLocalTime().ToString()
                });

            }
            return View(fileList);
        }


        public async Task<IActionResult> Download(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("products");
            var blobClient = containerClient.GetBlobClient(blobName);
            var blobDownloadInfo = await blobClient.DownloadAsync();

            Stream blobStream = blobClient.OpenReadAsync().Result;
            return File(blobStream, blobDownloadInfo.Value.ContentType, blobClient.Name);
        }


        public async Task<IActionResult> Delete(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("products");
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();

            var exisiting_file_name = await _context.Files.Where(s => s.FileName == blobName).FirstOrDefaultAsync();
            _context.Remove(exisiting_file_name);
            await _context.SaveChangesAsync();

            var filepath_with_name = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images")).Root + $@"\{blobName}";
            if (System.IO.File.Exists(filepath_with_name))
            {
                System.IO.File.Delete(filepath_with_name);
            }

            TempData["SuccessMessage"] = "Image successfully Deleted!";
            return RedirectToAction("ShowAllBlobs", "Demo2");
        }

    }
}
