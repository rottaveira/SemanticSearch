namespace InferenceCore.Entity
{
    public class NerResponse
    {
        public List<NerEntity> Entities { get; set; } = new List<NerEntity>();
    }

    public class NerEntity
    {
        public string Value { get; set; }
        public string Category { get; set; } // PESSOA, LOCAL, ORGANIZACAO, DATA, PRODUTO
    }
}
