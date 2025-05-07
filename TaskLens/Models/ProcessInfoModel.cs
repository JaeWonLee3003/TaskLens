using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TaskLens.Models
{
    public class ProcessInfoModel
    {
        public string Name { get; set; }
        public float Cpu { get; set; }
        public float Ram { get; set; }
        public string AiDescription { get; set; } // ← Ollama 설명 결과 저장

         public ImageSource Icon { get; set; } 
    }



}
