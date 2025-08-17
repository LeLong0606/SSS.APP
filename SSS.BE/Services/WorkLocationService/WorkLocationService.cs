using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Models.WorkShift;
using SSS.BE.Persistence;
using SSS.BE.Services.Common;

namespace SSS.BE.Services.WorkLocationService;

public interface IWorkLocationService
{
    Task<PagedResponse<WorkLocationDto>> GetWorkLocationsAsync(int pageNumber, int pageSize, string? search);
    Task<ApiResponse<WorkLocationDto?>> GetWorkLocationByIdAsync(int id);
    Task<ApiResponse<WorkLocationDto>> CreateWorkLocationAsync(CreateWorkLocationRequest request);
    Task<ApiResponse<WorkLocationDto>> UpdateWorkLocationAsync(int id, UpdateWorkLocationRequest request);
    Task<ApiResponse<object>> DeleteWorkLocationAsync(int id);
}

public class WorkLocationService : BaseService, IWorkLocationService
{
    private readonly ApplicationDbContext _context;

    public WorkLocationService(ApplicationDbContext context, ILogger<WorkLocationService> logger) 
        : base(logger)
    {
        _context = context;
    }

    public async Task<PagedResponse<WorkLocationDto>> GetWorkLocationsAsync(int pageNumber, int pageSize, string? search)
    {
        return await HandlePagedOperationAsync(async () =>
        {
            var query = _context.WorkLocations.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.Name.Contains(search) || 
                                        (w.LocationCode != null && w.LocationCode.Contains(search)) ||
                                        (w.Address != null && w.Address.Contains(search)));
            }

            query = query.Where(w => w.IsActive);

            var totalCount = await query.CountAsync();
            
            var locations = await query
                .OrderBy(w => w.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WorkLocationDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    LocationCode = w.LocationCode,
                    Address = w.Address,
                    Description = w.Description,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt
                })
                .ToListAsync();

            return (locations, totalCount);
        }, pageNumber, pageSize, "Get work locations");
    }

    public async Task<ApiResponse<WorkLocationDto?>> GetWorkLocationByIdAsync(int id)
    {
        return await HandleOperationAsync(async () =>
        {
            var location = await _context.WorkLocations
                .Where(w => w.Id == id)
                .Select(w => new WorkLocationDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    LocationCode = w.LocationCode,
                    Address = w.Address,
                    Description = w.Description,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return location;
        }, "Get work location by ID");
    }

    public async Task<ApiResponse<WorkLocationDto>> CreateWorkLocationAsync(CreateWorkLocationRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            // Validate location code uniqueness
            if (!string.IsNullOrEmpty(request.LocationCode))
            {
                await ValidateLocationCodeUniquenessAsync(request.LocationCode);
            }

            var location = new WorkLocation
            {
                Name = request.Name,
                LocationCode = request.LocationCode,
                Address = request.Address,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.WorkLocations.Add(location);
            await _context.SaveChangesAsync();

            return new WorkLocationDto
            {
                Id = location.Id,
                Name = location.Name,
                LocationCode = location.LocationCode,
                Address = location.Address,
                Description = location.Description,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            };
        }, "Create work location");
    }

    public async Task<ApiResponse<WorkLocationDto>> UpdateWorkLocationAsync(int id, UpdateWorkLocationRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var location = await _context.WorkLocations
                .FirstOrDefaultAsync(w => w.Id == id);

            if (location == null)
            {
                throw new InvalidOperationException("Work location not found");
            }

            // Validate location code uniqueness if changed
            if (!string.IsNullOrEmpty(request.LocationCode) && 
                request.LocationCode != location.LocationCode)
            {
                await ValidateLocationCodeUniquenessAsync(request.LocationCode, id);
            }

            // Update location
            location.Name = request.Name;
            location.LocationCode = request.LocationCode;
            location.Address = request.Address;
            location.Description = request.Description;
            location.IsActive = request.IsActive;
            location.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkLocationDto
            {
                Id = location.Id,
                Name = location.Name,
                LocationCode = location.LocationCode,
                Address = location.Address,
                Description = location.Description,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            };
        }, "Update work location");
    }

    public async Task<ApiResponse<object>> DeleteWorkLocationAsync(int id)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var location = await _context.WorkLocations
                .Include(w => w.WorkShifts)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (location == null)
            {
                throw new InvalidOperationException("Work location not found");
            }

            // Check if location has active work shifts
            var activeShifts = location.WorkShifts.Where(ws => ws.IsActive && ws.ShiftDate >= DateTime.Today).Count();
            if (activeShifts > 0)
            {
                throw new InvalidOperationException($"Cannot delete work location with {activeShifts} active shifts. Please reassign or remove them first.");
            }

            location.IsActive = false;
            location.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (object)new { Message = "Work location deleted successfully" };
        }, "Delete work location");
    }

    // Private helper methods
    private async Task ValidateLocationCodeUniquenessAsync(string locationCode, int? excludeId = null)
    {
        var existing = await _context.WorkLocations
            .FirstOrDefaultAsync(w => w.LocationCode == locationCode && 
                                     (excludeId == null || w.Id != excludeId));
        
        if (existing != null)
        {
            throw new InvalidOperationException("Location code already exists");
        }
    }
}