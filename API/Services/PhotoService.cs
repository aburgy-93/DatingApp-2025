using System;
using API.Helpers;
using API.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace API.Services;

public class PhotoService : IPhotoService
{
    // Setting up the ability to talk to Cloudindary
    private readonly Cloudinary _cloudinary;

    public PhotoService(IOptions<CloudinarySettings> config)
    {
        // Configure the acc by passing in the Cloudinary CloudName. ApiKey, and ApiSecret from the config 
        var acc = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);

        // Get the cloudinary account by passing in the created account
        _cloudinary = new Cloudinary(acc);
    }
    public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
    {
        // Set up a new upload image result object
        var uploadResult = new ImageUploadResult();

        // Check to make sure the file uploaded is there
        if(file.Length > 0) {
            // Read the uploaded file
            using var stream = file.OpenReadStream();

            // Set upload params
            var uploadParams = new ImageUploadParams
            {
                // Create new file with file descriptions
                File = new FileDescription(file.FileName, stream),

                // Transform the picture to your/my specifications
                Transformation = new Transformation()
                    .Height(500).Width(500).Crop("fill").Gravity("face"),

                // Tell which Cloudinary folder to place uploaded photo into
                Folder = "da-net8"
            };

            // Upload the photo
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }

        // Return the uploaded photo
        return uploadResult;
    }

    // Delete a photo
    public async Task<DeletionResult> DeletePhotoAsync(string publicId)
    {
        // Pass in the publicId of the photo 
        var deleteParams = new DeletionParams(publicId);

        // Delete the photo on Cloudinary account
        return await _cloudinary.DestroyAsync(deleteParams);
    }
}
