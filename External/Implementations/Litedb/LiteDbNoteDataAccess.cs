using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Models.API;
using Automax.Models.Note;
using LiteDB;

namespace Automax.External.Implementations.Litedb;

public class LiteDbNoteDataAccess : INoteDataAccess
{
    private readonly ILiteCollection<Note> _collection;

    public LiteDbNoteDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<Note>("notes");
    }

    public Task<Note?> GetNoteAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult<Note?>(result);
    }

    public Task<List<Note>> GetNotesForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        var query = _collection.Query()
            .Where(x => x.VehicleId == vehicleId);

        if (filter?.StartDate is not null)
        {
            var start = filter.StartDate.Value;
            query = query.Where(x => x.CreatedAt >= start);
        }

        if (filter?.EndDate is not null)
        {
            var end = filter.EndDate.Value;
            query = query.Where(x => x.CreatedAt <= end);
        }

        var list = query.ToList();
        return Task.FromResult(list);
    }

    public Task<int> SaveNoteAsync(Note note)
    {
        if (note.Id == 0)
        {
            var newId = _collection.Insert(note).AsInt32;
            note.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(note);
        return Task.FromResult(note.Id);
    }

    public Task DeleteNoteAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
