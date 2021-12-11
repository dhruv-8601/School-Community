using Azure.Storage.Blobs;
using Lab4.Data;
using Lab4.Models.ViewModels;
using Lab4.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Azure;
using System.IO;

namespace Lab4.Controllers
{
    public class AdvertisementsController : Controller
    {
        private readonly SchoolCommunityContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string containerName = "advertisements";

        public AdvertisementsController(SchoolCommunityContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        public IActionResult Index(string ID)
        {
            var community = _context.Communities.Where(community => community.ID == ID).FirstOrDefault();
            var advertisementViewModel = new AdsViewModel();
            advertisementViewModel.Community = community;
            var communityAds = _context.AdvertisementCommunity.Where(community => community.CommunityID == ID).Include(community => community.Advertisement);

            advertisementViewModel.Advertisements = communityAds.Select(community => community.Advertisement).ToList();
            return View(advertisementViewModel);
        }



        public IActionResult Create(string ID)
        {
            return View(_context.Communities.Find(ID));
        }

        public async Task<IActionResult> Delete(int? ID)
        {
            if (ID == null)
            {
                return NotFound();
            }

            var image = await _context.Advertisements
                .FirstOrDefaultAsync(community => community.ID == ID);
            if (image == null)
            {
                return NotFound();
            }

            return View(image);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int ID)
        {
            var image = await _context.Advertisements.FindAsync(ID);
            var adsViewModel = new AdsViewModel();

            var advCommunity = _context.AdvertisementCommunity
                .Where(adv => adv.AdvertisementID == ID).Include(ac => ac.Community).Single();

            var community = advCommunity.Community;

            adsViewModel.Community = community;

            var ads = _context.AdvertisementCommunity.Where(c => c.CommunityID == community.ID)
                .Include(community => community.Advertisement)
                .Select(ac => ac.Advertisement).Where(adv => adv.ID != image.ID).ToList();

            adsViewModel.Advertisements = ads;

            BlobContainerClient containerClient;
            // Get the container and return a container client object
            try
            {
                containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            try
            {
                // Get the blob that holds the data
                var blockBlob = containerClient.GetBlobClient(image.FileName);
                if (await blockBlob.ExistsAsync())
                {
                    await blockBlob.DeleteAsync();
                }

                _context.Advertisements.Remove(image);
                await _context.SaveChangesAsync();

            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            //_context.AdvertisementCommunity.Remove(advCommunity);

            return View("Index", adsViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string ID, IFormFile file)
        {
            BlobContainerClient containerClient;
            // Create the container and return a container client object
            try
            {
                containerClient = await _blobServiceClient.CreateBlobContainerAsync(containerName);
                // Give access to public
                containerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
            }
            catch (RequestFailedException)
            {
                containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            }


            try
            {
                // create the blob to hold the data
                var blockBlob = containerClient.GetBlobClient(file.FileName);
                if (await blockBlob.ExistsAsync())
                {
                    await blockBlob.DeleteAsync();
                }

                using (var memoryStream = new MemoryStream())
                {
                    // copy the file data into memory
                    await file.CopyToAsync(memoryStream);

                    // navigate back to the beginning of the memory stream
                    memoryStream.Position = 0;

                    // send the file to the cloud
                    await blockBlob.UploadAsync(memoryStream);
                    memoryStream.Close();
                }

                // add the photo to the database if it uploaded successfully
                var image = new Advertisement();
                image.URL = blockBlob.Uri.AbsoluteUri;
                image.FileName = file.FileName;

                _context.Advertisements.Add(image);
                _context.SaveChanges();

                var communityAds = new AdvertisementCommunity();
                communityAds.AdvertisementID = image.ID;
                communityAds.CommunityID = ID;

                _context.AdvertisementCommunity.Add(communityAds);
                _context.SaveChanges();
            }
            catch (RequestFailedException)
            {
                View("Error");
            }

            return RedirectToAction("Index", new { id = ID });
        }
    }
}
