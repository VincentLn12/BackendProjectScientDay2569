namespace ProjectScientDay2568.Service
{
    public class EducationLevelService : IEducationLevelService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public EducationLevelService(ApplicationDbContext context ,IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<EducationLevelDto>> GetEducationLevels()
        {
            var educationLevels = await _context.EducationLevels.ToListAsync();
            return _mapper.Map<List<EducationLevelDto>>(educationLevels);
        }
        public async Task<EducationLevelDto> GetEducationLevelsId(int id)
        {
            var educationLevel = await _context.EducationLevels.FindAsync(id);
            if (educationLevel == null)
            {
                throw new KeyNotFoundException($"Year with ID {id} not found.");
            }
            return _mapper.Map<EducationLevelDto>(educationLevel);
        }

        public async Task<EducationLevelDto> CreateEducationLevelsAsync(EducationLevelDto educationLevelDto)
        {
            if (educationLevelDto == null)
            {
                throw new ArgumentNullException(nameof(educationLevelDto), "educationLevelDto cannot be null.");
            }

            var educationLevel = _mapper.Map<EducationLevel>(educationLevelDto);

            await _context.EducationLevels.AddAsync(educationLevel);
            await _context.SaveChangesAsync();

            return _mapper.Map<EducationLevelDto>(educationLevel);
        }
        public async Task<EducationLevelDto> UpdateEducationLevelsAsync(int id,EducationLevelDto educationLevelDto)
        {
            if (educationLevelDto == null)
            {
                throw new ArgumentNullException(nameof(educationLevelDto), "educationLevelDto cannot be null.");
            }

            var educationLevelToUpdate = await _context.EducationLevels.FindAsync(id);
            if (educationLevelToUpdate == null)
            {
                throw new KeyNotFoundException($"educationLevelToUpdate with ID {id} not found.");
            }

            _mapper.Map(educationLevelDto, educationLevelToUpdate);

            await _context.SaveChangesAsync();
            return _mapper.Map<EducationLevelDto>(educationLevelToUpdate);
        }
        public async Task DeleteEducationLevelAsync(int id)
        {
            var educationLevel = await _context.EducationLevels.FindAsync(id);

            if (educationLevel == null) throw new KeyNotFoundException("ไม่พบระดับชั้น");

            _context.EducationLevels.Remove(educationLevel);
            await _context.SaveChangesAsync();
        }

    }
}
