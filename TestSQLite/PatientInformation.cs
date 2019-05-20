using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataEntities
{
    public class PatientInformation : INotifyPropertyChanged, IComparable<PatientInformation>
    {
        public enum Genders { Male, Female, Other, Not_Specified}

        public PatientInformation()
        {
            Lastname = "";
            Firstname = "";
            Middlename = "";
            PatientMRN = "";
            DOB_Day = "";
            DOB_Month = "";
            DOB_Year = "";
            AccessionNo = "";
            Gender = Genders.Not_Specified;
            LastImageDate = "";
            FileDirectory = "";
            FlagForAlert = false;
        }

        public string PatientKey { get; set; }

        private String _lastName;
        public String Lastname
        {
            get { return _lastName; }
            set { _lastName = value; InvokePropertyChanged(new PropertyChangedEventArgs("LastName")); }
        }
        private String _firstname;
        public String Firstname
        {
            get { return _firstname; }
            set { _firstname = value; InvokePropertyChanged(new PropertyChangedEventArgs("Firstname")); }
        }
        private String _middlename = "";
        public String Middlename
        {
            get { return _middlename; }
            set { _middlename = value; InvokePropertyChanged(new PropertyChangedEventArgs("Middlename")); }
        }
        private String _patientMRN;
        public String PatientMRN
        {
            get { return _patientMRN; }
            set { _patientMRN = value; InvokePropertyChanged(new PropertyChangedEventArgs("PatientMRN")); }
        }
        private String _dOB_Day;
        public String DOB_Day
        {
            get { return _dOB_Day; }
            set { _dOB_Day = value; InvokePropertyChanged(new PropertyChangedEventArgs("DOB_Day")); }
        }
        private String _dOB_Month;
        public String DOB_Month
        {
            get { return _dOB_Month; }
            set { _dOB_Month = value; InvokePropertyChanged(new PropertyChangedEventArgs("DOB_Month")); }
        }
        private String _dOB_Year;
        public String DOB_Year
        {
            get { return _dOB_Year; }
            set { _dOB_Year = value; InvokePropertyChanged(new PropertyChangedEventArgs("DOB_Year")); }
        }
        private String _referringPhysicianLastName { get; set; }
        public String ReferringPhysicianLastName
        {
            get { return _referringPhysicianLastName; }
            set { _referringPhysicianLastName = value; InvokePropertyChanged(new PropertyChangedEventArgs("ReferringPhysicianLastName")); }
        }
        private String _referringPhysicianFirstName;
        public String ReferringPhysicianFirstName
        {
            get { return _referringPhysicianFirstName; }
            set { _referringPhysicianFirstName = value; InvokePropertyChanged(new PropertyChangedEventArgs("ReferringPhysicianFirstName")); }
        }
        private String _accessionNo;
        public String AccessionNo
        {
            get { return _accessionNo; }
            set { _accessionNo = value; InvokePropertyChanged(new PropertyChangedEventArgs("Firstname")); }
        }
        private Genders _gender;
        public Genders Gender
        {
            get { return _gender; }
            set { _gender = value; InvokePropertyChanged(new PropertyChangedEventArgs("Gender")); }
        }
        private String _fileDirectory;
        public String FileDirectory
        {
            get { return _fileDirectory; }
            set { _fileDirectory = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("FileDirectory")); }
        }

        public string _lastImageDate;
        public string LastImageDate
        {
            get { return _lastImageDate; }
            set { _lastImageDate = value; }
        }

        private bool _flagForAlert;
        public bool FlagForAlert
        {
            get => _flagForAlert;
            set { _flagForAlert = value; }
        }

        public String OriginalValue
        {
            get
            {
                return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}",
                    PatientMRN, Lastname, Firstname, Middlename, Gender, DOB_Year, DOB_Month, DOB_Day, AccessionNo,
                    ReferringPhysicianLastName, ReferringPhysicianFirstName, FileDirectory, LastImageDate,
                    !string.IsNullOrWhiteSpace(PatientKey) ? PatientKey : Guid.NewGuid().ToString());
            }
        }

        public string ToSearchString() => string.Format("{0}\t{1}\t{2}\t{3}\t{4}", PatientMRN, Lastname, Firstname,
            Middlename, DateOfBirth);

        // Helper Methods

        // PatientFullName - returns the full name of a patient (First Middle Last)
        public String PatientFullName { get { return String.Format("{0}, {1} {2}", Lastname, Firstname, Middlename); } }

        // PatientDicomFullName - returns the full name of a patient (First Middle Last)
        public String PatientDicomFullName { get { return String.Format("{0}^{1}^{2}", Lastname, Firstname, Middlename); } }


        // ReferringFullName - returns the full name of a the Referring Physician
        public String ReferringFullName { get { return String.Format("{0}, {1}", ReferringPhysicianLastName, ReferringPhysicianFirstName); } }
       
        // ReferringDicomFullName - returns the full name of a the Referring Physician in Dicom Format
        public String ReferringDicomFullName { get { return String.Format("{0}^{1}", ReferringPhysicianLastName, ReferringPhysicianFirstName); } }

        // DateOfBirth - returns the full date for the date of birth
        public String DateOfBirth { get { return String.Format("{0}/{1}/{2}", DOB_Month, DOB_Day, DOB_Year); } }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }

        #endregion

        public bool PatientsAreTheSame(PatientInformation other)
        {
            if (!String.Equals(this.Lastname, other.Lastname, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!String.Equals(this.Firstname, other.Firstname, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!String.Equals(this.Middlename, other.Middlename, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!String.Equals(this.DateOfBirth, other.DateOfBirth, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!(this.Gender == other.Gender))
                return false;

            // Everything matches so return true!
            return true;

        }

        public static Genders ParseGenderString(string gender)
        {
            if ((gender == null) || (gender.Length == 0)) return Genders.Not_Specified;
            if (gender.StartsWith("F"))
                return Genders.Female;
            else if (gender.StartsWith("M"))
                return Genders.Male;
            else if (gender.StartsWith("O"))
                return Genders.Other;
            else
                return Genders.Not_Specified;
        }

        public PatientInformation Clone()
        {
            var clonedPatient = new PatientInformation();

            clonedPatient.Lastname = this.Lastname;
            clonedPatient.AccessionNo = this.AccessionNo;
            clonedPatient.DOB_Day = this.DOB_Day;
            clonedPatient.DOB_Month = this.DOB_Month;
            clonedPatient.DOB_Year = this.DOB_Year;
            clonedPatient.Gender = this.Gender;
            clonedPatient.PatientMRN = this.PatientMRN;
            clonedPatient.FileDirectory = this.FileDirectory;
            clonedPatient.Firstname = this.Firstname;
            clonedPatient.ReferringPhysicianFirstName =
                this.ReferringPhysicianFirstName;
            clonedPatient.ReferringPhysicianLastName =
                this.ReferringPhysicianLastName;
            clonedPatient.Middlename = this.Middlename;
            clonedPatient.LastImageDate = this.LastImageDate;
            clonedPatient.PatientKey = this.PatientKey;
            clonedPatient.FlagForAlert = this.FlagForAlert;

            return clonedPatient;
        }

        public static void CopyPatientInformation(PatientInformation inboundPatientInformation, PatientInformation outboundPatientInformation)
        {
            outboundPatientInformation.Lastname = inboundPatientInformation.Lastname;
            outboundPatientInformation.AccessionNo = inboundPatientInformation.AccessionNo;
            outboundPatientInformation.DOB_Day = inboundPatientInformation.DOB_Day;
            outboundPatientInformation.DOB_Month = inboundPatientInformation.DOB_Month;
            outboundPatientInformation.DOB_Year = inboundPatientInformation.DOB_Year;
            outboundPatientInformation.Gender = inboundPatientInformation.Gender;
            outboundPatientInformation.PatientMRN = inboundPatientInformation.PatientMRN;
            outboundPatientInformation.FileDirectory = inboundPatientInformation.FileDirectory;
            outboundPatientInformation.Firstname = inboundPatientInformation.Firstname;
            outboundPatientInformation.ReferringPhysicianFirstName =
                inboundPatientInformation.ReferringPhysicianFirstName;
            outboundPatientInformation.ReferringPhysicianLastName =
                inboundPatientInformation.ReferringPhysicianLastName;
            outboundPatientInformation.Middlename = inboundPatientInformation.Middlename;
            outboundPatientInformation.LastImageDate = inboundPatientInformation.LastImageDate;
            outboundPatientInformation.PatientKey = inboundPatientInformation.PatientKey;
            outboundPatientInformation.FlagForAlert = inboundPatientInformation.FlagForAlert;
        }

        public int CompareTo(PatientInformation other)
        {
            if (other == null) return -1;
            return this.PatientFullName.CompareTo(other.PatientFullName);
        }
    }
}
