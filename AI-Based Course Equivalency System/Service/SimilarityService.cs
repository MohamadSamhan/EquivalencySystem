using Domine.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service
{
    public class SimilarityService : ISimilarityService
    {
        // Common English stop words to filter out for better comparison
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the","a","an","and","or","but","in","on","at","to","for","of","with","by",
            "from","is","are","was","were","be","been","being","have","has","had","do",
            "does","did","will","would","could","should","may","might","shall","can",
            "this","that","these","those","it","its","not","no","as","if","then","than",
            "so","up","out","about","into","over","after","before","between","under",
            "again","further","once","here","there","when","where","why","how","all",
            "each","every","both","few","more","most","other","some","such","only","own",
            "same","too","very","just","also","well","back","even","still","new","old"
        };

        public double CalculateSimilarity(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
                return 1.0;
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return 0.0;

            var tokensA = Tokenize(a);
            var tokensB = Tokenize(b);

            if (tokensA.Length == 0 || tokensB.Length == 0)
                return 0.0;

            // 1) Jaccard similarity on unique tokens
            var setA = new HashSet<string>(tokensA);
            var setB = new HashSet<string>(tokensB);
            var intersectionCount = setA.Intersect(setB).Count();
            var unionCount = setA.Union(setB).Count();
            var jaccard = unionCount > 0 ? (double)intersectionCount / unionCount : 0.0;

            // 2) Cosine similarity using term frequency vectors
            var freqA = BuildFrequencyMap(tokensA);
            var freqB = BuildFrequencyMap(tokensB);
            var allTerms = freqA.Keys.Union(freqB.Keys);

            double dot = 0, normA = 0, normB = 0;
            foreach (var term in allTerms)
            {
                freqA.TryGetValue(term, out var fa);
                freqB.TryGetValue(term, out var fb);
                dot += fa * fb;
                normA += fa * fa;
                normB += fb * fb;
            }

            var cosine = (normA > 0 && normB > 0)
                ? dot / (Math.Sqrt(normA) * Math.Sqrt(normB))
                : 0.0;

            // 3) Bigram overlap for phrase-level similarity
            var bigramsA = BuildBigrams(tokensA);
            var bigramsB = BuildBigrams(tokensB);
            var bigramIntersection = bigramsA.Intersect(bigramsB).Count();
            var bigramUnion = bigramsA.Union(bigramsB).Count();
            var bigramSimilarity = bigramUnion > 0 ? (double)bigramIntersection / bigramUnion : 0.0;

            // Weighted combination: cosine (50%) + jaccard (30%) + bigrams (20%)
            var combined = (cosine * 0.50) + (jaccard * 0.30) + (bigramSimilarity * 0.20);

            return Math.Clamp(combined, 0.0, 1.0);
        }

        private static string[] Tokenize(string input)
        {
            var cleaned = Regex.Replace(input.ToLowerInvariant(), @"[^\w\s]", " ");
            return cleaned
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length > 1 && !StopWords.Contains(s))
                .ToArray();
        }

        private static Dictionary<string, int> BuildFrequencyMap(string[] tokens)
        {
            var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tokens)
            {
                if (freq.TryGetValue(t, out var count))
                    freq[t] = count + 1;
                else
                    freq[t] = 1;
            }
            return freq;
        }

        private static HashSet<string> BuildBigrams(string[] tokens)
        {
            var bigrams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                bigrams.Add($"{tokens[i]}_{tokens[i + 1]}");
            }
            return bigrams;
        }

        public Task<double[]?> GetEmbeddingAsync(string text) => Task.FromResult<double[]?>(null);
    }
}
