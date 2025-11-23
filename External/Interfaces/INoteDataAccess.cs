using CarCareTracker.Models.API;
using CarCareTracker.Models.Note;

namespace CarCareTracker.External.Interfaces;

public interface INoteDataAccess
{
    Task<Note?> GetNoteAsync(int id);
    Task<List<Note>> GetNotesForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveNoteAsync(Note note);
    Task DeleteNoteAsync(int id);
}
