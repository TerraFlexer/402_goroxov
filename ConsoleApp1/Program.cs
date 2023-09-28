using NuGetAnswering;
class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please specify file in arguments");
            return;
        }

        string FilePath = args[0];
        /*FilePath = "C:\\Users\\gameh\\source\\repos\\Apploger\\Apploger\\hobbit.txt";*/

        string text = ReadFile(FilePath);
        Console.WriteLine(text);
        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken cancelToken = cts.Token;
        string modelUrl = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        string modelPath = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        var answerTask = new BertModel(modelUrl, modelPath, cancelToken);
        await answerTask.Create();
        var tasks = new List<Task>();
        while (!cancelToken.IsCancellationRequested)
        {
            Console.Write("Ask a question or press enter to exit: ");
            string question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
                cts.Cancel();

            var task = answerTask.GetAnswerAsync(text, question).ContinueWith(task => { Console.WriteLine(question + " : " + task.Result); });
            tasks.Add(task);

        }
        await Task.WhenAll(tasks);


    }

    static string ReadFile(string path)
    {
        StreamReader reader = null;
        try
        {
            reader = new StreamReader(path);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception loading from file: {ex.Message}");
            return null;
        }
        finally
        {
            if (reader != null)
            {
                reader.Dispose();
            }
        }
    }
}