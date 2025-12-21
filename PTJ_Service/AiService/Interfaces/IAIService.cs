namespace PTJ_Service.AiService
    {
    public interface IAIService
        {
        // ===============================
        //  Embedding
        // ===============================
        Task<float[]> CreateEmbeddingAsync(string text);

        // ===============================
        // Pinecone – Upsert
        // ===============================
        Task UpsertVectorAsync(
            string ns,
            string id,
            float[] vector,
            object metadata
        );

        //// ===============================
        ////  Pinecone – Query (no filter)
        //// ===============================
        //Task<List<(string Id, double Score)>> QuerySimilarAsync(
        //    string ns,
        //    float[] vector,
        //    int topK
        //);

        // ===============================
        //  Pinecone – Query WITH ID filter
        // (EmployerPostService & JobSeekerPostService ĐANG DÙNG)
        // ===============================
        Task<List<(string Id, double Score)>> QueryWithIDsAsync(
            string ns,
            float[] vector,
            IEnumerable<int> allowedIds,
            int topK = 50
        );

        // ===============================
        //  Pinecone – Delete
        // (AutoFixPostStatusAsync ĐANG DÙNG)
        // ===============================
        Task DeleteVectorAsync(
            string ns,
            string id
        );
        }
    }
