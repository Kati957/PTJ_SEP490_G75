using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.AIService
{
    public interface IAIService
    {
        Task<float[]> CreateEmbeddingAsync(string text);
        Task UpsertVectorAsync(string ns, string id, float[] vector, object metadata);
        Task<List<(string Id, double Score)>> QuerySimilarAsync(string ns, float[] vector, int topK);
    }
}
