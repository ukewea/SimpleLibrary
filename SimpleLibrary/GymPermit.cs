namespace SimpleLibrary
{
    public class GymPermit 
    {
        public Guid PermitId { get; set; }

        /// <summary>
        /// 生效開始時間
        /// </summary>
        public DateTime? ValidTime { get; set; }

        /// <summary>
        /// 生效結束時間
        /// </summary>
        public DateTime? InvalidTime { get; set; }
    }
}
