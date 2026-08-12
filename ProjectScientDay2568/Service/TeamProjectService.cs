using ProjectScientDay2568.Models.Dtos.Team;

namespace ProjectScientDay2568.Service
{
    public class TeamProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TeamProjectService(ApplicationDbContext context,IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<TeamProjectDto>> GetByTeamIdAsync(int teamId)
        {
            var projects = await _context.TeamProjects
                .Where(x => x.TeamID == teamId)
                .OrderByDescending(x => x.TeamProjectID)
                .ToListAsync();

            return _mapper.Map<List<TeamProjectDto>>(projects);
        }

        public async Task<TeamProjectDto> GetByIdAsync(int id)
        {
            var project = await _context.TeamProjects
                .FirstOrDefaultAsync(x => x.TeamProjectID == id);

            if (project == null)
                throw new KeyNotFoundException("ไม่พบข้อมูลโครงงาน");

            return _mapper.Map<TeamProjectDto>(project);
        }

        public async Task<TeamProjectDto> CreateAsync(TeamProjectCreateDto dto)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(x => x.TeamID == dto.TeamID);

            if (team == null)
                throw new KeyNotFoundException("ไม่พบทีม");

            var project = new TeamProject
            {
                TeamID = dto.TeamID,
                ProjectName = dto.ProjectName
            };

            if (dto.File != null && dto.File.Length > 0)
            {
                project.ProjectPDFLink = await UploadProjectFile(dto.File);
            }

            team.ProjectName = project.ProjectName;
            if (!string.IsNullOrWhiteSpace(project.ProjectPDFLink))
            {
                team.ProjectPDFLink = project.ProjectPDFLink;
            }

            await _context.TeamProjects.AddAsync(project);
            await _context.SaveChangesAsync();

            return _mapper.Map<TeamProjectDto>(project);
        }

        public async Task<TeamProjectDto> UpdateAsync(int id, TeamProjectUpdateDto dto)
        {
            var project = await _context.TeamProjects
                .FirstOrDefaultAsync(x => x.TeamProjectID == id);

            if (project == null)
                throw new KeyNotFoundException("ไม่พบข้อมูลโครงงาน");

            project.ProjectName = dto.ProjectName;

            if (dto.File != null && dto.File.Length > 0)
            {
                project.ProjectPDFLink = await UploadProjectFile(dto.File);
            }

            var team = await _context.Teams.FirstOrDefaultAsync(x => x.TeamID == project.TeamID);
            if (team != null)
            {
                team.ProjectName = project.ProjectName;
                team.ProjectPDFLink = project.ProjectPDFLink;
            }

            await _context.SaveChangesAsync();

            return _mapper.Map<TeamProjectDto>(project);
        }

        public async Task DeleteAsync(int id)
        {
            var project = await _context.TeamProjects
                .FirstOrDefaultAsync(x => x.TeamProjectID == id);

            if (project == null)
                throw new KeyNotFoundException("ไม่พบข้อมูลโครงงาน");

            _context.TeamProjects.Remove(project);
            await _context.SaveChangesAsync();
        }

        private async Task<string> UploadProjectFile(IFormFile file)
        {
            var uploadsFolder = Path.Combine("wwwroot", "uploads", "projects");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/projects/{uniqueFileName}";
        }
    }
}
