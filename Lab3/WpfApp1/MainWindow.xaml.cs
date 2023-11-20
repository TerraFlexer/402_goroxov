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
using Database;
using static System.Net.Mime.MediaTypeNames;
using Database.DAL;
using DataBase;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Path = System.IO.Path;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        BertModel bertModel;
        CancellationTokenSource cts;
        CancellationToken ctoken;
        string fileContent;
        int file_id;

        public MainWindow()
        {
            InitializeComponent();
            cts = new CancellationTokenSource();
            ctoken = cts.Token;
            bertModel = new BertModel(ctoken);
            LoadModelAsync();
            fileContent = null;
            Init_Old_Dialog();
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

                string filename = Path.GetFileName(selectedFilePath);

                chatHistoryTextBox.Text += "Система: Текст из файла успешно загружен.\nЕго содержимое:\n";
                chatHistoryTextBox.Text += fileContent + "\n";
                using (var context = new DataBaseContext())
                {
                    var exist_file = context.Files.FirstOrDefault(t => t.FileName == filename);
                    if (exist_file != null)
                    {
                        file_id = exist_file.ID;
                    }
                    else
                    {
                        var file_element = new FileText() { FileName = filename };
                        context.Files.Add(file_element);
                        file_id = file_element.ID;
                        context.SaveChanges();
                        /*chatHistoryTextBox.Text += file_element.FileName + file_element.ID + "\n";*/
                    }
                }
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
                string answer = "";
                using (var context = new DataBaseContext())
                {
                    var exist_answer = context.QA.FirstOrDefault(t => t.Question == userQuestion && t.FileID == file_id);

                    if (exist_answer != null)
                    {
                        answer = "Вы уже задавали этот вопрос,  ответ на него: " + exist_answer.Answer;
                    }
                    else
                    {
                        answer = await bertModel.answer(fileContent, userQuestion);

                        var qa_element = new QuestionAnswer() { Question = userQuestion, Answer = answer, FileID = file_id };
                        context.QA.Add(qa_element);
                        context.SaveChanges();
                    }
                }

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
        private void Clear_database(object sender, RoutedEventArgs e)
        {
            using (var context = new DataBaseContext())
            {
                context.QA.RemoveRange(context.QA);
                context.SaveChanges();
            }
            chatHistoryTextBox.Text = string.Empty;
        }
        public void Init_Old_Dialog()
        {
            using (var context = new DataBaseContext())
            {
                foreach (var cat in context.QA.ToList())
                {
                    chatHistoryTextBox.Text += $"Пользователь: {cat.Question}\n";
                    chatHistoryTextBox.Text += $"Система: {cat.Answer}\n";
                }
            }
        }
    }
}