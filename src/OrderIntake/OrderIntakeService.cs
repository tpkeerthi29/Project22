using System.Globalization;
using System.Text.Json;

namespace OrderIntake;

public class OrderIntakeService
{
    public OrderResult Process(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return MalformedResult("Input is null, empty, or whitespace.");
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return MalformedResult("Input is not valid JSON.");
        }

        using (document)
        {
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return MalformedResult("Input must be a JSON object.");
            }

            if (!HasCompatibleTypes(root))
            {
                return MalformedResult("A recognized field has an incompatible JSON type.");
            }

            var errors = new List<ValidationError>();

            string? orderId = GetStringValue(root, "orderId");
            string? patientId = GetStringValue(root, "patientId");
            string? specimenId = GetStringValue(root, "specimenId");
            string? specimenType = GetStringValue(root, "specimenType");
            string? priority = GetStringValue(root, "priority");
            string? collectionDateText = GetStringValue(root, "collectionDate");

            ValidateId(
                "orderId",
                orderId,
                errors);

            ValidateId(
                "patientId",
                patientId,
                errors);

            ValidateId(
                "specimenId",
                specimenId,
                errors);

            string? normalizedSpecimenType = ValidateSpecimenType(
                specimenType,
                errors);

            string? normalizedPriority = ValidatePriority(
                priority,
                errors);

            DateTime? collectionDate = ValidateCollectionDate(
                collectionDateText,
                errors);

            List<string>? requestedTests = ValidateRequestedTests(
                root,
                errors);

            if (errors.Count > 0)
            {
                return new OrderResult(
                    "Rejected",
                    null,
                    errors);
            }

            var order = new Order(
                orderId!,
                patientId!,
                specimenId!,
                normalizedSpecimenType!,
                normalizedPriority!,
                collectionDate!.Value,
                requestedTests!);

            return new OrderResult(
                "Accepted",
                order,
                Array.Empty<ValidationError>());
        }
    }

    private static bool HasCompatibleTypes(JsonElement root)
    {
        string[] recognizedFields =
        {
            "orderId",
            "patientId",
            "specimenId",
            "specimenType",
            "priority",
            "collectionDate",
            "requestedTests"
        };

        foreach (string field in recognizedFields)
        {
            if (!root.TryGetProperty(field, out JsonElement value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (field == "requestedTests")
            {
                if (value.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String &&
                        item.ValueKind != JsonValueKind.Null)
                    {
                        return false;
                    }
                }
            }
            else if (value.ValueKind != JsonValueKind.String)
            {
                return false;
            }
        }

        return true;
    }

    private static string? GetStringValue(
        JsonElement root,
        string field)
    {
        if (!root.TryGetProperty(field, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }

    private static void ValidateId(
        string field,
        string? value,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(
                field,
                "REQUIRED",
                $"{field} is required."));
            return;
        }

        if (value.Length > 20)
        {
            errors.Add(new ValidationError(
                field,
                "MAX_LENGTH",
                $"{field} must not exceed 20 characters."));
        }
    }

    private static string? ValidateSpecimenType(
        string? value,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(
                "specimenType",
                "REQUIRED",
                "specimenType is required."));
            return null;
        }

        if (value.Equals("Blood", StringComparison.OrdinalIgnoreCase))
        {
            return "Blood";
        }

        if (value.Equals("Urine", StringComparison.OrdinalIgnoreCase))
        {
            return "Urine";
        }

        if (value.Equals("Tissue", StringComparison.OrdinalIgnoreCase))
        {
            return "Tissue";
        }

        if (value.Equals("Saliva", StringComparison.OrdinalIgnoreCase))
        {
            return "Saliva";
        }

        errors.Add(new ValidationError(
            "specimenType",
            "INVALID_VALUE",
            "specimenType must be Blood, Urine, Tissue, or Saliva."));

        return null;
    }

    private static string? ValidatePriority(
        string? value,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(
                "priority",
                "REQUIRED",
                "priority is required."));
            return null;
        }

        if (value.Equals("Routine", StringComparison.OrdinalIgnoreCase))
        {
            return "Routine";
        }

        if (value.Equals("Urgent", StringComparison.OrdinalIgnoreCase))
        {
            return "Urgent";
        }

        errors.Add(new ValidationError(
            "priority",
            "INVALID_VALUE",
            "priority must be Routine or Urgent."));

        return null;
    }

    private static DateTime? ValidateCollectionDate(
        string? value,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(
                "collectionDate",
                "REQUIRED",
                "collectionDate is required."));
            return null;
        }

        bool parsed = DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date);

        if (!parsed)
        {
            errors.Add(new ValidationError(
                "collectionDate",
                "INVALID_FORMAT",
                "collectionDate must be a real date in yyyy-MM-dd format."));
            return null;
        }

        if (date.Date > DateTime.Today)
        {
            errors.Add(new ValidationError(
                "collectionDate",
                "FUTURE_DATE",
                "collectionDate must not be after today."));
        }

        return date.Date;
    }

    private static List<string>? ValidateRequestedTests(
        JsonElement root,
        List<ValidationError> errors)
    {
        if (!root.TryGetProperty("requestedTests", out JsonElement value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            errors.Add(new ValidationError(
                "requestedTests",
                "REQUIRED",
                "requestedTests is required."));
            return null;
        }

        var tests = new List<string>();

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Null)
            {
                errors.Add(new ValidationError(
                    "requestedTests",
                    "INVALID_VALUE",
                    "requestedTests must not contain empty items."));
                continue;
            }

            string testName = item.GetString()!;

            if (string.IsNullOrWhiteSpace(testName))
            {
                errors.Add(new ValidationError(
                    "requestedTests",
                    "INVALID_VALUE",
                    "requestedTests must not contain empty items."));
                continue;
            }

            tests.Add(testName);
        }

        if (tests.Count == 0)
        {
            errors.Add(new ValidationError(
                "requestedTests",
                "REQUIRED",
                "requestedTests must contain at least one item."));
            return tests;
        }

        bool duplicateFound = tests
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (duplicateFound)
        {
            errors.Add(new ValidationError(
                "requestedTests",
                "DUPLICATE",
                "requestedTests must not contain duplicate test names."));
        }

        return tests;
    }

    private static OrderResult MalformedResult(string message)
    {
        return new OrderResult(
            "Rejected",
            null,
            new[]
            {
                new ValidationError(
                    "$",
                    "MALFORMED_INPUT",
                    message)
            });
    }
}