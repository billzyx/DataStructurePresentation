namespace integrateOfDataStructure.Utility
{
    public class LogVo
    {
        public int LogId { get; set; }

        public string Action { get; set; }

        public string Data { get; set; }

        public int SelectId { get; set; }

        public string Selectdata { get; set; }

        public int TargetId { get; set; }

        public string HideString { get; set; }

        public LogVo() { }

        public LogVo(int logId, string action, int selctId, string data)
        {
            this.LogId = logId;
            this.Action = action;
            this.Data = data;
            this.SelectId = selctId;
        }

        public LogVo(int logId, string action, string selectdata, string data)
        {
            this.LogId = logId;
            this.Action = action;
            this.Data = data;
            this.Selectdata = selectdata;
        }

        public LogVo(int logId, string action, string selectdata, string data,string hideString)
        {
            this.LogId = logId;
            this.Action = action;
            this.Data = data;
            this.Selectdata = selectdata;
            this.HideString = hideString;
        }

        public LogVo(int logId, string action, int selctId, int targetId, string data)
        {
            this.LogId = logId;
            this.Action = action;
            this.Data = data;
            this.SelectId = selctId;
            this.TargetId = targetId;
        }

    }
}
