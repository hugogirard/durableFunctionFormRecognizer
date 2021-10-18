[System.Serializable]
public class TransientFailureException : System.Exception
{
    public TransientFailureException() { }
    public TransientFailureException(string message) : base(message) { }
    public TransientFailureException(string message, System.Exception inner) : base(message, inner) { }
    protected TransientFailureException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}