namespace OrderIntake;

public class OrderResult
{
    public string Status { get; }
    public Order? Order { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    public OrderResult(
        string status,
        Order? order,
        IReadOnlyList<ValidationError> errors)
    {
        Status = status;
        Order = order;
        Errors = errors;
    }
}