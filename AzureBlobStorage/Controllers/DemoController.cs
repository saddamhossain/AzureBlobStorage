using AzureBlobStorage.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBlobStorage.Controllers
{
    //This controller using WindowsAzure.Storage package
    public class DemoController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _context;

        public DemoController(IConfiguration configuration, DatabaseContext context)
        {
            _configuration = configuration;
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
                        return RedirectToAction("Create", "Demo");
                    }
                    else if (FileSize > 1048576) // 1 MB
                    {
                        TempData["ErrorMessage"] = "File size Should Be UpTo 1 MB";
                        return RedirectToAction("Create", "Demo");
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
                    string blobStorageConnection = _configuration.GetValue<string>("AzureBlobStorageConnectionString");

                    // Retrieve storage account from connection string.
                    CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobStorageConnection);

                    // Create the blob client.
                    CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                    // Retrieve a reference to a container.
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("products");

                    BlobContainerPermissions permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };

                    await cloudBlobContainer.SetPermissionsAsync(permissions);
                    await using (var target = new MemoryStream())
                    {
                        files.CopyTo(target);
                        obj.DataFiles = target.ToArray();
                    }

                    // This also does not make a service call; it only creates a local object.
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(new_file_name);
                    await cloudBlockBlob.UploadFromByteArrayAsync(obj.DataFiles, 0, obj.DataFiles.Length);
                    #endregion



                    // Save in database start
                    var Width = 0;
                    var Height = 0;
                    var HorizontalResolution = 0.0;
                    var VerticalResolution = 0.0;
                    using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var image = Image.FromStream(fileStream, false, false))
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
                        obj.PrimaryUri = cloudBlockBlob.StorageUri.PrimaryUri.ToString();
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


        public async Task<IActionResult> Create2(IFormFile files)
        {
            string systemFileName = files.FileName;
            string blobstorageconnection = _configuration.GetValue<string>("AzureBlobStorageConnectionString");

            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);

            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("products");

            // This also does not make a service call; it only creates a local object.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(systemFileName);

            await using (var data = files.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(data);
            }
            return View("Create");
        }


        public async Task<IActionResult> ShowAllBlobs()
        {
            string blobstorageconnection = _configuration.GetValue<string>("AzureBlobStorageConnectionString");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("products");
            CloudBlobDirectory dirb = container.GetDirectoryReference("products");

            BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(string.Empty, true, BlobListingDetails.Metadata, 100, null, null, null);
            List<FileData> fileList = new List<FileData>();

            foreach (var blobItem in resultSegment.Results)
            {
                // A flat listing operation returns only blobs, not virtual directories.
                var blob = (CloudBlob)blobItem;
                fileList.Add(new FileData()
                {
                    FileName = blob.Name,
                    FileSize = Math.Round((blob.Properties.Length / 1024f) / 1024f, 2).ToString(),
                    ModifiedOn = DateTime.Parse(blob.Properties.LastModified.ToString()).ToLocalTime().ToString()
                });
            }
            return View(fileList);
        }


        public async Task<IActionResult> Download(string blobName)
        {
            CloudBlockBlob blockBlob;
            await using (MemoryStream memoryStream = new MemoryStream())
            {
                string blobStorageConnection = _configuration.GetValue<string>("AzureBlobStorageConnectionString");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobStorageConnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("products");
                blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                await blockBlob.DownloadToStreamAsync(memoryStream);
            }
            Stream blobStream = blockBlob.OpenReadAsync().Result;
            return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        }


        public async Task<IActionResult> Delete(string blobName)
        {
            string blobStorageConnection = _configuration.GetValue<string>("AzureBlobStorageConnectionString");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobStorageConnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = "products";
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(blobName);
            await blob.DeleteIfExistsAsync();

            var exisiting_file_name = await _context.Files.Where(s => s.FileName == blobName).FirstOrDefaultAsync();
            _context.Remove(exisiting_file_name);
            await _context.SaveChangesAsync();

            var filepath_with_name = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images")).Root + $@"\{blobName}";
            if (System.IO.File.Exists(filepath_with_name))
            {
                System.IO.File.Delete(filepath_with_name);
            }

            TempData["SuccessMessage"] = "Image successfully Deleted!";
            return RedirectToAction("ShowAllBlobs", "Demo");
        }

    }
}
