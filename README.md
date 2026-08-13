# 🚀 Science Week Backend Project (ProjectScientDay2568)

ระบบ Backend API สำหรับงานสัปดาห์วิทยาศาสตร์ คณะวิทยาศาสตร์และเทคโนโลยี มหาวิทยาลัยราชภัฏกาญจนบุรี
พัฒนาด้วย **.NET 8 (ASP.NET Core Web API)** พร้อมระบบยืนยันตัวตน (ASP.NET Core Identity / JWT), Entity Framework Core (SQL Server), Real-time Update (SignalR), และระบบสร้างใบประกาศนียบัตร PDF (QuestPDF/SkiaSharp)

---

## 📁 โครงสร้างโฟลเดอร์แบบละเอียด (Folder Structure)

```text
backend/
├── ProjectScientDay2568/                  # โครงสร้างโปรเจกต์หลัก (Web API Project)
│   ├── Controllers/                       # [API Controllers] รองรับ HTTP Requests (29 Controllers)
│   │   ├── AdminUsersController.cs        # จัดการข้อมูลผู้ใช้งานสำหรับแอดมิน
│   │   ├── AuthController.cs              # ระบบเข้าสู่ระบบ / ลงทะเบียน (Authentication)
│   │   ├── AuthSController.cs             # ระบบการยืนยันตัวตนย่อย
│   │   ├── BranchesController.cs          # จัดการข้อมูลสาขาวิชา
│   │   ├── CertificateConfigController.cs # จัดการตั้งค่าใบประกาศนียบัตรและผู้ลงนาม
│   │   ├── CompetitionController.cs       # จัดการข้อมูลการแข่งขันหลัก
│   │   ├── CompetitionlevelController.cs  # จัดการระดับการแข่งขันย่อย
│   │   ├── CompetitionTypeController.cs   # จัดการประเภทการแข่งขัน
│   │   ├── CompetitionYearController.cs   # จัดการข้อมูลการแข่งขันในแต่ละปี
│   │   ├── EducationLevelController.cs    # จัดการระดับการศึกษา (ประถม/ม.ต้น/ม.ปลาย)
│   │   ├── ExportController.cs           # ส่งออกข้อมูลเป็น PDF / Excel / Zip
│   │   ├── HomeBannersController.cs       # จัดการรูปภาพแบนเนอร์หน้าแรก
│   │   ├── IndividualCheckinController.cs # ระบบเช็คอินรายบุคคล / สแกนเข้าร่วม
│   │   ├── MatchController.cs             # จัดการการจับคู่แข่งขันและผลการแข่งขัน
│   │   ├── NewsController.cs              # จัดการข่าวสารและกิจกรรมประชาสัมพันธ์
│   │   ├── PublicReportController.cs      # รายงานและสถิติสำหรับเผยแพร่สาธารณะ
│   │   ├── ReportController.cs            # ออกรายงานสำหรับแอดมิน
│   │   ├── RewardTypeController.cs        # จัดการประเภทรางวัล (ชนะเลิศ/ชมเชย ฯลฯ)
│   │   ├── RoleController.cs              # จัดการสิทธิ์การใช้งาน (Roles)
│   │   ├── SchoolController.cs            # จัดการข้อมูลโรงเรียน
│   │   ├── StudentController.cs           # จัดการข้อมูลนักเรียน/ผู้เข้าร่วมแข่งขัน
│   │   ├── SystemSettingsController.cs    # ตั้งค่าระบบ ธีม และเมนูหน้าแรก
│   │   ├── TeamAdvisorsController.cs      # จัดการข้อมูลคุณครูที่ปรึกษา
│   │   ├── TeamController.cs              # จัดการข้อมูลทีมแข่งและสถานะทีม
│   │   ├── TeamProjectController.cs       # จัดการอัปโหลดไฟล์โครงการ PDF ของทีม
│   │   ├── TeamRegistrationController.cs  # ระบบสมัครแข่งขันของทีม
│   │   ├── TrainingRegistrationsController.cs # ระบบลงทะเบียนเข้าร่วมอบรม/เวิร์กช็อป
│   │   ├── TrainingtopicsController.cs   # จัดการหัวข้อการอบรม/เวิร์กช็อป
│   │   └── YearController.cs              # จัดการปีการแข่งขัน
│   │
│   ├── Data/                              # [Data Access Layer]
│   │   └── ApplicationDbContext.cs        # EF Core DbContext กำหนด DbSet และ Relations
│   │
│   ├── DataProtectionKeys/                # [Security] จัดเก็บ Key สำหรับ Data Protection
│   │
│   ├── Fonts/                             # [Assets] ฟอนต์ภาษาไทยสำหรับการเจนไฟล์ PDF ใบประกาศนียบัตร
│   │   ├── THSarabun.ttf / Bold / Italic
│   │   └── THSarabunNew.ttf / Bold / Italic
│   │
│   ├── Helper/                            # [Helper Utilities]
│   │   └── FileHelper.cs                  # ฟังก์ชันช่วยจัดการอัปโหลด/บันทึก/ลบไฟล์สื่อ
│   │
│   ├── Hubs/                              # [Real-time Communication]
│   │   └── MatchHub.cs                    # SignalR Hub ส่งข้อมูลผลการแข่งขันแบบ Real-time
│   │
│   ├── Interface/                         # [Service Interfaces] กำหนด Abstractions สำหรับ DI
│   │   ├── ICompetitionService.cs
│   │   ├── ICompetitionTypeService.cs
│   │   ├── ICompetitionTypeYearService.cs
│   │   ├── ICompetitionYearService.cs
│   │   ├── ICompetitionlevelService.cs
│   │   ├── IEducationLevelService.cs
│   │   ├── IRewardTypeService.cs
│   │   ├── ISchoolService.cs
│   │   ├── IStudentService.cs
│   │   ├── ITeamService.cs
│   │   └── IYearService.cs
│   │
│   ├── Mappings/                          # [Object Mapper]
│   │   └── MappingProfile.cs              # AutoMapper Mapping Configurations (Model <-> DTO)
│   │
│   ├── Migrations/                        # [EF Core Migrations] ประวัติการเปลี่ยนแปลงโครงสร้าง DB
│   │
│   ├── Models/                            # [Domain Models / Entity Definitions]
│   │   ├── 📘 [Master Data Models]
│   │   │   ├── ApplicationUser.cs         # โมเดลผู้ใช้งาน (สืบทอดจาก IdentityUser)
│   │   │   ├── Branch.cs                  # โมเดลสาขาวิชา
│   │   │   ├── Competition.cs             # โมเดลการแข่งขัน
│   │   │   ├── CompetitionType.cs         # โมเดลประเภทการแข่งขัน
│   │   │   ├── EducationLevel.cs          # โมเดลระดับการศึกษา
│   │   │   ├── RewardType.cs              # โมเดลประเภทรางวัล
│   │   │   ├── Schools.cs                 # โมเดลโรงเรียน/สถานศึกษา
│   │   │   ├── Trainingtopic.cs           # โมเดลหัวข้อการอบรม
│   │   │   └── Year.cs                    # โมเดลปีการแข่งขัน
│   │   │
│   │   ├── 🔄 [Transaction Data Models]
│   │   │   ├── CompetitionLevel.cs        # โมเดลระดับการแข่งขันย่อย
│   │   │   ├── CompetitionYear.cs         # โมเดลการแข่งขันประจำปี
│   │   │   ├── Match.cs                   # โมเดลตารางการตารางแข่งขัน/จับคู่
│   │   │   ├── Student.cs                 # โมเดลข้อมูลนักเรียน
│   │   │   ├── Team.cs                    # โมเดลทีมแข่งและผู้จัดการทีม
│   │   │   ├── TeamAdvisor.cs             # โมเดลครูที่ปรึกษาประจำทีม
│   │   │   ├── TeamCertificate.cs         # โมเดลออกเลขเกียรติบัตรให้ทีม
│   │   │   ├── TeamProject.cs             # โมเดลโครงการ/เอกสารแนบของทีม
│   │   │   └── TrainingRegistration.cs    # โมเดลการลงทะเบียนอบรม
│   │   │
│   │   ├── ⚙️ [System & Config Models]
│   │   │   ├── CertificateConfig.cs       # โมเดลการตั้งค่าใบประกาศนียบัตรประจำปี
│   │   │   ├── HomeBanner.cs              # โมเดลแบนเนอร์หน้าแรก
│   │   │   ├── News.cs                    # โมเดลข่าวประชาสัมพันธ์
│   │   │   └── SystemSetting.cs           # โมเดลตั้งค่าระบบและอินเทอร์เฟซ
│   │   │
│   │   └── Dtos/                          # Data Transfer Objects สำหรับ Request/Response API
│   │       ├── Auth/                      # DTO ระบบเข้าสู่ระบบ/สมัครสมาชิก
│   │       ├── Competition/               # DTO สำหรับจัดการการแข่งขัน
│   │       ├── Match/                     # DTO สำหรับจับคู่แข่งขัน
│   │       ├── News/                      # DTO สำหรับข่าวสาร
│   │       ├── Report/                    # DTO สำหรับรายงานสถิติ
│   │       ├── Team/                      # DTO สำหรับการสมัครและจัดการทีม
│   │       ├── CloneYearRequest.cs        # DTO สำหรับคัดลอกข้อมูลปีการแข่งขัน
│   │       └── NewsDto.cs                 # DTO รับ-ส่งข้อมูลข่าวสาร
│   │
│   ├── Properties/                        # [Project Properties]
│   │   └── launchSettings.json            # การตั้งค่า Environment / URLs ในการรันพัฒนา
│   │
│   ├── Service/                           # [Business Logic Layer] (19 Services)
│   │   ├── AccountService.cs              # ระบบบัญชีผู้ใช้และความปลอดภัย
│   │   ├── CertificateConfigService.cs    # บริการสร้างและจัดการใบประกาศนียบัตร PDF
│   │   ├── CompetitionService.cs          # บริการจัดการการแข่งขัน
│   │   ├── CompetitionTypeService.cs      # บริการจัดการประเภทการแข่งขัน
│   │   ├── CompetitionYearService.cs      # บริการจัดการปีการแข่งขันย่อย
│   │   ├── CompetitionlevelService.cs     # บริการจัดการระดับการแข่งขัน
│   │   ├── EducationLevelService.cs       # บริการจัดการระดับการศึกษา
│   │   ├── ExportService.cs               # บริการออกรายงาน Excel / ZIP / PDF
│   │   ├── MatchService.cs                # บริการจัดการจับคู่แข่งขันและคำนวณสายแข่ง
│   │   ├── NewsService.cs                 # บริการจัดการข่าวประชาสัมพันธ์
│   │   ├── RewardTypeService.cs           # บริการจัดการรางวัล
│   │   ├── RoleService.cs                 # บริการจัดการสิทธิ์การใช้งาน
│   │   ├── SchoolService.cs               # บริการค้นหา/เพิ่มข้อมูลโรงเรียน
│   │   ├── StudentService.cs              # บริการจัดการนักเรียนผู้เข้าร่วม
│   │   ├── TeamProjectService.cs          # บริการจัดการไฟล์โครงการ PDF
│   │   ├── TeamService.cs                 # บริการจัดการทีม/ตรวจสอบเงื่อนไขสมัคร
│   │   ├── UserContextService.cs          # บริการดึงข้อมูล Context ของผู้ใช้ที่เข้าสู่ระบบ
│   │   ├── UserService.cs                 # บริการจัดการผู้ใช้งานระบบ
│   │   └── YearService.cs                 # บริการจัดการปีการแข่งขันและการ Clone ปี
│   │
│   ├── Utility/                           # [Utilities & Constants]
│   │   └── SD.cs                          # Static Details (Enums: TitlePrefix, TeamStatus ฯลฯ)
│   │
│   ├── wwwroot/                           # [Static Files Root]
│   │   ├── uploads/                       # โฟลเดอร์จัดเก็บไฟล์อัปโหลด (รูปภาพ, เอกสาร PDF)
│   │   ├── assets/                        # ไฟล์ Asset สาธารณะ
│   │   └── certificate-bg.png             # ภาพพื้นหลังเกียรติบัตรเริ่มต้น
│   │
│   ├── Program.cs                         # จุดเริ่มต้นโปรเจกต์ (DI Configuration, Middleware, Pipeline)
│   ├── ProjectScientDay2568.csproj        # ไฟล์ตั้งค่าและ Nuget Dependencies
│   └── appsettings.json                   # ไฟล์ตั้งค่า Connection String และ App Configs
│
└── ProjectScientDay2568.Tests/            # โปรเจกต์สำหรับการทดสอบ Unit Test
    ├── service/                           # Test Cases สำหรับทดสอบ Service Logic
    └── ProjectScientDay2568.Tests.csproj  # ไฟล์คอนฟิก Unit Test
```

---

## 🗄️ สรุปโครงสร้างฐานข้อมูล (Database Schema & Tables)

โครงสร้างตารางฐานข้อมูลอ้างอิงจาก **Models Class Definition** ในระบบ (Entity Framework Core Mapping):

### 1. `AspNetUsers` (Model: `ApplicationUser`)
ตารางจัดการผู้ใช้งานระบบ (สืบทอดจาก `IdentityUser`)
- **Primary Key:** `Id` (string / nvarchar(450))
- **Fields:** `UserName`, `Email`, `PasswordHash`, `PhoneNumber`, `SecurityStamp` ฯลฯ

### 2. `Years` (Model: `Year`)
ตารางข้อมูลปีการแข่งขันสัปดาห์วิทยาศาสตร์
- **Primary Key:** `YearID` (int, Identity)
- **Fields:** 
  - `YearNumber` (int) - ปี พ.ศ. (เช่น 2568)
  - `YearName` (nvarchar(255)) - ชื่อปีการแข่งขัน
  - `isActive` (bit) - สถานะเปิดใช้งานปีปัจจุบัน
  - `CompetitionRegulationsUrl` (nvarchar(max)) - ลิงก์กติกา/ระเบียบการแข่งขัน
  - `DateName` (nvarchar(max)) - ข้อความวันที่จัดงาน
  - `RegistrationOpenDate` / `RegistrationCloseDate` (datetime) - ช่วงเวลาเปิด-ปิดรับสมัคร
- **Relationships:** One-to-Many กับ `CompetitionYears`, One-to-One กับ `CertificateConfig`

### 3. `CompetitionTypes` (Model: `CompetitionType`)
ตารางประเภทการแข่งขัน (เช่น การแข่งขันประกวดโครงงาน, การแข่งขันตอบปัญหา)
- **Primary Key:** `CompetitionTypeID` (int, Identity)
- **Fields:** `CompetitionTypeName` (nvarchar(255))
- **Relationships:** One-to-Many กับ `Competitions`

### 4. `Competitions` (Model: `Competition`)
ตารางรายการแข่งขันหลัก
- **Primary Key:** `CompetitionID` (int, Identity)
- **Foreign Key:** `CompetitionTypeID` -> `CompetitionTypes.CompetitionTypeID`
- **Fields:** 
  - `CompetitionName` (nvarchar(255)) - ชื่อรายการแข่งขัน
  - `CoordinatorName` (nvarchar(255)) - ชื่อผู้ประสานงาน
  - `CoordinatorPhone` (nvarchar(50)) - เบอร์โทรผู้ประสานงาน
  - `RequiresProject` (bit) - ต้องส่งไฟล์โครงงานหรือไม่

### 5. `CompetitionYears` (Model: `CompetitionYear`)
ตารางเชื่อมโยงรายการแข่งขันในแต่ละปีการศึกษา (กำหนดเงื่อนไขการสมัครแต่ละปี)
- **Primary Key:** `CompetitionYearID` (int, Identity)
- **Foreign Keys:**
  - `YearID` -> `Years.YearID`
  - `CompetitionID` -> `Competitions.CompetitionID`
- **Fields:**
  - `CompetitionEndDate` (datetime) - วันสิ้นสุดการแข่งขัน
  - `RegistrationLocation` (nvarchar(max)) - สถานที่จัดแข่งขัน/ลงทะเบียน
  - `IsActive` (bit) - สถานะเปิดการแข่งขันนี้
  - `IsRegistrationOpen` (bit) - เปิดรับสมัครการแข่งขันนี้หรือไม่
  - `AdvisorCount` (int) - จำนวนครูที่ปรึกษาที่อนุญาต
  - `RegistrationMode` (nvarchar(10)) - โหมดการสมัคร (`team` / `individual`)
  - `MaxStudentsPerSchool` (int) - โควตาจำนวนนักเรียนสูงสุดต่อโรงเรียน

### 6. `EducationLevels` (Model: `EducationLevel`)
ตารางระดับการศึกษาของผู้เข้าแข่งขัน
- **Primary Key:** `LevelID` (int, Identity)
- **Fields:** `LevelName` (nvarchar(50)) - เช่น ประถมศึกษา, มัธยมศึกษาตอนต้น, มัธยมศึกษาตอนปลาย

### 7. `CompetitionLevels` (Model: `CompetitionLevel`)
ตารางกำหนดระดับย่อยของการแข่งขันแต่ละรายการในปีนั้นๆ
- **Primary Key:** `CompetitionLevelID` (int, Identity)
- **Foreign Keys:**
  - `CompetitionYearID` -> `CompetitionYears.CompetitionYearID`
  - `LevelID` -> `EducationLevels.LevelID`
- **Fields:**
  - `CompetitionDateTime` (datetime) - วันเวลาแข่งขัน
  - `MainPlayersCount` (int) - จำนวนผู้เล่นตัวจริง
  - `ReservePlayersCount` (int) - จำนวนผู้เล่นสำรอง
  - `IsCertificateNumberGenerated` (bit) - สถานะการเจนเลขเกียรติบัตรแล้วหรือยัง

### 8. `RewardTypes` (Model: `RewardType`)
ตารางประเภทรางวัลการแข่งขัน
- **Primary Key:** `RewardTypeID` (int, Identity)
- **Fields:** `RewardTypeName` (nvarchar(50)) - เช่น ชนะเลิศอันดับ 1, รองชนะเลิศอันดับ 1, รางวัลชมเชย, เข้าร่วม

### 9. `Teams` (Model: `Team`)
ตารางข้อมูลทีมสมัครเข้าแข่งขันและผู้จัดการทีม
- **Primary Key:** `TeamID` (int, Identity)
- **Foreign Keys:**
  - `CompetitionLevelID` -> `CompetitionLevels.CompetitionLevelID`
  - `school_code` -> `School.school_code`
  - `RewardTypeID` -> `RewardTypes.RewardTypeID`
  - `UserId` -> `AspNetUsers.Id`
- **Fields:**
  - `NameOrdinal` (int) - ลำดับชื่อทีมซ้ำ (ทีม A, ทีม B)
  - `RegistrationDateTime` (datetime) - วันเวลาที่สมัคร
  - `PrimaryFirstName`, `PrimaryLastName`, `PrimaryTitlePrefix`, `PrimaryOtherTitlePrefix` - ข้อมูลผู้ดูแลหลัก
  - `SecondaryFirstName`, `SecondaryLastName`, `SecondaryTitlePrefix` ... `Fifth...` - ข้อมูลผู้ดูแลลำดับ 2-5
  - `ProjectName` (nvarchar(max)) - ชื่อโครงงาน
  - `ProjectPDFLink` (nvarchar(255)) - พาธไฟล์โครงงาน PDF
  - `Status` (enum/int) - สถานะทีม (`Pending`, `Approved`, `Rejected` ฯลฯ)
  - `StatusTime` (datetime) - วันเวลาอัปเดตสถานะ
  - `IsLuckyDrawWinner` (bit) - ได้รับรางวัลสุ่ม/กิจกรรมหรือไม่
  - `TeamNumberInLevel` (int) - หมายเลขประจำทีมในการแข่งขัน

### 10. `Students` (Model: `Student`)
ตารางข้อมูลนักเรียน / ผู้เข้าร่วมแข่งขันในทีม
- **Primary Key:** `StudentID` (int, Identity)
- **Foreign Keys:**
  - `TeamID` -> `Teams.TeamID`
  - `RewardTypeID` -> `RewardTypes.RewardTypeID`
- **Fields:**
  - `TitlePrefix` (enum) - คำนำหน้าชื่อ (นาย/นางสาว/เด็กชาย/เด็กหญิง)
  - `FirstName` (nvarchar(255)) - ชื่อ
  - `LastName` (nvarchar(255)) - นามสกุล
  - `IsMainMember` (bit) - เป็นตัวจริง (true) หรือตัวสำรอง (false)
  - `IsCertificateGenerated` (bit) - พิมพ์/สร้างเกียรติบัตรแล้วหรือยัง
  - `IsCheckedIn` (bit) - เช็คอินเข้างานแล้วหรือยัง

### 11. `TeamAdvisors` (Model: `TeamAdvisor`)
ตารางข้อมูลครูที่ปรึกษาประจำทีม
- **Primary Key:** `TeamAdvisorID` (int, Identity)
- **Foreign Key:** `TeamID` -> `Teams.TeamID`
- **Fields:**
  - `SortOrder` (int) - ลำดับครูที่ปรึกษา (ที่ปรึกษาหลัก/รอง)
  - `TitlePrefix` (enum), `OtherTitlePrefix` (nvarchar(100)) - คำนำหน้าชื่อ
  - `FirstName` (nvarchar(100)), `LastName` (nvarchar(100)) - ชื่อ-นามสกุล

### 12. `TeamProjects` (Model: `TeamProject`)
ตารางจัดเก็บไฟล์โครงการ/ผลงานของทีม
- **Primary Key:** `TeamProjectID` (int, Identity)
- **Foreign Key:** `TeamID` -> `Teams.TeamID`
- **Fields:**
  - `ProjectName` (nvarchar(max)) - ชื่อโครงการ
  - `ProjectPDFLink` (nvarchar(max)) - ลิงก์/พาธไฟล์ PDF

### 13. `TeamCertificates` (Model: `TeamCertificate`)
ตารางเกียรติบัตรของทีมและเลขรันเกียรติบัตร
- **Primary Key:** `CertificateID` (int, Identity)
- **Foreign Key:** `TeamID` -> `Teams.TeamID`
- **Fields:**
  - `CertificateNumber` (nvarchar(255)) - เลขที่เกียรติบัตร (เช่น กจ.331/2568)
  - `CertificateName` (nvarchar(max)) - ข้อความเกียรติบัตร
  - `IssuedDate` (datetime) - วันที่ออกใบประกาศ

### 14. `Matches` (Model: `Match`)
ตารางสายการแข่งขันและการจับคู่แข่งขัน
- **Primary Key:** `Id` (int, Identity)
- **Foreign Key:** `CompetitionLevelID` -> `CompetitionLevels.CompetitionLevelID`
- **Fields:**
  - `Round` (int) - รอบการแข่งขัน (เช่น รอบ 1, รอบ 8 ทีม, รอบชิงชนะเลิศ)
  - `TeamA` / `TeamB` (Navigation Properties) - ทีมที่เข้าแข่ง
  - `Winner` (Navigation Property) - ทีมที่ชนะ
  - `Status` (nvarchar(max)) - สถานะแมตช์ (`Pending`, `Finished` ฯลฯ)
  - `MatchTime` (datetime) - เวลาแข่งขัน

### 15. `CertificateConfigs` (Model: `CertificateConfig`)
ตารางตั้งค่าเกียรติบัตร รูปแบบ และผู้ลงนามประจำปี
- **Primary Key & Foreign Key:** `YearID` -> `Years.YearID` (One-to-One)
- **Fields:**
  - `StartNumber` (int) - รหัสเริ่มต้นการรันเลขเกียรติบัตร (เช่น 221)
  - `CertificateName` (nvarchar(max)) - อักษรย่อเกียรติบัตร (เช่น "กจ")
  - `CertificateBackgroundPath` (nvarchar(max)) - พาธภาพพื้นหลังเกียรติบัตร
  - `UniversityDeanTitle` / `UniversityDeanName` / `UniversityDeanSignature` - ตำแหน่ง ชื่อ และพาธลายเซ็นผู้ลงนามมหาวิทยาลัย
  - `FacultyDeanTitle` / `FacultyDeanName` / `FacultyDeanSignature` - ตำแหน่ง ชื่อ และพาธลายเซ็นคณบดีคณะ
  - `UniversityLogoPath` / `InstituteLogoPath` - พาธภาพโลโก้มหาวิทยาลัยและสถาบัน

### 16. `School` (Model: `Schools`)
ตารางข้อมูลโรงเรียนและสถานศึกษา
- **Primary Key:** `school_code` (int, Identity/Key)
- **Fields:**
  - `school_name` (nvarchar(200)) - ชื่อโรงเรียน
  - `subdistrict` (nvarchar(100)) - ตำบล
  - `district` (nvarchar(100)) - อำเภอ
  - `province` (nvarchar(100)) - จังหวัด

### 17. `News` (Model: `News`)
ตารางประชาสัมพันธ์ข่าวสารในระบบ
- **Primary Key:** `Id` (int, Identity)
- **Fields:**
  - `Title` (nvarchar(max)) - หัวข้อข่าว
  - `Content` (nvarchar(max)) - เนื้อหาข่าว
  - `DatePublished` (datetime) - วันที่โพสต์ข่าว
  - `CoverImagePath` (nvarchar(max)) - พาธรูปภาพปก
  - `ContentImagePaths` (List<string> / JSON) - รายการรูปภาพประกอบเนื้อหา

### 18. `SystemSettings` (Model: `SystemSetting`)
ตารางตั้งค่าระบบ ธีม และการเปิด/ปิดเมนูหน้าแรก
- **Primary Key:** `Id` (int, Identity)
- **Fields:**
  - `ProjectName` (nvarchar(255)) / `ProjectSubtitle` (nvarchar(255)) - ชื่อโครงการและข้อความรอง
  - `ShowNavbar` (bit) - แสดงแถบนวาร์หรือไม่
  - `LogoPath` (nvarchar(500)) - พาธโลโก้ระบบ
  - `Theme` (nvarchar(50)) - ธีมสีระบบ (เช่น `emerald`)
  - `ShowTrainingMenu` / `ShowNewsMenu` / `ShowResultsMenu` / `ShowActivityMenu` (bit) - สวิตช์เปิด/ปิดเมนูต่างๆ
  - `HomepageContent` (nvarchar(max)) - การตั้งค่า JSON สำหรับเนื้อหาหน้าแรก
  - `HomeHeroTitle` / `HomeHeroHighlight` / `HomeHeroDescription` - ข้อความส่วน Banner Hero หน้าแรก
  - `HomeDisplayYearId` (int) - ปีการศึกษาที่เลือกแสดงบนหน้าแรก

### 19. `HomeBanners` (Model: `HomeBanner`)
ตารางรูปภาพสไลด์แบนเนอร์หน้าแรก
- **Primary Key:** `Id` (int, Identity)
- **Fields:**
  - `ImagePath` (nvarchar(500)) - พาธไฟล์รูปภาพ
  - `SortOrder` (int) - ลำดับการแสดงผล
  - `IsActive` (bit) - สถานะเปิดใช้งาน

### 20. `Branches` (Model: `Branch`)
ตารางสาขาวิชาภายในคณะวิทยาศาสตร์และเทคโนโลยี
- **Primary Key:** `Id` (int, Identity)
- **Fields:** `Name` (nvarchar(255)) - ชื่อสาขาวิชา (เช่น คอมพิวเตอร์, ฟิสิกส์, เคมี ฯลฯ)

### 21. `Trainingtopics` (Model: `Trainingtopic`)
ตารางหัวข้อการอบรม / เวิร์กช็อป
- **Primary Key:** `Id` (int, Identity)
- **Foreign Keys:**
  - `Branch_id` -> `Branches.Id`
  - `CertificateTemplateId` -> `CertificateConfigs.YearID`
- **Fields:**
  - `Name` (nvarchar(255)) - ชื่อหัวข้อการอบรม
  - `Description` (nvarchar(max)) - รายละเอียดการอบรม
  - `TrainingDate` (datetime) / `StartTime` / `EndTime` - วันและเวลาจัดอบรม
  - `MaxParticipants` (int) - จำนวนผู้เข้าร่วมสูงสุด
  - `IsOpenRegister` (bit) - สถานะเปิดรับสมัคร
  - `RegisterStartDate` / `RegisterEndDate` (datetime) - วันเปิด-ปิดรับสมัคร
  - `Location` (nvarchar(max)) - สถานที่จัดอบรม
  - `TeacherName` (nvarchar(max)) - วิทยากร/ผู้สอน
  - `Trainingtopicsrequirements` (nvarchar(max)) - คุณสมบัติ/ข้อกำหนดผู้สมัคร

### 22. `TrainingRegistrations` (Model: `TrainingRegistration`)
ตารางการลงทะเบียนเข้าร่วมการอบรมของผู้ใช้งาน
- **Primary Key:** `Id` (int, Identity)
- **Foreign Keys:**
  - `TrainingtopicId` -> `Trainingtopics.Id`
  - `UserId` -> `AspNetUsers.Id`
- **Fields:**
  - `Prefix` (nvarchar(max)), `FirstName` (nvarchar(100)), `LastName` (nvarchar(100)) - ข้อมูลผู้สมัครอบรม
  - `RegisteredAt` (datetime) - วันเวลาที่ลงทะเบียน
  - `Status` (nvarchar(max)) - สถานะการลงทะเบียน (เช่น "ลงทะเบียนแล้ว")

---

### 📌 การจำแนกประเภทตาราง (Classification of Database Tables)

เพื่อความเข้าใจในสถาปัตยกรรมข้อมูล ตารางทั้งหมด 22 ตารางสามารถแบ่งออกตามลักษณะการใช้งานได้ดังนี้:

#### 1. 📘 Master File / Master Data (ตารางอ้างอิงหลัก)
ข้อมูลอ้างอิงพื้นฐานของระบบที่มีการเปลี่ยนแปลงน้อย ใช้สำหรับอ้างอิงในตารางอื่นๆ:
- `AspNetUsers` (ผู้ใช้งานระบบ)
- `Years` (ปีการแข่งขันหลัก)
- `CompetitionTypes` (ประเภทการแข่งขัน)
- `Competitions` (รายการแข่งขันหลัก)
- `EducationLevels` (ระดับการศึกษา)
- `RewardTypes` (ประเภทรางวัล)
- `School` (สถานศึกษา/โรงเรียน)
- `Branches` (สาขาวิชา)
- `Trainingtopics` (หัวข้อการอบรม/เวิร์กช็อป)

#### 2. 🔄 Transaction Data (ตารางรายการ / กิจกรรม)
ข้อมูลที่เกิดจากการสมัคร ทำรายการ หรือประมวลผลตามกิจกรรม/ช่วงเวลา:
- `CompetitionYears` (รายการแข่งขันประจำปี)
- `CompetitionLevels` (ระดับย่อยของการแข่งขันแต่ละรายการในปีนั้นๆ)
- `Teams` (ทีมสมัครเข้าแข่งขัน)
- `Students` (นักเรียน/ผู้เข้าแข่งขันประจำทีม)
- `TeamAdvisors` (ครูที่ปรึกษาประจำทีม)
- `TeamProjects` (ไฟล์โครงการ/ผลงานของทีม)
- `TeamCertificates` (เกียรติบัตรและเลขรันเกียรติบัตรของทีม)
- `Matches` (ตารางจับคู่แข่งขันและผลการแข่งขัน)
- `TrainingRegistrations` (การลงทะเบียนเข้าร่วมอบรม)

#### 3. ⚙️ System & Config Data (ตารางตั้งค่าระบบ)
ข้อมูลการตั้งค่า ค่าคอนฟิก และคอนเทนต์สำหรับแสดงผลหน้าเว็บไซต์:
- `CertificateConfigs` (ตั้งค่าแม่แบบเกียรติบัตรและผู้ลงนาม)
- `News` (ข่าวประชาสัมพันธ์)
- `SystemSettings` (ตั้งค่าระบบ ธีม และสวิตช์เปิด/ปิดเมนู)
- `HomeBanners` (รูปภาพสไลด์แบนเนอร์)

---

## 🛠️ เทคโนโลยีที่ใช้ (Tech Stack)

- **Framework:** .NET 8 (ASP.NET Core Web API)
- **Database:** SQL Server
- **ORM:** Entity Framework Core 8.0
- **Authentication:** ASP.NET Core Identity & JWT Bearer Token
- **Real-time:** SignalR Core
- **PDF Generation:** QuestPDF / SkiaSharp
- **Object Mapper:** AutoMapper
