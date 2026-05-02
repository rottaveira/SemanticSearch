using InferenceCore.Entity;
using InferenceCore.NER.Impl;
using InferenceCore.ONNX.Impl;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        Inference();
        //var engine = new SearchEngine();

        //var path = "D:\\Projects\\Lucene\\SemanticSearch\\AppConsole\\Assets\\schema.xml"; 
        //engine.ConfigureLucene(path);

        //var documents = new DocumentModel();
        //documents.SetField("Titulo", "Documento 9");
        //documents.SetField("Descricao", "Lucene.NET Avançado");
        //var tags = new List<string> { "perifericos", "gamer", "usb-c", "rgb" };
        //documents.SetField("Categoria", tags); 
        //documents.SetField("SKU", "Um guia completo sobre padrões de projeto.");

        //engine.AddDocument(documents);

        //engine.GetAll().ForEach(doc =>
        //{
        //    Console.WriteLine("Documento:");
        //    foreach (var field in doc.Fields)
        //    {
        //        var value = field.Value is IEnumerable<string> list ? string.Join(',', list) : field.Value;
        //        Console.WriteLine($"{field.Key}: {value}");
        //    }
        //    Console.WriteLine();
        //});
    } 


    public static void Inference()
    {
        var executor = new OnnxExecutor(new OnnxInferenceEngine(), new NerPostProcessor());

        // 1. Texto que contém as entidades
        string texto = "Fiat, Jeep, Mercedes, McLaren, são marcas famosas. Obama, Neymar e Ronaldo possuem todas elas na sede da fifa e no senado. In the White House and eiffel tower we can see som residents. Pero en espanha ou Barcelona tambien";

        // 2. Constrói o prompt especializado em NER
        var prompt = executor.BuildPrompt(texto);

        // 3. Executa a inferência para obter a string JSON
        string jsonResposta = executor.PromptInference();

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