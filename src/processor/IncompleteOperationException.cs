[System.Serializable]
public class IncompleteOperationException : System.Exception
{
    public IncompleteOperationException() { }
    public IncompleteOperationException(string message) : base(message) { }
    public IncompleteOperationException(string message, System.Exception inner) : base(message, inner) { }
    protected IncompleteOperationException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}