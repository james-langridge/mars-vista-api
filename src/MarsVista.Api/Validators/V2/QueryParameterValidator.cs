using System.Globalization;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Models.V2;

namespace MarsVista.Api.Validators.V2;

/// <summary>
/// Validates and parses query parameters for v2 API endpoints
/// Returns detailed, helpful error messages for invalid inputs
/// </summary>
public class QueryParameterValidator
{
    private static readonly HashSet<string> ValidRovers = new(StringComparer.OrdinalIgnoreCase)
    {
        "curiosity", "perseverance", "opportunity", "spirit"
    };

    private static readonly HashSet<string> ValidCameras = new(StringComparer.OrdinalIgnoreCase)
    {
        "FHAZ", "RHAZ", "MAST", "CHEMCAM", "MAHLI", "MARDI", "NAVCAM",
        "PANCAM", "MINITES", "EDL_RUCAM", "EDL_RDCAM", "EDL_PUCAM1", "EDL_DDCAM"
    };

    private static readonly HashSet<string> ValidSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "sol", "earth_date", "date_taken_utc", "camera", "created_at"
    };

    private static readonly HashSet<string> ValidPhotoFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "sol", "earth_date", "date_taken_utc", "date_taken_mars",
        "img_src", "img_src_small", "img_src_medium", "img_src_large", "img_src_full",
        "width", "height", "sample_type", "site", "drive", "xyz",
        "mast_az", "mast_el", "title", "caption", "created_at"
    };

    private static readonly HashSet<string> ValidIncludeResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "rover", "camera"
    };

    /// <summary>
    /// Validate and parse photo query parameters
    /// Returns null if valid, or ApiError if validation fails
    /// </summary>
    public static ApiError? ValidatePhotoQuery(PhotoQueryParameters parameters, string requestPath)
    {
        var errors = new List<ValidationError>();

        // Validate and parse rovers
        if (!string.IsNullOrWhiteSpace(parameters.Rovers))
        {
            parameters.RoverList = parameters.Rovers
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim().ToLowerInvariant())
                .ToList();

            foreach (var rover in parameters.RoverList)
            {
                if (!ValidRovers.Contains(rover))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "rovers",
                        Value = rover,
                        Message = $"Invalid rover name: '{rover}'",
                        Example = "curiosity,perseverance,opportunity,spirit"
                    });
                }
            }
        }

        // Validate and parse cameras
        if (!string.IsNullOrWhiteSpace(parameters.Cameras))
        {
            parameters.CameraList = parameters.Cameras
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToUpperInvariant())
                .ToList();

            foreach (var camera in parameters.CameraList)
            {
                if (!ValidCameras.Contains(camera))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "cameras",
                        Value = camera,
                        Message = $"Invalid camera name: '{camera}'",
                        Example = "FHAZ,NAVCAM,MAST"
                    });
                }
            }
        }

        // Validate sol ranges
        if (parameters.Sol.HasValue)
        {
            parameters.SolMin = parameters.Sol;
            parameters.SolMax = parameters.Sol;
        }

        if (parameters.SolMin.HasValue && parameters.SolMax.HasValue && parameters.SolMin > parameters.SolMax)
        {
            errors.Add(new ValidationError
            {
                Field = "sol_min",
                Value = parameters.SolMin,
                Message = "sol_min must be <= sol_max",
                Example = "sol_min=100&sol_max=200"
            });
        }

        // Validate and parse dates
        if (!string.IsNullOrWhiteSpace(parameters.EarthDate))
        {
            parameters.DateMin = parameters.EarthDate;
            parameters.DateMax = parameters.EarthDate;
        }

        if (!string.IsNullOrWhiteSpace(parameters.DateMin))
        {
            if (!DateTime.TryParseExact(parameters.DateMin, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            {
                errors.Add(new ValidationError
                {
                    Field = "date_min",
                    Value = parameters.DateMin,
                    Message = "Must be in YYYY-MM-DD format",
                    Example = "2023-01-01"
                });
            }
            else
            {
                parameters.DateMinParsed = date;
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.DateMax))
        {
            if (!DateTime.TryParseExact(parameters.DateMax, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            {
                errors.Add(new ValidationError
                {
                    Field = "date_max",
                    Value = parameters.DateMax,
                    Message = "Must be in YYYY-MM-DD format",
                    Example = "2023-12-31"
                });
            }
            else
            {
                parameters.DateMaxParsed = date;
            }
        }

        if (parameters.DateMinParsed.HasValue && parameters.DateMaxParsed.HasValue && parameters.DateMinParsed > parameters.DateMaxParsed)
        {
            errors.Add(new ValidationError
            {
                Field = "date_min",
                Value = parameters.DateMin,
                Message = "date_min must be <= date_max",
                Example = "date_min=2023-01-01&date_max=2023-12-31"
            });
        }

        // Validate and parse sort fields
        if (!string.IsNullOrWhiteSpace(parameters.Sort))
        {
            var sortParts = parameters.Sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in sortParts)
            {
                var trimmed = part.Trim();
                var descending = trimmed.StartsWith('-');
                var field = descending ? trimmed[1..] : trimmed;

                if (!ValidSortFields.Contains(field))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "sort",
                        Value = trimmed,
                        Message = $"Invalid sort field: '{field}'",
                        Example = "-earth_date,sol"
                    });
                }
                else
                {
                    parameters.SortFields.Add(new SortField
                    {
                        Field = field.ToLowerInvariant(),
                        Direction = descending ? SortDirection.Descending : SortDirection.Ascending
                    });
                }
            }
        }

        // Validate and parse fields
        if (!string.IsNullOrWhiteSpace(parameters.Fields))
        {
            parameters.FieldList = parameters.Fields
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim().ToLowerInvariant())
                .ToList();

            foreach (var field in parameters.FieldList)
            {
                if (!ValidPhotoFields.Contains(field))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "fields",
                        Value = field,
                        Message = $"Invalid field name: '{field}'",
                        Example = "id,img_src,sol,earth_date"
                    });
                }
            }
        }

        // Validate and parse include
        if (!string.IsNullOrWhiteSpace(parameters.Include))
        {
            parameters.IncludeList = parameters.Include
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim().ToLowerInvariant())
                .ToList();

            foreach (var include in parameters.IncludeList)
            {
                if (!ValidIncludeResources.Contains(include))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "include",
                        Value = include,
                        Message = $"Invalid include resource: '{include}'",
                        Example = "rover,camera"
                    });
                }
            }
        }

        // Return errors if any
        if (errors.Count > 0)
        {
            return new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "The request contains invalid parameters",
                Instance = requestPath,
                Errors = errors
            };
        }

        return null;
    }
}
