namespace AILA.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>Upload ảnh lên cloud storage và trả về URL công khai.</summary>
        Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder, CancellationToken ct = default);
    }
}
