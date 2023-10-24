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
        CancellationToken ctoken = cts.Token;
        var answerTask = new BertModel(ctoken);
        await answerTask.Create();
        var tasks = new List<Task>();
        while (!ctoken.IsCancellationRequested)
        {
            try
            {
                Console.Write("Ask a question or press enter to exit: ");
            string question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
            {
                cts.Cancel();
                ctoken.ThrowIfCancellationRequested();
            }
                var task = answerTask.answer(text, question).ContinueWith(task => { Console.WriteLine(question + " : " + task.Result); });
                tasks.Add(task);
            }
            catch (OperationCanceledException) {
                Console.ForegroundColor = ConsoleColor.Red; // устанавливаем цвет
                Console.WriteLine("Operation was cancelled");
                Console.ResetColor();
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red; // устанавливаем цвет
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

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