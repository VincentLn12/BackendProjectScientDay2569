
using ProjectScientDay2568.Models.Dtos;
using ProjectScientDay2568.Models.Dtos.Match;
using ProjectScientDay2568.Models.Dtos.Team;
using ProjectScientDay2568.Models.YourNamespace.Models;

namespace ProjectScientDay2568.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //ปีการเเข่งขัน
            CreateMap<Year, YearDto>();
            CreateMap<YearDto, Year>();
            //ประเภทการเเข่งขัน
            CreateMap<CompetitionType, CompetitionTypeDto>();
            CreateMap<CompetitionTypeDto, CompetitionType>();
        
            //กิจกรรมการเเข่งขัน
            CreateMap<Competition, CompetitionDto>()
                .ForMember(dest => dest.CompetitionTypeName, opt => opt.MapFrom(src => src.CompetitionType.CompetitionTypeName));

            CreateMap<CompetitionDto, Competition>()
                .ForMember(dest => dest.CompetitionType, opt => opt.Ignore()); // optional เพื่อความปลอดภัย

            //ประเภทเเละปีการเเข่งขัน
            CreateMap<CompetitionYear, CompetitionYearDto>()
                            .ForMember(dest => dest.CompetitionName, opt => opt.MapFrom(src => src.Competition.CompetitionName ))
                             .ForMember(dest => dest.RequiresProject, opt => opt.MapFrom(src => src.Competition.RequiresProject))
                            ;

            CreateMap<CompetitionYearDto, CompetitionYear>()
                            .ForMember(dest => dest.Competition, opt => opt.Ignore());
            //ระดับการศึกษา
            CreateMap<EducationLevel, EducationLevelDto>();
            CreateMap<EducationLevelDto, EducationLevel>();

            //รายละเอียดกิจกรรมต่อระดับชั้น
            CreateMap<CompetitionLevel, CompetitionLevelDto>()
                         .ForMember(dest => dest.LevelName, opt => opt.MapFrom(src => src.EducationLevel.LevelName));
            CreateMap<CompetitionLevelDto, CompetitionLevel>()
                                            .ForMember(dest => dest.EducationLevel, opt => opt.Ignore());
            //ประเภทรางวัล
            CreateMap<RewardType, RewardTypeDto>();
            CreateMap<RewardTypeDto, RewardType>();

        
            //ทีม
            CreateMap<Team, TeamDto>()
             .ForMember(dest => dest.LevelName, opt => opt.MapFrom(src => src.CompetitionLevel.EducationLevel.LevelName))
                .ForMember(dest => dest.school_name, opt => opt.MapFrom(src => src.schools.school_name))
                .ForMember(dest => dest.province, opt => opt.MapFrom(src => src.schools.province))
                .ForMember(dest => dest.district, opt => opt.MapFrom(src => src.schools.district))
              .ForMember(dest => dest.CompetitionName,
                 opt => opt.MapFrom(src => src.CompetitionLevel.CompetitionYear.Competition.CompetitionName))
                           .ForMember(dest => dest.RewardType, opt => opt.MapFrom(src => src.RewardType.RewardTypeName))
                               .ForMember(d => d.Students, o => o.MapFrom(s => s.Students));


            CreateMap<TeamDto, Team>()
            .ForMember(dest => dest.TeamID, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Condition(src => src.Status != default))
            .ForMember(dest => dest.school_code, opt => opt.MapFrom(src => src.School_code)) // ✅ แมปค่า FK ให้ชัดเจน
            .ForMember(dest => dest.schools, opt => opt.Ignore()) // ✅ ป้องกันการ override navigation ด้วย null
                .ForMember(d => d.Students, o => o.Ignore()); // ✅ ป้องกัน override คอลเลกชัน



            //นักเรียนในทีม
            CreateMap<Team, TeamCreateDto>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<TeamCreateDto, Team>();

            // TeamProject
            CreateMap<TeamProject, TeamProjectDto>();
            CreateMap<TeamProjectCreateDto, TeamProject>();
            CreateMap<TeamProjectUpdateDto, TeamProject>();


            CreateMap<Team, TeamRegisterDto>()
            .ForMember(dest => dest.RequiresProject, opt => opt.MapFrom(src =>
                         src.CompetitionLevel.CompetitionYear.Competition.RequiresProject));
            
            CreateMap<TeamRegisterDto, Team>();

            //นักเรียนในทีม
            CreateMap<Student, StudentDto>();
            CreateMap<StudentDto, Student>();

            CreateMap<TeamCertificate, TeamCertificateDto>();

            CreateMap<Schools, SchoolDto>();
            CreateMap<SchoolDto, Schools>();

            CreateMap<CertificateConfig, CertificateConfigDto>();
            CreateMap<CertificateConfigDto, CertificateConfig>()
                    .ForMember(d => d.Year, opt => opt.Ignore());

            // ✅ Match -> MatchDto (flatten ทีมและผู้ชนะ)
            CreateMap<Match, MatchDto>()
                .ForMember(d => d.TeamA, o => o.MapFrom(s => s.TeamA))
                .ForMember(d => d.TeamB, o => o.MapFrom(s => s.TeamB))
                .ForMember(d => d.Winner, o => o.MapFrom(s => s.Winner))
            ;

            // (ถ้าต้องการ map ย้อนกลับ)
            CreateMap<MatchDto, Match>()
                .ForMember(d => d.TeamA, o => o.Ignore())
                .ForMember(d => d.TeamB, o => o.Ignore())
                .ForMember(d => d.Winner, o => o.Ignore());

            CreateMap<News, NewsDto>();
            CreateMap<NewsDto, News>()
           .ForMember(d => d.Id, opt => opt.Ignore()) // ❗ ห้ามแก้ Id
           .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}