namespace ProjectScientDay2568.Service
{
    public class CompetitionService : ICompetitionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CompetitionService(ApplicationDbContext context  , IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<CompetitionDto>> GetCompetitionListAsync()
        {
           var Competition = await _context.Competitions.Include(p => p.CompetitionType).ToListAsync();
            return _mapper.Map<List<CompetitionDto>>(Competition);
        }

        public async Task<CompetitionDto> GetCompetitionByIdAsync(int id)
        {
            var Competition = await _context.Competitions.FindAsync(id);
            if (Competition == null)
            {
                throw new KeyNotFoundException($"Competition with ID {id} not found.");
            }
            return _mapper.Map<CompetitionDto>(Competition);
        }

        public async Task<CompetitionDto> CreateCompetition(CompetitionDto competitionDto)
        {
            if (competitionDto == null)
            {
                throw new ArgumentNullException(nameof(competitionDto), "competitionTypeDto cannot be null.");
            }

            var Competition = _mapper.Map<Competition>(competitionDto);

            await _context.Competitions.AddAsync(Competition);
            await _context.SaveChangesAsync();

            return _mapper.Map<CompetitionDto>(Competition);
        }
        public async Task<CompetitionDto> UpdateCompetition(int id, CompetitionDto competitionDto)
        {
            if (competitionDto == null)
            {
                throw new ArgumentNullException(nameof(competitionDto), "competitionTypeDto cannot be null.");
            }
            var competitionTypeDtoToUpdate = await _context.Competitions.FindAsync(id);
            if (competitionTypeDtoToUpdate == null)
            {
                throw new KeyNotFoundException($"competition with ID {id} not found.");
            }

            _mapper.Map(competitionDto, competitionTypeDtoToUpdate);

            await _context.SaveChangesAsync();
            return _mapper.Map<CompetitionDto>(competitionTypeDtoToUpdate); throw new NotImplementedException();
        }

        public async Task DeleteCompetition(int id)
        {
            var competitions = await _context.Competitions.FindAsync(id);

            if (competitions == null) throw new KeyNotFoundException("ไม่พบกิจกรรม");

            _context.Competitions.Remove(competitions);
            await _context.SaveChangesAsync();
        }
    }
}
