using CarCareTracker.External.Interfaces;

namespace CarCareTracker.Logic;

public class OdometerLogic
{
    private readonly IOdometerRecordDataAccess _odometerRecordDataAccess;

    public OdometerLogic(IOdometerRecordDataAccess odometerRecordDataAccess)
    {
        _odometerRecordDataAccess = odometerRecordDataAccess;
    }

    /// <summary>
    /// Returns the last recorded odometer value for the specified vehicle, or null if none exist.
    /// </summary>
    public async Task<int?> GetLastOdometerAsync(int vehicleId)
    {
        var records = await _odometerRecordDataAccess.GetOdometerRecordsForVehicleAsync(vehicleId, null);
        if (records == null || records.Count == 0)
        {
            return null;
        }

        var latest = records.OrderByDescending(r => r.Date).First();
        return latest.Odometer;
    }

    /// <summary>
    /// Returns the minimum and maximum odometer values for the specified vehicle.
    /// </summary>
    public async Task<(int? Min, int? Max)> GetOdometerRangeAsync(int vehicleId)
    {
        var records = await _odometerRecordDataAccess.GetOdometerRecordsForVehicleAsync(vehicleId, null);
        if (records == null || records.Count == 0)
        {
            return (null, null);
        }

        var min = records.Min(r => r.Odometer);
        var max = records.Max(r => r.Odometer);
        return (min, max);
    }

    /// <summary>
    /// Returns an "adjusted" odometer value based on configuration.
    /// For now, this is a simple passthrough; adjustment rules will be added in a later phase.
    /// </summary>
    public Task<int?> GetAdjustedOdometerAsync(int vehicleId, int reportedOdometer)
    {
        // TODO: In a later phase, use per-vehicle adjustment configuration to adjust the reported odometer.
        return Task.FromResult<int?>(reportedOdometer);
    }
}
