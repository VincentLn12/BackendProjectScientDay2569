using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Service
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserContextService _userContext;

        public StudentService(ApplicationDbContext context, IMapper mapper, UserContextService userContext)
        {
            _context = context;
            _mapper = mapper;
            _userContext = userContext;
        }

        private async Task EnsureRegistrationOpenAsync(int? teamId)
        {
            if (_userContext.IsStaffOrAdmin) return;

            if (!teamId.HasValue)
                throw new ArgumentException("ต้องระบุทีม");

            var closeDate = await _context.Teams
                .Where(team => team.TeamID == teamId.Value)
                .Select(team => team.CompetitionLevel!.CompetitionYear!.Year!.RegistrationCloseDate)
                .SingleOrDefaultAsync();

            if (closeDate.HasValue && DateTime.Now > closeDate.Value)
                throw new InvalidOperationException("ปิดรับลงทะเบียนแล้ว ไม่สามารถแก้ไขหรือจัดการสมาชิกได้");
        }
        public async Task<List<StudentDto>> GetAllStudentAsync(int teamId)
        {
            var students = await _context.Students
                .Where(p => p.TeamID == teamId)
                .Include(p => p.Team)
                .ToListAsync();
            return _mapper.Map<List<StudentDto>>(students);
        }
        public async Task<StudentDto> CreateStudentAsync(StudentDto studentDto)
        {
            if (studentDto == null)
            {
                throw new ArgumentNullException(nameof(studentDto), "student cannot be null.");
            }

            await EnsureRegistrationOpenAsync(studentDto.TeamID);

            // นับจำนวนตัวจริงและตัวสำรองจากฐานข้อมูล
            int mainCount = await _context.Students.CountAsync(s => s.TeamID == studentDto.TeamID && s.IsMainMember == true);
            int reserveCount = await _context.Students.CountAsync(s => s.TeamID == studentDto.TeamID && s.IsMainMember == false);

            if (studentDto.IsMainMember && mainCount >= 5)
            {
                throw new InvalidOperationException("ไม่สามารถเพิ่มผู้เล่นตัวจริงได้เกิน 5 คนในทีมเดียวกัน");
            }

            if (!studentDto.IsMainMember && reserveCount >= 2)
            {
                throw new InvalidOperationException("ไม่สามารถเพิ่มผู้เล่นตัวสำรองได้เกิน 2 คนในทีมเดียวกัน");
            }
           

            var student = _mapper.Map<Student>(studentDto);

            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> UpdateStudentAsync(int id, StudentDto studentDto)
        {
            if (studentDto == null)
            {
                throw new ArgumentNullException(nameof(studentDto), "student cannot be null.");
            }

            var studentToUpdate = await _context.Students.FindAsync(id);
            if (studentToUpdate == null)
            {
                throw new KeyNotFoundException($"student with ID {id} not found.");
            }

            await EnsureRegistrationOpenAsync(studentToUpdate.TeamID);

            // สมมุติใช้ TeamId เช่นเดียวกับ create
            if (studentDto.TeamID == null)
            {
                throw new ArgumentException("TeamId must be provided.");
            }

            // นับจำนวนตัวจริงในทีมนี้ ยกเว้นตัวที่จะอัปเดต
            int mainCount = await _context.Students.CountAsync(s =>
                s.TeamID == studentDto.TeamID &&
                s.IsMainMember == true &&
                s.StudentID != id
            );

            int reserveCount = await _context.Students.CountAsync(s =>
                s.TeamID == studentDto.TeamID &&
                s.IsMainMember == false &&
                s.StudentID != id
            );

            // ตรวจสอบเฉพาะกรณีที่มีการเปลี่ยนสถานะ
            if (studentDto.IsMainMember && !studentToUpdate.IsMainMember && mainCount >= 5)
            {
                throw new InvalidOperationException("ไม่สามารถอัปเดตเป็นตัวจริงได้เกิน 5 คนในทีมนี้");
            }

            if (!studentDto.IsMainMember && studentToUpdate.IsMainMember && reserveCount >= 2)
            {
                throw new InvalidOperationException("ไม่สามารถอัปเดตเป็นตัวสำรองได้เกิน 2 คนในทีมนี้");
            }

            _mapper.Map(studentDto, studentToUpdate);
            await _context.SaveChangesAsync();

            return _mapper.Map<StudentDto>(studentToUpdate);
        }

        public async Task DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null) throw new KeyNotFoundException("ไม่พบผู้เล่น");

            await EnsureRegistrationOpenAsync(student.TeamID);

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }
       
    }
}
