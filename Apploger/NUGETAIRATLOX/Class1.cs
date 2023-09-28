using BERTTokenizers;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
namespace NuGetAnswering
{
    public class BertModel
    {
        private static InferenceSession session;
        private static string modelurl;
        private static string modelpath;
        static public Queue<string> progress = new Queue<string>();
        CancellationToken cancelToken;
        public BertModel(string url, string path, CancellationToken token = default)
        {
            modelpath = url;
            modelurl = path;
            cancelToken = token;

        }
        public async Task Create()
        {
            if (!File.Exists(modelpath))
            {
                await DownloadModelWithRetryAsync();
            }
            session = new InferenceSession(modelpath);
        }


        public Task<string> answer(string text, string question)
        {
            return Task.Factory.StartNew(() => {
                try
                {
                    cancelToken.ThrowIfCancellationRequested();
                    var sentence = "{\"question\": \"" + question + "\", \"context\": \"@CTX\"}".Replace("@CTX", text);
                    var tokenizer = new BertUncasedLargeTokenizer();
                    var tokens = tokenizer.Tokenize(sentence);
                    var encoded = tokenizer.Encode(tokens.Count(), sentence);
                    var bertInput = new BertInput()
                    {
                        InputIds = encoded.Select(t => t.InputIds).ToArray(),
                        AttentionMask = encoded.Select(t => t.AttentionMask).ToArray(),
                        TypeIds = encoded.Select(t => t.TokenTypeIds).ToArray(),
                    };

                    var input_ids = ConvertToTensor(bertInput.InputIds, bertInput.InputIds.Length);
                    var attention_mask = ConvertToTensor(bertInput.AttentionMask, bertInput.InputIds.Length);
                    var token_type_ids = ConvertToTensor(bertInput.TypeIds, bertInput.InputIds.Length);
                    var input = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", input_ids),
                                                            NamedOnnxValue.CreateFromTensor("input_mask", attention_mask),
                                                            NamedOnnxValue.CreateFromTensor("segment_ids", token_type_ids) };

                    cancelToken.ThrowIfCancellationRequested();
                    IDisposableReadOnlyCollection<DisposableNamedOnnxValue>? output;
                    lock (session)
                    {
                        output = session.Run(input);
                    }
                    cancelToken.ThrowIfCancellationRequested();

                    List<float> startLogits = (output.ToList().First().Value as IEnumerable<float>).ToList();
                    List<float> endLogits = (output.ToList().Last().Value as IEnumerable<float>).ToList();

                    var startIndex = startLogits.ToList().IndexOf(startLogits.Max());
                    var endIndex = endLogits.ToList().IndexOf(endLogits.Max());
                    var predictedTokens = tokens
                                .Skip(startIndex)
                                .Take(endIndex + 1 - startIndex)
                                .Select(o => tokenizer.IdToToken((int)o.VocabularyIndex))
                                .ToList();

                    var answer = String.Join(" ", predictedTokens);
                    cancelToken.ThrowIfCancellationRequested();
                    return answer;
                }
                catch (OperationCanceledException) { return "Operation was cancelled"; }
                catch (Exception ex) { return ex.Message; }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        }
        public async Task DownloadModelWithRetryAsync()
        {
            int maxr = 5, ind = 0;
            while (ind < maxr)
            {
                try
                {
                    var client = new HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(5);
                    var response = await client.GetAsync(modelurl);
                    response.EnsureSuccessStatusCode();
                    using var stream = await client.GetStreamAsync(modelurl);
                    using var fileStream = new FileStream(modelpath, FileMode.CreateNew);
                    await stream.CopyToAsync(fileStream);
                    return;
                }
                catch (Exception ex)
                {
                    ind ++;
                    throw;
                }
            }
        }

        public class BertInput
        {
            public long[] InputIds { get; set; }
            public long[] AttentionMask { get; set; }
            public long[] TypeIds { get; set; }
        }
        public static Tensor<long> ConvertToTensor(long[] inputArray, int inputDimension)
        {
            Tensor<long> input = new DenseTensor<long>(new[] { 1, inputDimension });

            for (var i = 0; i < inputArray.Length; i++)
            {
                input[0, i] = inputArray[i];
            }
            return input;
        }
    }
}