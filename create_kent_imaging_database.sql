CREATE TABLE "ActivityLog" (
	"ID"	TEXT NOT NULL,
	"ActivityDateTime"	TEXT NOT NULL,
	"UserID"	TEXT NOT NULL,
	"MachineIP"	TEXT NOT NULL,
	"Operation"	TEXT NOT NULL,
	"Reason"	TEXT NOT NULL,
	PRIMARY KEY("ID")
);

CREATE TABLE "Patients" (
	"ID"	TEXT NOT NULL,
	"FirstName"	TEXT,
	"MiddleName"	TEXT,
	"LastName"	TEXT,
	"DateOfBirth" TEXT,
	"CreatedOn"	TEXT,
	"UpdatedOn"	TEXT,
	"CreatedBy"	TEXT,
	"UpdatedBy"	TEXT,
	PRIMARY KEY("ID")
);

CREATE TABLE "Series" (
	"ID"	TEXT NOT NULL,
	"SeriesDate"	TEXT NOT NULL,
	"SeriesDescription"	TEXT NOT NULL,
	"StudyID"	TEXT NOT NULL,
	"DrapeUsedAtCapture"	INTEGER NOT NULL,
	"Thumbnail" BLOB, 
	PRIMARY KEY("ID")
);

CREATE TABLE "SeriesImage" (
	"ID"	TEXT NOT NULL,
	"ImageType"	TEXT NOT NULL,
	"Image"	BLOB NOT NULL,
	"OriginalFileName" TEXT,
	"SeriesID"	TEXT NOT NULL,
	PRIMARY KEY("ID")
);

CREATE TABLE "Study" (
	"ID"	TEXT NOT NULL,
	"StudyDate"	TEXT NOT NULL,
	"StudyDescription"	TEXT,
	"PatientID"	TEXT NOT NULL,
	PRIMARY KEY("ID")
);

CREATE TABLE "UserPatients" (
	"PatientID"	TEXT NOT NULL,
	"UserID"	TEXT NOT NULL,
	PRIMARY KEY("PatientID","UserID")
);

CREATE TABLE "Users" (
	"ID"	TEXT NOT NULL,
	"Name"	TEXT NOT NULL,
	"Email"	TEXT NOT NULL,
	"CreatedOn"	TEXT NOT NULL,
	"CreatedBy"	TEXT NOT NULL,
	"UpdatedOn"	TEXT NOT NULL,
	"UpdatedBy"	TEXT,
	PRIMARY KEY("ID")
);

