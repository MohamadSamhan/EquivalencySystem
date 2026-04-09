using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Interface
{
    public interface ISimilarityService
    {
        /// <summary>
        /// Calculate semantic similarity between two texts as a value between 0.0 and 1.0.
        /// </summary>
        double CalculateSimilarity(string a, string b);

        // New: return embedding vector for a text (null if not available).
        Task<double[]?> GetEmbeddingAsync(string text);
    }
}
