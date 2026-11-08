using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace RaceDay.Api.Services;
public class BlobStorageService(IConfiguration configuration)
{
    private readonly string? connection=configuration["AzureBlobStorage:ConnectionString"];
    private readonly string container=configuration["AzureBlobStorage:ContainerName"]??"raceday-images";
    public async Task<string> UploadAsync(IFormFile file,string blobName,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString.");
        var service=new BlobServiceClient(connection);
        var client=service.GetBlobContainerClient(container);
        await client.CreateIfNotExistsAsync(PublicAccessType.Blob,cancellationToken:ct);
        var blob=client.GetBlobClient(blobName);
        await using var stream=file.OpenReadStream();
        await blob.UploadAsync(stream,new BlobHttpHeaders{ContentType=file.ContentType},cancellationToken:ct);
        return blob.Uri.ToString();
    }
}
