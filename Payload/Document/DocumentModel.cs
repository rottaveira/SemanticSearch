namespace Payload.Document
{
    public class DocumentModel
    {
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();

        public void SetField(string name, string value)
        {
            Fields[name] = value;
        }
        public void SetField(string name, double value)
        {
            Fields[name] = value;
        }
        public void SetField(string name, int value)
        {
            Fields[name] = value;
        }
        public void SetField(string name, List<string> value)
        {
            Fields[name] = value;
        }

        public void Clear() => Fields.Clear();
    }
}
