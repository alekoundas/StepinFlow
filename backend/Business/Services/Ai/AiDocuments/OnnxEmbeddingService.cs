using System.Numerics.Tensors;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace Business.Services.Ai.AiDocuments
{
    /// <summary>
    /// Turns text into a vector with bge-small-en-v1.5, on the cpu, locally.
    ///
    /// The model is a loose file beside the exe, so it can be absent - a build that skipped
    /// "npm run model:download", or a user who deleted it. That is reported through
    /// <see cref="IsAvailable"/> rather than thrown, because the assistant still answers without it.
    /// </summary>

    public class OnnxEmbeddingService : IEmbeddingService, IDisposable
    {

        // bge-small-en-v1.5 was trained with these on the query side only, and using them on both
        // sides, or neither, measurably costs recall. Chunks are embedded bare.
        // The wording below is verbatim from training, not a description of it - rewording it
        // costs recall silently.
        private const string _queryPrefix = "Represent this sentence for searching relevant passages: "; // TODO aiprompthelper
        private const int _dimensions = 384;
        private const int _maxTokens = 512;

        private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "AiModels", "model.onnx");
        private readonly string _vocabPath = Path.Combine(AppContext.BaseDirectory, "AiModels", "vocab.txt");

        private readonly Lock _lockObj = new Lock();

        private InferenceSession? _session;
        private BertTokenizer? _tokenizer;
        private bool _isLoadAttempted;

        public int Dimensions => _dimensions;

        // ================================================================
        // Public methods
        // ================================================================

        public bool IsAvailable()
        {
            Load();
            return _session != null && _tokenizer != null;
        }

        // Identity of the loaded model, for anything that caches what the model produced. Length
        // rather than a hash of 127mb: two different models being byte-identical in size is not a
        // thing that happens, and this is read on every start.
        public string ModelFingerprint()
        {
            FileInfo file = new FileInfo(_modelPath);
            return file.Exists ? $"{_dimensions}-{file.Length}" : string.Empty;
        }

        public float[] EmbedChunk(string text)
        {
            return Embed(text);
        }

        public float[] EmbedQuery(string text)
        {
            return Embed(_queryPrefix + text);
        }
        public void Dispose()
        {
            _session?.Dispose();
            GC.SuppressFinalize(this);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // 127mb and a few seconds to load, so it happens once, on first use rather than at startup.
        private void Load()
        {
            if (_isLoadAttempted)
                return;

            lock (_lockObj)
            {
                if (_isLoadAttempted)
                    return;

                try
                {
                    if (!File.Exists(_modelPath) || !File.Exists(_vocabPath))
                        return;

                    using FileStream vocab = File.OpenRead(_vocabPath);
                    _tokenizer = BertTokenizer.Create(vocab, new BertOptions());
                    _session = new InferenceSession(_modelPath);
                }
                catch (Exception)
                {
                    // A half downloaded or corrupt model throws here. Unavailable is the answer,
                    // not a crash - the app does considerably more than answer questions.
                    _session?.Dispose();
                    _session = null;
                    _tokenizer = null;
                }
                finally
                {
                    // Set last, not first: whoever sees this set must also see what was loaded.
                    // Set at all, even on the paths above that give up, so a missing model is not
                    // looked for again on every question.
                    _isLoadAttempted = true;
                }
            }
        }

        private float[] Embed(string text)
        {
            if (!IsAvailable())
                throw new InvalidOperationException($"The embedding model is not available at {_modelPath}.");


            // 1. convert the text chunk into tokens.
            // ex. "Loop repeats steps"  →  [CLS] loop repeats steps [SEP]  →  [101, 7077, 17993, 4084, 102]
            IReadOnlyList<int> ids = _tokenizer!.EncodeToIds(text, _maxTokens, out string? _, out int _);

            // 2. Bulid the 3 tensors model requires.
            // One text at a time, if it gets slow, switch on the batches (Dramatic speed gain). 
            DenseTensor<long> inputIds = new DenseTensor<long>([1, ids.Count]);      // The actual tokens.
            DenseTensor<long> attentionMask = new DenseTensor<long>([1, ids.Count]); // 1 = real token, look at it; 0 = padding, ignore it. (used only when baches are more than 1)
            DenseTensor<long> tokenTypeIds = new DenseTensor<long>([1, ids.Count]);  // used to define a relation between baches            (used only when baches are more than 1)

            for (int i = 0; i < ids.Count; i++)
            {
                inputIds[0, i] = ids[i];
                attentionMask[0, i] = 1;
            }

            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
            };

            // 3. Run the network, with many transformer layers(12 in this case).
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session!.Run(inputs);

            // 4. Get a vector per token (not chunk). [batch, token, dimension]
            // bge pools by taking the [CLS] token - not the mean.
            // we could also just save all the vectors in a technique called "late interaction - ColBERT" but size multiplies A LOT(times 285)! - also needs different model.
            Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> hidden = outputs.First().AsTensor<float>();

            float[] vector = new float[_dimensions];
            for (int i = 0; i < _dimensions; i++)
                vector[i] = hidden[0, 0, i]; //[batch, tokens, 384 vectors] <- always token[0] becase we only need the [CLS] token

            // 5. Set same length(magnitude) on all vectors since we only care about the direction.
            float length = TensorPrimitives.Norm(vector.AsSpan());
            if (length > 0)
                TensorPrimitives.Divide(vector.AsSpan(), length, vector.AsSpan());

            return vector;
        }
    }
}
