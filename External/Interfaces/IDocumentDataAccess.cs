using Automax.Models.Document;

namespace Automax.External.Interfaces;

public interface IDocumentDataAccess
{
    Task<IReadOnlyList<DocumentMetadata>> GetDocumentsForVehicleAsync(int vehicleId);
    Task<int> SaveDocumentMetadataAsync(DocumentMetadata metadata);
    Task DeleteDocumentMetadataAsync(int id);
}
