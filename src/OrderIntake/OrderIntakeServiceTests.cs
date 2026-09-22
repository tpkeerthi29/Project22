using OrderIntake;

namespace OrderIntake.Tests;

public class OrderIntakeServiceTests
{
    private readonly OrderIntakeService _service = new();

    [Fact]
    public void AcceptedOrder_MixedCaseValues_AreNormalized_AndUnknownFieldsIgnored()
    {
        string json = """
        {
            "orderId": "ORD-1005",
            "patientId": "PAT-505",
            "specimenId": "SP-9005",
            "specimenType": "bLoOd",
            "priority": "uRgEnT",
            "collectionDate": "2026-09-18",
            "requestedTests": ["Glucose", "CompleteBloodCount"],
            "senderNote": "ignore me"
        }
        """;

        OrderResult result = _service.Process(json);

        Assert.Equal("Accepted", result.Status);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Order);

        Assert.Equal("ORD-1005", result.Order!.OrderId);
        Assert.Equal("PAT-505", result.Order.PatientId);
        Assert.Equal("SP-9005", result.Order.SpecimenId);
        Assert.Equal("Blood", result.Order.SpecimenType);
        Assert.Equal("Urgent", result.Order.Priority);
        Assert.Equal(
            new DateTime(2026, 9, 18),
            result.Order.CollectionDate);

        Assert.Equal(
            new[] { "Glucose", "CompleteBloodCount" },
            result.Order.RequestedTests);
    }

    [Fact]
    public void InvalidOrder_ReturnsAllExpectedErrors()
    {
        string json = """
        {
            "orderId": "   ",
            "patientId": "PAT-505",
            "specimenId": "SP-9005",
            "specimenType": "Plasma",
            "priority": "Emergency",
            "collectionDate": "2026-02-30",
            "requestedTests": []
        }
        """;

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);
        Assert.Null(result.Order);

        Assert.Contains(
            result.Errors,
            e => e.Field == "orderId" && e.Code == "REQUIRED");

        Assert.Contains(
            result.Errors,
            e => e.Field == "specimenType" && e.Code == "INVALID_VALUE");

        Assert.Contains(
            result.Errors,
            e => e.Field == "priority" && e.Code == "INVALID_VALUE");

        Assert.Contains(
            result.Errors,
            e => e.Field == "collectionDate" &&
                 e.Code == "INVALID_FORMAT");

        Assert.Contains(
            result.Errors,
            e => e.Field == "requestedTests" &&
                 e.Code == "REQUIRED");
    }

    [Fact]
    public void OrderId_WithExactly20Characters_IsAccepted()
    {
        string orderId = new string('A', 20);

        string json = CreateValidJson(
            orderId: orderId);

        OrderResult result = _service.Process(json);

        Assert.Equal("Accepted", result.Status);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Order);
        Assert.Equal(orderId, result.Order!.OrderId);
    }

    [Fact]
    public void OrderId_With21Characters_ReturnsMaxLength()
    {
        string orderId = new string('A', 21);

        string json = CreateValidJson(
            orderId: orderId);

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);

        Assert.Contains(
            result.Errors,
            e => e.Field == "orderId" &&
                 e.Code == "MAX_LENGTH");
    }

    [Fact]
    public void InvalidCollectionDate_IsRejected()
    {
        string json = CreateValidJson(
            collectionDate: "2026-02-30");

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);

        Assert.Contains(
            result.Errors,
            e => e.Field == "collectionDate" &&
                 e.Code == "INVALID_FORMAT");
    }

    [Fact]
    public void IncorrectCollectionDateFormat_IsRejected()
    {
        string json = CreateValidJson(
            collectionDate: "2026/09/20");

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);

        Assert.Contains(
            result.Errors,
            e => e.Field == "collectionDate" &&
                 e.Code == "INVALID_FORMAT");
    }

    [Fact]
    public void FutureCollectionDate_IsRejected()
    {
        string futureDate =
            DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        string json = CreateValidJson(
            collectionDate: futureDate);

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);

        Assert.Contains(
            result.Errors,
            e => e.Field == "collectionDate" &&
                 e.Code == "FUTURE_DATE");
    }

    [Fact]
    public void EmptyRequestedTests_IsRejected()
    {
        string json = CreateValidJson(
            requestedTests: Array.Empty<string>());

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);

        Assert.Contains(
            result.Errors,
            e => e.Field == "requestedTests" &&
                 e.Code == "REQUIRED");
    }

    [Fact]
    public void RequestedTests_DifferingOnlyByCase_AreDuplicates()
    {
        string json = CreateValidJson(
            requestedTests: new[]
            {
                "Glucose",
                "glucose"
            });

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);

        Assert.Contains(
            result.Errors,
            e => e.Field == "requestedTests" &&
                 e.Code == "DUPLICATE");
    }

    [Fact]
    public void MalformedJson_ReturnsSingleMalformedInputError()
    {
        string json = """
        {"orderId": "ORD-1005"
        """;

        OrderResult result = _service.Process(json);

        Assert.Equal("Rejected", result.Status);
        Assert.Null(result.Order);

        Assert.Single(result.Errors);

        Assert.Equal("$", result.Errors[0].Field);
        Assert.Equal(
            "MALFORMED_INPUT",
            result.Errors[0].Code);
    }

    private static string CreateValidJson(
        string orderId = "ORD-1005",
        string collectionDate = "2026-09-18",
        string[]? requestedTests = null)
    {
        requestedTests ??= new[]
        {
            "Glucose"
        };

        string testsJson =
            string.Join(
                ", ",
                requestedTests.Select(
                    test => $"\"{test}\""));

        return $$"""
        {
            "orderId": "{{orderId}}",
            "patientId": "PAT-505",
            "specimenId": "SP-9005",
            "specimenType": "Blood",
            "priority": "Routine",
            "collectionDate": "{{collectionDate}}",
            "requestedTests": [{{testsJson}}]
        }
        """;
    }
}