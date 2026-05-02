using Infrastructure.Util.Constants;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;   
using Lucene.Net.Analysis.Miscellaneous; 
using Lucene.Net.Analysis.Standard; 
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Infrastructure.Schemas
{
    public class LuceneSchemaFactory
    {
        /// <summary>
        /// Constrói um PerFieldAnalyzerWrapper que mapeia cada campo ao seu respectivo Analyzer 
        /// definido no FieldType correspondente.
        /// </summary>
        public Analyzer BuildAnalyzer(SchemaConfig config)
        {
            var fieldAnalyzers = new Dictionary<string, Analyzer>();

            foreach (var field in config.FieldList.Fields)
            {
                // Busca a definição do tipo referenciada pelo campo
                var typeDef = config.FieldTypes.FirstOrDefault(t => t.Id == field.FieldTypeRef);

                if (typeDef != null && typeDef.Analyzers.Any())
                {
                    fieldAnalyzers.Add(field.Name, CreateCustomAnalyzer(typeDef));
                }
            }

            // O StandardAnalyzer é usado como padrão para campos não especificados no dicionário
            return new PerFieldAnalyzerWrapper(new StandardAnalyzer(LuceneConstants.AppLuceneVersion), fieldAnalyzers);
        }

        /// <summary>
        /// Transforma os Analyzers/SubAnalyzers do XML em uma cadeia de processamento do Lucene.
        /// </summary>
        private Analyzer CreateCustomAnalyzer(FieldTypeInfo typeInfo)
        {
            return Analyzer.NewAnonymous((fieldName, reader) =>
            {
                Tokenizer source = new StandardTokenizer(LuceneConstants.AppLuceneVersion, reader);
                TokenStream result = source;

                // Aplicamos os filtros baseados nos SubAnalyzers do XML
                foreach (var analyzerDef in typeInfo.Analyzers)
                {
                    foreach (var sub in analyzerDef.SubAnalyzers)
                    {
                        result = ApplyFilter(sub.Name, result);
                    }
                }

                return new TokenStreamComponents(source, result);
            });
        }

        /// <summary>
        /// Fábrica de TokenFilters baseada em strings (mapeia o nome no XML para a classe do Lucene)
        /// </summary>
        private TokenStream ApplyFilter(string filterName, TokenStream input)
        {
            return filterName.ToLower() switch
            {
                "lowercase" => new LowerCaseFilter(LuceneConstants.AppLuceneVersion, input),
                "asciifolding" => new ASCIIFoldingFilter(input),
                "stopword" => new StopFilter(LuceneConstants.AppLuceneVersion, input, StopAnalyzer.ENGLISH_STOP_WORDS_SET),
                "trim" => new TrimFilter(LuceneConstants.AppLuceneVersion,input),
                _ => input // Se não encontrar o filtro, retorna o fluxo original
            };
        }

        public Document CreateDocument(SchemaConfig config, Dictionary<string, object> data)
        {
            var doc = new Document();

            foreach (var fieldInfo in config.FieldList.Fields)
            {
                if (data.TryGetValue(fieldInfo.Name, out object value) && value != null)
                {
                    var isStored = fieldInfo.Stored ? Field.Store.YES : Field.Store.NO;

                    // Trata Múltiplos Valores (Listas de Strings)
                    if (value is IEnumerable<string> listValue && !(value is string))
                    {
                        foreach (var item in listValue)
                        {
                            AddFieldToDocument(doc, fieldInfo, item, isStored);
                        }
                    }
                    else
                    {
                        // Trata valor único (String, Int, Double)
                        AddFieldToDocument(doc, fieldInfo, value, isStored);
                    }
                }
            }

            return doc;
        }

        private void AddFieldToDocument(Document doc, FieldInfo fieldInfo, object value, Field.Store store)
        {
            bool isAnalyzed = fieldInfo.IndexMode.Equals("Analyzed", StringComparison.OrdinalIgnoreCase);

            switch (value)
            {
                case int intVal:
                    // Campos numéricos usam classes específicas
                    doc.Add(new Int32Field(fieldInfo.Name, intVal, store));
                    break;

                case double doubleVal:
                    doc.Add(new DoubleField(fieldInfo.Name, doubleVal, store));
                    break;

                default: 
                    string stringVal = value.ToString();
                    if (isAnalyzed)
                        doc.Add(new TextField(fieldInfo.Name, stringVal, store));
                    else
                        doc.Add(new StringField(fieldInfo.Name, stringVal, store));
                    break;
            }
        }
    }
}