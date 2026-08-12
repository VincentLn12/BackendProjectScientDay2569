namespace ProjectScientDay2568.Interface
{
    public interface IRewardTypeService
    {
        Task<List<RewardTypeDto>> GetAllRewardTypesAsync();
        Task<RewardTypeDto?> GetRewardTypeByIdAsync(int id);
        Task<RewardTypeDto> CreateRewardTypeAsync(RewardTypeDto rewardTypeDto);
        Task<RewardTypeDto> UpdateRewardTypeAsync(int id, RewardTypeDto rewardTypeDto);
        Task DeleteRewardTypeAsync(int id);
    }
}
