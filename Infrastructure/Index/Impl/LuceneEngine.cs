using Infrastructure.Index.Interface;
using Infrastructure.Schemas;
using Infrastructure.Util.Constants;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Payload.Document;

namespace Infrastructure.Index.Impl
{
    public class LuceneEngine : ILuceneEngine
    {
        private readonly LuceneSchemaFactory _schemaFactory;
        private SchemaConfig _schema;
        private FSDirectory _directory;

        public LuceneEngine()
        {
            _schemaFactory = new();
        }

        /// <summary>
        /// Cria a estrutura física do índice baseada no SchemaConfig.
        /// </summary>
        public void InitializeLucene(SchemaConfig mySchema)
        {
            SetEnvironment(mySchema);
            bool indexExists = DirectoryReader.IndexExists(_directory);

            if (indexExists)
            {
                // Opcional: Log ou console avisando que o banco já está pronto
                Console.WriteLine("O índice Lucene já existe. Pulando inicialização.");
                return;
            }

            // 2. Obtém o Analyzer customizado gerado pela Factory (respeitando os FieldTypes do XML) 
            using var analyzer = _schemaFactory.BuildAnalyzer(mySchema);


            // 3. Configuração do Escritor
            var config = new IndexWriterConfig(LuceneConstants.AppLuceneVersion, analyzer)
            {
                // CREATE garante que um novo índice seja iniciado. 
                // CREATE_OR_APPEND manteria o que já existe.
                OpenMode = OpenMode.CREATE
            };

            // 4. Garante a criação do banco (Arquivos de Segmento)
            using (var writer = new IndexWriter(_directory, config))
            {
                // Commit vazio cria os arquivos de metadados do Lucene (segments.gen, segments_N)
                // Isso efetivamente "formata" o diretório como um índice Lucene válido.
                writer.Commit();
            }
        }

        private void SetEnvironment(SchemaConfig mySchema)
        {
            _schema = mySchema;
            if (string.IsNullOrEmpty(mySchema.LucenePath))
                throw new ArgumentException("O caminho do Lucene (LucenePath) não foi definido no Schema.");
             
            if (!System.IO.Directory.Exists(mySchema.LucenePath))
            {
                System.IO.Directory.CreateDirectory(mySchema.LucenePath);
            }

            _directory = FSDirectory.Open(new DirectoryInfo(mySchema.LucenePath));
        }

        public void IndexDocument(IEnumerable<DocumentModel> documents)
        { 
            using var analyzer = _schemaFactory.BuildAnalyzer(_schema);

            var config = new IndexWriterConfig(LuceneConstants.AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.APPEND
            };

            using var writer = new IndexWriter(_directory, config);

            foreach (var docModel in documents)
            { 
                var luceneDoc = _schemaFactory.CreateDocument(_schema, docModel.Fields);
                writer.AddDocument(luceneDoc);
            }

            writer.Commit();
        }

        public void UpdateDocument(string idFieldName, string idValue, DocumentModel updatedDoc)
        {
            if (_schema == null)
                throw new InvalidOperationException("LuceneEngine não inicializado. Chame InitializeLucene primeiro.");

            if (string.IsNullOrEmpty(idFieldName) || string.IsNullOrEmpty(idValue))
                throw new ArgumentException("O nome e o valor do campo identificador são obrigatórios para o update.");

            using var analyzer = _schemaFactory.BuildAnalyzer(_schema);

            var config = new IndexWriterConfig(LuceneConstants.AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.APPEND // Abre o índice existente para modificação
            };

            using var writer = new IndexWriter(_directory, config);

            // 1. Cria o termo de busca para localizar o documento exato a ser substituído
            // Usamos Term porque o ID normalmente não é analisado (StringField / NotAnalyzed)
            var updateTerm = new Term(idFieldName, idValue);

            // 2. Transforma o DocumentModel em um Document do Lucene
            var newLuceneDoc = _schemaFactory.CreateDocument(_schema, updatedDoc.Fields);

            // 3. O Lucene busca pelo termo, remove o documento antigo e insere o novo atomicamente
            writer.UpdateDocument(updateTerm, newLuceneDoc);

            // 4. Consolida a alteração
            writer.Commit();
        }

        public void DeleteDocument(string idFieldName, string idValue)
        {
            if (_schema == null)
                throw new InvalidOperationException("LuceneEngine não inicializado. Chame InitializeLucene primeiro.");

            if (string.IsNullOrEmpty(idFieldName) || string.IsNullOrEmpty(idValue))
                throw new ArgumentException("O nome e o valor do campo identificador são obrigatórios para a deleção.");

            using var analyzer = _schemaFactory.BuildAnalyzer(_schema);

            var config = new IndexWriterConfig(LuceneConstants.AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.APPEND
            };

            using var writer = new IndexWriter(_directory, config);

            // 1. Cria o termo de busca para localizar o documento exato a ser deletado
            var deleteTerm = new Term(idFieldName, idValue);

            // 2. O Lucene remove todos os documentos que correspondem ao termo
            writer.DeleteDocuments(deleteTerm);

            // 3. Consolida a deleção fisicamente
            writer.Commit();
        }

        public void ReindexAll(SchemaConfig mySchema)
        {
            SetEnvironment(mySchema);
             
            // 1. Obtemos os dados frescos da fonte original ANTES de apagar o índice
            // Isso garante que se a fonte falhar, não apagamos o banco à toa.
            var freshData = GetAllDocuments();

            // 2. Criamos o Analyzer
            using var analyzer = _schemaFactory.BuildAnalyzer(_schema);

            // 3. Configuração com OpenMode.CREATE 
            // Isso apaga todos os documentos e segmentos existentes na pasta.
            var config = new IndexWriterConfig(LuceneConstants.AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.CREATE
            };

            // 4. Executa a limpeza e reinserção
            using (var writer = new IndexWriter(_directory, config))
            {
                foreach (var docModel in freshData)
                {
                    var luceneDoc = _schemaFactory.CreateDocument(_schema, docModel.Fields);
                    writer.AddDocument(luceneDoc);
                }

                // Commit final consolida o novo "banco"
                writer.Commit();
            }

            Console.WriteLine("Reindexação concluída com sucesso.");
        }

        public List<DocumentModel> GetAllDocuments()
        {
            var results = new List<DocumentModel>();
            using var reader = DirectoryReader.Open(_directory);
            var searcher = new IndexSearcher(reader);
            var query = new MatchAllDocsQuery();

            int totalDocs = reader.MaxDoc > 0 ? reader.MaxDoc : 1;
            var hits = searcher.Search(query, totalDocs).ScoreDocs;

            foreach (var hit in hits)
            {
                var luceneDoc = searcher.Doc(hit.Doc);
                var model = new DocumentModel();

                foreach (var field in luceneDoc.Fields)
                {
                    // Verifica se o campo já existe no model (indica múltiplos valores)
                    if (model.Fields.ContainsKey(field.Name))
                    {
                        var existingValue = model.Fields[field.Name];

                        if (existingValue is List<string> list)
                        {
                            list.Add(field.GetStringValue());
                        }
                        else
                        {
                            // Transforma o valor único anterior em uma lista
                            var newList = new List<string> { existingValue.ToString(), field.GetStringValue() };
                            model.Fields[field.Name] = newList;
                        }
                    }
                    else
                    {
                        // Se for numérico, tenta extrair o valor correto
                        if (field.GetDoubleValue() != null)
                        {
                            model.Fields[field.Name] = field.GetDoubleValue();
                        }
                        else
                        {
                            model.Fields[field.Name] = field.GetStringValue();
                        }
                    }
                }

                results.Add(model);
            }

            return results;
        }
    }
}
