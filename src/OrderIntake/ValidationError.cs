namespace OrderIntake;

public class ValidationError
{
    public string Field { get; }
    public string Code { get; }
    public string Message { get; }

    public ValidationError(
        string field,
        string code,
        string message)
    {
        Field = field;
        Code = code;
        Message = message;
    }
}