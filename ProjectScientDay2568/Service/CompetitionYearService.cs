
namespace ProjectScientDay2568.Service
{
    public class CompetitionYearService : ICompetitionYearService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CompetitionYearService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<CompetitionYearDto>> GetCompetitionYearAsync(int YearID)
        {
            var CompetitionYear = await _context.CompetitionYears
                .AsNoTracking()
                .Where(p => p.YearID == YearID)
                .Include(p => p.Year)
                .Include(p => p.Competition)
                .ToListAsync();

            return _mapper.Map<List<CompetitionYearDto>>(CompetitionYear);
        }

        public async Task<CompetitionYearDto> GetCompetitionYearByIdAsync(int id)
        {
            var competitionYear = await _context.CompetitionYears.FindAsync(id);
            if (competitionYear == null)
            {
                throw new KeyNotFoundException($"CompetitionYear with ID {id} not found.");
            }
            return _mapper.Map<CompetitionYearDto>(competitionYear);
        }

        public async Task<CompetitionYearDto> CreateCompetitionYear(CompetitionYearDto competitionYearDto)
        {
            if (competitionYearDto == null)
            {
                throw new ArgumentNullException(nameof(competitionYearDto), "competitionYearDto cannot be null.");
            }

            var competitionYear = _mapper.Map<CompetitionYear>(competitionYearDto);

            await _context.CompetitionYears.AddAsync(competitionYear);
            await _context.SaveChangesAsync();

            return _mapper.Map<CompetitionYearDto>(competitionYear);
        }
        public async Task<CompetitionYearDto> UpdateCompetitionYear(int id, CompetitionYearDto competitionYearDto)
        {
            if (competitionYearDto == null)
            {
                throw new ArgumentNullException(nameof(competitionYearDto), "competitionYearDto cannot be null.");
            }

            var competitionYearToUpdate = await _context.CompetitionYears.FindAsync(id);
            if (competitionYearToUpdate == null)
            {
                throw new KeyNotFoundException($"competitionYearToUpdate with ID {id} not found.");
            }

            _mapper.Map(competitionYearDto, competitionYearToUpdate);

            await _context.SaveChangesAsync();
            return _mapper.Map<CompetitionYearDto>(competitionYearToUpdate);
        }
        public async Task DeleteCompetitionYear(int id)
        {
            var competitionYears = await _context.CompetitionYears.FindAsync(id);

            if (competitionYears == null) throw new KeyNotFoundException("ไม่พบกิจกรรมปีนี้");

            _context.CompetitionYears.Remove(competitionYears);
            await _context.SaveChangesAsync();
        }

      

     
    }
}
