namespace PTJ_Service.AIService
{
    public class AiMatchService
    {
        private readonly OpenAIService _openai;
        private readonly PineconeService _pinecone;

        public AiMatchService(OpenAIService openai, PineconeService pinecone)
        {
            _openai = openai;
            _pinecone = pinecone;
        }

        public async Task SavePostEmbeddingAsync(string type, int id, string title, string desc, string location)
        {
            string text = $"{title}. {desc}. Location: {location}";
            var vector = await _openai.CreateEmbeddingAsync(text);

            await _pinecone.UpsertAsync(
                ns: type == "Employer" ? "employer_posts" : "job_seeker_posts",
                id: $"{type}Post:{id}",
                vector: vector,
                metadata: new { title, location }
            );
        }

        public async Task<List<(string Id, double Percent)>> FindSimilarAsync(string ns, string queryText)
        {
            var vector = await _openai.CreateEmbeddingAsync(queryText);
            var results = await _pinecone.QueryAsync(ns, vector, 5);
            return results.Select(r => (r.Id, Math.Round(r.Score * 100, 2))).ToList();
        }
    }
}
