using BonddyPlatform.Services.Options;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace BonddyPlatform.Services.Implements
{
    public class FirebaseService
    {
        private readonly StorageClient _storageClient;
        private readonly FirebaseOptions _firebaseOptions;
        private const string BucketName = "bonddyplatform.firebasestorage.app";

        public FirebaseService(IOptions<FirebaseOptions> firebaseOptions)
        {
            _firebaseOptions = firebaseOptions.Value;

            GoogleCredential credential;

            var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                credential = GoogleCredential.FromFile(path);
            }
            else if (!string.IsNullOrWhiteSpace(_firebaseOptions.CredentialsPath) && File.Exists(_firebaseOptions.CredentialsPath))
            {
                credential = GoogleCredential.FromFile(_firebaseOptions.CredentialsPath);
            }
            else if (!string.IsNullOrWhiteSpace(_firebaseOptions.CredentialsJson))
            {
                var json = _firebaseOptions.CredentialsJson.Trim();
                if (!json.StartsWith("{"))
                {
                    try
                    {
                        var decoded = Convert.FromBase64String(json);
                        json = System.Text.Encoding.UTF8.GetString(decoded);
                    }
                    catch
                    {
                        throw new InvalidOperationException("Firebase:CredentialsJson must be valid JSON or base64-encoded JSON.");
                    }
                }

                credential = GoogleCredential.FromJson(json);
            }
            else
            {
                throw new InvalidOperationException(
                    "Firebase not configured for Storage. Set GOOGLE_APPLICATION_CREDENTIALS or Firebase:CredentialsPath or Firebase:CredentialsJson in configuration.");
            }

            _storageClient = StorageClient.Create(credential);
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

                var storageObject = await _storageClient.UploadObjectAsync(
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

                await _storageClient.UpdateObjectAsync(storageObject);


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