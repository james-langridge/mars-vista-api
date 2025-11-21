using System.Globalization;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Helpers;
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

        // Validate and parse Mars time parameters
        if (!string.IsNullOrWhiteSpace(parameters.MarsTimeMin))
        {
            if (!MarsTimeHelper.TryParseMarsTime(parameters.MarsTimeMin, out var marsTime))
            {
                errors.Add(new ValidationError
                {
                    Field = "mars_time_min",
                    Value = parameters.MarsTimeMin,
                    Message = "Invalid Mars time format. Use Mhh:mm:ss or hh:mm:ss",
                    Example = "M06:00:00"
                });
            }
            else
            {
                parameters.MarsTimeMinParsed = marsTime;
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.MarsTimeMax))
        {
            if (!MarsTimeHelper.TryParseMarsTime(parameters.MarsTimeMax, out var marsTime))
            {
                errors.Add(new ValidationError
                {
                    Field = "mars_time_max",
                    Value = parameters.MarsTimeMax,
                    Message = "Invalid Mars time format. Use Mhh:mm:ss or hh:mm:ss",
                    Example = "M18:00:00"
                });
            }
            else
            {
                parameters.MarsTimeMaxParsed = marsTime;
            }
        }

        if (parameters.MarsTimeMinParsed.HasValue && parameters.MarsTimeMaxParsed.HasValue &&
            parameters.MarsTimeMinParsed > parameters.MarsTimeMaxParsed)
        {
            errors.Add(new ValidationError
            {
                Field = "mars_time_min",
                Value = parameters.MarsTimeMin,
                Message = "mars_time_min must be <= mars_time_max",
                Example = "mars_time_min=M06:00:00&mars_time_max=M18:00:00"
            });
        }

        // Validate location ranges
        if (parameters.Site.HasValue)
        {
            // If exact site is specified, set both min and max to same value
            if (!parameters.SiteMin.HasValue) parameters.SiteMin = parameters.Site;
            if (!parameters.SiteMax.HasValue) parameters.SiteMax = parameters.Site;
        }

        if (parameters.Drive.HasValue)
        {
            // If exact drive is specified, set both min and max to same value
            if (!parameters.DriveMin.HasValue) parameters.DriveMin = parameters.Drive;
            if (!parameters.DriveMax.HasValue) parameters.DriveMax = parameters.Drive;
        }

        if (parameters.SiteMin.HasValue && parameters.SiteMax.HasValue && parameters.SiteMin > parameters.SiteMax)
        {
            errors.Add(new ValidationError
            {
                Field = "site_min",
                Value = parameters.SiteMin,
                Message = "site_min must be <= site_max",
                Example = "site_min=70&site_max=80"
            });
        }

        if (parameters.DriveMin.HasValue && parameters.DriveMax.HasValue && parameters.DriveMin > parameters.DriveMax)
        {
            errors.Add(new ValidationError
            {
                Field = "drive_min",
                Value = parameters.DriveMin,
                Message = "drive_min must be <= drive_max",
                Example = "drive_min=1000&drive_max=1200"
            });
        }

        // location_radius requires both site and drive
        if (parameters.LocationRadius.HasValue && (!parameters.Site.HasValue || !parameters.Drive.HasValue))
        {
            errors.Add(new ValidationError
            {
                Field = "location_radius",
                Value = parameters.LocationRadius,
                Message = "location_radius requires both site and drive parameters",
                Example = "site=79&drive=1204&location_radius=5"
            });
        }

        // Validate image dimension ranges
        if (parameters.MinWidth.HasValue && parameters.MaxWidth.HasValue && parameters.MinWidth > parameters.MaxWidth)
        {
            errors.Add(new ValidationError
            {
                Field = "min_width",
                Value = parameters.MinWidth,
                Message = "min_width must be <= max_width",
                Example = "min_width=1024&max_width=1920"
            });
        }

        if (parameters.MinHeight.HasValue && parameters.MaxHeight.HasValue && parameters.MinHeight > parameters.MaxHeight)
        {
            errors.Add(new ValidationError
            {
                Field = "min_height",
                Value = parameters.MinHeight,
                Message = "min_height must be <= max_height",
                Example = "min_height=768&max_height=1080"
            });
        }

        // Validate and parse sample types
        if (!string.IsNullOrWhiteSpace(parameters.SampleType))
        {
            var validSampleTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Full", "Thumbnail", "Subframe", "Downsampled"
            };

            parameters.SampleTypeList = parameters.SampleType
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            foreach (var sampleType in parameters.SampleTypeList)
            {
                if (!validSampleTypes.Contains(sampleType))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "sample_type",
                        Value = sampleType,
                        Message = $"Invalid sample type: '{sampleType}'",
                        Example = "Full,Thumbnail"
                    });
                }
            }
        }

        // Validate and parse aspect ratio
        if (!string.IsNullOrWhiteSpace(parameters.AspectRatio))
        {
            if (!MarsTimeHelper.TryParseAspectRatio(parameters.AspectRatio, out var aspectRatio))
            {
                errors.Add(new ValidationError
                {
                    Field = "aspect_ratio",
                    Value = parameters.AspectRatio,
                    Message = "Invalid aspect ratio format. Use width:height (e.g., 16:9)",
                    Example = "16:9"
                });
            }
            else
            {
                parameters.AspectRatioParsed = aspectRatio;
            }
        }

        // Validate camera angle ranges
        if (parameters.MastElevationMin.HasValue && parameters.MastElevationMax.HasValue &&
            parameters.MastElevationMin > parameters.MastElevationMax)
        {
            errors.Add(new ValidationError
            {
                Field = "mast_elevation_min",
                Value = parameters.MastElevationMin,
                Message = "mast_elevation_min must be <= mast_elevation_max",
                Example = "mast_elevation_min=-30&mast_elevation_max=30"
            });
        }

        if (parameters.MastAzimuthMin.HasValue && parameters.MastAzimuthMax.HasValue &&
            parameters.MastAzimuthMin > parameters.MastAzimuthMax)
        {
            errors.Add(new ValidationError
            {
                Field = "mast_azimuth_min",
                Value = parameters.MastAzimuthMin,
                Message = "mast_azimuth_min must be <= mast_azimuth_max",
                Example = "mast_azimuth_min=90&mast_azimuth_max=180"
            });
        }

        // Validate and parse field set
        if (!string.IsNullOrWhiteSpace(parameters.FieldSet))
        {
            var fieldSetLower = parameters.FieldSet.Trim().ToLowerInvariant();
            if (Enum.TryParse<FieldSetType>(fieldSetLower, ignoreCase: true, out var fieldSet))
            {
                parameters.FieldSetParsed = fieldSet;
            }
            else
            {
                // If not a preset, treat as invalid
                errors.Add(new ValidationError
                {
                    Field = "field_set",
                    Value = parameters.FieldSet,
                    Message = "Invalid field set. Must be one of: minimal, standard, extended, scientific, complete",
                    Example = "extended"
                });
            }
        }

        // Validate and parse image sizes
        if (!string.IsNullOrWhiteSpace(parameters.ImageSizes))
        {
            var validImageSizes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "small", "medium", "large", "full"
            };

            parameters.ImageSizesList = parameters.ImageSizes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .ToList();

            foreach (var size in parameters.ImageSizesList)
            {
                if (!validImageSizes.Contains(size))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "image_sizes",
                        Value = size,
                        Message = $"Invalid image size: '{size}'",
                        Example = "small,medium,large,full"
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
