using ProjectScientDay2568.Models.Dtos.Competition;

namespace ProjectScientDay2568.Service
{
    public class CompetitionTypeService : ICompetitionTypeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CompetitionTypeService(ApplicationDbContext context , IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<CompetitionTypeDto>> GetCompetitionTypesAsync()
        {
            var CompetitionType = await _context.CompetitionTypes.ToListAsync();
            return _mapper.Map<List<CompetitionTypeDto>>(CompetitionType);
        }
        public async Task<CompetitionTypeDto> GetCompetitionTypeByIdAsync(int id)
        {
            var competitionTypes = await _context.CompetitionTypes.FindAsync(id);
            if (competitionTypes == null)
            {
                throw new KeyNotFoundException($"CompetitionType with ID {id} not found.");
            }
            return _mapper.Map<CompetitionTypeDto>(competitionTypes);
        }
        public async Task<CompetitionTypeDto> CreateCompetitionTypeAsync(CompetitionTypeDto competitionType)
        {
            if (competitionType == null)
            {
                throw new ArgumentNullException(nameof(competitionType), "competitionType cannot be null.");
            }

            var competitionTypes = _mapper.Map<CompetitionType>(competitionType);

            await _context.CompetitionTypes.AddAsync(competitionTypes);
            await _context.SaveChangesAsync();

            return _mapper.Map<CompetitionTypeDto>(competitionTypes);
        }
        public async Task<CompetitionTypeDto> UpdateCompetitionTypeAsync(int id, CompetitionTypeDto competitionTypedto)
        {
            if (competitionTypedto == null)
            {
                throw new ArgumentNullException(nameof(competitionTypedto), "competitionType cannot be null.");
            }

            var competitionTypeToUpdate = await _context.CompetitionTypes.FindAsync(id);
            if (competitionTypeToUpdate == null)
            {
                throw new KeyNotFoundException($"competitionType with ID {id} not found.");
            }

            _mapper.Map(competitionTypedto, competitionTypeToUpdate);

            await _context.SaveChangesAsync();
            return _mapper.Map<CompetitionTypeDto>(competitionTypeToUpdate);
        }
        public async Task DeleteCompetitionTypeAsync(int id)
        {
            var competitionType = await _context.CompetitionTypes.FindAsync(id);

            if (competitionType == null) throw new KeyNotFoundException("ไม่พบปี");

            _context.CompetitionTypes.Remove(competitionType);
            await _context.SaveChangesAsync();
        }
   
    }
}
