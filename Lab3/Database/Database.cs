using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase
{
    public class FileText
    {
        public int ID { get; set; }
        public string FileName { get; set; }
    }
    public class QuestionAnswer
    {
        public int ID { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public int FileID { get; set; }
        [ForeignKey("FileID")]
        virtual public FileText Filet { get; set; }
    }
}