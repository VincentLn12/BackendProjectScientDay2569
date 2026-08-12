using ProjectScientDay2568.Models.Dtos.Team;
using ProjectScientDay2568.Models.YourNamespace.Models;

namespace ProjectScientDay2568.Service
{
    public class SchoolService : ISchoolService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SchoolService(ApplicationDbContext context , IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<SchoolDto>> GetSchool()
        {
            var school = await _context.School.AsNoTracking().ToListAsync();
            return _mapper.Map<List<SchoolDto>>(school);
        }

        public async Task<List<SchoolDto>> SearchSchoolsAsync(string? query, int limit = 50)
        {
            var q = _context.School.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                // เบื้องต้น: partial match
                q = q.Where(s => s.school_name.Contains(query));
                // ถ้าใช้ SQL Server และอยากไม่สนตัวพิมพ์/วรรณยุกต์ ให้พิจารณา collation CI_AI
                // q = q.Where(s => EF.Functions.Collate(s.SchoolName, "Thai_100_CI_AI")!.Contains(query));
            }

            var results = await q
                .OrderBy(s => s.school_code)
                .Take(limit)
                .ToListAsync();

            return _mapper.Map<List<SchoolDto>>(results);
        }

        public async Task<SchoolDto?> GetByCodeAsync(int schoolCode)
        {
            var s = await _context.School.AsNoTracking()
                .FirstOrDefaultAsync(x => x.school_code == schoolCode);

            return s == null ? null : _mapper.Map<SchoolDto>(s);
        }
        public async Task<SchoolDto> CreateSchoolAsync(SchoolDto schoolDto)
        {
            if (schoolDto == null)
            {
                throw new ArgumentNullException(nameof(schoolDto), "school cannot be null.");
            }
            // ตรวจสอบว่ามีโรงเรียนที่มีชื่อเดียวกันอยู่แล้วหรือไม่
            var existingSchool = await _context.School
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.school_name == schoolDto.school_name);
            if (existingSchool != null)
            {
                throw new InvalidOperationException($"โรงเรียนที่มีชื่อ '{schoolDto.school_name}' มีอยู่แล้ว");
            }

            var school = _mapper.Map<Schools>(schoolDto);

            await _context.School.AddAsync(school);
            await _context.SaveChangesAsync();

            return _mapper.Map<SchoolDto>(school);
        }
        public async Task<SchoolDto> UpdateSchooAsync(int schoolCode, SchoolDto schoolDto)
        {
            if (schoolDto == null)
            {
                throw new ArgumentNullException(nameof(schoolDto), "school cannot be null.");
            }
           
            var schoolToUpdate = await _context.School.FindAsync(schoolCode);
            if (schoolToUpdate == null)
            {
                throw new KeyNotFoundException($"school with ID {schoolCode} not found.");
            }
            
            _mapper.Map(schoolDto, schoolToUpdate);
            await _context.SaveChangesAsync();

            return _mapper.Map<SchoolDto>(schoolToUpdate);
        }
        public async Task DeleteSchooAsync(int schoolCode)
        {
            var school = await _context.School.FindAsync(schoolCode);

            if (school == null) throw new KeyNotFoundException("ไม่พบผู้เล่น");

            _context.School.Remove(school);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetProvincesAsync()
        {
            return await _context.School.AsNoTracking()
                .Where(s => !string.IsNullOrEmpty(s.province))
                .Select(s => s.province!)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }
        public async Task<List<string>> GetDistrictsByProvinceAsync(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return new List<string>();

            return await _context.School.AsNoTracking()
                .Where(s => s.province == province && !string.IsNullOrEmpty(s.district))
                .Select(s => s.district!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }
        public async Task<List<SchoolDto>> SearchByProvinceDistrictAsync(
            string province,
            string district,
            string? q,
            int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(district))
                return new List<SchoolDto>();

            var queryable = _context.School.AsNoTracking()
                .Where(s => s.province == province && s.district == district);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                queryable = queryable.Where(s => s.school_name.Contains(kw));     
            }

            var results = await queryable
                .OrderBy(s => s.school_name)
                .Take(limit)
                .ToListAsync();

            return _mapper.Map<List<SchoolDto>>(results);
        }
    }
}
