using BonddyPlatform.Services.Options;
using BonddyPlatform.Services.Helpers;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace BonddyPlatform.Services.Implements
{
    public class FirebaseService
    {
        // Created on first upload so the API can start (and serve other endpoints) without Firebase configured.
        private readonly Lazy<StorageClient> _storageClient;
        private readonly FirebaseOptions _firebaseOptions;
        private string BucketName => _firebaseOptions.BucketName;

        public FirebaseService(IOptions<FirebaseOptions> firebaseOptions)
        {
            _firebaseOptions = firebaseOptions.Value;

            _storageClient = new Lazy<StorageClient>(() => StorageClient.Create(FirebaseCredentialLoader.Load(_firebaseOptions)));
        }

        public async Task<string> UploadAvatarAsync(Stream imageStream, string fileName)
        {
            try
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

                //Check có phải file ảnh không
                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp",".tiff"
                };
                if (!allowedExtensions.Contains(Path.GetExtension(fileName)))
                    throw new NotSupportedException("Unsupported file type.");

                string contentType = GetContentType(fileName);

                var storageObject = await _storageClient.Value.UploadObjectAsync(
                    BucketName,
                    $"avatars/{uniqueFileName}",
                    contentType,
                    imageStream
                );

                storageObject.Acl = new[] { new Google.Apis.Storage.v1.Data.ObjectAccessControl
                    {
                        Entity = "allUsers",
                        Role = "READER"
                    }
                };

                await _storageClient.Value.UpdateObjectAsync(storageObject);

                return $"https://storage.googleapis.com/{BucketName}/avatars/{uniqueFileName}";
            }
            catch (Exception ex)
            {

                throw new InvalidOperationException("Error uploading file to Firebase Storage.", ex);
            }
        }

        private string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" => "image/jpg",
                ".jpeg" => "image/jpeg",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream",
            };
        }
    }
}