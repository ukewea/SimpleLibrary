using System.ComponentModel.DataAnnotations;

namespace SimpleLibrary
{
    public enum SpecialEventType : int
    {
        [Display(Name = "國定假日")]
        NATIONAL_HOLIDAY = 20,

        [Display(Name = "補班日")]
        MAKEUP_DAY = 100
    }
}
