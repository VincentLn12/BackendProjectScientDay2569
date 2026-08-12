
namespace ProjectScientDay2568.Utility
{
    public static class SD
    {
        public const string Role_Admin = "admin";
        public const string Role_User = "user";

        public enum TeamStatus
        {
            Registered = 0,          // ทีมที่ลงทะเบียนแล้ว
            Verified = 1,            // การลงทะเบียนสำเร็จ
            ProjectRevision = 2,     // รอแก้ไขโครงงาน
            CheckedIn = 3,           // ทีมที่เช็คอินแล้ว
            Disqualified = 4,        // ทีมที่ถูกตัดสิทธิ์
        }

        public enum TitlePrefix
        {     
            นาย,
            นาง,
            นางสาว,
            เด็กชาย,
            เด็กหญิง,
            ว่าที่ร้อยตรี,
            ว่าที่ร้อยตรีหญิง,
            Other
        }

        public enum SignatureRole
        {
            UniversityDean,
            FacultyDean,       
        }

        public enum LogoRole
        {        
            UniversityLogo,
            InstituteLogo
        }


    }

}
