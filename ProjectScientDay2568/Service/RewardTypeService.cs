
namespace ProjectScientDay2568.Service
{
    public class RewardTypeService : IRewardTypeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public RewardTypeService(ApplicationDbContext context , IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<RewardTypeDto>> GetAllRewardTypesAsync()
        {
           var rewardTypes = await _context.RewardTypes.ToListAsync();
           return _mapper.Map<List<RewardTypeDto>>(rewardTypes);
        }
        public async Task<RewardTypeDto?> GetRewardTypeByIdAsync(int id)
        {
            var rewardType = await _context.RewardTypes.FindAsync(id);
            if (rewardType == null)
            {
                throw new KeyNotFoundException($"RewardType with ID {id} not found.");
            }
            return _mapper.Map<RewardTypeDto>(rewardType);
        }
        public async Task<RewardTypeDto> CreateRewardTypeAsync(RewardTypeDto rewardTypeDto)
        {
            if (rewardTypeDto == null)
            {
                throw new ArgumentNullException(nameof(rewardTypeDto), "rewardType cannot be null.");
            }

            var rewardType = _mapper.Map<RewardType>(rewardTypeDto);

            await _context.RewardTypes.AddAsync(rewardType);
            await _context.SaveChangesAsync();

            return _mapper.Map<RewardTypeDto>(rewardType);
        }
        public async Task<RewardTypeDto> UpdateRewardTypeAsync(int id, RewardTypeDto rewardTypeDto)
        {
            if (rewardTypeDto == null)
            {
                throw new ArgumentNullException(nameof(rewardTypeDto), "rewardType cannot be null.");
            }

            var rewardTypeToUpdate = await _context.RewardTypes.FindAsync(id);
            if (rewardTypeToUpdate == null)
            {
                throw new KeyNotFoundException($"rewardType with ID {id} not found.");
            }

            _mapper.Map(rewardTypeDto, rewardTypeToUpdate);

            await _context.SaveChangesAsync();
            return _mapper.Map<RewardTypeDto>(rewardTypeToUpdate);
        }
        public async Task DeleteRewardTypeAsync(int id)
        {
            var rewardType = await _context.RewardTypes.FindAsync(id);

            if (rewardType == null) throw new KeyNotFoundException("rewardType notfound");

            _context.RewardTypes.Remove(rewardType);
            await _context.SaveChangesAsync();
        }
   
    }
}
