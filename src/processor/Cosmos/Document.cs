using System.Collections.Generic;
using Azure.AI.FormRecognizer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Document
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get;set; }

    public string State { get;set; }

    public IEnumerable<JObject> Forms { get; set; }    

    public string Exception { get;set; }

    public int TransientFailureCount { get;set; }
}