using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Infrastructure.Schemas
{

    [XmlRoot("Configuration")]
    public class SchemaConfig
    {
        [XmlAttribute("LucenePath")]
        public string LucenePath { get; set; }

        // Repositório de tipos reutilizáveis
        [XmlArray("FieldTypeDefinitions")]
        [XmlArrayItem("FieldType")]
        public List<FieldTypeInfo> FieldTypes { get; set; } = new List<FieldTypeInfo>();

        [XmlElement("FieldList")]
        public FieldListInfo FieldList { get; set; }
    }

    public class FieldTypeInfo
    {
        [XmlAttribute("Id")] // O nome que será referenciado
        public string Id { get; set; }

        [XmlElement("Analyzer")]
        public List<AnalyzerInfo> Analyzers { get; set; } = new List<AnalyzerInfo>();
    }

    public class AnalyzerInfo
    {
        [XmlAttribute("Type")]
        public string Type { get; set; } // search ou index

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlElement("Analyzer")]
        public List<AnalyzerInfo> SubAnalyzers { get; set; } = new List<AnalyzerInfo>();
    }

    public class FieldInfo
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Stored")]
        public bool Stored { get; set; } = true;

        [XmlAttribute("IndexMode")]
        public string IndexMode { get; set; } = "Analyzed";

        [XmlAttribute("FieldType")] // Referência ao ID do FieldTypeInfo
        public string FieldTypeRef { get; set; }
    }

    public class FieldListInfo
    {
        [XmlElement("Field")]
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
    }
}