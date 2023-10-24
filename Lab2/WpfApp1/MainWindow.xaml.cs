using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NuGetAnswering;
using static System.Net.Mime.MediaTypeNames;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        BertModel bertModel;
        CancellationTokenSource cts;
        CancellationToken ctoken;
        string fileContent;

        public MainWindow()
        {
            InitializeComponent();
            cts = new CancellationTokenSource();
            ctoken = cts.Token;
            bertModel = new BertModel(ctoken);
            LoadModelAsync();
            fileContent = null;
        }

        private async void LoadModelAsync()
        {
            try
            {
                await bertModel.Create();
                // Модель успешно загружена, активируйте интерфейс
                // Например, разблокируйте кнопку загрузки файла
                chatHistoryTextBox.Text += "Модель загружена\n";
            }
            catch (Exception ex)
            {
                // Обработка ошибок загрузки модели
                MessageBox.Show($"Ошибка загрузки модели: {ex.Message}");
            }
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
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
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

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем OpenFileDialog и настраиваем его параметры
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                fileContent = ReadFile(selectedFilePath);

                chatHistoryTextBox.Text += "Система: Текст из файла успешно загружен.\nЕго содержимое:\n";
                chatHistoryTextBox.Text += fileContent + "\n";
            }
        }

        private async void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            cancelButton.IsEnabled = true;
            answerButton.IsEnabled = false;
            if (fileContent == null)
            {
                chatHistoryTextBox.Text += $"Система: Пожалуйста загрузите файл.\n";
                cancelButton.IsEnabled = false;
                answerButton.IsEnabled = true;
                return;
            }
            // Запросить вопрос от пользователя
            string userQuestion = questionTextBox.Text;

            try
            {
                var answer = await bertModel.answer(fileContent, userQuestion);

                chatHistoryTextBox.Text += $"Пользователь: {userQuestion}\n";
                chatHistoryTextBox.Text += $"Система: {answer}\n";
            }
            catch (Exception ex)
            {
                // Обработка ошибок анализа текста
                if (ctoken.IsCancellationRequested)
                {
                    chatHistoryTextBox.Text += "Система: Анализ текста был прерван пользователем.\n";
                    cancelButton.IsEnabled = false;
                    answerButton.IsEnabled = true;
                    cts.Dispose();
                    cts = new CancellationTokenSource();
                    ctoken = cts.Token;
                    bertModel.change_token(ctoken);
                    return;
                }
                MessageBox.Show($"Ошибка анализа текста: {ex.Message}");
            }
            cancelButton.IsEnabled = false;
            answerButton.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Прервать процесс анализа текста
            cts.Cancel();
        }
    }
}