using System.Collections.Generic;
using System.Linq;
using Azure.AI.FormRecognizer.Models;
using Newtonsoft.Json;

public class Document
{
    public class Field
    {
        public string Name { get;set; }
        public float? Confidence { get;set; }
        public string Value { get;set; }
    }

    public class Page
    {
        public int Number { get;set; }
        public IEnumerable<Line> Lines { get;set; }
    }    

    public class Line
    {
        public IEnumerable<string> Words { get;set; }
    }

    public class Form
    {        
        public string FormType { get;set; }
        public float? FormTypeConfidence { get;set; }
        public string ModelId { get;set; }
        public IEnumerable<Field> Fields { get;set; }
        public IEnumerable<Page> Pages { get;set; }

        public Form() { }

        public Form(RecognizedForm recognizedForm)
        {
            FormType = recognizedForm.FormType;
            FormTypeConfidence = recognizedForm.FormTypeConfidence;
            ModelId = recognizedForm.ModelId;
            Fields = recognizedForm.Fields.Select(x => new Field() { Name = x.Value.Name, Confidence = x.Value.Confidence, Value = x.Value.Value.AsString()  });
            Pages = recognizedForm.Pages.Select(p => new Page(){ Number = p.PageNumber, Lines = p.Lines.Select(l => new Line() { Words = l.Words.Select(w => w.Text) })  });
        }
    }

    [JsonProperty(PropertyName = "id")]
    public string Id { get;set; }

    public string State { get;set; }    

    public string Exception { get;set; }

    public int TransientFailureCount { get;set; }

    public IEnumerable<Form> Forms { get; set; } 
}