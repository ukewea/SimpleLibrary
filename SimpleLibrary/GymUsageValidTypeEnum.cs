using System.ComponentModel.DataAnnotations;

namespace SimpleLibrary
{
    public enum GymUsageValidTypeEnum : int
    {
        [Display(Name = "沒有進入健身房")]
        NO = 0,
        [Display(Name = "有進入健身房")]
        YES = 10,
        [Display(Name = "沒進公司")]
        NO_RECORD = 20,
        [Display(Name = "非工作日")]
        SKIP_NON_WORKDAY = 45,
        [Display(Name = "非工作日")]
        SKIP_HOLIDAY = 46,
        [Display(Name = "非工作日")]
        NO_MAKEUP_DAY = 47,
        [Display(Name = "無生效健身證")]
        INVALID_TIME = 80,
        [Display(Name = "尚未上傳健身紀錄")]
        IMPORT_NOT_YET = 999,
    }
}
