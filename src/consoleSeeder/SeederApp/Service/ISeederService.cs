using SeederApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeederApp.Service
{
    public interface ISeederService
    {
        Task<IEnumerable<ActivityResult>> StartProcessingTasks(int index);


    }
}
