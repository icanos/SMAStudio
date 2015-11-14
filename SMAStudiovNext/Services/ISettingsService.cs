using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Services
{
    public interface ISettingsService
    {
        void Save();

        void Load();
    }
}
