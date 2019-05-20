using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DataEntities;
using Newtonsoft.Json;

namespace TestSQLite
{
    public class MainViewModel
    {
        private List<PatientInformation> _items = null;

        public MainViewModel()
        {
        }

        public void Process()
        {
            CreateUsers();

            ReadJSON();

            WriteDomainObjectsToDb();
        }

        private void CreateUsers()
        {
            var user = AppSingleton.DatabaseService.CreateUser("keith", "keith@kentimaging.com", true);
            user = AppSingleton.DatabaseService.CreateUser("pierre", "pierre@kentimaging.com", true);
        }

        private void ReadJSON()
        {
            using (StreamReader r = new StreamReader(@"C:\ProgramData\Kent Imaging\patientlist_KentDbver1.1.0.12.db"))
            {
                string json = r.ReadToEnd();
                _items = JsonConvert.DeserializeObject<List<PatientInformation>>(json);
            }
        }

        private void WriteDomainObjectsToDb()
        {
            if (_items != null)
            {
                foreach (var patient in _items)
                {
                    if (string.IsNullOrWhiteSpace(patient.PatientKey))
                    {
                        patient.PatientKey = Guid.NewGuid().ToString();
                    }

                    AppSingleton.DatabaseService.CreatePatient(patient);
                }
            }
        }
    }
}
