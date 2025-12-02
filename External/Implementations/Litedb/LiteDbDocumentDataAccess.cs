using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Models.Document;
using LiteDB;

namespace Automax.External.Implementations.Litedb;

public class LiteDbDocumentDataAccess : IDocumentDataAccess
{
    private readonly ILiteCollection<DocumentMetadata> _collection;

    public LiteDbDocumentDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<DocumentMetadata>("documents");
    }

    public Task<IReadOnlyList<DocumentMetadata>> GetDocumentsForVehicleAsync(int vehicleId)
    {
        var list = _collection.Query()
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.UploadedAt)
            .ToList()
            .Cast<DocumentMetadata>()
            .ToList();

        return Task.FromResult<IReadOnlyList<DocumentMetadata>>(list);
    }

    public Task<int> SaveDocumentMetadataAsync(DocumentMetadata metadata)
    {
        if (metadata.Id == 0)
        {
            var newId = _collection.Insert(metadata).AsInt32;
            metadata.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(metadata);
        return Task.FromResult(metadata.Id);
    }

    public Task DeleteDocumentMetadataAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
