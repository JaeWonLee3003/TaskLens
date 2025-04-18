using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskLens.Models
{
    public class ProcessInfoModel
    {
        public string Name { get; set; }
        public float Cpu { get; set; }
        public float Ram { get; set; }
        public string AiDescription { get; set; } // ← Ollama 설명 결과 저장
    }



}
