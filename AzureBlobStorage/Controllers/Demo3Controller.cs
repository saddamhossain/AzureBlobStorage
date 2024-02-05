using Azure.Storage.Blobs;
using AzureBlobStorage.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBlobStorage.Controllers
{
    public class Demo3Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly DatabaseContext _context;
        const string SessionKeyName = "_blobName";


        public Demo3Controller(IConfiguration configuration, BlobServiceClient blobServiceClient, DatabaseContext context)
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

                    var fileName = Path.GetFileName(files.FileName);
                    var uniqueFileName = Convert.ToString(Guid.NewGuid());
                    var fileExtension = Path.GetExtension(fileName).ToLower();
                    var new_file_name = string.Concat(uniqueFileName, fileExtension);

                    #region Checking
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
                    #endregion


                    #region Azure Blob Storage upload task in the products container
                    var containerClient = _blobServiceClient.GetBlobContainerClient("products");
                    var blobClient = containerClient.GetBlobClient(new_file_name);

                    MemoryStream memory = new MemoryStream();
                    await files.CopyToAsync(memory);
                    memory.Position = 0;
                    await blobClient.UploadAsync(memory, true);
                    #endregion

                    #region Image resize and Azure Blob Storage upload task in the products-thumbnail container
                    var containerClient_resize = _blobServiceClient.GetBlobContainerClient("products-thumbnail");
                    var blobClient_resize = containerClient_resize.GetBlobClient(new_file_name);

                    using (var memory_stream = new MemoryStream())
                    {
                        files.CopyTo(memory_stream);
                        obj.DataFiles = memory_stream.ToArray();

                        using (MemoryStream ms = new MemoryStream(obj.DataFiles, 0, obj.DataFiles.Length))
                        {
                            using (Image img = Image.FromStream(ms))
                            {
                                int h = 100;
                                int w = 150;
                                using (Bitmap b = new Bitmap(img, new Size(w, h)))
                                {
                                    using (MemoryStream ms2 = new MemoryStream())
                                    {
                                        b.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                                        obj.DataFiles = ms2.ToArray();
                                        ms2.Position = 0;
                                        await blobClient_resize.UploadAsync(ms2, true);
                                    }
                                }
                            }
                        }
                    }
                    #endregion


                    #region Save in Database
                    var Width = 0;
                    var Height = 0;
                    var HorizontalResolution = 0.0;
                    var VerticalResolution = 0.0;
                    memory.Position = 0;

                    using (var image = System.Drawing.Image.FromStream(memory, false, false))
                    {
                        Width = image.Width;
                        Height = image.Height;
                        HorizontalResolution = image.HorizontalResolution;
                        VerticalResolution = image.VerticalResolution;
                    }

                    using (var target = new MemoryStream())
                    {
                        files.CopyTo(target);
                        obj.DataFiles = target.ToArray();

                        obj.DocumentId = 0;
                        obj.FileName = new_file_name;
                        obj.FileType = fileExtension;
                        obj.LocalFilePath = null;
                        obj.Width = Width;
                        obj.Height = Height;
                        obj.HorizontalResolution = HorizontalResolution;
                        obj.VerticalResolution = VerticalResolution;
                        obj.PrimaryUri = blobClient.Uri.AbsoluteUri.ToString();
                        obj.CreatedOn = DateTime.Now;
                        obj.IsDelete = false;
                    }
                    await _context.Files.AddAsync(obj);
                    await _context.SaveChangesAsync();
                    #endregion

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

                    #region Checking
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
                    #endregion

                    var blobName = HttpContext.Session.GetString(SessionKeyName).ToString();


                    #region Azure Blob Storage upload task in the products container
                    var containerClient = _blobServiceClient.GetBlobContainerClient("products");
                    var blobClient = containerClient.GetBlobClient(blobName);

                    MemoryStream memory = new MemoryStream();
                    await files.CopyToAsync(memory);
                    memory.Position = 0;
                    await blobClient.UploadAsync(memory, true);
                    #endregion


                    #region Image resize and Azure Blob Storage upload task in the products-thumbnail container
                    var containerClient_resize = _blobServiceClient.GetBlobContainerClient("products-thumbnail");
                    var blobClient_resize = containerClient_resize.GetBlobClient(blobName);

                    using (var memory_stream = new MemoryStream())
                    {
                        files.CopyTo(memory_stream);
                        obj.DataFiles = memory_stream.ToArray();

                        using (MemoryStream ms = new MemoryStream(obj.DataFiles, 0, obj.DataFiles.Length))
                        {
                            using (Image img = Image.FromStream(ms))
                            {
                                int h = 100;
                                int w = 150;
                                using (Bitmap b = new Bitmap(img, new Size(w, h)))
                                {
                                    using (MemoryStream ms2 = new MemoryStream())
                                    {
                                        b.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                                        obj.DataFiles = ms2.ToArray();
                                        ms2.Position = 0;
                                        await blobClient_resize.UploadAsync(ms2, true);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region Update in Database
                    var Width = 0;
                    var Height = 0;
                    var HorizontalResolution = 0.0;
                    var VerticalResolution = 0.0;
                    memory.Position = 0;

                    using (var image = System.Drawing.Image.FromStream(memory, false, false))
                    {
                        Width = image.Width;
                        Height = image.Height;
                        HorizontalResolution = image.HorizontalResolution;
                        VerticalResolution = image.VerticalResolution;
                    }

                    var exisiting_file_name = await _context.Files.Where(s => s.FileName == blobName).FirstOrDefaultAsync();

                    using (var target = new MemoryStream())
                    {
                        files.CopyTo(target);
                        exisiting_file_name.DataFiles = target.ToArray();

                        // obj.DocumentId = 0;
                        exisiting_file_name.FileName = blobName;
                        exisiting_file_name.FileType = fileExtension;
                        exisiting_file_name.LocalFilePath = null;
                        exisiting_file_name.Width = Width;
                        exisiting_file_name.Height = Height;
                        exisiting_file_name.HorizontalResolution = HorizontalResolution;
                        exisiting_file_name.VerticalResolution = VerticalResolution;
                        exisiting_file_name.PrimaryUri = blobClient.Uri.AbsoluteUri.ToString();
                        exisiting_file_name.UpdatedOn = DateTime.Now;
                        exisiting_file_name.IsDelete = false;
                    }

                    await _context.SaveChangesAsync(true);
                    #endregion

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
            var containerClient = _blobServiceClient.GetBlobContainerClient("products-thumbnail");

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
            //var containerClient = _blobServiceClient.GetBlobContainerClient("products");
            //var blobClient = containerClient.GetBlobClient(blobName);
            //await blobClient.DeleteIfExistsAsync();

            //var containerClient_resize = _blobServiceClient.GetBlobContainerClient("products-thumbnail");
            //var blobClient_resize = containerClient_resize.GetBlobClient(blobName);
            //await blobClient_resize.DeleteIfExistsAsync();

            Files model = new Files();
            var exisiting_file_name = await _context.Files.Where(s => s.FileName == blobName).FirstOrDefaultAsync();

            exisiting_file_name.FileName = exisiting_file_name.FileName;
            exisiting_file_name.FileType = exisiting_file_name.FileType;
            exisiting_file_name.LocalFilePath = exisiting_file_name.LocalFilePath;
            exisiting_file_name.PrimaryUri = exisiting_file_name.PrimaryUri;
            exisiting_file_name.DataFiles = exisiting_file_name.DataFiles;
            exisiting_file_name.Width = exisiting_file_name.Width;
            exisiting_file_name.Height = exisiting_file_name.Height;
            exisiting_file_name.HorizontalResolution = exisiting_file_name.HorizontalResolution;
            exisiting_file_name.VerticalResolution = exisiting_file_name.VerticalResolution;
            exisiting_file_name.CreatedOn = exisiting_file_name.CreatedOn;
            exisiting_file_name.UpdatedOn = DateTime.Now;
            exisiting_file_name.IsDelete = true;

            await _context.SaveChangesAsync(true);

            TempData["SuccessMessage"] = "Image successfully Deleted!";
            return RedirectToAction("ShowAllBlobs", "Demo2");
        }
    }
}
