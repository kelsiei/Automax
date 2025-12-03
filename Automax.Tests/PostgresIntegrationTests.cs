#nullable enable
using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.GasRecord;
using Automax.Models.Note;
using Automax.Models.Reminder;
using Automax.Models.Document;
using Automax.Models.OdometerRecord;
using Automax.Models.PlanRecord;
using Automax.Models.ServiceRecord;
using Automax.Models.User;
using Automax.Models.Vehicle;
using Automax.Models.Settings;
using Automax.Models.Shared;
using Automax.Logic;
using Automax.Helper;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automax.Tests;

public class PostgresIntegrationTests
{
    private static ServiceProvider BuildPostgresServices()
    {
        var connectionString = Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING")
                               ?? "Host=localhost;Database=automax;Username=user;Password=pass";

        var inMemory = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ServerConfig:StorageProvider", "Postgres"),
                new KeyValuePair<string, string?>("ServerConfig:PostgresConnectionString", connectionString)
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<ServerConfig>(inMemory.GetSection("ServerConfig"));
        services.AddLogging();

        services.AddSingleton<PostgresConnectionFactory>();
        services.AddScoped<IVehicleDataAccess, PostgresVehicleDataAccess>();
        services.AddScoped<IGasRecordDataAccess, PostgresGasRecordDataAccess>();
        services.AddScoped<IOdometerRecordDataAccess, PostgresOdometerDataAccess>();
        services.AddScoped<IServiceRecordDataAccess, PostgresServiceRecordDataAccess>();
        services.AddScoped<IPlanRecordDataAccess, PostgresPlanRecordDataAccess>();
        services.AddScoped<IReminderRecordDataAccess, PostgresReminderRecordDataAccess>();
        services.AddScoped<INoteDataAccess, PostgresNoteDataAccess>();
        services.AddScoped<IDocumentDataAccess, PostgresDocumentDataAccess>();
        services.AddScoped<IUserRecordDataAccess, PostgresUserRecordDataAccess>();
        services.AddScoped<IUserConfigDataAccess, PostgresUserConfigDataAccess>();
        services.AddScoped<IUserAccessDataAccess, PostgresUserAccessDataAccess>();
        services.AddScoped<IExtraFieldDataAccess, PostgresExtraFieldDataAccess>();

        return services.BuildServiceProvider();
    }

    [PostgresIntegrationFact]
    public async Task Vehicles_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();

        var vehicle = new Vehicle { Year = 2020, Make = "Test", Model = "Postgres", LicensePlate = "PG-INTEG" };
        var id = await vehicleData.SaveVehicleAsync(vehicle);
        Assert.True(id > 0);

        try
        {
            var fetched = await vehicleData.GetVehicleAsync(id);
            Assert.NotNull(fetched);
            Assert.Equal("Test", fetched!.Make);

            fetched.Make = "Updated";
            await vehicleData.SaveVehicleAsync(fetched);

            var updated = await vehicleData.GetVehicleAsync(id);
            Assert.Equal("Updated", updated!.Make);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(id);
        }
    }

    [PostgresIntegrationFact]
    public async Task Users_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var userData = services.GetRequiredService<IUserRecordDataAccess>();

        var username = $"user_{Guid.NewGuid():N}";
        var user = new UserData
        {
            UserName = username,
            EmailAddress = $"{username}@example.com",
            PasswordHash = "hash",
            IsAdmin = false,
            IsRootUser = false
        };

        var id = await userData.SaveUserAsync(user);
        Assert.True(id > 0);

        try
        {
            var byName = await userData.GetUserByUserNameAsync(username);
            Assert.NotNull(byName);
            Assert.Equal(username, byName!.UserName);

            var any = await userData.AnyUsersAsync();
            Assert.True(any);
        }
        finally
        {
            await userData.DeleteUserAsync(id);
        }
    }

    [PostgresIntegrationFact]
    public async Task Notes_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var noteData = services.GetRequiredService<INoteDataAccess>();

        var vehicle = new Vehicle { Year = 2021, Make = "Note", Model = "Fixture" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var note = new Note
            {
                VehicleId = vehicleId,
                Title = "Hello",
                Body = "World",
                CreatedAt = DateTime.UtcNow
            };

            var noteId = await noteData.SaveNoteAsync(note);
            Assert.True(noteId > 0);

            var fetched = await noteData.GetNoteAsync(noteId);
            Assert.NotNull(fetched);
            Assert.Equal("Hello", fetched!.Title);

            var list = await noteData.GetNotesForVehicleAsync(vehicleId, new MethodParameter());
            Assert.Contains(list, n => n.Id == noteId);

            await noteData.DeleteNoteAsync(noteId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task GasRecords_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var gasData = services.GetRequiredService<IGasRecordDataAccess>();

        var vehicle = new Vehicle { Year = 2022, Make = "Gas", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var record = new GasRecord
            {
                VehicleId = vehicleId,
                Date = DateTime.UtcNow.Date,
                Volume = 10,
                PricePerUnit = 3,
                TotalCost = 30,
                Odometer = 12345,
                Notes = "integration test"
            };

            var recordId = await gasData.SaveGasRecordAsync(record);
            Assert.True(recordId > 0);

            var fetched = await gasData.GetGasRecordAsync(recordId);
            Assert.NotNull(fetched);
            Assert.Equal(vehicleId, fetched!.VehicleId);
            Assert.Equal(10, fetched.Volume);
            Assert.Equal(30, fetched.TotalCost);

            var list = await gasData.GetGasRecordsForVehicleAsync(vehicleId, new MethodParameter());
            Assert.Contains(list, g => g.Id == recordId);

            await gasData.DeleteGasRecordAsync(recordId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task OdometerRecords_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var odoData = services.GetRequiredService<IOdometerRecordDataAccess>();

        var vehicle = new Vehicle { Year = 2023, Make = "Odo", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var record = new OdometerRecord
            {
                VehicleId = vehicleId,
                Date = DateTime.UtcNow.Date,
                Odometer = 55555,
                Notes = "odo note"
            };

            var recordId = await odoData.SaveOdometerRecordAsync(record);
            Assert.True(recordId > 0);

            var fetched = await odoData.GetOdometerRecordAsync(recordId);
            Assert.NotNull(fetched);
            Assert.Equal(55555, fetched!.Odometer);

            var list = await odoData.GetOdometerRecordsForVehicleAsync(vehicleId, new MethodParameter());
            Assert.Contains(list, o => o.Id == recordId);

            await odoData.DeleteOdometerRecordAsync(recordId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task ServiceRecords_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var serviceData = services.GetRequiredService<IServiceRecordDataAccess>();

        var vehicle = new Vehicle { Year = 2024, Make = "Service", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var record = new ServiceRecord
            {
                VehicleId = vehicleId,
                Date = DateTime.UtcNow.Date,
                Description = "Oil change",
                Cost = 49.99m,
                Odometer = 20000
            };

            var recordId = await serviceData.SaveServiceRecordAsync(record);
            Assert.True(recordId > 0);

            var fetched = await serviceData.GetServiceRecordAsync(recordId);
            Assert.NotNull(fetched);
            Assert.Equal("Oil change", fetched!.Description);

            var list = await serviceData.GetServiceRecordsForVehicleAsync(vehicleId, new MethodParameter());
            Assert.Contains(list, s => s.Id == recordId);

            await serviceData.DeleteServiceRecordAsync(recordId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task PlanRecords_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var planData = services.GetRequiredService<IPlanRecordDataAccess>();

        var vehicle = new Vehicle { Year = 2025, Make = "Plan", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var record = new PlanRecord
            {
                VehicleId = vehicleId,
                Description = "Replace tires",
                PlannedDate = DateTime.UtcNow.Date.AddDays(30),
                Priority = Automax.Enum.PlanPriority.High,
                Progress = Automax.Enum.PlanProgress.NotStarted,
                IsArchived = false
            };

            var recordId = await planData.SavePlanRecordAsync(record);
            Assert.True(recordId > 0);

            var fetched = await planData.GetPlanRecordAsync(recordId);
            Assert.NotNull(fetched);
            Assert.Equal("Replace tires", fetched!.Description);

            var list = await planData.GetPlanRecordsForVehicleAsync(vehicleId, new MethodParameter());
            Assert.Contains(list, p => p.Id == recordId);

            await planData.DeletePlanRecordAsync(recordId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task ReminderRecords_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var reminderData = services.GetRequiredService<IReminderRecordDataAccess>();

        var vehicle = new Vehicle { Year = 2026, Make = "Reminder", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var record = new ReminderRecord
            {
                VehicleId = vehicleId,
                Description = "Rotate tires",
                Metric = Automax.Enum.ReminderMetric.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(7),
                DueOdometer = null,
                Urgency = Automax.Enum.ReminderUrgency.Urgent,
                IsCompleted = false,
                Tags = "test"
            };

            var recordId = await reminderData.SaveReminderRecordAsync(record);
            Assert.True(recordId > 0);

            var fetched = await reminderData.GetReminderRecordAsync(recordId);
            Assert.NotNull(fetched);
            Assert.Equal("Rotate tires", fetched!.Description);
            Assert.Equal(Automax.Enum.ReminderUrgency.Urgent, fetched.Urgency);

            var list = await reminderData.GetReminderRecordsForVehicleAsync(vehicleId, new MethodParameter());
            Assert.Contains(list, r => r.Id == recordId);

            await reminderData.DeleteReminderRecordAsync(recordId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task DocumentMetadata_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var documentData = services.GetRequiredService<IDocumentDataAccess>();

        var vehicle = new Vehicle { Year = 2027, Make = "Doc", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var meta = new DocumentMetadata
            {
                VehicleId = vehicleId,
                FileName = "test-doc.txt",
                FilePath = $"data/documents/{vehicleId}/test-doc.txt",
                UploadedAt = DateTime.UtcNow
            };

            var docId = await documentData.SaveDocumentMetadataAsync(meta);
            Assert.True(docId > 0);

            var list = await documentData.GetDocumentsForVehicleAsync(vehicleId);
            Assert.Contains(list, d => d.Id == docId && d.FileName == "test-doc.txt");

            await documentData.DeleteDocumentMetadataAsync(docId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task ExtraFields_Crud_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var extraFieldData = services.GetRequiredService<IExtraFieldDataAccess>();

        var vehicle = new Vehicle { Year = 2031, Make = "Extra", Model = "Tester" };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var extra = new RecordExtraField
            {
                RecordId = vehicleId,
                Name = "Custom",
                Value = "Value",
                Type = Automax.Enum.ExtraFieldType.Text
            };

            var extraId = await extraFieldData.SaveExtraFieldAsync(extra);
            Assert.True(extraId > 0);

            var list = await extraFieldData.GetExtraFieldsForRecordAsync(vehicleId);
            Assert.Contains(list, e => e.Id == extraId && e.Name == "Custom" && e.Value == "Value");

            await extraFieldData.DeleteExtraFieldAsync(extraId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
        }
    }

    [PostgresIntegrationFact]
    public async Task UserConfig_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var userData = services.GetRequiredService<IUserRecordDataAccess>();
        var userConfigData = services.GetRequiredService<IUserConfigDataAccess>();

        var username = $"usercfg_{Guid.NewGuid():N}";
        var user = new UserData
        {
            UserName = username,
            EmailAddress = $"{username}@example.com",
            PasswordHash = "hash",
            IsAdmin = false,
            IsRootUser = false
        };

        var userId = await userData.SaveUserAsync(user);

        try
        {
            var config = new UserConfig { DefaultTab = "garage", HideSoldVehicles = true };
            await userConfigData.SaveUserConfigAsync(userId, config);

            var fetched = await userConfigData.GetUserConfigAsync(userId);
            Assert.NotNull(fetched);
            Assert.Equal("garage", fetched!.Config?.DefaultTab);

            await userData.DeleteUserAsync(userId);
        }
        finally
        {
            // Ensure cleanup if deletion failed earlier.
            try { await userData.DeleteUserAsync(userId); } catch { /* ignore */ }
        }
    }

    [PostgresIntegrationFact]
    public async Task UserAccess_RoundTrip()
    {
        using var services = BuildPostgresServices();
        var userData = services.GetRequiredService<IUserRecordDataAccess>();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var accessData = services.GetRequiredService<IUserAccessDataAccess>();

        var username = $"access_{Guid.NewGuid():N}";
        var user = new UserData
        {
            UserName = username,
            EmailAddress = $"{username}@example.com",
            PasswordHash = "hash",
            IsAdmin = false,
            IsRootUser = false
        };

        var userId = await userData.SaveUserAsync(user);
        var vehicleId = await vehicleData.SaveVehicleAsync(new Vehicle { Year = 2030, Make = "Access", Model = "Tester" });

        try
        {
            var access = new UserAccess { UserId = userId, VehicleId = vehicleId, CanEdit = true };
            var accessId = await accessData.SaveUserAccessAsync(access);
            Assert.True(accessId > 0);

            var forUser = await accessData.GetUserAccessForUserAsync(userId);
            Assert.Contains(forUser, a => a.VehicleId == vehicleId && a.CanEdit);

            var forVehicle = await accessData.GetUserAccessForVehicleAsync(vehicleId);
            Assert.Contains(forVehicle, a => a.UserId == userId && a.CanEdit);

            await accessData.DeleteUserAccessAsync(accessId);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(vehicleId);
            await userData.DeleteUserAsync(userId);
        }
    }

    [PostgresIntegrationFact]
    public async Task AdminPasswordUpdate_PersistsNewHash()
    {
        using var services = BuildPostgresServices();
        var userData = services.GetRequiredService<IUserRecordDataAccess>();
        var passwordHelper = new PasswordHelper();

        var username = $"pwupdate_{Guid.NewGuid():N}";
        var initialPassword = "InitPass123";
        var updatedPassword = "NewPass456";

        var user = new UserData
        {
            UserName = username,
            EmailAddress = $"{username}@example.com",
            PasswordHash = passwordHelper.HashPassword(initialPassword),
            IsAdmin = false,
            IsRootUser = false
        };

        var userId = await userData.SaveUserAsync(user);

        try
        {
            var fetched = await userData.GetUserByIdAsync(userId);
            Assert.NotNull(fetched);
            Assert.True(passwordHelper.VerifyPassword(initialPassword, fetched!.PasswordHash));

            fetched.PasswordHash = passwordHelper.HashPassword(updatedPassword);
            await userData.SaveUserAsync(fetched);

            var afterUpdate = await userData.GetUserByIdAsync(userId);
            Assert.NotNull(afterUpdate);
            Assert.True(passwordHelper.VerifyPassword(updatedPassword, afterUpdate!.PasswordHash));
            Assert.False(passwordHelper.VerifyPassword(initialPassword, afterUpdate.PasswordHash));
        }
        finally
        {
            await userData.DeleteUserAsync(userId);
        }
    }

    [PostgresIntegrationFact]
    public async Task VehicleDashboard_ShowsNewVehicleDetails()
    {
        using var services = BuildPostgresServices();
        var vehicleData = services.GetRequiredService<IVehicleDataAccess>();
        var gasData = services.GetRequiredService<IGasRecordDataAccess>();
        var serviceData = services.GetRequiredService<IServiceRecordDataAccess>();
        var odometerData = services.GetRequiredService<IOdometerRecordDataAccess>();
        var reminderData = services.GetRequiredService<IReminderRecordDataAccess>();
        var planData = services.GetRequiredService<IPlanRecordDataAccess>();
        var noteData = services.GetRequiredService<INoteDataAccess>();
        var fileHelper = new FileHelper(Mock.Of<ILogger<FileHelper>>());

        var vehicle = new Vehicle { Year = 2015, Make = "Honda", Model = "Civic", LicensePlate = "1B2 3C4" };
        var id = await vehicleData.SaveVehicleAsync(vehicle);

        try
        {
            var logic = new VehicleLogic(vehicleData, gasData, serviceData, odometerData, reminderData, planData, noteData, fileHelper);
            var dashboard = await logic.GetVehicleDashboardAsync(userId: 0, isRootUser: true, allowedVehicleIds: null, searchTerm: null);

            var match = dashboard.FirstOrDefault(v => v.Vehicle.Id == id);
            Assert.NotNull(match);
            Assert.Equal(2015, match!.Vehicle.Year);
            Assert.Equal("Honda", match.Vehicle.Make);
            Assert.Equal("Civic", match.Vehicle.Model);
            Assert.Equal("1B2 3C4", match.Vehicle.LicensePlate);
        }
        finally
        {
            await vehicleData.DeleteVehicleAsync(id);
        }
    }
}
