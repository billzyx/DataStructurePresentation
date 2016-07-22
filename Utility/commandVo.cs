namespace integrateOfDataStructure.Utility
{
    class CommandVo
    {
        public string Operation { get; set; }

        private int _startId = -1;

        public int StartId
        {
            get { return _startId; }
            set { _startId = value; }
        }

        private int _targetId=-1;

        public int TargetId
        {
            get { return _targetId; }
            set { _targetId = value; }
        }

        public string Data { get; set; }

        private int _step=-1;

        public CommandVo()
        {
            Data = null;
        }

        public int Step
        {
            get { return _step; }
            set { _step = value; }
        }

    }
}
