using System.Collections.Generic;
using Azure.AI.FormRecognizer.Models;

public class FormRecognizerResult 
{
    public enum ResultStatus
    {
        NotCompleted = 0,
        CompletedWithResult = 1,
        CompletedWithoutResult = 2,
        TransientFailure = 3
    }

    public IEnumerable<RecognizedForm> Forms { get; set; }
    public ResultStatus Status { get; set; }
}