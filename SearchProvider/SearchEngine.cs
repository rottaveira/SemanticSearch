using DIService;
using Domain.Index.Interfaces;
using Domain.Search.Interfaces;
using Infrastructure.Schemas;
using Payload.Document;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace SearchProvider
{
    public class SearchEngine
    {
        private IIndexingManager _indexingManager;
        private ISearchManager _searchManager;

        public SearchEngine()
        {
            Initialize();
        }

        private void Initialize()
        {
            var di = new IoC();
            di.Initialize();
            _indexingManager = di.GetService<IIndexingManager>();
            _searchManager = di.GetService<ISearchManager>();
        }

        public void ConfigureLucene(string schemaPath)
        {
            // 1. Deserializa o XML para obter o LucenePath
            var schema = DeserializeSchemaXml(schemaPath);

            // 2. Calcula o Hash do arquivo XML atual
            string currentHash = GetFileHash(schemaPath);

            // 3. Define o caminho do arquivo de controle de hash (dentro da pasta do Lucene)
            string hashFilePath = Path.Combine(schema.LucenePath, "schema.hash");

            // 4. Verifica se o banco já existe E se o hash é idêntico
            if (File.Exists(hashFilePath))
            {
                string lastHash = File.ReadAllText(hashFilePath);
                if (currentHash == lastHash)
                {
                    Console.WriteLine("Schema inalterado. Pulando reconfiguração.");
                    _indexingManager.InitializeIndex(schema);
                    return;
                }
                else
                {
                    _indexingManager.ReIndexAll(schema);
                }
            } 

            // 5. Salva o novo hash para a próxima verificação
            if (!Directory.Exists(schema.LucenePath)) Directory.CreateDirectory(schema.LucenePath);
            File.WriteAllText(hashFilePath, currentHash);
        }

        private string GetFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(stream);

            // Converte para string hexadecimal
            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public void AddDocument(DocumentModel document)
        {
            _indexingManager.InsertDocument(document);
        }

        public List<DocumentModel> GetAll()
        {
            return _searchManager.GetAll();
        }

        private SchemaConfig DeserializeSchemaXml(string xmlPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SchemaConfig));
            using (StreamReader reader = new StreamReader(xmlPath))
            {
                return (SchemaConfig)serializer.Deserialize(reader);
            }
        }
    }
}
