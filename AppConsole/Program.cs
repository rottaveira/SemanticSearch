using DIService;
using InferenceCore.Entity;
using InferenceCore.NER.Impl;
using InferenceCore.ONNX.Impl;
using Infrastructure.NLP.Impl;
using Payload.Document;
using SearchProvider;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        //NLP();
        //Lucene();
        //InferenceNer();
        InferenceEmbbedings();

    }

    public async static void NLP()
    {
        var nlp = new NlpService();
        var text = "Cafu, Pelé e Zico foram grandes jogadores de futebol no brasil e na europa";
        var lang = await nlp.GetLanguageAsync(text);
        var document = await nlp.GetProcessedTextAsync(text, lang);
        var tokens = await nlp.GetNerAsync(text, lang);
         
    }

    public static void Lucene()
    {
        var engine = new SearchEngine();

        var path = "D:\\Projects\\Lucene\\SemanticSearch\\AppConsole\\Assets\\schema.xml";
        engine.ConfigureLucene(path);

        var documents = new DocumentModel();
        documents.SetField("Titulo", "Documento 9");
        documents.SetField("Descricao", "Lucene.NET Avançado");
        var tags = new List<string> { "perifericos", "gamer", "usb-c", "rgb" };
        documents.SetField("Categoria", tags);
        documents.SetField("SKU", "Um guia completo sobre padrões de projeto.");

        engine.AddDocument(documents);

        engine.GetAll().ForEach(doc =>
        {
            Console.WriteLine("Documento:");
            foreach (var field in doc.Fields)
            {
                var value = field.Value is IEnumerable<string> list ? string.Join(',', list) : field.Value;
                Console.WriteLine($"{field.Key}: {value}");
            }
            Console.WriteLine();
        });
    }

    public static void InferenceEmbbedings()
    {
        var executor = new OnnxExecutor(new NerPostProcessor());

        string texto = "My name is Wolfgang and I live in Berlin";
        var prompt = executor.BuildPrompt(texto);
        var embeddings = executor.GenerateEmbedding(prompt);

    }

    public static void InferenceNer()
    {
        var executor = new OnnxExecutor(new NerPostProcessor());

        // 1. Texto que contém as entidades
        string texto = "My name is Wolfgang and I live in Berlin";

        // 2. Constrói o prompt especializado em NER
        var prompt = executor.BuildPrompt(texto);

        // 3. Executa a inferência para obter a string JSON
        string jsonResposta = executor.NerInference();

        try
        {
            // 4. Converte o JSON de volta para classes C#
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            NerResponse nerResult = JsonSerializer.Deserialize<NerResponse>(jsonResposta, options);

            // 5. Exibe os resultados extraídos pela IA
            Console.WriteLine("\n--- Entidades Encontradas ---");
            foreach (var entity in nerResult.Entities)
            {
                Console.WriteLine($"[ {entity.Category} ] -> {entity.Value}");
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("\nErro: O modelo não retornou um JSON válido.");
            Console.WriteLine("Resposta bruta da IA:");
            Console.WriteLine(jsonResposta);
        }
    }
}