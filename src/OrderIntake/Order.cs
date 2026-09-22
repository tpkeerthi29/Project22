namespace OrderIntake;

public class Order
{
    public string OrderId { get; }
    public string PatientId { get; }
    public string SpecimenId { get; }
    public string SpecimenType { get; }
    public string Priority { get; }
    public DateTime CollectionDate { get; }
    public IReadOnlyList<string> RequestedTests { get; }

    public Order(
        string orderId,
        string patientId,
        string specimenId,
        string specimenType,
        string priority,
        DateTime collectionDate,
        IReadOnlyList<string> requestedTests)
    {
        OrderId = orderId;
        PatientId = patientId;
        SpecimenId = specimenId;
        SpecimenType = specimenType;
        Priority = priority;
        CollectionDate = collectionDate;
        RequestedTests = requestedTests;
    }
}