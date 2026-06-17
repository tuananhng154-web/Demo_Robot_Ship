using System;

namespace Demo_Robot_Ship
{
    public class DeliveryOrder
    {
        public int Id { get; set; }
        public string Room { get; set; }
        public double WeightKg { get; set; }
        public Node Target { get; set; }
        public string Status { get; set; }
        public string AssignedRobotId { get; set; }
        public string BatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public int CreatedTick { get; set; }
        public int AssignedTick { get; set; }
        public int DeliveredTick { get; set; }

        public string TargetKey
        {
            get { return Target == null ? "" : Target.Key(); }
        }

        public DeliveryOrder(int id, string room, double weightKg, Node target) : this(id, room, weightKg, target, 0)
        {
        }

        public DeliveryOrder(int id, string room, double weightKg, Node target, int createdTick)
        {
            Id = id;
            Room = room;
            WeightKg = weightKg;
            Target = target;
            Status = "WAITING";
            AssignedRobotId = "";
            BatchId = "";
            CreatedAt = DateTime.Now;
            CreatedTick = createdTick;
            AssignedTick = -1;
            DeliveredTick = -1;
        }

        public int GetWaitTicks(int currentTick)
        {
            return Math.Max(0, currentTick - CreatedTick);
        }

        public override string ToString()
        {
            return string.Format("#{0:000} | Phòng {1} | {2:0.0} kg | {3}", Id, Room, WeightKg, Status);
        }
    }
}
