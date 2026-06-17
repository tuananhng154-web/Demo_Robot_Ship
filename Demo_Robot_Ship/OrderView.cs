namespace Demo_Robot_Ship
{
    internal class OrderView
    {
        public string IdText { get; set; }
        public string Room { get; set; }
        public string WeightText { get; set; }
        public string StatusText { get; set; }
        public string RobotText { get; set; }
        public string CreatedText { get; set; }
        public string DeliveredText { get; set; }

        public OrderView(DeliveryOrder order)
        {
            IdText = "#" + order.Id.ToString("000");
            Room = order.Room;
            WeightText = order.WeightKg.ToString("0.0");
            StatusText = ConvertStatus(order.Status);
            RobotText = string.IsNullOrEmpty(order.AssignedRobotId) ? "-" : order.AssignedRobotId;
            CreatedText = order.CreatedAt.ToString("HH:mm:ss");
            DeliveredText = order.DeliveredAt.HasValue ? order.DeliveredAt.Value.ToString("HH:mm:ss") : "-";
        }

        private string ConvertStatus(string status)
        {
            if (status == "WAITING") return "Đang chờ";
            if (status == "ASSIGNED") return "Đã gán";
            if (status == "DELIVERING") return "Đang giao";
            if (status == "DELIVERED") return "Đã giao";
            if (status == "FAILED") return "Lỗi";
            return status;
        }
    }
}
